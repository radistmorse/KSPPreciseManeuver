using System;
using System.Linq;

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
internal class NodeManager {

  static private NodeManager _instance;
  static internal NodeManager getInstance () {
    if (_instance == null)
      _instance = new NodeManager ();
    return _instance;
  }

  private class SavedNode {
    private double dx;
    private double dy;
    private double dz;
    private double origdx;
    private double origdy;
    private double origdz;
    private double _ut;
    private double _origUt;
    private const double epsilon = 1E-05;

    internal Vector3d dv { get { return new Vector3d (dx, dy, dz); } }
    internal double ut { get { return _ut; } }
    internal bool changed { get; private set; } = false;

    internal SavedNode (ManeuverNode node) {
      dx = node.DeltaV.x;
      dy = node.DeltaV.y;
      dz = node.DeltaV.z;
      origdx = node.DeltaV.x;
      origdy = node.DeltaV.y;
      origdz = node.DeltaV.z;
      _ut = node.UT;
      _origUt = node.UT;
    }

    internal void update (double dvx, double dvy, double dvz, double ut) {
      if (Math.Abs (dx - dvx) > epsilon) {
        dx = dvx;
        changed = true;
      }
      if (Math.Abs (dy - dvy) > epsilon) {
        dy = dvy;
        changed = true;
      }
      if (Math.Abs (dz - dvz) > epsilon) {
        dz = dvz;
        changed = true;
      }
      if (Math.Abs (_ut - ut) > epsilon) {
        _ut = ut;
        changed = true;
      }
    }

    internal void updateOrig (ManeuverNode node) {
      origdx = node.DeltaV.x;
      origdy = node.DeltaV.y;
      origdz = node.DeltaV.z;
      _origUt = node.UT;
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
              Math.Abs (_origUt - node.UT) < epsilon);
    }
  }

  private ManeuverNode currentNode = null;
  private SavedNode currentSavedNode = null;
  private KACWrapper.KACAPI.KACAlarm currentAlarm = null;

  internal void init () {
    KACWrapper.InitKACWrapper();
  }

  internal bool alarmPluginEnabled() {
    return KACWrapper.APIReady;
  }

  private void changeCurrentNode (ManeuverNode node) {
    currentNode = node;
    if (currentSavedNode != null)
      currentSavedNode.updateOrig (node);
    else
      currentSavedNode = new SavedNode (node);
    if (KACWrapper.APIReady)
      currentAlarm = KACWrapper.KAC.Alarms.FirstOrDefault
                                  (a => ((Math.Abs (a.AlarmTime + 600.0 - node.UT) < 1E-05) &&
                                         (a.VesselID == FlightGlobals.ActiveVessel.id.ToString ()) &&
                                         (a.AlarmType == KACWrapper.KACAPI.AlarmTypeEnum.Maneuver)));
  }

  internal void createAlarm (ManeuverNode node) {
    if (node != currentNode)
      changeCurrentNode (node);

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

  internal void deleteAlarm (ManeuverNode node) {
    if (node != currentNode)
      changeCurrentNode (node);

    if (currentAlarm != null) {
      KACWrapper.KAC.DeleteAlarm (currentAlarm.ID);
      currentAlarm = null;
    }
  }

  internal bool alarmCreated (ManeuverNode node) {
    if (node != currentNode)
      changeCurrentNode (node);
    return currentAlarm != null;
  }

  internal bool unchanged (ManeuverNode node) {
    if (node != currentNode)
     return false;
    return (currentSavedNode != null) && !currentSavedNode.changed;
  }

  internal void changeNode (ManeuverNode node, double dvx, double dvy, double dvz, double ut) {
    if (node != currentNode)
      changeCurrentNode (node);

    currentSavedNode.update (dvx, dvy, dvz, ut);
  }

  internal void updateNodes() {
    if (currentNode == null)
      return;

    if (FlightGlobals.ActiveVessel.patchedConicSolver.maneuverNodes.Count == 0) {
      currentNode = null;
      currentSavedNode = null;
      currentAlarm = null;
      return;
    }

    if (currentSavedNode.changed) {
      if (currentSavedNode.origSame (currentNode)) {
        Vector3d newdv = currentSavedNode.dv;
        if (currentNode.attachedGizmo != null) {
          currentNode.attachedGizmo.DeltaV = newdv;
          currentNode.attachedGizmo.UT = currentSavedNode.ut;
        }
        currentNode.OnGizmoUpdated (newdv, currentSavedNode.ut);
      }
      currentSavedNode.updateOrig (currentNode);
    }
  }
}
}
