using System;
using UnityEngine;
using KSP.IO;

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

namespace KSPPreciseManeuver {
internal class PreciseManeuverConfig {

  internal static PreciseManeuverConfig _instance = null;

  internal static PreciseManeuverConfig getInstance () {
    if (_instance == null) _instance = new PreciseManeuverConfig ();
    return _instance;
  }

  private Rect _mainWindowPos = new Rect (Screen.width / 10, 20, 0, 0);
  internal Rect mainWindowPos {
    get {
      return _mainWindowPos;
    }
    set {
      _mainWindowPos = value;
    }
  }
  internal void readjustMainWindow() {
    _mainWindowPos.height = 0;
  }
  private Rect _keymapperWindowPos = new Rect (Screen.width / 5, 20, 0, 0);
  internal Rect keymapperWindowPos {
    get {
      return _keymapperWindowPos;
    }
    set {
      _keymapperWindowPos = value;
    }
  }

  internal bool showMainWindow = true;
  internal bool showKeymapperWindow = false;

  private int _increment = 0;
  internal double increment { get { return Math.Pow (10, _increment); } }
  internal int incrementRaw {
    get {
      return _increment;
    }
    set {
      if (value >= -2 && value <= 2)
        _increment = value;
    }
  }
  internal void setIncrementUp() {
    _increment = (_increment == 2) ? 2 : _increment+1;
  }
  internal void setIncrementDown() {
    _increment = (_increment == -2) ? -2 : _increment-1;
  }

  internal bool x10UTincrement { get; set; } = true;

  private bool useKSPskin = true;
  private GUISkin _skin = null;
  internal GUISkin skin {
    get {
      if (_skin == null) {
        if (useKSPskin) {
          _skin = (GUISkin)GUISkin.Instantiate(HighLogic.Skin);
          var padding = skin.button.padding;
          padding.top = 6;
          padding.bottom = 3;
        } else {
          _skin = GUI.skin;
        }
      }
      return _skin;
    }
  }

  internal enum HotkeyType {
    PROGINC,
    PROGDEC,
    NORMINC,
    NORMDEC,
    RADIINC,
    RADIDEC,
    TIMEINC,
    TIMEDEC,
    PAGEINC,
    PAGECON,
    HIDEWIN
  };

  private KeyCode[] hotkeys = { KeyCode.Keypad8,
                                KeyCode.Keypad5,
                                KeyCode.Keypad9,
                                KeyCode.Keypad7,
                                KeyCode.Keypad6,
                                KeyCode.Keypad4,
                                KeyCode.Keypad3,
                                KeyCode.Keypad1,
                                KeyCode.Keypad0,
                                KeyCode.Keypad2,
                                KeyCode.P
                              };

  internal void setHotkey (HotkeyType type, KeyCode code) {
    hotkeys[(int)type] = code;
  }

  internal KeyCode getHotkey (HotkeyType type) {
    return hotkeys[(int)type];
  }

  /// <summary>
  /// Save our configuration to file.
  /// </summary>
  internal void saveConfig() {
    Debug.Log ("Saving PreciseManeuver settings.");
    PluginConfiguration config = KSP.IO.PluginConfiguration.CreateForType<PreciseManeuver> (null);

    foreach (HotkeyType type in Enum.GetValues (typeof (HotkeyType)))
      config[type.ToString ()] = hotkeys[(int)type].ToString ();

    config["increment"] = _increment;
    config["x10UTincrement"] = x10UTincrement;

    config["mainWindowX"] = (int)_mainWindowPos.x;
    config["mainWindowY"] = (int)_mainWindowPos.y;
    config["keyWindowX"] = (int)_keymapperWindowPos.x;
    config["keyWindowY"] = (int)_keymapperWindowPos.y;

    config["useKSPskin"] = useKSPskin;

    config.save();
  }

  /// <summary>
  /// Load any saved configuration from file.
  /// </summary>
  internal void loadConfig () {
    Debug.Log ("Loading PreciseManeuver settings.");
    PluginConfiguration config = KSP.IO.PluginConfiguration.CreateForType<PreciseManeuver> (null);
    config.load ();

    try {
      foreach (HotkeyType type in Enum.GetValues (typeof (HotkeyType)))
        hotkeys[(int)type] = (KeyCode)Enum.Parse (typeof (KeyCode), config.GetValue<String> (type.ToString (), hotkeys[(int)type].ToString ()));

      _increment = config.GetValue<int> ("increment", _increment);
      x10UTincrement = config.GetValue<bool> ("x10UTincrement", x10UTincrement);

      useKSPskin = config.GetValue<bool>("useKSPskin", useKSPskin);

      _mainWindowPos.x = config.GetValue<int> ("mainWindowX", (int)_mainWindowPos.x);
      _mainWindowPos.y = config.GetValue<int> ("mainWindowY", (int)_mainWindowPos.y);
      _keymapperWindowPos.x = config.GetValue<int> ("keyWindowX", (int)_keymapperWindowPos.x);
      _keymapperWindowPos.y = config.GetValue<int> ("keyWindowY", (int)_keymapperWindowPos.y);
    } catch (Exception) {
      // do nothing here, the defaults are already set
    }
  }
}
}
