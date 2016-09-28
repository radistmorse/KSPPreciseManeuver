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

using System;
using KSPPreciseManeuver.UI;
using UnityEngine;

namespace KSPPreciseManeuver {
internal class PreciseManeuverHotkeys {

  private PreciseManeuverConfig config = PreciseManeuverConfig.Instance;
  private NodeManager nodeManager = NodeManager.Instance;

  private bool keyboardLocked {
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

    public KeyCode code {
      get { return PreciseManeuverConfig.Instance.getHotkey (_type); }
    }

    public string keyName {
      get { return _name; }
    }

    public void setKey (Action<KeyCode> callback) {
      if (_parent.expectingConfigHotkey) {
        // We are already expecting another hotkey, so abort that first
        _parent.onSetCallback (PreciseManeuverConfig.Instance.getHotkey (_parent.expectedHotkey));
      }
      ScreenMessages.PostScreenMessage ("Press a key to bind '" + _name.ToLower () + "'...", expectedTimeout, ScreenMessageStyle.UPPER_CENTER);
      _parent.expectedHotkey = _type;
      _parent.expectingConfigHotkey = true;
      _parent.expectedTime = Planetarium.GetUniversalTime () + expectedTimeout;
      _parent.onSetCallback = callback;
    }

    public void unsetKey () {
      PreciseManeuverConfig.Instance.setHotkey (_type, KeyCode.None);
      _parent.expectingConfigHotkey = false;
    }

    public void abortSetKey () {
      _parent.expectingConfigHotkey = false;
    }
  }

  #endregion

  #region Hotkey Set Vars

  private bool expectingConfigHotkey = false;
  PreciseManeuverConfig.HotkeyType expectedHotkey;
  private double expectedTime = 0.0;
  private const float expectedTimeout = 5.0f;
  private int expectedHotkeyCooldown = 0;
  private Action<KeyCode> onSetCallback;

  #endregion

  #region Hotkey Repeat Vars

  private bool repeatButtonPressed = false;
  private KeyCode repeatButtonPressedCode = KeyCode.None;
  private int repeatButtonPressInterval = 0;
  private int repeatButtonReleaseInterval = 0;
  private bool repeatButtonDelay (KeyCode code) {
    if (repeatButtonPressed)
      return false;
    if (code != repeatButtonPressedCode) {
      repeatButtonPressedCode = code;
      repeatButtonPressInterval = 0;
    }
    repeatButtonPressed = true;
    nodeManager.beginAtomicChange ();
    if (repeatButtonPressInterval > 20 || repeatButtonPressInterval == 0)
      return true;
    else
      return false;
  }

  #endregion

  #region GUI Construct

  private GameObject m_KeybindingsCtrlPrefab = PreciseManeuverConfig.Instance.prefabs.LoadAsset<GameObject> ("PreciseManeuverKeyControl");

  internal void fillKeymapperWindow (UI.DraggableWindow window) {
    newKeyControl (window, "Hide/show window", PreciseManeuverConfig.HotkeyType.HIDEWIN);
    newKeyControl (window, "Increment prograde", PreciseManeuverConfig.HotkeyType.PROGINC);
    newKeyControl (window, "Decrement prograde", PreciseManeuverConfig.HotkeyType.PROGDEC);
    newKeyControl (window, "Zero prograde", PreciseManeuverConfig.HotkeyType.PROGZER);
    newKeyControl (window, "Increment normal", PreciseManeuverConfig.HotkeyType.NORMINC);
    newKeyControl (window, "Decrement normal", PreciseManeuverConfig.HotkeyType.NORMDEC);
    newKeyControl (window, "Zero normal", PreciseManeuverConfig.HotkeyType.NORMZER);
    newKeyControl (window, "Increment radial", PreciseManeuverConfig.HotkeyType.RADIINC);
    newKeyControl (window, "Decrement radial", PreciseManeuverConfig.HotkeyType.RADIDEC);
    newKeyControl (window, "Zero radial", PreciseManeuverConfig.HotkeyType.RADIZER);
    newKeyControl (window, "Increment time", PreciseManeuverConfig.HotkeyType.TIMEINC);
    newKeyControl (window, "Decrement time", PreciseManeuverConfig.HotkeyType.TIMEDEC);
    newKeyControl (window, "Circularize orbit", PreciseManeuverConfig.HotkeyType.CIRCORB);
    newKeyControl (window, "Turn orbit up", PreciseManeuverConfig.HotkeyType.TURNOUP);
    newKeyControl (window, "Turn orbit down", PreciseManeuverConfig.HotkeyType.TURNODN);
    newKeyControl (window, "Change increment (w/Alt)", PreciseManeuverConfig.HotkeyType.PAGEINC);
    newKeyControl (window, "Change orbit mode (w/Alt)", PreciseManeuverConfig.HotkeyType.PAGECON);
    newKeyControl (window, "Focus on next encounter", PreciseManeuverConfig.HotkeyType.FOCNENC);
    newKeyControl (window, "Focus on vessel", PreciseManeuverConfig.HotkeyType.FOCVESL);
    newKeyControl (window, "Show more trajectories", PreciseManeuverConfig.HotkeyType.PLUSORB);
    newKeyControl (window, "Show fewer trajectories", PreciseManeuverConfig.HotkeyType.MINUORB);
    newKeyControl (window, "Toggle x10 time", PreciseManeuverConfig.HotkeyType.PAGEX10);
    newKeyControl (window, "Next maneuver", PreciseManeuverConfig.HotkeyType.NEXTMAN);
    newKeyControl (window, "Prev maneuver", PreciseManeuverConfig.HotkeyType.PREVMAN);
    newKeyControl (window, "Delete maneuver", PreciseManeuverConfig.HotkeyType.MNVRDEL);
  }

  private void newKeyControl (DraggableWindow window, string title, PreciseManeuverConfig.HotkeyType type) {
    GameObject keybindingsCtrlObject = UnityEngine.Object.Instantiate(m_KeybindingsCtrlPrefab);
    if (keybindingsCtrlObject == null)
      return;
    StyleManager.Process (keybindingsCtrlObject);

    KeybindingControl keybindingCtrl = keybindingsCtrlObject.GetComponent<KeybindingControl>();
    keybindingCtrl.setControl (new KeybindingControlInterface (this, type, title));
    window.AddToContent (keybindingsCtrlObject);

  }

  #endregion

  #region Hotkey Set

  internal void processHotkeySet () {
    if (expectingConfigHotkey && expectedTime < Planetarium.GetUniversalTime ()) {
      expectingConfigHotkey = false;
      onSetCallback (config.getHotkey (expectedHotkey));
    }
    if (Input.anyKey && GUIUtility.keyboardControl == 0) {
      if (expectingConfigHotkey && Input.anyKeyDown) {
        expectingConfigHotkey = false;
        KeyCode key = NodeTools.fetchKey ();
        if (key != KeyCode.None && key != KeyCode.Escape) {
          config.setHotkey (expectedHotkey, key);
          ScreenMessages.PostScreenMessage ("Binded to '" + key.ToString () + "'", 0.5f, ScreenMessageStyle.UPPER_CENTER);
          onSetCallback (key);
          // prevent the hotkey being detected as pressed the same moment it was binded
          expectedHotkeyCooldown = 5;
        } else {
          onSetCallback (config.getHotkey (expectedHotkey));
        }
      }
    }
  }

  #endregion

  #region Global Hotkeys

  internal void processGlobalHotkeys () {
    if (Input.anyKey && GUIUtility.keyboardControl == 0 && !keyboardLocked) {
      // hide/show window
      if (Input.GetKeyDown (config.getHotkey (PreciseManeuverConfig.HotkeyType.HIDEWIN)))
        config.showMainWindow = !config.showMainWindow;
      // more patches
      if (Input.GetKeyDown (config.getHotkey (PreciseManeuverConfig.HotkeyType.PLUSORB)))
        FlightGlobals.ActiveVessel.patchedConicSolver.IncreasePatchLimit ();
      // less patches
      if (Input.GetKeyDown (config.getHotkey (PreciseManeuverConfig.HotkeyType.MINUORB)))
        FlightGlobals.ActiveVessel.patchedConicSolver.DecreasePatchLimit ();
      // Page Conics
      if (Input.GetKeyDown (config.getHotkey (PreciseManeuverConfig.HotkeyType.PAGECON))) {
        int mode = config.conicsMode;
        if (Event.current.alt)
          mode = (mode == 0) ? 4 : mode - 1;
        else
          mode = (mode == 4) ? 0 : mode + 1;
        config.conicsMode = mode;
      }
    }
  }

  #endregion

  #region Process Hotkeys

  internal void processRegularHotkeys () {
    repeatButtonPressed = false;

    if (expectedHotkeyCooldown > 0) {
      expectedHotkeyCooldown--;
      return;
    }

    if (Input.anyKey && GUIUtility.keyboardControl == 0 && !keyboardLocked) {

      ManeuverNode node = nodeManager.currentNode;

      // process normal keyboard input
      double dvx = 0;
      double dvy = 0;
      double dvz = 0;
      double dut = 0;
      bool changed = false;
      // prograde increment
      if (Input.GetKey (config.getHotkey (PreciseManeuverConfig.HotkeyType.PROGINC)) &&
          repeatButtonDelay (config.getHotkey (PreciseManeuverConfig.HotkeyType.PROGINC))) {
        dvz += config.increment;
        changed = true;
      }
      // prograde decrement
      if (Input.GetKey (config.getHotkey (PreciseManeuverConfig.HotkeyType.PROGDEC)) &&
          repeatButtonDelay (config.getHotkey (PreciseManeuverConfig.HotkeyType.PROGDEC))) {
        dvz -= config.increment;
        changed = true;
      }
      // prograde zero
      if (Input.GetKeyDown (config.getHotkey (PreciseManeuverConfig.HotkeyType.PROGZER))) {
        nodeManager.changeNodeDVMult (1, 1, 0);
      }
      // normal increment
      if (Input.GetKey (config.getHotkey (PreciseManeuverConfig.HotkeyType.NORMINC)) &&
          repeatButtonDelay (config.getHotkey (PreciseManeuverConfig.HotkeyType.NORMINC))) {
        dvy += config.increment;
        changed = true;
      }
      // normal decrement
      if (Input.GetKey (config.getHotkey (PreciseManeuverConfig.HotkeyType.NORMDEC)) &&
          repeatButtonDelay (config.getHotkey (PreciseManeuverConfig.HotkeyType.NORMDEC))) {
        dvy -= config.increment;
        changed = true;
      }
      // normal zero
      if (Input.GetKeyDown (config.getHotkey (PreciseManeuverConfig.HotkeyType.NORMZER))) {
        nodeManager.changeNodeDVMult (1, 0, 1);
      }
      // radial increment
      if (Input.GetKey (config.getHotkey (PreciseManeuverConfig.HotkeyType.RADIINC)) &&
          repeatButtonDelay (config.getHotkey (PreciseManeuverConfig.HotkeyType.RADIINC))) {
        dvx += config.increment;
        changed = true;
      }
      // radial decrement
      if (Input.GetKey (config.getHotkey (PreciseManeuverConfig.HotkeyType.RADIDEC)) &&
          repeatButtonDelay (config.getHotkey (PreciseManeuverConfig.HotkeyType.RADIDEC))) {
        dvx -= config.increment;
        changed = true;
      }
      // radial zero
      if (Input.GetKeyDown (config.getHotkey (PreciseManeuverConfig.HotkeyType.RADIZER))) {
        nodeManager.changeNodeDVMult (0, 1, 1);
      }
      // UT increment
      if (Input.GetKey (config.getHotkey (PreciseManeuverConfig.HotkeyType.TIMEINC)) &&
          repeatButtonDelay (config.getHotkey (PreciseManeuverConfig.HotkeyType.TIMEINC))) {
        dut += config.incrementUt;
        changed = true;
      }
      // UT decrement
      if (Input.GetKey (config.getHotkey (PreciseManeuverConfig.HotkeyType.TIMEDEC)) &&
          repeatButtonDelay (config.getHotkey (PreciseManeuverConfig.HotkeyType.TIMEDEC))) {
        dut -= config.incrementUt;
        changed = true;
      }
      if (changed)
        nodeManager.changeNodeDiff (dvx, dvy, dvz, dut);

      // change increment
      if (Input.GetKeyDown (config.getHotkey (PreciseManeuverConfig.HotkeyType.PAGEINC))) {
        if (Event.current.alt)
          config.setIncrementUp ();
        else
          config.setIncrementDown ();
      }
      // toggle x10
      if (Input.GetKeyDown (config.getHotkey (PreciseManeuverConfig.HotkeyType.PAGEX10)))
        config.x10UTincrement = !config.x10UTincrement;
      // next node
      if (Input.GetKeyDown (config.getHotkey (PreciseManeuverConfig.HotkeyType.NEXTMAN)))
        nodeManager.switchNextNode ();
      // prev node
      if (Input.GetKeyDown (config.getHotkey (PreciseManeuverConfig.HotkeyType.PREVMAN)))
        nodeManager.switchPreviousNode ();
      // delete node
      if (Input.GetKeyDown (config.getHotkey (PreciseManeuverConfig.HotkeyType.MNVRDEL)))
        nodeManager.deleteNode ();
      // turn orbit up
      if (Input.GetKey (config.getHotkey (PreciseManeuverConfig.HotkeyType.TURNOUP)))
        if (repeatButtonDelay (config.getHotkey (PreciseManeuverConfig.HotkeyType.TURNOUP)))
          nodeManager.turnOrbitUp ();
      // turn orbit down
      if (Input.GetKey (config.getHotkey (PreciseManeuverConfig.HotkeyType.TURNODN)))
        if (repeatButtonDelay (config.getHotkey (PreciseManeuverConfig.HotkeyType.TURNODN)))
          nodeManager.turnOrbitDown ();
      // circularize orbit
      if (Input.GetKeyDown (config.getHotkey (PreciseManeuverConfig.HotkeyType.CIRCORB)))
        nodeManager.circularizeOrbit ();
      // focus on target
      if (Input.GetKeyDown (config.getHotkey (PreciseManeuverConfig.HotkeyType.FOCNENC))) {
        var nextEnc = NodeTools.findNextEncounter ();
        if (nextEnc != null) {
          MapObject mapObject = PlanetariumCamera.fetch.targets.Find (o => (o.celestialBody != null) && (o.celestialBody == nextEnc));
          MapView.MapCamera.SetTarget (mapObject);
        }
      }
      // focus on vessel
      if (Input.GetKeyDown (config.getHotkey (PreciseManeuverConfig.HotkeyType.FOCVESL))) {
        MapObject mapObject = PlanetariumCamera.fetch.targets.Find (o => (o.vessel != null) && o.vessel.Equals (FlightGlobals.ActiveVessel));
        MapView.MapCamera.SetTarget (mapObject);
      }
    }
    if (repeatButtonPressed) {
      if (repeatButtonPressInterval < 50)
        repeatButtonPressInterval++;
      repeatButtonReleaseInterval = 0;
    } else {
      if (repeatButtonReleaseInterval < 2) {
        repeatButtonReleaseInterval++;
      } else {
        if (repeatButtonReleaseInterval < 100) {
          nodeManager.endAtomicChange ();
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
