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
using System.Linq;
using UnityEngine.Events;
using System.Collections.Generic;

namespace KSPPreciseManeuver {
  internal partial class NodeManager {

    #region Singleton

    private NodeManager () { }

    static private NodeManager instance;
    static internal NodeManager Instance {
      get {
        if (instance == null)
          instance = new NodeManager ();
        return instance;
      }
    }

    #endregion

    /* Node we're currently working with */
    private ManeuverNode currentNode = null;
    internal ManeuverNode CurrentNode {
      get {
        if (currentNode == null) {
          UpdateCurrentNode ();
        }
        return currentNode;
      }
    }
    internal int CurrentNodeIdx { get; private set; } = -1;
    private int nodeCount = 0;
    /* list that helps find the newly selected nodes */
    private List<ManeuverNode> prevGizmos = null;
    /* Internal copy of the node */
    private SavedNode currentSavedNode = null;
    private SavedNode CurrentSavedNode {
      get {
        if (currentSavedNode == null)
          UpdateCurrentNode ();
        return currentSavedNode;
      }
    }

    private Orbit target = null;

    #region KAC integration

    private KACWrapper.KACAPI.KACAlarm currentAlarm = null;

    internal void CreateAlarm () {
      if (!KACWrapper.APIReady)
        return;

      if (currentAlarm != null)
        return;

      string newID = KACWrapper.KAC.CreateAlarm (KACWrapper.KACAPI.AlarmTypeEnum.Maneuver,
                                               KSP.Localization.Localizer.Format ("precisemaneuver_KAC_name", FlightGlobals.ActiveVessel.GetName()),
                                               CurrentNode.UT - 600.0);
      currentAlarm = KACWrapper.KAC.Alarms.First (a => a.ID == newID);

      currentAlarm.VesselID = FlightGlobals.ActiveVessel.id.ToString ();
      currentAlarm.Notes = KSP.Localization.Localizer.Format ("precisemaneuver_KAC_note");
    }

    internal void DeleteAlarm () {
      if (currentAlarm != null) {
        KACWrapper.KAC.DeleteAlarm (currentAlarm.ID);
        currentAlarm = null;
      }
    }

    internal bool AlarmCreated () {
      return currentAlarm != null;
    }

    #endregion

    #region Update API

    internal void ChangeNodeDiff (double ddvx, double ddvy, double ddvz, double dut) {
      CurrentSavedNode.UpdateDiff (ddvx, ddvy, ddvz, dut);
    }

    internal void ChangeNodeDVMult (double mdvx, double mdvy, double mdvz) {
      CurrentSavedNode.UpdateMult (mdvx, mdvy, mdvz);
    }

    internal void ChangeNodeDVAbs (double dvx, double dvy, double dvz) {
      CurrentSavedNode.UpdateDvAbs (dvx, dvy, dvz);
    }

    internal void ChangeNodeUTtoAP () {
      // a deliberate error that may fix some crashes
      CurrentSavedNode.UpdateUtAbs (CurrentNode.patch.StartUT + CurrentNode.patch.timeToAp + 1E-3);
    }
    internal void ChangeNodeUTtoPE () {
      // a deliberate error that may fix some crashes
      CurrentSavedNode.UpdateUtAbs (CurrentNode.patch.StartUT + CurrentNode.patch.timeToPe + 1E-3);
    }
    internal void ChangeNodeUTtoAN () {
      CurrentSavedNode.UpdateUtAbs (CurrentNode.patch.GetTargetANUT (target));
    }
    internal void ChangeNodeUTtoDN () {
      CurrentSavedNode.UpdateUtAbs (CurrentNode.patch.GetTargetDNUT (target));
    }
    internal void ChangeNodeUTPlusOrbit () {
      if (CurrentNode.patch.IsClosed ())
        CurrentSavedNode.UpdateDiff (0.0, 0.0, 0.0, CurrentNode.patch.period);
    }
    internal void ChangeNodeUTMinusOrbit () {
      if (CurrentNode.patch.IsClosed () && (CurrentNode.UT - CurrentNode.patch.period - Planetarium.GetUniversalTime ()) > 0.0)
        CurrentSavedNode.UpdateDiff (0.0, 0.0, 0.0, -CurrentNode.patch.period);
    }
    internal void ChangeNodeFromString (string str) {
      var reader = new System.IO.StringReader (str);
      string line;
      double nx = CurrentNode.DeltaV.x;
      double ny = CurrentNode.DeltaV.y;
      double nz = CurrentNode.DeltaV.z;
      double nut = CurrentNode.UT;
      bool nextlineut = false;
      while ((line = reader.ReadLine ()) != null) {
        var splitline = line.Split(' ');
        if (line.Contains ("Depart at")) {
          nextlineut = true;
        } else {
          if (line.Contains ("UT") && nextlineut) {
            if (!double.TryParse (splitline[splitline.Length - 1], out nut))
              nut = CurrentNode.UT;
          } else if (line.Contains ("Prograde Δv")) {
            if (!double.TryParse (splitline[splitline.Length - 2], out nz))
              nz = CurrentNode.DeltaV.z;
          } else if (line.Contains ("Normal Δv")) {
            if (!double.TryParse (splitline[splitline.Length - 2], out ny))
              ny = CurrentNode.DeltaV.y;
          } else if (line.Contains ("Radial Δv")) {
            if (!double.TryParse (splitline[splitline.Length - 2], out nx))
              nx = CurrentNode.DeltaV.x;
          } else if (line.Contains ("Ejection Angle")) {
            if (splitline.Length > 3
                && double.TryParse (splitline[splitline.Length - 3].Replace ("°", ""), out double target_eangle)
                && splitline[splitline.Length - 2] == "to"
                && (splitline[splitline.Length - 1] == "prograde" || splitline[splitline.Length - 1] == "retrograde")
                && CurrentNode.patch.IsClosed ()) {
              if (splitline[splitline.Length - 1] == "retrograde")
                target_eangle += 180;
              target_eangle *= Orbit.Deg2Rad;
              Vector3d prograde = CurrentNode.patch.referenceBody.orbit.getOrbitalVelocityAtUT (nut);
              Vector3d position = CurrentNode.patch.getRelativePositionAtUT (nut);
              double eangle = Math.Atan2 (prograde.y, prograde.x) - Math.Atan2 (position.y, position.x);
              if (eangle < 0)
                eangle += Math.PI * 2;

              nut += NodeTools.GetUTdiffForAngle (CurrentNode.patch, nut, eangle - target_eangle);

              while (nut < Planetarium.GetUniversalTime ())
                nut += CurrentNode.patch.period;
            }
          }
          nextlineut = false;
        }
      }
      if (nx != CurrentNode.DeltaV.x || ny != CurrentNode.DeltaV.y || nz != CurrentNode.DeltaV.z || nut != CurrentNode.UT) {
        CurrentSavedNode.BeginAtomicChange ();
        CurrentSavedNode.UpdateDvAbs (nx, ny, nz);
        CurrentSavedNode.UpdateUtAbs (nut);
        CurrentSavedNode.EndAtomicChange ();
      }
    }

    internal bool NextNodeAvailable { get { return CurrentNodeIdx < nodeCount - 1; } }
    internal bool PreviousNodeAvailable { get { return CurrentNodeIdx > 0; } }

    internal bool UndoAvailable { get { return CurrentSavedNode.UndoAvailable; } }

    internal void Undo () {
      CurrentSavedNode.Undo ();
    }

    internal bool RedoAvailable { get { return CurrentSavedNode.RedoAvailable; } }

    internal void Redo () {
      CurrentSavedNode.Redo ();
    }

    internal void BeginAtomicChange () {
      CurrentSavedNode.BeginAtomicChange ();
    }

    internal void EndAtomicChange () {
      CurrentSavedNode.EndAtomicChange ();
    }

    internal void SwitchNextNode () {
      if (NextNodeAvailable) {
        currentNode = null;
        CurrentNodeIdx += 1;
        UpdateCurrentNode ();
        NotifyIndexChanged ();
      }
    }

    internal void SwitchPreviousNode () {
      if (PreviousNodeAvailable) {
        currentNode = null;
        CurrentNodeIdx -= 1;
        UpdateCurrentNode ();
        NotifyIndexChanged ();
      }
    }

    internal void SwitchNode (int idx) {
      if (idx < FlightGlobals.ActiveVessel.patchedConicSolver.maneuverNodes.Count &&
          idx != CurrentNodeIdx) {
        currentNode = null;
        CurrentNodeIdx = idx;
        UpdateCurrentNode ();
        NotifyIndexChanged ();
      }
    }

    internal void DeleteNode () {
      if (currentNode != null) {
        currentNode.RemoveSelf ();
        currentNode = null;
        UpdateCurrentNode ();
      }
    }

    internal void LoadPreset (string name) {
      var dv = PreciseManeuverConfig.Instance.GetPreset (name);
      CurrentSavedNode.UpdateDvAbs (dv.x, dv.y, dv.z);
    }

    internal void TurnOrbitUp () {
      TurnOrbit (-PreciseManeuverConfig.Instance.IncrementDeg);
    }
    internal void TurnOrbitDown () {
      TurnOrbit (PreciseManeuverConfig.Instance.IncrementDeg);
    }

    private void TurnOrbit (double theta) {
      var maneuverPos = CurrentNode.patch.getRelativePositionAtUT (CurrentNode.UT).xzy;
      var maneuverVel = CurrentNode.patch.getOrbitalVelocityAtUT (CurrentNode.UT).xzy;

      if (maneuverPos.NotNAN () && !maneuverPos.IsZero () && maneuverVel.NotNAN ()) {
        var nprog = maneuverVel.normalized;
        var nnorm = Vector3d.Cross (maneuverVel, maneuverPos).normalized;
        var nrad = Vector3d.Cross (nnorm, nprog);

        if (!nprog.IsZero () && !nnorm.IsZero () && !nrad.IsZero ()) {
          var dv = CurrentNode.DeltaV;
          var calcVel = maneuverVel + nrad * dv.x + nnorm * dv.y + nprog * dv.z;
          NodeTools.TurnVector (ref calcVel, maneuverPos, theta);
          var newDV = calcVel - maneuverVel;

          CurrentSavedNode.UpdateDvAbs (Vector3d.Dot (newDV, nrad),
                                        Vector3d.Dot (newDV, nnorm),
                                        Vector3d.Dot (newDV, nprog));
          return;
        }
      }
      // position and velocity are perfectly parallel (less probable)
      // or
      // KSP API returned NaN or some other weird shit (much more probable)
      ScreenMessages.PostScreenMessage (KSP.Localization.Localizer.Format ("precisemaneuver_erroneous_orbit"), 2.0f, ScreenMessageStyle.UPPER_CENTER);
    }

    internal void CircularizeOrbit () {
      var maneuverPos = CurrentNode.patch.getRelativePositionAtUT (CurrentNode.UT).xzy;
      var maneuverVel = CurrentNode.patch.getOrbitalVelocityAtUT (CurrentNode.UT).xzy;

      if (maneuverPos.NotNAN () && !maneuverPos.IsZero () && maneuverVel.NotNAN ()) {
        var nprog = maneuverVel.normalized;
        var nnorm = Vector3d.Cross (maneuverVel, maneuverPos).normalized;
        var nrad = Vector3d.Cross (nnorm, nprog);
        var curVel = CurrentNode.nextPatch.getOrbitalVelocityAtUT (CurrentNode.UT).xzy;
        if (!nprog.IsZero () && !nnorm.IsZero () && !nrad.IsZero () && curVel.NotNAN ()) {
          double rezSpeed = Math.Sqrt (CurrentNode.patch.referenceBody.gravParameter / maneuverPos.magnitude);

          var normVel = Vector3d.Cross (maneuverPos, curVel);
          var newVel = Vector3d.Cross (normVel, maneuverPos).normalized * rezSpeed;
          var newDV = newVel - maneuverVel;

          CurrentSavedNode.UpdateDvAbs (Vector3d.Dot (newDV, nrad),
                                        Vector3d.Dot (newDV, nnorm),
                                        Vector3d.Dot (newDV, nprog));
          return;
        }
      }
      // position and velocity are perfectly parallel (less probable)
      // or
      // KSP API returned NaN or some other weird shit (much more probable)
      ScreenMessages.PostScreenMessage (KSP.Localization.Localizer.Format ("precisemaneuver_erroneous_orbit"), 2.0f, ScreenMessageStyle.UPPER_CENTER);
    }

    #endregion

    #region Updaters

    internal void SearchNewGizmo () {
      var solver = FlightGlobals.ActiveVessel.patchedConicSolver;
      var curList = solver.maneuverNodes.Where (a => a.attachedGizmo != null);
      var tmp = curList.ToList ();
      /* let's see if user is hovering a mouse *
       * over any gizmo. That would be a hint. */
      if (curList.Count (a => a.attachedGizmo.MouseOverGizmo) == 1) {
        var node = curList.First (a => a.attachedGizmo.MouseOverGizmo);
        if (node != currentNode) {
          currentNode = node;
          NotifyNodeChanged ();
        }
      } else {
        /* then, let's see if we can find any     *
         * new gizmos that were created recently. */
        if (prevGizmos != null)
          curList = curList.Except (prevGizmos);
        if (curList.Count () == 1) {
          var node = curList.First ();
          if (node != currentNode) {
            currentNode = node;
            NotifyNodeChanged ();
          }
        }
      }
      prevGizmos = tmp;
    }

    private void UpdateCurrentNode () {
      bool idxboolcache1 = NextNodeAvailable;
      bool idxboolcache2 = PreviousNodeAvailable;
      bool notifyIdxChange = false;
      var solver = FlightGlobals.ActiveVessel.patchedConicSolver;

      if (nodeCount != solver.maneuverNodes.Count)
        notifyIdxChange = true;
      nodeCount = solver.maneuverNodes.Count;

      /* setting the current node */
      int idx = -1;
      /* first, let's see if we already have a valid node */
      if (currentNode != null)
        idx = solver.maneuverNodes.IndexOf (currentNode);
      if (idx != -1) {
        if (CurrentNodeIdx != idx) {
          CurrentNodeIdx = idx;
          notifyIdxChange = true;
        }
      } else {
        /* if no, let's see if our index is still good */
        if (CurrentNodeIdx != -1 && CurrentNodeIdx < solver.maneuverNodes.Count) {
          currentNode = solver.maneuverNodes[CurrentNodeIdx];
          NotifyNodeChanged ();
        } else {
          /* if no, pick the last node */
          if (nodeCount > 0) {
            currentNode = solver.maneuverNodes[nodeCount - 1];
            CurrentNodeIdx = nodeCount - 1;
            notifyIdxChange = true;
            NotifyNodeChanged ();
          } else {
            currentNode = null;
            CurrentNodeIdx = -1;
          }
        }
      }
      /* the state of the prev/next maneuver buttons should change */
      if (idxboolcache1 != NextNodeAvailable || idxboolcache2 != PreviousNodeAvailable || notifyIdxChange)
        NotifyIndexChanged ();
    }

    internal void UpdateNodes () {
      var newtarget = NodeTools.GetTargetOrbit(CurrentNode.patch.referenceBody);

      if (newtarget != target) {
        target = newtarget;
        NotifyTargetChanged ();
      }

      UpdateCurrentNode ();
      bool origSame = CurrentSavedNode.OrigSame (CurrentNode);
      bool changed = CurrentSavedNode.Changed;
      if (changed) {
        if (origSame) {
          Vector3d newdv = CurrentSavedNode.dV;
          if (CurrentNode.attachedGizmo != null) {
            CurrentNode.attachedGizmo.DeltaV = newdv;
            CurrentNode.attachedGizmo.UT = CurrentSavedNode.UT;
          }
          CurrentNode.OnGizmoUpdated (newdv, CurrentSavedNode.UT);
          CurrentSavedNode.UpdateOrig ();
        }
      }
      if (!origSame)
        CurrentSavedNode.ResetSavedNode (CurrentNode);
      if (changed || !origSame)
        NotifyDvUTChanged ();
    }

    internal void Clear () {
      currentNode = null;
      CurrentNodeIdx = -1;
    }

    #endregion

    #region EventListeners

    private enum ChangeType {
      dvut,
      index,
      target,
      undo
    }

    private Dictionary<ChangeType,List<UnityAction>> listeners;

    private Dictionary<ChangeType, List<UnityAction>> Listeners {
      get {
        if (listeners == null) {
          listeners = new Dictionary<ChangeType, List<UnityAction>> (3) {
            [ChangeType.dvut] = new List<UnityAction> (),
            [ChangeType.index] = new List<UnityAction> (),
            [ChangeType.target] = new List<UnityAction> (),
            [ChangeType.undo] = new List<UnityAction> ()
          };
        }
        return listeners;
      }
    }

    public void ListenToIdxChange (UnityAction listener) {
      Listeners[ChangeType.index].Add (listener);
    }
    public void ListenToValuesChange (UnityAction listener) {
      Listeners[ChangeType.dvut].Add (listener);
    }
    public void ListenToTargetChange (UnityAction listener) {
      Listeners[ChangeType.target].Add (listener);
    }
    public void ListenToUndoChange (UnityAction listener) {
      Listeners[ChangeType.undo].Add (listener);
    }

    public void RemoveListener (UnityAction listener) {
      foreach (var list in Listeners.Values)
        list.RemoveAll (a => (a == listener));
    }

    private void NotifyNodeChanged () {
      /* update savednode */
      if (currentSavedNode != null)
        currentSavedNode.ResetSavedNode (CurrentNode);
      else
        currentSavedNode = new SavedNode (CurrentNode);
      /* update KAC alarm */
      if (KACWrapper.APIReady)
        currentAlarm = KACWrapper.KAC.Alarms.FirstOrDefault
                                    (a => ((Math.Abs (a.AlarmTime + 600.0 - CurrentNode.UT) < 1E-05) &&
                                           (a.VesselID == FlightGlobals.ActiveVessel.id.ToString ()) &&
                                           (a.AlarmType == KACWrapper.KACAPI.AlarmTypeEnum.Maneuver)));

      /* if the node changed, its values changed too */
      NotifyDvUTChanged ();
    }

    private void NotifyDvUTChanged () {
      foreach (var act in Listeners[ChangeType.dvut])
        act ();
    }

    private void NotifyIndexChanged () {
      foreach (var act in Listeners[ChangeType.index])
        act ();
    }

    private void NotifyTargetChanged () {
      foreach (var act in Listeners[ChangeType.target])
        act ();
    }

    private void NotifyUndoChanged () {
      foreach (var act in Listeners[ChangeType.undo])
        act ();
    }

    #endregion

  }
}
