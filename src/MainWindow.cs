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
using System.Linq;
using UnityEngine;

namespace KSPPreciseManeuver {
using UI;
internal class MainWindow {
  /* some globals for convenience*/
  private PreciseManeuverConfig config = PreciseManeuverConfig.Instance;
  private NodeManager nodeManager = NodeManager.Instance;
  private PatchedConicSolver solver = null;

  /* current node we're working with*/
  private ManeuverNode currentNode = null;
  internal int currentNodeIdx { get; private set; } = -1;
  private int nodeCount = -1;

  /* parameters of the window*/
  private bool showEject = false;
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
  private const int leftLabelSize = 120;
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
    internal string value { get; private set; } = "N/A";
    private double previous = double.NaN;
    private double current = double.NaN;
    private const double epsilon = 1E-03;
    private string format = "{0}";
    internal FastString () { }
    internal FastString (string format) {
      this.format = format;
    }
    internal bool update (double value, bool abs = false, bool abbriv = false) {
      previous = current;
      current = abs ? value : Math.Abs (value);
      if (!double.IsNaN (current) && (double.IsNaN (previous) || Math.Abs (previous - current) > epsilon)) {
        if (abbriv)
          this.value = String.Format (format, NodeTools.formatMeters (current));
        else
          this.value = String.Format (format, current);
        return true;
      }
      if (double.IsNaN (current) && !double.IsNaN (previous)) {
        this.value = "N/A";
        return true;
      }
      return false;
    }
  }
  private FastString eAngle = new FastString ("{0:0.0° from prograde;0.0° from retrograde}");
  private string eAngleStr = "N/A";
  private FastString eIncl = new FastString ("{0:0.0° north;0.0° south}");
  private string eInclStr = "N/A";
  private FastString nodeDVX = new FastString ();
  private FastString nodeDVY = new FastString ();
  private FastString nodeDVZ = new FastString ();
  private FastString nodeUT = new FastString ();
  private FastString orbitAp = new FastString ();
  private FastString orbitPe = new FastString ();
  private FastString orbitInc = new FastString ("{0}°");
  private FastString totalDV = new FastString ("{0} m/s");

  /* caches for string representation for nodes list */
  private IDictionary<long, string> humanTimes = new Dictionary<long, string> ();

  private IDictionary<long, string> dVs = new Dictionary<long, string> ();
  private string getDV (double dv) {
    if (dVs.ContainsKey ((long)(dv * 1000)))
      return dVs[(long)(dv * 1000)];
    if (dVs.Count > 100)
      dVs.Clear ();
    string tmp = dv.ToString ("0.##") + " m/s";
    humanTimes[(long)(dv * 1000)] = tmp;
    return tmp;
  }
  private string getHumanTime (double UT) {
    if (humanTimes.ContainsKey ((long)UT))
      return humanTimes[(long)UT];
    if (humanTimes.Count > 100)
      humanTimes.Clear ();
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
          currentNode = solver.maneuverNodes.Last ();
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

    if (showEject) {
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
        orbitAp.update (currentNode.nextPatch.ApA, true, true);
        orbitPe.update (currentNode.nextPatch.PeA, true, true);
        orbitInc.update (currentNode.nextPatch.inclination, false, false);
      } else {
        orbitAp.update (double.NaN);
        orbitPe.update (double.NaN);
        orbitInc.update (double.NaN);
      }
    }

    nodeUT.update (currentNode.UT, false);
    nodeDVX.update (currentNode.DeltaV.x);
    nodeDVY.update (currentNode.DeltaV.y);
    nodeDVZ.update (currentNode.DeltaV.z);
    totalDV.update (currentNode.DeltaV.magnitude);
  }

  private static void drawDoubleLabel (String text1, String text2) {
    GUILayout.BeginHorizontal ();
    GUILayout.Label (text1, GUILayout.Width (leftLabelSize));
    GUILayout.Label (text2, GUILayout.ExpandWidth (true));
    GUILayout.EndHorizontal ();
  }

  private void drawManeuverPager () {

    int pageButtonSize = (leftLabelSize + 5 * buttonSize + 2 * GUI.skin.button.margin.left - bigButtonSize - bigButtonSize / 2) / 2;

    GUILayout.BeginHorizontal ();

    GUI.enabled = currentNodeIdx > 0;
    if (GUILayout.Button ("◄", GUILayout.Width (pageButtonSize)) ||
        (currentNodeIdx > 0 && config.isHotkeyRegistered (PreciseManeuverConfig.HotkeyType.PREVMAN))) {
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
    if (GUILayout.Button ("Del", GUILayout.Width (bigButtonSize / 2)) ||
        ((currentNodeIdx == nodeCount - 1) && config.isHotkeyRegistered (PreciseManeuverConfig.HotkeyType.MNVRDEL))) {
      currentNode.RemoveSelf ();
      nodeCount--;
      currentNodeIdx--;
      if (currentNodeIdx != -1)
        currentNode = solver.maneuverNodes[currentNodeIdx];
      else
        currentNode = null;
    }
    GUI.contentColor = oldContentColor;

    GUI.enabled = currentNodeIdx < nodeCount - 1;
    if (GUILayout.Button ("►", GUILayout.Width (pageButtonSize)) ||
        ((currentNodeIdx < nodeCount - 1) && config.isHotkeyRegistered (PreciseManeuverConfig.HotkeyType.NEXTMAN))) {
      currentNodeIdx++;
      currentNode = solver.maneuverNodes[currentNodeIdx];
    }
    GUI.enabled = true;
    GUILayout.EndHorizontal ();
  }

  private double drawTimeControls (ref bool changed) {
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
    GUILayout.TextField (nodeUT.value, GUILayout.Width (buttonSize * 3 + GUI.skin.button.margin.left * 2));

    if (GUILayout.RepeatButton ("+", GUILayout.Width (buttonSize))) {
      if (repeatButtonDelay ()) {
        ut += config.increment * (config.x10UTincrement ? 10 : 1);
        changed = true;
      }
    }
    if (GUILayout.RepeatButton ("-", GUILayout.Width (buttonSize))) {
      if (repeatButtonDelay ()) {
        ut -= config.increment * (config.x10UTincrement ? 10 : 1);
        changed = true;
      }
    }
    GUILayout.EndHorizontal ();
    GUILayout.BeginHorizontal ();
    config.x10UTincrement = GUILayout.Toggle (config.x10UTincrement, "x10", "button", GUILayout.Width (buttonSize));
    GUI.enabled = currentNode != null && currentNode.patch.isClosed ();
    if (GUILayout.Button ("Ap", GUILayout.Width (buttonSize))) {
      ut = currentNode.patch.StartUT + currentNode.patch.timeToAp;
      changed = true;
    }
    GUI.enabled = currentNode != null;
    if (GUILayout.Button ("Pe", GUILayout.Width (buttonSize))) {
      ut = currentNode.patch.StartUT + currentNode.patch.timeToPe;
      changed = true;
    }

    if (currentNode != null)
      target = NodeTools.getTargetOrbit ();

    GUI.enabled = target != null;
    if (GUILayout.Button ("AN", GUILayout.Width (buttonSize))) {
      ut = currentNode.patch.getTargetANUT (target);
      changed = true;
    }
    if (GUILayout.Button ("DN", GUILayout.Width (buttonSize))) {
      ut = currentNode.patch.getTargetDNUT (target);
      changed = true;
    }
    GUI.enabled = currentNode != null;

    GUILayout.EndHorizontal ();
    GUILayout.EndVertical ();
    GUILayout.EndHorizontal ();

    GUI.enabled = true;
    return ut;
  }

  private double drawAxisControls (string name, string text, double init, GUIStyle[] style, ref bool changed) {
    double rez = init;

    GUILayout.BeginHorizontal ();
    GUILayout.Label (name, style[0], GUILayout.Width (leftLabelSize));
    GUILayout.TextField (text, style[1], GUILayout.Width (buttonSize * 2 + GUI.skin.button.margin.left));
    if (GUILayout.RepeatButton ("+", GUILayout.Width (buttonSize))) {
      if (repeatButtonDelay ()) {
        rez += config.increment;
        changed = true;
      }
    }
    if (GUILayout.RepeatButton ("-", GUILayout.Width (buttonSize))) {
      if (repeatButtonDelay ()) {
        rez -= config.increment;
        changed = true;
      }
    }
    if (GUILayout.Button ("0", GUILayout.Width (buttonSize))) {
      rez = 0;
      changed = true;
    }
    GUILayout.EndHorizontal ();
    return rez;
  }

  private void drawEncounter () {
    CelestialBody nextEnc = null;
    string theName = "N/A";

    int labelSize = 5 * buttonSize + 3 * GUI.skin.button.margin.left - bigButtonSize;

    if (currentNode != null)
      nextEnc = NodeTools.findNextEncounter ();

    if (nextEnc != null)
      theName = nextEnc.theName;

    GUILayout.BeginHorizontal ();
    GUILayout.Label ("Next encounter:", GUILayout.Width (leftLabelSize));
    GUILayout.Label (theName, GUILayout.Width (labelSize));
    GUI.enabled = nextEnc != null;
    if (GUILayout.Button ("Focus", GUILayout.Width (bigButtonSize)) ||
        (nextEnc != null && config.isHotkeyRegistered (PreciseManeuverConfig.HotkeyType.FOCNENC))) {
      MapObject mapObject = PlanetariumCamera.fetch.targets.Find (o => (o.celestialBody != null) && (o.celestialBody == nextEnc));
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
      solver.IncreasePatchLimit ();
    if (GUILayout.Button ("- orbits", GUILayout.Width (orbButtonlSize)))
      solver.DecreasePatchLimit ();

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

    GUI.skin = null;

    if (progradeStyle == null) {
      progradeStyle = new GUIStyle[] { new GUIStyle (GUI.skin.label), new GUIStyle (GUI.skin.textField) };
      normalStyle = new GUIStyle[] { new GUIStyle (GUI.skin.label), new GUIStyle (GUI.skin.textField) };
      radialStyle = new GUIStyle[] { new GUIStyle (GUI.skin.label), new GUIStyle (GUI.skin.textField) };
      progradeStyle[0].normal.textColor = PROGRADE_COLOR;
      progradeStyle[1].normal.textColor = PROGRADE_COLOR;
      normalStyle[0].normal.textColor = NORMAL_COLOR;
      normalStyle[1].normal.textColor = NORMAL_COLOR;
      radialStyle[0].normal.textColor = RADIAL_COLOR;
      radialStyle[1].normal.textColor = RADIAL_COLOR;
    }

    /* Keymapping button */
    /*config.showKeymapperWindow =
      GUI.Toggle (new Rect (config.mainWindowPos.width - 24, 2, 22, 18),
            config.showKeymapperWindow, "K", "button");*/

    /* maneuver pager */
    this.drawManeuverPager ();

    /* maneuver time and alarm button */
    int labelSize = (int)(3.5 * buttonSize) + 3 * GUI.skin.button.margin.left;
    GUILayout.BeginHorizontal ();
    GUILayout.Label ("Time:", GUILayout.Width (leftLabelSize));
    GUILayout.Label ((currentNode != null) ? NodeTools.convertUTtoHumanTime (currentNode.UT) : "N/A", GUILayout.Width (labelSize));
    bool alarmCreated = (currentNode != null) && nodeManager.alarmCreated ();
    GUI.enabled = (currentNode != null);
    if (GUILayout.Toggle (alarmCreated, "Alarm", "button", GUILayout.Width (1.5f * buttonSize))) {
      if (!alarmCreated)
        nodeManager.createAlarm ();
    } else {
      if (alarmCreated)
        nodeManager.deleteAlarm ();
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

    double dx = 0;
    double dy = 0;
    double dz = 0;
    bool changed = false;

    /* time control panel */
    double ut = this.drawTimeControls (ref changed);

    GUILayout.Space (verticalSpace);


    /* axis control panels */
    if (currentNode != null) {
      dz = drawAxisControls ("Prograde:", nodeDVZ.value, currentNode.DeltaV.z, progradeStyle, ref changed);
      dy = drawAxisControls ("Normal:", nodeDVY.value, currentNode.DeltaV.y, normalStyle, ref changed);
      dx = drawAxisControls ("Radial:", nodeDVX.value, currentNode.DeltaV.x, radialStyle, ref changed);
    } else {
      drawAxisControls ("Prograde:", "0", 0, progradeStyle, ref changed);
      drawAxisControls ("Normal:", "0", 0, normalStyle, ref changed);
      drawAxisControls ("Radial:", "0", 0, radialStyle, ref changed);
    }

    GUILayout.Space (verticalSpace);

    /* maneuver & orbit info */
    labelSize = 2 * buttonSize + 2 * GUI.skin.button.margin.left;
    GUILayout.BeginHorizontal ();
    GUILayout.Label ("Total Δv:", GUILayout.Width (leftLabelSize));
    GUILayout.Label (totalDV.value, GUILayout.Width (labelSize));
    bool showEjectPrevious = showEject;
    bool showOrbitPrevious = showOrbit;

    this.showOrbit = GUILayout.Toggle (this.showOrbit, "Orbit", "button", GUILayout.Width (1.5f * buttonSize));
    this.showEject = GUILayout.Toggle (this.showEject, "Eject.", "button", GUILayout.Width (1.5f * buttonSize));
    GUILayout.EndHorizontal ();

    bool circ = false;
    bool up = false;
    bool down = false;

    if (showOrbit) {
      labelSize = 5 * buttonSize + 3 * GUI.skin.button.margin.left - bigButtonSize;
      int bigButtonHalved = (bigButtonSize - GUI.skin.button.margin.left) / 2;
      GUILayout.BeginHorizontal ();
      GUILayout.BeginVertical ();
      GUILayout.BeginHorizontal ();
      GUILayout.Label ("Apoapsis:", GUILayout.Width (leftLabelSize));
      GUILayout.Label (orbitAp.value, GUILayout.Width (labelSize));
      GUILayout.EndHorizontal ();
      GUILayout.BeginHorizontal ();
      GUILayout.Label ("Periapsis:", GUILayout.Width (leftLabelSize));
      GUILayout.Label (orbitPe.value, GUILayout.Width (labelSize));
      GUILayout.EndHorizontal ();
      GUILayout.EndVertical ();
      GUILayout.BeginVertical ();
      GUILayout.FlexibleSpace ();
      circ = GUILayout.Button ("Circularize\nOrbit", GUILayout.Width (bigButtonSize));
      GUILayout.FlexibleSpace ();
      GUILayout.EndVertical ();
      GUILayout.EndHorizontal ();
      GUILayout.BeginHorizontal ();
      GUILayout.Label ("Inclination:", GUILayout.Width (leftLabelSize));
      GUILayout.Label (orbitInc.value, GUILayout.Width (labelSize));
      up = GUILayout.RepeatButton ("▲", GUILayout.Width (bigButtonHalved));
      down = GUILayout.RepeatButton ("▼", GUILayout.Width (bigButtonHalved));
      GUILayout.EndHorizontal ();
    }

    if (currentNode != null && (circ || config.isHotkeyRegistered (PreciseManeuverConfig.HotkeyType.CIRCORB))) {
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

          dx = Vector3d.Dot (newDV, nrad);
          dy = Vector3d.Dot (newDV, nnorm);
          dz = Vector3d.Dot (newDV, nprog);
          changed = true;
        }
      }
      if (!changed) {
        // position and velocity are perfectly parallel (less probable)
        // or
        // KSP API returned NaN or some other weird shit (much more probable)
        ScreenMessages.PostScreenMessage ("Can't change the orbit, parameters are invalid", 2.0f, ScreenMessageStyle.UPPER_CENTER);
      }
    }

    up = (up && repeatButtonDelay ()) || config.isHotkeyRegistered (PreciseManeuverConfig.HotkeyType.TURNOUP);
    down = (down && repeatButtonDelay ()) || config.isHotkeyRegistered (PreciseManeuverConfig.HotkeyType.TURNODN);

    if (currentNode != null && (up || down)) {
      var maneuverPos = currentNode.patch.getRelativePositionAtUT (currentNode.UT).xzy;
      var maneuverVel = currentNode.patch.getOrbitalVelocityAtUT (currentNode.UT).xzy;

      if (maneuverPos.NotNAN () && !maneuverPos.IsZero () && maneuverVel.NotNAN ()) {
        var nprog = maneuverVel.normalized;
        var nnorm = Vector3d.Cross (maneuverVel, maneuverPos).normalized;
        var nrad = Vector3d.Cross (nnorm, nprog);

        if (!nprog.IsZero () && !nnorm.IsZero () && !nrad.IsZero ()) {
          double theta = config.incrementDeg;
          if (up)
            theta = -theta;

          var dv = currentNode.DeltaV;
          var calcVel = maneuverVel + nrad * dv.x + nnorm * dv.y + nprog * dv.z;
          NodeTools.turnVector (ref calcVel, maneuverPos, theta);
          var newDV = calcVel - maneuverVel;

          dx = Vector3d.Dot (newDV, nrad);
          dy = Vector3d.Dot (newDV, nnorm);
          dz = Vector3d.Dot (newDV, nprog);
          changed = true;
        }
      }
      if (!changed) {
        // position and velocity are perfectly parallel (less probable)
        // or
        // KSP API returned NaN or some other weird shit (much more probable)
        ScreenMessages.PostScreenMessage ("Can't change the orbit, parameters are invalid", 2.0f, ScreenMessageStyle.UPPER_CENTER);
      }
    }

    if (showEject) {
      MainWindow.drawDoubleLabel ("Eject. angle:", eAngleStr);
      MainWindow.drawDoubleLabel ("Eject. inclination:", eInclStr);
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

    if (GUILayout.Button ("Focus on\nVessel", GUILayout.Width (endButtonSize)) ||
        config.isHotkeyRegistered (PreciseManeuverConfig.HotkeyType.FOCVESL)) {
      MapObject mapObject = PlanetariumCamera.fetch.targets.Find (o => (o.vessel != null) && o.vessel.Equals (FlightGlobals.ActiveVessel));
      MapView.MapCamera.SetTarget (mapObject);
    }

    GUILayout.EndHorizontal ();

    //if (currentNode != null && changed)
    //  nodeManager.changeNode (currentNode, dx, dy, dz, ut);

    /* list of maneuvers */
    if (showManeuvers) {
      GUILayout.Space (verticalSpace);
      foreach (var node in solver.maneuverNodes) {
        drawNodeSummary (node);
      }
    }

    /*if ((showEjectPrevious != showEject) ||
        (showOrbitPrevious != showOrbit) ||
        (showManueversPrev != showManeuvers) ||
        (showManeuvers && (nodeCount != nodeCountShow)))
      config.readjustMainWindow ();*/
    nodeCountShow = nodeCount;

    if (repeatButtonPressed) {
      if (repeatButtonPressInterval < 100)
        repeatButtonPressInterval++;
      repeatButtonReleaseInterval = 0;
    } else {
      if (repeatButtonReleaseInterval < 3)
        repeatButtonReleaseInterval++;
      else
        repeatButtonPressInterval = 0;
    }

    GUI.DragWindow ();
  }











  #region UT + Axis Control GUI

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

    public void PlusButtonPressed () {
      _parent.nodeManager.changeNodeDiff (0, 0, 0, _parent.config.incrementUt);
    }

    public void MinusButtonPressed () {
      _parent.nodeManager.changeNodeDiff (0, 0, 0, -_parent.config.incrementUt);
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

    public void ZeroButtonPressed () {
      double dx = _axis == Axis.radial ? 0 : 1;
      double dy = _axis == Axis.normal ? 0 : 1;
      double dz = _axis == Axis.prograde ? 0 : 1;
      _parent.nodeManager.changeNodeDVMult (dx, dy, dz);
    }

    public void registerUpdateAction (Action action) {
      _parent.nodeManager.listenToValuesChange (action);
    }
    public void deregisterUpdateAction (Action action) {
      _parent.nodeManager.removeListener (action);
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

  GameObject[] panels = null;

  internal void updateMainWindow (DraggableWindow window) {
    if (panels == null)
      panels = new GameObject[10];
    window.DivideContentPanel (panels.Length);
    // PAGER
    int num = 0;
    if (config.getModuleState (PreciseManeuverConfig.ModuleType.PAGER)) {
      if (panels[num] == null) {
        panels[num] = window.createInnerContentPanel (num);
        createPagerControls (panels[num]);
      }
    } else {
      if (panels[num] != null) {
        UnityEngine.Object.Destroy (panels[num]);
        panels[num] = null;
      }
    }
    // TIME & ALARM (always on)
    num = 1;
    if (panels[num] == null) {
      panels[num] = window.createInnerContentPanel (num);
      createTimeAlarmControls (panels[num]);
    }
    // INCREMENT (on if manual || tools)
    num = 2;
    if (config.getModuleState (PreciseManeuverConfig.ModuleType.INPUT) ||
        config.getModuleState (PreciseManeuverConfig.ModuleType.TOOLS)) {
      if (panels[num] == null) {
        panels[num] = window.createInnerContentPanel (num);
        createIncrementControls (panels[num]);
      }
    } else {
      if (panels[num] != null) {
        UnityEngine.Object.Destroy (panels[num]);
        panels[num] = null;
      }
    }
    // MANUAL INPUT
    num = 3;
    if (config.getModuleState (PreciseManeuverConfig.ModuleType.INPUT)) {
      if (panels[num] == null) {
        panels[num] = window.createInnerContentPanel (num);
        createUtAxisControls (panels[num]);
      }
    } else {
      if (panels[num] != null) {
        UnityEngine.Object.Destroy (panels[num]);
        panels[num] = null;
      }
    }
  }

  internal void clearMainWindow () {
    if (panels != null)
      foreach (var panel in panels)
        if (panel != null)
          UnityEngine.Object.Destroy (panel);
    panels = null;
  }
}
}
