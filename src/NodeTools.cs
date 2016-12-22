/******************************************************************************
 * Copyright (c) 2013-2014, Justin Bengtson
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

using System;
using UnityEngine;

namespace KSPPreciseManeuver {
internal static class NodeTools {
  /// <summary>
  /// Sets the conics render mode
  /// </summary>
  /// <param name="mode">The conics render mode to use, one of 0, 1, 2, 3, or 4.  Arguments outside those will be set to 3.</param>
  internal static void setConicsMode (int mode) {
    switch (mode) {
    case 0:
      FlightGlobals.ActiveVessel.patchedConicRenderer.relativityMode = PatchRendering.RelativityMode.LOCAL_TO_BODIES;
      break;
    case 1:
      FlightGlobals.ActiveVessel.patchedConicRenderer.relativityMode = PatchRendering.RelativityMode.LOCAL_AT_SOI_ENTRY_UT;
      break;
    case 2:
      FlightGlobals.ActiveVessel.patchedConicRenderer.relativityMode = PatchRendering.RelativityMode.LOCAL_AT_SOI_EXIT_UT;
      break;
    case 3:
      FlightGlobals.ActiveVessel.patchedConicRenderer.relativityMode = PatchRendering.RelativityMode.RELATIVE;
      break;
    case 4:
      FlightGlobals.ActiveVessel.patchedConicRenderer.relativityMode = PatchRendering.RelativityMode.DYNAMIC;
      break;
    default:
      // revert to KSP default
      FlightGlobals.ActiveVessel.patchedConicRenderer.relativityMode = PatchRendering.RelativityMode.RELATIVE;
      break;
    }
  }

  internal static int getConicsMode () {
    if (FlightGlobals.ActiveVessel.patchedConicRenderer.relativityMode == PatchRendering.RelativityMode.LOCAL_TO_BODIES)
      return 0;
    if (FlightGlobals.ActiveVessel.patchedConicRenderer.relativityMode == PatchRendering.RelativityMode.LOCAL_AT_SOI_ENTRY_UT)
      return 1;
    if (FlightGlobals.ActiveVessel.patchedConicRenderer.relativityMode == PatchRendering.RelativityMode.LOCAL_AT_SOI_EXIT_UT)
      return 2;
    if (FlightGlobals.ActiveVessel.patchedConicRenderer.relativityMode == PatchRendering.RelativityMode.RELATIVE)
      return 3;
    if (FlightGlobals.ActiveVessel.patchedConicRenderer.relativityMode == PatchRendering.RelativityMode.DYNAMIC)
      return 4;
    return 3;

  }

  /// <summary>
  /// Returns the orbit of the currently targeted item or null if there is none.
  /// </summary>
  /// <returns>The orbit or null.</returns>
  internal static Orbit getTargetOrbit () {
    ITargetable tgt = FlightGlobals.fetch.VesselTarget;
    if (tgt != null) {
      // if we have a null vessel it's a celestial body
      if (tgt.GetVessel () == null) {
        return tgt.GetOrbit ();
      }
      // otherwise make sure we're not targeting ourselves.
      if (!FlightGlobals.fetch.activeVessel.Equals (tgt.GetVessel ())) {
        return tgt.GetOrbit ();
      }
    }
    return null;
  }

  /// <summary>
  /// Gets the UT for the ascending node in reference to the target orbit.
  /// </summary>
  /// <returns>The UT for the ascending node in reference to the target orbit.</returns>
  /// <param name="a">The orbit to find the UT on.</param>
  /// <param name="b">The target orbit.</param>
  internal static double getTargetANUT (this Orbit a, Orbit b) {
    //TODO: Add safeguards for bad UTs, may need to be refactored to NodeManager
    Vector3d ANVector = Vector3d.Cross (b.h, a.GetOrbitNormal()).normalized;
    return a.GetUTforTrueAnomaly (a.GetTrueAnomalyOfZupVector (ANVector), 2);
  }

  /// <summary>
  /// Gets the UT for the descending node in reference to the target orbit.
  /// </summary>
  /// <returns>The UT for the descending node in reference to the target orbit.</returns>
  /// <param name="a">The orbit to find the UT on.</param>
  /// <param name="b">The target orbit.</param>
  internal static double getTargetDNUT (this Orbit a, Orbit b) {
    //TODO: Add safeguards for bad UTs, may need to be refactored to NodeManager
    Vector3d DNVector = Vector3d.Cross (a.GetOrbitNormal(), b.h).normalized;
    return a.GetUTforTrueAnomaly (a.GetTrueAnomalyOfZupVector (DNVector), 2);
  }

  /// <summary>
  /// Gets the UT for the equatorial AN.
  /// </summary>
  /// <returns>The equatorial AN UT.</returns>
  /// <param name="o">The Orbit to calculate the UT from.</param>
  internal static double getEquatorialANUT (this Orbit o) {
    //TODO: Add safeguards for bad UTs, may need to be refactored to NodeManager
    return o.GetUTforTrueAnomaly (o.GetTrueAnomalyOfZupVector (o.GetANVector()), 2);
  }

  /// <summary>
  /// Gets the UT for the equatorial DN.
  /// </summary>
  /// <returns>The equatorial DN UT.</returns>
  /// <param name="o">The Orbit to calculate the UT from.</param>
  internal static double getEquatorialDNUT (this Orbit o) {
    //TODO: Add safeguards for bad UTs, may need to be refactored to NodeManager
    Vector3d DNVector = QuaternionD.AngleAxis ((o.LAN + 180).Angle360(), Planetarium.Zup.Z) * Planetarium.Zup.X;
    return o.GetUTforTrueAnomaly (o.GetTrueAnomalyOfZupVector (DNVector), 2);
  }

  /// <summary>
  /// Adjusts the specified angle to between 0 and 360 degrees.
  /// </summary>
  /// <param name="d">The specified angle to restrict.</param>
  internal static double Angle360 (this double d) {
    d %= 360;
    if (d < 0)
      return d + 360;
    return d;
  }

  internal static CelestialBody findNextEncounter () {
    var plan = FlightGlobals.ActiveVessel.patchedConicSolver.flightPlan.AsReadOnly();
    var curOrbit = FlightGlobals.ActiveVessel.orbit;
    foreach (var o in plan) {
      if (curOrbit.referenceBody.name != o.referenceBody.name && !o.referenceBody.isSun()) {
        return o.referenceBody;
      }
    }
    return null;
  }

  /// <summary>
  /// Gets the ejection angle of the current maneuver node.
  /// </summary>
  /// <returns>The ejection angle in degrees.  Positive results are the angle from prograde, negative results are the angle from retrograde.</returns>
  /// <param name="nodeUT">Kerbal Space Program Universal Time.</param>
  internal static double getEjectionAngle (this Orbit o, ManeuverNode node) {
    if (node.nextPatch.patchEndTransition == Orbit.PatchTransitionType.ESCAPE) {
      CelestialBody body = o.referenceBody;

      // Calculate the angle between the node's position and the reference body's velocity at nodeUT
      Vector3d prograde = body.orbit.getOrbitalVelocityAtUT (node.UT);
      Vector3d position = o.getRelativePositionAtUT (node.UT);
      double eangle = ((Math.Atan2 (prograde.y, prograde.x) - Math.Atan2 (position.y, position.x)) * 180.0 / Math.PI).Angle360();

      // Correct to angle from retrograde if needed.
      if (eangle > 180) {
        eangle = 180 - eangle;
      }

      return eangle;
    } else {
      return double.NaN;
    }
  }

  internal static double getEjectionInclination (this Orbit o, ManeuverNode node) {
    if (node.nextPatch.patchEndTransition == Orbit.PatchTransitionType.ESCAPE) {
      CelestialBody body = o.referenceBody;
      Orbit bodyOrbit = body.orbit;
      Orbit orbitAfterEscape = node.nextPatch.nextPatch;
      return bodyOrbit.getRelativeInclination (orbitAfterEscape);
    } else {
      return double.NaN;
    }
  }

  internal static double getRelativeInclination (this Orbit o, Orbit other) {
    Vector3d normal = o.GetOrbitNormal().xzy.normalized;
    Vector3d otherNormal = other.GetOrbitNormal().xzy.normalized;
    double angle = Vector3d.Angle (normal, otherNormal);
    bool south = Vector3d.Dot (Vector3d.Cross (normal, otherNormal), normal.xzy) > 0;
    return south ? -angle : angle;
  }

  internal static bool isClosed (this Orbit o) {
    return o.patchEndTransition == Orbit.PatchTransitionType.FINAL;
  }

  internal static bool isSun (this CelestialBody body) {
    return body.name == "Sun";
  }

  internal static void turnVector (ref Vector3d v, Vector3d axis, double theta) {
    double costheta = Math.Cos (theta);
    double sintheta = Math.Sin (theta);
    var naxis = axis.normalized;
    double newx = (costheta + (1 - costheta) * naxis.x * naxis.x) * v.x + ((1 - costheta) * naxis.x * naxis.y - sintheta * naxis.z) * v.y + ((1 - costheta) * naxis.x * naxis.z + sintheta * naxis.y) * v.z;
    double newy = ((1 - costheta) * naxis.x * naxis.y + sintheta * naxis.z) * v.x + (costheta + (1 - costheta) * naxis.y * naxis.y) * v.y + ((1 - costheta) * naxis.y * naxis.z - sintheta * naxis.x) * v.z;
    double newz = ((1 - costheta) * naxis.x * naxis.z - sintheta * naxis.y) * v.x + ((1 - costheta) * naxis.y * naxis.z + sintheta * naxis.x) * v.y + (costheta + (1 - costheta) * naxis.z * naxis.z) * v.z;
    v.x = newx;
    v.y = newy;
    v.z = newz;
  }

  internal static bool NotNAN (this Vector3d v) {
    if (Double.IsNaN(v.x) || Double.IsNaN(v.y) || Double.IsNaN(v.z))
      return false;
    return true;
  }

  /// <summary>
  /// Converts the UT to human-readable Kerbal local time.
  /// </summary>
  /// <returns>The converted time.</returns>
  /// <param name="UT">Kerbal Space Program Universal Time.</param>
  internal static string convertUTtoHumanTime (double UT) {
    long secs;
    long mins;
    long hour;
    long day;
    long year;

    if (GameSettings.KERBIN_TIME) {
      secs = (long)Math.Floor (UT % 60);
      mins = (long)Math.Floor ((UT / 60) % 60);
      hour = (long)Math.Floor ((UT / (60 * 60)) % 6);
      day = (long)Math.Floor ((UT / (6 * 60 * 60)) % 426) + 1; // Ensure we don't get a "Day 0" here.
      year = (long)Math.Floor (UT / (426 * 6 * 60 * 60)) + 1; // Ensure we don't get a "Year 0" here.
    } else {
      secs = (long)Math.Floor (UT % 60);
      mins = (long)Math.Floor ((UT / 60) % 60);
      hour = (long)Math.Floor ((UT / (60 * 60)) % 24);
      day = (long)Math.Floor ((UT / (24 * 60 * 60)) % 365) + 1; // Ensure we don't get a "Day 0" here.
      year = (long)Math.Floor (UT / (365 * 24 * 60 * 60)) + 1; // Ensure we don't get a "Year 0" here.
    }

    return "Year " + year + ", Day " + day + ", " + hour + ":" + (mins < 10 ? "0" : "") + mins + ":" + (secs < 10 ? "0" : "") + secs;
  }
  
  internal static void copyToClipboard (Orbit o, ManeuverNode node) {
    string message = "Precise Maneuver Information\r\n";
    message += String.Format ("Depart at:      {0}\r\n", convertUTtoHumanTime(node.UT));
    message += String.Format ("       UT:      {0:0}\r\n", node.UT);
    string e = String.Format ("{0:0.00° from prograde;0.00° from retrograde}",o.getEjectionAngle(node));
    if (e == "NaN")
        e = "N/A";
    message += String.Format ("Ejection Angle: {0}\r\n", e);
    e = String.Format ("{0:0.00}°", o.getEjectionInclination(node));
    if (e == "NaN°")
        e = "N/A";
    message += String.Format ("Ejection Inc.:  {0}\r\n", e);
    message += String.Format ("Prograde Δv:    {0:0.0} m/s\r\n", node.DeltaV.z);
    message += String.Format ("Normal Δv:      {0:0.0} m/s\r\n", node.DeltaV.y);
    message += String.Format ("Radial Δv:      {0:0.0} m/s\r\n", node.DeltaV.x);
    message += String.Format ("Total Δv:       {0:0} m/s", node.DeltaV.magnitude);

    GUIUtility.systemCopyBuffer = message;
  }
  
  /// <summary>
  /// Formats the given double into meters.
  /// </summary>
  /// <returns>The string format, in meters.</returns>
  /// <param name="d">The double to format</param>
  internal static string formatMeters (this double d) {
    string multiplier = " ";
    if ((Math.Abs (d) / 1000) >= 100) {
      d /= 1000;
      multiplier = " k";

      if ((Math.Abs (d) / 1000) >= 100) {
        d /= 1000;
        multiplier = " M";

        if ((Math.Abs (d) / 1000) >= 100) {
          d /= 1000;
          multiplier = " G";
        }
      }
    }
    return string.Format ("{0:0.##}{1}", d, multiplier);
  }

  internal static KeyCode fetchKey () {

    foreach (KeyCode code in Enum.GetValues (typeof (KeyCode)))
      if (Input.GetKeyDown (code) && ((int)code < (int)KeyCode.Mouse0 || (int)code > (int)KeyCode.Mouse6))
        return code;

    return KeyCode.None;
  }

  public static bool patchedConicsUnlocked {
    get {
      return GameVariables.Instance.GetOrbitDisplayMode
                (ScenarioUpgradeableFacilities.GetFacilityLevel (SpaceCenterFacility.TrackingStation)) == GameVariables.OrbitDisplayMode.PatchedConics;
    }
  }
}
}
