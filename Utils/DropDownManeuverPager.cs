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

namespace KSPPreciseManeuver.UI {
[RequireComponent (typeof (RectTransform))]
public class DropDownManeuverPager : Dropdown {
  private PagerControl m_control = null;

  override protected void Awake() {
    base.Awake ();
    m_control = GetComponentInParent<PagerControl> ();
  }

  private int num = 0;
  override protected GameObject CreateDropdownList (GameObject template) {
    num = 0;
    return base.CreateDropdownList (template);
  }

  override protected DropdownItem CreateItem (DropdownItem itemTemplate) {
    Text nodetime = null;
    Text nodedv = null;
    foreach (var item in itemTemplate.GetComponentsInChildren<Text> ()) {
      if (item.name == "NodeTime")
        nodetime = item;
      if (item.name == "NodeDV")
        nodedv = item;
    }

    if (m_control != null && nodetime != null && nodedv != null) {
      nodetime.text = m_control.GetTimeForNode (num);
      nodedv.text = m_control.GetDVForNode (num);
    }
    num++;
    return Instantiate (itemTemplate);
  }
}
}
