# QuestPro4Neos

A [NeosModLoader](https://github.com/zkxs/NeosModLoader) mod that brings the Quest Pro's [eye tracking](https://developer.oculus.com/documentation/unity/move-eye-tracking/) and [natural expressions](https://developer.oculus.com/documentation/unity/move-face-tracking/) to [Neos VR](https://neos.com/) avatars.

Related issues on the Neos Github:
1. https://github.com/Neos-Metaverse/NeosPublic/issues/1140
1. https://github.com/Neos-Metaverse/NeosPublic/issues/3770

## Usage
1. Install [NeosModLoader](https://github.com/zkxs/NeosModLoader).
1. Download the [latest release](https://github.com/dfgHiatus/QuestPro4Neos/releases/latest) of this mod and place it in your mods folder. This folder should be at `C:\Program Files (x86)\Steam\steamapps\common\NeosVR\nml_mods` for a default install. You can create it if it's missing, or if you launch the game once with NeosModLoader.
1. Follow the instructions to install [ALXR](https://docs.google.com/document/d/1MFGxIrdh7U2tq368X_UthryceIsapwz6C7hydmnaWQM/edit) and run it.
1. Start the game.

If you want to verify that the mod is working you can check your Neos logs, or create an EmptyObject with an AvatarRawEyeData/AvatarRawMouthData Component (Found under Users -> Common Avatar System -> Face -> AvatarRawEyeData/AvatarRawMouthData).

A big thanks to [Geenz](https://github.com/Geenz) for their contributions and testing this mod, not owning the headset myself this would not be possible without them. Check out their fork [here](https://github.com/Geenz/QuestPro4Neos)
