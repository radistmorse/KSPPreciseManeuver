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
using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KSPPreciseManeuver.UI {
  [RequireComponent (typeof (RectTransform))]
  public class PreciseManeuverDropdownItem : MonoBehaviour, IPointerEnterHandler, ICancelHandler {
    [SerializeField]
    private RectTransform m_RectTransform;
    [SerializeField]
    private Toggle m_Toggle;

    public RectTransform rectTransform { get { return m_RectTransform; } set { m_RectTransform = value; } }
    public Toggle Toggle { get { return m_Toggle; } set { m_Toggle = value; } }
    public int Index { get; set; }

    public virtual void OnPointerEnter (PointerEventData eventData) {
      EventSystem.current.SetSelectedGameObject (gameObject);
    }

    public virtual void OnCancel (BaseEventData eventData) {
      PreciseManeuverDropdown dropdown = GetComponentInParent<PreciseManeuverDropdown>();
      if (dropdown)
        dropdown.Hide ();
    }
  }


  [RequireComponent (typeof (RectTransform))]
  public class PreciseManeuverDropdown : Selectable, IPointerClickHandler, ISubmitHandler, ICancelHandler {
    [SerializeField]
    private RectTransform m_Template;
    public RectTransform Template { get { return m_Template; } set { m_Template = value; RefreshShownValue (); } }

    [SerializeField]
    private GameObject m_CaptionArea;
    public GameObject CaptionArea { get { return m_CaptionArea; } set { m_CaptionArea = value; RefreshShownValue (); } }

    [SerializeField]
    private Dropdown.DropdownEvent m_OnValueChanged = new Dropdown.DropdownEvent();
    public Dropdown.DropdownEvent OnValueChanged { get { return m_OnValueChanged; } set { m_OnValueChanged = value; } }
    
    [SerializeField]
    private float m_AlphaFadeSpeed = 0.15f;
    public float AlphaFadeSpeed { get { return m_AlphaFadeSpeed; } set { m_AlphaFadeSpeed = value; } }

    private UnityAction<int, GameObject> updateDropdownCaption = null;
    public UnityAction<int, GameObject> UpdateDropdownCaption {
      get { return updateDropdownCaption; }
      set { updateDropdownCaption = value; RefreshShownValue (); }
    }

    private UnityAction<PreciseManeuverDropdownItem> updateDropdownOption = null;
    public UnityAction<PreciseManeuverDropdownItem> UpdateDropdownOption {
      get { return updateDropdownOption; }
      set { updateDropdownOption = value; Hide (); }
    }

    private GameObject m_Dropdown;
    private GameObject m_Blocker;
    private List<PreciseManeuverDropdownItem> m_Items = new List<PreciseManeuverDropdownItem>();
    private IEnumerator m_FadeCoroutine;
    private bool validTemplate = false;
    private int m_Value = -1;

    public int Value {
      get {
        return m_Value;
      }
      set {
        Set (value);
      }
    }

    private int m_OptionsCount = -1;
    public int OptionsCount {
      get { return m_OptionsCount; }
      set { m_OptionsCount = value; if (this.Value >= value) { this.SetValueWithoutNotify(value > 0 ? 0 : -1); } }
    }

    public void SetValueWithoutNotify (int input) {
      Set (input, false);
    }

    void Set (int value, bool sendCallback = true) {
      if (Application.isPlaying && (value == m_Value || OptionsCount == 0))
        return;

      m_Value = Mathf.Clamp (value, 0, OptionsCount - 1);
      RefreshShownValue ();

      if (sendCallback) {
        // Notify all listeners
        UISystemProfilerApi.AddMarker ("Dropdown.value", this);
        m_OnValueChanged.Invoke (m_Value);
      }
    }

    protected PreciseManeuverDropdown () { }

    protected override void Awake () {
      if (m_Template)
        m_Template.gameObject.SetActive (false);
    }

    protected override void Start () {
      base.Start ();

      RefreshShownValue ();
    }

    protected override void OnDisable () {
      //Destroy dropdown and blocker in case user deactivates the dropdown when they click an option (case 935649)
      ImmediateDestroyDropdownList ();

      if (m_Blocker != null)
        DestroyBlocker (m_Blocker);
      m_Blocker = null;

      base.OnDisable ();
    }

    public void RefreshShownValue () {
      if (OptionsCount > 0)
        UpdateDropdownCaption?.Invoke (Value, CaptionArea);
    }

    private void SetupTemplate () {
      validTemplate = false;

      if (!m_Template) {
        Debug.LogError ("The dropdown template is not assigned. The template needs to be assigned and must have a child GameObject with a Toggle component serving as the item.", this);
        return;
      }

      GameObject templateGo = m_Template.gameObject;
      templateGo.SetActive (true);
      Toggle itemToggle = m_Template.GetComponentInChildren<Toggle>();

      validTemplate = true;
      if (!itemToggle || itemToggle.transform == Template) {
        validTemplate = false;
        Debug.LogError ("The dropdown template is not valid. The template must have a child GameObject with a Toggle component serving as the item.", Template);
      } else if (!(itemToggle.transform.parent is RectTransform)) {
        validTemplate = false;
        Debug.LogError ("The dropdown template is not valid. The child GameObject with a Toggle component (the item) must have a RectTransform on its parent.", Template);
      }

      if (!validTemplate) {
        templateGo.SetActive (false);
        return;
      }

      var items = templateGo.GetComponentsInChildren<PreciseManeuverDropdownItem> (true);
      if (items.Length != 1) {
        templateGo.SetActive (false);
        validTemplate = false;
        return;
      }

      // Find the Canvas that this dropdown is a part of
      Canvas parentCanvas = null;
      Transform parentTransform = m_Template.parent;
      while (parentTransform != null) {
        parentCanvas = parentTransform.GetComponent<Canvas> ();
        if (parentCanvas != null)
          break;

        parentTransform = parentTransform.parent;
      }

      Canvas popupCanvas = GetOrAddComponent<Canvas>(templateGo);
      popupCanvas.overrideSorting = true;
      popupCanvas.sortingOrder = 30000;

      // If we have a parent canvas, apply the same raycasters as the parent for consistency.
      if (parentCanvas != null) {
        Component[] components = parentCanvas.GetComponents<BaseRaycaster>();
        for (int i = 0; i < components.Length; i++) {
          Type raycasterType = components[i].GetType();
          if (templateGo.GetComponent (raycasterType) == null) {
            templateGo.AddComponent (raycasterType);
          }
        }
      } else {
        GetOrAddComponent<GraphicRaycaster> (templateGo);
      }

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
      if (!IsActive () || !IsInteractable () || m_Dropdown != null)
        return;




      // Get root Canvas.
      var list = gameObject.GetComponentsInParent<Canvas> (false);
      Canvas rootCanvas = list.LastOrDefault(c => c.isRootCanvas) ?? list.LastOrDefault();
      if (rootCanvas == null)
        return;


      if (!validTemplate) {
        SetupTemplate ();
        if (!validTemplate)
          return;
      }

      m_Template.gameObject.SetActive (true);

      // popupCanvas used to assume the root canvas had the default sorting Layer, next line fixes (case 958281 - [UI] Dropdown list does not copy the parent canvas layer when the panel is opened)
      m_Template.GetComponent<Canvas> ().sortingLayerID = rootCanvas.sortingLayerID;

      // Instantiate the drop-down template
      m_Dropdown = CreateDropdownList (m_Template.gameObject);
      m_Dropdown.name = "Dropdown List";
      m_Dropdown.SetActive (true);

      // Make drop-down RectTransform have same values as original.
      RectTransform dropdownRectTransform = m_Dropdown.transform as RectTransform;
      dropdownRectTransform.SetParent (m_Template.transform.parent, false);

      // Instantiate the drop-down list items

      // Find the dropdown item and disable it.
      PreciseManeuverDropdownItem itemTemplate = m_Dropdown.GetComponentInChildren<PreciseManeuverDropdownItem>();

      GameObject content = itemTemplate.rectTransform.parent.gameObject;
      RectTransform contentRectTransform = content.transform as RectTransform;
      itemTemplate.rectTransform.gameObject.SetActive (true);

      // Get the rects of the dropdown and item
      Rect dropdownContentRect = contentRectTransform.rect;
      Rect itemTemplateRect = itemTemplate.rectTransform.rect;

      // Calculate the visual offset between the item's edges and the background's edges
      Vector2 offsetMin = itemTemplateRect.min - dropdownContentRect.min + (Vector2)itemTemplate.rectTransform.localPosition;
      Vector2 offsetMax = itemTemplateRect.max - dropdownContentRect.max + (Vector2)itemTemplate.rectTransform.localPosition;
      Vector2 itemSize = itemTemplateRect.size;

      m_Items.Clear ();

      Toggle prev = null;
      for (int i = 0; i < OptionsCount; ++i) {
        PreciseManeuverDropdownItem item = AddItem(Value == i, itemTemplate, m_Items.Count);
        if (item == null)
          continue;

        // Automatically set up a toggle state change listener
        item.Toggle.isOn = Value == i;
        item.Toggle.onValueChanged.AddListener (x => OnSelectItem (item));

        // Select current option
        if (item.Toggle.isOn)
          item.Toggle.Select ();

        // Automatically set up explicit navigation
        if (prev != null) {
          Navigation prevNav = prev.navigation;
          Navigation toggleNav = item.Toggle.navigation;
          prevNav.mode = Navigation.Mode.Explicit;
          toggleNav.mode = Navigation.Mode.Explicit;

          prevNav.selectOnDown = item.Toggle;
          prevNav.selectOnRight = item.Toggle;
          toggleNav.selectOnLeft = prev;
          toggleNav.selectOnUp = prev;

          prev.navigation = prevNav;
          item.Toggle.navigation = toggleNav;
        }
        prev = item.Toggle;

        m_Items.Add (item);
      }

      // Reposition all items now that all of them have been added
      Vector2 sizeDelta = contentRectTransform.sizeDelta;
      sizeDelta.y = itemSize.y * m_Items.Count + offsetMin.y - offsetMax.y;
      contentRectTransform.sizeDelta = sizeDelta;

      float extraSpace = dropdownRectTransform.rect.height - contentRectTransform.rect.height;
      if (extraSpace > 0)
        dropdownRectTransform.sizeDelta = new Vector2 (dropdownRectTransform.sizeDelta.x, dropdownRectTransform.sizeDelta.y - extraSpace);

      // Invert anchoring and position if dropdown is partially or fully outside of canvas rect.
      // Typically this will have the effect of placing the dropdown above the button instead of below,
      // but it works as inversion regardless of initial setup.
      Vector3[] corners = new Vector3[4];
      dropdownRectTransform.GetWorldCorners (corners);

      RectTransform rootCanvasRectTransform = rootCanvas.transform as RectTransform;
      Rect rootCanvasRect = rootCanvasRectTransform.rect;
      for (int axis = 0; axis < 2; axis++) {
        bool outside = false;
        for (int i = 0; i < 4; i++) {
          Vector3 corner = rootCanvasRectTransform.InverseTransformPoint(corners[i]);
          if ((corner[axis] < rootCanvasRect.min[axis] && !Mathf.Approximately (corner[axis], rootCanvasRect.min[axis])) ||
              (corner[axis] > rootCanvasRect.max[axis] && !Mathf.Approximately (corner[axis], rootCanvasRect.max[axis]))) {
            outside = true;
            break;
          }
        }
        if (outside)
          RectTransformUtility.FlipLayoutOnAxis (dropdownRectTransform, axis, false, false);
      }

      for (int i = 0; i < m_Items.Count; i++) {
        RectTransform itemRect = m_Items[i].rectTransform;
        itemRect.anchorMin = new Vector2 (itemRect.anchorMin.x, 0);
        itemRect.anchorMax = new Vector2 (itemRect.anchorMax.x, 0);
        itemRect.anchoredPosition = new Vector2 (itemRect.anchoredPosition.x, offsetMin.y + itemSize.y * (m_Items.Count - 1 - i) + itemSize.y * itemRect.pivot.y);
        itemRect.sizeDelta = new Vector2 (itemRect.sizeDelta.x, itemSize.y);
      }

      // Fade in the popup
      AlphaFadeList (m_AlphaFadeSpeed, 0f, 1f);

      // Make drop-down template and item template inactive
      m_Template.gameObject.SetActive (false);
      itemTemplate.gameObject.SetActive (false);

      m_Blocker = CreateBlocker (rootCanvas);
    }

    protected virtual GameObject CreateBlocker (Canvas rootCanvas) {
      // Create blocker GameObject.
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

      // Find the Canvas that this dropdown is a part of
      Canvas parentCanvas = null;
      Transform parentTransform = m_Template.parent;
      while (parentTransform != null) {
        parentCanvas = parentTransform.GetComponent<Canvas> ();
        if (parentCanvas != null)
          break;

        parentTransform = parentTransform.parent;
      }

      // If we have a parent canvas, apply the same raycasters as the parent for consistency.
      if (parentCanvas != null) {
        Component[] components = parentCanvas.GetComponents<BaseRaycaster>();
        for (int i = 0; i < components.Length; i++) {
          Type raycasterType = components[i].GetType();
          if (blocker.GetComponent (raycasterType) == null) {
            blocker.AddComponent (raycasterType);
          }
        }
      } else {
        // Add raycaster since it's needed to block.
        GetOrAddComponent<GraphicRaycaster> (blocker);
      }

      // Add image since it's needed to block, but make it clear.
      Image blockerImage = blocker.AddComponent<Image>();
      blockerImage.color = Color.clear;

      // Add button since it's needed to block, and to close the dropdown when blocking area is clicked.
      Button blockerButton = blocker.AddComponent<Button>();
      blockerButton.onClick.AddListener (Hide);

      return blocker;
    }

    protected virtual void DestroyBlocker (GameObject blocker) {
      Destroy (blocker);
    }

    protected virtual GameObject CreateDropdownList (GameObject template) {
      return (GameObject)Instantiate (template);
    }

    protected virtual void DestroyDropdownList (GameObject dropdownList) {
      Destroy (dropdownList);
    }

    protected virtual PreciseManeuverDropdownItem CreateItem (PreciseManeuverDropdownItem itemTemplate) {
      return (PreciseManeuverDropdownItem)Instantiate (itemTemplate);
    }

    protected virtual void DestroyItem (PreciseManeuverDropdownItem item) { }

    private PreciseManeuverDropdownItem AddItem (bool selected, PreciseManeuverDropdownItem itemTemplate, int index) {
      PreciseManeuverDropdownItem item = CreateItem(itemTemplate);
      item.rectTransform.SetParent (itemTemplate.rectTransform.parent, false);

      item.gameObject.name = "Item " + index;
      item.Index = index;
      if (item.Toggle != null) {
        item.Toggle.isOn = false;
      }
      UpdateDropdownOption?.Invoke (item);

      item.gameObject.SetActive (true);

      return item;
    }

    private void AlphaFadeList (float duration, float alpha) {
      CanvasGroup group = m_Dropdown.GetComponent<CanvasGroup>();
      if (group.alpha == alpha)
        return;
      if (m_FadeCoroutine != null)
        StopCoroutine (m_FadeCoroutine);
      if (!gameObject.activeInHierarchy) {
        SetAlpha (alpha);
        return;
      }
      m_FadeCoroutine = AlphaFadeList (duration, group.alpha, alpha);
      StartCoroutine (m_FadeCoroutine);
    }

    private IEnumerator AlphaFadeList (float duration, float start, float end) {
      var elapsedTime = 0.0f;
      while (elapsedTime < duration) {
        elapsedTime += Time.unscaledDeltaTime;
        var percentage = Mathf.Clamp01(elapsedTime / duration);
        SetAlpha (Mathf.Lerp (start, end, percentage));
        yield return null;
      }
      SetAlpha (end);
    }

    private void SetAlpha (float alpha) {
      if (!m_Dropdown)
        return;
      CanvasGroup group = m_Dropdown.GetComponent<CanvasGroup>();
      group.alpha = alpha;
    }

    public void Hide () {
      if (m_Dropdown != null) {
        AlphaFadeList (m_AlphaFadeSpeed, 0f);

        // User could have disabled the dropdown during the OnValueChanged call.
        if (IsActive ())
          StartCoroutine (DelayedDestroyDropdownList (m_AlphaFadeSpeed));
      }
      if (m_Blocker != null)
        DestroyBlocker (m_Blocker);
      m_Blocker = null;
      Select ();
    }

    private IEnumerator DelayedDestroyDropdownList (float delay) {
      yield return new WaitForSecondsRealtime (delay);
      ImmediateDestroyDropdownList ();
    }

    private void ImmediateDestroyDropdownList () {
      for (int i = 0; i < m_Items.Count; i++) {
        if (m_Items[i] != null)
          DestroyItem (m_Items[i]);
      }
      m_Items.Clear ();
      if (m_Dropdown != null)
        DestroyDropdownList (m_Dropdown);
      m_Dropdown = null;
    }

    // Change the value and hide the dropdown.
    private void OnSelectItem (PreciseManeuverDropdownItem item) {
      if (!item.Toggle.isOn)
        item.Toggle.isOn = true;

      Value = item.Index;
      Hide ();
    }
  }
}