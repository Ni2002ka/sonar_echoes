using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class EchoScanSetupEditor
{
    const string EchoShaderName = "Custom/EchoSegmentScanSurface";
    const string GeneratedFolder = "Assets/Materials/EchoScanGenerated";
    const string LitFolder = "Assets/Resources/EchoLit";

    [MenuItem("Tools/Echo Scan/Setup Mine Scene (Fix Pink)")]
    static void SetupMineScene()
    {
        Shader echoShader = Shader.Find(EchoShaderName);

        if (echoShader == null)
        {
            EditorUtility.DisplayDialog(
                "Echo Scan",
                "Echo shader not found. Check the Console for compile errors on EchoScanSurface.shader.",
                "OK"
            );
            return;
        }

        int count = 0;
        MeshRenderer[] renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);

        foreach (MeshRenderer renderer in renderers)
        {
            if (ShouldSkipRenderer(renderer))
            {
                continue;
            }

            count += ApplyToRenderer(renderer);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog(
            "Echo Scan",
            $"Updated {count} mesh renderer(s).",
            "OK"
        );

        BuildMaterialCatalog();
        WireLightingController();

        Debug.Log($"Echo scan mine setup complete. Updated {count} renderer(s).");
    }

    [MenuItem("Tools/Echo Scan/Build Material Catalog")]
    public static void BuildMaterialCatalog()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        const string catalogPath = "Assets/Resources/EchoMaterialCatalog.asset";
        EchoMaterialCatalog catalog = AssetDatabase.LoadAssetAtPath<EchoMaterialCatalog>(catalogPath);

        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<EchoMaterialCatalog>();
            AssetDatabase.CreateAsset(catalog, catalogPath);
        }

        List<EchoMaterialCatalog.Entry> entries = new List<EchoMaterialCatalog.Entry>();

        if (!AssetDatabase.IsValidFolder(GeneratedFolder))
        {
            Debug.LogWarning("No generated echo materials found. Run Setup Mine Scene first.");
            catalog.entries = entries.ToArray();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return;
        }

        string[] echoGuids = AssetDatabase.FindAssets("t:Material", new[] { GeneratedFolder });

        foreach (string guid in echoGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material echoMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (echoMaterial == null || !echoMaterial.name.StartsWith("EchoScan_"))
            {
                continue;
            }

            string baseName = echoMaterial.name.Substring("EchoScan_".Length);
            Material original = FindMineMaterial(baseName);

            if (original == null)
            {
                Debug.LogWarning($"Could not find original mine material for {echoMaterial.name}.");
                continue;
            }

            entries.Add(new EchoMaterialCatalog.Entry
            {
                echoMaterial = echoMaterial,
                originalMaterial = GetOrCreateLitMaterial(original)
            });
        }

        catalog.entries = entries.ToArray();
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();

        Debug.Log($"Echo material catalog built with {entries.Count} URP Lit mapping(s).");
    }

    [MenuItem("Tools/Echo Scan/Build URP Lit Materials")]
    public static void BuildUrpLitMaterials()
    {
        BuildMaterialCatalog();
    }

    [MenuItem("Tools/Echo Scan/Regenerate Resources Lit Materials (Quest)")]
    public static void RegenerateResourcesLitMaterials()
    {
        string scriptPath = $"{Application.dataPath}/Editor/GenerateEchoLitMaterials.py";

        if (!System.IO.File.Exists(scriptPath))
        {
            Debug.LogError("GenerateEchoLitMaterials.py not found.");
            return;
        }

        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "python3",
            Arguments = $"\"{scriptPath}\"",
            WorkingDirectory = Application.dataPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using (System.Diagnostics.Process process = System.Diagnostics.Process.Start(startInfo))
        {
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (!string.IsNullOrEmpty(output))
            {
                Debug.Log(output);
            }

            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError(error);
            }
        }

        AssetDatabase.Refresh();
        WireLightingController();
    }

    [MenuItem("Tools/Echo Scan/Wire Lighting Controller On EchoSystem")]
    public static void WireLightingController()
    {
        EchoLightingController lighting = Object.FindFirstObjectByType<EchoLightingController>();

        if (lighting == null)
        {
            EchoPulseController pulse = Object.FindFirstObjectByType<EchoPulseController>();

            if (pulse == null)
            {
                Debug.LogWarning("No EchoPulseController found in the scene.");
                return;
            }

            lighting = pulse.gameObject.AddComponent<EchoLightingController>();
        }

        EchoMaterialCatalog catalog = AssetDatabase.LoadAssetAtPath<EchoMaterialCatalog>(
            "Assets/Resources/EchoMaterialCatalog.asset"
        );

        if (catalog == null)
        {
            BuildMaterialCatalog();
            catalog = AssetDatabase.LoadAssetAtPath<EchoMaterialCatalog>(
                "Assets/Resources/EchoMaterialCatalog.asset"
            );
        }

        lighting.materialCatalog = catalog;
        EditorUtility.SetDirty(lighting);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("EchoLightingController wired to EchoMaterialCatalog.");
    }

    static Material FindMineMaterial(string materialName)
    {
        string[] guids = AssetDatabase.FindAssets($"{materialName} t:Material", new[] { "Assets/Mine" });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material != null && material.name == materialName)
            {
                return material;
            }
        }

        return null;
    }

    [MenuItem("Tools/Echo Scan/Add VR Floor To Mine Scene")]
    static void AddVrFloor()
    {
        if (GameObject.Find("EchoScan_Floor") != null)
        {
            Debug.LogWarning("EchoScan_Floor already exists in this scene.");
            return;
        }

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "EchoScan_Floor";
        floor.transform.localScale = new Vector3(8f, 1f, 8f);

        MeshRenderer renderer = floor.GetComponent<MeshRenderer>();
        Material stoneEcho = AssetDatabase.LoadAssetAtPath<Material>($"{GeneratedFolder}/EchoScan_Stone.mat");

        if (stoneEcho != null)
        {
            renderer.sharedMaterial = stoneEcho;
        }

        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.shadowCastingMode = ShadowCastingMode.Off;

        if (GameObject.Find("OVRCameraRig") != null)
        {
            Transform rig = GameObject.Find("OVRCameraRig").transform;
            floor.transform.position = new Vector3(rig.position.x, rig.position.y - 0.05f, rig.position.z);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Added EchoScan_Floor under the camera rig.");
    }

    [MenuItem("Tools/Echo Scan/Repair Generated Materials (Fix Pink)")]
    static void RepairGeneratedMaterials()
    {
        Shader echoShader = Shader.Find(EchoShaderName);

        if (echoShader == null)
        {
            Debug.LogError("Echo scan shader not found.");
            return;
        }

        if (!AssetDatabase.IsValidFolder(GeneratedFolder))
        {
            Debug.LogWarning("No generated echo materials folder found. Run Setup Mine Scene first.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { GeneratedFolder });
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null || !material.name.StartsWith("EchoScan_"))
            {
                continue;
            }

            material.shader = echoShader;
            EditorUtility.SetDirty(material);
            count++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Repaired {count} echo scan material(s) for URP.");
    }

    [MenuItem("Tools/Echo Scan/Apply To Selected (Keep Textures)")]
    static void ApplyToSelected()
    {
        if (Selection.gameObjects.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Echo Scan",
                "Select the mine root object in the Hierarchy first.",
                "OK"
            );
            return;
        }

        int count = 0;

        foreach (GameObject root in Selection.gameObjects)
        {
            MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);

            foreach (MeshRenderer renderer in renderers)
            {
                if (!ShouldSkipRenderer(renderer))
                {
                    count += ApplyToRenderer(renderer);
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log($"Echo scan materials applied to {count} renderer(s).");
    }

    static bool ShouldSkipRenderer(MeshRenderer renderer)
    {
        return renderer.gameObject.name.Contains("Lantern");
    }

    static int ApplyToRenderer(MeshRenderer renderer)
    {
        Material[] materials = renderer.sharedMaterials;
        bool changed = false;

        for (int i = 0; i < materials.Length; i++)
        {
            Material source = materials[i];

            if (source == null)
            {
                continue;
            }

            if (source.shader != null && source.shader.name == EchoShaderName)
            {
                continue;
            }

            if (!ShouldConvertMaterial(source))
            {
                continue;
            }

            materials[i] = GetOrCreateEchoMaterial(source);
            changed = true;
        }

        if (changed)
        {
            renderer.sharedMaterials = materials;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            EditorUtility.SetDirty(renderer);
            return 1;
        }

        return 0;
    }

    static bool ShouldConvertMaterial(Material source)
    {
        if (source.shader == null)
        {
            return false;
        }

        string shaderName = source.shader.name;

        if (shaderName == EchoShaderName)
        {
            return false;
        }

        if (shaderName == "Standard" || shaderName.StartsWith("Universal Render Pipeline/"))
        {
            return true;
        }

        string path = AssetDatabase.GetAssetPath(source);

        return !string.IsNullOrEmpty(path) && path.Contains("/Mine/");
    }

    static Material GetOrCreateEchoMaterial(Material source)
    {
        EnsureFolder(GeneratedFolder);

        string safeName = source.name.Replace(" ", "_");
        string assetPath = $"{GeneratedFolder}/EchoScan_{safeName}.mat";

        Shader echoShader = Shader.Find(EchoShaderName);

        if (echoShader == null)
        {
            Debug.LogError("Echo scan shader not found.");
            return source;
        }

        Material existing = AssetDatabase.LoadAssetAtPath<Material>(assetPath);

        if (existing != null)
        {
            existing.shader = echoShader;

            if (source.HasProperty("_MainTex"))
            {
                existing.SetTexture("_MainTex", source.GetTexture("_MainTex"));
            }

            existing.SetFloat("_AlbedoStrength", 0.7f);
            existing.SetColor("_BaseColor", new Color(0.12f, 0.12f, 0.14f, 1f));
            existing.SetColor("_EchoColor", Color.cyan);
            existing.SetFloat("_EchoRadius", -100f);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        Material echoMaterial = new Material(echoShader);
        echoMaterial.name = $"EchoScan_{safeName}";

        if (source.HasProperty("_MainTex"))
        {
            Texture mainTex = source.GetTexture("_MainTex");
            echoMaterial.SetTexture("_MainTex", mainTex);
            echoMaterial.SetFloat("_AlbedoStrength", 0.7f);
        }
        else
        {
            echoMaterial.SetFloat("_AlbedoStrength", 0f);
        }

        echoMaterial.SetColor("_BaseColor", new Color(0.12f, 0.12f, 0.14f, 1f));
        echoMaterial.SetColor("_EchoColor", Color.cyan);
        echoMaterial.SetFloat("_EchoRadius", -100f);

        AssetDatabase.CreateAsset(echoMaterial, assetPath);
        AssetDatabase.SaveAssets();

        return echoMaterial;
    }

    static Material GetOrCreateLitMaterial(Material source)
    {
        EnsureLitFolder();

        string safeName = source.name.Replace(" ", "_");
        string assetPath = $"{LitFolder}/Lit_{safeName}.mat";
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");

        if (litShader == null)
        {
            Debug.LogError("URP Lit shader not found.");
            return source;
        }

        Material existing = AssetDatabase.LoadAssetAtPath<Material>(assetPath);

        if (existing != null)
        {
            existing.shader = litShader;
            EchoLitMaterialUtility.CopyStandardToLit(source, existing);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        Material litMaterial = new Material(litShader)
        {
            name = $"Lit_{safeName}"
        };

        EchoLitMaterialUtility.CopyStandardToLit(source, litMaterial);

        AssetDatabase.CreateAsset(litMaterial, assetPath);
        AssetDatabase.SaveAssets();

        return litMaterial;
    }

    static void EnsureLitFolder()
    {
        if (AssetDatabase.IsValidFolder(LitFolder))
        {
            return;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        AssetDatabase.CreateFolder("Assets/Materials", "EchoLitGenerated");
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        AssetDatabase.CreateFolder("Assets/Materials", "EchoScanGenerated");
    }
}
