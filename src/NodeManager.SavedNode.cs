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

namespace KSPPreciseManeuver {
  internal partial class NodeManager {
    private class SavedNode {
      private struct Vector4d {
        internal double x;
        internal double y;
        internal double z;
        internal double t;
        public static implicit operator Vector3d (Vector4d v) {
          return new Vector3d (v.x, v.y, v.z);
        }
      }

      #region StateChain class

      private class StateChain {
        internal StateChain prev = null;
        internal Vector4d cur;
        internal StateChain next = null;
        internal bool notLast = false;
        private int index;

        private static StateChain first = null;

        private static readonly int maxLen = 30;

        private StateChain () { }

        internal static StateChain AddToChain (StateChain prev) {
          if (prev == null)
            return null;

          StateChain newNode = new StateChain () {
            cur = prev.cur,
            prev = prev,
            index = prev.index + 1
          };
          prev.next = newNode;
          prev.notLast = true;

          if (newNode.index - first.index > maxLen) {
            // remove the first item in the chain
            first = first.next;
            first.prev = null;
          }

          return newNode;
        }

        internal static StateChain NewChain (Vector4d state) {
          StateChain newNode = new StateChain () {
            cur = state,
            index = 1
          };
          first = newNode;
          return newNode;
        }
      }

      #endregion

      private Vector4d orig_state = new Vector4d ();

      private StateChain chain = null;

      private const double epsilon = 1E-5;

      private bool atomicChange = false;

      private void AddToChain () {
        if (atomicChange)
          return;
        chain = StateChain.AddToChain (chain);
        NodeManager.Instance.NotifyUndoChanged ();
      }

      internal Vector3d dV { get { return chain.cur; } }
      internal double UT { get { return chain.cur.t; } }
      internal bool Changed { get; private set; } = false;

      internal SavedNode (ManeuverNode node) {
        ResetSavedNode (node);
      }

      internal void UpdateDiff (double ddvx, double ddvy, double ddvz, double ddut) {
        AddToChain ();
        chain.cur.x += ddvx;
        chain.cur.y += ddvy;
        chain.cur.z += ddvz;
        chain.cur.t += ddut;
        Changed = true;
      }
      internal void UpdateMult (double mdvx, double mdvy, double mdvz) {
        AddToChain ();
        chain.cur.x *= mdvx;
        chain.cur.y *= mdvy;
        chain.cur.z *= mdvz;
        Changed = true;
      }
      internal void UpdateDvAbs (double dvx, double dvy, double dvz) {
        if (System.Double.IsNaN (dvx) || System.Double.IsNaN (dvy) || System.Double.IsNaN (dvz))
          return;
        AddToChain ();
        chain.cur.x = dvx;
        chain.cur.y = dvy;
        chain.cur.z = dvz;
        Changed = true;
      }
      internal void UpdateUtAbs (double ut) {
        if (System.Double.IsNaN (ut) || ut <= 0.0)
          return;
        AddToChain ();
        chain.cur.t = ut;
        Changed = true;
      }
      internal void BeginAtomicChange () {
        AddToChain ();
        atomicChange = true;
      }
      internal void EndAtomicChange () {
        atomicChange = false;
      }

      internal bool UndoAvailable {
        get { return chain.prev != null; }
      }

      internal void Undo () {
        if (atomicChange || chain.prev == null)
          return;
        chain = chain.prev;
        Changed = true;
        NodeManager.Instance.NotifyUndoChanged ();
      }

      internal bool RedoAvailable {
        get { return chain.notLast; }
      }

      internal void Redo () {
        if (atomicChange || !chain.notLast)
          return;
        chain = chain.next;
        Changed = true;
        NodeManager.Instance.NotifyUndoChanged ();
      }

      internal void ResetSavedNode (ManeuverNode node) {
        orig_state.x = node.DeltaV.x;
        orig_state.y = node.DeltaV.y;
        orig_state.z = node.DeltaV.z;
        orig_state.t = node.UT;
        chain = StateChain.NewChain (orig_state);
        Changed = false;
        atomicChange = false;
        NodeManager.Instance.NotifyUndoChanged ();
      }

      internal void UpdateOrig () {
        orig_state = chain.cur;
        Changed = false;
      }

      internal bool OrigSame (ManeuverNode node) {
        return (System.Math.Abs (orig_state.x - node.DeltaV.x) < epsilon &&
                System.Math.Abs (orig_state.y - node.DeltaV.y) < epsilon &&
                System.Math.Abs (orig_state.z - node.DeltaV.z) < epsilon &&
                System.Math.Abs (orig_state.t - node.UT) < epsilon);
      }
    }
  }
}
