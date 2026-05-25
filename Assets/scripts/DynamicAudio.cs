using UnityEngine;
using Meta.XR.Audio;
public class DynamicAudio : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Enable the global room modeling system
        MetaXRAudioNativeInterface.Interface.SetEnabled((int)EnableFlag.SIMPLE_ROOM_MODELING, true);

        // Enable late reverb (the "tail" of the sound that changes with room size)
        MetaXRAudioNativeInterface.Interface.SetEnabled((int)EnableFlag.LATE_REVERBERATION, true);

        var audio = MetaXRAudioNativeInterface.Interface;

        // 1. Set how many rays are cast (higher = more accurate, more CPU)
        audio.SetDynamicRoomRaysPerSecond(512);

        // 2. Set the Max Distance (how far rays travel before giving up)
        // Set this to slightly larger than your largest room.
        audio.SetDynamicRoomMaxWallDistance(50.0f);

        // 3. Set the Interpolation Speed (0.0 to 1.0)
        // 0.9 is a good default. Lower values make transitions smoother but slower.
        audio.SetDynamicRoomInterpSpeed(0.9f);

        // 4. Set the Cache Size
        // This stores previous ray hits to stabilize the room estimation.
        audio.SetDynamicRoomRaysRayCacheSize(512);
    }

    // Update is called once per frame
    void Update()
    {

        float[] dims = new float[3]; // Width, Height, Depth
        float[] coeffs = new float[24]; // 6 walls * 4 frequency bands
        Vector3 pos;

        MetaXRAudioNativeInterface.Interface.GetRoomDimensions(dims, coeffs, out pos);
        Debug.Log($"Dynamic Room Size: {dims[0]}x{dims[1]}x{dims[2]}");
    }
}
