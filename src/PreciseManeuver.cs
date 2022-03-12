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
  [KSPAddon (KSPAddon.Startup.Flight, false)]
  internal class PreciseManeuver : MonoBehaviour {
    private MainWindow mainWindow = new MainWindow();
    private PreciseManeuverHotkeys hotkeys = new PreciseManeuverHotkeys();

    private PreciseManeuverConfig config = PreciseManeuverConfig.Instance;
    private NodeManager manager = NodeManager.Instance;

    private UI.DraggableWindow m_KeybindingsWindow = null;
    private GameObject         m_KeybindingsWindowObject = null;

    private UI.DraggableWindow m_MainWindow = null;
    private GameObject         m_MainWindowObject = null;

    private GameObject m_WindowPrefab = PreciseManeuverConfig.Instance.Prefabs.LoadAsset<GameObject> ("PreciseManeuverWindow");

    internal void Start () {
      GameEvents.onManeuverNodeSelected.Add (new EventVoid.OnEvent (manager.SearchNewGizmo));

      KACWrapper.InitKACWrapper ();
    }

    internal void OnDestroy() {
      GameEvents.onManeuverNodeSelected.Remove (new EventVoid.OnEvent (manager.SearchNewGizmo));
    }

    internal void OnDisable () {
      CloseKeybindingsWindow ();
      CloseMainWindow ();
      config.SaveConfig ();
    }

    internal void Update () {
      if (!NodeTools.PatchedConicsUnlocked)
        return;

      if (config.ShowKeymapperWindow && config.UiActive && MapView.MapIsEnabled)
        OpenKeybindingsWindow ();
      else
        CloseKeybindingsWindow ();

      if (config.ShowMainWindow && config.UiActive && CanShowNodeEditor)
        OpenMainWindow ();
      else
        CloseMainWindow ();

      if (m_KeybindingsWindowObject != null)
        hotkeys.ProcessHotkeySet ();

      if (!FlightDriver.Pause && CanShowNodeEditor) {
        hotkeys.ProcessGlobalHotkeys ();
        if (m_MainWindowObject != null) {
          hotkeys.ProcessRegularHotkeys ();

          manager.UpdateNodes ();
        }
      }

      if (m_MainWindowObject == null) {
        manager.Clear ();
      }
    }

    #region MainWindow

    private void OpenMainWindow () {
      // fade in if already open
      if (m_MainWindow != null) {
        m_MainWindow.MoveToBackground (config.IsInBackground);
        if (m_MainWindow.IsFadingOut)
          m_MainWindow.FadeIn ();
        if (config.ModulesChanged)
          mainWindow.UpdateMainWindow (m_MainWindow);
        if (config.TooltipsChanged)
          GUIComponentManager.EnableTooltips(m_MainWindowObject, config.IsTooltipsEnabled);
        return;
      }

      if (m_WindowPrefab == null || m_MainWindowObject != null)
        return;

      // create object
      Vector3 pos = new Vector3(config.MainWindowPos.x, config.MainWindowPos.y, MainCanvasUtil.MainCanvasRect.position.z);
      m_MainWindowObject = Instantiate (m_WindowPrefab);
      if (m_MainWindowObject == null)
        return;

      m_MainWindow = m_MainWindowObject.GetComponent<UI.DraggableWindow> ();
      if (m_MainWindow != null) {
        m_MainWindow.SetTitle (Localizer.Format ("precisemaneuver_caption"));
        m_MainWindow.SetMainCanvasTransform (MainCanvasUtil.MainCanvasRect);
        mainWindow.ClearMainWindow ();
        mainWindow.UpdateMainWindow (m_MainWindow);
        // should be done before moving to background
        GUIComponentManager.ReplaceLabelsWithTMPro (m_MainWindowObject);
        m_MainWindow.MoveToBackground (config.IsInBackground);
        m_MainWindow.setWindowInputLock = SetWindow1InputLock;
        m_MainWindow.resetWindowInputLock = ResetWindow1InputLock;
      }

      GUIComponentManager.ProcessStyle (m_MainWindowObject);
      GUIComponentManager.EnableTooltips(m_MainWindowObject, config.IsTooltipsEnabled);

      // set object as a child of the main canvas
      m_MainWindowObject.transform.SetParent (MainCanvasUtil.MainCanvas.transform);
      m_MainWindowObject.transform.position = pos;

      // do the scaling after the parent has been set
      ScaleMainWindow ();
      config.ListenToScaleChange (ScaleMainWindow);
    }

    private void ScaleMainWindow () {
      if (m_MainWindowObject == null)
        return;
      m_MainWindowObject.GetComponent<RectTransform> ().localScale = Vector3.one * config.GUIScale;
    }

    private void CloseMainWindow () {
      if (m_MainWindow != null) {
        if (!m_MainWindow.IsFadingOut) {
          config.MainWindowPos = m_MainWindow.WindowPosition;
          m_MainWindow.FadeClose ();
          config.RemoveListener (ScaleMainWindow);
          ResetWindow1InputLock ();
        }
      } else if (m_MainWindowObject != null) {
        Destroy (m_MainWindowObject);
        mainWindow.ClearMainWindow ();
        config.RemoveListener (ScaleMainWindow);
        ResetWindow1InputLock ();
      }
    }

    #endregion

    #region KeybindingsWindow

    private void OpenKeybindingsWindow () {
      // fade in if already open
      if (m_KeybindingsWindow != null) {
        if (m_KeybindingsWindow.IsFadingOut)
          m_KeybindingsWindow.FadeIn ();
        return;
      }

      if (m_WindowPrefab == null || m_KeybindingsWindowObject != null)
        return;

      // create window object
      Vector3 pos = new Vector3(config.KeymapperWindowPos.x, config.KeymapperWindowPos.y, MainCanvasUtil.MainCanvasRect.position.z);
      m_KeybindingsWindowObject = Instantiate (m_WindowPrefab);
      if (m_KeybindingsWindowObject == null)
        return;

      // populate window
      m_KeybindingsWindow = m_KeybindingsWindowObject.GetComponent<UI.DraggableWindow> ();
      if (m_KeybindingsWindow != null) {
        m_KeybindingsWindow.SetTitle (Localizer.Format ("precisemaneuver_keybindings_caption"));
        m_KeybindingsWindow.SetMainCanvasTransform (MainCanvasUtil.MainCanvasRect);
        hotkeys.FillKeymapperWindow (m_KeybindingsWindow);
        m_KeybindingsWindow.setWindowInputLock = SetWindow2InputLock;
        m_KeybindingsWindow.resetWindowInputLock = ResetWindow2InputLock;
      }

      GUIComponentManager.ProcessStyle (m_KeybindingsWindowObject);
      GUIComponentManager.ProcessLocalization (m_KeybindingsWindowObject);
      GUIComponentManager.ReplaceLabelsWithTMPro (m_KeybindingsWindowObject);

      // set object as a child of the main canvas
      m_KeybindingsWindowObject.transform.SetParent (MainCanvasUtil.MainCanvas.transform);
      m_KeybindingsWindowObject.transform.position = pos;
    }

    private void CloseKeybindingsWindow () {
      if (m_KeybindingsWindow != null) {
        if (!m_KeybindingsWindow.IsFadingOut) {
          config.KeymapperWindowPos = m_KeybindingsWindow.WindowPosition;
          m_KeybindingsWindow.FadeClose ();
          ResetWindow2InputLock ();
        }
      } else if (m_KeybindingsWindowObject != null) {
        Destroy (m_KeybindingsWindowObject);
        ResetWindow2InputLock ();
      }
    }

    #endregion

    private void SetWindow1InputLock () {
      InputLockManager.SetControlLock (ControlTypes.MAP_UI, "PreciseManeuverWindow1ControlLock");
    }
    private void ResetWindow1InputLock () {
      InputLockManager.RemoveControlLock ("PreciseManeuverWindow1ControlLock");
    }
    private void SetWindow2InputLock () {
      InputLockManager.SetControlLock (ControlTypes.MAP_UI, "PreciseManeuverWindow2ControlLock");
    }
    private void ResetWindow2InputLock () {
      InputLockManager.RemoveControlLock ("PreciseManeuverWindow2ControlLock");
    }

    private bool CanShowNodeEditor {
      get {
        if (!NodeTools.PatchedConicsUnlocked)
          return false;
        PatchedConicSolver solver = FlightGlobals.ActiveVessel.patchedConicSolver;
        return (FlightGlobals.ActiveVessel != null) && MapView.MapIsEnabled && (solver != null) && (solver.maneuverNodes.Count > 0);
      }
    }
  }
}
