/******************************************************************************
 * Copyright (c) 2015-2016, George Sedov
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

using UnityEngine;
using UnityEngine.UI;
using KSP.Localization;

namespace KSPPreciseManeuver {
  internal class PreciseManeuverHotkeys {

    private PreciseManeuverConfig config = PreciseManeuverConfig.Instance;
    private NodeManager nodeManager = NodeManager.Instance;

    private bool KeyboardLocked {
      get {
        return (InputLockManager.LockMask & (ulong)ControlTypes.KEYBOARDINPUT) == (ulong)ControlTypes.KEYBOARDINPUT;
      }
    }

    #region GUI Controls

    private class KeybindingControlInterface : UI.IKeybindingsControl {
      private PreciseManeuverConfig.HotkeyType _type;
      private string _name;
      private PreciseManeuverHotkeys _parent;
      internal KeybindingControlInterface (PreciseManeuverHotkeys parent, PreciseManeuverConfig.HotkeyType type, string name) {
        _parent = parent;
        _type = type;
        _name = name;
      }

      public UnityEngine.Events.UnityAction<string> replaceTextComponentWithTMPro (Text text) {
        var ugui = GUIComponentManager.ReplaceTextWithTMPro (text);
        return ugui.SetText;
      }

      public KeyCode code {
        get { return PreciseManeuverConfig.Instance.GetHotkey (_type); }
      }

      public string keyName {
        get { return _name; }
      }

      public void setKey (UnityEngine.Events.UnityAction<KeyCode> callback) {
        if (_parent.expectingConfigHotkey) {
          // We are already expecting another hotkey, so abort that first
          _parent.onSetCallback (PreciseManeuverConfig.Instance.GetHotkey (_parent.expectedHotkey));
        }
        ScreenMessages.PostScreenMessage (Localizer.Format ("precisemaneuver_keybindings_expecting_hotkey", _name.ToLower ()), expectedTimeout, ScreenMessageStyle.UPPER_CENTER);
        _parent.expectedHotkey = _type;
        _parent.expectingConfigHotkey = true;
        _parent.expectedTime = Planetarium.GetUniversalTime () + expectedTimeout;
        _parent.onSetCallback = callback;
      }

      public void unsetKey () {
        PreciseManeuverConfig.Instance.SetHotkey (_type, KeyCode.None);
        _parent.expectingConfigHotkey = false;
      }

      public void abortSetKey () {
        _parent.expectingConfigHotkey = false;
      }
    }

    #endregion

    #region Hotkey Set Vars

    private bool expectingConfigHotkey = false;
    private PreciseManeuverConfig.HotkeyType expectedHotkey;
    private double expectedTime = 0.0;
    private const float expectedTimeout = 5.0f;
    private int expectedHotkeyCooldown = 0;
    private UnityEngine.Events.UnityAction<KeyCode> onSetCallback;

    #endregion

    #region Hotkey Repeat Vars

    private bool repeatButtonPressed = false;
    private KeyCode repeatButtonPressedCode = KeyCode.None;
    private int repeatButtonPressInterval = 0;
    private int repeatButtonReleaseInterval = 0;
    private bool RepeatButtonDelay (KeyCode code) {
      if (repeatButtonPressed)
        return false;
      if (code != repeatButtonPressedCode) {
        repeatButtonPressedCode = code;
        repeatButtonPressInterval = 0;
      }
      repeatButtonPressed = true;
      nodeManager.BeginAtomicChange ();
      if (repeatButtonPressInterval > 20 || repeatButtonPressInterval == 0)
        return true;
      else
        return false;
    }

    #endregion

    #region GUI Construct

    private GameObject m_KeybindingsCtrlPrefab = PreciseManeuverConfig.Instance.Prefabs.LoadAsset<GameObject> ("PreciseManeuverKeyControl");

    internal void FillKeymapperWindow (UI.DraggableWindow window) {
      NewKeyControl (window, Localizer.Format ("precisemaneuver_keybindings_enable"), PreciseManeuverConfig.HotkeyType.HIDEWIN);
      NewKeyControl (window, Localizer.Format ("precisemaneuver_keybindings_proginc"), PreciseManeuverConfig.HotkeyType.PROGINC);
      NewKeyControl (window, Localizer.Format ("precisemaneuver_keybindings_progdec"), PreciseManeuverConfig.HotkeyType.PROGDEC);
      NewKeyControl (window, Localizer.Format ("precisemaneuver_keybindings_progzero"), PreciseManeuverConfig.HotkeyType.PROGZER);
      NewKeyControl (window, Localizer.Format ("precisemaneuver_keybindings_norminc"), PreciseManeuverConfig.HotkeyType.NORMINC);
      NewKeyControl (window, Localizer.Format ("precisemaneuver_keybindings_normdec"), PreciseManeuverConfig.HotkeyType.NORMDEC);
      NewKeyControl (window, Localizer.Format ("precisemaneuver_keybindings_normzero"), PreciseManeuverConfig.HotkeyType.NORMZER);
      NewKeyControl (window, Localizer.Format ("precisemaneuver_keybindings_radinc"), PreciseManeuverConfig.HotkeyType.RADIINC);
      NewKeyControl (window, Localizer.Format ("precisemaneuver_keybindings_raddec"), PreciseManeuverConfig.HotkeyType.RADIDEC);
      NewKeyControl (window, Localizer.Format ("precisemaneuver_keybindings_radzero"), PreciseManeuverConfig.HotkeyType.RADIZER);
      NewKeyControl (window, Localizer.Format ("precisemaneuver_keybindings_timeinc"), PreciseManeuverConfig.HotkeyType.TIMEINC);
      NewKeyControl (window, Localizer.Format ("precisemaneuver_keybindings_timedex"), PreciseManeuverConfig.HotkeyType.TIMEDEC);
      NewKeyControl (window, Localizer.Format ("precisemaneuver_keybindings_circularize"), PreciseManeuverConfig.HotkeyType.CIRCORB);
      NewKeyControl (window, Localizer.Format ("precisemaneuver_keybindings_turnup"), PreciseManeuverConfig.HotkeyType.TURNOUP);
      NewKeyControl (window, Localizer.Format ("precisemaneuver_keybindings_turndown"), PreciseManeuverConfig.HotkeyType.TURNODN);
      NewKeyControl (window, Localizer.Format ("precisemaneuver_keybindings_encfocus"), PreciseManeuverConfig.HotkeyType.FOCNENC);
      NewKeyControl (window, Localizer.Format ("precisemaneuver_keybindings_patchesmore"), PreciseManeuverConfig.HotkeyType.PLUSORB);
      NewKeyControl (window, Localizer.Format ("precisemaneuver_keybindings_patchesless"), PreciseManeuverConfig.HotkeyType.MINUORB);
      NewKeyControl (window, Localizer.Format ("precisemaneuver_keybindings_timex10"), PreciseManeuverConfig.HotkeyType.PAGEX10);
      NewKeyControl (window, Localizer.Format ("precisemaneuver_keybindings_nextnode"), PreciseManeuverConfig.HotkeyType.NEXTMAN);
      NewKeyControl (window, Localizer.Format ("precisemaneuver_keybindings_prevnode"), PreciseManeuverConfig.HotkeyType.PREVMAN);
      NewKeyControl (window, Localizer.Format ("precisemaneuver_keybindings_delnode"), PreciseManeuverConfig.HotkeyType.MNVRDEL);

      GameObject label = new GameObject ("PreciseManeuverKeybindingsAltLabel");
      label.AddComponent<RectTransform> ();
      var text = label.AddComponent<TMPro.TextMeshProUGUI> ();
      text.text = Localizer.Format ("precisemaneuver_keybindings_alt");
      text.font = UISkinManager.TMPFont;
      text.fontSize = 14;
      text.richText = false;
      text.color = Color.white;

      window.AddToContent (label);

      NewKeyControl (window, Localizer.Format ("precisemaneuver_keybindings_pageinc"), PreciseManeuverConfig.HotkeyType.PAGEINC);
      NewKeyControl (window, Localizer.Format ("precisemaneuver_keybindings_pageconics"), PreciseManeuverConfig.HotkeyType.PAGECON);
    }

    private void NewKeyControl (UI.DraggableWindow window, string title, PreciseManeuverConfig.HotkeyType type) {
      GameObject keybindingsCtrlObject = Object.Instantiate (m_KeybindingsCtrlPrefab);
      if (keybindingsCtrlObject == null)
        return;

      var keybindingCtrl = keybindingsCtrlObject.GetComponent<UI.KeybindingControl> ();
      keybindingCtrl.SetControl (new KeybindingControlInterface (this, type, title));
      window.AddToContent (keybindingsCtrlObject);

    }

    #endregion

    #region Hotkey Set

    internal void ProcessHotkeySet () {
      if (expectingConfigHotkey && expectedTime < Planetarium.GetUniversalTime ()) {
        expectingConfigHotkey = false;
        onSetCallback (config.GetHotkey (expectedHotkey));
      }
      if (Input.anyKey && GUIUtility.keyboardControl == 0) {
        if (expectingConfigHotkey && Input.anyKeyDown) {
          expectingConfigHotkey = false;
          KeyCode key = NodeTools.FetchKey ();
          if (key != KeyCode.None && key != KeyCode.Escape) {
            config.SetHotkey (expectedHotkey, key);
            ScreenMessages.PostScreenMessage ("Binded to '" + key.ToString () + "'", 0.5f, ScreenMessageStyle.UPPER_CENTER);
            onSetCallback (key);
            // prevent the hotkey being detected as pressed the same moment it was binded
            expectedHotkeyCooldown = 5;
          } else {
            onSetCallback (config.GetHotkey (expectedHotkey));
          }
        }
      }
    }

    #endregion

    #region Global Hotkeys

    internal void ProcessGlobalHotkeys () {
      if (Input.anyKey && GUIUtility.keyboardControl == 0 && !KeyboardLocked) {
        // hide/show window
        if (Input.GetKeyDown (config.GetHotkey (PreciseManeuverConfig.HotkeyType.HIDEWIN)))
          config.ShowMainWindow = !config.ShowMainWindow;
        // more patches
        if (Input.GetKeyDown (config.GetHotkey (PreciseManeuverConfig.HotkeyType.PLUSORB)))
          FlightGlobals.ActiveVessel.patchedConicSolver.IncreasePatchLimit ();
        // less patches
        if (Input.GetKeyDown (config.GetHotkey (PreciseManeuverConfig.HotkeyType.MINUORB)))
          FlightGlobals.ActiveVessel.patchedConicSolver.DecreasePatchLimit ();
        // Page Conics
        if (Input.GetKeyDown (config.GetHotkey (PreciseManeuverConfig.HotkeyType.PAGECON))) {
          int mode = config.ConicsMode;
          if (Event.current.alt)
            mode = (mode == 0) ? 4 : mode - 1;
          else
            mode = (mode == 4) ? 0 : mode + 1;
          config.ConicsMode = mode;
        }
      }
    }

    #endregion

    #region Process Hotkeys

    internal void ProcessRegularHotkeys () {
      repeatButtonPressed = false;

      if (expectedHotkeyCooldown > 0) {
        expectedHotkeyCooldown--;
        return;
      }

      if (Input.anyKey && GUIUtility.keyboardControl == 0 && !KeyboardLocked) {

        ManeuverNode node = nodeManager.CurrentNode;

        // process normal keyboard input
        double dvx = 0;
        double dvy = 0;
        double dvz = 0;
        double dut = 0;
        bool changed = false;
        // prograde increment
        if (Input.GetKey (config.GetHotkey (PreciseManeuverConfig.HotkeyType.PROGINC)) &&
            RepeatButtonDelay (config.GetHotkey (PreciseManeuverConfig.HotkeyType.PROGINC))) {
          dvz += config.Increment;
          changed = true;
        }
        // prograde decrement
        if (Input.GetKey (config.GetHotkey (PreciseManeuverConfig.HotkeyType.PROGDEC)) &&
            RepeatButtonDelay (config.GetHotkey (PreciseManeuverConfig.HotkeyType.PROGDEC))) {
          dvz -= config.Increment;
          changed = true;
        }
        // prograde zero
        if (Input.GetKeyDown (config.GetHotkey (PreciseManeuverConfig.HotkeyType.PROGZER))) {
          nodeManager.ChangeNodeDVMult (1, 1, 0);
        }
        // normal increment
        if (Input.GetKey (config.GetHotkey (PreciseManeuverConfig.HotkeyType.NORMINC)) &&
            RepeatButtonDelay (config.GetHotkey (PreciseManeuverConfig.HotkeyType.NORMINC))) {
          dvy += config.Increment;
          changed = true;
        }
        // normal decrement
        if (Input.GetKey (config.GetHotkey (PreciseManeuverConfig.HotkeyType.NORMDEC)) &&
            RepeatButtonDelay (config.GetHotkey (PreciseManeuverConfig.HotkeyType.NORMDEC))) {
          dvy -= config.Increment;
          changed = true;
        }
        // normal zero
        if (Input.GetKeyDown (config.GetHotkey (PreciseManeuverConfig.HotkeyType.NORMZER))) {
          nodeManager.ChangeNodeDVMult (1, 0, 1);
        }
        // radial increment
        if (Input.GetKey (config.GetHotkey (PreciseManeuverConfig.HotkeyType.RADIINC)) &&
            RepeatButtonDelay (config.GetHotkey (PreciseManeuverConfig.HotkeyType.RADIINC))) {
          dvx += config.Increment;
          changed = true;
        }
        // radial decrement
        if (Input.GetKey (config.GetHotkey (PreciseManeuverConfig.HotkeyType.RADIDEC)) &&
            RepeatButtonDelay (config.GetHotkey (PreciseManeuverConfig.HotkeyType.RADIDEC))) {
          dvx -= config.Increment;
          changed = true;
        }
        // radial zero
        if (Input.GetKeyDown (config.GetHotkey (PreciseManeuverConfig.HotkeyType.RADIZER))) {
          nodeManager.ChangeNodeDVMult (0, 1, 1);
        }
        // UT increment
        if (Input.GetKey (config.GetHotkey (PreciseManeuverConfig.HotkeyType.TIMEINC)) &&
            RepeatButtonDelay (config.GetHotkey (PreciseManeuverConfig.HotkeyType.TIMEINC))) {
          dut += config.IncrementUt;
          changed = true;
        }
        // UT decrement
        if (Input.GetKey (config.GetHotkey (PreciseManeuverConfig.HotkeyType.TIMEDEC)) &&
            RepeatButtonDelay (config.GetHotkey (PreciseManeuverConfig.HotkeyType.TIMEDEC))) {
          dut -= config.IncrementUt;
          changed = true;
        }
        if (changed)
          nodeManager.ChangeNodeDiff (dvx, dvy, dvz, dut);

        // change increment
        if (Input.GetKeyDown (config.GetHotkey (PreciseManeuverConfig.HotkeyType.PAGEINC))) {
          if (Event.current.alt)
            config.SetIncrementUp ();
          else
            config.SetIncrementDown ();
        }
        // toggle x10
        if (Input.GetKeyDown (config.GetHotkey (PreciseManeuverConfig.HotkeyType.PAGEX10)))
          config.X10UTincrement = !config.X10UTincrement;
        // next node
        if (Input.GetKeyDown (config.GetHotkey (PreciseManeuverConfig.HotkeyType.NEXTMAN)))
          nodeManager.SwitchNextNode ();
        // prev node
        if (Input.GetKeyDown (config.GetHotkey (PreciseManeuverConfig.HotkeyType.PREVMAN)))
          nodeManager.SwitchPreviousNode ();
        // delete node
        if (Input.GetKeyDown (config.GetHotkey (PreciseManeuverConfig.HotkeyType.MNVRDEL)))
          nodeManager.DeleteNode ();
        // turn orbit up
        if (Input.GetKey (config.GetHotkey (PreciseManeuverConfig.HotkeyType.TURNOUP)))
          if (RepeatButtonDelay (config.GetHotkey (PreciseManeuverConfig.HotkeyType.TURNOUP)))
            nodeManager.TurnOrbitUp ();
        // turn orbit down
        if (Input.GetKey (config.GetHotkey (PreciseManeuverConfig.HotkeyType.TURNODN)))
          if (RepeatButtonDelay (config.GetHotkey (PreciseManeuverConfig.HotkeyType.TURNODN)))
            nodeManager.TurnOrbitDown ();
        // circularize orbit
        if (Input.GetKeyDown (config.GetHotkey (PreciseManeuverConfig.HotkeyType.CIRCORB)))
          nodeManager.CircularizeOrbit ();
        // focus on target
        if (Input.GetKeyDown (config.GetHotkey (PreciseManeuverConfig.HotkeyType.FOCNENC))) {
          var nextEnc = NodeTools.FindNextEncounter ();
          if (nextEnc != null) {
            MapObject mapObject = PlanetariumCamera.fetch.targets.Find (o => (o.celestialBody != null) && (o.celestialBody == nextEnc));
            MapView.MapCamera.SetTarget (mapObject);
          }
        }
      }
      // manage the repeating hotkeys
      if (repeatButtonPressed) {
        if (repeatButtonPressInterval < 50)
          repeatButtonPressInterval++;
        repeatButtonReleaseInterval = 0;
      } else {
        if (repeatButtonReleaseInterval < 2) {
          repeatButtonReleaseInterval++;
        } else {
          if (repeatButtonReleaseInterval < 100) {
            nodeManager.EndAtomicChange ();
            repeatButtonReleaseInterval = 100;
          }
          repeatButtonPressInterval = 0;
          repeatButtonPressedCode = KeyCode.None;
        }
      }
    }

    #endregion

  }
}
