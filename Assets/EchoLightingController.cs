using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class EchoLightingController : MonoBehaviour
{
    const string EchoShaderName = "Custom/EchoSegmentScanSurface";

    [Header("Material restore")]
    public EchoMaterialCatalog materialCatalog;
    public Material urpLitReference;

    [Header("Toggle")]
    public bool lightsOn;
    public KeyCode toggleKey = KeyCode.L;

    [Header("Scene lights")]
    public bool controlLanternLights = true;

    readonly List<RendererBinding> bindings = new List<RendererBinding>();
    readonly List<Light> lanternLights = new List<Light>();
    readonly List<bool> lanternLightStates = new List<bool>();
    readonly List<MeshRenderer> lanternRenderers = new List<MeshRenderer>();
    readonly List<ParticleSystem> lanternFlames = new List<ParticleSystem>();

    struct RendererBinding
    {
        public MeshRenderer renderer;
        public Material[] echoMaterials;
        public Material[] litMaterials;
        public LightProbeUsage echoLightProbes;
        public ReflectionProbeUsage echoReflectionProbes;
        public ShadowCastingMode echoShadows;
    }

    void Awake()
    {
        if (materialCatalog == null)
        {
            materialCatalog = Resources.Load<EchoMaterialCatalog>("EchoMaterialCatalog");
        }

        if (urpLitReference == null)
        {
            urpLitReference = Resources.Load<Material>("EchoLit/Lit_Stone");
        }

        if (urpLitReference != null)
        {
            EchoLitMaterialUtility.SetLitShader(urpLitReference.shader);
        }

        CacheRenderers();
        CacheLanterns();
        ApplyLightsState(lightsOn);
    }

    void Update()
    {
        if (GetOvrButtonDown(OVRInput.Button.Three) || WasKeyPressed(toggleKey))
        {
            SetLightsOn(!lightsOn);
        }
    }

    public void SetLightsOn(bool enabled)
    {
        lightsOn = enabled;
        ApplyLightsState(lightsOn);
        Debug.Log(lightsOn ? "Lights on (original materials)" : "Lights off (echo scan)");
    }

    void ApplyLightsState(bool enabled)
    {
        for (int i = 0; i < bindings.Count; i++)
        {
            RendererBinding binding = bindings[i];

            if (binding.renderer == null)
            {
                continue;
            }

            binding.renderer.sharedMaterials = enabled ? binding.litMaterials : binding.echoMaterials;

            if (enabled)
            {
                binding.renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
                binding.renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
                binding.renderer.shadowCastingMode = ShadowCastingMode.On;
            }
            else
            {
                binding.renderer.lightProbeUsage = binding.echoLightProbes;
                binding.renderer.reflectionProbeUsage = binding.echoReflectionProbes;
                binding.renderer.shadowCastingMode = binding.echoShadows;
            }
        }

        if (!controlLanternLights)
        {
            return;
        }

        for (int i = 0; i < lanternLights.Count; i++)
        {
            Light light = lanternLights[i];

            if (light == null)
            {
                continue;
            }

            light.enabled = enabled && lanternLightStates[i];
        }

        for (int i = 0; i < lanternRenderers.Count; i++)
        {
            MeshRenderer renderer = lanternRenderers[i];

            if (renderer != null)
            {
                renderer.enabled = enabled;
            }
        }

        for (int i = 0; i < lanternFlames.Count; i++)
        {
            ParticleSystem flame = lanternFlames[i];

            if (flame == null)
            {
                continue;
            }

            flame.gameObject.SetActive(enabled);

            if (enabled)
            {
                flame.Play();
            }
            else
            {
                flame.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }

    void CacheRenderers()
    {
        bindings.Clear();

        MeshRenderer[] renderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);

        foreach (MeshRenderer renderer in renderers)
        {
            if (ShouldSkipRenderer(renderer))
            {
                continue;
            }

            Material[] current = renderer.sharedMaterials;
            Material[] echo = new Material[current.Length];
            Material[] lit = new Material[current.Length];
            bool usesEcho = false;

            for (int i = 0; i < current.Length; i++)
            {
                Material slot = current[i];
                echo[i] = slot;

                if (IsEchoMaterial(slot))
                {
                    usesEcho = true;
                    lit[i] = ResolveOriginalMaterial(slot);
                }
                else
                {
                    lit[i] = slot;
                }
            }

            if (!usesEcho)
            {
                continue;
            }

            bindings.Add(new RendererBinding
            {
                renderer = renderer,
                echoMaterials = echo,
                litMaterials = lit,
                echoLightProbes = renderer.lightProbeUsage,
                echoReflectionProbes = renderer.reflectionProbeUsage,
                echoShadows = renderer.shadowCastingMode
            });
        }
    }

    void CacheLanterns()
    {
        lanternLights.Clear();
        lanternLightStates.Clear();
        lanternRenderers.Clear();
        lanternFlames.Clear();

        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);

        foreach (Light light in lights)
        {
            if (light == null)
            {
                continue;
            }

            if (IsUnderLantern(light.transform))
            {
                lanternLights.Add(light);
                lanternLightStates.Add(light.enabled);
            }
        }

        MeshRenderer[] renderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);

        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer == null || ShouldSkipRenderer(renderer))
            {
                continue;
            }

            if (IsUnderLantern(renderer.transform))
            {
                lanternRenderers.Add(renderer);
            }
        }

        ParticleSystem[] flames = FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);

        foreach (ParticleSystem flame in flames)
        {
            if (flame != null && IsUnderLantern(flame.transform))
            {
                lanternFlames.Add(flame);
            }
        }
    }

    static bool IsUnderLantern(Transform transform)
    {
        Transform root = transform;

        while (root != null)
        {
            if (root.name.Contains("Lantern"))
            {
                return true;
            }

            root = root.parent;
        }

        return false;
    }

    Material ResolveOriginalMaterial(Material echoMaterial)
    {
        Material source = null;

        if (materialCatalog != null && materialCatalog.TryGetOriginal(echoMaterial, out Material original))
        {
            source = original;
        }

        if (source == null)
        {
            Debug.LogWarning($"No original material mapped for {echoMaterial.name}.");
            return echoMaterial;
        }

        if (EchoLitMaterialUtility.IsUrpLit(source))
        {
            return source;
        }

        return EchoLitMaterialUtility.GetLitMaterial(source);
    }

    static bool IsEchoMaterial(Material material)
    {
        return material != null
            && material.shader != null
            && material.shader.name == EchoShaderName;
    }

    static bool ShouldSkipRenderer(MeshRenderer renderer)
    {
        if (renderer == null)
        {
            return true;
        }

        string name = renderer.gameObject.name;

        if (name.Contains("OVRCameraRig")
            || name.Contains("CenterEye")
            || name.Contains("Hand")
            || name.Contains("Controller"))
        {
            return true;
        }

        Transform root = renderer.transform;

        while (root != null)
        {
            if (root.name.Contains("OVRCameraRig"))
            {
                return true;
            }

            root = root.parent;
        }

        return false;
    }

    static bool GetOvrButtonDown(OVRInput.Button button)
    {
        try
        {
            return OVRInput.GetDown(button);
        }
        catch
        {
            return false;
        }
    }

    static bool WasKeyPressed(KeyCode keyCode)
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null)
        {
            return false;
        }

        if (keyCode == KeyCode.L)
        {
            return Keyboard.current.lKey.wasPressedThisFrame;
        }

        return false;
#else
        return Input.GetKeyDown(keyCode);
#endif
    }
}
