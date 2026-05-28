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
1. Drag the ButtonMenu prefab under your CenterEyeAnchor.
2. Drag the UIInteractionManager prefab to the top level of your scene hierarchy.
3. Every EasyEditButton has a top-level script called ButtonQuickEdit.cs. Here you can change the button label. It will display this text at runtime. These buttons are children inside the Button Menu (a few levels down).
4. Also at the top level of a button, you should see a list under the "Button" component called "On Click ()". Here, add a reference to the UIInteractionManager. 
5. If you need new functionality, add it to MenuInteractions.cs as a public void. There are many other functions there that may be helpful examples.
6. Delete buttons you don't want. To add more, just drag the EasyEditButton prefab (in the prefabs folder) under the UIBackplate.
