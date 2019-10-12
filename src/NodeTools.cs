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
using KSP.Localization;

namespace KSPPreciseManeuver {
  internal static class NodeTools {
    internal static Orbit GetTargetOrbit (CelestialBody refbody) {
      ITargetable tgt = FlightGlobals.fetch.VesselTarget;
      if (tgt == null || FlightGlobals.ActiveVessel == tgt.GetVessel ())
        return null;

      Orbit o = tgt.GetOrbit ();
      if (o == null)
        return null;

      if (o.referenceBody == refbody)
        return o;

      while (o.nextPatch != null) {
        if (o.IsClosed ())
          return null;
        o = o.nextPatch;
        if (o.referenceBody == refbody)
          return o;
      }
      return null;
    }

    internal static double GetTargetANUT (this Orbit a, Orbit b) {
      Vector3d ANVector = Vector3d.Cross (a.GetOrbitNormal ().xzy, b.GetOrbitNormal ().xzy).normalized;
      return a.GetOrbitZupUT (ANVector.xzy);
    }

    internal static double GetTargetDNUT (this Orbit a, Orbit b) {
      Vector3d DNVector = Vector3d.Cross (b.GetOrbitNormal ().xzy, a.GetOrbitNormal ().xzy).normalized;
      return a.GetOrbitZupUT (DNVector.xzy);
    }

    private static double GetOrbitZupUT (this Orbit a, Vector3d Zup) {
      double new_obT = a.getObTAtMeanAnomaly (a.GetMeanAnomaly (a.GetEccentricAnomaly (a.GetTrueAnomalyOfZupVector (Zup))));
      double cur_obT = a.getObtAtUT (Planetarium.GetUniversalTime ());

      double new_ut = Planetarium.GetUniversalTime () - cur_obT + new_obT;

      if (new_obT > cur_obT) {
        if (!a.IsClosed () && a.EndUT < new_ut)
          return Double.NaN;
        else
          return new_ut;
      } else {
        if (a.IsClosed ())
          return new_ut + a.period;
        else
          return Double.NaN;
      }
    }

    internal static CelestialBody FindNextEncounter () {
      var plan = FlightGlobals.ActiveVessel.patchedConicSolver.flightPlan.AsReadOnly();
      var curOrbit = FlightGlobals.ActiveVessel.orbit;
      foreach (var o in plan) {
        if (curOrbit.referenceBody.name != o.referenceBody.name && !o.referenceBody.IsSun ()) {
          return o.referenceBody;
        }
      }
      return null;
    }

    internal static double GetEjectionAngle (this Orbit o, ManeuverNode node) {
      if (node.nextPatch.patchEndTransition == Orbit.PatchTransitionType.ESCAPE) {
        CelestialBody body = o.referenceBody;

        if (body.IsSun ())
          return double.NaN;

        // Calculate the angle between the node's position and the reference body's velocity at nodeUT
        Vector3d prograde = body.orbit.getOrbitalVelocityAtUT (node.UT);
        Vector3d position = o.getRelativePositionAtUT (node.UT);
        double eangle = ((Math.Atan2 (prograde.y, prograde.x) - Math.Atan2 (position.y, position.x)) * 180.0 / Math.PI);
        if (eangle < 0)
          eangle += 360;
        // Correct to angle from retrograde if needed.
        if (eangle > 180) {
          eangle = 180 - eangle;
        }

        return eangle;
      } else {
        return double.NaN;
      }
    }

    internal static double GetEjectionInclination (this Orbit o, ManeuverNode node) {
      if (node.nextPatch.patchEndTransition == Orbit.PatchTransitionType.ESCAPE) {
        CelestialBody body = o.referenceBody;

        if (body.IsSun ())
          return double.NaN;

        Orbit bodyOrbit = body.orbit;
        Orbit orbitAfterEscape = node.nextPatch.nextPatch;
        return bodyOrbit.GetRelativeInclination (orbitAfterEscape);
      } else {
        return double.NaN;
      }
    }

    internal static double GetRelativeInclination (this Orbit o, Orbit other) {
      Vector3d normal = o.GetOrbitNormal().xzy.normalized;
      Vector3d otherNormal = other.GetOrbitNormal().xzy.normalized;
      double angle = Vector3d.Angle (normal, otherNormal);
      bool south = Vector3d.Dot (Vector3d.Cross (normal, otherNormal), normal.xzy) > 0;
      return south ? -angle : angle;
    }

    internal static bool IsClosed (this Orbit o) {
      return o.patchEndTransition == Orbit.PatchTransitionType.FINAL;
    }

    internal static bool IsSun (this CelestialBody body) {
      return body.referenceBody == body;
    }

    internal static void TurnVector (ref Vector3d v, Vector3d axis, double theta) {
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
      if (Double.IsNaN (v.x) || Double.IsNaN (v.y) || Double.IsNaN (v.z))
        return false;
      return true;
    }

    internal static void CopyToClipboard (Orbit o, ManeuverNode node) {
      string message = "Precise Maneuver Information\r\n";
      message += String.Format ("Depart at:      {0}\r\n", KSPUtil.dateTimeFormatter.PrintTime (node.UT, 10, false, true));
      message += String.Format ("       UT:      {0:0}\r\n", node.UT);
      double eang = o.GetEjectionAngle(node);
      if (!double.IsNaN (eang)) {
        string e = String.Format ("{0:0.00° to prograde;0.00° to retrograde}",o.GetEjectionAngle(node));
        message += String.Format ("Ejection Angle: {0}\r\n", e);
        e = String.Format ("{0:0.00}°", o.GetEjectionInclination (node));
        message += String.Format ("Ejection Inc.:  {0}\r\n", e);
      }
      message += String.Format ("Prograde Δv:    {0:0.0} m/s\r\n", node.DeltaV.z);
      message += String.Format ("Normal Δv:      {0:0.0} m/s\r\n", node.DeltaV.y);
      message += String.Format ("Radial Δv:      {0:0.0} m/s\r\n", node.DeltaV.x);
      message += String.Format ("Total Δv:       {0:0} m/s", node.DeltaV.magnitude);

      GUIUtility.systemCopyBuffer = message;
    }

    internal static double GetUTdiffForAngle (this Orbit o, double ut, double angle) {
      double tA = o.TrueAnomalyAtUT(ut);
      tA += angle;
      if (tA < 0)
        tA += Math.PI * 2;
      if (tA > (Math.PI * 2))
        tA -= Math.PI * 2;
      double old_obT = o.getObtAtUT (ut);
      // yo dawg! so I heard you like anomalies!
      double new_obT = o.getObTAtMeanAnomaly (o.GetMeanAnomaly (o.GetEccentricAnomaly (tA)));

      double diff = new_obT - old_obT;
      double period = o.period;

      if (diff < -period / 2)
        diff += period;
      if (diff > period / 2)
        diff -= period;

      return diff;
    }

    internal static string AbbriviateWithMetricPrefix (this double d) {
      string prefix = " ";
      if ((Math.Abs (d) / 1000) >= 100) {
        d /= 1000;
        prefix = " " + Localizer.Format ("precisemaneuver_prefix_kilo");

        if ((Math.Abs (d) / 1000) >= 100) {
          d /= 1000;
          prefix = " " + Localizer.Format ("precisemaneuver_prefix_mega");

          if ((Math.Abs (d) / 1000) >= 100) {
            d /= 1000;
            prefix = " " + Localizer.Format ("precisemaneuver_prefix_giga");
          }
        }
      }
      return string.Format ("{0:0.##}{1}", d, prefix);
    }

    internal static KeyCode FetchKey () {
      foreach (KeyCode code in Enum.GetValues (typeof (KeyCode)))
        if (Input.GetKeyDown (code) && ((int)code < (int)KeyCode.Mouse0 || (int)code > (int)KeyCode.Mouse6))
          return code;

      return KeyCode.None;
    }

    public static bool PatchedConicsUnlocked {
      get {
        return GameVariables.Instance.GetOrbitDisplayMode
                  (ScenarioUpgradeableFacilities.GetFacilityLevel (SpaceCenterFacility.TrackingStation)) == GameVariables.OrbitDisplayMode.PatchedConics;
      }
    }
  }
}
