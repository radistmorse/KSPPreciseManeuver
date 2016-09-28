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
using System.Collections.Generic;
using UnityEngine;

namespace KSPPreciseManeuver {
using UI;
internal class MainWindow {
  private PreciseManeuverConfig config = PreciseManeuverConfig.Instance;
  private NodeManager nodeManager = NodeManager.Instance;

  #region FastString

  private class FastString {
    internal string value { get; private set; } = "N/A";
    private double current = double.NaN;
    private const double epsilon = 1E-03;
    private string format = "{0}";
    private bool abs = false;
    private bool abbriv = false;
    internal FastString () { }
    internal FastString (string format) {
      this.format = format;
    }
    internal FastString (string format, bool abs, bool abbriv) {
      this.format = format;
      this.abs = abs;
      this.abbriv = abbriv;
    }
    internal bool update (double value) {
      value = abs ? Math.Abs (value) : value;
      if (!double.IsNaN (value) && (double.IsNaN (current) || Math.Abs (current - value) > epsilon)) {
        current = value;
        if (abbriv)
          this.value = String.Format (format, NodeTools.formatMeters (current));
        else
          this.value = String.Format (format, current);
        return true;
      }
      if (double.IsNaN (value) && !double.IsNaN (current)) {
        this.value = "N/A";
        current = value;
        return true;
      }
      return false;
    }
  }

  #endregion

  GameObject[] panels = null;
  private Dictionary<PreciseManeuverConfig.ModuleType, int> sectionPosition = new Dictionary<PreciseManeuverConfig.ModuleType, int> ()
  {
    { PreciseManeuverConfig.ModuleType.PAGER, 0 },
    { PreciseManeuverConfig.ModuleType.SAVER, 2 },
    { PreciseManeuverConfig.ModuleType.INPUT, 4 },
    { PreciseManeuverConfig.ModuleType.TOOLS, 5 },
    { PreciseManeuverConfig.ModuleType.GIZMO, 6 },
    { PreciseManeuverConfig.ModuleType.ENCOT, 7 },
    { PreciseManeuverConfig.ModuleType.EJECT, 8 },
    { PreciseManeuverConfig.ModuleType.ORBIT, 9 },
    { PreciseManeuverConfig.ModuleType.PATCH, 10 },
  };
  private readonly int size = 12;
  
  private void fillSection (PreciseManeuverConfig.ModuleType type, DraggableWindow window, Action<GameObject> createControls, bool initial = false) {
    fillSection (sectionPosition[type], config.getModuleState (type), window, createControls, initial);
  }

  private void fillSection (int num, bool state, DraggableWindow window, Action<GameObject> createControls, bool initial = false) {
    if (state) {
      if (panels[num] == null) {
        panels[num] = window.createInnerContentPanel (num);
        if (!initial) {
          var fader = panels[num].GetComponent<CanvasGroupFader> ();
          fader.setTransparent ();
          fader.fadeIn ();
        }
        createControls (panels[num]);
      } else {
        if (panels[num].GetComponent<CanvasGroupFader> ().IsFadingOut )
          panels[num].GetComponent<CanvasGroupFader> ().fadeIn ();
      }
    } else {
      if (panels[num] != null) {
        panels[num].GetComponent<CanvasGroupFader> ().fadeClose ();
      }
    }
  }

  #region Pager

  GameObject PagerPrefab = PreciseManeuverConfig.Instance.prefabs.LoadAsset<GameObject> ("PreciseManeuverPager");

  private class PagerControlInterface : IPagerControl {

    MainWindow _parent;

    public bool prevManeuverExists {
      get {
        return _parent.nodeManager.previousNodeAvailable;
      }
    }
    public bool nextManeuverExists {
      get {
        return _parent.nodeManager.nextNodeAvailable;
      }
    }
    public int maneuverIdx {
      get {
        return _parent.nodeManager.currentNodeIdx;
      }
    }
    public string CanvasName {
      get {
        return MainCanvasUtil.MainCanvas.sortingLayerName;
      }
    }
    public int maneuverCount {
      get {
        return FlightGlobals.ActiveVessel.patchedConicSolver.maneuverNodes.Count;
      }
    }
    internal PagerControlInterface (MainWindow parent) {
      _parent = parent;
    }
    public void registerUpdateAction (Action action) {
      _parent.nodeManager.listenToIdxChange (action);
    }
    public void deregisterUpdateAction (Action action) {
      _parent.nodeManager.removeListener (action);
    }
    public void PrevButtonPressed () {
      _parent.nodeManager.switchPreviousNode ();
    }
    public void FocusButtonPressed () {
      MapView.MapCamera.SetTarget (_parent.nodeManager.currentNode.scaledSpaceTarget);
    }
    public void DelButtonPressed () {
      _parent.nodeManager.deleteNode ();
    }
    public void NextButtonPressed () {
      _parent.nodeManager.switchNextNode ();
    }
    public string getManeuverTime (int idx) {
      var time = NodeTools.convertUTtoHumanTime(FlightGlobals.ActiveVessel.patchedConicSolver.maneuverNodes[idx].UT);
      return time.Replace ("Year ", "Y").Replace ("Day ", "D");
    }
    public string getManeuverDV (int idx) {
      var dv = FlightGlobals.ActiveVessel.patchedConicSolver.maneuverNodes[idx].DeltaV.magnitude;
      return dv.ToString ("0.##");
    }
    public void SwitchNode (int value) {
      _parent.nodeManager.switchNode (value);
    }
  }

  private void createPagerControls (GameObject panel) {
    if (PagerPrefab == null)
      return;

    var pagerObj = UnityEngine.Object.Instantiate (PagerPrefab) as GameObject;
    StyleManager.Process (pagerObj);
    PagerControl pagercontrol = pagerObj.GetComponent<PagerControl>();
    pagercontrol.SetPagerControl (new PagerControlInterface (this));
    pagerObj.transform.SetParent (panel.transform, false);
  }

  #endregion

  #region Saver

  GameObject SaverPrefab = PreciseManeuverConfig.Instance.prefabs.LoadAsset<GameObject> ("PreciseManeuverSaver");

  private class SaverControlInterface : ISaverControl {

    MainWindow _parent;

    public string CanvasName {
      get {
        return MainCanvasUtil.MainCanvas.sortingLayerName;
      }
    }
    public List<string> presetNames {
      get {
        return _parent.config.getPresetNames ();
      }
    }
    internal SaverControlInterface (MainWindow parent) {
      _parent = parent;
    }
    public void AddPreset (string name) {
      _parent.config.addPreset (name);
    }
    public void RemovePreset (string name) {
      _parent.config.removePreset (name);
    }
    public void loadPreset (string name) {
      _parent.nodeManager.loadPreset (name);
    }
    public string suggestPresetName () {
      var current = _parent.nodeManager.currentNode.patch.referenceBody;
      var next = NodeTools.findNextEncounter();
      if (current != null && next != null && current != next)
        return current.name + " → " + next.name;
      return "";
    }
    public void lockKeyboard () {
      _parent.config.setKeyboardInputLock ();
    }
    public void unlockKeyboard () {
      _parent.config.resetKeyboardInputLock ();
    }
  }

  private void createSaverControls (GameObject panel) {
    if (SaverPrefab == null)
      return;

    var saverObj = UnityEngine.Object.Instantiate (SaverPrefab) as GameObject;
    StyleManager.Process (saverObj);
    SaverControl savercontrol = saverObj.GetComponent<SaverControl>();
    savercontrol.SetControl (new SaverControlInterface (this));
    saverObj.transform.SetParent (panel.transform, false);
  }

  #endregion

  #region UT + Axis Control

  GameObject UTPrefab = PreciseManeuverConfig.Instance.prefabs.LoadAsset<GameObject> ("PreciseManeuverUTControl");
  GameObject axisPrefab = PreciseManeuverConfig.Instance.prefabs.LoadAsset<GameObject> ("PreciseManeuverAxisControl");

  private class UTControlInterface : IUTControl {

    MainWindow _parent;
    FastString _value = new FastString("{0:0.##}");

    internal UTControlInterface (MainWindow parent) {
      _parent = parent;
    }
    public bool APAvailable {
      get {
        return _parent.nodeManager.currentNode.patch.isClosed ();
      }
    }
    public bool PEAvailable {
      get {
        return true;
      }
    }
    public bool ANAvailable {
      get {
        return NodeTools.getTargetOrbit () != null;
      }
    }

    public bool DNAvailable {
      get {
        return NodeTools.getTargetOrbit () != null;
      }
    }
    public bool POAvailable {
      get {
        return _parent.nodeManager.currentNode.patch.isClosed ();
      }
    }
    public bool MOAvailable {
      get {
        return _parent.nodeManager.currentNode.patch.isClosed () && (_parent.nodeManager.currentNode.UT - Planetarium.GetUniversalTime () - _parent.nodeManager.currentNode.patch.period) > 0.0;
      }
    }
    public string UTValue {
      get {
        _value.update (_parent.nodeManager.currentNode.UT);
        return _value.value;
      }
    }
    public bool X10State {
      get {
        return _parent.config.x10UTincrement;
      }

      set {
        _parent.config.x10UTincrement = value;
      }
    }
    public void APButtonPressed () {
      _parent.nodeManager.changeNodeUTtoAP ();
    }
    public void PEButtonPressed () {
      _parent.nodeManager.changeNodeUTtoPE ();
    }
    public void ANButtonPressed () {
      _parent.nodeManager.changeNodeUTtoAN ();
    }
    public void DNButtonPressed () {
      _parent.nodeManager.changeNodeUTtoDN ();
    }
    public void POButtonPressed () {
      _parent.nodeManager.changeNodeUTPlusOrbit ();
    }
    public void MOButtonPressed () {
      _parent.nodeManager.changeNodeUTMinusOrbit ();
    }
    public void PlusButtonPressed () {
      _parent.nodeManager.changeNodeDiff (0, 0, 0, _parent.config.incrementUt);
    }
    public void MinusButtonPressed () {
      _parent.nodeManager.changeNodeDiff (0, 0, 0, -_parent.config.incrementUt);
    }
    public void BeginAtomicChange () {
      _parent.nodeManager.beginAtomicChange ();
    }
    public void EndAtomicChange () {
      _parent.nodeManager.endAtomicChange ();
    }
    public void registerUpdateAction (Action action) {
      _parent.nodeManager.listenToValuesChange (action);
      _parent.nodeManager.listenToTargetChange (action);
      _parent.config.listenTox10Change (action);
    }
    public void deregisterUpdateAction (Action action) {
      _parent.nodeManager.removeListener (action);
      _parent.config.removeListener (action);
    }
  }


  private class AxisControlInterface : IAxisControl {
    internal enum Axis {
      prograde,
      normal,
      radial
    }
    Axis _axis;
    MainWindow _parent;
    FastString _value = new FastString("{0:0.##}");

    internal AxisControlInterface (MainWindow parent, Axis axis) {
      _parent = parent;
      _axis = axis;
    }
    public Color AxisColor {
      get {
        switch (_axis) {
          case Axis.prograde:
            return Color.green;
          case Axis.normal:
            return Color.magenta;
          case Axis.radial:
            return Color.cyan;
        }
        return Color.white;
      }
    }
    public string AxisName {
      get {
        switch (_axis) {
          case Axis.prograde:
            return "Prograde";
          case Axis.normal:
            return "Normal";
          case Axis.radial:
            return "Radial";
        }
        return "To outer space, apparently";
      }
    }
    public string AxisValue {
      get {
        var node = _parent.nodeManager.currentNode;
        if (node != null) {
          switch (_axis) {
            case Axis.prograde:
              _value.update (node.DeltaV.z);
              break;
            case Axis.normal:
              _value.update (node.DeltaV.y);
              break;
            case Axis.radial:
              _value.update (node.DeltaV.x);
              break;
          }
        } else {
          _value.update (Double.NaN);
        }
        return _value.value;
      }
    }
    public void MinusButtonPressed () {
      double dx = _axis == Axis.radial ? _parent.config.increment : 0;
      double dy = _axis == Axis.normal ? _parent.config.increment : 0;
      double dz = _axis == Axis.prograde ? _parent.config.increment : 0;
      _parent.nodeManager.changeNodeDiff (-dx, -dy, -dz, 0.0);
    }
    public void PlusButtonPressed () {
      double dx = _axis == Axis.radial ? _parent.config.increment : 0;
      double dy = _axis == Axis.normal ? _parent.config.increment : 0;
      double dz = _axis == Axis.prograde ? _parent.config.increment : 0;
      _parent.nodeManager.changeNodeDiff (dx, dy, dz, 0.0);
    }
    public void BeginAtomicChange () {
      _parent.nodeManager.beginAtomicChange ();
    }
    public void EndAtomicChange () {
      _parent.nodeManager.endAtomicChange ();
    }
    public void ZeroButtonPressed () {
      double dx = _axis == Axis.radial ? 0 : 1;
      double dy = _axis == Axis.normal ? 0 : 1;
      double dz = _axis == Axis.prograde ? 0 : 1;
      _parent.nodeManager.changeNodeDVMult (dx, dy, dz);
    }

    public void UpdateValueAbs (double value) {
      double dx = _axis == Axis.radial ? value : _parent.nodeManager.currentNode.DeltaV.x;
      double dy = _axis == Axis.normal ? value : _parent.nodeManager.currentNode.DeltaV.y;
      double dz = _axis == Axis.prograde ? value : _parent.nodeManager.currentNode.DeltaV.z;
      _parent.nodeManager.changeNodeDVAbs (dx, dy, dz);
    }

    public void registerUpdateAction (Action action) {
      _parent.nodeManager.listenToValuesChange (action);
    }
    public void deregisterUpdateAction (Action action) {
      _parent.nodeManager.removeListener (action);
    }
    public void lockKeyboard () {
      _parent.config.setKeyboardInputLock ();
    }
    public void unlockKeyboard () {
      _parent.config.resetKeyboardInputLock ();
    }
  }

  private void createUtAxisControls (GameObject panel) {

    if (UTPrefab == null || axisPrefab == null)
      return;

    var utObj = UnityEngine.Object.Instantiate (UTPrefab) as GameObject;
    StyleManager.Process (utObj);
    UTControl utcontrol = utObj.GetComponent<UTControl>();
    utcontrol.SetUTControl (new UTControlInterface (this));
    utObj.transform.SetParent (panel.transform, false);

    var progradeObj = UnityEngine.Object.Instantiate (axisPrefab) as GameObject;
    StyleManager.Process (progradeObj);
    AxisControl progradeAxis = progradeObj.GetComponent<AxisControl>();
    progradeAxis.SetAxisControl (new AxisControlInterface (this, AxisControlInterface.Axis.prograde));
    progradeObj.transform.SetParent (panel.transform, false);

    var normalObj = UnityEngine.Object.Instantiate (axisPrefab) as GameObject;
    StyleManager.Process (normalObj);
    AxisControl normalAxis = normalObj.GetComponent<AxisControl>();
    normalAxis.SetAxisControl (new AxisControlInterface (this, AxisControlInterface.Axis.normal));
    normalObj.transform.SetParent (panel.transform, false);

    var radialObj = UnityEngine.Object.Instantiate (axisPrefab) as GameObject;
    StyleManager.Process (radialObj);
    AxisControl radialAxis = radialObj.GetComponent<AxisControl>();
    radialAxis.SetAxisControl (new AxisControlInterface (this, AxisControlInterface.Axis.radial));
    radialObj.transform.SetParent (panel.transform, false);
  }

  #endregion

  #region Time & Alarm

  GameObject TimeAlarmPrefab = PreciseManeuverConfig.Instance.prefabs.LoadAsset<GameObject> ("PreciseManeuverTimeAlarm");

  private class TimeAlarmControlInterface : ITimeAlarmControl {

    MainWindow _parent;
    double _localUT = -1;
    string _localUTstr = "";

    public string TimeValue {
      get {
        if (_localUT != _parent.nodeManager.currentNode.UT) {
          _localUTstr = NodeTools.convertUTtoHumanTime (_parent.nodeManager.currentNode.UT);
        }
        return _localUTstr;
      }
    }
    public bool AlarmAvailable {
      get {
        return KACWrapper.APIReady;
      }
    }
    public bool AlarmEnabled {
      get {
        return _parent.nodeManager.alarmCreated ();
      }
    }
    internal TimeAlarmControlInterface (MainWindow parent) {
      _parent = parent;
    }
    public void registerUpdateAction (Action action) {
      _parent.nodeManager.listenToValuesChange (action);
    }
    public void deregisterUpdateAction (Action action) {
      _parent.nodeManager.removeListener (action);
    }
    public void alarmToggle (bool state) {
      if (state)
        _parent.nodeManager.createAlarm ();
      else
        _parent.nodeManager.deleteAlarm ();
    }
  }

  private void createTimeAlarmControls (GameObject panel) {
    if (TimeAlarmPrefab == null)
      return;

    var timeAlarmObj = UnityEngine.Object.Instantiate (TimeAlarmPrefab) as GameObject;
    StyleManager.Process (timeAlarmObj);
    TimeAlarmControl timealarmcontrol = timeAlarmObj.GetComponent<TimeAlarmControl>();
    timealarmcontrol.SetTimeAlarmControl (new TimeAlarmControlInterface (this));
    timeAlarmObj.transform.SetParent (panel.transform, false);
  }

  #endregion

  #region Increment

  GameObject IncrementPrefab = PreciseManeuverConfig.Instance.prefabs.LoadAsset<GameObject> ("PreciseManeuverIncrement");

  private class IncrementControlInterface : IIncrementControl {

    MainWindow _parent;

    public int getRawIncrement {
      get {
        return _parent.config.incrementRaw;
      }
    }
    internal IncrementControlInterface (MainWindow parent) {
      _parent = parent;
    }
    public void registerUpdateAction (Action action) {
      _parent.config.listenToIncrementChange (action);
    }
    public void deregisterUpdateAction (Action action) {
      _parent.config.removeListener(action);
    }
    public void incrementChanged (int num) {
      _parent.config.incrementRaw = num;
    }
  }

  private void createIncrementControls (GameObject panel) {
    if (IncrementPrefab == null)
      return;

    var incrementObj = UnityEngine.Object.Instantiate (IncrementPrefab) as GameObject;
    StyleManager.Process (incrementObj);
    IncrementControl pagercontrol = incrementObj.GetComponent<IncrementControl>();
    pagercontrol.SetIncrementControl (new IncrementControlInterface (this));
    incrementObj.transform.SetParent (panel.transform, false);
  }

  #endregion

  #region Orbit Tools

  GameObject OrbitToolsPrefab = PreciseManeuverConfig.Instance.prefabs.LoadAsset<GameObject> ("PreciseManeuverOrbitTools");

  private class OrbitToolsControlInterface : IOrbitToolsControl {

    MainWindow _parent;

    internal OrbitToolsControlInterface (MainWindow parent) {
        _parent = parent;
    }
    public void registerUpdateAction (Action action) {
    }
    public void deregisterUpdateAction (Action action) {
    }
    public void OrbitUpButtonPressed () {
      _parent.nodeManager.turnOrbitUp();
    }
    public void OrbitDnButtonPressed () {
      _parent.nodeManager.turnOrbitDown();
    }
    public void BeginAtomicChange () {
      _parent.nodeManager.beginAtomicChange ();
    }
    public void EndAtomicChange () {
      _parent.nodeManager.endAtomicChange ();
    }
    public void CircularizeButtonPressed () {
      _parent.nodeManager.circularizeOrbit ();
    }
  }

  private void createOrbitToolsControls (GameObject panel) {
    if (OrbitToolsPrefab == null)
      return;

    var orbitToolsObj = UnityEngine.Object.Instantiate (OrbitToolsPrefab) as GameObject;
    StyleManager.Process (orbitToolsObj);
    OrbitToolsControl orbittoolscontrol = orbitToolsObj.GetComponent<OrbitToolsControl>();
    orbittoolscontrol.SetControl (new OrbitToolsControlInterface (this));
    orbitToolsObj.transform.SetParent (panel.transform, false);
  }

  #endregion

  #region Gizmo

  GameObject GizmoPrefab = PreciseManeuverConfig.Instance.prefabs.LoadAsset<GameObject> ("PreciseManeuverGizmo");

  private class GizmoControlInterface : IGizmoControl {

    private MainWindow _parent;
    private Action _controlUpdate = null;
    private bool undoAvailableCache = false;
    private bool redoAvailableCache = false;

    public bool undoAvailable {
      get {
        return _parent.nodeManager.undoAvailable;
      }
    }
    public bool redoAvailable {
      get {
        return _parent.nodeManager.redoAvailable;
      }
    }
    public bool APAvailable {
      get {
        return _parent.nodeManager.currentNode.patch.isClosed ();
      }
    }
    public bool PEAvailable {
      get {
        return true;
      }
    }
    public bool POAvailable {
      get {
        return _parent.nodeManager.currentNode.patch.isClosed ();
      }
    }
    public bool MOAvailable {
      get {
        return _parent.nodeManager.currentNode.patch.isClosed () && (_parent.nodeManager.currentNode.UT - Planetarium.GetUniversalTime () - _parent.nodeManager.currentNode.patch.period) > 0.0;
      }
    }
    public float sensitivity {
      get {
        return _parent.config.gizmoSensitivity;
      }
      set {
        _parent.config.gizmoSensitivity = value;
      }
    }
    internal GizmoControlInterface (MainWindow parent) {
      _parent = parent;
    }
    public void updateNode (double ddx, double ddy, double ddz, double dut) {
      _parent.nodeManager.changeNodeDiff (ddx, ddy, ddz, dut);
    }
    public void registerUpdateAction (Action action) {
      _parent.nodeManager.listenToUndoChange (undoRedoUpdate);
      _parent.nodeManager.listenToValuesChange (action);
      _controlUpdate = action;
    }
    public void deregisterUpdateAction (Action action) {
      _parent.nodeManager.removeListener (undoRedoUpdate);
      _controlUpdate = null;
    }
    public void Undo () {
      if (_parent.nodeManager.undoAvailable) {
        _parent.nodeManager.undo ();
        _controlUpdate?.Invoke ();
      }
    }
    public void Redo () {
      if (_parent.nodeManager.redoAvailable) {
        _parent.nodeManager.redo ();
        _controlUpdate?.Invoke ();
      }
    }
    public void APButtonPressed () {
      _parent.nodeManager.changeNodeUTtoAP ();
    }
    public void PEButtonPressed () {
      _parent.nodeManager.changeNodeUTtoPE ();
    }
    public void POButtonPressed () {
      _parent.nodeManager.changeNodeUTPlusOrbit ();
    }
    public void MOButtonPressed () {
      _parent.nodeManager.changeNodeUTMinusOrbit ();
    }
    public void undoRedoUpdate () {
      if (undoAvailableCache != _parent.nodeManager.undoAvailable ||
          redoAvailableCache != _parent.nodeManager.redoAvailable) {
        undoAvailableCache = _parent.nodeManager.undoAvailable;
        redoAvailableCache = _parent.nodeManager.redoAvailable;
        _controlUpdate?.Invoke ();
      }
    }
    public void beginAtomicChange () {
      _parent.nodeManager.beginAtomicChange ();
    }
    public void endAtomicChange () {
      _parent.nodeManager.endAtomicChange ();
    }
  }

  private void createGizmoControls (GameObject panel) {
    if (GizmoPrefab == null)
      return;

    var gizmoObj = UnityEngine.Object.Instantiate (GizmoPrefab) as GameObject;
    StyleManager.Process (gizmoObj);
    GizmoControl gizmocontrol = gizmoObj.GetComponent<GizmoControl>();
    gizmocontrol.SetControl (new GizmoControlInterface (this));
    gizmoObj.transform.SetParent (panel.transform, false);
  }

  #endregion

  #region Encounter

  GameObject EncounterPrefab = PreciseManeuverConfig.Instance.prefabs.LoadAsset<GameObject> ("PreciseManeuverEncounter");

  private class EncounterControlInterface : IEncounterControl {

    MainWindow _parent;
    FastString _periapsis = new FastString ("{0}m", true, true);
    private bool nextenc = false;

    internal EncounterControlInterface (MainWindow parent) {
      _parent = parent;
    }
    public string Encounter {
      get {
        CelestialBody enc = null;
        var plan = FlightGlobals.ActiveVessel.patchedConicSolver.flightPlan.AsReadOnly();
        var curOrbit = FlightGlobals.ActiveVessel.orbit;
        foreach (var o in plan) {
          if (curOrbit.referenceBody.name != o.referenceBody.name && !o.referenceBody.isSun()) {
            enc = o.referenceBody;
            _periapsis.update (o.PeA);
          }
        }
        nextenc = enc != null;
        if (nextenc)
          return enc.theName;
        return "N/A";
      }
    }
    public string PE {
      get {
        if (nextenc)
          return _periapsis.value;
        return "N/A";
      }
    }
    public void registerUpdateAction (Action action) {
      _parent.nodeManager.listenToValuesChange (action);
    }
    public void deregisterUpdateAction (Action action) {
      _parent.nodeManager.removeListener (action);
    }

    public void focus () {
      var enc = NodeTools.findNextEncounter ();
      if (enc != null)
        MapView.MapCamera.SetTarget (enc);
    }
  }

  private void createEncounterControls (GameObject panel) {
    if (EncounterPrefab == null)
      return;

    var encounterObj = UnityEngine.Object.Instantiate (EncounterPrefab) as GameObject;
    StyleManager.Process (encounterObj);
    EncounterControl encountercontrol = encounterObj.GetComponent<EncounterControl>();
    encountercontrol.SetControl (new EncounterControlInterface (this));
    encounterObj.transform.SetParent (panel.transform, false);
  }

  #endregion

  #region Ejection

  GameObject EjectionPrefab = PreciseManeuverConfig.Instance.prefabs.LoadAsset<GameObject> ("PreciseManeuverEjection");

  private class EjectionControlInterface : IEjectionControl {

    MainWindow _parent;
    FastString _angle = new FastString ("{0:0.00° from prograde;0.00° from retrograde}");
    FastString _inclination = new FastString ("{0:0.00° north;0.00° south}");

    internal EjectionControlInterface (MainWindow parent) {
      _parent = parent;
    }
    public string AngleValue {
      get {
        _angle.update (FlightGlobals.ActiveVessel.orbit.getEjectionAngle (_parent.nodeManager.currentNode));
        return _angle.value;
      }
    }
    public string InclinationValue {
      get {
        _inclination.update (FlightGlobals.ActiveVessel.orbit.getEjectionInclination (_parent.nodeManager.currentNode));
        return _inclination.value;
      }
    }
    public void registerUpdateAction (Action action) {
      _parent.nodeManager.listenToValuesChange (action);
    }
    public void deregisterUpdateAction (Action action) {
      _parent.nodeManager.removeListener (action);
    }
  }

  private void createEjectionControls (GameObject panel) {
    if (EjectionPrefab == null)
      return;

    var ejectionObj = UnityEngine.Object.Instantiate (EjectionPrefab) as GameObject;
    StyleManager.Process (ejectionObj);
    EjectionControl ejectioncontrol = ejectionObj.GetComponent<EjectionControl>();
    ejectioncontrol.SetControl (new EjectionControlInterface (this));
    ejectionObj.transform.SetParent (panel.transform, false);
  }

  #endregion

  #region Orbit Info

  GameObject OrbitInfoPrefab = PreciseManeuverConfig.Instance.prefabs.LoadAsset<GameObject> ("PreciseManeuverOrbitInfo");

  private class OrbitInfoControlInterface : IOrbitInfoControl {

    MainWindow _parent;
    FastString _apoapsis = new FastString ("{0}m", true, true);
    FastString _periapsis = new FastString ("{0}m", true, true);
    FastString _inclination = new FastString ("{0:0.##}°", true, false);
    FastString _eccentricity = new FastString ("{0:0.###}", true, false);

    internal OrbitInfoControlInterface (MainWindow parent) {
      _parent = parent;
    }
    public string ApoapsisValue {
      get {
        _apoapsis.update (_parent.nodeManager.currentNode.nextPatch.ApA);
        return _apoapsis.value;
      }
    }
    public string PeriapsisValue {
      get {
        _periapsis.update (_parent.nodeManager.currentNode.nextPatch.PeA);
        return _periapsis.value;
      }
    }
    public string InclinationValue {
      get {
        _inclination.update (_parent.nodeManager.currentNode.nextPatch.inclination);
        return _inclination.value;
      }
    }
    public string EccentricityValue {
      get {
        _eccentricity.update (_parent.nodeManager.currentNode.nextPatch.eccentricity);
        return _eccentricity.value;
      }
    }
    public void registerUpdateAction (Action action) {
      _parent.nodeManager.listenToValuesChange (action);
    }
    public void deregisterUpdateAction (Action action) {
      _parent.nodeManager.removeListener (action);
    }
  }

  private void createOrbitInfoControls (GameObject panel) {
    if (OrbitInfoPrefab == null)
      return;

    var orbitinfoObj = UnityEngine.Object.Instantiate (OrbitInfoPrefab) as GameObject;
    StyleManager.Process (orbitinfoObj);
    OrbitInfoControl orbitinfocontrol = orbitinfoObj.GetComponent<OrbitInfoControl>();
    orbitinfocontrol.SetControl (new OrbitInfoControlInterface (this));
    orbitinfoObj.transform.SetParent (panel.transform, false);
  }

  #endregion

  #region Conics

  GameObject ConicsPrefab = PreciseManeuverConfig.Instance.prefabs.LoadAsset<GameObject> ("PreciseManeuverConicsControl");

  private class ConicsControlInterface : IConicsControl {

    MainWindow _parent;

    public int getPatchesMode {
      get {
        return _parent.config.conicsMode;
      }
    }
    internal ConicsControlInterface (MainWindow parent) {
      _parent = parent;
    }
    public void conicsModeChanged (int num) {
      _parent.config.conicsMode = num;
    }
    public void MoreConicPatches () {
      FlightGlobals.ActiveVessel.patchedConicSolver.IncreasePatchLimit ();
    }

    public void LessConicPatches () {
      FlightGlobals.ActiveVessel.patchedConicSolver.DecreasePatchLimit ();
    }

    public void registerUpdateAction (Action action) {
      _parent.config.listenToConicsModeChange (action);
    }
    public void deregisterUpdateAction (Action action) {
      _parent.config.removeListener(action);
    }
  }

  private void createConicsControls (GameObject panel) {
    if (ConicsPrefab == null)
      return;

    var conicsObj = UnityEngine.Object.Instantiate (ConicsPrefab) as GameObject;
    StyleManager.Process (conicsObj);
    ConicsControl conicscontrol = conicsObj.GetComponent<ConicsControl>();
    conicscontrol.SetControl (new ConicsControlInterface (this));
    conicsObj.transform.SetParent (panel.transform, false);
  }

  #endregion

  #region Main Window

  internal void updateMainWindow (DraggableWindow window) {
    if (panels == null)
      panels = new GameObject[size];

    bool initial = window.DivideContentPanel (panels.Length);
    // 0 - PAGER
    fillSection (PreciseManeuverConfig.ModuleType.PAGER, window, createPagerControls, initial);
    // 1 - TIME & ALARM (always on)
    fillSection (1, true, window, createTimeAlarmControls, initial);
    // 2 - SAVER
    fillSection (PreciseManeuverConfig.ModuleType.SAVER, window, createSaverControls, initial);
    // 3 - INCREMENT (on if manual || tools)
    bool state = config.getModuleState (PreciseManeuverConfig.ModuleType.INPUT) ||
                 config.getModuleState (PreciseManeuverConfig.ModuleType.TOOLS);
    fillSection (3, state, window, createIncrementControls, initial);
    // 4 - MANUAL INPUT
    fillSection (PreciseManeuverConfig.ModuleType.INPUT, window, createUtAxisControls, initial);
    // 5 - ORBIT TOOLS
    fillSection (PreciseManeuverConfig.ModuleType.TOOLS, window, createOrbitToolsControls, initial);
    // 6 - GIZMO
    fillSection (PreciseManeuverConfig.ModuleType.GIZMO, window, createGizmoControls, initial);
    // 7 - ENCOUNTER
    fillSection (PreciseManeuverConfig.ModuleType.ENCOT, window, createEncounterControls, initial);
    // 8 - EJECTION
    fillSection (PreciseManeuverConfig.ModuleType.EJECT, window, createEjectionControls, initial);
    // 9 - ORBIT INFO
    fillSection (PreciseManeuverConfig.ModuleType.ORBIT, window, createOrbitInfoControls, initial);
    // 10 - CONICS
    fillSection (PreciseManeuverConfig.ModuleType.PATCH, window, createConicsControls, initial);
  }

  internal void clearMainWindow () {
    if (panels != null)
      foreach (var panel in panels)
        if (panel != null)
          UnityEngine.Object.Destroy (panel);
    panels = null;
  }

  #endregion

}
}
