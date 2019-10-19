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
  public class PreciseManeuverPagerItem : PreciseManeuverDropdownItem {
    [SerializeField]
    private RectTransform m_NodeIndex = null;
    public RectTransform NodeIdx { get { return m_NodeIndex; } }
    [SerializeField]
    private RectTransform m_NodeTime = null;
    public RectTransform NodeTime { get { return m_NodeTime; } }
    [SerializeField]
    private RectTransform m_NodeDV = null;
    public RectTransform NodedV { get { return m_NodeDV; } }
    [SerializeField]
    private RectTransform m_Label = null;
    public RectTransform dVLabel { get { return m_Label; } }
  }

  [RequireComponent (typeof (RectTransform))]
  public class PagerControl : MonoBehaviour {
    [SerializeField]
    private Button m_ButtonPrev = null;
    [SerializeField]
    private Button m_ButtonNext = null;
    [SerializeField]
    private PreciseManeuverDropdown m_Chooser = null;
    private UnityAction<string> chooserText = null;

    private IPagerControl m_Control = null;

    public void SetControl (IPagerControl control) {
      m_Control = control;

      m_Chooser.UpdateDropdownCaption = SetChooserText;
      m_Chooser.UpdateDropdownOption = SetChooserOption;
      chooserText = m_Control.ReplaceTextComponentWithTMPro (m_Chooser.CaptionArea.GetComponent<Text> ());
      UpdateGUI ();
      m_Control.RegisterUpdateAction (UpdateGUI);
    }

    public void OnDestroy () {
      m_Chooser.Hide ();
      m_Control.DeregisterUpdateAction (UpdateGUI);
      m_Control = null;
    }

    public void PrevButtonAction () {
      m_Control.PrevButtonPressed ();
    }

    public void FocusButtonAction () {
      m_Control.FocusButtonPressed ();
    }

    public void DelButtonAction () {
      m_Control.DelButtonPressed ();
    }

    public void NextButtonAction () {
      m_Control.NextButtonPressed ();
    }

    public void UpdateGUI () {
      if (m_Control.prevManeuverExists) {
        m_ButtonPrev.interactable = true;
        m_ButtonPrev.GetComponent<Image> ().color = new Color (1.0f, 1.0f, 1.0f, 1.0f);
      } else {
        m_ButtonPrev.interactable = false;
        m_ButtonPrev.GetComponent<Image> ().color = new Color (0.0f, 0.0f, 0.0f, 0.25f);
      }
      if (m_Control.nextManeuverExists) {
        m_ButtonNext.interactable = true;
        m_ButtonNext.GetComponent<Image> ().color = new Color (1.0f, 1.0f, 1.0f, 1.0f);
      } else {
        m_ButtonNext.interactable = false;
        m_ButtonNext.GetComponent<Image> ().color = new Color (0.0f, 0.0f, 0.0f, 0.25f);
      }
      m_Chooser.OptionsCount = m_Control.maneuverCount;
      m_Chooser.SetValueWithoutNotify (m_Control.maneuverIdx);
    }
    private void SetChooserText (int index, GameObject caption) {
      chooserText (m_Control.getLocalizedNode (index + 1));
    }
    private void SetChooserOption (PreciseManeuverDropdownItem item) {
      if (!(item is PreciseManeuverPagerItem))
        return;
      var pageritem = item as PreciseManeuverPagerItem;
      m_Control.ReplaceTextComponentWithTMPro (pageritem.NodeIdx.GetComponent<Text> ())?.
          Invoke (m_Control.getLocalizedNodeln (pageritem.Index + 1));
      m_Control.ReplaceTextComponentWithTMPro (pageritem.NodeTime.GetComponent<Text> ())?.
          Invoke (m_Control.getManeuverTime (pageritem.Index));
      m_Control.ReplaceTextComponentWithTMPro (pageritem.NodedV.GetComponent<Text> ())?.
          Invoke (m_Control.getManeuverDV (pageritem.Index));
      m_Control.ReplaceTextComponentWithTMPro (pageritem.dVLabel.GetComponent<Text> ());
    }

    public void ChooserValueChange (int value) {
      m_Control.SwitchNode (value);
    }
  }
}
