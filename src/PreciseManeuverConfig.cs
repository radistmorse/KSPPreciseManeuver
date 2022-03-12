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
using UnityEngine.Events;
using KSP.IO;
using KSP.Localization;
using System.Collections.Generic;

namespace KSPPreciseManeuver {
  internal class PreciseManeuverConfig {

    #region Singleton

    private PreciseManeuverConfig () {
      LoadConfig ();
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

    private AssetBundle prefabs;
    internal AssetBundle Prefabs {
      get {
        if (prefabs == null) {
          var path = System.Reflection.Assembly.GetExecutingAssembly().Location;
          path = path.Replace (System.IO.Path.GetFileName (path), "precisemaneuverprefabs");

          prefabs = AssetBundle.LoadFromFile (path);
        }
        return prefabs;
      }
    }

    internal bool UiActive { get; set; } = true;

    internal int ConicsMode {
      get {
        return (int)FlightGlobals.ActiveVessel.patchedConicRenderer.relativityMode;
      }
      set {
        if (value >= 0 && value <= 4)
          FlightGlobals.ActiveVessel.patchedConicRenderer.relativityMode = (PatchRendering.RelativityMode)value;
        NotifyConicsModeChanged ();
      }
    }

    private float guiScale = 1f;
    internal float GUIScale {
      get { return guiScale; }
      set {
        if (value != guiScale) {
          guiScale = value;
          NotifyScaleChanged ();
        }
      }
    }

    internal float GizmoSensitivity { get; set; } = 0;

    internal void SetKeyboardInputLock () {
      InputLockManager.SetControlLock (ControlTypes.KEYBOARDINPUT, "PreciseManeuverKeyboardControlLock");
    }

    internal void ResetKeyboardInputLock () {
      InputLockManager.RemoveControlLock ("PreciseManeuverKeyboardControlLock");
    }

    #region MainWindow

    private bool showMainWindow = true;
    internal bool ShowMainWindow {
      get { return showMainWindow; }
      set {
        if (showMainWindow != value) {
          showMainWindow = value;
          NotifyShowChanged ();
        }
      }
    }

    internal bool IsInBackground { get; set; } = false;

    internal bool IsTooltipsEnabled { get; set; } = true;

    private bool tooltipsPrevious = true;

    internal bool TooltipsChanged {
      get {
        if (tooltipsPrevious != IsTooltipsEnabled) {
          tooltipsPrevious = IsTooltipsEnabled;
          return true;
        }
        return false;
      }
    }

    internal Vector3 MainWindowPos { get; set; }

    #endregion

    #region KeymapperWindow

    private bool showKeymapperWindow = false;
    internal bool ShowKeymapperWindow {
      get { return showKeymapperWindow; }
      set {
        if (showKeymapperWindow != value) {
          showKeymapperWindow = value;
          NotifyShowChanged ();
        }
      }
    }

    internal Vector3 KeymapperWindowPos { get; set; }

    #endregion

    #region Increment

    private int increment = 0;
    internal double Increment { get { return System.Math.Pow (10, increment); } }
    internal double IncrementDeg { get { return System.Math.PI * System.Math.Pow (10, increment) / 180; } }
    internal double IncrementUt { get { return Increment * (X10UTincrement ? 10 : 1); } }
    internal int IncrementRaw {
      get { return increment; }
      set {
        if (value >= -2 && value <= 2 && increment != value) {
          increment = value;
          NotifyIncrementChanged ();
        }
      }
    }
    internal void SetIncrementUp () {
      if (increment < 2) {
        increment += 1;
        NotifyIncrementChanged ();
      }
    }
    internal void SetIncrementDown () {
      if (increment > -2) {
        increment -= 1;
        NotifyIncrementChanged ();
      }
    }
    private bool x10UTincrement = false;
    internal bool X10UTincrement {
      get {
        return x10UTincrement;
      }
      set {
        x10UTincrement = value;
        NotifyX10Changed ();
      }
    }

    #endregion

    #region Presets

    Dictionary<string, Vector3d> presets = new Dictionary<string, Vector3d> ();

    internal void AddPreset (string name) {
      if (presets.ContainsKey (name))
        presets.Remove (name);
      presets.Add (name, NodeManager.Instance.CurrentNode.DeltaV);
    }

    internal void RemovePreset (string name) {
      presets.Remove (name);
    }

    internal Vector3d GetPreset (string name) {
      if (presets.ContainsKey (name))
        return presets[name];
      return Vector3d.zero;
    }

    internal List<string> GetPresetNames () {
      var list = presets.Keys.ToList();
      list.Sort ();
      return list;
    }

    #endregion

    #region MainWindow Modules

    internal enum ModuleType {
      PAGER = 0,
      TIME = 1,
      SAVER = 2,
      INCR = 3,
      INPUT = 4,
      TOOLS = 5,
      GIZMO = 6,
      ENCOT = 7,
      EJECT = 8,
      ORBIT = 9,
      PATCH = 10
    };

    private static readonly string[] moduleLabels = {
    "precisemaneuver_module_pager",
    "time. should never appear.",
    "precisemaneuver_module_saver",
    "increment. should never appear",
    "precisemaneuver_module_precise_input",
    "precisemaneuver_module_orbit_tools",
    "precisemaneuver_module_gizmo",
    "precisemaneuver_module_next_encounter",
    "precisemaneuver_module_ejection",
    "precisemaneuver_module_info",
    "precisemaneuver_module_patches",
  };

    private bool[] moduleState = {
    true,
    true,
    false,
    true,
    true,
    true,
    false,
    false,
    false,
    false,
    true
  };

    private bool modulesChanged = false;

    internal bool ModulesChanged {
      get {
        if (modulesChanged) {
          modulesChanged = false;
          return true;
        }
        return false;
      }
    }

    internal static string GetModuleName (ModuleType type) {
      return Localizer.Format (moduleLabels[(int)type]);
    }

    internal bool GetModuleState (ModuleType type) {
      return moduleState[(int)type];
    }

    internal void SetModuleState (ModuleType type, bool state) {
      if (moduleState[(int)type] != state) {
        moduleState[(int)type] = state;
        modulesChanged = true;
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
    KeyCode.None,       //PLUSORB
    KeyCode.None,       //MINUORB
    KeyCode.None,       //PAGEX10
    KeyCode.None,       //NEXTMAN
    KeyCode.None,       //PREVMAN
    KeyCode.None        //MNVRDEL
  };

    internal void SetHotkey (HotkeyType type, KeyCode code) {
      hotkeys[(int)type] = code;
    }

    internal KeyCode GetHotkey (HotkeyType type) {
      return hotkeys[(int)type];
    }

    #endregion

    #region Listeners

    private enum ChangeType {
      x10,
      increment,
      visibility,
      conicsmode,
      scale
    }

    private Dictionary<ChangeType, List<UnityAction>> listeners;

    private Dictionary<ChangeType, List<UnityAction>> Listeners {
      get {
        if (listeners == null) {
          listeners = new Dictionary<ChangeType, List<UnityAction>> () {
            [ChangeType.x10] = new List<UnityAction> (),
            [ChangeType.increment] = new List<UnityAction> (),
            [ChangeType.visibility] = new List<UnityAction> (),
            [ChangeType.conicsmode] = new List<UnityAction> (),
            [ChangeType.scale] = new List<UnityAction> ()
          };
        }
        return listeners;
      }
    }

    public void ListenTox10Change (UnityAction listener) {
      Listeners[ChangeType.x10].Add (listener);
    }

    public void ListenToIncrementChange (UnityAction listener) {
      Listeners[ChangeType.increment].Add (listener);
    }

    public void ListenToShowChange (UnityAction listener) {
      Listeners[ChangeType.visibility].Add (listener);
    }

    public void ListenToConicsModeChange (UnityAction listener) {
      Listeners[ChangeType.conicsmode].Add (listener);
    }

    public void ListenToScaleChange (UnityAction listener) {
      Listeners[ChangeType.scale].Add (listener);
    }

    public void RemoveListener (UnityAction listener) {
      foreach (var list in Listeners.Values)
        list.RemoveAll (a => (a == listener));
    }

    private void NotifyX10Changed () {
      foreach (var act in Listeners[ChangeType.x10])
        act ();
    }

    private void NotifyIncrementChanged () {
      foreach (var act in Listeners[ChangeType.increment])
        act ();
    }

    private void NotifyShowChanged () {
      foreach (var act in Listeners[ChangeType.visibility])
        act ();
    }

    private void NotifyConicsModeChanged () {
      foreach (var act in Listeners[ChangeType.conicsmode])
        act ();
    }

    private void NotifyScaleChanged () {
      foreach (var act in Listeners[ChangeType.scale])
        act ();
    }

    #endregion

    #region Save/Load

    internal void SaveConfig () {
      Debug.Log ("[Precise Maneuver] Saving settings.");
      PluginConfiguration config = KSP.IO.PluginConfiguration.CreateForType<PreciseManeuver> (null);

      config["enabled"] = showMainWindow;
      config["background"] = IsInBackground;
      config["tooltips"] = IsTooltipsEnabled;

      foreach (HotkeyType type in Enum.GetValues (typeof (HotkeyType)))
        config[type.ToString ()] = hotkeys[(int)type].ToString ();

      foreach (ModuleType type in Enum.GetValues (typeof (ModuleType)))
        config[type.ToString ()] = moduleState[(int)type];

      config["increment"] = increment;
      config["x10UTincrement"] = X10UTincrement;

      config["scale"] = (int)(GUIScale * 1000f);
      config["sensitivity"] = (int)(GizmoSensitivity * 10000f);

      config["mainWindowX"] = (int)MainWindowPos.x;
      config["mainWindowY"] = (int)MainWindowPos.y;
      config["keyWindowX"] = (int)KeymapperWindowPos.x;
      config["keyWindowY"] = (int)KeymapperWindowPos.y;

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

    internal void LoadConfig () {
      Debug.Log ("[Precise Maneuver] Loading settings.");
      PluginConfiguration config = KSP.IO.PluginConfiguration.CreateForType<PreciseManeuver> (null);
      config.load ();

      try {
        showMainWindow = config.GetValue<bool> ("enabled", showMainWindow);
        IsInBackground = config.GetValue<bool> ("background", IsInBackground);
        IsTooltipsEnabled = config.GetValue<bool> ("tooltips", IsTooltipsEnabled);

        foreach (HotkeyType type in Enum.GetValues (typeof (HotkeyType)))
          hotkeys[(int)type] = (KeyCode)Enum.Parse (typeof (KeyCode), config.GetValue<string> (type.ToString (), hotkeys[(int)type].ToString ()));

        foreach (ModuleType type in Enum.GetValues (typeof (ModuleType)))
          moduleState[(int)type] = config.GetValue<bool> (type.ToString (), moduleState[(int)type]);

        increment = config.GetValue<int> ("increment", increment);
        X10UTincrement = config.GetValue<bool> ("x10UTincrement", X10UTincrement);

        GUIScale = (float)config.GetValue<int> ("scale", (int)(GUIScale * 1000f)) / 1000f;
        GizmoSensitivity = (float)config.GetValue<int> ("sensitivity", (int)(GizmoSensitivity * 10000f)) / 10000f;

        Vector2 mainWindowPos;
        mainWindowPos.x = config.GetValue<int> ("mainWindowX", (int)MainWindowPos.x);
        mainWindowPos.y = config.GetValue<int> ("mainWindowY", (int)MainWindowPos.y);
        MainWindowPos = mainWindowPos;
        Vector2 keymapperWindowPos;
        keymapperWindowPos.x = config.GetValue<int> ("keyWindowX", (int)KeymapperWindowPos.x);
        keymapperWindowPos.y = config.GetValue<int> ("keyWindowY", (int)KeymapperWindowPos.y);
        KeymapperWindowPos = keymapperWindowPos;

        // presets
        var count = config.GetValue<int> ("presetsCount", 0);
        for (int i = 0; i < count; i++) {
          var name = config.GetValue<string> ("preset"+i.ToString()+"name", "");
          if (name != "" &&
              Double.TryParse (config.GetValue<string> ("preset" + i.ToString () + "dx", ""), out double dx) &&
              Double.TryParse (config.GetValue<string> ("preset" + i.ToString () + "dy", ""), out double dy) &&
              Double.TryParse (config.GetValue<string> ("preset" + i.ToString () + "dz", ""), out double dz)) {
            presets.Add (name, new Vector3d (dx, dy, dz));
          }
        }
      } catch (Exception e) {
        Debug.Log ("[Precise Maneuver] There was an error reading config. That's OK if you're launching the mod the first time. " + e);
      }
    }

    #endregion

  }
}
