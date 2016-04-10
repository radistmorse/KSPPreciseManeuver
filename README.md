KSPPreciseManeuver
===============

PreciseManeuve plugin for Kerbal Space Program

Provides a window for more precise maneuver node editing.

![Screenshot of the Precise Maneuver main window](/../screenshots/PreciseManeuver1.png?raw=true "Precise Maneuver Window")

The modular structure allows you to disable all the unneded components and make a window as small as you want.

NOTES:
--------------
Works with KSP 1.1 pre-release

BUILD:
--------------
.NET v3.5 is required.  
Both Visual Studio on windows and Monodevelop on linux are able to build the project. Just open the .csproj file.  
The libraries are expected to be found in /Libs folder:
```
UnityEngine.dll
UnityEngine.UI.dll
Assembly-CSharp.dll
Assembly-CSharp-firstpass.dll
KSPUtil.dll
```
All five files can be taken from the KSP_Data/Managed directory of your KSP installation.  
Also, you will need the AssetBundle file with unity prefabs, which can be generated in Unity Editor from prefabs in /Prefabs folder.

INSTALLATION:
--------------
Unpack the plugin into GameData folder inside your KSP installation.  


USAGE:
--------------
The toolbar icon will appear during the mapview, and by pressing on it you can enable and disable various components of the plugin.

The following hotkeys are available:
- Keypad8/5: increase prograde/retrograde
- Keypad4/6: increase/decreas radial
- Keypad7/9: increase/decrease normal
- Keypad1/3: increase/decrease time
- Keypad2: switch different modes for trajectories (+alt for reverse)
- Keypad0: raise the increment step (+alt for reverse)
- "P": hide/show the window (all the hotkeys excluding the trajectories controls will stop working)
