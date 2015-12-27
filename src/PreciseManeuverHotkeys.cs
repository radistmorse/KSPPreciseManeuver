using UnityEngine;

/******************************************************************************
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
internal class PreciseManeuverHotkeys {

  PreciseManeuverConfig config = PreciseManeuverConfig.getInstance ();
  NodeManager nodeManager = NodeManager.getInstance ();

  /* hotkey setting */
  private bool expectingConfigHotkey = false;
  PreciseManeuverConfig.HotkeyType expectedHotkey;
  private double expectedTime = 0.0;
  private const float expectedTimeout = 5.0f;

  /* hotkey repeat delay */
  private bool repeatButtonPressed = false;
  private KeyCode repeatButtonPressedCode = KeyCode.None;
  private int repeatButtonPressInterval = 0;
  private int repeatButtonReleaseInterval = 0;
  private bool repeatButtonDelay (KeyCode code) {
    if (code != repeatButtonPressedCode) {
      repeatButtonPressedCode = code;
      repeatButtonPressInterval = 0;
    }
    repeatButtonPressed = true;
    if (repeatButtonPressInterval > 20 || repeatButtonPressInterval == 0)
      return true;
    else
      return false;
  }

  /// <summary>
  /// Draws the Keymapper window.
  /// </summary>
  internal void drawKeymapperWindow () {

    GUI.skin = config.skin;

    if (GUI.Button (new Rect (config.keymapperWindowPos.width - 24, 2, 22, 18), "X"))
      config.showKeymapperWindow = false;

    GUILayout.BeginVertical ();
    drawKeyControls ("Hide/show window",   PreciseManeuverConfig.HotkeyType.HIDEWIN);
    drawKeyControls ("Increment prograde", PreciseManeuverConfig.HotkeyType.PROGINC);
    drawKeyControls ("Decrement prograde", PreciseManeuverConfig.HotkeyType.PROGDEC);
    drawKeyControls ("Zero prograde",      PreciseManeuverConfig.HotkeyType.PROGZER);
    drawKeyControls ("Increment normal",   PreciseManeuverConfig.HotkeyType.NORMINC);
    drawKeyControls ("Decrement normal",   PreciseManeuverConfig.HotkeyType.NORMDEC);
    drawKeyControls ("Zero normal",        PreciseManeuverConfig.HotkeyType.NORMZER);
    drawKeyControls ("Increment radial",   PreciseManeuverConfig.HotkeyType.RADIINC);
    drawKeyControls ("Decrement radial",   PreciseManeuverConfig.HotkeyType.RADIDEC);
    drawKeyControls ("Zero radial",        PreciseManeuverConfig.HotkeyType.RADIZER);
    drawKeyControls ("Increment time",     PreciseManeuverConfig.HotkeyType.TIMEINC);
    drawKeyControls ("Decrement time",     PreciseManeuverConfig.HotkeyType.TIMEDEC);
    drawKeyControls ("Circularize orbit",  PreciseManeuverConfig.HotkeyType.CIRCORB);
    drawKeyControls ("Turn orbit up",      PreciseManeuverConfig.HotkeyType.TURNOUP);
    drawKeyControls ("Turn orbit down",    PreciseManeuverConfig.HotkeyType.TURNODN);
    drawKeyControls ("Change increment forward\n(+Alt to change backward)",
                                           PreciseManeuverConfig.HotkeyType.PAGEINC);
    drawKeyControls ("Change orbit mode forward\n(+Alt to change backward)",
                                           PreciseManeuverConfig.HotkeyType.PAGECON);
    drawKeyControls ("Show orbit info",    PreciseManeuverConfig.HotkeyType.SHOWORB);
    drawKeyControls ("Show ejection info", PreciseManeuverConfig.HotkeyType.SHOWEJC);
    drawKeyControls ("Focus on target",    PreciseManeuverConfig.HotkeyType.FOCTARG);
    drawKeyControls ("Focus on vessel",    PreciseManeuverConfig.HotkeyType.FOCVESL);
    drawKeyControls ("Show more orbits",   PreciseManeuverConfig.HotkeyType.PLUSORB);
    drawKeyControls ("Show less orbits",   PreciseManeuverConfig.HotkeyType.MINUORB);
    drawKeyControls ("Toggle x10 time",    PreciseManeuverConfig.HotkeyType.PAGEX10);
    drawKeyControls ("Next maneuver",      PreciseManeuverConfig.HotkeyType.NEXTMAN);
    drawKeyControls ("Prev maneuver",      PreciseManeuverConfig.HotkeyType.PREVMAN);
    drawKeyControls ("Delete maneuver",    PreciseManeuverConfig.HotkeyType.MNVRDEL);
    GUILayout.EndVertical ();
    GUI.DragWindow ();
  }

  private void drawKeyControls (string title, PreciseManeuverConfig.HotkeyType type) {
    GUILayout.BeginHorizontal ();
    GUILayout.BeginVertical ();
    GUILayout.FlexibleSpace ();
    GUILayout.BeginHorizontal ();
    if (GUILayout.Button ("Set")) {
      config.setHotkey (type, KeyCode.None);
      ScreenMessages.PostScreenMessage ("Press a key to bind '" + title.ToLower () + "'...", expectedTimeout, ScreenMessageStyle.UPPER_CENTER);
      expectedHotkey = type;
      expectingConfigHotkey = true;
      expectedTime = Planetarium.GetUniversalTime () + expectedTimeout;
    }
    if (GUILayout.Button ("Unset")) {
      config.setHotkey (type, KeyCode.None);
      expectingConfigHotkey = false;
    }
    GUILayout.EndHorizontal ();
    GUILayout.FlexibleSpace ();
    GUILayout.EndVertical ();
    GUILayout.Label (title + ": " + config.getHotkey (type).ToString (), GUILayout.Width (300));
    GUILayout.EndHorizontal ();
  }

  internal void processGlobalHotkeys () {
    if (Input.anyKey && GUIUtility.keyboardControl == 0) {
      // hide/show window
      if (Input.GetKeyDown (config.getHotkey (PreciseManeuverConfig.HotkeyType.HIDEWIN))) {
        config.showMainWindow = !config.showMainWindow;
      }
    }
  }

  internal void processKeyInput (int nodeIdx) {
    repeatButtonPressed = false;
    if (Input.anyKey && GUIUtility.keyboardControl == 0) {

      // process any key input for settings
      if (expectingConfigHotkey && Input.anyKeyDown) {
        expectingConfigHotkey = false;
        if (expectedTime > Planetarium.GetUniversalTime ()) {
          KeyCode key = NodeTools.fetchKey ();
          if (key != KeyCode.None && key != KeyCode.Escape) {
            config.setHotkey (expectedHotkey, key);
            ScreenMessages.PostScreenMessage ("Binded to '" + key.ToString () + "'", 0.5f, ScreenMessageStyle.UPPER_CENTER);
            // prevent the hotkey being detected as pressed the next frame after it is set
            repeatButtonDelay (key);
          }
          return;
        }
      }

      if (nodeIdx == -1)
        return;

      ManeuverNode node = FlightGlobals.ActiveVessel.patchedConicSolver.maneuverNodes[nodeIdx];

      // process normal keyboard input
      double dvx = node.DeltaV.x;
      double dvy = node.DeltaV.y;
      double dvz = node.DeltaV.z;
      double ut = node.UT;
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
        dvz = 0;
        changed = true;
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
        dvy = 0;
        changed = true;
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
        dvx = 0;
        changed = true;
      }
      // UT increment
      if (Input.GetKey (config.getHotkey (PreciseManeuverConfig.HotkeyType.TIMEINC)) &&
          repeatButtonDelay (config.getHotkey (PreciseManeuverConfig.HotkeyType.TIMEINC))) {
        ut += config.increment * (config.x10UTincrement ? 10 : 1);
        changed = true;
      }
      // UT decrement
      if (Input.GetKey (config.getHotkey (PreciseManeuverConfig.HotkeyType.TIMEDEC)) &&
          repeatButtonDelay (config.getHotkey (PreciseManeuverConfig.HotkeyType.TIMEDEC))) {
        ut -= config.increment * (config.x10UTincrement ? 10 : 1);
        changed = true;
      }
      if (changed)
        nodeManager.changeNode (node, dvx, dvy, dvz, ut);

      // Page Conics
      if (Input.GetKeyDown (config.getHotkey (PreciseManeuverConfig.HotkeyType.PAGECON))) {
        int mode = NodeTools.getConicsMode();
        if (Event.current.alt)
          mode = (mode == 0) ? 4 : mode - 1;
        else
          mode = (mode == 4) ? 0 : mode + 1;
        NodeTools.setConicsMode (mode);
      }
      // change increment
      if (Input.GetKeyDown (config.getHotkey (PreciseManeuverConfig.HotkeyType.PAGEINC))) {
        if (Event.current.alt)
          config.setIncrementDown();
        else
          config.setIncrementUp();
      }
      // toggle x10
      if (Input.GetKeyDown (config.getHotkey (PreciseManeuverConfig.HotkeyType.PAGEX10)))
        config.x10UTincrement = !config.x10UTincrement;
      // more patches
      if (Input.GetKeyDown (config.getHotkey (PreciseManeuverConfig.HotkeyType.PLUSORB)))
        FlightGlobals.ActiveVessel.patchedConicSolver.IncreasePatchLimit ();
      // less patches
      if (Input.GetKeyDown (config.getHotkey (PreciseManeuverConfig.HotkeyType.MINUORB)))
        FlightGlobals.ActiveVessel.patchedConicSolver.DecreasePatchLimit ();

      // all the rest, just pass it to the config, for main window to take
      if (Input.GetKey (config.getHotkey (PreciseManeuverConfig.HotkeyType.TURNOUP)))
        if (repeatButtonDelay (config.getHotkey (PreciseManeuverConfig.HotkeyType.TURNOUP)))
          config.registerHotkeyPress(PreciseManeuverConfig.HotkeyType.TURNOUP);
      if (Input.GetKey (config.getHotkey (PreciseManeuverConfig.HotkeyType.TURNODN)))
        if (repeatButtonDelay (config.getHotkey (PreciseManeuverConfig.HotkeyType.TURNODN)))
          config.registerHotkeyPress(PreciseManeuverConfig.HotkeyType.TURNODN);
      if (Input.GetKeyDown (config.getHotkey (PreciseManeuverConfig.HotkeyType.CIRCORB)))
        config.registerHotkeyPress(PreciseManeuverConfig.HotkeyType.CIRCORB);


      if (Input.GetKeyDown (config.getHotkey (PreciseManeuverConfig.HotkeyType.SHOWORB)))
        config.registerHotkeyPress(PreciseManeuverConfig.HotkeyType.SHOWORB);
      if (Input.GetKeyDown (config.getHotkey (PreciseManeuverConfig.HotkeyType.SHOWEJC)))
        config.registerHotkeyPress(PreciseManeuverConfig.HotkeyType.SHOWEJC);
      if (Input.GetKeyDown (config.getHotkey (PreciseManeuverConfig.HotkeyType.FOCTARG)))
        config.registerHotkeyPress(PreciseManeuverConfig.HotkeyType.FOCTARG);
      if (Input.GetKeyDown (config.getHotkey (PreciseManeuverConfig.HotkeyType.FOCVESL)))
        config.registerHotkeyPress(PreciseManeuverConfig.HotkeyType.FOCVESL);
      if (Input.GetKeyDown (config.getHotkey (PreciseManeuverConfig.HotkeyType.NEXTMAN)))
        config.registerHotkeyPress(PreciseManeuverConfig.HotkeyType.NEXTMAN);
      if (Input.GetKeyDown (config.getHotkey (PreciseManeuverConfig.HotkeyType.PREVMAN)))
        config.registerHotkeyPress(PreciseManeuverConfig.HotkeyType.PREVMAN);
      if (Input.GetKeyDown (config.getHotkey (PreciseManeuverConfig.HotkeyType.MNVRDEL)))
        config.registerHotkeyPress(PreciseManeuverConfig.HotkeyType.MNVRDEL);
    }
    if (repeatButtonPressed) {
      if (repeatButtonPressInterval < 50)
        repeatButtonPressInterval++;
      repeatButtonReleaseInterval = 0;
    } else {
      if (repeatButtonReleaseInterval < 2) {
        repeatButtonReleaseInterval++;
      } else {
        repeatButtonPressInterval = 0;
        repeatButtonPressedCode = KeyCode.None;
      }
    }
  }
}
}
