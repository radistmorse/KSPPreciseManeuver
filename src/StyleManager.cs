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
public static class StyleManager {
  private static GameObject s_WindowPrefab;

  /// <summary>
  ///     Creates and returns a new window object.
  /// </summary>
  public static Window CreateWindow(string title, float width) {
    GameObject windowPrefab = GetWindowPrefab();
    if (windowPrefab == null)
      return null;

    GameObject windowObject = Object.Instantiate(windowPrefab);
    if (windowObject == null)
      return null;

    // process style applicators
    Process(windowObject);

    // assign game object to be a child of the main canvas
    windowObject.transform.SetParent(MainCanvasUtil.MainCanvas.transform, false);

    // set window values
    Window window = windowObject.GetComponent<Window>();
    if (window != null) {
      window.SetTitle(title);
      window.SetWidth(width);
    }

    return window;
  }

  /// <summary>
  ///     Processes all of the style applicators on the supplied game object.
  /// </summary>
  public static void Process(GameObject gameObject) {
    if (gameObject == null)
      return;

    StyleApplicator[] applicators = gameObject.GetComponentsInChildren<StyleApplicator>();

    if (applicators != null)
      for (int i = 0; i < applicators.Length; i++)
        Process(applicators[i]);
  }

  /// <summary>
  ///     Processes all the style applicators on the supplied component's game object.
  /// </summary>
  public static void Process(Component component) {
    if (component != null)
      Process(component.gameObject);
  }

  /// <summary>
  ///     Gets a new ThemeTextStyle created from KSP UIStyle and UIStyleState objects.
  /// </summary>
  private static TextStyle GetTextStyle(UIStyle style, UIStyleState styleState) {
    TextStyle textStyle = new TextStyle();

    if (style != null) {
      textStyle.Font = style.font;
      textStyle.Style = style.fontStyle;
      textStyle.Size = style.fontSize;
    }

    if (styleState != null)
      textStyle.Colour = styleState.textColor;

    return textStyle;
  }

  /// <summary>
  ///     Gets a window prefab object.
  /// </summary>
  private static GameObject GetWindowPrefab() {
    if (s_WindowPrefab == null && PreciseManeuverConfig.getInstance().prefabs != null)
      s_WindowPrefab = PreciseManeuverConfig.getInstance().prefabs.LoadAsset<GameObject>("Window");

    return s_WindowPrefab;
  }

  /// <summary>
  ///     Processes a theme on the supplied applicator.
  /// </summary>
  private static void Process(StyleApplicator applicator) {
    if (applicator == null)
      return;

    // get the default skin
    UISkinDef skin = UISkinManager.defaultSkin;
    if (skin == null)
      return;

    // apply selected theme type
    switch (applicator.ElementType) {
      case StyleApplicator.ElementTypes.Window:
        applicator.SetImage(skin.window.normal.background, Image.Type.Sliced);
        break;

      case StyleApplicator.ElementTypes.Box:
        applicator.SetImage(skin.box.normal.background, Image.Type.Sliced);
        break;

      case StyleApplicator.ElementTypes.Button:
        applicator.SetSelectable(null, skin.button.normal.background,
            skin.button.highlight.background,
            skin.button.active.background,
            skin.button.disabled.background);
        break;

      case StyleApplicator.ElementTypes.ButtonToggle:
        applicator.SetToggle(null, skin.button.normal.background,
            skin.button.highlight.background,
            skin.button.active.background,
            skin.button.disabled.background);
        break;

      case StyleApplicator.ElementTypes.Label:
        applicator.SetText(GetTextStyle(skin.label, skin.label.normal));
        break;
    }
  }
}
}
