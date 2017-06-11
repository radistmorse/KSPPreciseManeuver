/******************************************************************************
 * Copyright (c) 2017, George Sedov
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
using UnityEngine.UI;
using UnityEngine.Events;
using KSP.Localization;

namespace KSPPreciseManeuver {
  internal class FastString {
    internal string Value { get; private set; } = "N/A";
    private double current = double.NaN;
    private const double epsilon = 1E-03;
    private string format;
    private bool abs;
    private bool abbriv;

    private FastString () { }

    internal FastString (string format, bool abs = false, bool abbriv = false) {
      this.format = format;
      this.abs = abs;
      this.abbriv = abbriv;
    }

    internal bool Update (double value) {
      value = abs ? Math.Abs (value) : value;
      if (!double.IsNaN (value) && (double.IsNaN (current) || Math.Abs (current - value) > epsilon)) {
        current = value;
        if (abbriv)
          Value = String.Format (format, NodeTools.AbbriviateWithMetricPrefix (current));
        else
          Value = String.Format (format, current);
        return true;
      }
      if (double.IsNaN (value) && !double.IsNaN (current)) {
        Value = "N/A";
        current = value;
        return true;
      }
      return false;
    }
  }

  internal abstract class GUIControl : UI.IControl {
    public abstract void RegisterUpdateAction (UnityAction action);
    public abstract void DeregisterUpdateAction (UnityAction action);

    public UnityAction<string> ReplaceTextComponentWithTMPro (Text text) {
      var ugui = GUIComponentManager.ReplaceTextWithTMPro (text);
      return ugui.SetText;
    }

    private TMPro.TMP_InputField inputField = null;

    public void ReplaceInputFieldWithTMPro (InputField field, UnityAction<string> onSubmit = null, UnityAction<string> onChange = null) {
      var text = GUIComponentManager.ReplaceTextWithTMPro (field.textComponent);
      Graphic placeholder = field.placeholder;
      if (placeholder is Text)
        placeholder = GUIComponentManager.ReplaceTextWithTMPro (placeholder as Text);
      var contentType = field.contentType;
      var lineType = field.lineType;
      var charLimit = field.characterLimit;
      var interactable = field.interactable;

      var go = field.gameObject;
      UnityEngine.Object.DestroyImmediate (field);
      inputField = go.AddOrGetComponent<TMPro.TMP_InputField> ();

      inputField.textComponent = text;
      inputField.textViewport = text.transform as RectTransform;
      inputField.placeholder = placeholder;
      if (onSubmit != null)
        inputField.onEndEdit.AddListener (onSubmit);
      if (onChange != null)
        inputField.onValueChanged.AddListener (onChange);
      inputField.contentType = (TMPro.TMP_InputField.ContentType)contentType;
      inputField.lineType = (TMPro.TMP_InputField.LineType)lineType;
      inputField.characterLimit = charLimit;
      inputField.interactable = interactable;
    }

    public string TMProText {
      get { if (inputField == null) return ""; else return inputField.text; }
      set { if (inputField != null) inputField.text = value; }
    }

    public bool TMProIsInteractable {
      get { if (inputField == null) return false; return inputField.interactable; }
      set { if (inputField != null) inputField.interactable = value; }
    }

    public void TMProActivateInputField () {
      if (inputField == null)
        return;
      inputField.ActivateInputField ();
    }

    public void TMProSelectAllText () {
      inputField.Select ();
      if (inputField.text.Length > 0) {
        inputField.caretPosition = inputField.text.Length;
        inputField.selectionAnchorPosition = 0;
        inputField.selectionFocusPosition = inputField.text.Length;
      }
    }
  }

  internal class PagerControlInterface : GUIControl, UI.IPagerControl {

    MainWindow _parent;

    public bool prevManeuverExists {
      get {
        return _parent.NodeManager.PreviousNodeAvailable;
      }
    }
    public bool nextManeuverExists {
      get {
        return _parent.NodeManager.NextNodeAvailable;
      }
    }
    public int maneuverIdx {
      get {
        return _parent.NodeManager.CurrentNodeIdx;
      }
    }
    public Canvas Canvas {
      get {
        return MainCanvasUtil.MainCanvas;
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
    public override void RegisterUpdateAction (UnityAction action) {
      _parent.NodeManager.ListenToIdxChange (action);
    }
    public override void DeregisterUpdateAction (UnityAction action) {
      _parent.NodeManager.RemoveListener (action);
    }
    public void PrevButtonPressed () {
      _parent.NodeManager.SwitchPreviousNode ();
    }
    public void FocusButtonPressed () {
      MapView.MapCamera.SetTarget (_parent.NodeManager.CurrentNode.scaledSpaceTarget);
    }
    public void DelButtonPressed () {
      _parent.NodeManager.DeleteNode ();
    }
    public void NextButtonPressed () {
      _parent.NodeManager.SwitchNextNode ();
    }
    public string getManeuverNodeLocalized () {
      return Localizer.Format ("precisemaneuver_node");
    }
    public string getManeuverTime (int idx) {
      return NodeTools.ConvertUTtoHumanTime (FlightGlobals.ActiveVessel.patchedConicSolver.maneuverNodes[idx].UT, true);
    }
    public string getManeuverDV (int idx) {
      var dv = FlightGlobals.ActiveVessel.patchedConicSolver.maneuverNodes[idx].DeltaV.magnitude;
      return dv.ToString ("0.##");
    }
    public void SwitchNode (int value) {
      _parent.NodeManager.SwitchNode (value);
    }
  }

  internal class SaverControlInterface : GUIControl, UI.ISaverControl {

    MainWindow _parent;

    public Canvas Canvas {
      get {
        return MainCanvasUtil.MainCanvas;
      }
    }
    public System.Collections.Generic.List<string> presetNames () {
      return _parent.Config.GetPresetNames ();
    }
    internal SaverControlInterface (MainWindow parent) {
      _parent = parent;
    }
    public void AddPreset (string name) {
      _parent.Config.AddPreset (name);
    }
    public void RemovePreset (string name) {
      _parent.Config.RemovePreset (name);
    }
    public void loadPreset (string name) {
      _parent.NodeManager.LoadPreset (name);
    }
    public string newPresetLocalized { get { return Localizer.Format ("precisemaneuver_saver_new_preset"); } }
    public string suggestPresetName () {
      var current = _parent.NodeManager.CurrentNode.patch.referenceBody;
      var next = NodeTools.FindNextEncounter();
      if (current != null && next != null && current != next)
        return Localizer.Format ("<<1>> - <<2>>", current.GetDisplayName (), next.GetDisplayName ());
      return "";
    }
    public void lockKeyboard () {
      _parent.Config.SetKeyboardInputLock ();
    }
    public void unlockKeyboard () {
      _parent.Config.ResetKeyboardInputLock ();
    }
    public override void RegisterUpdateAction (UnityAction action) {
    }
    public override void DeregisterUpdateAction (UnityAction action) {
    }
  }

  internal class UTControlInterface : GUIControl, UI.IUTControl {

    MainWindow _parent;
    FastString _value = new FastString("{0:0.##}");

    internal UTControlInterface (MainWindow parent) {
      _parent = parent;
    }
    public bool APAvailable {
      get {
        return _parent.NodeManager.CurrentNode.patch.IsClosed ();
      }
    }
    public bool PEAvailable {
      get {
        return true;
      }
    }
    public bool ANAvailable {
      get {
        return NodeTools.GetTargetOrbit (_parent.NodeManager.CurrentNode.patch.referenceBody) != null;
      }
    }

    public bool DNAvailable {
      get {
        return NodeTools.GetTargetOrbit (_parent.NodeManager.CurrentNode.patch.referenceBody) != null;
      }
    }
    public bool POAvailable {
      get {
        return _parent.NodeManager.CurrentNode.patch.IsClosed ();
      }
    }
    public bool MOAvailable {
      get {
        return _parent.NodeManager.CurrentNode.patch.IsClosed () && (_parent.NodeManager.CurrentNode.UT - Planetarium.GetUniversalTime () - _parent.NodeManager.CurrentNode.patch.period) > 0.0;
      }
    }
    public string UTValue {
      get {
        _value.Update (_parent.NodeManager.CurrentNode.UT);
        return _value.Value;
      }
    }
    public bool X10State {
      get {
        return _parent.Config.X10UTincrement;
      }

      set {
        _parent.Config.X10UTincrement = value;
      }
    }
    public void APButtonPressed () {
      _parent.NodeManager.ChangeNodeUTtoAP ();
    }
    public void PEButtonPressed () {
      _parent.NodeManager.ChangeNodeUTtoPE ();
    }
    public void ANButtonPressed () {
      _parent.NodeManager.ChangeNodeUTtoAN ();
    }
    public void DNButtonPressed () {
      _parent.NodeManager.ChangeNodeUTtoDN ();
    }
    public void POButtonPressed () {
      _parent.NodeManager.ChangeNodeUTPlusOrbit ();
    }
    public void MOButtonPressed () {
      _parent.NodeManager.ChangeNodeUTMinusOrbit ();
    }
    public void PlusButtonPressed () {
      _parent.NodeManager.ChangeNodeDiff (0, 0, 0, _parent.Config.IncrementUt);
    }
    public void MinusButtonPressed () {
      _parent.NodeManager.ChangeNodeDiff (0, 0, 0, -_parent.Config.IncrementUt);
    }
    public void BeginAtomicChange () {
      _parent.NodeManager.BeginAtomicChange ();
    }
    public void EndAtomicChange () {
      _parent.NodeManager.EndAtomicChange ();
    }
    public override void RegisterUpdateAction (UnityAction action) {
      _parent.NodeManager.ListenToValuesChange (action);
      _parent.NodeManager.ListenToTargetChange (action);
      _parent.Config.ListenTox10Change (action);
    }
    public override void DeregisterUpdateAction (UnityAction action) {
      _parent.NodeManager.RemoveListener (action);
      _parent.Config.RemoveListener (action);
    }
  }

  internal class AxisControlInterface : GUIControl, UI.IAxisControl {
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
            return Localizer.Format ("precisemaneuver_axis_prograde");
          case Axis.normal:
            return Localizer.Format ("precisemaneuver_axis_normal");
          case Axis.radial:
            return Localizer.Format ("precisemaneuver_axis_radial");
        }
        return "To outer space, apparently";
      }
    }
    public string AxisValue {
      get {
        var node = _parent.NodeManager.CurrentNode;
        if (node != null) {
          switch (_axis) {
            case Axis.prograde:
              _value.Update (node.DeltaV.z);
              break;
            case Axis.normal:
              _value.Update (node.DeltaV.y);
              break;
            case Axis.radial:
              _value.Update (node.DeltaV.x);
              break;
          }
        } else {
          _value.Update (Double.NaN);
        }
        return _value.Value;
      }
    }
    public void MinusButtonPressed () {
      double dx = _axis == Axis.radial ? _parent.Config.Increment : 0;
      double dy = _axis == Axis.normal ? _parent.Config.Increment : 0;
      double dz = _axis == Axis.prograde ? _parent.Config.Increment : 0;
      _parent.NodeManager.ChangeNodeDiff (-dx, -dy, -dz, 0.0);
    }
    public void PlusButtonPressed () {
      double dx = _axis == Axis.radial ? _parent.Config.Increment : 0;
      double dy = _axis == Axis.normal ? _parent.Config.Increment : 0;
      double dz = _axis == Axis.prograde ? _parent.Config.Increment : 0;
      _parent.NodeManager.ChangeNodeDiff (dx, dy, dz, 0.0);
    }
    public void BeginAtomicChange () {
      _parent.NodeManager.BeginAtomicChange ();
    }
    public void EndAtomicChange () {
      _parent.NodeManager.EndAtomicChange ();
    }
    public void ZeroButtonPressed () {
      double dx = _axis == Axis.radial ? 0 : 1;
      double dy = _axis == Axis.normal ? 0 : 1;
      double dz = _axis == Axis.prograde ? 0 : 1;
      _parent.NodeManager.ChangeNodeDVMult (dx, dy, dz);
    }

    public void UpdateValueAbs (double value) {
      double dx = _axis == Axis.radial ? value : _parent.NodeManager.CurrentNode.DeltaV.x;
      double dy = _axis == Axis.normal ? value : _parent.NodeManager.CurrentNode.DeltaV.y;
      double dz = _axis == Axis.prograde ? value : _parent.NodeManager.CurrentNode.DeltaV.z;
      _parent.NodeManager.ChangeNodeDVAbs (dx, dy, dz);
    }

    public override void RegisterUpdateAction (UnityAction action) {
      _parent.NodeManager.ListenToValuesChange (action);
    }
    public override void DeregisterUpdateAction (UnityAction action) {
      _parent.NodeManager.RemoveListener (action);
    }
    public void LockKeyboard () {
      _parent.Config.SetKeyboardInputLock ();
    }
    public void UnlockKeyboard () {
      _parent.Config.ResetKeyboardInputLock ();
    }
  }

  internal class TimeAlarmControlInterface : GUIControl, UI.ITimeAlarmControl {

    MainWindow _parent;
    double _localUT = -1;
    string _localUTstr = "";

    public string TimeValue {
      get {
        if (_localUT != _parent.NodeManager.CurrentNode.UT) {
          _localUTstr = NodeTools.ConvertUTtoHumanTime (_parent.NodeManager.CurrentNode.UT);
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
        return _parent.NodeManager.AlarmCreated ();
      }
    }
    internal TimeAlarmControlInterface (MainWindow parent) {
      _parent = parent;
    }
    public override void RegisterUpdateAction (UnityAction action) {
      _parent.NodeManager.ListenToValuesChange (action);
    }
    public override void DeregisterUpdateAction (UnityAction action) {
      _parent.NodeManager.RemoveListener (action);
    }
    public void alarmToggle (bool state) {
      if (state)
        _parent.NodeManager.CreateAlarm ();
      else
        _parent.NodeManager.DeleteAlarm ();
    }
  }

  internal class IncrementControlInterface : GUIControl, UI.IIncrementControl {

    MainWindow _parent;

    public int getRawIncrement {
      get {
        return _parent.Config.IncrementRaw;
      }
    }
    internal IncrementControlInterface (MainWindow parent) {
      _parent = parent;
    }
    public override void RegisterUpdateAction (UnityAction action) {
      _parent.Config.ListenToIncrementChange (action);
    }
    public override void DeregisterUpdateAction (UnityAction action) {
      _parent.Config.RemoveListener (action);
    }
    public void incrementChanged (int num) {
      _parent.Config.IncrementRaw = num;
    }
  }

  internal class OrbitToolsControlInterface : GUIControl, UI.IOrbitToolsControl {

    MainWindow _parent;

    internal OrbitToolsControlInterface (MainWindow parent) {
      _parent = parent;
    }
    public override void RegisterUpdateAction (UnityAction action) {
    }
    public override void DeregisterUpdateAction (UnityAction action) {
    }
    public void OrbitUpButtonPressed () {
      _parent.NodeManager.TurnOrbitUp ();
    }
    public void OrbitDnButtonPressed () {
      _parent.NodeManager.TurnOrbitDown ();
    }
    public void BeginAtomicChange () {
      _parent.NodeManager.BeginAtomicChange ();
    }
    public void EndAtomicChange () {
      _parent.NodeManager.EndAtomicChange ();
    }
    public void CircularizeButtonPressed () {
      _parent.NodeManager.CircularizeOrbit ();
    }
    public void CopyButtonPressed () {
      NodeTools.CopyToClipboard (FlightGlobals.ActiveVessel.orbit, _parent.NodeManager.CurrentNode);
    }
    public void PasteButtonPressed () {
      string clipboard = GUIUtility.systemCopyBuffer;
      _parent.NodeManager.ChangeNodeFromString (clipboard);
    }
  }

  internal class GizmoControlInterface : GUIControl, UI.IGizmoControl {

    private MainWindow _parent;
    private UnityAction _controlUpdate = null;
    private bool undoAvailableCache = false;
    private bool redoAvailableCache = false;

    public bool UndoAvailable {
      get {
        return _parent.NodeManager.UndoAvailable;
      }
    }
    public bool RedoAvailable {
      get {
        return _parent.NodeManager.RedoAvailable;
      }
    }
    public bool APAvailable {
      get {
        return _parent.NodeManager.CurrentNode.patch.IsClosed ();
      }
    }
    public bool PEAvailable {
      get {
        return true;
      }
    }
    public bool POAvailable {
      get {
        return _parent.NodeManager.CurrentNode.patch.IsClosed ();
      }
    }
    public bool MOAvailable {
      get {
        return _parent.NodeManager.CurrentNode.patch.IsClosed () && (_parent.NodeManager.CurrentNode.UT - Planetarium.GetUniversalTime () - _parent.NodeManager.CurrentNode.patch.period) > 0.0;
      }
    }
    public float Sensitivity {
      get {
        return _parent.Config.GizmoSensitivity;
      }
      set {
        _parent.Config.GizmoSensitivity = value;
      }
    }
    internal GizmoControlInterface (MainWindow parent) {
      _parent = parent;
    }
    public void UpdateNode (double ddx, double ddy, double ddz, double dut) {
      _parent.NodeManager.ChangeNodeDiff (ddx, ddy, ddz, dut);
    }
    public override void RegisterUpdateAction (UnityAction action) {
      _controlUpdate = action;
      _parent.NodeManager.ListenToUndoChange (UndoRedoUpdate);
      _parent.NodeManager.ListenToValuesChange (_controlUpdate);
    }
    public override void DeregisterUpdateAction (UnityAction action) {
      _parent.NodeManager.RemoveListener (UndoRedoUpdate);
      _parent.NodeManager.RemoveListener (_controlUpdate);
      _controlUpdate = null;
    }
    public void Undo () {
      if (_parent.NodeManager.UndoAvailable) {
        _parent.NodeManager.Undo ();
        _controlUpdate?.Invoke ();
      }
    }
    public void Redo () {
      if (_parent.NodeManager.RedoAvailable) {
        _parent.NodeManager.Redo ();
        _controlUpdate?.Invoke ();
      }
    }
    public void APButtonPressed () {
      _parent.NodeManager.ChangeNodeUTtoAP ();
    }
    public void PEButtonPressed () {
      _parent.NodeManager.ChangeNodeUTtoPE ();
    }
    public void POButtonPressed () {
      _parent.NodeManager.ChangeNodeUTPlusOrbit ();
    }
    public void MOButtonPressed () {
      _parent.NodeManager.ChangeNodeUTMinusOrbit ();
    }
    public void UndoRedoUpdate () {
      if (undoAvailableCache != _parent.NodeManager.UndoAvailable ||
          redoAvailableCache != _parent.NodeManager.RedoAvailable) {
        undoAvailableCache = _parent.NodeManager.UndoAvailable;
        redoAvailableCache = _parent.NodeManager.RedoAvailable;
        _controlUpdate?.Invoke ();
      }
    }
    public void BeginAtomicChange () {
      _parent.NodeManager.BeginAtomicChange ();
    }
    public void EndAtomicChange () {
      _parent.NodeManager.EndAtomicChange ();
    }
  }

  internal class EncounterControlInterface : GUIControl, UI.IEncounterControl {

    private MainWindow _parent;

    private FastString periapsis;
    private bool isnextenc = false;
    private CelestialBody nextenc = null;

    internal EncounterControlInterface (MainWindow parent) {
      _parent = parent;
      periapsis = new FastString ("{0}" + Localizer.Format ("precisemaneuver_meter"), true, true);
    }

    public bool IsEncounter {
      get {
        var plan = FlightGlobals.ActiveVessel.patchedConicSolver.flightPlan.AsReadOnly();
        var curOrbit = FlightGlobals.ActiveVessel.orbit;
        nextenc = null;
        foreach (var o in plan) {
          if (curOrbit.referenceBody.name != o.referenceBody.name && !o.referenceBody.IsSun ()) {
            nextenc = o.referenceBody;
            periapsis.Update (o.PeA);
            break;
          }
        }
        isnextenc = nextenc != null;
        return isnextenc;
      }
    }
    public string Encounter {
      get {
        if (isnextenc)
          return Localizer.Format ("<<1>>", nextenc.GetDisplayName ());
        return "N/A";
      }
    }
    public string PE {
      get {
        if (isnextenc)
          return periapsis.Value;
        return "N/A";
      }
    }

    public override void RegisterUpdateAction (UnityAction action) {
      _parent.NodeManager.ListenToValuesChange (action);
    }
    public override void DeregisterUpdateAction (UnityAction action) {
      _parent.NodeManager.RemoveListener (action);
    }

    public void focus () {
      var enc = NodeTools.FindNextEncounter ();
      if (enc != null)
        MapView.MapCamera.SetTarget (enc);
    }
  }

  internal class EjectionControlInterface : GUIControl, UI.IEjectionControl {

    MainWindow _parent;
    FastString _angle;
    FastString _inclination;

    internal EjectionControlInterface (MainWindow parent) {
      _parent = parent;
      _angle = new FastString (Localizer.Format ("precisemaneuver_ejection_angle_format"));
      _inclination = new FastString (Localizer.Format ("precisemaneuver_ejection_inclination_format"));
    }
    public string AngleValue {
      get {
        _angle.Update (FlightGlobals.ActiveVessel.orbit.GetEjectionAngle (_parent.NodeManager.CurrentNode));
        return _angle.Value;
      }
    }
    public string InclinationValue {
      get {
        _inclination.Update (FlightGlobals.ActiveVessel.orbit.GetEjectionInclination (_parent.NodeManager.CurrentNode));
        return _inclination.Value;
      }
    }
    public override void RegisterUpdateAction (UnityAction action) {
      _parent.NodeManager.ListenToValuesChange (action);
    }
    public override void DeregisterUpdateAction (UnityAction action) {
      _parent.NodeManager.RemoveListener (action);
    }
  }

  internal class OrbitInfoControlInterface : GUIControl, UI.IOrbitInfoControl {

    MainWindow _parent;
    FastString _apoapsis;
    FastString _periapsis;
    FastString _inclination = new FastString ("{0:0.##}°", true, false);
    FastString _eccentricity = new FastString ("{0:0.###}", true, false);

    internal OrbitInfoControlInterface (MainWindow parent) {
      _parent = parent;
      _apoapsis = new FastString ("{0}" + Localizer.Format ("precisemaneuver_meter"), false, true);
      _periapsis = new FastString ("{0}" + Localizer.Format ("precisemaneuver_meter"), false, true);
    }
    public string ApoapsisValue {
      get {
        _apoapsis.Update (_parent.NodeManager.CurrentNode.nextPatch.ApA);
        return _apoapsis.Value;
      }
    }
    public string PeriapsisValue {
      get {
        _periapsis.Update (_parent.NodeManager.CurrentNode.nextPatch.PeA);
        return _periapsis.Value;
      }
    }
    public string InclinationValue {
      get {
        _inclination.Update (_parent.NodeManager.CurrentNode.nextPatch.inclination);
        return _inclination.Value;
      }
    }
    public string EccentricityValue {
      get {
        _eccentricity.Update (_parent.NodeManager.CurrentNode.nextPatch.eccentricity);
        return _eccentricity.Value;
      }
    }
    public override void RegisterUpdateAction (UnityAction action) {
      _parent.NodeManager.ListenToValuesChange (action);
    }
    public override void DeregisterUpdateAction (UnityAction action) {
      _parent.NodeManager.RemoveListener (action);
    }
  }

  internal class ConicsControlInterface : GUIControl, UI.IConicsControl {

    MainWindow _parent;

    public int getPatchesMode {
      get {
        return _parent.Config.ConicsMode;
      }
    }
    internal ConicsControlInterface (MainWindow parent) {
      _parent = parent;
    }
    public void conicsModeChanged (int num) {
      _parent.Config.ConicsMode = num;
    }
    public void MoreConicPatches () {
      FlightGlobals.ActiveVessel.patchedConicSolver.IncreasePatchLimit ();
    }

    public void LessConicPatches () {
      FlightGlobals.ActiveVessel.patchedConicSolver.DecreasePatchLimit ();
    }

    public override void RegisterUpdateAction (UnityAction action) {
      _parent.Config.ListenToConicsModeChange (action);
    }
    public override void DeregisterUpdateAction (UnityAction action) {
      _parent.Config.RemoveListener (action);
    }
  }
}
