using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class MicrophoneReplay : MonoBehaviour
{
    AudioSource audioSource;
    string device;

    IEnumerator Start()
    {
        Debug.Log("=== MIC TEST START ===");

        audioSource = GetComponent<AudioSource>();

#if UNITY_ANDROID && !UNITY_EDITOR
        yield return Application.RequestUserAuthorization(
            UserAuthorization.Microphone
        );

        yield return new WaitForSeconds(1f);

        if (!Application.HasUserAuthorization(
            UserAuthorization.Microphone))
        {
            Debug.LogError("MIC PERMISSION DENIED");
            yield break;
        }
#endif

        Debug.Log("Mic device count: " + Microphone.devices.Length);

        foreach (var d in Microphone.devices)
        {
            Debug.Log("Mic device: " + d);
        }

        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("NO MICROPHONES FOUND");
            yield break;
        }

        device = Microphone.devices[0];

        Debug.Log("Using mic: " + device);

        audioSource.clip = Microphone.Start(
            device,
            true,
            1,
            AudioSettings.outputSampleRate
        );

        float timeout = 5f;
        float timer = 0f;

        while (Microphone.GetPosition(device) <= 0)
        {
            timer += Time.deltaTime;

            if (timer > timeout)
            {
                Debug.LogError("MIC FAILED TO START");
                yield break;
            }

            yield return null;
        }

        Debug.Log("Microphone initialized.");

        audioSource.loop = true;
        audioSource.Play();

        Debug.Log("Playback started.");
    }
}