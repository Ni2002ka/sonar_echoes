using UnityEngine;
using System.Collections;

public class EchoSegmentShaderScanner : MonoBehaviour
{
    public Material scanMaterial;
    public Transform head;

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

    public void StartScan(Vector3 origin, Vector3 forward, float farthestHit)
    {
        if (scanMaterial == null)
        {
            Debug.LogError("Missing scan material.");
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
        scanMaterial.SetColor("_BaseColor", baseColor);
        scanMaterial.SetColor("_EchoColor", echoColor);
        scanMaterial.SetVector("_HeadPos", origin);
        scanMaterial.SetVector("_HeadForward", forward.normalized);

        float coneRad = coneAngleDegrees * Mathf.Deg2Rad;
        scanMaterial.SetFloat("_ConeAngleRad", coneRad);
        scanMaterial.SetFloat("_ConeSoftness", coneSoftness);
        scanMaterial.SetFloat("_RingWidth", ringWidth);
        scanMaterial.SetFloat("_WaveTrail", waveTrail);
        scanMaterial.SetFloat("_WaveFade", waveFade);
        scanMaterial.SetFloat("_EchoIntensity", echoIntensity);

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
            scanMaterial.SetFloat("_EchoRadius", radius);

            yield return null;
        }

        float fadeElapsed = 0f;
        const float fadeOutTime = 0.4f;

        while (fadeElapsed < fadeOutTime)
        {
            fadeElapsed += Time.deltaTime;
            float fade = 1f - fadeElapsed / fadeOutTime;
            scanMaterial.SetFloat("_EchoIntensity", echoIntensity * fade);
            yield return null;
        }

        scanMaterial.SetFloat("_EchoRadius", -100f);
        scanMaterial.SetFloat("_EchoIntensity", echoIntensity);

        routine = null;
    }
}
