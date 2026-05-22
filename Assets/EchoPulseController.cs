using UnityEngine;
using System.Collections;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class EchoPulseController : MonoBehaviour
{
    [Header("References")]
    public Transform head;
    public AudioClip pulseClip;
    public AudioClip echoClip;
    public EchoSegmentShaderScanner scanner;

    [Header("Echo")]
    public float maxDistance = 15f;
    public float speedOfSound = 343f;
    public float echoDelayMultiplier = 8f;

    [Header("Cone (20°)")]
    public LayerMask echoLayers = ~0;
    public int horizontalRays = 24;
    public int verticalRays = 12;
    public float coneAngle = 20f;
    [Range(0.01f, 1f)]
    public float coneSoftness = 0.35f;

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

        if (scanner == null)
        {
            scanner = GetComponent<EchoSegmentShaderScanner>();
        }

        if (scanner != null)
        {
            scanner.maxDistance = maxDistance;
            scanner.speedOfSound = speedOfSound;
            scanner.coneAngleDegrees = coneAngle;
            scanner.coneSoftness = coneSoftness;
        }
    }

    void Update()
    {
        if (GetOvrButtonDown(OVRInput.Button.One) || WasKeyPressed(KeyCode.Space))
        {
            FireScanner();
        }
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

        Key? key = KeyCodeToKey(keyCode);
        return key.HasValue && Keyboard.current[key.Value].wasPressedThisFrame;
#else
        return Input.GetKeyDown(keyCode);
#endif
    }

#if ENABLE_INPUT_SYSTEM
    static Key? KeyCodeToKey(KeyCode keyCode)
    {
        if (keyCode == KeyCode.Space)
        {
            return Key.Space;
        }

        return null;
    }
#endif

    void FireScanner()
    {
        if (head == null)
        {
            Debug.LogError("Head missing.");
            return;
        }

        if (pulseClip != null)
        {
            pulseSource.PlayOneShot(pulseClip, 1f);
        }

        RaycastHit[,] hits;
        bool[,] didHit;

        CastConeGrid(out hits, out didHit);

        float farthestHit = GetFarthestHitDistance(hits, didHit);

        if (scanner != null)
        {
            scanner.maxDistance = maxDistance;
            scanner.speedOfSound = speedOfSound;
            scanner.coneAngleDegrees = coneAngle;
            scanner.coneSoftness = coneSoftness;
            scanner.StartScan(head.position, head.forward, farthestHit);
        }

        PlayEchoesFromHits(hits, didHit);
    }

    void CastConeGrid(out RaycastHit[,] hits, out bool[,] didHit)
    {
        int rayH = Mathf.Max(1, horizontalRays);
        int rayV = Mathf.Max(1, verticalRays);

        hits = new RaycastHit[rayH, rayV];
        didHit = new bool[rayH, rayV];

        for (int x = 0; x < rayH; x++)
        {
            for (int y = 0; y < rayV; y++)
            {
                Vector3 direction = GetGridConeDirection(x, y, rayH, rayV);

                Ray ray = new Ray(head.position, direction);

                if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, echoLayers))
                {
                    hits[x, y] = hit;
                    didHit[x, y] = true;

                    Debug.DrawRay(head.position, direction * hit.distance, Color.cyan, 1f);
                }
                else
                {
                    didHit[x, y] = false;
                    Debug.DrawRay(head.position, direction * maxDistance, Color.red, 1f);
                }
            }
        }
    }

    Vector3 GetGridConeDirection(int x, int y, int rayH, int rayV)
    {
        float u = rayH <= 1 ? 0f : (x / (float)(rayH - 1)) * 2f - 1f;
        float v = rayV <= 1 ? 0f : (y / (float)(rayV - 1)) * 2f - 1f;

        float angleRad = coneAngle * Mathf.Deg2Rad;

        Vector3 localDirection = new Vector3(
            u * Mathf.Tan(angleRad),
            v * Mathf.Tan(angleRad),
            1f
        ).normalized;

        return head.TransformDirection(localDirection);
    }

    float GetFarthestHitDistance(RaycastHit[,] hits, bool[,] didHit)
    {
        float farthest = 0f;
        int rayH = hits.GetLength(0);
        int rayV = hits.GetLength(1);

        for (int x = 0; x < rayH; x++)
        {
            for (int y = 0; y < rayV; y++)
            {
                if (!didHit[x, y])
                {
                    continue;
                }

                if (hits[x, y].distance > farthest)
                {
                    farthest = hits[x, y].distance;
                }
            }
        }

        return farthest;
    }

    void PlayEchoesFromHits(RaycastHit[,] hits, bool[,] didHit)
    {
        int rayH = hits.GetLength(0);
        int rayV = hits.GetLength(1);

        for (int x = 0; x < rayH; x += 4)
        {
            for (int y = 0; y < rayV; y += 3)
            {
                if (!didHit[x, y])
                {
                    continue;
                }

                RaycastHit hit = hits[x, y];
                float delay = CalculateEchoDelay(hit.distance);
                StartCoroutine(PlayEchoAfterDelay(hit.point, hit.distance, delay));
            }
        }
    }

    float CalculateEchoDelay(float distance)
    {
        return (2f * distance / speedOfSound) * echoDelayMultiplier;
    }

    IEnumerator PlayEchoAfterDelay(Vector3 position, float distance, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (echoClip == null)
        {
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
}
