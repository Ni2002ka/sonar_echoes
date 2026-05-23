using System.Collections.Generic;
using UnityEngine;

public static class EchoLitMaterialUtility
{
    const string LitShaderName = "Universal Render Pipeline/Lit";

    static Shader litShaderOverride;
    static readonly Dictionary<Material, Material> RuntimeLitCache = new Dictionary<Material, Material>();

    public static void SetLitShader(Shader shader)
    {
        litShaderOverride = shader;
    }

    public static bool IsUrpLit(Material material)
    {
        return material != null
            && material.shader != null
            && material.shader.name == LitShaderName;
    }

    public static Material GetLitMaterial(Material source)
    {
        if (source == null)
        {
            return null;
        }

        if (IsUrpLit(source))
        {
            return source;
        }

        if (RuntimeLitCache.TryGetValue(source, out Material cached))
        {
            return cached;
        }

        Shader litShader = litShaderOverride != null ? litShaderOverride : Shader.Find(LitShaderName);

        if (litShader == null)
        {
            Debug.LogError("URP Lit shader not found. Assign urpLitReference on EchoLightingController.");
            return source;
        }

        Material litMaterial = new Material(litShader)
        {
            name = $"Lit_{source.name}"
        };

        CopyStandardToLit(source, litMaterial);
        RuntimeLitCache[source] = litMaterial;
        return litMaterial;
    }

    public static void CopyStandardToLit(Material source, Material lit)
    {
        if (source == null || lit == null)
        {
            return;
        }

        lit.globalIlluminationFlags = source.globalIlluminationFlags;
        lit.doubleSidedGI = source.doubleSidedGI;

        if (source.HasProperty("_Color") && lit.HasProperty("_BaseColor"))
        {
            lit.SetColor("_BaseColor", source.GetColor("_Color"));
        }

        if (source.HasProperty("_MainTex") && lit.HasProperty("_BaseMap"))
        {
            Texture mainTex = source.GetTexture("_MainTex");
            lit.SetTexture("_BaseMap", mainTex);

            if (mainTex != null)
            {
                lit.SetTextureScale("_BaseMap", source.GetTextureScale("_MainTex"));
                lit.SetTextureOffset("_BaseMap", source.GetTextureOffset("_MainTex"));
            }
        }

        if (source.HasProperty("_BumpMap") && lit.HasProperty("_BumpMap"))
        {
            Texture bump = source.GetTexture("_BumpMap");

            if (bump != null)
            {
                lit.SetTexture("_BumpMap", bump);
                lit.EnableKeyword("_NORMALMAP");
            }
        }

        if (source.HasProperty("_Metallic") && lit.HasProperty("_Metallic"))
        {
            lit.SetFloat("_Metallic", source.GetFloat("_Metallic"));
        }

        if (source.HasProperty("_Glossiness") && lit.HasProperty("_Smoothness"))
        {
            lit.SetFloat("_Smoothness", source.GetFloat("_Glossiness"));
        }
        else if (source.HasProperty("_GlossMapScale") && lit.HasProperty("_Smoothness"))
        {
            lit.SetFloat("_Smoothness", source.GetFloat("_GlossMapScale"));
        }

        if (source.HasProperty("_OcclusionMap") && lit.HasProperty("_OcclusionMap"))
        {
            Texture occlusion = source.GetTexture("_OcclusionMap");

            if (occlusion != null)
            {
                lit.SetTexture("_OcclusionMap", occlusion);
            }
        }

        if (source.HasProperty("_ParallaxMap") && lit.HasProperty("_ParallaxMap"))
        {
            Texture parallax = source.GetTexture("_ParallaxMap");

            if (parallax != null)
            {
                lit.SetTexture("_ParallaxMap", parallax);
            }
        }

        if (source.IsKeywordEnabled("_EMISSION") || source.HasProperty("_EmissionColor"))
        {
            if (lit.HasProperty("_EmissionColor"))
            {
                Color emission = source.HasProperty("_EmissionColor")
                    ? source.GetColor("_EmissionColor")
                    : Color.black;
                lit.SetColor("_EmissionColor", emission);
            }

            if (source.HasProperty("_EmissionMap") && lit.HasProperty("_EmissionMap"))
            {
                Texture emissionMap = source.GetTexture("_EmissionMap");

                if (emissionMap != null)
                {
                    lit.SetTexture("_EmissionMap", emissionMap);
                }
            }

            lit.EnableKeyword("_EMISSION");
            lit.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
    }
}
