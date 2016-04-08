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

  private IAxisControl m_axisControl = null;

  public void SetAxisControl(IAxisControl axisControl) {
    m_axisControl = axisControl;
    if (m_AxisName != null) {
      m_AxisName.color = m_axisControl.AxisColor;
      m_AxisName.text = m_axisControl.AxisName;
    }
    if (m_AxisValue != null) {
      m_AxisValue.textComponent.color = m_axisControl.AxisColor;
      m_AxisValue.text = m_axisControl.AxisValue;
    }
    m_axisControl.registerUpdateAction (updateAxisValue);
  }

  public void OnDestroy () {
    m_axisControl.deregisterUpdateAction (updateAxisValue);
    m_axisControl = null;
  }

  public void PlusButtonAction () {
    if (m_axisControl != null)
      m_axisControl.PlusButtonPressed ();
  }
  public void MinusButtonAction () {
    if (m_axisControl != null)
      m_axisControl.MinusButtonPressed ();
  }
  public void ZeroButtonAction () {
    if (m_axisControl != null)
      m_axisControl.ZeroButtonPressed ();
  }

  public void updateAxisValue () {
    if (m_AxisValue != null && m_axisControl != null)
      m_AxisValue.text = m_axisControl.AxisValue;
  }
}
}
