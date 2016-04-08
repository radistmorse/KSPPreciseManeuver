/******************************************************************************
 * Copyright (c) 2013-2014, Justin Bengtson
 * Copyright (c) 2015, George Sedov
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice,
 * this list of conditions and the following disclaimer.
 *
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 * this list of conditions and the following disclaimer in the documentation
 * and/or other materials provided with the distribution.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 ******************************************************************************/

using System;
using System.Linq;
using UnityEngine;
using KSP.IO;
using System.Collections.Generic;

namespace KSPPreciseManeuver {
internal class PreciseManeuverConfig {
  #region Singleton

  private PreciseManeuverConfig () {
    loadConfig ();
  }
  internal static PreciseManeuverConfig _instance = null;
  internal static PreciseManeuverConfig Instance {
    get {
      if (_instance == null)
        _instance = new PreciseManeuverConfig ();
      return _instance;
    }
  }

  #endregion

  private AssetBundle _prefabs;
  internal AssetBundle prefabs {
    get {
      if (_prefabs == null) {
        var path = System.Reflection.Assembly.GetExecutingAssembly().Location;
        path = path.Replace (System.IO.Path.GetFileName (path), "prefabs");
        var www = new WWW("file://"+path);
        _prefabs = www.assetBundle;
      }
      return _prefabs;
    }
  }

  #region MainWindow

  private bool _showMainWindow = true;
  internal bool showMainWindow {
    get { return _showMainWindow; }
    set {
      if (_showMainWindow != value) {
        _showMainWindow = value;
        notifyShowChanged ();
      }
    }
  }
  private Vector3 _mainWindowPos = new Vector3 ();
  internal Vector3 mainWindowPos {
    get { return _mainWindowPos; }
    set { _mainWindowPos = value; }
  }

  #endregion

  #region KeymapperWindow

  private bool _showKeymapperWindow = false;
  internal bool showKeymapperWindow {
    get { return _showKeymapperWindow; }
    set {
      if (_showKeymapperWindow != value) {
        _showKeymapperWindow = value;
        notifyShowChanged ();
      }
    }
  }

  private Vector3 _keymapperWindowPos = new Vector3 ();
  internal Vector3 keymapperWindowPos {
    get { return _keymapperWindowPos; }
    set { _keymapperWindowPos = value; }
  }

  #endregion

  #region Increment

  private int _increment = 0;
  internal double increment { get { return Math.Pow (10, _increment); } }
  internal double incrementDeg { get { return Math.PI * Math.Pow (10, _increment) / 180; } }
  internal double incrementUt { get { return increment * (x10UTincrement ? 10 : 1); } }
  internal int incrementRaw {
    get { return _increment; }
    set {
      if (value >= -2 && value <= 2 && _increment != value) {
        _increment = value;
        notifyIncrementChanged ();
      }
    }
  }
  internal void setIncrementUp () {
    if (_increment < 2) {
      _increment += 1;
      notifyIncrementChanged ();
    }
  }
  internal void setIncrementDown () {
    if (_increment > -2) {
      _increment -= 1;
      notifyIncrementChanged ();
    }
  }
  private bool _x10UTincrement = false;
  internal bool x10UTincrement {
    get {
      return _x10UTincrement;
    }
    set {
      _x10UTincrement = value;
      notifyX10Changed ();
    }
  }

  #endregion

  #region MainWindow Modules

  internal enum ModuleType {
    PAGER,
    //timealarm
    //increment
    INPUT,
    TOOLS,
    GIZMO,
    //totdv
    EJECT,
    ORBIT,
    ENCOT,
    PATCH
  };

  private static readonly string[] moduleNames = {
  "Maneuver Pager",
  "Precise Input",
  "Orbit Tools",
  "Maneuver Gizmo",
  "Ejection angles",
  "Orbit Info",
  "Next Encounter",
  "Patches Control",
};

  private bool[] moduleState = {
  true,
  true,
  true,
  false,
  false,
  false,
  false,
  true
};

  private bool _modulesChanged = false;

  internal bool modulesChanged {
    get {
      if (_modulesChanged) {
        _modulesChanged = false;
        return true;
      }
      return false;
    }
  }

  internal static string getModuleName (ModuleType type) {
    return moduleNames[(int)type];
  }

  internal bool getModuleState (ModuleType type) {
    return moduleState[(int)type];
  }

  internal void setModuleState (ModuleType type, bool state) {
    if (moduleState[(int)type] != state) {
      moduleState[(int)type] = state;
      _modulesChanged = true;
    }
  }

  #endregion

  #region Hotkeys

  internal enum HotkeyType {
    PROGINC,
    PROGDEC,
    PROGZER,
    NORMINC,
    NORMDEC,
    NORMZER,
    RADIINC,
    RADIDEC,
    RADIZER,
    TIMEINC,
    TIMEDEC,
    CIRCORB,
    TURNOUP,
    TURNODN,
    PAGEINC,
    PAGECON,
    HIDEWIN,
    FOCNENC,
    FOCVESL,
    PLUSORB,
    MINUORB,
    PAGEX10,
    NEXTMAN,
    PREVMAN,
    MNVRDEL
  };

  private KeyCode[] hotkeys = {
  KeyCode.Keypad8,    //PROGINC
  KeyCode.Keypad5,    //PROGDEC
  KeyCode.None,       //PROGZER
  KeyCode.Keypad9,    //NORMINC
  KeyCode.Keypad7,    //NORMDEC
  KeyCode.None,       //NORMZER
  KeyCode.Keypad6,    //RADIINC
  KeyCode.Keypad4,    //RADIDEC
  KeyCode.None,       //RADIZER
  KeyCode.Keypad3,    //TIMEINC
  KeyCode.Keypad1,    //TIMEDEC
  KeyCode.None,       //CIRCORB
  KeyCode.None,       //TURNOUP
  KeyCode.None,       //TURNODN
  KeyCode.Keypad0,    //PAGEINC
  KeyCode.Keypad2,    //PAGECON
  KeyCode.P,          //HIDEWIN
  KeyCode.None,       //FOCNENC
  KeyCode.None,       //FOCVESL
  KeyCode.None,       //PLUSORB
  KeyCode.None,       //MINUORB
  KeyCode.None,       //PAGEX10
  KeyCode.None,       //NEXTMAN
  KeyCode.None,       //PREVMAN
  KeyCode.None        //MNVRDEL
};
  private bool[] hotkeyPresses = Enumerable.Repeat(false, Enum.GetValues(typeof(HotkeyType)).Length).ToArray ();

  internal void setHotkey (HotkeyType type, KeyCode code) {
    hotkeys[(int)type] = code;
  }

  internal KeyCode getHotkey (HotkeyType type) {
    return hotkeys[(int)type];
  }

  internal void registerHotkeyPress (HotkeyType type) {
    hotkeyPresses[(int)type] = true;
  }

  internal bool isHotkeyRegistered (HotkeyType type) {
    if (hotkeyPresses[(int)type] == true) {
      hotkeyPresses[(int)type] = false;
      return true;
    }
    return false;
  }

  #endregion

  #region Listeners

  private enum changeType {
    x10,
    increment,
    visibility
  }

  private Dictionary<changeType, List<Action>> _listeners;

  private Dictionary<changeType, List<Action>> listeners {
    get {
      if (_listeners == null) {
        _listeners = new Dictionary<changeType, List<Action>> (3);
        _listeners[changeType.x10] = new List<Action> ();
        _listeners[changeType.increment] = new List<Action> ();
        _listeners[changeType.visibility] = new List<Action> ();
      }
      return _listeners;
    }
  }

  public void listenTox10Change (Action listener) {
    listeners[changeType.x10].Add (listener);
  }

  public void listenToIncrementChange (Action listener) {
    listeners[changeType.increment].Add (listener);
  }

  public void listenToShowChange (Action listener) {
    listeners[changeType.visibility].Add (listener);
  }

  public void removeListener (Action listener) {
    foreach (var list in listeners.Values)
      list.RemoveAll (a => (a == listener));
  }

  private void notifyX10Changed () {
    foreach (var act in listeners[changeType.x10])
      act ();
  }

  private void notifyIncrementChanged () {
    foreach (var act in listeners[changeType.increment])
      act ();
  }

  private void notifyShowChanged () {
    foreach (var act in listeners[changeType.visibility])
      act ();
  }

  #endregion

  #region Save/Load

  internal void saveConfig () {
    Debug.Log ("Saving PreciseManeuver settings.");
    PluginConfiguration config = KSP.IO.PluginConfiguration.CreateForType<PreciseManeuver> (null);

    foreach (HotkeyType type in Enum.GetValues (typeof (HotkeyType)))
      config[type.ToString ()] = hotkeys[(int)type].ToString ();

    foreach (ModuleType type in Enum.GetValues (typeof (ModuleType)))
      config[type.ToString ()] = moduleState[(int)type];

    config["increment"] = _increment;
    config["x10UTincrement"] = x10UTincrement;

    config["mainWindowX"] = (int)_mainWindowPos.x;
    config["mainWindowY"] = (int)_mainWindowPos.y;
    config["keyWindowX"] = (int)_keymapperWindowPos.x;
    config["keyWindowY"] = (int)_keymapperWindowPos.y;

    config.save ();
  }

  internal void loadConfig () {
    Debug.Log ("Loading PreciseManeuver settings.");
    PluginConfiguration config = KSP.IO.PluginConfiguration.CreateForType<PreciseManeuver> (null);
    config.load ();

    try {
      foreach (HotkeyType type in Enum.GetValues (typeof (HotkeyType)))
        hotkeys[(int)type] = (KeyCode)Enum.Parse (typeof (KeyCode), config.GetValue<String> (type.ToString (), hotkeys[(int)type].ToString ()));

      foreach (ModuleType type in Enum.GetValues (typeof (ModuleType)))
        moduleState[(int)type] = config.GetValue<bool> (type.ToString (), moduleState[(int)type]);

      _increment = config.GetValue<int> ("increment", _increment);
      x10UTincrement = config.GetValue<bool> ("x10UTincrement", x10UTincrement);

      _mainWindowPos.x = config.GetValue<int> ("mainWindowX", (int)_mainWindowPos.x);
      _mainWindowPos.y = config.GetValue<int> ("mainWindowY", (int)_mainWindowPos.y);
      _keymapperWindowPos.x = config.GetValue<int> ("keyWindowX", (int)_keymapperWindowPos.x);
      _keymapperWindowPos.y = config.GetValue<int> ("keyWindowY", (int)_keymapperWindowPos.y);
    } catch (Exception) {
      // do nothing here, the defaults are already set
    }
  }

  #endregion
}
}
