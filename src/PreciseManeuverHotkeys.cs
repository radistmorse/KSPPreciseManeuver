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
    drawKeyControls ("Increment normal",   PreciseManeuverConfig.HotkeyType.NORMINC);
    drawKeyControls ("Decrement normal",   PreciseManeuverConfig.HotkeyType.NORMDEC);
    drawKeyControls ("Increment radial",   PreciseManeuverConfig.HotkeyType.RADIINC);
    drawKeyControls ("Decrement radial",   PreciseManeuverConfig.HotkeyType.RADIDEC);
    drawKeyControls ("Increment time",     PreciseManeuverConfig.HotkeyType.TIMEINC);
    drawKeyControls ("Decrement time",     PreciseManeuverConfig.HotkeyType.TIMEDEC);
    drawKeyControls ("Change increment forward\n(+Alt to change backward)",
                                            PreciseManeuverConfig.HotkeyType.PAGEINC);
    drawKeyControls ("Change orbit mode forward\n(+Alt to change backward)",
                                            PreciseManeuverConfig.HotkeyType.PAGECON);

    GUILayout.EndVertical ();
    GUI.DragWindow ();
  }

  private void drawKeyControls (string title, PreciseManeuverConfig.HotkeyType type) {
    GUILayout.BeginHorizontal ();
    GUILayout.Label (title + ": " + config.getHotkey (type).ToString (), GUILayout.Width (300));
    if (GUILayout.Button ("Set")) {
      ScreenMessages.PostScreenMessage ("Press a key to bind '" + title.ToLower () + "'...", expectedTimeout, ScreenMessageStyle.UPPER_CENTER);
      expectedHotkey = type;
      expectingConfigHotkey = true;
      expectedTime = Planetarium.GetUniversalTime () + expectedTimeout;
    }
    GUILayout.EndHorizontal ();
  }

  /// <summary>
  /// Processes keyboard input.
  /// </summary>
  internal void processKeyInput (int nodeIdx) {
    repeatButtonPressed = false;
    if (Input.anyKey && GUIUtility.keyboardControl == 0) {

      // process any key input for settings
      if (expectingConfigHotkey && Input.anyKeyDown) {
        expectingConfigHotkey = false;
        if (expectedTime > Planetarium.GetUniversalTime ()) {
          KeyCode key = NodeTools.fetchKey ();
          if (key != KeyCode.None) {
            config.setHotkey (expectedHotkey, key);
            ScreenMessages.PostScreenMessage ("Binded to '" + key.ToString () + "'", 0.5f, ScreenMessageStyle.UPPER_CENTER);
          }
          return;
        }
      }

      // hide/show window
      if (Input.GetKeyDown (config.getHotkey (PreciseManeuverConfig.HotkeyType.HIDEWIN))) {
        config.showMainWindow = !config.showMainWindow;
      }

      if (!config.showMainWindow || nodeIdx == -1)
        return;

      ManeuverNode node = FlightGlobals.ActiveVessel.patchedConicSolver.maneuverNodes[nodeIdx];

      // process normal keyboard input
      // prograde increment
      if (Input.GetKey (config.getHotkey (PreciseManeuverConfig.HotkeyType.PROGINC))) {
        if (repeatButtonDelay (config.getHotkey (PreciseManeuverConfig.HotkeyType.PROGINC)))
          nodeManager.changeNode (node,
                                  node.DeltaV.x,
                                  node.DeltaV.y,
                                  node.DeltaV.z + config.increment,
                                  node.UT);
      }
      // prograde decrement
      if (Input.GetKey (config.getHotkey (PreciseManeuverConfig.HotkeyType.PROGDEC))) {
        if (repeatButtonDelay (config.getHotkey (PreciseManeuverConfig.HotkeyType.PROGDEC)))
          nodeManager.changeNode (node,
                                  node.DeltaV.x,
                                  node.DeltaV.y,
                                  node.DeltaV.z - config.increment,
                                  node.UT);
      }
      // normal increment
      if (Input.GetKey (config.getHotkey (PreciseManeuverConfig.HotkeyType.NORMINC))) {
        if (repeatButtonDelay (config.getHotkey (PreciseManeuverConfig.HotkeyType.NORMINC)))
          nodeManager.changeNode (node,
                                  node.DeltaV.x,
                                  node.DeltaV.y + config.increment,
                                  node.DeltaV.z,
                                  node.UT);
      }
      // normal decrement
      if (Input.GetKey (config.getHotkey (PreciseManeuverConfig.HotkeyType.NORMDEC))) {
        if (repeatButtonDelay (config.getHotkey (PreciseManeuverConfig.HotkeyType.NORMDEC)))
          nodeManager.changeNode (node,
                                  node.DeltaV.x,
                                  node.DeltaV.y - config.increment,
                                  node.DeltaV.z,
                                  node.UT);
      }
      // radial increment
      if (Input.GetKey (config.getHotkey (PreciseManeuverConfig.HotkeyType.RADIINC))) {
        if (repeatButtonDelay (config.getHotkey (PreciseManeuverConfig.HotkeyType.RADIINC)))
          nodeManager.changeNode (node,
                                  node.DeltaV.x + config.increment,
                                  node.DeltaV.y,
                                  node.DeltaV.z,
                                  node.UT);
      }
      // radial decrement
      if (Input.GetKey (config.getHotkey (PreciseManeuverConfig.HotkeyType.RADIDEC))) {
        if (repeatButtonDelay (config.getHotkey (PreciseManeuverConfig.HotkeyType.RADIDEC)))
          nodeManager.changeNode (node,
                                  node.DeltaV.x - config.increment,
                                  node.DeltaV.y,
                                  node.DeltaV.z,
                                  node.UT);
      }
      // UT increment
      if (Input.GetKey (config.getHotkey (PreciseManeuverConfig.HotkeyType.TIMEINC))) {
        if (repeatButtonDelay (config.getHotkey (PreciseManeuverConfig.HotkeyType.TIMEINC)))
          nodeManager.changeNode (node,
                                  node.DeltaV.x,
                                  node.DeltaV.y,
                                  node.DeltaV.z,
                                  node.UT + config.increment * (config.x10UTincrement ? 10 : 1));
      }
      // UT decrement
      if (Input.GetKey (config.getHotkey (PreciseManeuverConfig.HotkeyType.TIMEDEC))) {
        if (repeatButtonDelay (config.getHotkey (PreciseManeuverConfig.HotkeyType.TIMEDEC)))
          nodeManager.changeNode (node,
                                  node.DeltaV.x,
                                  node.DeltaV.y,
                                  node.DeltaV.z,
                                  node.UT - config.increment * (config.x10UTincrement ? 10 : 1));
      }
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
