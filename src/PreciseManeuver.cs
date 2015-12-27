using UnityEngine;

/******************************************************************************
 * Copyright (c) 2013-2014, Justin Bengtson
 * Copyright (c) 2014-2015, Maik Schreiber
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
[KSPAddon (KSPAddon.Startup.Flight, false)]
internal class PreciseManeuver : MonoBehaviour {

    private MainWindow mainWindow = new MainWindow ();
    private PreciseManeuverHotkeys hotkeys = new PreciseManeuverHotkeys ();

    private PreciseManeuverConfig config = PreciseManeuverConfig.getInstance ();
    private NodeManager manager = NodeManager.getInstance ();

    private readonly int mainWindowId = 841358683;
    private readonly int keymapperWindowId = 546312356;

    /// <summary>
    /// Overridden function from MonoBehavior
    /// </summary>
    internal void Start () {
        config.loadConfig ();
        manager.init ();
    }

    /// <summary>
    /// Overridden function from MonoBehavior
    /// </summary>
    internal void OnDisable () {
        config.saveConfig ();
    }

    /// <summary>
    /// Overridden function from MonoBehavior
    /// </summary>
    internal void Update () {
        if (!patchedConicsUnlocked ())
            return;

        if (!FlightDriver.Pause && canShowNodeEditor) {
            hotkeys.processGlobalHotkeys ();
            if (config.showMainWindow) {
                hotkeys.processKeyInput (mainWindow.currentNodeIdx);
                manager.updateNodes ();
                mainWindow.updateValues ();
            }
        }
    }

    /// <summary>
    /// Overridden function from MonoBehavior
    /// </summary>
    internal void OnGUI () {
        if (!patchedConicsUnlocked ())
            return;

        if (config.showMainWindow && canShowNodeEditor) {
            config.mainWindowPos = GUILayout.Window (mainWindowId, config.mainWindowPos, (id) => mainWindow.draw (),
                                                     "Precise Maneuver", GUILayout.ExpandHeight (true));
            if (config.showKeymapperWindow) {
                config.keymapperWindowPos = GUILayout.Window (keymapperWindowId, config.keymapperWindowPos, (id) => hotkeys.drawKeymapperWindow (),
                                                              "Precise Maneuver Hotkeys", GUILayout.ExpandHeight (true));
            }
        }
    }

    private bool patchedConicsUnlocked () {
        return GameVariables.Instance.GetOrbitDisplayMode
                 (ScenarioUpgradeableFacilities.GetFacilityLevel (SpaceCenterFacility.TrackingStation)) == GameVariables.OrbitDisplayMode.PatchedConics;
    }

    /// <summary>
    /// Returns whether the Node Editor can be shown based on a number of global factors.
    /// </summary>
    /// <value><c>true</c> if the Node Editor can be shown; otherwise, <c>false</c>.</value>
    private bool canShowNodeEditor {
        get {
            PatchedConicSolver solver = FlightGlobals.ActiveVessel.patchedConicSolver;
            return (FlightGlobals.ActiveVessel != null) && MapView.MapIsEnabled && (solver != null) && (solver.maneuverNodes.Count > 0);
        }
    }
}
}
