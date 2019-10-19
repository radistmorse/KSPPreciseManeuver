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
  [RequireComponent (typeof (RectTransform))]
  public class SaverControl : MonoBehaviour {
    [SerializeField]
    private Button m_ButtonSave = null;
    [SerializeField]
    private Button m_ButtonDel = null;
    [SerializeField]
    private Button m_ButtonOk = null;
    [SerializeField]
    private PreciseManeuverDropdown m_Chooser = null;
    private UnityAction<string> chooserText = null;
    [SerializeField]
    private InputField m_NameInput = null;
    [SerializeField]
    private GameObject m_ChooserPanel = null;
    [SerializeField]
    private GameObject m_SaverPanel = null;

    private int savedChooserValue = 0;
    private System.Collections.Generic.List<string> presetCache = new System.Collections.Generic.List<string> ();
    private ISaverControl m_Control = null;

    public void SetControl (ISaverControl control) {
      m_Control = control;
      m_Chooser.UpdateDropdownCaption = SetChooserText;
      m_Chooser.UpdateDropdownOption = SetChooserOption;

      chooserText = m_Control.ReplaceTextComponentWithTMPro (m_Chooser.CaptionArea.GetComponent<Text> ());
      m_Control.ReplaceInputFieldWithTMPro (m_NameInput, InputFieldSubmit, InputFieldChange);
      SwitchChooser ();
      RepopulateChooser ();
    }

    public void OnDestroy () {
      m_Chooser.Hide ();
      m_Control = null;
    }

    public void SaveButtonAction () {
      if (m_Chooser.Value != 0)
        m_Control.AddPreset (presetCache[m_Chooser.Value - 1]);
    }

    public void SaveAsButtonAction () {
      SwitchSaver ();
      var text = m_Control.suggestPresetName ();
      m_Control.TMProText = text;
      m_Control.TMProActivateInputField ();
      m_Control.TMProSelectAllText ();
      InputFieldChange (text);
    }

    public void DelButtonAction () {
      if (m_Chooser.Value != 0)
        m_Control.RemovePreset (presetCache[m_Chooser.Value - 1]);
      RepopulateChooser ();

    }

    public void OKButtonAction () {
      var text = m_Control.TMProText;
      if (text.Length > 0) {
        m_Control.AddPreset (text);
        RepopulateChooser ();
        var items = presetCache.FindAll (a => (a == text));
        if (items.Count == 1)
          m_Chooser.SetValueWithoutNotify (presetCache.FindIndex (a => (a == text)) + 1);
        SwitchChooser ();
        UpdateControls ();
      }
    }

    public void CancelButtonAction () {
      SwitchChooser ();
      UpdateControls ();
    }

    public void InputFieldChange (string text) {
      if (text.Length > 0) {
        m_ButtonOk.interactable = true;
        m_ButtonOk.GetComponent<Image> ().color = new Color (1.0f, 1.0f, 1.0f, 1.0f);
      } else {
        m_ButtonOk.interactable = false;
        m_ButtonOk.GetComponent<Image> ().color = new Color (0.0f, 0.0f, 0.0f, 0.25f);
      }
    }

    public void InputFieldSubmit (string text) {
      if (Input.GetKeyDown (KeyCode.Return) || Input.GetKeyDown (KeyCode.KeypadEnter)) {
        OKButtonAction ();
      }
    }

    private void SwitchSaver () {
      var canvasgroup = m_ChooserPanel.GetComponent<CanvasGroup> ();
      canvasgroup.interactable = false;
      canvasgroup.blocksRaycasts = false;
      m_ChooserPanel.GetComponent<CanvasGroupFader> ().FadeOut ();

      m_SaverPanel.GetComponent<CanvasGroupFader> ().FadeIn ();
      canvasgroup = m_SaverPanel.GetComponent<CanvasGroup> ();
      canvasgroup.interactable = true;
      canvasgroup.blocksRaycasts = true;
    }

    private void SwitchChooser () {
      var canvasgroup = m_SaverPanel.GetComponent<CanvasGroup> ();
      canvasgroup.interactable = false;
      canvasgroup.blocksRaycasts = false;
      m_SaverPanel.GetComponent<CanvasGroupFader> ().FadeOut ();

      m_ChooserPanel.GetComponent<CanvasGroupFader> ().FadeIn ();
      canvasgroup = m_ChooserPanel.GetComponent<CanvasGroup> ();
      canvasgroup.interactable = true;
      canvasgroup.blocksRaycasts = true;
    }

    public void UpdateControls () {
      if (m_Chooser.Value > 0) {
        m_ButtonSave.interactable = true;
        m_ButtonSave.GetComponent<Image> ().color = new Color (1.0f, 1.0f, 1.0f, 1.0f);
        m_ButtonDel.interactable = true;
        m_ButtonDel.GetComponent<Image> ().color = new Color (1.0f, 1.0f, 1.0f, 1.0f);
      } else {
        m_ButtonSave.interactable = false;
        m_ButtonSave.GetComponent<Image> ().color = new Color (0.0f, 0.0f, 0.0f, 0.25f);
        m_ButtonDel.interactable = false;
        m_ButtonDel.GetComponent<Image> ().color = new Color (0.0f, 0.0f, 0.0f, 0.25f);
      }
    }

    public void RepopulateChooser () {
      presetCache = m_Control.presetNames ();
      m_Chooser.OptionsCount = 1 + presetCache.Count;
      m_Chooser.SetValueWithoutNotify (0);
      UpdateControls ();
    }

    private void SetChooserText (int index, GameObject caption) {
      if (index < 0 || index > presetCache.Count)
        return;
      if (index == 0)
        chooserText (m_Control.newPresetLocalized);
      else
        chooserText (presetCache[index - 1]);
    }

    private void SetChooserOption (PreciseManeuverDropdownItem item) {
      if (item.Index == 0)
        m_Control.ReplaceTextComponentWithTMPro (item.GetComponentInChildren<Text> ())?.Invoke (m_Control.newPresetLocalized);
      else
        m_Control.ReplaceTextComponentWithTMPro (item.GetComponentInChildren<Text> ())?.Invoke (presetCache[item.Index - 1]);
    }

    public void ChooserValueChange (int value) {
      if (value == 0) {
        SaveAsButtonAction ();
      } else {
        m_Control.loadPreset (presetCache[value - 1]);
      }
      UpdateControls ();
    }

    public void ItemClicked () {
      if (savedChooserValue != m_Chooser.Value) {
        savedChooserValue = m_Chooser.Value;
      } else {
        ChooserValueChange (savedChooserValue);
      }
    }

    public void InputFieldSelected () {
      m_Control.lockKeyboard ();
    }
    public void InputFieldDeselected () {
      m_Control.unlockKeyboard ();
    }
  }
}
