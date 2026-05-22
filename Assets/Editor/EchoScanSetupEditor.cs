using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class EchoScanSetupEditor
{
    const string EchoShaderName = "Custom/EchoSegmentScanSurface";
    const string GeneratedFolder = "Assets/Materials/EchoScanGenerated";

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

        Debug.Log($"Echo scan mine setup complete. Updated {count} renderer(s).");
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
