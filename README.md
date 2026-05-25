# sonar_echoes

# Using CustomAcoustics.cs
Some helpful setup:
* Edit > Project Settings > Audio: Set DSP Buffer Size to Best Latency.

To use:
1. Apply CustomAcoustics.cs to a sound source that should produce echoes.
2. Set the Wall Layer to the Unity layer which you want to reflect sounds.
 - Everything that reflects sound should be on this layer.
3. Set Player Voice Source to the Audio Source producing microphone output. 
 - In future, this can just grab the audioSource on the current object with a GetComponent call.

This should be enough to generate "dry" early echoes. To flesh out the sound:
* Add an AudioReverbFilter to your player voice source.
* Consider positioning the player voice source behind the player head.
