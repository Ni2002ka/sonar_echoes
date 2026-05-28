using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class MicrophoneReplay : MonoBehaviour
{
    AudioSource audioSource;
    string device;

    // Samples to record before playback (no manual timeSamples — avoids ring-buffer crackle).
    const int MinBufferSamples = 2048;

    IEnumerator Start()
    {
        Debug.Log("=== MIC TEST START ===");

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.Stop();

#if UNITY_ANDROID && !UNITY_EDITOR
        yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
        yield return new WaitForSeconds(1f);

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

        audioSource.clip = Microphone.Start(device, true, 1, AudioSettings.outputSampleRate);

        float timeout = 5f;
        float timer = 0f;

        int minSamples = Mathf.Max(MinBufferSamples, (int)(AudioSettings.outputSampleRate * 0.05f));

        while (Microphone.GetPosition(device) < minSamples)
        {
            timer += Time.deltaTime;

            if (timer > timeout)
            {
                Debug.LogError("MIC FAILED TO START");
                yield break;
            }

            yield return null;
        }

        audioSource.Play();

        Debug.Log("Microphone initialized and playing.");
    }
}
