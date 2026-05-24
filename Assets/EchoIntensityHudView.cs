using UnityEngine;
using System.Collections;

public class EchoIntensityHudView : MonoBehaviour
{
    EchoPulseController controller;
    Transform hudRoot;
    LineRenderer backgroundLine;
    LineRenderer fillLine;

    static Material hudMaterial;

    [Header("HUD (fixed in view)")]
    public Vector3 localOffset = new Vector3(0f, -0.07f, 0.38f);
    public float barWidth = 0.16f;
    public float lineWidth = 0.01f;

    [Header("Visibility")]
    public float holdAfterRelease = 0.4f;
    public float fadeOutDuration = 0.7f;

    [Header("Colors")]
    public Color backgroundColor = new Color(0.12f, 0.12f, 0.14f, 0.95f);
    public Color fillColorLow = new Color(0f, 0.45f, 0.5f, 1f);
    public Color fillColorHigh = new Color(0.3f, 1f, 1f, 1f);

    bool isBuilt;
    float lastAdjustTime = -100f;
    float displayAlpha;

    public void Initialize(EchoPulseController pulseController)
    {
        controller = pulseController;
        StartCoroutine(BuildHud());
    }

    public void NotifyAdjusting(bool adjusting)
    {
        if (adjusting)
        {
            lastAdjustTime = Time.time;
        }
    }

    IEnumerator BuildHud()
    {
        if (isBuilt)
        {
            yield break;
        }

        Transform eye = null;

        for (int frame = 0; frame < 240; frame++)
        {
            if (controller != null)
            {
                controller.ResolveHead();
                eye = controller.head;
            }

            if (eye == null)
            {
                GameObject centerEye = GameObject.Find("CenterEyeAnchor");
                if (centerEye != null)
                {
                    eye = centerEye.transform;
                }
            }

            if (eye == null && Camera.main != null)
            {
                eye = Camera.main.transform;
            }

            if (eye != null)
            {
                break;
            }

            yield return null;
        }

        if (eye == null)
        {
            Debug.LogWarning("EchoIntensityHudView: no eye transform found.");
            enabled = false;
            yield break;
        }

        BuildLineHud(eye);
        isBuilt = true;
        displayAlpha = 0f;
        ApplyAlpha(0f);
    }

    void BuildLineHud(Transform eye)
    {
        GameObject rootObject = new GameObject("IntensityHUD");
        rootObject.transform.SetParent(eye, false);
        rootObject.transform.localPosition = localOffset;
        rootObject.transform.localRotation = Quaternion.identity;
        rootObject.transform.localScale = Vector3.one;
        hudRoot = rootObject.transform;

        backgroundLine = CreateLineRenderer("Background", backgroundColor, 2);
        backgroundLine.SetPosition(0, new Vector3(-barWidth * 0.5f, 0f, 0f));
        backgroundLine.SetPosition(1, new Vector3(barWidth * 0.5f, 0f, 0f));

        fillLine = CreateLineRenderer("Fill", fillColorHigh, 2);
        UpdateFillLine(1f, 0f);
    }

    LineRenderer CreateLineRenderer(string name, Color color, int positions)
    {
        GameObject lineObject = new GameObject(name);
        lineObject.transform.SetParent(hudRoot, false);
        lineObject.transform.localPosition = Vector3.zero;
        lineObject.transform.localRotation = Quaternion.identity;

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.positionCount = positions;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.numCapVertices = 4;
        line.numCornerVertices = 4;
        line.material = GetHudMaterial();
        line.startColor = color;
        line.endColor = color;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.allowOcclusionWhenDynamic = false;
        line.enabled = false;

        return line;
    }

    void LateUpdate()
    {
        if (!isBuilt || controller == null)
        {
            return;
        }

        float elapsed = Time.time - lastAdjustTime;
        float targetAlpha = 0f;

        if (elapsed < holdAfterRelease)
        {
            targetAlpha = 1f;
        }
        else if (elapsed < holdAfterRelease + fadeOutDuration)
        {
            targetAlpha = 1f - (elapsed - holdAfterRelease) / fadeOutDuration;
        }

        displayAlpha = targetAlpha;
        UpdateFillLine(controller.originalBeamIntensity, displayAlpha);
    }

    void UpdateFillLine(float intensity, float alpha)
    {
        bool visible = alpha > 0.01f;

        if (backgroundLine != null)
        {
            backgroundLine.enabled = visible;
        }

        if (fillLine != null)
        {
            fillLine.enabled = visible;
        }

        if (!visible)
        {
            return;
        }

        float clamped = Mathf.Clamp01(intensity);
        float fillWidth = Mathf.Max(barWidth * 0.04f, barWidth * clamped);
        float half = fillWidth * 0.5f;

        fillLine.SetPosition(0, new Vector3(-half, 0f, 0f));
        fillLine.SetPosition(1, new Vector3(half, 0f, 0f));

        Color fillColor = Color.Lerp(fillColorLow, fillColorHigh, clamped);
        fillColor.a *= alpha;
        fillLine.startColor = fillColor;
        fillLine.endColor = fillColor;

        Color bgColor = backgroundColor;
        bgColor.a *= alpha;
        backgroundLine.startColor = bgColor;
        backgroundLine.endColor = bgColor;
    }

    void ApplyAlpha(float alpha)
    {
        if (controller == null)
        {
            return;
        }

        UpdateFillLine(controller.originalBeamIntensity, alpha);
    }

    static Material GetHudMaterial()
    {
        if (hudMaterial != null)
        {
            return hudMaterial;
        }

        Shader shader = Shader.Find("Sprites/Default");

        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        hudMaterial = new Material(shader);
        return hudMaterial;
    }
}
