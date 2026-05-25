using System.Collections;
using UnityEngine;

public class CustomAcoustics : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("The AudioSource managed by your MicrophoneReplay.cs script.")]
    public AudioSource playerVoiceSource;

    [Header("Acoustic Settings")]
    [Tooltip("Layer mask for your room geometry (exclude the player!)")]
    public LayerMask wallLayer;
    public float speedOfSound = 343f; // Meters per second
    public float maxDistance = 30f;   // Max distance to check for echoes
    public float updateRate = 0.1f;   // How often to cast rays (seconds)
    [Range(0f, 1f)]
    public float masterReflectionVolume = 0.8f;

    // Directions to cast rays (Local to the player)
    private readonly Vector3[] directions = {
        Vector3.forward, Vector3.back,
        Vector3.left, Vector3.right,
        Vector3.up, Vector3.down
    };

    // Helper class to manage each reflection point
    private class ReflectionNode
    {
        public GameObject gameObject;
        public AudioSource audioSource;
        public AudioEchoFilter delayFilter;
        public AudioLowPassFilter lowPassFilter;
    }

    private ReflectionNode[] reflectionNodes;

    void Start()
    {
        if (playerVoiceSource == null)
        {
            Debug.LogError("CustomAcoustics: Player Voice Source is not assigned! Please drag the AudioSource managed by MicrophoneReplay.cs into the inspector.");
            enabled = false;
            return;
        }

        InitializeReflectionNodes();

        // Start the coroutine that links to your existing Microphone setup
        StartCoroutine(LinkToPlayerVoice());
    }

    private void InitializeReflectionNodes()
    {
        reflectionNodes = new ReflectionNode[directions.Length];

        for (int i = 0; i < directions.Length; i++)
        {
            GameObject obj = new GameObject($"ReflectionNode_{i}");
            obj.transform.parent = null; // Keep them in world space

            // 1. Setup AudioSource (The spatialized emitter on the wall)
            AudioSource src = obj.AddComponent<AudioSource>();
            src.spatialBlend = 1.0f; // Fully 3D
            src.spatialize = true;
            src.loop = true;
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.minDistance = 1f;
            src.maxDistance = maxDistance;
            // NOTE: We do not assign the clip or call Play() here yet.

            // 2. Setup Delay (Audio Echo Filter used as a pure delay line)
            AudioEchoFilter echo = obj.AddComponent<AudioEchoFilter>();
            echo.decayRatio = 0f; // No repeats, just a single delayed slap
            echo.dryMix = 0f;     // Mute the immediate sound
            echo.wetMix = 1f;     // Only play the delayed sound

            // 3. Setup Material Absorption (Low Pass Filter)
            AudioLowPassFilter lpf = obj.AddComponent<AudioLowPassFilter>();

            reflectionNodes[i] = new ReflectionNode
            {
                gameObject = obj,
                audioSource = src,
                delayFilter = echo,
                lowPassFilter = lpf
            };
        }
    }

    private IEnumerator LinkToPlayerVoice()
    {
        // Wait until your MicrophoneReplay.cs script has successfully initialized the mic
        // and assigned the recording clip to the player's AudioSource.
        while (playerVoiceSource.clip == null)
        {
            yield return null;
        }

        // Once the clip exists, assign it to all of our wall reflection nodes
        foreach (ReflectionNode node in reflectionNodes)
        {
            node.audioSource.clip = playerVoiceSource.clip;

            // Sync the playback position to match the player's main AudioSource exactly
            node.audioSource.timeSamples = playerVoiceSource.timeSamples;
            node.audioSource.Play();
        }

        // Now that audio is flowing, begin tracking the walls and updating the delays
        StartCoroutine(UpdateAcousticsLoop());
    }

    private IEnumerator UpdateAcousticsLoop()
    {
        while (true)
        {
            float totalDistance = 0f;
            int hitCount = 0;

            for (int i = 0; i < directions.Length; i++)
            {
                // Convert local directions (e.g. forward) to world directions based on player's head rotation
                Vector3 worldDir = transform.TransformDirection(directions[i]);
                ReflectionNode node = reflectionNodes[i];

                if (Physics.Raycast(transform.position, worldDir, out RaycastHit hit, maxDistance, wallLayer))
                {
                    // Debug visualization (Turn on Gizmos in Game view to see the rays)
                    Debug.DrawLine(transform.position, hit.point, Color.green, updateRate);

                    // Move the virtual audio source to the wall
                    node.gameObject.transform.position = hit.point;

                    // Calculate physics
                    float distanceToWall = hit.distance;
                    totalDistance += distanceToWall;
                    hitCount++;

                    // Sound travels from mouth -> wall -> ears. Total distance = distance * 2.
                    float totalTravelDistance = distanceToWall * 2f;

                    // Delay in milliseconds = (Distance / Speed) * 1000
                    float delayMs = (totalTravelDistance / speedOfSound) * 1000f;
                    node.delayFilter.delay = Mathf.Clamp(delayMs, 10f, 5000f);

                    // Attenuation (Inverse square law approximation for volume)
                    float attenuation = 1f / (1f + totalTravelDistance);
                    node.audioSource.volume = attenuation * masterReflectionVolume;

                    // Signal Processing (Material Absorption)
                    node.lowPassFilter.cutoffFrequency = GetMaterialCutoff(hit.collider.tag);
                }
                else
                {
                    // Ray hit nothing (e.g., looking at the sky or open door)
                    Debug.DrawRay(transform.position, worldDir * maxDistance, Color.red, updateRate);
                    node.audioSource.volume = 0f; // Mute this reflection
                    totalDistance += maxDistance;
                }
            }

            // Update the Late Reverb based on room size
            //UpdateLateReverb(totalDistance / directions.Length);

            yield return new WaitForSeconds(updateRate);
        }
    }

    private float GetMaterialCutoff(string materialTag)
    {
        // Adjust these tags to match your project
        switch (materialTag)
        {
            case "Carpet":
            case "Fabric":
                return 1500f;  // Highly muffled echo
            case "Wood":
                return 6000f;  // Warm echo
            case "Concrete":
            case "Glass":
            case "Tile":
                return 22000f; // Very bright, harsh echo (No filtering)
            default:
                return 10000f; // Standard drywall
        }
    }

    //private void UpdateLateReverb(float averageRoomDistance)
    //{
    //    if (averageRoomDistance < 3f)
    //        reverbZone.reverbPreset = AudioReverbPreset.Bathroom;
    //    else if (averageRoomDistance < 8f)
    //        reverbZone.reverbPreset = AudioReverbPreset.Room;
    //    else if (averageRoomDistance < 15f)
    //        reverbZone.reverbPreset = AudioReverbPreset.Auditorium;
    //    else if (averageRoomDistance < 30f)
    //        reverbZone.reverbPreset = AudioReverbPreset.Cave;
    //    else
    //        reverbZone.reverbPreset = AudioReverbPreset.Mountains; // Massive open space tail
    //}
}