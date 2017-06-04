/*******************************************************************************
 * Copyright (c) 2017, George Sedov
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
using UnityEngine.EventSystems;

namespace KSPPreciseManeuver.UI {
[RequireComponent (typeof (RectTransform))]
public class PreciseManeuverDropdownItem : MonoBehaviour, IPointerEnterHandler, ICancelHandler {
  [SerializeField]
  private Toggle m_Toggle = null;
  public Toggle toggle {
    get { return m_Toggle; }
    set {
      if (value.transform.IsChildOf (transform))
        m_Toggle = value;
    }
  }

  private int m_Index = -1;
  public int index {
    get { return m_Index; }
    internal set { m_Index = value; }
  }

  public virtual void OnPointerEnter (PointerEventData eventData) {
    EventSystem.current.SetSelectedGameObject (gameObject);
  }

  public virtual void OnCancel (BaseEventData eventData) {
    GetComponentInParent<PreciseManeuverDropdown> ()?.Hide ();
  }
}

[RequireComponent (typeof (RectTransform))]
public class PreciseManeuverDropdown : Selectable, IPointerClickHandler, ISubmitHandler, ICancelHandler {
  [SerializeField]
  private RectTransform m_Template;
  public RectTransform template { get { return m_Template; } set { m_Template = value; Refresh (); } }

  [SerializeField]
  private GameObject m_CaptionArea;
  public GameObject captionArea { get { return m_CaptionArea; } set { m_CaptionArea = value; Refresh (); } }
    
  [SerializeField]
  private Dropdown.DropdownEvent m_OnValueChanged = new Dropdown.DropdownEvent();
  public Dropdown.DropdownEvent onValueChanged { get { return m_OnValueChanged; } set { m_OnValueChanged = value; } }

  private GameObject m_Dropdown;
  private GameObject m_Blocker;
  private Canvas m_RootCanvas;
  public void setRootCanvas (Canvas rootCanvas) {
    m_RootCanvas = rootCanvas;
    if (template?.GetComponent<Canvas> () != null)
      template.GetComponent<Canvas> ().sortingLayerName = rootCanvas?.sortingLayerName;
  }
  private int m_Value = -1;
  private bool validTemplate = false;

  private int m_OptionCount = 0;
  public int optionCount {
    get { return m_OptionCount; }
    set {
      m_OptionCount = value;
      if (this.value >= value) {
        if (value > 0)
          setValueNoInvoke (0);
        else
          setValueNoInvoke (-1);
      }
      Refresh ();
    }
  }

  private UnityAction<int, GameObject> m_UpdateDropdownCaption = null;
  public UnityAction<int, GameObject> updateDropdownCaption {
    get { return m_UpdateDropdownCaption; }
    set { m_UpdateDropdownCaption = value; Refresh (); }
  }

  private UnityAction<PreciseManeuverDropdownItem> m_UpdateDropdownOption = null;
  public UnityAction<PreciseManeuverDropdownItem> updateDropdownOption {
    get { return m_UpdateDropdownOption; }
    set { m_UpdateDropdownOption = value; Hide (); }
  }

  public int value {
    get {
      return m_Value;
    }
    set {
      setValueNoInvoke (value);
      onValueChanged.Invoke (m_Value);
    }
  }

  public void setValueNoInvoke (int value) {
    if (value == m_Value || optionCount == 0)
      return;

    if (value > optionCount - 1 || value < 0)
      m_Value = 0;
    else
      m_Value = value;
    Refresh ();
  }

  protected override void Awake () {
    if (m_Template)
      m_Template.gameObject.SetActive (false);
  }

  protected override void OnDestroy () {
    Hide ();
    base.OnDestroy ();
  }

  void Refresh () {
    if (optionCount == 0)
      return;

    updateDropdownCaption?.Invoke (value, captionArea);
  }

  private void SetupTemplate () {
    validTemplate = false;

    if (!m_Template) {
      return;
    }

    GameObject templateGo = m_Template.gameObject;
    templateGo.SetActive (true);
    var prefabs = templateGo.GetComponentsInChildren<PreciseManeuverDropdownItem> (true);
    if (prefabs.Length != 1) {
        templateGo.SetActive (false);
        return;
    }
    Canvas popupCanvas = GetOrAddComponent<Canvas>(templateGo);
    popupCanvas.overrideSorting = true;
    popupCanvas.sortingOrder = 30000;
    popupCanvas.sortingLayerName = m_RootCanvas?.sortingLayerName;

    GetOrAddComponent<GraphicRaycaster> (templateGo);
    GetOrAddComponent<CanvasGroup> (templateGo);
    templateGo.SetActive (false);
    validTemplate = true;
  }

  private static T GetOrAddComponent<T> (GameObject go) where T : Component {
    T comp = go.GetComponent<T>();
    if (!comp)
      comp = go.AddComponent<T> ();
    return comp;
  }

  public virtual void OnPointerClick (PointerEventData eventData) {
    Show ();
  }

  public virtual void OnSubmit (BaseEventData eventData) {
    Show ();
  }

  public virtual void OnCancel (BaseEventData eventData) {
    Hide ();
  }

  public void Show () {
    if (!IsActive () || !IsInteractable () || m_Dropdown != null || updateDropdownOption == null)
      return;

    if (!validTemplate) {
      SetupTemplate ();
      if (!validTemplate)
        return;
    }

    m_Template.gameObject.SetActive (true);

    // Instantiate the drop-down template
    m_Dropdown = Instantiate (template.gameObject);
    m_Dropdown.name = "Dropdown List";
    m_Dropdown.SetActive (true);

    RectTransform dropdownRectTransform = m_Dropdown.transform as RectTransform;
    dropdownRectTransform.SetParent (template.parent, false);
    var layout = GetOrAddComponent<VerticalLayoutGroup> (m_Dropdown);
    layout.childForceExpandHeight = false;
    layout.childForceExpandWidth = true;
    var fitter = GetOrAddComponent<ContentSizeFitter> (m_Dropdown);
    fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
    fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

    PreciseManeuverDropdownItem itemPrefab = m_Dropdown.GetComponentInChildren<PreciseManeuverDropdownItem> ();
    itemPrefab.gameObject.SetActive (true);

    Toggle prev = null;
    for (int i = 0; i < optionCount; ++i) {
      PreciseManeuverDropdownItem item = AddItem(i, itemPrefab);
      if (item == null)
        continue;
      // Automatically set up a toggle state change listener
      item.toggle.isOn = (value == i);
      item.toggle.onValueChanged.AddListener (x => OnSelectItem (item.index));

      // Select current option
      if (item.toggle.isOn)
        item.toggle.Select ();

      // Automatically set up explicit navigation
      if (prev != null) {
        Navigation prevNav = prev.navigation;
        Navigation toggleNav = item.toggle.navigation;
        prevNav.mode = Navigation.Mode.Explicit;
        toggleNav.mode = Navigation.Mode.Explicit;

        prevNav.selectOnDown = item.toggle;
        prevNav.selectOnRight = item.toggle;
        toggleNav.selectOnLeft = prev;
        toggleNav.selectOnUp = prev;

        prev.navigation = prevNav;
        item.toggle.navigation = toggleNav;
      }
      prev = item.toggle;
    }

    // Invert anchoring and position if dropdown is partially or fully outside of canvas rect.
    // Typically this will have the effect of placing the dropdown above the button instead of below,
    // but it works as inversion regardless of initial setup.
    Vector3[] corners = new Vector3[4];
    dropdownRectTransform.GetWorldCorners (corners);
    bool outside = false;
    RectTransform rootCanvasRectTransform = m_RootCanvas.transform as RectTransform;
    for (int i = 0; i < 4; i++) {
      Vector3 corner = rootCanvasRectTransform.InverseTransformPoint(corners[i]);
      if (!rootCanvasRectTransform.rect.Contains (corner)) {
        outside = true;
        break;
      }
    }
    if (outside) {
      RectTransformUtility.FlipLayoutOnAxis (dropdownRectTransform, 0, false, false);
      RectTransformUtility.FlipLayoutOnAxis (dropdownRectTransform, 1, false, false);
    }

    // Fade in the popup
    SetAlpha (0f);
    AlphaFadeList (0.15f, 1f);

    // Make drop-down template and item template inactive
    template.gameObject.SetActive (false);
    itemPrefab.gameObject.SetActive (false);
    Destroy (itemPrefab);

    m_Blocker = CreateBlocker (m_RootCanvas);
  }

  protected virtual GameObject CreateBlocker (Canvas rootCanvas) {
    GameObject blocker = new GameObject("Blocker");

    // Setup blocker RectTransform to cover entire root canvas area.
    RectTransform blockerRect = blocker.AddComponent<RectTransform>();
    blockerRect.SetParent (rootCanvas.transform, false);
    blockerRect.anchorMin = Vector3.zero;
    blockerRect.anchorMax = Vector3.one;
    blockerRect.sizeDelta = Vector2.zero;

    // Make blocker be in separate canvas in same layer as dropdown and in layer just below it.
    Canvas blockerCanvas = blocker.AddComponent<Canvas>();
    blockerCanvas.overrideSorting = true;
    Canvas dropdownCanvas = m_Dropdown.GetComponent<Canvas>();
    blockerCanvas.sortingLayerID = dropdownCanvas.sortingLayerID;
    blockerCanvas.sortingOrder = dropdownCanvas.sortingOrder - 1;

    // Add raycaster since it's needed to block.
    blocker.AddComponent<GraphicRaycaster> ();

    // Add image since it's needed to block, but make it clear.
    Image blockerImage = blocker.AddComponent<Image>();
    blockerImage.color = Color.clear;

    // Add button since it's needed to block, and to close the dropdown when blocking area is clicked.
    Button blockerButton = blocker.AddComponent<Button>();
    blockerButton.onClick.AddListener (Hide);

    return blocker;
  }

  private PreciseManeuverDropdownItem AddItem (int index, PreciseManeuverDropdownItem itemTemplate) {
    GameObject go = Instantiate (itemTemplate.gameObject);
    PreciseManeuverDropdownItem item = go.GetComponent<PreciseManeuverDropdownItem> ();
    go.name = gameObject.name + " item " + index.ToString ();
    item.index = index;
    var layout = GetOrAddComponent<LayoutElement> (go);
    layout.preferredHeight = (go.transform as RectTransform).rect.height;

    if (item.toggle != null) {
      item.toggle.isOn = false;
    }
    updateDropdownOption?.Invoke (item);

    go.transform.SetParent (itemTemplate.transform.parent, false);
    go.SetActive (true);

    return item;
  }

  private void AlphaFadeList (float duration, float alpha) {
    CanvasGroup group = m_Dropdown.GetComponent<CanvasGroup>();
    StartCoroutine (AlphaFadeList (duration, group.alpha, alpha));
  }

  private void SetAlpha (float alpha) {
    if (!m_Dropdown)
      return;
    CanvasGroup group = m_Dropdown.GetComponent<CanvasGroup>();
    group.alpha = alpha;
  }

  public void Hide () {
    if (m_Dropdown != null) {
      AlphaFadeList (0.15f, 0f);
      StartCoroutine (DelayedDestroyDropdownList (0.15f));
    }
    if (m_Blocker != null)
      Destroy (m_Blocker);
    m_Blocker = null;
    Select ();
  }

  private System.Collections.IEnumerator DelayedDestroyDropdownList (float delay) {
    yield return new WaitForSeconds (delay);
    if (m_Dropdown != null)
      Destroy (m_Dropdown);
    m_Dropdown = null;
  }

  private System.Collections.IEnumerator AlphaFadeList (float duration, float start, float end) {
    // wait for end of frame so that only the last call to fade that frame is honoured.
    yield return new WaitForEndOfFrame ();

    float progress = 0.0f;

    while (progress <= 1.0f) {
      progress += Time.deltaTime / duration;
      SetAlpha (Mathf.Lerp (start, end, progress));
      yield return null;
    }
  }

  private void OnSelectItem (int index) {
    if (index < 0)
      return;

    value = index;
    Hide ();
  }
}
}