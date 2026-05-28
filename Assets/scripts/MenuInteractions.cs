using Meta.XR.ImmersiveDebugger.UserInterface.Generic;
using Oculus.Interaction.Samples;
using System;
using TMPro;
using Unity.Mathematics.Geometry;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuInteractions : MonoBehaviour
{
    public AudioReverbFilter playerReverb;
    public CustomAcoustics customAcoustics;
    private AudioReverbPreset audioReverbPreset = AudioReverbPreset.Auditorium;
    public void LoadDemo()
    {
        SceneManager.LoadScene("CustomAudioDemo");
    }

    public void LoadSample()
    {
        SceneManager.LoadScene("SampleScene");
    }

    public void LoadMine()
    {
        SceneManager.LoadScene("Mine");
    }

    public void ReverbOff()
    {
        playerReverb.reverbPreset = AudioReverbPreset.Off;
    }

    public void SetAuditoriumReverb()
    {
        playerReverb.reverbPreset = AudioReverbPreset.Auditorium;
    }

    public void SetStoneCorridorReverb()
    {
        playerReverb.reverbPreset = AudioReverbPreset.StoneCorridor;

    }

    public void SetSmallRoomReverb()
    {
        playerReverb.reverbPreset = AudioReverbPreset.Room;
    }

    public void SetNextReverb()
    {
        int preset = (int)playerReverb.reverbPreset;
        preset = (preset + 1) % 27;
        playerReverb.reverbPreset = (AudioReverbPreset)preset;
        Debug.Log($"Changing reverb to {playerReverb.reverbPreset}");
    }

    public void SetPreviousReverb()
    {
        int preset = (int)playerReverb.reverbPreset;
        preset = (preset - 1) % 27;
        if (preset < 0) preset = -preset;
        playerReverb.reverbPreset = (AudioReverbPreset)preset;
        Debug.Log($"Changing reverb to {playerReverb.reverbPreset}");
    }

    public void EarlyEchoOn()
    {
        customAcoustics.masterReflectionVolume = 0.8f;
    }

    public void EarlyEchoOff()
    {
        customAcoustics.masterReflectionVolume = 0f;
    }

    public void ChangeSpeedOfSound(float multiplier)
    {
        customAcoustics.speedOfSound = 343 * multiplier; ;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
