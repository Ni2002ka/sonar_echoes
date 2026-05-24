using UnityEngine;
using System.Collections;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
#if UNITY_EDITOR
using UnityEngine.XR;
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

    [Header("Original Beam")]
    [Range(0f, 1f)]
    public float originalBeamIntensity = 1f;
    public float intensityAdjustSpeed = 1.5f;
    public float keyboardStepAmount = 0.1f;
    public float scrollStepAmount = 0.08f;

    [Header("Intensity HUD")]
    public bool showIntensityHud = true;

    private AudioSource pulseSource;
    EchoIntensityHudView intensityHud;

    void Start()
    {
        pulseSource = GetComponent<AudioSource>();

        if (pulseSource == null)
        {
            pulseSource = gameObject.AddComponent<AudioSource>();
        }

        ResolveHead();
        ConfigurePulseAudioSource();
        EnsureAudioListenerOnHead();

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

#if ENABLE_INPUT_SYSTEM
        EnsureKeyboardDevice();
#endif

        if (showIntensityHud)
        {
            EnsureIntensityHud();
        }
    }

    void EnsureIntensityHud()
    {
        EchoIntensityHudView[] huds = GetComponents<EchoIntensityHudView>();

        if (huds.Length == 0)
        {
            intensityHud = gameObject.AddComponent<EchoIntensityHudView>();
            intensityHud.Initialize(this);
            return;
        }

        intensityHud = huds[0];
        intensityHud.Initialize(this);
    }

    void Update()
    {
        if (GetOvrButtonDown(OVRInput.Button.One) || WasFirePressed())
        {
            FireScanner();
        }

        ApplyOriginalBeamIntensityAdjustment();
    }

    void ApplyOriginalBeamIntensityAdjustment()
    {
        bool adjusting = false;

        float step = GetKeyboardIntensityStep() + GetVrIntensityStep();
        if (Mathf.Abs(step) > 0f)
        {
            originalBeamIntensity = Mathf.Clamp01(originalBeamIntensity + step);
            adjusting = true;
        }

        float delta = GetVrIntensityDelta();
        delta += GetKeyboardIntensityDelta();
        delta += GetScrollIntensityDelta();

        if (Mathf.Abs(delta) > 0f)
        {
            originalBeamIntensity = Mathf.Clamp01(
                originalBeamIntensity + delta * intensityAdjustSpeed * Time.deltaTime
            );
            adjusting = true;
        }

        if (showIntensityHud && intensityHud != null)
        {
            intensityHud.NotifyAdjusting(adjusting);
        }
    }

    float GetVrIntensityStep()
    {
        float step = 0f;

        if (GetOvrButtonDown(OVRInput.Button.Four))
        {
            step -= keyboardStepAmount;
        }

        if (GetOvrButtonDown(OVRInput.Button.Two))
        {
            step += keyboardStepAmount;
        }

        return step;
    }

    static float GetVrIntensityDelta()
    {
        float delta = 0f;

        if (GetOvrButton(OVRInput.Button.Four))
        {
            delta -= 1f;
        }

        if (GetOvrButton(OVRInput.Button.Two))
        {
            delta += 1f;
        }

        return delta;
    }

    static bool GetOvrButton(OVRInput.Button button)
    {
        try
        {
            return OVRInput.Get(button);
        }
        catch
        {
            return false;
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

    public void ResolveHead()
    {
        if (head != null)
        {
            return;
        }

        GameObject centerEye = GameObject.Find("CenterEyeAnchor");

        if (centerEye != null)
        {
            head = centerEye.transform;
            return;
        }

        Camera mainCamera = Camera.main;

        if (mainCamera != null)
        {
            head = mainCamera.transform;
        }
    }

    void ConfigurePulseAudioSource()
    {
        if (pulseSource == null)
        {
            return;
        }

        pulseSource.playOnAwake = false;
        pulseSource.spatialBlend = 0f;
        pulseSource.spatialize = false;
        pulseSource.spatializePostEffects = false;
        pulseSource.mute = false;
        pulseSource.volume = 1f;
        pulseSource.outputAudioMixerGroup = null;
        DisableMetaXrAudioComponents();
    }

    void EnsureAudioListenerOnHead()
    {
        ResolveHead();

        if (head == null)
        {
            return;
        }

        AudioListener listener = head.GetComponent<AudioListener>();

        if (listener == null)
        {
            listener = head.gameObject.AddComponent<AudioListener>();
        }

        listener.enabled = true;
    }

    void DisableMetaXrAudioComponents()
    {
        MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();

        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];

            if (behaviour == null || behaviour == this)
            {
                continue;
            }

            string typeName = behaviour.GetType().Name;

            if (typeName == "MetaXRAudioSource" || typeName == "OculusAudioSource")
            {
                behaviour.enabled = false;
            }
        }
    }

    static bool UseEditorFriendlyEcho()
    {
#if UNITY_EDITOR
        return !XRSettings.isDeviceActive;
#else
        return false;
#endif
    }

#if ENABLE_INPUT_SYSTEM
    static void EnsureKeyboardDevice()
    {
        if (Keyboard.current == null)
        {
            InputSystem.AddDevice<Keyboard>();
        }
    }
#endif

    static bool WasFirePressed()
    {
#if ENABLE_INPUT_SYSTEM
        EnsureKeyboardDevice();

        if (Keyboard.current != null)
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame
                || Keyboard.current.enterKey.wasPressedThisFrame
                || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
            {
                return true;
            }
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            return true;
        }

        return false;
#else
        return Input.GetKeyDown(KeyCode.Space)
            || Input.GetKeyDown(KeyCode.Return)
            || Input.GetMouseButtonDown(0);
#endif
    }

    float GetKeyboardIntensityStep()
    {
#if ENABLE_INPUT_SYSTEM
        EnsureKeyboardDevice();

        if (Keyboard.current == null)
        {
            return 0f;
        }

        if (IsKeyPressedThisFrame(Key.Minus, Key.NumpadMinus, Key.LeftBracket, Key.Comma))
        {
            return -keyboardStepAmount;
        }

        if (IsKeyPressedThisFrame(Key.Equals, Key.NumpadPlus, Key.RightBracket, Key.Period))
        {
            return keyboardStepAmount;
        }

        if (IsKeyPressedThisFrame(Key.Y))
        {
            return -keyboardStepAmount;
        }

        if (IsKeyPressedThisFrame(Key.B))
        {
            return keyboardStepAmount;
        }

        return 0f;
#else
        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.LeftBracket) || Input.GetKeyDown(KeyCode.Comma))
        {
            return -keyboardStepAmount;
        }

        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.RightBracket) || Input.GetKeyDown(KeyCode.Period))
        {
            return keyboardStepAmount;
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            return -keyboardStepAmount;
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            return keyboardStepAmount;
        }

        return 0f;
#endif
    }

    static float GetKeyboardIntensityDelta()
    {
#if ENABLE_INPUT_SYSTEM
        EnsureKeyboardDevice();

        if (Keyboard.current == null)
        {
            return 0f;
        }

        float delta = 0f;

        if (IsKeyHeld(Key.Minus, Key.NumpadMinus, Key.LeftBracket, Key.Comma))
        {
            delta -= 1f;
        }

        if (IsKeyHeld(Key.Equals, Key.NumpadPlus, Key.RightBracket, Key.Period))
        {
            delta += 1f;
        }

        if (IsKeyHeld(Key.Y))
        {
            delta -= 1f;
        }

        if (IsKeyHeld(Key.B))
        {
            delta += 1f;
        }

        return delta;
#else
        float delta = 0f;

        if (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.LeftBracket) || Input.GetKey(KeyCode.Comma))
        {
            delta -= 1f;
        }

        if (Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.RightBracket) || Input.GetKey(KeyCode.Period))
        {
            delta += 1f;
        }

        if (Input.GetKey(KeyCode.Y))
        {
            delta -= 1f;
        }

        if (Input.GetKey(KeyCode.B))
        {
            delta += 1f;
        }

        return delta;
#endif
    }

    float GetScrollIntensityDelta()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null)
        {
            return 0f;
        }

        float scrollY = Mouse.current.scroll.ReadValue().y;

        if (Mathf.Abs(scrollY) < 0.01f)
        {
            return 0f;
        }

        return Mathf.Sign(scrollY) * scrollStepAmount;
#else
        float scroll = Input.mouseScrollDelta.y;

        if (Mathf.Abs(scroll) < 0.01f)
        {
            return 0f;
        }

        return Mathf.Sign(scroll) * scrollStepAmount;
#endif
    }

#if ENABLE_INPUT_SYSTEM
    static bool IsKeyHeld(params Key[] keys)
    {
        if (Keyboard.current == null)
        {
            return false;
        }

        for (int i = 0; i < keys.Length; i++)
        {
            if (Keyboard.current[keys[i]].isPressed)
            {
                return true;
            }
        }

        return false;
    }

    static bool IsKeyPressedThisFrame(params Key[] keys)
    {
        if (Keyboard.current == null)
        {
            return false;
        }

        for (int i = 0; i < keys.Length; i++)
        {
            if (Keyboard.current[keys[i]].wasPressedThisFrame)
            {
                return true;
            }
        }

        return false;
    }
#endif

    void FireScanner()
    {
        ResolveHead();

        if (head == null)
        {
            Debug.LogError("EchoPulseController: Head missing. Add OVRCameraRig or assign Head on EchoSystem.");
            return;
        }

        ConfigurePulseAudioSource();

        if (pulseClip != null && originalBeamIntensity > 0f)
        {
            pulseSource.PlayOneShot(pulseClip, originalBeamIntensity);
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
        echoSource.mute = false;
        echoSource.spatialize = false;
        echoSource.spatializePostEffects = false;
        echoSource.outputAudioMixerGroup = null;
        echoSource.rolloffMode = AudioRolloffMode.Logarithmic;
        echoSource.minDistance = 0.2f;
        echoSource.maxDistance = maxDistance;
        echoSource.volume = Mathf.Clamp(1.5f / Mathf.Max(distance, 0.5f), 0.25f, 1f);
        echoSource.pitch = Mathf.Lerp(1.4f, 0.7f, distance / maxDistance);

        if (UseEditorFriendlyEcho())
        {
            echoSource.spatialBlend = 0f;
        }
        else
        {
            echoSource.spatialBlend = 1f;
        }

        echoSource.Play();

        Destroy(echoObject, echoClip.length + 1f);
    }
}
