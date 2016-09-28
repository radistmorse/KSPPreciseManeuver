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
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KSPPreciseManeuver.UI {
[RequireComponent(typeof(CanvasGroupFader))]
public class DraggableWindow : MonoBehaviour {
  [SerializeField]
  private Text m_Title = null;

  [SerializeField]
  private Transform m_Content = null;

  [SerializeField]
  private GameObject m_Header = null;

  private Vector2 m_BeginMousePosition;
  private RectTransform m_RectTransform;
  private RectTransform m_CanvasRectTransform;

  private bool inBackground = false;

  private CanvasGroupFader m_fader;

  private List<GameObject> m_ContentSections = new List<GameObject>();

  private float clampx1, clampx2, clampy1, clampy2;

  public Action OnWindowPointerEnter = null;
  public Action OnWindowPointerExit = null;

  public RectTransform RectTransform {
    get {
      return m_RectTransform;
    }
  }

  public bool IsFadingOut { get { return m_fader.IsFadingOut; } }

  public void fadeIn () {
    m_fader.fadeIn ();
  }

  public void fadeClose () {
    m_fader.fadeClose ();
  }

  public void setMainCanvasTransform(RectTransform transform) {
    m_CanvasRectTransform = transform;
  }

  public void Awake() {
    m_fader = GetComponent<CanvasGroupFader> ();
    m_fader.collapseOnFade = false;
    m_RectTransform = GetComponent<RectTransform>();
  }

  protected virtual void Start() {
    m_fader.setTransparent();
    m_fader.fadeIn();
  }

  public void MoveToBackground (bool state) {
    if (inBackground != state) {
      inBackground = state;
      m_Header.SetActive (!inBackground);
      var position = m_RectTransform.localPosition;
      if (inBackground) {
        GetComponent<Image> ().color = new Color (1.0f, 1.0f, 1.0f, 0.5f);
        position.y -= 10;
      } else {
        GetComponent<Image> ().color = new Color (1.0f, 1.0f, 1.0f, 1.0f);
        position.y += 10;
      }
      m_RectTransform.localPosition = position;
    }
  }

  public void OnPointerDown() {
    m_RectTransform.SetAsLastSibling();
  }

  public void OnPointerEnter () {
    OnWindowPointerEnter?.Invoke ();
  }

  public void OnPointerExit () {
    OnWindowPointerExit?.Invoke ();
  }

  public void OnBeginDrag(BaseEventData data) {
    var eventData = data as PointerEventData;
    RectTransformUtility.ScreenPointToLocalPointInRectangle(m_RectTransform, eventData.position, eventData.pressEventCamera, out m_BeginMousePosition);
    m_BeginMousePosition.x = m_BeginMousePosition.x * m_RectTransform.localScale.x;
    m_BeginMousePosition.y = m_BeginMousePosition.y * m_RectTransform.localScale.y;

    if (m_CanvasRectTransform != null) {
      Vector3[] panelCorners = new Vector3[4];
      Vector3[] canvasCorners = new Vector3[4];

      m_RectTransform.GetWorldCorners(panelCorners);
      m_CanvasRectTransform.GetWorldCorners(canvasCorners);
      clampx1 = eventData.position.x - panelCorners[0].x + canvasCorners[0].x;
      clampx2 = eventData.position.x + canvasCorners[2].x - panelCorners[2].x;
      clampy1 = eventData.position.y - panelCorners[0].y + canvasCorners[0].y;
      clampy2 = eventData.position.y + canvasCorners[2].y - panelCorners[2].y;
    } else {
      clampx1 = clampx2 = clampy1 = clampy2 = 0;
    }
  }

  public void OnDrag(BaseEventData data) {
    var eventData = data as PointerEventData;
    Vector2 localPointerPosition;
    if (RectTransformUtility.ScreenPointToLocalPointInRectangle (
                 m_CanvasRectTransform, ClampToWindow (eventData.position), eventData.pressEventCamera, out localPointerPosition))
      m_RectTransform.localPosition = localPointerPosition - m_BeginMousePosition;
  }

  private Vector2 ClampToWindow(Vector2 data) {
    float clampedX = data.x;
    float clampedY = data.y;
    if (clampx1 < clampx2 && clampy1 < clampy2) {
      clampedX = Mathf.Clamp(data.x, clampx1, clampx2);
      clampedY = Mathf.Clamp(data.y, clampy1, clampy2);
    }
    Vector2 newPointerPosition = new Vector2(clampedX, clampedY);
    return newPointerPosition;
  }

  public void AddToContent (GameObject childObject) {
    if (m_Content != null && childObject != null)
      childObject.transform.SetParent (m_Content, false);
  }

  public bool DivideContentPanel (int num) {
    if (m_ContentSections.Count > 0)
      return false;
    for (int i = 0; i < num; i++) {
      var outerpanel = new GameObject("PreciseManeuverOuterPanel"+i.ToString());
      outerpanel.AddComponent<RectTransform> ();
      outerpanel.AddComponent<VerticalLayoutGroup> ();
      m_ContentSections.Add (outerpanel);
      AddToContent (outerpanel);
    }
    return true;
  }

  public GameObject createInnerContentPanel (int num) {
    if (m_ContentSections.Count <= num)
      return null;
    var innerpanel = new GameObject("PreciseManeuverInnerPanel"+num.ToString());
    innerpanel.AddComponent<RectTransform> ();
    var layout = innerpanel.AddComponent<ShrinkPanel> ();
    layout.padding = new RectOffset (4, 4, 4, 4);
    layout.spacing = 2.0f;
    var fader = innerpanel.AddComponent<CanvasGroupFader> ();
    fader.collapseOnFade = true;
    innerpanel.transform.SetParent (m_ContentSections[num].transform, false);
    return innerpanel;
  }

  public void SetTitle(string title) {
    if (m_Title != null) {
      m_Title.text = title;
    }
  }
}
}
