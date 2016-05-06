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
using System.Linq;

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
  private Dropdown m_Chooser = null;
  [SerializeField]
  private InputField m_NameInput = null;
  [SerializeField]
  private GameObject m_ChooserPanel = null;
  [SerializeField]
  private GameObject m_SaverPanel = null;

  private int savedChooserValue = 0;

  private ISaverControl m_saverControl = null;

  public void SetControl (ISaverControl saverControl) {
    m_saverControl = saverControl;
    updateControls ();
    foreach (var fixer in GetComponentsInChildren<CanvasFixer> (true))
      fixer.m_canvasLayer = saverControl.CanvasName;
    repopulateChooser ();
  }

  public void OnDestroy () {
    m_saverControl = null;
  }

  public void SaveButtonAction () {
    if (m_Chooser.value != 0)
      m_saverControl.AddPreset (m_Chooser.options[m_Chooser.value].text);
  }

  public void SaveAsButtonAction () {
    switchSaver ();
    m_NameInput.text = m_saverControl.suggestPresetName ();
    m_NameInput.Select ();
    m_NameInput.ActivateInputField ();
    if (m_NameInput.text.Length > 0) {
      m_NameInput.caretPosition = m_NameInput.text.Length;
      m_NameInput.selectionAnchorPosition = 0;
      m_NameInput.selectionFocusPosition = m_NameInput.text.Length;
    } else {
      inputFieldChange ("");
    }
  }

  public void DelButtonAction () {
    if (m_Chooser.value != 0)
      m_saverControl.RemovePreset (m_Chooser.options[m_Chooser.value].text);
    repopulateChooser ();

  }

  public void okButtonAction () {
    if (m_NameInput.text.Length > 0) {
      m_saverControl.AddPreset (m_NameInput.text);
      repopulateChooser ();
      m_Chooser.onValueChanged.SetPersistentListenerState (0, UnityEngine.Events.UnityEventCallState.Off);
      var items = m_Chooser.options.Where (a => ( a.text == m_NameInput.text ));
      if (items.Count () == 1)
        m_Chooser.value = m_Chooser.options.IndexOf (items.First ());
      m_Chooser.onValueChanged.SetPersistentListenerState (0, UnityEngine.Events.UnityEventCallState.RuntimeOnly);
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

  public void inputFieldSubmit () {
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
    m_Chooser.options.Clear ();
    m_Chooser.options.Add (new Dropdown.OptionData ("New preset..."));

    foreach (var line in m_saverControl.presetNames) {
      m_Chooser.options.Add (new Dropdown.OptionData (line));
    }
    m_Chooser.onValueChanged.SetPersistentListenerState (0, UnityEngine.Events.UnityEventCallState.Off);
    m_Chooser.captionText.text = "New preset...";
    m_Chooser.value = 0;
    m_Chooser.onValueChanged.SetPersistentListenerState (0, UnityEngine.Events.UnityEventCallState.RuntimeOnly);
    updateControls ();
  }

  public void chooserValueChange (int value) {
    if (value == 0) {
      SaveAsButtonAction ();
    } else {
      m_saverControl.loadPreset (m_Chooser.options[value].text);
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
}
}
