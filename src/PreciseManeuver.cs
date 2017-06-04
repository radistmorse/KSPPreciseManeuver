/******************************************************************************
 * Copyright (c) 2013-2014, Justin Bengtson
 * Copyright (c) 2014-2015, Maik Schreiber
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
using KSP.Localization;

namespace KSPPreciseManeuver {
using UI;
[KSPAddon(KSPAddon.Startup.Flight, false)]
internal class PreciseManeuver : MonoBehaviour {

  private MainWindow mainWindow = new MainWindow();
  private PreciseManeuverHotkeys hotkeys = new PreciseManeuverHotkeys();

  private PreciseManeuverConfig config = PreciseManeuverConfig.Instance;
  private NodeManager manager = NodeManager.Instance;

  private DraggableWindow m_KeybindingsWindow = null;
  private GameObject      m_KeybindingsWindowObject = null;

  private DraggableWindow m_MainWindow = null;
  private GameObject      m_MainWindowObject = null;

  private GameObject m_WindowPrefab = PreciseManeuverConfig.Instance.prefabs.LoadAsset<GameObject> ("PreciseManeuverWindow");

  private int waitForGizmo = 0;

  internal void Start () {
    KACWrapper.InitKACWrapper ();
  }

  internal void OnDisable() {
    closeKeybindingsWindow();
    closeMainWindow ();
    config.saveConfig ();
  }

  internal void Update() {
    if (!NodeTools.patchedConicsUnlocked)
      return;

    if (config.showKeymapperWindow && config.uiActive)
      openKeybindingsWindow ();
    else
      closeKeybindingsWindow ();

    if (config.showMainWindow && config.uiActive && canShowNodeEditor)
      openMainWindow ();
    else
      closeMainWindow ();

    if (m_KeybindingsWindowObject != null)
      hotkeys.processHotkeySet ();

    if (!FlightDriver.Pause && canShowNodeEditor) {
      hotkeys.processGlobalHotkeys();
      if (m_MainWindowObject != null) {
        hotkeys.processRegularHotkeys();

        if (Input.GetMouseButtonUp (0))
          waitForGizmo = 3;
        if (waitForGizmo > 0) {
          if (waitForGizmo == 1)
            manager.searchNewGizmo ();
          waitForGizmo--;
        }
        manager.updateNodes ();
      }
    }

    if (m_MainWindowObject == null) {
      manager.clear ();
    }
  }

  #region MainWindow

  private void openMainWindow () {
    // fade in if already open
    if (m_MainWindow != null) {
      m_MainWindow.MoveToBackground (config.isInBackground);
      if (m_MainWindow.IsFadingOut)
        m_MainWindow.fadeIn ();
      if (config.modulesChanged)
        mainWindow.updateMainWindow (m_MainWindow);
      return;
    }

    if (m_WindowPrefab == null || m_MainWindowObject != null)
      return;

    // create object
    Vector3 pos = new Vector3(config.mainWindowPos.x, config.mainWindowPos.y, MainCanvasUtil.MainCanvasRect.position.z);
    m_MainWindowObject = Instantiate (m_WindowPrefab, pos, Quaternion.identity) as GameObject;
    if (m_MainWindowObject == null)
      return;

    m_MainWindow = m_MainWindowObject.GetComponent<DraggableWindow> ();
    if (m_MainWindow != null) {
      m_MainWindow.SetTitle (Localizer.Format("precisemaneuver_caption"));
      m_MainWindow.setMainCanvasTransform (MainCanvasUtil.MainCanvasRect);
      mainWindow.clearMainWindow ();
      mainWindow.updateMainWindow (m_MainWindow);
      m_MainWindow.MoveToBackground (config.isInBackground);
      m_MainWindow.OnWindowPointerEnter = setWindow1InputLock;
      m_MainWindow.OnWindowPointerExit = resetWindow1InputLock;
    }
    scaleMainWindow ();
    config.listenToScaleChange (scaleMainWindow);

    GUIComponentManager.processStyle (m_MainWindowObject);
    GUIComponentManager.replaceLabelsWithTMPro (m_MainWindowObject);

    // set object as a child of the main canvas
    m_MainWindowObject.transform.SetParent (MainCanvasUtil.MainCanvas.transform);
  }

  private void scaleMainWindow () {
    if (m_MainWindowObject == null)
      return;
    m_MainWindowObject.GetComponent<RectTransform> ().localScale = Vector3.one * config.guiScale;
  }

  private void closeMainWindow () {
    if (m_MainWindow != null) {
      if (!m_MainWindow.IsFadingOut) {
        config.mainWindowPos = m_MainWindow.RectTransform.position;
        m_MainWindow.fadeClose ();
        config.removeListener (scaleMainWindow);
        resetWindow1InputLock ();
      }
    } else if (m_MainWindowObject != null) {
      Destroy (m_MainWindowObject);
      mainWindow.clearMainWindow ();
      config.removeListener (scaleMainWindow);
      resetWindow1InputLock ();
    }
  }

  #endregion

  #region KeybindingsWindow

  private void openKeybindingsWindow() {
    // fade in if already open
    if (m_KeybindingsWindow != null) {
      if (m_KeybindingsWindow.IsFadingOut)
        m_KeybindingsWindow.fadeIn();
      return;
    }

    if (m_WindowPrefab == null || m_KeybindingsWindowObject != null)
      return;

    // create window object
    Vector3 pos = new Vector3(config.keymapperWindowPos.x, config.keymapperWindowPos.y, MainCanvasUtil.MainCanvasRect.position.z);
    m_KeybindingsWindowObject = Instantiate(m_WindowPrefab, pos, Quaternion.identity) as GameObject;
    if (m_KeybindingsWindowObject == null)
      return;

    // populate window
    m_KeybindingsWindow = m_KeybindingsWindowObject.GetComponent<DraggableWindow>();
    if (m_KeybindingsWindow != null) {
      m_KeybindingsWindow.SetTitle(Localizer.Format("precisemaneuver_keybindings_caption"));
      m_KeybindingsWindow.setMainCanvasTransform(MainCanvasUtil.MainCanvasRect);
      hotkeys.fillKeymapperWindow(m_KeybindingsWindow);
      m_KeybindingsWindow.OnWindowPointerEnter = setWindow2InputLock;
      m_KeybindingsWindow.OnWindowPointerExit = resetWindow2InputLock;
    }

    GUIComponentManager.processStyle(m_KeybindingsWindowObject);
    GUIComponentManager.processLocalization(m_KeybindingsWindowObject);
    GUIComponentManager.replaceLabelsWithTMPro(m_KeybindingsWindowObject);

    // set object as a child of the main canvas
    m_KeybindingsWindowObject.transform.SetParent(MainCanvasUtil.MainCanvas.transform);

  }

  private void closeKeybindingsWindow () {
    if (m_KeybindingsWindow != null) {
      if (!m_KeybindingsWindow.IsFadingOut) {
        config.keymapperWindowPos = m_KeybindingsWindow.RectTransform.position;
        m_KeybindingsWindow.fadeClose ();
        resetWindow2InputLock ();
      }
    } else if (m_KeybindingsWindowObject != null) {
      Destroy (m_KeybindingsWindowObject);
      resetWindow2InputLock ();
    }
  }

  #endregion

  private void setWindow1InputLock () {
    InputLockManager.SetControlLock (ControlTypes.MAP_UI, "PreciseManeuverWindow1ControlLock");
  }
  private void resetWindow1InputLock () {
    InputLockManager.RemoveControlLock ("PreciseManeuverWindow1ControlLock");
  }
  private void setWindow2InputLock () {
    InputLockManager.SetControlLock (ControlTypes.MAP_UI, "PreciseManeuverWindow2ControlLock");
  }
  private void resetWindow2InputLock () {
    InputLockManager.RemoveControlLock ("PreciseManeuverWindow2ControlLock");
  }

  private bool canShowNodeEditor {
    get {
      if (!NodeTools.patchedConicsUnlocked)
        return false;
      PatchedConicSolver solver = FlightGlobals.ActiveVessel.patchedConicSolver;
      return (FlightGlobals.ActiveVessel != null) && MapView.MapIsEnabled && (solver != null) && (solver.maneuverNodes.Count > 0);
    }
  }
}
}
