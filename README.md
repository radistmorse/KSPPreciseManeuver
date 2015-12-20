KSPPreciseNode
===============

PreciseNode plugin for Kerbal Space Program

Provides a window for more precise maneuver node editing.

NOTES:
--------------
Works with KSP 1.0.5

BUILD:
--------------
.NET v3.5 is required.  
Both Visual Studio on windows and Monodevelop on linux are able to build the project. Just open the .csproj file.  
Three files must be present in the project root directory:
```
UnityEngine.dll
Assembly-CSharp.dll
Assembly-CSharp-firstpass.dll
```
All three files can be taken from the KSP_Data/Managed directory of your KSP installation.

INSTALLATION:
--------------
Put the PreciseNode.dll library into GameData/PreciseNode folder inside your KSP installation.

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

The keybindings can be changed by pressing the "K" button in top right corner of the main window.
