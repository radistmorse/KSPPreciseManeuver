using System;
using System.Collections.Generic;
using System.Linq;
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
internal class MainWindow {

  /* some globals for convenience*/
  private PreciseManeuverConfig config = PreciseManeuverConfig.getInstance();
  private NodeManager nodeManager = NodeManager.getInstance();
  private PatchedConicSolver solver = null;

  /* current node we're working with*/
  private ManeuverNode currentNode = null;
  internal int currentNodeIdx { get; private set; } = -1;
  private int nodeCount = -1;

  /* parameters of the window*/
  private bool showAngles = false;
  private bool showOrbit = false;
  private bool showManeuvers = false;
  private int nodeCountShow = -1;

  /* delay counters */
  private bool repeatButtonPressed = false;
  private int repeatButtonPressInterval = 0;
  private int repeatButtonReleaseInterval = 0;
  private int waitForGizmo = 0;
  private bool repeatButtonDelay () {
    repeatButtonPressed = true;
    if (repeatButtonPressInterval > 20 || repeatButtonPressInterval == 0)
      return true;
    else
      return false;
  }

  /* GUI size*/
  private const int buttonSize = 40;
  private const int bigButtonSize = 100;
  private const int leftLabelSize = 100;
  private const int verticalSpace = 10;

  /* GUI colors*/
  private readonly Color PROGRADE_COLOR = new Color (0, 1, 0);
  private readonly Color NORMAL_COLOR = new Color (1, 0, 1);
  private readonly Color RADIAL_COLOR = new Color (0, 1, 1);
  private GUIStyle[] progradeStyle = null;
  private GUIStyle[] normalStyle = null;
  private GUIStyle[] radialStyle = null;

  /* helper class to cache the string representation of doubles */
  private class FastString {
    internal string value = "N/A";
    private double previous = double.NaN;
    private double current = double.NaN;
    private const double epsilon = 1E-03;
    internal bool update (double value, bool distance = true, string postfix = "") {
      previous = current;
      current = distance ? value : Math.Abs (value);
      if (!double.IsNaN (current) && (double.IsNaN (previous) || Math.Abs (previous - current) > epsilon)) {
        this.value = distance ? NodeTools.formatMeters (current) : current.ToString ("0.##") + postfix;
        return true;
      }
      if (double.IsNaN (current) && !double.IsNaN (previous)) {
        this.value = "N/A";
        return true;
      }
      return false;
    }
  }
  private FastString eAngle = new FastString ();
  private string eAngleStr = "N/A";
  private FastString eIncl = new FastString ();
  private string eInclStr = "N/A";
  private FastString nodeDVX = new FastString ();
  private FastString nodeDVY = new FastString ();
  private FastString nodeDVZ = new FastString ();
  private FastString nodeUT = new FastString ();
  private FastString nodeAp = new FastString ();
  private FastString nodePe = new FastString ();
  private FastString nodeInc = new FastString ();
  private FastString totalDV = new FastString ();

  /* caches for string representation for nodes list */
  private IDictionary<long, string> humanTimes = new Dictionary<long, string> ();
  private IDictionary<long, string> dVs = new Dictionary<long, string> ();
  private string getDV (double dv) {
    if (dVs.ContainsKey ((long)(dv*1000)))
      return dVs[(long)(dv*1000)];
    if (dVs.Count > 100)
      dVs.Clear();
    string tmp = dv.ToString ("0.##") + " m/s";
    humanTimes[(long)(dv*1000)] = tmp;
    return tmp;
  }
  private string getHumanTime (double UT) {
    if (humanTimes.ContainsKey ((long)UT))
      return humanTimes[(long)UT];
    if (humanTimes.Count > 100)
      humanTimes.Clear();
    string tmp = NodeTools.convertUTtoHumanTime (UT);
    humanTimes[(long)UT] = tmp;
    return tmp;
  }

  private bool nodeRecentlyAdded = false;
  private List<ManeuverNode> prevGizmos = null;

  internal void updateValues () {
    solver = FlightGlobals.ActiveVessel.patchedConicSolver;
    int tmp = solver.maneuverNodes.Count;
    if (tmp > nodeCount)
      nodeRecentlyAdded = true;
    nodeCount = tmp;

    /* wait for a couple of frames for gizmo to be created */
    if (Input.GetMouseButtonUp (0))
      waitForGizmo = 3;
    /* select the node which was recently selected in the mapview */
    if (waitForGizmo > 0) {
      if (waitForGizmo == 1) {
        var curList = solver.maneuverNodes.Where (a => a.attachedGizmo != null);
        var tmp2 = curList.ToList ();
        /* first, if the node was just created, shoose it */
        if (nodeRecentlyAdded) {
          currentNode = solver.maneuverNodes.Last();
          nodeRecentlyAdded = false;
        } else {
          /* then, let's see if user is hovering a mouse *
           * over any gizmo. That would be a hint.       */
          if (curList.Count (a => a.attachedGizmo.MouseOverGizmo) == 1) {
            currentNode = curList.First (a => a.attachedGizmo.MouseOverGizmo);
          } else {
            /* finally, let's see if we can find any  *
             * new gizmos that were created recently. */
            if (prevGizmos != null)
              curList = curList.Except (prevGizmos);
            if (curList.Count () == 1)
              currentNode = curList.First ();
          }
        }
        prevGizmos = tmp2;
      }
      waitForGizmo--;
    }

    /* setting the current node */
    int idx = -1;
    /* first, let's see if we already have a valid node */
    if (currentNode != null)
      idx = solver.maneuverNodes.IndexOf (currentNode);

    if (idx != -1) {
      currentNodeIdx = idx;
    } else {
      /* if no, let's see if our index is still good */
      if (currentNodeIdx != -1 && currentNodeIdx < solver.maneuverNodes.Count) {
        currentNode = solver.maneuverNodes[currentNodeIdx];
      } else {
        /* if no, pick the first node */
        if (nodeCount > 0) {
          currentNode = solver.maneuverNodes[0];
          currentNodeIdx = 0;
        } else {
          currentNode = null;
          currentNodeIdx = -1;
        }
      }
    }

    if (currentNode == null)
      return;

    if (showAngles) {
      if (!FlightGlobals.ActiveVessel.orbit.referenceBody.isSun ()) {
        double eang = FlightGlobals.ActiveVessel.orbit.getEjectionAngle (currentNode);
        if (eAngle.update (eang, true))
          if (double.IsNaN (eang)) {
            eAngleStr = "N/A";
          } else {
            eAngleStr = eAngle.value + "° from " + ((eang >= 0) ? "prograde" : "retrograde");
          }
      }

      if (!FlightGlobals.ActiveVessel.orbit.referenceBody.isSun ()) {
        double eincl = FlightGlobals.ActiveVessel.orbit.getEjectionInclination (currentNode);
        if (eIncl.update (eincl, true))
          if (double.IsNaN (eincl)) {
            eInclStr = "N/A";
          } else {
            eInclStr = eIncl.value + "° " + ((eincl >= 0) ? "north" : "south");
          }
      }
    }

    if (showOrbit) {
      if (currentNode.solver.flightPlan.Count > 1) {
        nodeAp.update (currentNode.nextPatch.ApA, true, "m");
        nodePe.update (currentNode.nextPatch.PeA, true, "m");
        nodeInc.update (currentNode.nextPatch.inclination, false, "°");
      } else {
        nodeAp.update (double.NaN);
        nodePe.update (double.NaN);
        nodeInc.update (double.NaN);
      }
    }

    nodeUT.update (currentNode.UT, false);
    nodeDVX.update (currentNode.DeltaV.x);
    nodeDVY.update (currentNode.DeltaV.y);
    nodeDVZ.update (currentNode.DeltaV.z);
    totalDV.update (currentNode.DeltaV.magnitude, false, " m/s");
  }

  private static void drawDoubleLabel (String text1, String text2) {
    GUILayout.BeginHorizontal();
    GUILayout.Label (text1, GUILayout.Width (leftLabelSize));
    GUILayout.Label (text2, GUILayout.ExpandWidth (true));
    GUILayout.EndHorizontal ();
  }

  private void drawManeuverPager () {

    int pageButtonSize = (leftLabelSize + 5 * buttonSize + 2 * GUI.skin.button.margin.left - bigButtonSize - bigButtonSize / 2) / 2;

    GUILayout.BeginHorizontal ();

    GUI.enabled = currentNodeIdx > 0;
    if (GUILayout.Button ("◀", GUILayout.Width (pageButtonSize))) {
      currentNodeIdx--;
      currentNode = solver.maneuverNodes[currentNodeIdx];
    }
    GUI.enabled = true;

    GUI.enabled = (currentNodeIdx != -1);
    if (GUILayout.Button ("Node " + (currentNodeIdx + 1), GUILayout.Width (bigButtonSize))) {
      MapView.MapCamera.SetTarget (currentNode.scaledSpaceTarget);
    }

    GUI.enabled = (currentNodeIdx == nodeCount - 1);
    Color oldContentColor = GUI.contentColor;
    GUI.contentColor = Color.red;
    if (GUILayout.Button ("Del", GUILayout.Width (bigButtonSize/2))) {
      solver.RemoveManeuverNode (currentNode);
      nodeCount--;
      currentNodeIdx--;
      if (currentNodeIdx != -1)
        currentNode = solver.maneuverNodes[currentNodeIdx];
      else
        currentNode = null;
    }
    GUI.contentColor = oldContentColor;

    GUI.enabled = currentNodeIdx < nodeCount - 1;
    if (GUILayout.Button ("▶", GUILayout.Width (pageButtonSize))) {
      currentNodeIdx++;
      currentNode = solver.maneuverNodes[currentNodeIdx];
    }
    GUI.enabled = true;
    GUILayout.EndHorizontal ();
  }

  private double drawTimeControls () {
    Orbit target = null;

    GUI.enabled = currentNode != null;

    GUILayout.BeginHorizontal ();
    GUILayout.Label ("UT:", GUILayout.Width (leftLabelSize));
    GUILayout.BeginVertical ();
    GUILayout.BeginHorizontal ();

    double ut = 0;
    if (currentNode != null) {
      ut = currentNode.UT;
    }
    GUILayout.TextField (nodeUT.value, GUILayout.Width (buttonSize * 2 + GUI.skin.button.margin.left));

    if (GUILayout.RepeatButton ("+", GUILayout.Width (buttonSize))) {
      if (repeatButtonDelay ())
        ut += config.increment * (config.x10UTincrement ? 10 : 1);
    }
    if (GUILayout.RepeatButton ("-", GUILayout.Width (buttonSize))) {
      if (repeatButtonDelay ())
        ut -= config.increment * (config.x10UTincrement ? 10 : 1);
    }
    GUILayout.EndHorizontal ();
    GUILayout.BeginHorizontal ();
    config.x10UTincrement = GUILayout.Toggle (config.x10UTincrement,"x10","button", GUILayout.Width (buttonSize));
    GUI.enabled = currentNode != null && currentNode.patch.isClosed ();
    if (GUILayout.Button ("Ap", GUILayout.Width (buttonSize)))
      ut = Planetarium.GetUniversalTime () + currentNode.patch.timeToAp;
    GUI.enabled = currentNode != null;
    if (GUILayout.Button ("Pe", GUILayout.Width (buttonSize)))
      ut = Planetarium.GetUniversalTime () + currentNode.patch.timeToPe;

    if (currentNode != null)
      target = NodeTools.getTargetOrbit ();

    GUI.enabled = target != null;
    if (GUILayout.Button ("AN", GUILayout.Width (buttonSize))) {
      ut = currentNode.patch.getTargetANUT (target);
    }
    if (GUILayout.Button ("DN", GUILayout.Width (buttonSize))) {
      ut = currentNode.patch.getTargetDNUT (target);
    }
    GUI.enabled = currentNode != null;

    GUILayout.EndHorizontal ();
    GUILayout.EndVertical ();
    GUILayout.EndHorizontal ();

    GUI.enabled = true;
    return ut;
  }

  private double drawAxisControls (string name, string text, double init, GUIStyle[] style) {
    double rez = init;

    GUILayout.BeginHorizontal ();
    GUILayout.Label (name, style[0], GUILayout.Width (leftLabelSize));
    GUILayout.TextField (text, style[1], GUILayout.Width (buttonSize * 2 + GUI.skin.button.margin.left));
    if (GUILayout.RepeatButton ("+", GUILayout.Width (buttonSize))) {
      if (repeatButtonDelay ())
        rez += config.increment;
    }
    if (GUILayout.RepeatButton ("-", GUILayout.Width (buttonSize))) {
      if (repeatButtonDelay ())
        rez -= config.increment;
    }
    if (GUILayout.Button ("0", GUILayout.Width (buttonSize))) {
      rez = 0;
    }
    GUILayout.EndHorizontal ();
    return rez;
  }

  private void drawEncounter () {
    Orbit nextEnc = null;
    string theName = "N/A";

    int labelSize = 5 * buttonSize + 3 * GUI.skin.button.margin.left - bigButtonSize;

    if (currentNode != null)
      nextEnc = currentNode.findNextEncounter ();

    if (nextEnc != null)
      theName = nextEnc.referenceBody.theName;

    GUILayout.BeginHorizontal();
    GUILayout.Label ("Next encounter:", GUILayout.Width (leftLabelSize));
    GUILayout.Label (theName, GUILayout.Width (labelSize));
    GUI.enabled = nextEnc != null;
    if (GUILayout.Button ("Focus", GUILayout.Width (bigButtonSize))) {
      MapObject mapObject = PlanetariumCamera.fetch.targets.Find (o => (o.celestialBody != null) && (o.celestialBody == nextEnc.referenceBody));
      MapView.MapCamera.SetTarget (mapObject);
    }
    GUI.enabled = true;
    GUILayout.EndHorizontal ();
  }

  private void drawConicsControls () {
    GUILayout.BeginHorizontal ();
    GUILayout.Label ("Orbit mode: ", GUILayout.Width (leftLabelSize));
    GUILayout.BeginVertical ();
    GUILayout.BeginHorizontal ();

    int currentMode = NodeTools.getConicsMode ();

    String[] modes = { "loc", "ent", "ext", "rel", "dyn" };

    for (int mode = 0; mode <= 4; mode++) {
      if (GUILayout.Toggle ((currentMode == mode), modes[mode], "button", GUILayout.Width (buttonSize))) {
        if (currentMode != mode)
          NodeTools.setConicsMode (mode);
      }
    }

    GUILayout.EndHorizontal ();

    GUILayout.BeginHorizontal ();
    int orbButtonlSize = (5 * buttonSize + 3 * GUI.skin.button.margin.left) / 2;

    if (GUILayout.Button ("+ orbits", GUILayout.Width (orbButtonlSize)))
      solver.IncreasePatchLimit();
    if (GUILayout.Button ("- orbits", GUILayout.Width (orbButtonlSize)))
      solver.DecreasePatchLimit();

    GUILayout.EndHorizontal ();
    GUILayout.EndVertical ();
    GUILayout.EndHorizontal ();
  }

  private void drawNodeSummary (ManeuverNode node) {
    bool isNodeCurrent = node == currentNode;
    GUILayout.BeginHorizontal ();
    if (GUILayout.Toggle (isNodeCurrent, "Select", "button", GUILayout.ExpandHeight (true)) && !isNodeCurrent)
      currentNode = node;
    GUILayout.BeginVertical ();
    GUILayout.BeginHorizontal ();
    GUILayout.Label ("Time:", GUILayout.Width (buttonSize));
    GUILayout.Label (getHumanTime (node.UT), GUILayout.ExpandWidth (true));
    GUILayout.EndHorizontal ();
    GUILayout.BeginHorizontal ();
    GUILayout.Label ("dV:", GUILayout.Width (buttonSize));
    GUILayout.Label (getDV (node.DeltaV.magnitude), GUILayout.ExpandWidth (true));
    GUILayout.EndHorizontal ();
    GUILayout.EndVertical ();
    GUILayout.EndHorizontal ();
  }

  internal void draw () {
    repeatButtonPressed = false;

    GUI.skin = config.skin;

    if (progradeStyle == null) {
      progradeStyle = new GUIStyle[] { new GUIStyle (GUI.skin.label), new GUIStyle (GUI.skin.textField) };
      normalStyle   = new GUIStyle[] { new GUIStyle (GUI.skin.label), new GUIStyle (GUI.skin.textField) };
      radialStyle   = new GUIStyle[] { new GUIStyle (GUI.skin.label), new GUIStyle (GUI.skin.textField) };
      progradeStyle[0].normal.textColor = PROGRADE_COLOR;
      progradeStyle[1].normal.textColor = PROGRADE_COLOR;
      normalStyle[0].normal.textColor = NORMAL_COLOR;
      normalStyle[1].normal.textColor = NORMAL_COLOR;
      radialStyle[0].normal.textColor = RADIAL_COLOR;
      radialStyle[1].normal.textColor = RADIAL_COLOR;
    }

    /* Keymapping button */
    config.showKeymapperWindow =
      GUI.Toggle (new Rect (config.mainWindowPos.width - 24, 2, 22, 18),
            config.showKeymapperWindow, "K", "button");

    /* maneuver pager */
    this.drawManeuverPager ();

    /* maneuver time and alarm button */
    int labelSize = (int)(3.5 * buttonSize) + 3 * GUI.skin.button.margin.left;
    GUILayout.BeginHorizontal ();
    GUILayout.Label ("Time:", GUILayout.Width (leftLabelSize));
    GUILayout.Label ((currentNode != null) ? NodeTools.convertUTtoHumanTime (currentNode.UT) : "N/A", GUILayout.Width (labelSize));
    bool alarmCreated = (currentNode != null) && nodeManager.alarmCreated (currentNode);
    GUI.enabled = (currentNode != null) && nodeManager.alarmPluginEnabled ();
    if (GUILayout.Toggle (alarmCreated, "Alarm", "button", GUILayout.Width (1.5f * buttonSize))) {
      if (!alarmCreated)
        nodeManager.createAlarm (currentNode);
    } else {
      if (alarmCreated)
        nodeManager.deleteAlarm (currentNode);
    }
    GUI.enabled = true;
    GUILayout.EndHorizontal ();

    /* increment buttons */
    GUILayout.BeginHorizontal ();
    GUILayout.Label ("Increment:", GUILayout.Width (leftLabelSize));
    if (GUILayout.Toggle ((config.incrementRaw == -2), "0.01", "button", GUILayout.Width (buttonSize)))
      config.incrementRaw = -2;
    if (GUILayout.Toggle ((config.incrementRaw == -1), "0.1", "button", GUILayout.Width (buttonSize)))
      config.incrementRaw = -1;
    if (GUILayout.Toggle ((config.incrementRaw == 0), "1", "button", GUILayout.Width (buttonSize)))
      config.incrementRaw = 0;
    if (GUILayout.Toggle ((config.incrementRaw == 1), "10", "button", GUILayout.Width (buttonSize)))
      config.incrementRaw = 1;
    if (GUILayout.Toggle ((config.incrementRaw == 2), "100", "button", GUILayout.Width (buttonSize)))
      config.incrementRaw = 2;
    GUILayout.EndHorizontal ();

    GUILayout.Space (verticalSpace);

    /* time control panel */
    double ut = this.drawTimeControls ();

    GUILayout.Space (verticalSpace);

    double dx = 0;
    double dy = 0;
    double dz = 0;

    /* axis control panels */
    if (currentNode != null) {
      dz = drawAxisControls ("Prograde:", nodeDVZ.value, currentNode.DeltaV.z, progradeStyle);
      dy = drawAxisControls ("Normal:", nodeDVY.value, currentNode.DeltaV.y, normalStyle);
      dx = drawAxisControls ("Radial:", nodeDVX.value, currentNode.DeltaV.x, radialStyle);
    } else {
      drawAxisControls ("Prograde:", "0", 0, progradeStyle);
      drawAxisControls ("Normal:", "0", 0, normalStyle);
      drawAxisControls ("Radial:", "0", 0, radialStyle);
    }

    GUILayout.Space (verticalSpace);

    /* maneuver & orbit info */
    labelSize = 2*buttonSize + 2 * GUI.skin.button.margin.left;
    GUILayout.BeginHorizontal ();
    GUILayout.Label ("Total Δv:", GUILayout.Width (100));
    GUILayout.Label (totalDV.value, GUILayout.Width (labelSize));
    bool showAnglesPrevious = showAngles;
    bool showOrbitPrevious = showOrbit;
    this.showOrbit = GUILayout.Toggle (this.showOrbit, "Orbit", "button", GUILayout.Width (1.5f*buttonSize));
    this.showAngles = GUILayout.Toggle (this.showAngles, "Angles", "button", GUILayout.Width (1.5f*buttonSize));
    GUILayout.EndHorizontal ();

    if (showAngles) {
      MainWindow.drawDoubleLabel ("Ejection angle:", eAngleStr);
      MainWindow.drawDoubleLabel ("Eject. inclination:", eInclStr);
    }

    if (showOrbit) {
      MainWindow.drawDoubleLabel ("Apoapsis:", nodeAp.value);
      MainWindow.drawDoubleLabel ("Periapsis:", nodePe.value);
      MainWindow.drawDoubleLabel ("Inclination:", nodeInc.value);
    }

    /* next encounter info */
    this.drawEncounter ();

    GUILayout.Space (verticalSpace);

    /* conics mode & number */
    this.drawConicsControls ();

    GUILayout.Space (verticalSpace);

    /* bottom buttons */
    GUILayout.BeginHorizontal ();
    int endButtonSize = (leftLabelSize + 5 * buttonSize + 4 * GUI.skin.button.margin.left) / 2;
    bool showManueversPrev = showManeuvers;
    showManeuvers = GUILayout.Toggle (showManeuvers, "Show the List of\nManeuvers", "button", GUILayout.Width (endButtonSize));

    if (GUILayout.Button ("Focus on\nVessel", GUILayout.Width (endButtonSize))) {
      MapObject mapObject = PlanetariumCamera.fetch.targets.Find (o => (o.vessel != null) && o.vessel.Equals (FlightGlobals.ActiveVessel));
      MapView.MapCamera.SetTarget (mapObject);
    }

    GUILayout.EndHorizontal ();

    if (currentNode != null)
      nodeManager.changeNode (currentNode, dx, dy, dz, ut);

    /* list of maneuvers */
    if (showManeuvers) {
      GUILayout.Space (verticalSpace);
      foreach (var node in solver.maneuverNodes) {
        drawNodeSummary (node);
      }
    }

    if ((showAnglesPrevious != showAngles) ||
        (showOrbitPrevious != showOrbit) ||
        (showManueversPrev != showManeuvers) ||
        (showManeuvers && (nodeCount != nodeCountShow)))
      config.readjustMainWindow ();
    nodeCountShow = nodeCount;

    if (repeatButtonPressed) {
      if (repeatButtonPressInterval < 100)
        repeatButtonPressInterval++;
      repeatButtonReleaseInterval = 0;
    } else {
      if (repeatButtonReleaseInterval < 5)
        repeatButtonReleaseInterval++;
      else
        repeatButtonPressInterval = 0;
    }

    GUI.DragWindow ();
  }
}
}
