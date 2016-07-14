/*******************************************************************************
 * Copyright (c) 2016, George Sedov
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

using UnityEngine;
using UnityEngine.UI;

namespace KSPPreciseManeuver.UI {
[RequireComponent (typeof (RectTransform))]
public class ConicsControl : MonoBehaviour {
  [SerializeField]
  private Toggle m_Toggle1 = null;
  [SerializeField]
  private Toggle m_Toggle2 = null;
  [SerializeField]
  private Toggle m_Toggle3 = null;
  [SerializeField]
  private Toggle m_Toggle4 = null;
  [SerializeField]
  private Toggle m_Toggle5 = null;

  private IConicsControl m_control = null;

  public void SetControl(IConicsControl control) {
    m_control = control;
    updateControls ();
    m_control.registerUpdateAction (updateControls);
  }

  public void OnDestroy () {
    if (m_control != null)
      m_control.deregisterUpdateAction (updateControls);
    m_control = null;
  }

  public void PatchesModeAction (int num) {
    if (m_control != null)
      m_control.conicsModeChanged (num);
  }

  public void MoreConicPatches () {
    m_control.MoreConicPatches ();
  }

  public void LessConicPatches () {
    m_control.LessConicPatches ();
  }

  public void updateControls () {
    m_Toggle1.onValueChanged.SetPersistentListenerState (0, UnityEngine.Events.UnityEventCallState.Off);
    m_Toggle2.onValueChanged.SetPersistentListenerState (0, UnityEngine.Events.UnityEventCallState.Off);
    m_Toggle3.onValueChanged.SetPersistentListenerState (0, UnityEngine.Events.UnityEventCallState.Off);
    m_Toggle4.onValueChanged.SetPersistentListenerState (0, UnityEngine.Events.UnityEventCallState.Off);
    m_Toggle5.onValueChanged.SetPersistentListenerState (0, UnityEngine.Events.UnityEventCallState.Off);
    m_Toggle1.isOn = m_Toggle2.isOn = m_Toggle3.isOn = m_Toggle4.isOn = m_Toggle5.isOn = false;
    switch (m_control.getPatchesMode) {
      case 0: m_Toggle1.isOn = true; break;
      case 1: m_Toggle2.isOn = true; break;
      case 2: m_Toggle3.isOn = true; break;
      case 3: m_Toggle4.isOn = true; break;
      case 4: m_Toggle5.isOn = true; break;
    }
    m_Toggle1.onValueChanged.SetPersistentListenerState (0, UnityEngine.Events.UnityEventCallState.RuntimeOnly);
    m_Toggle2.onValueChanged.SetPersistentListenerState (0, UnityEngine.Events.UnityEventCallState.RuntimeOnly);
    m_Toggle3.onValueChanged.SetPersistentListenerState (0, UnityEngine.Events.UnityEventCallState.RuntimeOnly);
    m_Toggle4.onValueChanged.SetPersistentListenerState (0, UnityEngine.Events.UnityEventCallState.RuntimeOnly);
    m_Toggle5.onValueChanged.SetPersistentListenerState (0, UnityEngine.Events.UnityEventCallState.RuntimeOnly);
  }
}
}
