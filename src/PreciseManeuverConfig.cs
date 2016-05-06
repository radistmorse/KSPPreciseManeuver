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
        path = path.Replace (System.IO.Path.GetFileName (path), "precisemaneuverprefabs");
        var www = new WWW("file://"+path);
        _prefabs = www.assetBundle;
      }
      return _prefabs;
    }
  }

  internal int conicsMode {
    get {
      return NodeTools.getConicsMode ();
    }
    set {
      NodeTools.setConicsMode (value);
      notifyConicsModeChanged ();
    }
  }

  private float _guiScale = 1f;
  internal float guiScale {
    get { return _guiScale; }
    set {
      if (value != _guiScale) {
        _guiScale = value;
        notifyScaleChanged ();
      }
    }
  }

  internal float gizmoSensitivity { get; set; } = 0;

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

  #region Presets

  Dictionary<string, Vector3d> presets = new Dictionary<string, Vector3d> ();

  internal void addPreset (string name) {
    presets.Add (name, NodeManager.Instance.currentNode.DeltaV);
  }

  internal void removePreset (string name) {
    presets.Remove (name);
  }

  internal Vector3d getPreset (string name) {
    if (presets.ContainsKey(name))
      return presets[name];
    return Vector3d.zero;
  }

  internal List<string> getPresetNames () {
    var list = presets.Keys.ToList();
    list.Sort ();
    return list;
  }

  #endregion

  #region MainWindow Modules

  internal enum ModuleType {
    PAGER,
    //timealarm
    //increment
    SAVER,
    INPUT,
    TOOLS,
    GIZMO,
    ENCOT,
    EJECT,
    ORBIT,
    PATCH
  };

  private static readonly string[] moduleNames = {
    "Maneuver Pager",
    "Maneuver Presets",
    "Precise Input",
    "Orbit Tools",
    "Maneuver Gizmo",
    "Next Encounter",
    "Ejection angles",
    "Orbit Info",
    "Patches Control",
  };

  private bool[] moduleState = {
    true,
    false,
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

  internal void setHotkey (HotkeyType type, KeyCode code) {
    hotkeys[(int)type] = code;
  }

  internal KeyCode getHotkey (HotkeyType type) {
    return hotkeys[(int)type];
  }

  #endregion

  #region Listeners

  private enum changeType {
    x10,
    increment,
    visibility,
    conicsmode,
    scale
  }

  private Dictionary<changeType, List<Action>> _listeners;

  private Dictionary<changeType, List<Action>> listeners {
    get {
      if (_listeners == null) {
        _listeners = new Dictionary<changeType, List<Action>> (3);
        _listeners[changeType.x10] = new List<Action> ();
        _listeners[changeType.increment] = new List<Action> ();
        _listeners[changeType.visibility] = new List<Action> ();
        _listeners[changeType.conicsmode] = new List<Action> ();
        _listeners[changeType.scale] = new List<Action> ();
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

  public void listenToConicsModeChange (Action listener) {
    listeners[changeType.conicsmode].Add (listener);
  }

  public void listenToScaleChange (Action listener) {
    listeners[changeType.scale].Add (listener);
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

  private void notifyConicsModeChanged () {
    foreach (var act in listeners[changeType.conicsmode])
      act ();
  }

  private void notifyScaleChanged () {
    foreach (var act in listeners[changeType.scale])
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

    config["scale"] = (int)(guiScale*1000f);
    config["sensitivity"] = (int)(gizmoSensitivity*10000f);

    config["mainWindowX"] = (int)_mainWindowPos.x;
    config["mainWindowY"] = (int)_mainWindowPos.y;
    config["keyWindowX"] = (int)_keymapperWindowPos.x;
    config["keyWindowY"] = (int)_keymapperWindowPos.y;

    // presets
    config["presetsCount"] = (int)presets.Count;
    int i = 0;
    foreach (KeyValuePair<string, Vector3d> item in presets) {
      config["preset" + i.ToString () + "name"] = item.Key;
      config["preset" + i.ToString () + "dx"] = item.Value.x.ToString ("G17");
      config["preset" + i.ToString () + "dy"] = item.Value.y.ToString ("G17");
      config["preset" + i.ToString () + "dz"] = item.Value.z.ToString ("G17");
      i++;
    }

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

      guiScale = (float)config.GetValue<int> ("scale", (int)(guiScale*1000f)) / 1000f;
      gizmoSensitivity = (float)config.GetValue<int> ("scale", (int)(gizmoSensitivity*10000f)) / 10000f;


      _mainWindowPos.x = config.GetValue<int> ("mainWindowX", (int)_mainWindowPos.x);
      _mainWindowPos.y = config.GetValue<int> ("mainWindowY", (int)_mainWindowPos.y);
      _keymapperWindowPos.x = config.GetValue<int> ("keyWindowX", (int)_keymapperWindowPos.x);
      _keymapperWindowPos.y = config.GetValue<int> ("keyWindowY", (int)_keymapperWindowPos.y);

      // presets
      var count = config.GetValue<int> ("presetsCount", 0);
      for (int i = 0; i < count; i++) {
        var name = config.GetValue<string> ("preset"+i.ToString()+"name", "");
        double dx;
        double dy;
        double dz;

        if (name != "" &&
              Double.TryParse (config.GetValue<string> ("preset" + i.ToString () + "dx", ""), out dx) &&
              Double.TryParse (config.GetValue<string> ("preset" + i.ToString () + "dy", ""), out dy) &&
              Double.TryParse (config.GetValue<string> ("preset" + i.ToString () + "dz", ""), out dz)) {
          presets.Add (name, new Vector3d (dx, dy, dz));
        }
      }
    } catch (Exception) {
      // do nothing here, the defaults are already set
    }
  }

  #endregion

}
}
