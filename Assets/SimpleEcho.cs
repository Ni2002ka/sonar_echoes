using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SimpleEcho : MonoBehaviour
{
    [Header("References")]
    public Transform head;
    public AudioClip pulseClip;
    public AudioClip echoClip;

    [Header("Echo")]
    public float maxDistance = 20f;
    public float echoDelayMultiplier = 8f;

    [Header("Cone Visual")]
    public bool showCone = true;
    public float coneAngle = 3.5f;
    public float coneDuration = 1.5f;
    public Material coneMaterial;

    private AudioSource pulseSource;

    void Start()
    {
        pulseSource = GetComponent<AudioSource>();
        if (pulseSource == null)
        {
            pulseSource = gameObject.AddComponent<AudioSource>();
        }

        pulseSource.playOnAwake = false;
        pulseSource.spatialBlend = 0f;

        if (head == null)
        {
            GameObject centerEye = GameObject.Find("CenterEyeAnchor");
            if (centerEye != null)
            {
                head = centerEye.transform;
            }
        }
    }

    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.One) || Input.GetKeyDown(KeyCode.Space))
        {
            EmitEchoPulse();
        }
    }

    void EmitEchoPulse()
    {
        Debug.Log("A pressed / echo pulse fired");

        if (pulseClip != null)
        {
            pulseSource.PlayOneShot(pulseClip, 1f);
        }
        else
        {
            Debug.LogWarning("Pulse Clip is missing.");
        }

        if (head == null)
        {
            Debug.LogError("Head is missing. Assign CenterEyeAnchor to the Head field.");
            return;
        }


        Ray ray = new Ray(head.position, head.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
        {
            float distance = hit.distance;
            float delay = (2f * distance / 343f) * echoDelayMultiplier;
            StartCoroutine(PlayEchoAfterDelay(hit.point, distance, delay));
            StartCoroutine(ShowImpactRipple(hit.point, hit.normal, delay));
        }
        else
        {
            Debug.Log("No echo hit.");
        }
    }

    IEnumerator PlayEchoAfterDelay(Vector3 position, float distance, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (echoClip == null)
        {
            Debug.LogWarning("Echo Clip is missing.");
            yield break;
        }

        GameObject echoObject = new GameObject("Echo Sound");
        echoObject.transform.position = position;

        AudioSource echoSource = echoObject.AddComponent<AudioSource>();
        echoSource.clip = echoClip;
        echoSource.spatialBlend = 1f;
        echoSource.rolloffMode = AudioRolloffMode.Logarithmic;
        echoSource.minDistance = 0.2f;
        echoSource.maxDistance = maxDistance;
        echoSource.volume = Mathf.Clamp01(1.5f / Mathf.Max(distance, 0.5f));
        echoSource.pitch = Mathf.Lerp(1.4f, 0.7f, distance / maxDistance);

        echoSource.Play();

        Destroy(echoObject, echoClip.length + 1f);
    }

    IEnumerator ShowImpactRipple(Vector3 position, Vector3 normal, float delay)
    {
        yield return new WaitForSeconds(delay);

        int segments = 40;
        float duration = 0.6f;
        float maxRadius = 0.5f;
        float elapsed = 0f;

        // Build two axes perpendicular to the surface normal
        Vector3 axis1 = Vector3.Cross(normal, Vector3.up).normalized;
        if (axis1.magnitude < 0.01f) axis1 = Vector3.Cross(normal, Vector3.right).normalized;
        Vector3 axis2 = Vector3.Cross(normal, axis1).normalized;

        GameObject ringObj = new GameObject("Impact Ripple");
        LineRenderer ring = ringObj.AddComponent<LineRenderer>();
        ring.positionCount = segments + 1;
        ring.useWorldSpace = true;
        ring.startWidth = 0.008f;
        ring.endWidth = 0.008f;
        ring.material = new Material(Shader.Find("Sprites/Default"));

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float radius = Mathf.Lerp(0f, maxRadius, t);
            float alpha = Mathf.Lerp(0.9f, 0f, t);
            Color c = new Color(0f, 1f, 1f, alpha);
            ring.startColor = c;
            ring.endColor = c;

            for (int i = 0; i <= segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                ring.SetPosition(i, position
                    + axis1 * Mathf.Cos(angle) * radius
                    + axis2 * Mathf.Sin(angle) * radius);
            }

            yield return null;
        }

        Destroy(ringObj);
    }

}
