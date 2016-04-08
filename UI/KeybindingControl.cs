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
public class KeybindingControl : MonoBehaviour {
  [SerializeField]
  private Text m_KeybindingsText = null;

  [SerializeField]
  private Text m_KeybindingsKeycode = null;

  [SerializeField]
  private Toggle m_KeybindingsSetButton = null;

  private IKeybindingsControl m_Control;

  public void setControl (IKeybindingsControl control) {
    m_Control = control;
    if (m_KeybindingsText != null)
      m_KeybindingsText.text = control.keyName;
    if (m_KeybindingsKeycode != null)
      m_KeybindingsKeycode.text = control.code.ToString ();
  }

  public void setButtonPressed (bool value) {
    if (value == true) {
      m_Control.setKey (updateKeyCode);
    } else {
      m_Control.abortSetKey ();
    }
  }

  public void unsetButtonPressed () {
    m_Control.unsetKey ();
    if (m_KeybindingsKeycode != null)
      m_KeybindingsKeycode.text = KeyCode.None.ToString ();
  }

  private void updateKeyCode (KeyCode code) {
    if (m_KeybindingsKeycode != null)
      m_KeybindingsKeycode.text = code.ToString ();
    if (m_KeybindingsSetButton != null) {
      m_KeybindingsSetButton.onValueChanged.SetPersistentListenerState (0, UnityEngine.Events.UnityEventCallState.Off);
      m_KeybindingsSetButton.isOn = false;
      m_KeybindingsSetButton.onValueChanged.SetPersistentListenerState (0, UnityEngine.Events.UnityEventCallState.RuntimeOnly);
    }
  }
}
}
