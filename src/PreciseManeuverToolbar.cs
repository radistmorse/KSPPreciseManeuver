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

using KSP.UI;
using KSP.UI.Screens;
using UnityEngine;

namespace KSPPreciseManeuver {
  [KSPAddon (KSPAddon.Startup.MainMenu, true)]
  class PreciseManeuverToolbar : MonoBehaviour, UI.IMenuControl {

    private ApplicationLauncherButton appButton = null;
    private Texture appButtonTexture;

    private UI.ToolbarMenu m_ToolbarMenu;
    private GameObject m_MenuObject;
    private GameObject m_MenuPrefab;

    private bool m_MenuPointerIn = false;

    public bool IsMainWindowVisible {
      get { return PreciseManeuverConfig.Instance.ShowMainWindow && NodeTools.PatchedConicsUnlocked; }

      set { PreciseManeuverConfig.Instance.ShowMainWindow = value; }
    }

    public bool IsKeybindingsVisible {
      get { return PreciseManeuverConfig.Instance.ShowKeymapperWindow; }

      set { PreciseManeuverConfig.Instance.ShowKeymapperWindow = value; }
    }

    public bool IsInBackground {
      get { return PreciseManeuverConfig.Instance.IsInBackground; }
      set { PreciseManeuverConfig.Instance.IsInBackground = value; }
    }

    public bool IsTooltipsEnabled {
      get { return PreciseManeuverConfig.Instance.IsTooltipsEnabled; }
      set { PreciseManeuverConfig.Instance.IsTooltipsEnabled = value; }
    }

    public void ClampToScreen (RectTransform rectTransform) {
      UIMasterController.ClampToScreen (rectTransform, Vector2.zero);
    }

    private class SectionModule : UI.ISectionControl {
      PreciseManeuverConfig.ModuleType _type;

      internal SectionModule (PreciseManeuverConfig.ModuleType type) {
        _type = type;
      }
      public bool IsVisible {
        get { return PreciseManeuverConfig.Instance.GetModuleState (_type); }

        set { PreciseManeuverConfig.Instance.SetModuleState (_type, value); }
      }

      public string Name {
        get { return PreciseManeuverConfig.GetModuleName (_type); }
      }
    }

    public System.Collections.Generic.IList<UI.ISectionControl> GetSections () {
      var rez = new System.Collections.Generic.List<UI.ISectionControl>();
      foreach (PreciseManeuverConfig.ModuleType type in System.Enum.GetValues (typeof (PreciseManeuverConfig.ModuleType))) {
        // TimeAlarm and Increment modules do not have a switch
        if (type == PreciseManeuverConfig.ModuleType.TIME || type == PreciseManeuverConfig.ModuleType.INCR)
          continue;
        rez.Add (new SectionModule (type));
      }
      return rez;
    }

    public bool IsOn {
      get {
        return appButton != null &&
               appButton.isActiveAndEnabled &&
               appButton.toggleButton.Interactable &&
               appButton.toggleButton.CurrentState == UIRadioButton.State.True;
      }
    }

    public float scaleGUIValue {
      get {
        return PreciseManeuverConfig.Instance.GUIScale;
      }
      set {
        PreciseManeuverConfig.Instance.GUIScale = value;
      }
    }

    public void Disable () {
      HideMenu ();
    }

    public void Enable () {
      ShowMenuIfEnabled ();
    }

    public Vector3 GetAnchor () {
      if (appButton == null)
        return Vector3.zero;

      Vector3 anchor = appButton.GetAnchor();

      anchor.x -= 3.0f;

      return anchor;
    }

    public void OnMenuPointerEnter () {
      m_MenuPointerIn = true;
      InputLockManager.SetControlLock (ControlTypes.MAP_UI, "PreciseManeuverMenuControlLock");
    }

    public void OnMenuPointerExit () {
      m_MenuPointerIn = false;
      InputLockManager.RemoveControlLock ("PreciseManeuverMenuControlLock");
    }

    internal void Start () {
      DontDestroyOnLoad (this);

      appButtonTexture = PreciseManeuverConfig.Instance.Prefabs.LoadAsset<Texture> ("PreciseManeuverIcon");
      m_MenuPrefab = PreciseManeuverConfig.Instance.Prefabs.LoadAsset<GameObject> ("PreciseManeuverMenu");

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
      GameEvents.onGUIApplicationLauncherReady?.Remove (OnGUIApplicationLauncherReady);
      GameEvents.onGUIApplicationLauncherUnreadifying?.Remove (OnGUIApplicationLauncherUnreadifying);
      GameEvents.onHideUI?.Remove (OnHideUI);
      GameEvents.onShowUI?.Remove (OnShowUI);
      GameEvents.OnMapEntered?.Remove (ShowMenuIfEnabled);
      GameEvents.OnMapExited?.Remove (HideMenu);

      ApplicationLauncher.Instance?.RemoveModApplication (appButton);
      appButton = null;
    }

    private void OnGUIApplicationLauncherReady () {
      // create button
      if (ApplicationLauncher.Ready && appButton == null)
        appButton = ApplicationLauncher.Instance.AddModApplication (ShowMenu, HideMenu, ShowMenu, HideMenuIfDisabled, Enable, Disable,
                                                                    ApplicationLauncher.AppScenes.MAPVIEW, appButtonTexture);
      // Fix KSP bug where the button always shows
      appButton.gameObject.SetActive(ApplicationLauncher.Instance.DetermineVisibility(appButton));
    }

    private void OnGUIApplicationLauncherUnreadifying (GameScenes scene) {
      // this actually means the game scene gets changed
      Close ();
    }

    private void OnHideUI () {
      PreciseManeuverConfig.Instance.UiActive = false;
      HideMenu ();
    }

    private void OnShowUI () {
      PreciseManeuverConfig.Instance.UiActive = true;
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
      if (!IsOn && !m_MenuPointerIn)
        Close ();
    }

    private void Close () {
      if (m_ToolbarMenu != null) {
        if (!m_ToolbarMenu.IsFadingOut)
          m_ToolbarMenu.FadeClose ();
        InputLockManager.RemoveControlLock ("PreciseManeuverMenuControlLock");
      } else if (m_MenuObject != null) {
        Destroy (m_MenuObject);
        InputLockManager.RemoveControlLock ("PreciseManeuverMenuControlLock");
      }
    }

    private void Open () {
      // fade menu in if already open
      if (m_ToolbarMenu != null && m_ToolbarMenu.IsFadingOut) {
        m_ToolbarMenu.FadeIn ();
        return;
      }

      if (m_MenuPrefab == null || m_MenuObject != null)
        return;

      m_MenuObject = Instantiate (m_MenuPrefab);
      if (m_MenuObject == null)
        return;

      m_MenuObject.transform.SetParent (MainCanvasUtil.MainCanvas.transform);
      m_MenuObject.transform.position = GetAnchor ();
      m_ToolbarMenu = m_MenuObject.GetComponent<UI.ToolbarMenu> ();
      if (m_ToolbarMenu != null) {
        m_ToolbarMenu.SetControl (this);
        if (!NodeTools.PatchedConicsUnlocked)
          m_ToolbarMenu.DisableMainWindow ();
      }
      GUIComponentManager.ProcessStyle (m_MenuObject);
      GUIComponentManager.ProcessLocalization (m_MenuObject);
      GUIComponentManager.ReplaceLabelsWithTMPro (m_MenuObject);
    }

    public void registerUpdateAction (UnityEngine.Events.UnityAction action) {
      PreciseManeuverConfig.Instance.ListenToShowChange (action);
    }

    public void deregisterUpdateAction (UnityEngine.Events.UnityAction action) {
      PreciseManeuverConfig.Instance.RemoveListener (action);
    }
  }
}
