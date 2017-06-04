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
using System.Collections.Generic;

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
  private List<string> presetCache = new List<string> ();
  private ISaverControl m_saverControl = null;

  public void SetControl (ISaverControl saverControl) {
    m_saverControl = saverControl;
    m_Chooser.updateDropdownCaption = setChooserText;
    m_Chooser.updateDropdownOption = setChooserOption;
    m_Chooser.setRootCanvas (saverControl.Canvas);

    chooserText = saverControl.replaceTextComponentWithTMPro (m_Chooser.captionArea.GetComponent<Text> ());
    saverControl.replaceInputFieldWithTMPro (m_NameInput, inputFieldSubmit, inputFieldChange);
    switchChooser ();
    repopulateChooser ();
  }

  public void OnDestroy () {
    m_Chooser.Hide ();
    m_saverControl = null;
  }

  public void SaveButtonAction () {
    if (m_Chooser.value != 0)
      m_saverControl.AddPreset (presetCache[m_Chooser.value - 1]);
  }

  public void SaveAsButtonAction () {
    switchSaver ();
    var text = m_saverControl.suggestPresetName ();
    m_saverControl.TMProText = text;
    m_saverControl.TMProActivateInputField ();
    m_saverControl.TMProSelectAllText ();
    inputFieldChange (text);
  }

  public void DelButtonAction () {
    if (m_Chooser.value != 0)
      m_saverControl.RemovePreset (presetCache[m_Chooser.value - 1]);
    repopulateChooser ();

  }

  public void okButtonAction () {
    var text = m_saverControl.TMProText;
    if (text.Length > 0) {
      m_saverControl.AddPreset (text);
      repopulateChooser ();
      var items = presetCache.FindAll (a => (a == text));
      if (items.Count == 1)
        m_Chooser.setValueNoInvoke (presetCache.FindIndex (a => (a == text)) + 1);
      switchChooser ();
      updateControls ();
    }
  }

  public void cancelButtonAction () {
    switchChooser ();
    updateControls ();
  }

  public void inputFieldChange (string text) {
    if (text.Length > 0) {
      m_ButtonOk.interactable = true;
      m_ButtonOk.GetComponent<Image> ().color = new Color (1.0f, 1.0f, 1.0f, 1.0f);
    } else {
      m_ButtonOk.interactable = false;
      m_ButtonOk.GetComponent<Image> ().color = new Color (0.0f, 0.0f, 0.0f, 0.25f);
    }
  }

  public void inputFieldSubmit (string text) {
    if (Input.GetKeyDown (KeyCode.Return) || Input.GetKeyDown (KeyCode.KeypadEnter)) {
      okButtonAction ();
    }
  }

  private void switchSaver () {
    var canvasgroup = m_ChooserPanel.GetComponent<CanvasGroup> ();
    canvasgroup.interactable = false;
    canvasgroup.blocksRaycasts = false;
    m_ChooserPanel.GetComponent<CanvasGroupFader> ().fadeOut ();

    m_SaverPanel.GetComponent<CanvasGroupFader> ().fadeIn ();
    canvasgroup = m_SaverPanel.GetComponent<CanvasGroup> ();
    canvasgroup.interactable = true;
    canvasgroup.blocksRaycasts = true;
  }

  private void switchChooser () {
    var canvasgroup = m_SaverPanel.GetComponent<CanvasGroup> ();
    canvasgroup.interactable = false;
    canvasgroup.blocksRaycasts = false;
    m_SaverPanel.GetComponent<CanvasGroupFader> ().fadeOut ();

    m_ChooserPanel.GetComponent<CanvasGroupFader> ().fadeIn ();
    canvasgroup = m_ChooserPanel.GetComponent<CanvasGroup> ();
    canvasgroup.interactable = true;
    canvasgroup.blocksRaycasts = true;
  }

  public void updateControls () {
    if (m_Chooser.value > 0) {
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

  public void repopulateChooser () {
    presetCache = m_saverControl.presetNames ();
    m_Chooser.optionCount = 1 + presetCache.Count;
    m_Chooser.setValueNoInvoke (0);
    updateControls ();
  }

  private void setChooserText (int index, GameObject caption) {
    if (index < 0 || index > presetCache.Count)
      return;
    if (index == 0)
      chooserText (m_saverControl.newPresetLocalized);
    else
      chooserText (presetCache[index - 1]);
  }

  private void setChooserOption (PreciseManeuverDropdownItem item) {
    if (item.index == 0)
      m_saverControl.replaceTextComponentWithTMPro (item.GetComponentInChildren<Text> ())?.Invoke (m_saverControl.newPresetLocalized);
    else
      m_saverControl.replaceTextComponentWithTMPro (item.GetComponentInChildren<Text> ())?.Invoke (presetCache[item.index - 1]);
  }

  public void chooserValueChange (int value) {
    if (value == 0) {
      SaveAsButtonAction ();
    } else {
      m_saverControl.loadPreset (presetCache[value - 1]);
    }
    updateControls ();
  }

  public void itemClicked () {
    if (savedChooserValue != m_Chooser.value) {
      savedChooserValue = m_Chooser.value;
    } else {
      chooserValueChange (savedChooserValue);
    }
  }

  public void inputFieldSelected () {
    m_saverControl.lockKeyboard ();
  }
  public void inputFieldDeselected () {
    m_saverControl.unlockKeyboard ();
  }
}
}
