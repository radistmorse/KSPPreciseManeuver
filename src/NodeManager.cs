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
using System.Collections.Generic;

namespace KSPPreciseManeuver {
internal class NodeManager {
  #region Singleton

  private NodeManager () {
    initAlarm ();
  }
  static private NodeManager _instance;
  static internal NodeManager Instance {
    get {
      if (_instance == null)
        _instance = new NodeManager ();
      return _instance;
    }
  }

  #endregion

  #region SavedNode internal class

  private class SavedNode {
    private double dx = 0.0;
    private double dy = 0.0;
    private double dz = 0.0;
    private double origdx;
    private double origdy;
    private double origdz;
    private double _ut = 0.0;
    private double origUt;
    private const double epsilon = 1E-5;

    internal Vector3d dv { get { return new Vector3d (dx, dy, dz); } }
    internal double ut { get { return _ut; } }
    internal bool changed { get; private set; } = false;

    internal SavedNode (ManeuverNode node) {
      updateOrig (node);
    }

    internal void updateDiff (double ddvx, double ddvy, double ddvz, double ddut) {
      dx += ddvx;
      dy += ddvy;
      dz += ddvz;
      _ut += ddut;
      changed = true;
    }
    internal void updateMult (double mdvx, double mdvy, double mdvz) {
      dx *= mdvx;
      dy *= mdvy;
      dz *= mdvz;
      changed = true;
    }
    internal void updateDvAbs (double dvx, double dvy, double dvz) {
      dx = dvx;
      dy = dvy;
      dz = dvz;
      changed = true;
    }
    internal void updateUtAbs (double ut) {
      _ut = ut;
      changed = true;
    }

    internal void updateOrig (ManeuverNode node) {
      origdx = node.DeltaV.x;
      origdy = node.DeltaV.y;
      origdz = node.DeltaV.z;
      origUt = node.UT;
      dx = node.DeltaV.x;
      dy = node.DeltaV.y;
      dz = node.DeltaV.z;
      _ut = node.UT;
      changed = false;
    }

    internal bool origSame (ManeuverNode node) {
      return (Math.Abs (origdx - node.DeltaV.x) < epsilon &&
              Math.Abs (origdy - node.DeltaV.y) < epsilon &&
              Math.Abs (origdz - node.DeltaV.z) < epsilon &&
              Math.Abs (origUt - node.UT) < epsilon);
    }
  }

  #endregion

  /* Node we're currently working with */
  private ManeuverNode _currentNode = null;
  internal ManeuverNode currentNode {
    get {
      if (_currentNode == null) {
        updateCurrentNode ();
      }
      return _currentNode;
    }
  }
  internal int currentNodeIdx { get; private set; } = -1;
  private int nodeCount = 0;
  /* Variables that help find the newly selected nodes */
  private bool nodeRecentlyAdded = false;
  private List<ManeuverNode> prevGizmos = null;
  /* Internal copy of the node */
  private SavedNode _currentSavedNode = null;
  private SavedNode currentSavedNode {
    get {
      if (_currentSavedNode == null)
        updateCurrentNode ();
      return _currentSavedNode;
    }
  }
  /* KAC */
  private KACWrapper.KACAPI.KACAlarm currentAlarm = null;

  private Orbit target = null;

  #region KAC integration

  internal void initAlarm () {
    KACWrapper.InitKACWrapper ();
  }

  internal void createAlarm () {
    if (!KACWrapper.APIReady)
      return;

    if (currentAlarm != null)
      return;

    string newID = KACWrapper.KAC.CreateAlarm (KACWrapper.KACAPI.AlarmTypeEnum.Maneuver,
                                             "Maneuver for " + FlightGlobals.ActiveVessel.GetName(),
                                             currentNode.UT - 600.0);
    currentAlarm = KACWrapper.KAC.Alarms.First (a => a.ID == newID);

    currentAlarm.VesselID = FlightGlobals.ActiveVessel.id.ToString ();
    currentAlarm.Notes = "The maneuver is in 10 minutes.";
  }

  internal void deleteAlarm () {
    if (currentAlarm != null) {
      KACWrapper.KAC.DeleteAlarm (currentAlarm.ID);
      currentAlarm = null;
    }
  }

  internal bool alarmCreated () {
    return currentAlarm != null;
  }

  #endregion

  #region Update API

  internal void changeNodeDiff (double ddvx, double ddvy, double ddvz, double dut) {
    currentSavedNode.updateDiff (ddvx, ddvy, ddvz, dut);
  }

  internal void changeNodeDVMult (double mdvx, double mdvy, double mdvz) {
    currentSavedNode.updateMult (mdvx, mdvy, mdvz);
  }

  internal void changeNodeUTtoAP () {
    currentSavedNode.updateUtAbs (currentNode.patch.StartUT + currentNode.patch.timeToAp);
  }
  internal void changeNodeUTtoPE () {
    currentSavedNode.updateUtAbs (currentNode.patch.StartUT + currentNode.patch.timeToPe);
  }
  internal void changeNodeUTtoAN () {
    currentSavedNode.updateUtAbs (currentNode.patch.getTargetANUT (target));
  }
  internal void changeNodeUTtoDN () {
    currentSavedNode.updateUtAbs (currentNode.patch.getTargetDNUT (target));
  }

  internal bool nextNodeAvailable { get { return currentNodeIdx < nodeCount - 1; } }
  internal bool previousNodeAvailable { get { return currentNodeIdx > 0; } }

  internal void switchNextNode () {
    if (nextNodeAvailable) {
      _currentNode = null;
      currentNodeIdx += 1;
      updateCurrentNode ();
      notifyIndexChanged ();
    }
  }

  internal void switchPreviousNode () {
    if (previousNodeAvailable) {
      _currentNode = null;
      currentNodeIdx -= 1;
      updateCurrentNode ();
      notifyIndexChanged ();
    }
  }

  internal void switchNode (int idx) {
    if (idx < FlightGlobals.ActiveVessel.patchedConicSolver.maneuverNodes.Count &&
        idx != currentNodeIdx) {
      _currentNode = null;
      currentNodeIdx = idx;
      updateCurrentNode ();
      notifyIndexChanged ();
    }
  }

  internal void deleteNode () {
    if (_currentNode != null) {
      _currentNode.RemoveSelf ();
      _currentNode = null;
      updateCurrentNode ();
    }
  }

  internal void loadPreset (string name) {
    var dv = PreciseManeuverConfig.Instance.getPreset (name);
    currentSavedNode.updateDvAbs (dv.x, dv.y, dv.z);
  }
  
  internal void turnOrbitUp () {
    turnOrbit (-PreciseManeuverConfig.Instance.incrementDeg);
  }
  internal void turnOrbitDown () {
    turnOrbit (PreciseManeuverConfig.Instance.incrementDeg);
  }

  private void turnOrbit (double theta) {
    var maneuverPos = currentNode.patch.getRelativePositionAtUT (currentNode.UT).xzy;
    var maneuverVel = currentNode.patch.getOrbitalVelocityAtUT (currentNode.UT).xzy;

    if (maneuverPos.NotNAN () && !maneuverPos.IsZero () && maneuverVel.NotNAN ()) {
      var nprog = maneuverVel.normalized;
      var nnorm = Vector3d.Cross (maneuverVel, maneuverPos).normalized;
      var nrad = Vector3d.Cross (nnorm, nprog);

      if (!nprog.IsZero () && !nnorm.IsZero () && !nrad.IsZero ()) {
        var dv = currentNode.DeltaV;
        var calcVel = maneuverVel + nrad * dv.x + nnorm * dv.y + nprog * dv.z;
        NodeTools.turnVector (ref calcVel, maneuverPos, theta);
        var newDV = calcVel - maneuverVel;

        currentSavedNode.updateDvAbs (Vector3d.Dot (newDV, nrad),
                                      Vector3d.Dot (newDV, nnorm),
                                      Vector3d.Dot (newDV, nprog));
        return;
      }
    }
    // position and velocity are perfectly parallel (less probable)
    // or
    // KSP API returned NaN or some other weird shit (much more probable)
    ScreenMessages.PostScreenMessage ("Can't change the orbit, parameters are invalid", 2.0f, ScreenMessageStyle.UPPER_CENTER);
  }

  internal void circularizeOrbit () {
    var maneuverPos = currentNode.patch.getRelativePositionAtUT (currentNode.UT).xzy;
    var maneuverVel = currentNode.patch.getOrbitalVelocityAtUT (currentNode.UT).xzy;

    if (maneuverPos.NotNAN () && !maneuverPos.IsZero () && maneuverVel.NotNAN ()) {
      var nprog = maneuverVel.normalized;
      var nnorm = Vector3d.Cross (maneuverVel, maneuverPos).normalized;
      var nrad = Vector3d.Cross (nnorm, nprog);
      var curVel = currentNode.nextPatch.getOrbitalVelocityAtUT (currentNode.UT).xzy;
      if (!nprog.IsZero () && !nnorm.IsZero () && !nrad.IsZero () && curVel.NotNAN ()) {
        double rezSpeed = Math.Sqrt (currentNode.patch.referenceBody.gravParameter / maneuverPos.magnitude);

        var normVel = Vector3d.Cross (maneuverPos, curVel);
        var newVel = Vector3d.Cross (normVel, maneuverPos).normalized * rezSpeed;
        var newDV = newVel - maneuverVel;

        currentSavedNode.updateDvAbs (Vector3d.Dot (newDV, nrad),
                                      Vector3d.Dot (newDV, nnorm),
                                      Vector3d.Dot (newDV, nprog));
        return;
      }
    }
    // position and velocity are perfectly parallel (less probable)
    // or
    // KSP API returned NaN or some other weird shit (much more probable)
    ScreenMessages.PostScreenMessage ("Can't change the orbit, parameters are invalid", 2.0f, ScreenMessageStyle.UPPER_CENTER);
  }

  #endregion

  #region Updaters

  internal void searchNewGizmo () {
    var solver = FlightGlobals.ActiveVessel.patchedConicSolver;
    var curList = solver.maneuverNodes.Where (a => a.attachedGizmo != null);
    var tmp = curList.ToList ();
    /* first, if the node was just created, choose it */
    if (nodeRecentlyAdded) {
      var node = solver.maneuverNodes.Last ();
      if (node != _currentNode) {
        _currentNode = node;
        notifyNodeChanged ();
      }
      nodeRecentlyAdded = false;
    } else {
      /* then, let's see if user is hovering a mouse *
       * over any gizmo. That would be a hint.       */
      if (curList.Count (a => a.attachedGizmo.MouseOverGizmo) == 1) {
        var node = curList.First (a => a.attachedGizmo.MouseOverGizmo);
        if (node != _currentNode) {
          _currentNode = node;
          notifyNodeChanged ();
        }
      } else {
        /* finally, let's see if we can find any  *
         * new gizmos that were created recently. */
        if (prevGizmos != null)
          curList = curList.Except (prevGizmos);
        if (curList.Count () == 1) {
          var node = curList.First ();
          if (node != _currentNode) {
            _currentNode = node;
            notifyNodeChanged ();
          }
        }
      }
    }
    prevGizmos = tmp;
  }

  private void updateCurrentNode () {
    bool idxboolcache1 = nextNodeAvailable;
    bool idxboolcache2 = previousNodeAvailable;
    var solver = FlightGlobals.ActiveVessel.patchedConicSolver;
    int tmp = solver.maneuverNodes.Count;
    if (tmp > nodeCount)
      nodeRecentlyAdded = true;
    nodeCount = tmp;

    /* setting the current node */
    int idx = -1;
    /* first, let's see if we already have a valid node */
    if (_currentNode != null)
      idx = solver.maneuverNodes.IndexOf (_currentNode);
    if (idx != -1) {
      if (currentNodeIdx != idx) {
        currentNodeIdx = idx;
        notifyIndexChanged ();
      }
    } else {
      /* if no, let's see if our index is still good */
      if (currentNodeIdx != -1 && currentNodeIdx < solver.maneuverNodes.Count) {
        _currentNode = solver.maneuverNodes[currentNodeIdx];
        notifyNodeChanged ();
      } else {
        /* if no, pick the last node */
        if (nodeCount > 0) {
          _currentNode = solver.maneuverNodes[nodeCount - 1];
          currentNodeIdx = nodeCount - 1;
          notifyIndexChanged ();
          notifyNodeChanged ();
        } else {
          _currentNode = null;
          currentNodeIdx = -1;
        }
      }
    }
    /* the state of the prev/next maneuver buttons should change */
    if (idxboolcache1 != nextNodeAvailable || idxboolcache2 != previousNodeAvailable)
      notifyIndexChanged ();
  }

  internal void updateNodes () {
    var newtarget = NodeTools.getTargetOrbit();

    if (newtarget != target) {
      target = newtarget;
      notifyTargetChanged ();
    }

    updateCurrentNode ();
    bool origSame = currentSavedNode.origSame (currentNode);
    bool changed = currentSavedNode.changed;
    if (changed) {
      if (origSame) {
        Vector3d newdv = currentSavedNode.dv;
        if (currentNode.attachedGizmo != null) {
          currentNode.attachedGizmo.DeltaV = newdv;
          currentNode.attachedGizmo.UT = currentSavedNode.ut;
        }
        currentNode.OnGizmoUpdated (newdv, currentSavedNode.ut);
      }
      currentSavedNode.updateOrig (currentNode);
    }
    if (changed || !origSame)
      notifyDvUTChanged ();
  }

  internal void clear () {
    _currentNode = null;
    currentNodeIdx = -1;
  }

  #endregion

  #region EventListeners

  private enum changeType {
    dvut,
    index,
    target
  }

  private Dictionary<changeType,List<Action>> _listeners;

  private Dictionary<changeType, List<Action>> listeners {
    get {
      if (_listeners == null) {
        _listeners = new Dictionary<changeType, List<Action>> (3);
        _listeners[changeType.dvut] = new List<Action> ();
        _listeners[changeType.index] = new List<Action> ();
        _listeners[changeType.target] = new List<Action> ();
      }
      return _listeners;
    }
  }

  public void listenToIdxChange (Action listener) {
    listeners[changeType.index].Add (listener);
  }
  public void listenToValuesChange (Action listener) {
    listeners[changeType.dvut].Add (listener);
  }
  public void listenToTargetChange (Action listener) {
    listeners[changeType.target].Add (listener);
  }

  public void removeListener (Action listener) {
    foreach (var list in listeners.Values)
      list.RemoveAll (a => (a == listener));
  }

  private void notifyNodeChanged () {
    /* update savednode */
    if (_currentSavedNode != null)
      _currentSavedNode.updateOrig (currentNode);
    else
      _currentSavedNode = new SavedNode (currentNode);
    /* update KAC alarm */
    if (KACWrapper.APIReady)
      currentAlarm = KACWrapper.KAC.Alarms.FirstOrDefault
                                  (a => ((Math.Abs (a.AlarmTime + 600.0 - currentNode.UT) < 1E-05) &&
                                         (a.VesselID == FlightGlobals.ActiveVessel.id.ToString ()) &&
                                         (a.AlarmType == KACWrapper.KACAPI.AlarmTypeEnum.Maneuver)));

    /* if the node changed, its values changed too */
    notifyDvUTChanged ();
  }

  private void notifyDvUTChanged () {
    foreach (var act in listeners[changeType.dvut])
      act ();
  }

  private void notifyIndexChanged () {
    foreach (var act in listeners[changeType.index])
      act ();
  }

  private void notifyTargetChanged () {
    foreach (var act in listeners[changeType.target])
      act ();
  }

    #endregion

}
}
