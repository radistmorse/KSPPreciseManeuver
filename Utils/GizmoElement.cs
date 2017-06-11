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

using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace KSPPreciseManeuver.UI {
[RequireComponent (typeof (RectTransform))]
class GizmoElement : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler {
  private enum AxisType {
    Prograde,
    Retrograde,
    NormalUp,
    NormalDown,
    RadialOut,
    RadialIn,
    Central
  }
  [SerializeField]
  private AxisType m_Type = AxisType.Central;
  [SerializeField]
  private GizmoControl m_Control = null;

  [SerializeField]
  private Image m_glow = null;
  [SerializeField]
  private RectTransform m_scale = null;
  [SerializeField]
  private RectTransform m_drag = null;
  [SerializeField]
  private float m_rotation = 0f;
  [SerializeField]
  private float m_ClampDragMin = 0f;
  [SerializeField]
  private float m_ClampDragMax = 0f;

  private Vector3 _axisMult;
  private bool _axisMultReady = false;

  private Vector3 AxisMult {
    get {
      if (!_axisMultReady) {
        switch (m_Type) {
          case AxisType.Prograde:
            _axisMult = new Vector3 (0, 0, 1);
            break;
          case AxisType.Retrograde:
            _axisMult = new Vector3 (0, 0, -1);
            break;
          case AxisType.NormalUp:
            _axisMult = new Vector3 (0, 1, 0);
            break;
          case AxisType.NormalDown:
            _axisMult = new Vector3 (0, -1, 0);
            break;
          case AxisType.RadialOut:
            _axisMult = new Vector3 (1, 0, 0);
            break;
          case AxisType.RadialIn:
            _axisMult = new Vector3 (-1, 0, 0);
            break;
          case AxisType.Central:
            _axisMult = new Vector3 (0, 0, 0);
            break;
        }
        _axisMultReady = true;
      }
      return _axisMult;
    }
  }

  private readonly float m_duration = 0.4f;
  private System.Collections.IEnumerator m_SelectCoroutine;

  private Vector2 m_direction;
  private float m_startSize = 0;
  private float m_startPos = 0;

  private Vector2 m_BeginMousePosition;
  private bool m_dragging = false;
  private bool m_floating = false;

  public void Awake () {
    m_startSize = m_drag.sizeDelta.x;
    m_startPos = m_drag.localPosition.x;
    m_direction = new Vector2 ((float)Math.Cos (Math.PI / 180 * m_rotation), (float)Math.Sin (Math.PI / 180 * m_rotation));
  }

  public void OnBeginDrag (PointerEventData eventData) {
    m_BeginMousePosition = eventData.position;
    m_dragging = true;
    m_Control.Dragging = true;
    GlowOn ();
  }

  public void OnDrag (PointerEventData eventData) {
    Vector2 shift = eventData.position - m_BeginMousePosition;
    float proj = shift.x * m_direction.x + shift.y * m_direction.y;
    proj = Mathf.Clamp (proj, -m_ClampDragMin, m_ClampDragMax);
    if (m_Type != AxisType.Central) {
      Vector2 size = m_drag.sizeDelta;
      size.x = m_startSize + proj;
      m_drag.sizeDelta = size;

      double ddv = 0;
      if (proj > 0)
        ddv = Math.Pow (10, ((proj) / m_ClampDragMax) * 3.0 - 2.0) - 0.01;
      if (proj < 0)
        ddv = 0.01 - Math.Pow (10, ((-proj) / m_ClampDragMin) * 1.0 - 2.0);
      m_Control.ChangeddV (AxisMult.x * ddv, AxisMult.y * ddv, AxisMult.z * ddv, 0.0);
    } else {
      Vector2 pos = m_drag.localPosition;
      pos.x = m_startPos + proj;
      m_drag.localPosition = pos;

      double dut = 0;
      if (proj > 5)
        dut = Math.Pow (10, ((proj - 5) / m_ClampDragMax) * 5 - 1);
      if (proj < 0)
        dut = -Math.Pow (10, ((-proj - 5) / m_ClampDragMin) * 5 - 1);
      m_Control.ChangeddV (0.0, 0.0, 0.0, dut);
    }
  }

  public void OnEndDrag (PointerEventData eventData) {
    if (m_Type != AxisType.Central) {
      Vector2 size = m_drag.sizeDelta;
      size.x = m_startSize;
      m_drag.sizeDelta = size;
    } else {
      Vector2 pos = m_drag.localPosition;
      pos.x = m_startPos;
      m_drag.localPosition = pos;
    }
    m_dragging = false;
    m_Control.ChangeddV (0.0, 0.0, 0.0, 0.0);
    m_Control.Dragging = false;
    GlowOff ();
  }

  public void OnPointerEnter (PointerEventData eventData) {
    m_floating = true;
    GlowOn ();
  }

  public void OnPointerExit (PointerEventData eventData) {
    m_floating = false;
    GlowOff ();
  }

  public void GlowOn () {
    if (m_dragging || (m_floating && !m_Control.Dragging))
      Select (0f, 1f, 1f, 1.5f);
  }
  public void GlowOff () {
    if (!m_dragging && !m_floating)
      Select (1f, 0f, 1.5f, 1f);
  }

  private void Select (float glowfrom, float glowto, float scalefrom, float scaleto) {
    if (m_SelectCoroutine != null)
      StopCoroutine (m_SelectCoroutine);

    float current = m_glow.color.a;

    if (Math.Abs (current - glowto) < 0.1) {
      SetAlpha (glowto);
      SetScale (scaleto);
    } else {
      float duration = m_duration*Math.Abs(glowto-current);
      m_SelectCoroutine = SelectCoroutine (current, glowto, scalefrom, scaleto, duration);
      StartCoroutine (m_SelectCoroutine);
    }
  }

  private System.Collections.IEnumerator SelectCoroutine (float glowfrom, float glowto, float scalefrom, float scaleto, float duration) {
    // wait for end of frame so that only the last call to fade that frame is honoured.
    yield return new WaitForEndOfFrame ();

    float progress = 0.0f;

    while (progress <= 1.0f) {
      progress += Time.deltaTime / duration;
      SetAlpha (Mathf.Lerp (glowfrom, glowto, progress));
      SetScale (Mathf.Lerp (scalefrom, scaleto, progress));
      yield return null;
    }

    m_SelectCoroutine = null;
  }

  private void SetAlpha (float alpha) {
    if (m_glow != null) {
      var color = m_glow.color;
      color.a = alpha;
      m_glow.color = color;
    }
  }
  private void SetScale (float scale) {
    if (m_scale != null) {
      Vector3 scalevec = new Vector3(scale, scale, 1);
      m_scale.localScale = scalevec;
    }
  }
  protected virtual void Destroy () {
    // disable game object first due to an issue within unity 5.2.4f1 that shows a single frame at full opaque alpha just before destruction
    if (m_glow != null)
      m_glow.gameObject.SetActive (false);
    Destroy (gameObject);
  }
}
}
