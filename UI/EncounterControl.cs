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
public class EncounterControl : MonoBehaviour {
  [SerializeField]
  private Text m_Encounter = null;
  private UnityAction<string> encValueUpdate;

  [SerializeField]
  private Text m_PE = null;
  private UnityAction<string> peValueUpdate;

  [SerializeField]
  private Button m_Focus = null;

  private IEncounterControl m_Control = null;

  public void SetControl(IEncounterControl control) {
    m_Control = control;
    encValueUpdate = control.replaceTextComponentWithTMPro (m_Encounter);
    peValueUpdate = control.replaceTextComponentWithTMPro (m_PE);
    updateControl ();
    m_Control.registerUpdateAction (updateControl);
  }

  public void OnDestroy () {
    m_Control.deregisterUpdateAction (updateControl);
    m_Control = null;
  }

  public void FocusButtonAction () {
    m_Control.focus ();
  }

  public void updateControl () {
    bool isenc = m_Control.IsEncounter;
    encValueUpdate?.Invoke (m_Control.Encounter);
    peValueUpdate?.Invoke (m_Control.PE);
    if (isenc) {
      m_Focus.interactable = true;
      m_Focus.GetComponent<Image> ().color = new Color (1.0f, 1.0f, 1.0f, 1.0f);
    } else {
      m_Focus.interactable = false;
      m_Focus.GetComponent<Image> ().color = new Color (0.0f, 0.0f, 0.0f, 0.25f);
    }
  }
}
}
