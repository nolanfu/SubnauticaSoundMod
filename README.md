# Subnautica Sound Mod

An mod for changing sounds in the game <a href="https://unknownworlds.com/subnautica/">Subnautica</a>. 

## Building and Installation
 
* Build
* Put SubnauticaSoundMod.dll and Assembly-CSharp.Mod.mm.dll into a bin/ folder where Install.exe lives
* Put a built copy of MonoMod along with its required dlls in bin/
* Put your sound banks files and SoundModGUIDs.txt into a banks/ folder where Install.exe lives (see Sound Bank Guidelines below)
* Run Install.exe

## Sound Bank Guidelines

For your sound banks to actually replace FMOD events in game, you will have to take care in how you structure things in FMOD Studio.

* Your FMOD Studio version needs to match the version used to generate the native sound banks.
  * At the time this line was last updated, the version was 1.08.14.
* All of the events in your FMOD Studio project need to live in a sound_mod/ event folder. Events outside of sound_mod/ will be ignored.
* Inside sound_mod/, your event path should match (case-insensitive) the native event you want to replace.
  * For example, if you wanted to replace the /player/jump event, you would create a /sound_mod/player/jump event.
* All of your events must be stored in banks other than the master bank.
  * There can only be one master bank file, and the game already has one that we don't want to overwrite. 
* If you want your events to output to specific native buses, create buses in your FMOD Studio project with names that exactly match the native ones you want to use. The settings on these buses don't really matter, since we'll be using the native buses at runtime, but you are welcome to configure them for in-studio testing.
  * If SoundMod is properly installed, it will generate a NativeGUIDs.txt file in Subnautica_Data/StreamingAssets/ listing the names and GUIDs of native buses. You can use thise as a reference for creating your mirrored buses.
* Once you're satisfied with your project, build it, and then generate a GUIDs.txt file from within FMOD Studio. Rename GUIDs.txt to SoundModGUIDs.txt.
* Put your sound banks (except for Master Bank / Master Bank.strings) and SoundModGUIDs.txt file in Subnautica_Data/StreamingAssets/.

Note: When SoundMod initializes, it will rewrite any sound banks you provide with updated bus GUIDs. Keep copies (or just rebuild them) if you were planning on using them elsewhere.

