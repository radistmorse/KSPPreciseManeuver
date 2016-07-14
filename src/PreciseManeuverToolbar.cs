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
using KSP.UI;
using KSP.UI.Screens;
using UnityEngine;

namespace KSPPreciseManeuver {
using UI;
[KSPAddon (KSPAddon.Startup.MainMenu, true)]
class PreciseManeuverToolbar : MonoBehaviour, IMenuControl {

  private ApplicationLauncherButton appButton;
  private Texture appButtonTexture;

  private ToolbarMenu m_ToolbarMenu;
  private GameObject m_MenuObject;
  private GameObject m_MenuPrefab = PreciseManeuverConfig.Instance.prefabs.LoadAsset<GameObject>("PreciseManeuverMenu");


  public bool IsMainWindowVisible {
    get { return PreciseManeuverConfig.Instance.showMainWindow; }

    set { PreciseManeuverConfig.Instance.showMainWindow = value; }
  }

  public bool IsKeybindingsVisible {
    get { return PreciseManeuverConfig.Instance.showKeymapperWindow; }

    set { PreciseManeuverConfig.Instance.showKeymapperWindow = value; }
  }

  public bool IsInBackground {
    get { return PreciseManeuverConfig.Instance.isInBackground; }
    set { PreciseManeuverConfig.Instance.isInBackground = value; }
  }

  public void ClampToScreen (RectTransform rectTransform) {
    UIMasterController.ClampToScreen (rectTransform, Vector2.zero);
  }

  private class SectionModule : ISectionControl {
    PreciseManeuverConfig.ModuleType _type;

    internal SectionModule (PreciseManeuverConfig.ModuleType type) {
      _type = type;
    }
    public bool IsVisible {
      get { return PreciseManeuverConfig.Instance.getModuleState (_type); }

      set { PreciseManeuverConfig.Instance.setModuleState (_type, value); }
    }

    public string Name {
      get { return PreciseManeuverConfig.getModuleName (_type); }
    }
  }

  public IList<ISectionControl> GetSections () {
    var rez = new List<ISectionControl>();
    foreach (PreciseManeuverConfig.ModuleType type in Enum.GetValues (typeof (PreciseManeuverConfig.ModuleType)))
      rez.Add (new SectionModule (type));
    return rez;
  }

  public bool IsOn {
    get {
      return appButton != null &&
             appButton.toggleButton.Interactable &&
             appButton.toggleButton.CurrentState == UIRadioButton.State.True;
    }
    set {
      if (appButton == null)
        return;

      if (value)
        SetOn();
      else
        SetOff();
    }
  }

  public float scaleGUIValue {
    get {
      return PreciseManeuverConfig.Instance.guiScale;
    }
    set {
      PreciseManeuverConfig.Instance.guiScale = value;
    }
  }

  public void Disable () {
    if (appButton != null && appButton.toggleButton.Interactable)
      appButton.Disable ();
  }

  public void Enable () {
    if (appButton != null && appButton.toggleButton.Interactable == false)
      appButton.Enable ();
  }

  public Vector3 GetAnchor () {
    if (appButton == null)
      return Vector3.zero;

    Vector3 anchor = appButton.GetAnchor();

    anchor.x -= 3.0f;

    return anchor;
  }

  public void SetOff () {
    Enable ();
    if (appButton != null && appButton.toggleButton.CurrentState != UIRadioButton.State.False)
      appButton.SetFalse ();
  }

  public void SetOn () {
    Enable ();
    if (appButton != null && appButton.toggleButton.CurrentState != UIRadioButton.State.True)
      appButton.SetTrue ();
  }


  internal void Awake () {
    DontDestroyOnLoad (this);

    // Precise Maneuver Icon
    if (appButtonTexture == null)
      appButtonTexture = PreciseManeuverConfig.Instance.prefabs.LoadAsset<Texture> ("PreciseManeuverIcon");

    // subscribe event listeners
    GameEvents.onGUIApplicationLauncherReady.Add (OnGUIApplicationLauncherReady);
    GameEvents.onGUIApplicationLauncherUnreadifying.Add (OnGUIApplicationLauncherUnreadifying);
    GameEvents.onHideUI.Add (OnHideUI);
    GameEvents.onShowUI.Add (OnShowUI);
    GameEvents.OnMapEntered.Add (ShowMenuIfEnabled);
    GameEvents.OnMapExited.Add (HideMenu);
  }

  protected virtual void OnDestroy () {
    // unsubscribe event listeners
    GameEvents.onGUIApplicationLauncherReady.Remove (OnGUIApplicationLauncherReady);
    GameEvents.onGUIApplicationLauncherUnreadifying.Remove (OnGUIApplicationLauncherUnreadifying);
  }

  private void OnGUIApplicationLauncherReady () {
    // create button
    if (ApplicationLauncher.Ready)
      appButton = ApplicationLauncher.Instance.AddModApplication (ShowMenu, HideMenu, ShowMenu, HideMenuIfDisabled, Enable, Disable,
                                                                  ApplicationLauncher.AppScenes.MAPVIEW, appButtonTexture);
  }

  private void OnGUIApplicationLauncherUnreadifying (GameScenes scene) {
    // remove button
    if (appButton != null) {
      Close ();
      ApplicationLauncher.Instance.RemoveModApplication (appButton);
      appButton = null;
    }
  }

  private void OnHideUI () {
    PreciseManeuverConfig.Instance.uiActive = false;
    HideMenu ();
  }

  private void OnShowUI () {
    PreciseManeuverConfig.Instance.uiActive = true;
    ShowMenuIfEnabled ();
  }

  protected void ShowMenu () {
    Open ();
  }

  private void ShowMenuIfEnabled () {
    if (IsOn)
      Open ();
  }

  private void HideMenu () {
    Close ();
  }

  protected void HideMenuIfDisabled () {
    if (!IsOn)
      Close ();
  }

  private void Close () {
    if (m_ToolbarMenu != null) {
      if (!m_ToolbarMenu.IsFadingOut)
        m_ToolbarMenu.fadeClose ();
    } else if (m_MenuObject != null) {
      Destroy (m_MenuObject);
    }
  }

  private void Open () {
    // fade menu in if already open
    if (m_ToolbarMenu != null && m_ToolbarMenu.IsFadingOut) {
      m_ToolbarMenu.fadeIn ();
      return;
    }

    if (m_MenuPrefab == null || m_MenuObject != null)
      return;

    m_MenuObject = Instantiate (m_MenuPrefab, GetAnchor (), Quaternion.identity) as GameObject;
    if (m_MenuObject == null)
      return;

    m_MenuObject.transform.SetParent (MainCanvasUtil.MainCanvas.transform);
    m_ToolbarMenu = m_MenuObject.GetComponent<ToolbarMenu> ();
    if (m_ToolbarMenu != null) {
      m_ToolbarMenu.SetMenuControl (this);
      if (!NodeTools.patchedConicsUnlocked)
        m_ToolbarMenu.DisableMainWindow ();
    }
    StyleManager.Process (m_MenuObject);
  }

  public void registerUpdateAction (Action action) {
    PreciseManeuverConfig.Instance.listenToShowChange (action);
  }

  public void deregisterUpdateAction (Action action) {
    PreciseManeuverConfig.Instance.removeListener (action);
  }
}
}
