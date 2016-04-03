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
public class ToolbarMenu : CanvasGroupFader, IPointerEnterHandler, IPointerExitHandler {
  [SerializeField]
  private Toggle m_ShowMainWindowToggle = null;

  [SerializeField]
  private Toggle m_ShowKeybindingsToggle = null;

  [SerializeField]
  private GameObject m_MenuSectionPrefab = null;

  [SerializeField]
  private Transform m_SectionsTransform = null;

  [SerializeField]
  private float m_FastFadeDuration = 0.2f;

  [SerializeField]
  private float m_SlowFadeDuration = 1.0f;

  private IMenuAppLauncher m_FlightAppLauncher;
  private RectTransform m_RectTransform;

  public void OnPointerEnter(PointerEventData eventData) {
    FadeIn();
  }

  public void OnPointerExit(PointerEventData eventData) {
    if (m_FlightAppLauncher != null && m_FlightAppLauncher.IsOn == false)
      FadeTo(0.0f, m_SlowFadeDuration, Destroy);
  }

  /// <summary>
  ///     Fades out and destroys the menu.
  /// </summary>
  public void Close() {
    FadeTo(0.0f, m_FastFadeDuration, Destroy);
  }

  /// <summary>
  ///     Fades in the menu.
  /// </summary>
  public void FadeIn() {
    FadeTo(1.0f, m_FastFadeDuration);
  }

  /// <summary>
  ///     Sets the display stack visibility.
  /// </summary>
  public void SetMainWindowVisible(bool visible) {
    if (m_FlightAppLauncher != null)
      m_FlightAppLauncher.IsMainWindowVisible = visible;
  }

  /// <summary>
  ///     Sets the display stack visibility.
  /// </summary>
  public void SetKeybindingsVisible(bool visible) {
    if (m_FlightAppLauncher != null)
      m_FlightAppLauncher.IsKeybindingsVisible = visible;
  }


  /// <summary>
  ///     Sets a reference to the flight app launcher object.
  /// </summary>
  public void SetMenuAppLauncher(IMenuAppLauncher flightAppLauncher) {
    if (flightAppLauncher == null)
      return;
    m_FlightAppLauncher = flightAppLauncher;

    // create section controls
    CreateSectionControls(m_FlightAppLauncher.GetSections());
  }

  protected override void Awake() {
    base.Awake();

    // cache components
    m_RectTransform = GetComponent<RectTransform>();
  }

  protected virtual void Start() {
    // set starting alpha to zero and fade in
    SetAlpha(0.0f);
    FadeIn();
  }

  protected virtual void Update() {
    if (m_FlightAppLauncher == null)
      return;

    // set toggle states to match the actual states
    SetToggle(m_ShowMainWindowToggle, m_FlightAppLauncher.IsMainWindowVisible);
    SetToggle(m_ShowKeybindingsToggle, m_FlightAppLauncher.IsKeybindingsVisible);

    // update anchor position
    if (m_RectTransform != null) {
      m_RectTransform.position = m_FlightAppLauncher.GetAnchor();
      m_FlightAppLauncher.ClampToScreen(m_RectTransform);
    }
  }

  /// <summary>
  ///     Sets a given toggle to the specified state with null checking.
  /// </summary>
  private static void SetToggle(Toggle toggle, bool state) {
    if (toggle != null)
      toggle.isOn = state;
  }

  /// <summary>
  ///     Creates a menu section control.
  /// </summary>
  private void CreateSectionControl(ISectionModule section) {
    GameObject menuSectionObject = Instantiate(m_MenuSectionPrefab);
    if (menuSectionObject != null) {
      // apply ksp theme to the created menu section object
      m_FlightAppLauncher.ApplyTheme(menuSectionObject);

      menuSectionObject.transform.SetParent(m_SectionsTransform, false);

      ToolbarMenuSection menuSection = menuSectionObject.GetComponent<ToolbarMenuSection>();
      if (menuSection != null)
        menuSection.SetAssignedSection(section);
    }
  }

  /// <summary>
  ///     Creates a list of section controls from a given list of sections.
  /// </summary>
  private void CreateSectionControls(IList<ISectionModule> sections) {
    if (sections == null || m_MenuSectionPrefab == null || m_SectionsTransform == null)
      return;
    for (int i = 0; i < sections.Count; i++) {
      ISectionModule section = sections[i];
      if (section != null)
        CreateSectionControl(section);
    }
  }

  /// <summary>
  ///     Destroys the game object.
  /// </summary>
  private void Destroy() {
    // disable game object first due to an issue within unity 5.2.4f1 that shows a single frame at full opaque alpha just before destruction
    gameObject.SetActive(false);
    Destroy(gameObject);
  }
}
}
