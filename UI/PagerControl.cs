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
  public RectTransform nodeidx { get { return m_NodeIndex; } }
  [SerializeField]
  private RectTransform m_NodeTime = null;
  public RectTransform nodetime { get { return m_NodeTime; } }
  [SerializeField]
  private RectTransform m_NodeDV = null;
  public RectTransform nodedv { get { return m_NodeDV; } }
  [SerializeField]
  private RectTransform m_Label = null;
  public RectTransform dvlabel { get { return m_Label; } }
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

  private IPagerControl m_pagerControl = null;

  public void SetPagerControl(IPagerControl pagerControl) {
    m_pagerControl = pagerControl;

    m_Chooser.updateDropdownCaption = setChooserText;
    m_Chooser.updateDropdownOption = setChooserOption;
    m_Chooser.setRootCanvas (pagerControl.Canvas);
    chooserText = pagerControl.replaceTextComponentWithTMPro (m_Chooser.captionArea.GetComponent<Text> ());
    updatePagerValues ();
    m_pagerControl.registerUpdateAction (updatePagerValues);
  }

  public void OnDestroy () {
    m_Chooser.Hide ();
    m_pagerControl.deregisterUpdateAction (updatePagerValues);
    m_pagerControl = null;
  }

  public void PrevButtonAction () {
    if (m_pagerControl != null)
      m_pagerControl.PrevButtonPressed ();
  }

  public void FocusButtonAction () {
    if (m_pagerControl != null)
      m_pagerControl.FocusButtonPressed ();
  }

  public void DelButtonAction () {
    if (m_pagerControl != null)
      m_pagerControl.DelButtonPressed ();
  }

  public void NextButtonAction () {
    if (m_pagerControl != null)
      m_pagerControl.NextButtonPressed ();
  }

  public void updatePagerValues () {
    if (m_pagerControl.prevManeuverExists) {
      m_ButtonPrev.interactable = true;
      m_ButtonPrev.GetComponent<Image> ().color = new Color (1.0f, 1.0f, 1.0f, 1.0f);
    } else {
      m_ButtonPrev.interactable = false;
      m_ButtonPrev.GetComponent<Image> ().color = new Color (0.0f, 0.0f, 0.0f, 0.25f);
    }
    if (m_pagerControl.nextManeuverExists) {
      m_ButtonNext.interactable = true;
      m_ButtonNext.GetComponent<Image> ().color = new Color (1.0f, 1.0f, 1.0f, 1.0f);
    } else {
      m_ButtonNext.interactable = false;
      m_ButtonNext.GetComponent<Image> ().color = new Color (0.0f, 0.0f, 0.0f, 0.25f);
    }
    m_Chooser.optionCount = m_pagerControl.maneuverCount;
    m_Chooser.setValueNoInvoke (m_pagerControl.maneuverIdx);
  }
  private void setChooserText (int index, GameObject caption) {
    chooserText (m_pagerControl.getManeuverNodeLocalized () + " " + (index + 1).ToString ());
  }
  private void setChooserOption (PreciseManeuverDropdownItem item) {
    if (!(item is PreciseManeuverPagerItem))
        return;
    var pageritem = item as PreciseManeuverPagerItem;
    m_pagerControl.replaceTextComponentWithTMPro (pageritem.nodeidx.GetComponent<Text> ())?.
        Invoke (m_pagerControl.getManeuverNodeLocalized () + "\n" + (pageritem.index + 1).ToString ());
    m_pagerControl.replaceTextComponentWithTMPro (pageritem.nodetime.GetComponent<Text> ())?.
        Invoke (m_pagerControl.getManeuverTime (pageritem.index));
    m_pagerControl.replaceTextComponentWithTMPro (pageritem.nodedv.GetComponent<Text> ())?.
        Invoke (m_pagerControl.getManeuverDV (pageritem.index));
    m_pagerControl.replaceTextComponentWithTMPro (pageritem.dvlabel.GetComponent<Text> ());
  }

  public void chooserValueChange(int value) {
    m_pagerControl.SwitchNode (value);
  }
}
}
