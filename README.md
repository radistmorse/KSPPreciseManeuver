KSPPreciseManeuver
===============

PreciseManeuve plugin for Kerbal Space Program

Provides a window for more precise maneuver node editing.

Window can use the KSP look:  
![Screenshot of the Precise Maneuver main window](/../screenshots/PreciseManeuver-KSP-min.png?raw=true "KSP skin, minimum info") ![Screenshot of the Precise Maneuver main window](/../screenshots/PreciseManeuver-KSP-max.png?raw=true "KSP skin, maximum info")

Or it can use the default Unity look:  
![Screenshot of the Precise Maneuver main window](/../screenshots/PreciseManeuver-Unity-min.png?raw=true "Unity skin, minimum info") ![Screenshot of the Precise Maneuver main window](/../screenshots/PreciseManeuver-Unity-max.png?raw=true "Unity skin, maximum info")

NOTES:
--------------
Works with KSP 1.0.5  
Development version 1.1

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

INSTALLATION:
--------------
Put the PreciseManeuver.dll and PreciseManeuver.UI.dll library into GameData/PreciseManeuver folder inside your KSP installation.  
Also, you will need the AssetBundle file with unity prefabs, which can be generated in Unity Editor from prefabs in /Prefabs folder.

USAGE:
--------------
The following hotkeys are available:
- Keypad8/5: increase prograde/retrograde
- Keypad4/6: increase/decreas radial
- Keypad7/9: increase/decrease normal
- Keypad1/3: increase/decrease time
- Keypad2: switch different modes for orbit (+alt for reverse)
- Keypad0: raise the increment step (+alt for reverse)
- "P": hide/show the window (other hotkeys will stop working)
