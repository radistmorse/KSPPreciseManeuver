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
public class OrbitInfoControl : MonoBehaviour {
  [SerializeField]
  private Text m_ApValue = null;

  [SerializeField]
  private Text m_PeValue = null;

  [SerializeField]
  private Text m_InclValue = null;

  [SerializeField]
  private Text m_EccValue = null;

  private IOrbitInfoControl m_control = null;

  public void SetControl(IOrbitInfoControl control) {
    m_control = control;
    updateControl ();
    m_control.registerUpdateAction (updateControl);
  }

  public void OnDestroy () {
    m_control.deregisterUpdateAction (updateControl);
    m_control = null;
  }

  public void updateControl () {
    m_ApValue.text = m_control.ApoapsisValue;
    m_PeValue.text = m_control.PeriapsisValue;
    m_InclValue.text = m_control.InclinationValue;
    m_EccValue.text = m_control.EccentricityValue;
  }
}
}
