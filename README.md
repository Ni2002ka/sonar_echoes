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

Tutorials for setting up Interaction:
* https://developers.meta.com/horizon/documentation/unity/unity-isdk-setup/
* https://developers.meta.com/horizon/documentation/unity/unity-isdk-getting-started/
* I needed to add the raycast interactable script to the default UI Backplate prefab.

# Making a UI Menu
1. Copy everything under CenterEyeAnchor in CustomAudioDemo scene into your scene, also under CenterEyeAnchor.
2. Every EasyEditButton has a top-level script called ButtonQuickEdit.cs. Here you can change the button label. It will display this text at runtime.
3. Also at the top level of a button, you should see a list under the "Button" component called "On Click ()". Here, add a reference to the UIInteractionManager (you will have to copy this object into your scene, too. Maybe just make a prefab of it, Micha). 
4. Any function you want to occur when the button is clicked should be listed in this menu. Use the '+' button at the bottom right of the menu to add an object who possesses a function you want to call. See the CustomAudioDemo scene for an example.
5. Delete buttons you don't want. To add more, just drag the EasyEditButton prefab (in the prefabs folder) under the UIBackplate.
