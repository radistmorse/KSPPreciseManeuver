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
using UnityEngine.Events;

namespace KSPPreciseManeuver.UI {
[RequireComponent (typeof (RectTransform))]
public class UTControl : MonoBehaviour {
  [SerializeField]
  private InputField m_UTValue = null;
  [SerializeField]
  private Toggle m_x10Toggle = null;
  [SerializeField]
  private Button m_APButton = null;
  [SerializeField]
  private Button m_PEButton = null;
  [SerializeField]
  private Button m_ANButton = null;
  [SerializeField]
  private Button m_DNButton = null;
  [SerializeField]
  private Button m_POButton = null;
  [SerializeField]
  private Button m_MOButton = null;

  private IUTControl m_UTControl = null;

  public void SetUTControl(IUTControl utControl) {
    m_UTControl = utControl;
    utControl.replaceInputFieldWithTMPro (m_UTValue);
    updateControls ();
    m_UTControl.registerUpdateAction (updateControls);
  }

  public void OnDestroy () {
    m_UTControl.deregisterUpdateAction (updateControls);
    m_UTControl = null;
  }

  public void x10ToggleAction (bool state) {
    m_UTControl.X10State = state;
  }

  public void PlusButtonAction () {
    if (m_UTControl != null)
      m_UTControl.PlusButtonPressed ();
  }
  public void MinusButtonAction () {
    if (m_UTControl != null)
      m_UTControl.MinusButtonPressed ();
  }
  public void RepeatButtonStart () {
      m_UTControl.BeginAtomicChange ();
  }
  public void RepeatButtonStop () {
      m_UTControl.EndAtomicChange ();
  }
  public void APButtonAction () {
    if (m_UTControl != null)
      m_UTControl.APButtonPressed ();
  }
  public void PEButtonAction () {
    if (m_UTControl != null)
      m_UTControl.PEButtonPressed ();
  }
  public void ANButtonAction () {
    if (m_UTControl != null)
      m_UTControl.ANButtonPressed ();
  }
  public void DNButtonAction () {
    if (m_UTControl != null)
      m_UTControl.DNButtonPressed ();
  }
  public void POButtonAction () {
    if (m_UTControl != null)
      m_UTControl.POButtonPressed ();
  }
  public void MOButtonAction () {
    if (m_UTControl != null)
      m_UTControl.MOButtonPressed ();
  }

  public void updateControls () {
    m_UTControl.TMProText = m_UTControl.UTValue;
    m_x10Toggle.onValueChanged.SetPersistentListenerState (0, UnityEventCallState.Off);
    m_x10Toggle.isOn = m_UTControl.X10State;
    m_x10Toggle.onValueChanged.SetPersistentListenerState (0, UnityEventCallState.RuntimeOnly);

    enableButton (m_APButton, m_UTControl.APAvailable);
    enableButton (m_PEButton, m_UTControl.PEAvailable);
    enableButton (m_ANButton, m_UTControl.ANAvailable);
    enableButton (m_DNButton, m_UTControl.DNAvailable);
    enableButton (m_POButton, m_UTControl.POAvailable);
    enableButton (m_MOButton, m_UTControl.MOAvailable);
  }

  private void enableButton (Button button, bool state) {
    if (state) {
      button.interactable = true;
      button.GetComponent<Image> ().color = new Color (1.0f, 1.0f, 1.0f, 1.0f);
    } else {
      button.interactable = false;
      button.GetComponent<Image> ().color = new Color (0.0f, 0.0f, 0.0f, 0.25f);
    }
  }
}
}
