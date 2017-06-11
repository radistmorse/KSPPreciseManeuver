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
  public class AxisControl : MonoBehaviour {
    [SerializeField]
    private InputField m_AxisValue = null;
    [SerializeField]
    private Text m_AxisName = null;
    [SerializeField]
    private Button m_EditFieldButton = null;

    private IAxisControl m_Control = null;

    public void SetControl (IAxisControl control) {
      m_Control = control;
      m_AxisName.color = m_Control.AxisColor;
      m_AxisName.text = m_Control.AxisName;
      m_AxisValue.textComponent.color = m_Control.AxisColor;
      m_Control.ReplaceTextComponentWithTMPro (m_AxisName);
      m_Control.ReplaceInputFieldWithTMPro (m_AxisValue, InputFieldEndEdit);
      UpdateGUI ();
      m_Control.RegisterUpdateAction (UpdateGUI);
    }

    public void OnDestroy () {
      m_Control.DeregisterUpdateAction (UpdateGUI);
      m_Control = null;
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
    public void ZeroButtonAction () {
      m_Control.ZeroButtonPressed ();
    }
    public void EditButtonAction () {
      m_Control.TMProIsInteractable = true;
      m_EditFieldButton.interactable = false;
      m_Control.LockKeyboard ();
      m_Control.TMProActivateInputField ();
    }

    public void InputFieldEndEdit (string text) {
      if ((Input.GetKeyDown (KeyCode.Return) || Input.GetKeyDown (KeyCode.KeypadEnter)) && m_Control != null && double.TryParse (text, out double value))
        m_Control.UpdateValueAbs (value);

      m_Control.TMProIsInteractable = false;
      m_EditFieldButton.interactable = true;
      m_Control.UnlockKeyboard ();
      UpdateGUI ();
    }

    public void UpdateGUI () {
      if (m_Control.TMProIsInteractable == false)
        m_Control.TMProText = m_Control.AxisValue;
    }
  }
}
