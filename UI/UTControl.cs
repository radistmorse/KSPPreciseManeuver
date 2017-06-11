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

    private IUTControl m_Control = null;

    public void SetControl (IUTControl control) {
      m_Control = control;
      m_Control.ReplaceInputFieldWithTMPro (m_UTValue);
      UpdateGUI ();
      m_Control.RegisterUpdateAction (UpdateGUI);
    }

    public void OnDestroy () {
      m_Control.DeregisterUpdateAction (UpdateGUI);
      m_Control = null;
    }

    public void x10ToggleAction (bool state) {
      m_Control.X10State = state;
    }

    public void PlusButtonAction () {
      m_Control.PlusButtonPressed ();
    }
    public void MinusButtonAction () {
      m_Control.MinusButtonPressed ();
    }
    public void RepeatButtonStart () {
      m_Control.BeginAtomicChange ();
    }
    public void RepeatButtonStop () {
      m_Control.EndAtomicChange ();
    }
    public void APButtonAction () {
      m_Control.APButtonPressed ();
    }
    public void PEButtonAction () {
      m_Control.PEButtonPressed ();
    }
    public void ANButtonAction () {
      m_Control.ANButtonPressed ();
    }
    public void DNButtonAction () {
      m_Control.DNButtonPressed ();
    }
    public void POButtonAction () {
      m_Control.POButtonPressed ();
    }
    public void MOButtonAction () {
      m_Control.MOButtonPressed ();
    }

    public void UpdateGUI () {
      m_Control.TMProText = m_Control.UTValue;
      m_x10Toggle.onValueChanged.SetPersistentListenerState (0, UnityEventCallState.Off);
      m_x10Toggle.isOn = m_Control.X10State;
      m_x10Toggle.onValueChanged.SetPersistentListenerState (0, UnityEventCallState.RuntimeOnly);

      EnableButton (m_APButton, m_Control.APAvailable);
      EnableButton (m_PEButton, m_Control.PEAvailable);
      EnableButton (m_ANButton, m_Control.ANAvailable);
      EnableButton (m_DNButton, m_Control.DNAvailable);
      EnableButton (m_POButton, m_Control.POAvailable);
      EnableButton (m_MOButton, m_Control.MOAvailable);
    }

    private void EnableButton (Button button, bool state) {
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
