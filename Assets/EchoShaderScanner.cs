using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EchoSegmentShaderScanner : MonoBehaviour
{
    public Material scanMaterial;

    [Header("Scan Motion")]
    public float scanDuration = 2.5f;
    public float maxDistance = 15f;
    public bool useSpeedOfSound = true;
    public float speedOfSound = 343f;
    public float travelSpeedMultiplier = 0.35f;

    [Header("Cone")]
    public float coneAngleDegrees = 18f;

    [Header("Appearance")]
    public float coneSoftness = 0.08f;
    public float ringWidth = 0.45f;
    public float waveTrail = 1.1f;
    public float waveFade = 0.65f;
    public float echoIntensity = 3.5f;

    public Color baseColor = new Color(0.02f, 0.02f, 0.025f, 1f);
    public Color echoColor = Color.cyan;

    private Coroutine routine;
    private readonly List<Material> scanMaterials = new List<Material>();

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int EchoColorId = Shader.PropertyToID("_EchoColor");
    static readonly int HeadPosId = Shader.PropertyToID("_HeadPos");
    static readonly int HeadForwardId = Shader.PropertyToID("_HeadForward");
    static readonly int ConeAngleRadId = Shader.PropertyToID("_ConeAngleRad");
    static readonly int ConeSoftnessId = Shader.PropertyToID("_ConeSoftness");
    static readonly int RingWidthId = Shader.PropertyToID("_RingWidth");
    static readonly int WaveTrailId = Shader.PropertyToID("_WaveTrail");
    static readonly int WaveFadeId = Shader.PropertyToID("_WaveFade");
    static readonly int EchoIntensityId = Shader.PropertyToID("_EchoIntensity");
    static readonly int EchoRadiusId = Shader.PropertyToID("_EchoRadius");

    public void StartScan(Vector3 origin, Vector3 forward, float farthestHit)
    {
        if (scanMaterial == null)
        {
            Debug.LogError("Missing scan material.");
            return;
        }

        RefreshScanMaterials();

        if (scanMaterials.Count == 0)
        {
            Debug.LogWarning("No echo scan materials found in scene.");
            return;
        }

        if (routine != null)
        {
            StopCoroutine(routine);
        }

        routine = StartCoroutine(ScanRoutine(origin, forward, farthestHit));
    }

    IEnumerator ScanRoutine(Vector3 origin, Vector3 forward, float farthestHit)
    {
        float coneRad = coneAngleDegrees * Mathf.Deg2Rad;

        ApplyToAllMaterials(mat =>
        {
            mat.SetColor(BaseColorId, baseColor);
            mat.SetColor(EchoColorId, echoColor);
            mat.SetVector(HeadPosId, origin);
            mat.SetVector(HeadForwardId, forward.normalized);
            mat.SetFloat(ConeAngleRadId, coneRad);
            mat.SetFloat(ConeSoftnessId, coneSoftness);
            mat.SetFloat(RingWidthId, ringWidth);
            mat.SetFloat(WaveTrailId, waveTrail);
            mat.SetFloat(WaveFadeId, waveFade);
            mat.SetFloat(EchoIntensityId, echoIntensity);
        });

        float scanMax = Mathf.Min(maxDistance, farthestHit + ringWidth + waveTrail);
        float duration = scanDuration;

        if (useSpeedOfSound && speedOfSound > 0.01f)
        {
            duration = scanMax / (speedOfSound * travelSpeedMultiplier);
        }

        duration = Mathf.Max(duration, 0.5f);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float radius = Mathf.Lerp(0f, scanMax, elapsed / duration);

            ApplyToAllMaterials(mat => mat.SetFloat(EchoRadiusId, radius));

            yield return null;
        }

        float fadeElapsed = 0f;
        const float fadeOutTime = 0.4f;

        while (fadeElapsed < fadeOutTime)
        {
            fadeElapsed += Time.deltaTime;
            float fade = 1f - fadeElapsed / fadeOutTime;

            ApplyToAllMaterials(mat => mat.SetFloat(EchoIntensityId, echoIntensity * fade));

            yield return null;
        }

        ApplyToAllMaterials(mat =>
        {
            mat.SetFloat(EchoRadiusId, -100f);
            mat.SetFloat(EchoIntensityId, echoIntensity);
        });

        routine = null;
    }

    void RefreshScanMaterials()
    {
        scanMaterials.Clear();

        if (scanMaterial != null)
        {
            AddMaterial(scanMaterial);
        }

        Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);

        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.sharedMaterials;

            for (int i = 0; i < materials.Length; i++)
            {
                AddMaterial(materials[i]);
            }
        }
    }

    void AddMaterial(Material material)
    {
        if (material == null || material.shader == null)
        {
            return;
        }

        if (material.shader.name != "Custom/EchoSegmentScanSurface")
        {
            return;
        }

        if (!scanMaterials.Contains(material))
        {
            scanMaterials.Add(material);
        }
    }

    void ApplyToAllMaterials(System.Action<Material> apply)
    {
        foreach (Material material in scanMaterials)
        {
            apply(material);
        }
    }
}
