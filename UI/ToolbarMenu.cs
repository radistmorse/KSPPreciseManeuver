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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KSPPreciseManeuver.UI {
[RequireComponent(typeof(RectTransform))]
public class ToolbarMenu : CanvasGroupFader, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler {
  [SerializeField]
  private Toggle m_ShowMainWindowToggle = null;

  [SerializeField]
  private Toggle m_ShowKeybindingsToggle = null;

  [SerializeField]
  private Slider m_ScaleGUISlider = null;

  [SerializeField]
  private GameObject m_MenuSectionPrefab = null;

  [SerializeField]
  private Transform m_SectionsTransform = null;

  private IMenuControl m_MenuControl;
  private RectTransform m_RectTransform;

  public void OnPointerEnter(PointerEventData eventData) {
    fadeIn();
  }

  public void OnPointerExit(PointerEventData eventData) {
      if (m_MenuControl != null && m_MenuControl.IsOn == false)
        fadeCloseSlow ();
  }

  public void OnPointerDown (PointerEventData data) {
    m_RectTransform.SetAsLastSibling ();
  }

  public void SetMainWindowVisible(bool visible) {
    if (m_MenuControl != null)
      m_MenuControl.IsMainWindowVisible = visible;
  }

  public void SetKeybindingsVisible(bool visible) {
    if (m_MenuControl != null)
      m_MenuControl.IsKeybindingsVisible = visible;
  }

  public void SetGUIScale(float scale) {
    if (m_MenuControl != null)
      m_MenuControl.scaleGUIValue = scale;
  }

  public void SetMenuControl(IMenuControl menuControl) {
    m_MenuControl = menuControl;
    CreateMenuSections(m_MenuControl.GetSections());
    updateControls ();
    m_MenuControl.registerUpdateAction (updateControls);
  }

  protected override void Awake() {
    base.Awake();
    m_RectTransform = GetComponent<RectTransform>();
  }

  protected virtual void Start() {
    setTransparent();
    fadeIn();
  }

  public void OnDestroy () {
    m_MenuControl.deregisterUpdateAction (updateControls);
    m_MenuControl = null;
  }

  public void updateControls() {
    m_ShowMainWindowToggle.onValueChanged.SetPersistentListenerState (0, UnityEngine.Events.UnityEventCallState.Off);
    m_ShowKeybindingsToggle.onValueChanged.SetPersistentListenerState (0, UnityEngine.Events.UnityEventCallState.Off);
    m_ScaleGUISlider.onValueChanged.SetPersistentListenerState (0, UnityEngine.Events.UnityEventCallState.Off);
    m_ShowMainWindowToggle.isOn = m_MenuControl.IsMainWindowVisible;
    m_ShowKeybindingsToggle.isOn = m_MenuControl.IsKeybindingsVisible;
    m_ScaleGUISlider.value = m_MenuControl.scaleGUIValue;
    m_ShowMainWindowToggle.onValueChanged.SetPersistentListenerState (0, UnityEngine.Events.UnityEventCallState.RuntimeOnly);
    m_ShowKeybindingsToggle.onValueChanged.SetPersistentListenerState (0, UnityEngine.Events.UnityEventCallState.RuntimeOnly);
    m_ScaleGUISlider.onValueChanged.SetPersistentListenerState (0, UnityEngine.Events.UnityEventCallState.RuntimeOnly);
  }

  public void Update() {
    // update anchor position
    if (m_RectTransform != null) {
      m_RectTransform.position = m_MenuControl.GetAnchor();
      m_MenuControl.ClampToScreen(m_RectTransform);
    }
  }

  private void CreateMenuSection(ISectionControl section) {
    GameObject menuSectionObject = Instantiate(m_MenuSectionPrefab);
    if (menuSectionObject != null) {
      menuSectionObject.transform.SetParent(m_SectionsTransform, false);
      ToolbarMenuSection menuSection = menuSectionObject.GetComponent<ToolbarMenuSection>();
      if (menuSection != null)
        menuSection.SetSectionControl(section);
    }
  }

  private void CreateMenuSections(IList<ISectionControl> sections) {
    if (sections == null || m_MenuSectionPrefab == null || m_SectionsTransform == null)
      return;
    for (int i = 0; i < sections.Count; i++) {
      ISectionControl section = sections[i];
      if (section != null)
        CreateMenuSection(section);
    }
  }
}
}
