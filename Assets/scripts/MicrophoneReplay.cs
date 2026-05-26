//using UnityEngine;
//using System.Collections;

//[RequireComponent(typeof(AudioSource))]
//public class MicrophoneReplay : MonoBehaviour
//{
//    AudioSource audioSource;
//    string device;

//    // The number of samples to buffer before playing. 
//    // Lower = less latency. Higher = less crackling/stuttering.
//    // 128 to 256 is usually the sweet spot for low latency.
//    int latencySamples = 128;

//    IEnumerator Start()
//    {
//        Debug.Log("=== MIC TEST START ===");

//        audioSource = GetComponent<AudioSource>();

//#if UNITY_ANDROID && !UNITY_EDITOR
//        yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
//        yield return new WaitForSeconds(1f);

//        if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
//        {
//            Debug.LogError("MIC PERMISSION DENIED");
//            yield break;
//        }
//#endif

//        if (Microphone.devices.Length == 0)
//        {
//            Debug.LogError("NO MICROPHONES FOUND");
//            yield break;
//        }

//        device = Microphone.devices[0];
//        Debug.Log("Using mic: " + device);

//        // Start recording
//        audioSource.clip = Microphone.Start(device, true, 1, AudioSettings.outputSampleRate);
//        audioSource.loop = true;

//        float timeout = 5f;
//        float timer = 0f;

//        // WAIT for the microphone to start recording AND capture a tiny buffer
//        // BEFORE we start playing it back.
//        while (Microphone.GetPosition(device) < latencySamples)
//        {
//            timer += Time.deltaTime;

//            if (timer > timeout)
//            {
//                Debug.LogError("MIC FAILED TO START");
//                yield break;
//            }

//            yield return null;
//        }

//        // NOW play the audio. The read head is now safely just behind the write head.
//        audioSource.Play();

//        Debug.Log("Microphone initialized and playing with low latency.");
//    }
//}
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class MicrophoneReplay : MonoBehaviour
{
    AudioSource audioSource;
    string device;

    // The number of samples to buffer *after* microphone starts recording
    // and *before* we attempt to play.
    // This helps prevent the AudioSource read head from overtaking the mic write head.
    // A value around 0.1 seconds (4800 samples for 48kHz) is a good starting point for robustness.
    int initialBufferSamples;

    // The desired playback latency in samples. Lower = less latency, Higher = less crackling.
    // This defines how far behind the live microphone input the playback will be.
    const int playbackLatencySamples = 256; // Increased slightly for stability, can be tuned.

    IEnumerator Start()
    {
        Debug.Log("=== MIC TEST START ===");

        audioSource = GetComponent<AudioSource>();

#if UNITY_ANDROID && !UNITY_EDITOR
        yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
        yield return new WaitForSeconds(1f); // Give some time for permission dialog to show/hide

        if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            Debug.LogError("MIC PERMISSION DENIED");
            yield break;
        }
#endif

        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("NO MICROPHONES FOUND");
            yield break;
        }

        device = Microphone.devices[0];
        Debug.Log("Using mic: " + device);

        // Calculate initial buffer based on output sample rate
        initialBufferSamples = (int)(AudioSettings.outputSampleRate * 0.1f); // e.g., 0.1 seconds of buffer

        // Start recording into a 1-second clip, looping.
        audioSource.clip = Microphone.Start(device, true, 1, AudioSettings.outputSampleRate);
        audioSource.loop = true;

        float timeout = 5f;
        float timer = 0f;

        // WAIT for the microphone to start recording AND fill a substantial initial buffer.
        // This is crucial to ensure the AudioSource has enough data to start without issues.
        while (Microphone.GetPosition(device) < initialBufferSamples)
        {
            timer += Time.deltaTime;

            if (timer > timeout)
            {
                Debug.LogError("MIC FAILED TO START OR BUFFER");
                yield break;
            }
            yield return null;
        }

        // Now, explicitly set the AudioSource's playback position.
        // We want the read head to be precisely 'playbackLatencySamples' behind the current write head.
        int currentMicPosition = Microphone.GetPosition(device);

        // Ensure the read head starts at a valid position within the clip, safely behind the write head.
        // Modulo operator handles clip wrap-around for setting timeSamples if currentMicPosition > clip.samples
        audioSource.timeSamples = (currentMicPosition - playbackLatencySamples + audioSource.clip.samples) % audioSource.clip.samples;

        // Ensure it doesn't try to play from a position before actual recording started (e.g., negative index)
        if (audioSource.timeSamples < 0)
        {
            audioSource.timeSamples = 0; // Fallback, though the modulo should handle this for circular buffers.
        }

        // NOW play the audio. The read head is now safely just behind the write head.
        audioSource.Play();
        Debug.Log($"Playback started from timeSamples: {audioSource.timeSamples} (Mic Pos: {currentMicPosition}).");

        // Give the audio engine a few frames to stabilize after starting playback.
        // This is important before other scripts try to read its state.
        for (int i = 0; i < 5; i++)
        {
            yield return null;
        }

        Debug.Log("Microphone initialized and playing with low latency.");
    }
}