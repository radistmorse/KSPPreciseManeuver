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

using System;
using UnityEngine;

namespace KSPPreciseManeuver {
  internal class MainWindow {
    internal PreciseManeuverConfig Config { get; private set; } = PreciseManeuverConfig.Instance;
    internal NodeManager NodeManager { get; private set; } = NodeManager.Instance;

    private GameObject[] panels = null;

    private readonly int size = 12;

    private void FillSection (PreciseManeuverConfig.ModuleType type, UI.DraggableWindow window, Action<GameObject> createControls, bool initial = false) {
      FillSection (type, Config.GetModuleState (type), window, createControls, initial);
    }

    private void FillSection (PreciseManeuverConfig.ModuleType type, bool state, UI.DraggableWindow window, Action<GameObject> createControls, bool initial = false) {
      int num = (int)type;
      if (state) {
        if (panels[num] == null) {
          panels[num] = window.CreateInnerContentPanel (num);
          if (!initial) {
            var fader = panels[num].GetComponent<UI.CanvasGroupFader> ();
            fader.SetTransparent ();
            fader.FadeIn ();
          }
          createControls (panels[num]);
        } else {
          if (panels[num].GetComponent<UI.CanvasGroupFader> ().IsFadingOut)
            panels[num].GetComponent<UI.CanvasGroupFader> ().FadeIn ();
        }
      } else {
        if (panels[num] != null) {
          panels[num].GetComponent<UI.CanvasGroupFader> ().FadeClose ();
        }
      }
    }

    #region Pager

    private GameObject PagerPrefab = PreciseManeuverConfig.Instance.Prefabs.LoadAsset<GameObject> ("PreciseManeuverPager");

    private void CreatePagerControls (GameObject panel) {
      if (PagerPrefab == null)
        return;

      var pagerObj = UnityEngine.Object.Instantiate (PagerPrefab);
      GUIComponentManager.ProcessStyle (pagerObj);
      GUIComponentManager.ProcessLocalization (pagerObj);
      GUIComponentManager.ProcessTooltips (pagerObj);
      GUIComponentManager.ReplaceLabelsWithTMPro (pagerObj);
      var pagercontrol = pagerObj.GetComponent<UI.PagerControl>();
      pagercontrol.SetControl (new PagerControlInterface (this));
      pagerObj.transform.SetParent (panel.transform, false);
    }

    #endregion

    #region Saver

    private GameObject SaverPrefab = PreciseManeuverConfig.Instance.Prefabs.LoadAsset<GameObject> ("PreciseManeuverSaver");

    private void CreateSaverControls (GameObject panel) {
      if (SaverPrefab == null)
        return;

      var saverObj = UnityEngine.Object.Instantiate (SaverPrefab);
      GUIComponentManager.ProcessStyle (saverObj);
      GUIComponentManager.ProcessLocalization (saverObj);
      GUIComponentManager.ProcessTooltips (saverObj);
      GUIComponentManager.ReplaceLabelsWithTMPro (saverObj);
      var savercontrol = saverObj.GetComponent<UI.SaverControl>();
      savercontrol.SetControl (new SaverControlInterface (this));
      saverObj.transform.SetParent (panel.transform, false);
    }

    #endregion

    #region UT + Axis Control

    private GameObject UTPrefab = PreciseManeuverConfig.Instance.Prefabs.LoadAsset<GameObject> ("PreciseManeuverUTControl");
    private GameObject AxisPrefab = PreciseManeuverConfig.Instance.Prefabs.LoadAsset<GameObject> ("PreciseManeuverAxisControl");

    private void CreateUtAxisControls (GameObject panel) {

      if (UTPrefab == null || AxisPrefab == null)
        return;

      var utObj = UnityEngine.Object.Instantiate (UTPrefab);
      GUIComponentManager.ProcessStyle (utObj);
      GUIComponentManager.ProcessLocalization (utObj);
      GUIComponentManager.ProcessTooltips (utObj);
      GUIComponentManager.ReplaceLabelsWithTMPro (utObj);
      var utcontrol = utObj.GetComponent<UI.UTControl>();
      utcontrol.SetControl (new UTControlInterface (this));
      utObj.transform.SetParent (panel.transform, false);

      var progradeObj = UnityEngine.Object.Instantiate (AxisPrefab);
      GUIComponentManager.ProcessStyle (progradeObj);
      GUIComponentManager.ReplaceLabelsWithTMPro (progradeObj);
      var progradeAxis = progradeObj.GetComponent<UI.AxisControl>();
      progradeAxis.SetControl (new AxisControlInterface (this, AxisControlInterface.Axis.prograde));
      progradeObj.transform.SetParent (panel.transform, false);

      var normalObj = UnityEngine.Object.Instantiate (AxisPrefab);
      GUIComponentManager.ProcessStyle (normalObj);
      GUIComponentManager.ReplaceLabelsWithTMPro (normalObj);
      var normalAxis = normalObj.GetComponent<UI.AxisControl>();
      normalAxis.SetControl (new AxisControlInterface (this, AxisControlInterface.Axis.normal));
      normalObj.transform.SetParent (panel.transform, false);

      var radialObj = UnityEngine.Object.Instantiate (AxisPrefab);
      GUIComponentManager.ProcessStyle (radialObj);
      GUIComponentManager.ReplaceLabelsWithTMPro (radialObj);
      var radialAxis = radialObj.GetComponent<UI.AxisControl>();
      radialAxis.SetControl (new AxisControlInterface (this, AxisControlInterface.Axis.radial));
      radialObj.transform.SetParent (panel.transform, false);
    }

    #endregion

    #region Time & Alarm

    private GameObject TimeAlarmPrefab = PreciseManeuverConfig.Instance.Prefabs.LoadAsset<GameObject> ("PreciseManeuverTimeAlarm");

    private void CreateTimeAlarmControls (GameObject panel) {
      if (TimeAlarmPrefab == null)
        return;

      var timeAlarmObj = UnityEngine.Object.Instantiate (TimeAlarmPrefab);
      GUIComponentManager.ProcessStyle (timeAlarmObj);
      GUIComponentManager.ProcessLocalization (timeAlarmObj);
      GUIComponentManager.ProcessTooltips (timeAlarmObj);
      GUIComponentManager.ReplaceLabelsWithTMPro (timeAlarmObj);
      var timealarmcontrol = timeAlarmObj.GetComponent<UI.TimeAlarmControl>();
      timealarmcontrol.SetControl (new TimeAlarmControlInterface (this));
      GUIComponentManager.ReplaceLabelsWithTMPro (timeAlarmObj);
      timeAlarmObj.transform.SetParent (panel.transform, false);
    }

    #endregion

    #region Increment

    private GameObject IncrementPrefab = PreciseManeuverConfig.Instance.Prefabs.LoadAsset<GameObject> ("PreciseManeuverIncrement");

    private void CreateIncrementControls (GameObject panel) {
      if (IncrementPrefab == null)
        return;

      var incrementObj = UnityEngine.Object.Instantiate (IncrementPrefab);
      GUIComponentManager.ProcessStyle (incrementObj);
      GUIComponentManager.ProcessLocalization (incrementObj);
      GUIComponentManager.ProcessTooltips (incrementObj);
      GUIComponentManager.ReplaceLabelsWithTMPro (incrementObj);
      var pagercontrol = incrementObj.GetComponent<UI.IncrementControl>();
      pagercontrol.SetControl (new IncrementControlInterface (this));
      incrementObj.transform.SetParent (panel.transform, false);
    }

    #endregion

    #region Orbit Tools

    private GameObject OrbitToolsPrefab = PreciseManeuverConfig.Instance.Prefabs.LoadAsset<GameObject> ("PreciseManeuverOrbitTools");

    private void CreateOrbitToolsControls (GameObject panel) {
      if (OrbitToolsPrefab == null)
        return;

      var orbitToolsObj = UnityEngine.Object.Instantiate (OrbitToolsPrefab);
      GUIComponentManager.ProcessStyle (orbitToolsObj);
      GUIComponentManager.ProcessLocalization (orbitToolsObj);
      GUIComponentManager.ProcessTooltips (orbitToolsObj);
      GUIComponentManager.ReplaceLabelsWithTMPro (orbitToolsObj);
      var orbittoolscontrol = orbitToolsObj.GetComponent<UI.OrbitToolsControl>();
      orbittoolscontrol.SetControl (new OrbitToolsControlInterface (this));
      orbitToolsObj.transform.SetParent (panel.transform, false);
    }

    #endregion

    #region Gizmo

    private GameObject GizmoPrefab = PreciseManeuverConfig.Instance.Prefabs.LoadAsset<GameObject> ("PreciseManeuverGizmo");

    private void CreateGizmoControls (GameObject panel) {
      if (GizmoPrefab == null)
        return;

      var gizmoObj = UnityEngine.Object.Instantiate (GizmoPrefab);
      GUIComponentManager.ProcessStyle (gizmoObj);
      GUIComponentManager.ProcessLocalization (gizmoObj);
      GUIComponentManager.ProcessTooltips (gizmoObj);
      GUIComponentManager.ReplaceLabelsWithTMPro (gizmoObj);
      var gizmocontrol = gizmoObj.GetComponent<UI.GizmoControl>();
      gizmocontrol.SetControl (new GizmoControlInterface (this));
      gizmoObj.transform.SetParent (panel.transform, false);
    }

    #endregion

    #region Encounter

    private GameObject EncounterPrefab = PreciseManeuverConfig.Instance.Prefabs.LoadAsset<GameObject> ("PreciseManeuverEncounter");

    private void CreateEncounterControls (GameObject panel) {
      if (EncounterPrefab == null)
        return;

      var encounterObj = UnityEngine.Object.Instantiate (EncounterPrefab);
      GUIComponentManager.ProcessStyle (encounterObj);
      GUIComponentManager.ProcessLocalization (encounterObj);
      GUIComponentManager.ProcessTooltips (encounterObj);
      GUIComponentManager.ReplaceLabelsWithTMPro (encounterObj);
      var encountercontrol = encounterObj.GetComponent<UI.EncounterControl>();
      encountercontrol.SetControl (new EncounterControlInterface (this));
      encounterObj.transform.SetParent (panel.transform, false);
    }

    #endregion

    #region Ejection

    private GameObject EjectionPrefab = PreciseManeuverConfig.Instance.Prefabs.LoadAsset<GameObject> ("PreciseManeuverEjection");

    private void CreateEjectionControls (GameObject panel) {
      if (EjectionPrefab == null)
        return;

      var ejectionObj = UnityEngine.Object.Instantiate (EjectionPrefab);
      GUIComponentManager.ProcessStyle (ejectionObj);
      GUIComponentManager.ProcessLocalization (ejectionObj);
      GUIComponentManager.ProcessTooltips (ejectionObj);
      GUIComponentManager.ReplaceLabelsWithTMPro (ejectionObj);
      var ejectioncontrol = ejectionObj.GetComponent<UI.EjectionControl>();
      ejectioncontrol.SetControl (new EjectionControlInterface (this));
      ejectionObj.transform.SetParent (panel.transform, false);
    }

    #endregion

    #region Orbit Info

    private GameObject OrbitInfoPrefab = PreciseManeuverConfig.Instance.Prefabs.LoadAsset<GameObject> ("PreciseManeuverOrbitInfo");

    private void CreateOrbitInfoControls (GameObject panel) {
      if (OrbitInfoPrefab == null)
        return;
      var orbitinfoObj = UnityEngine.Object.Instantiate (OrbitInfoPrefab);
      GUIComponentManager.ProcessStyle (orbitinfoObj);
      GUIComponentManager.ProcessLocalization (orbitinfoObj);
      GUIComponentManager.ProcessTooltips (orbitinfoObj);
      GUIComponentManager.ReplaceLabelsWithTMPro (orbitinfoObj);
      var orbitinfocontrol = orbitinfoObj.GetComponent<UI.OrbitInfoControl>();
      orbitinfocontrol.SetControl (new OrbitInfoControlInterface (this));
      orbitinfoObj.transform.SetParent (panel.transform, false);
    }

    #endregion

    #region Conics

    private GameObject ConicsPrefab = PreciseManeuverConfig.Instance.Prefabs.LoadAsset<GameObject> ("PreciseManeuverConicsControl");

    private void CreateConicsControls (GameObject panel) {
      if (ConicsPrefab == null)
        return;

      var conicsObj = UnityEngine.Object.Instantiate (ConicsPrefab);
      GUIComponentManager.ProcessStyle (conicsObj);
      GUIComponentManager.ProcessLocalization (conicsObj);
      GUIComponentManager.ProcessTooltips (conicsObj);
      GUIComponentManager.ReplaceLabelsWithTMPro (conicsObj);
      var conicscontrol = conicsObj.GetComponent<UI.ConicsControl>();
      conicscontrol.SetControl (new ConicsControlInterface (this));
      conicsObj.transform.SetParent (panel.transform, false);
    }

    #endregion

    #region Main Window

    internal void UpdateMainWindow (UI.DraggableWindow window) {
      if (panels == null)
        panels = new GameObject[size];

      bool initial = window.DivideContentPanel (panels.Length);
      // 0 - PAGER
      FillSection (PreciseManeuverConfig.ModuleType.PAGER, window, CreatePagerControls, initial);
      // 1 - TIME & ALARM (always on)
      FillSection (PreciseManeuverConfig.ModuleType.TIME, true, window, CreateTimeAlarmControls, initial);
      // 2 - SAVER
      FillSection (PreciseManeuverConfig.ModuleType.SAVER, window, CreateSaverControls, initial);
      // 3 - INCREMENT (on if manual || tools)
      bool state = Config.GetModuleState (PreciseManeuverConfig.ModuleType.INPUT) ||
                 Config.GetModuleState (PreciseManeuverConfig.ModuleType.TOOLS);
      FillSection (PreciseManeuverConfig.ModuleType.INCR, state, window, CreateIncrementControls, initial);
      // 4 - MANUAL INPUT
      FillSection (PreciseManeuverConfig.ModuleType.INPUT, window, CreateUtAxisControls, initial);
      // 5 - ORBIT TOOLS
      FillSection (PreciseManeuverConfig.ModuleType.TOOLS, window, CreateOrbitToolsControls, initial);
      // 6 - GIZMO
      FillSection (PreciseManeuverConfig.ModuleType.GIZMO, window, CreateGizmoControls, initial);
      // 7 - ENCOUNTER
      FillSection (PreciseManeuverConfig.ModuleType.ENCOT, window, CreateEncounterControls, initial);
      // 8 - EJECTION
      FillSection (PreciseManeuverConfig.ModuleType.EJECT, window, CreateEjectionControls, initial);
      // 9 - ORBIT INFO
      FillSection (PreciseManeuverConfig.ModuleType.ORBIT, window, CreateOrbitInfoControls, initial);
      // 10 - CONICS
      FillSection (PreciseManeuverConfig.ModuleType.PATCH, window, CreateConicsControls, initial);
    }

    internal void ClearMainWindow () {
      if (panels != null)
        foreach (var panel in panels)
          if (panel != null)
            UnityEngine.Object.Destroy (panel);
      panels = null;
    }

    #endregion

  }
}
