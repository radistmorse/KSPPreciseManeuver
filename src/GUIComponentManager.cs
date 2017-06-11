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
using KSP.Localization;

namespace KSPPreciseManeuver {
  internal static class GUIComponentManager {
    static  UISkinDef skin = UISkinManager.defaultSkin;

    internal static void ProcessLocalization (GameObject gameObject) {
      if (gameObject == null)
        return;

      foreach (var localizator in gameObject.GetComponentsInChildren<UI.LocalizationComponent> (true)) {
        localizator.SetLocalizedString (Localizer.Format (localizator.GetTemplate ()));
        Object.DestroyImmediate (localizator);
      }
    }

    static private KSP.UI.TooltipTypes.Tooltip_Text prefab = null;
    static private KSP.UI.TooltipTypes.Tooltip_Text Prefab {
      get {
        if (prefab == null) {
          prefab = AssetBase.GetPrefab<KSP.UI.TooltipTypes.Tooltip_Text> ("Tooltip_Text");
        }
        return prefab;
      }
    }

    internal static void ProcessTooltips (GameObject gameObject) {
      if (gameObject == null)
        return;

      foreach (var tooltip in gameObject.GetComponentsInChildren<UI.TooltipComponent> (true)) {
        var controller = tooltip.gameObject.AddOrGetComponent<KSP.UI.TooltipTypes.TooltipController_Text> ();
        controller.SetText (tooltip.Text);
        controller.prefab = Prefab;
      }
    }

    internal static void ReplaceLabelsWithTMPro (GameObject gameObject) {
      if (gameObject == null)
        return;

      foreach (var text in gameObject.GetComponentsInChildren<Text> (true))
        if (text.gameObject.name == "Label")
          ReplaceTextWithTMPro (text);
    }

    internal static TMPro.TextMeshProUGUI ReplaceTextWithTMPro (Text text) {
      if (text == null)
        return null;
      var str = text.text;
      var alignment = text.alignment;
      var fontSize = text.fontSize;
      var fontStyle = text.fontStyle;
      var horizontalOverflow = text.horizontalOverflow;
      var lineSpacing = text.lineSpacing;
      var resizeTextForBestFit = text.resizeTextForBestFit;
      var resizeTextMaxSize = text.resizeTextMaxSize;
      var resizeTextMinSize = text.resizeTextMinSize;
      var supportRichText = text.supportRichText;
      var verticalOverflow = text.verticalOverflow;
      var color = text.color;

      var go = text.gameObject;
      var rt = go.GetComponent<RectTransform> ();
      var delta = rt.sizeDelta;
      Object.DestroyImmediate (text);
      var t = go.AddComponent<TMPro.TextMeshProUGUI> ();
      rt.sizeDelta = delta;

      t.text = str;
      t.font = UISkinManager.TMPFont;
      t.fontSize = fontSize;
      t.lineSpacing = lineSpacing;
      t.richText = supportRichText;
      t.enableAutoSizing = resizeTextForBestFit;
      t.fontSizeMin = resizeTextMinSize;
      t.fontSizeMax = resizeTextMaxSize;
      t.color = color;
      if (horizontalOverflow == HorizontalWrapMode.Wrap) {
        t.enableWordWrapping = true;
      }
      switch (fontStyle) {
        case FontStyle.Normal: {
            t.fontStyle = TMPro.FontStyles.Normal;
            break;
          }
        case FontStyle.Bold: {
            t.fontStyle = TMPro.FontStyles.Bold;
            break;
          }
        case FontStyle.Italic: {
            t.fontStyle = TMPro.FontStyles.Italic;
            break;
          }
        case FontStyle.BoldAndItalic: {
            t.fontStyle = TMPro.FontStyles.Bold | TMPro.FontStyles.Italic;
            break;
          }
      }

      switch (alignment) {
        case TextAnchor.UpperLeft: {
            t.alignment = TMPro.TextAlignmentOptions.TopLeft;
            break;
          }
        case TextAnchor.UpperCenter: {
            t.alignment = TMPro.TextAlignmentOptions.Top;
            break;
          }
        case TextAnchor.UpperRight: {
            t.alignment = TMPro.TextAlignmentOptions.TopRight;
            break;
          }
        case TextAnchor.MiddleLeft: {
            t.alignment = TMPro.TextAlignmentOptions.Left;
            break;
          }
        case TextAnchor.MiddleCenter: {
            t.alignment = TMPro.TextAlignmentOptions.Center;
            break;
          }
        case TextAnchor.MiddleRight: {
            t.alignment = TMPro.TextAlignmentOptions.Right;
            break;
          }
        case TextAnchor.LowerLeft: {
            t.alignment = TMPro.TextAlignmentOptions.BottomLeft;
            break;
          }
        case TextAnchor.LowerCenter: {
            t.alignment = TMPro.TextAlignmentOptions.Bottom;
            break;
          }
        case TextAnchor.LowerRight: {
            t.alignment = TMPro.TextAlignmentOptions.BottomRight;
            break;
          }
      }
      return t;
    }

    internal static void ProcessStyle (GameObject gameObject) {
      if (gameObject == null)
        return;

      foreach (var applicator in gameObject.GetComponentsInChildren<UI.StyleApplicator> (true))
        ProcessStyle (applicator);
    }

    private static void ProcessStyle (UI.StyleApplicator applicator) {
      switch (applicator.ElementType) {
        case UI.StyleApplicator.ElementTypes.Window:
          applicator.SetImage (skin.window.normal.background, Image.Type.Sliced);
          break;
        case UI.StyleApplicator.ElementTypes.Input:
          applicator.SetSelectable (skin.textField.normal.background,
                                    skin.textField.highlight.background,
                                    skin.textField.active.background,
                                    skin.textField.normal.background);
          break;
        case UI.StyleApplicator.ElementTypes.Button:
          applicator.SetSelectable (skin.button.normal.background,
                                    skin.button.highlight.background,
                                    skin.button.active.background,
                                    skin.button.disabled.background);
          break;
        case UI.StyleApplicator.ElementTypes.ButtonToggle:
          applicator.SetToggle (skin.button.normal.background,
                                skin.button.highlight.background,
                                skin.button.active.background,
                                skin.button.disabled.background);
          break;
        case UI.StyleApplicator.ElementTypes.Scrollbar:
          applicator.SetScrollbar (skin.verticalScrollbarThumb.normal.background,
                                    skin.verticalScrollbarThumb.highlight.background,
                                    skin.verticalScrollbarThumb.active.background,
                                    skin.verticalScrollbarThumb.disabled.background,
                                    skin.verticalScrollbar.normal.background);
          break;
        case UI.StyleApplicator.ElementTypes.Slider:
          applicator.SetSlider (skin.horizontalSliderThumb.normal.background,
                                skin.horizontalSliderThumb.highlight.background,
                                skin.horizontalSliderThumb.active.background,
                                skin.horizontalSliderThumb.disabled.background,
                                skin.horizontalSlider.normal.background);
          break;
      }
    }
  }
}
