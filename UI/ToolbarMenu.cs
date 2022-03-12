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
  [RequireComponent (typeof (CanvasGroupFader))]
  public class ToolbarMenu : MonoBehaviour {
    [SerializeField]
    private Toggle m_ShowMainWindowToggle = null;

    [SerializeField]
    private Toggle m_ShowKeybindingsToggle = null;

    [SerializeField]
    private Toggle m_BackgroundToggle = null;

    [SerializeField]
    private Toggle m_TooltipsToggle = null;

    [SerializeField]
    private Slider m_ScaleGUISlider = null;

    [SerializeField]
    private GameObject m_MenuSectionPrefab = null;

    [SerializeField]
    private Transform m_SectionsTransform = null;

    private CanvasGroupFader fader;

    private IMenuControl m_Control;
    private RectTransform rectTransform;

    public bool IsFadingOut { get { return fader.IsFadingOut; } }

    public void FadeIn () {
      fader.FadeIn ();
    }

    public void FadeClose () {
      fader.FadeClose ();
    }

    public void OnPointerEnter () {
      m_Control.OnMenuPointerEnter ();
      fader.FadeIn ();
    }

    public void OnPointerExit () {
      m_Control.OnMenuPointerExit ();
      if (m_Control.IsOn == false && !fader.IsFadingOut)
        fader.FadeCloseSlow ();
    }

    public void OnPointerDown () {
      rectTransform.SetAsLastSibling ();
    }

    public void SetMainWindowVisible (bool visible) {
      m_Control.IsMainWindowVisible = visible;
    }

    public void SetKeybindingsVisible (bool visible) {
      m_Control.IsKeybindingsVisible = visible;
    }

    public void SetBackground (bool state) {
      m_Control.IsInBackground = state;
    }

    public void SetTooltips (bool state) {
      m_Control.IsTooltipsEnabled = state;
    }

    public void SetGUIScale (float scale) {
      m_Control.scaleGUIValue = scale;
    }

    public void SetControl (IMenuControl control) {
      m_Control = control;
      CreateMenuSections (m_Control.GetSections ());
      UpdateGUI ();
      m_Control.registerUpdateAction (UpdateGUI);
    }

    public void Awake () {
      fader = GetComponent<CanvasGroupFader> ();
      fader.collapseOnFade = false;
      rectTransform = GetComponent<RectTransform> ();
    }

    protected virtual void Start () {
      fader.SetTransparent ();
      fader.FadeIn ();
    }

    public void OnDestroy () {
      m_Control.deregisterUpdateAction (UpdateGUI);
      m_Control = null;
    }

    public void UpdateGUI () {
      m_ShowMainWindowToggle.onValueChanged.SetPersistentListenerState (0, UnityEventCallState.Off);
      m_ShowKeybindingsToggle.onValueChanged.SetPersistentListenerState (0, UnityEventCallState.Off);
      m_ScaleGUISlider.onValueChanged.SetPersistentListenerState (0, UnityEventCallState.Off);
      m_BackgroundToggle.onValueChanged.SetPersistentListenerState (0, UnityEventCallState.Off);
      m_TooltipsToggle.onValueChanged.SetPersistentListenerState (0, UnityEventCallState.Off);
      m_ShowMainWindowToggle.isOn = m_Control.IsMainWindowVisible;
      m_ShowKeybindingsToggle.isOn = m_Control.IsKeybindingsVisible;
      m_ScaleGUISlider.value = m_Control.scaleGUIValue;
      if (m_Control.IsMainWindowVisible) {
        m_BackgroundToggle.interactable = true;
        m_BackgroundToggle.GetComponent<Image> ().color = new Color (1.0f, 1.0f, 1.0f, 1.0f);
        m_BackgroundToggle.isOn = m_Control.IsInBackground;
      } else {
        m_BackgroundToggle.isOn = false;
        m_BackgroundToggle.interactable = false;
        m_BackgroundToggle.GetComponent<Image> ().color = new Color (0.0f, 0.0f, 0.0f, 0.25f);
      }
      m_TooltipsToggle.isOn = m_Control.IsTooltipsEnabled;
      m_ShowMainWindowToggle.onValueChanged.SetPersistentListenerState (0, UnityEventCallState.RuntimeOnly);
      m_ShowKeybindingsToggle.onValueChanged.SetPersistentListenerState (0, UnityEventCallState.RuntimeOnly);
      m_ScaleGUISlider.onValueChanged.SetPersistentListenerState (0, UnityEventCallState.RuntimeOnly);
      m_BackgroundToggle.onValueChanged.SetPersistentListenerState (0, UnityEventCallState.RuntimeOnly);
      m_TooltipsToggle.onValueChanged.SetPersistentListenerState (0, UnityEventCallState.RuntimeOnly);
    }

    public void DisableMainWindow () {
      m_ShowMainWindowToggle.interactable = false;
      m_ShowMainWindowToggle.GetComponent<Image> ().color = new Color (0.0f, 0.0f, 0.0f, 0.25f);
    }

    public void Update () {
      // update anchor position
      if (rectTransform != null && !fader.IsFadingOut) {
        rectTransform.position = m_Control.GetAnchor ();
        m_Control.ClampToScreen (rectTransform);
      }
    }

    private void CreateMenuSection (ISectionControl section) {
      GameObject menuSectionObject = Instantiate(m_MenuSectionPrefab);
      if (menuSectionObject != null) {
        menuSectionObject.transform.SetParent (m_SectionsTransform, false);
        ToolbarMenuSection menuSection = menuSectionObject.GetComponent<ToolbarMenuSection>();
        if (menuSection != null)
          menuSection.SetControl (section);
      }
    }

    private void CreateMenuSections (System.Collections.Generic.IList<ISectionControl> sections) {
      if (sections == null || m_MenuSectionPrefab == null || m_SectionsTransform == null)
        return;
      foreach (var section in sections) {
        if (section != null)
          CreateMenuSection (section);
      }
    }
  }
}
