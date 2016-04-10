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

namespace KSPPreciseManeuver {
using UI;
internal static class StyleManager {
  static  UISkinDef skin = UISkinManager.defaultSkin;

  internal static void Process(GameObject gameObject) {
    if (gameObject == null)
      return;

    foreach (var applicator in gameObject.GetComponentsInChildren<StyleApplicator> (true))
        Process (applicator);
  }

  private static void Process(StyleApplicator applicator) {
    switch (applicator.ElementType) {
      case StyleApplicator.ElementTypes.Window:
        applicator.SetImage(skin.window.normal.background, Image.Type.Sliced);
      break;
      case StyleApplicator.ElementTypes.Input:
        applicator.SetSelectable (skin.textField.normal.background,
                                  skin.textField.highlight.background,
                                  skin.textField.active.background,
                                  skin.textField.normal.background);
      break;
      case StyleApplicator.ElementTypes.Button:
        applicator.SetSelectable (skin.button.normal.background,
                                  skin.button.highlight.background,
                                  skin.button.active.background,
                                  skin.button.disabled.background);
      break;
      case StyleApplicator.ElementTypes.ButtonToggle:
        applicator.SetToggle (skin.button.normal.background,
                              skin.button.highlight.background,
                              skin.button.active.background,
                              skin.button.disabled.background);
      break;
      case StyleApplicator.ElementTypes.Scrollbar:
        applicator.SetScrollbar (skin.verticalScrollbarThumb.normal.background,
                                 skin.verticalScrollbarThumb.highlight.background,
                                 skin.verticalScrollbarThumb.active.background,
                                 skin.verticalScrollbarThumb.disabled.background,
                                 skin.verticalScrollbar.normal.background);
      break;
    }
  }
}
}
