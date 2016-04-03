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
public class StyleApplicator : MonoBehaviour {
  public enum ElementTypes {
    None,
    Window,
    Box,
    Button,
    ButtonToggle,
    Label
  }

  [SerializeField]
  private ElementTypes m_ElementType = ElementTypes.None;

  /// <summary>
  ///     Gets the UI element type used by the ThemeManager for selecting how to apply the theme.
  /// </summary>
  public ElementTypes ElementType {
    get { return m_ElementType; }
  }

  /// <summary>
  ///     Sets a the applicator to apply the selected sprite to an attached image component.
  /// </summary>
  public void SetImage(Sprite sprite, Image.Type type) {
    Image image = GetComponent<Image>();
    if (image == null)
      return;

    image.sprite = sprite;
    image.type = type;
  }

  /// <summary>
  ///     Sets the applicator to apply the specified values to an attached selectable component.
  /// </summary>
  public void SetSelectable(TextStyle textStyle, Sprite normal, Sprite highlight, Sprite pressed, Sprite disabled) {
    SetText(textStyle, GetComponentInChildren<Text>());

    Selectable selectable = GetComponent<Selectable>();
    if (selectable != null) {
      selectable.image.sprite = normal;
      selectable.image.type = Image.Type.Sliced;

      selectable.transition = Selectable.Transition.SpriteSwap;

      SpriteState spriteState = selectable.spriteState;
      spriteState.highlightedSprite = highlight;
      spriteState.pressedSprite = pressed;
      spriteState.disabledSprite = disabled;
      selectable.spriteState = spriteState;
    }
  }

  /// <summary>
  ///     Sets the applicator to apply a style to an attached text component.
  /// </summary>
  public void SetText(TextStyle textStyle) {
    SetText(textStyle, GetComponent<Text>());
  }

  /// <summary>
  ///     Sets the applicator to apply the specified values to an attached toggle component.
  /// </summary>
  public void SetToggle(TextStyle textStyle, Sprite normal, Sprite highlight, Sprite pressed, Sprite disabled) {
    SetSelectable(textStyle, normal, highlight, pressed, disabled);

    Image toggleImage = GetComponent<Toggle>()?.graphic as Image;
    if (toggleImage != null) {
      toggleImage.sprite = pressed;
      toggleImage.type = Image.Type.Sliced;
    }
  }

  /// <summary>
  ///     Sets the applicator to apply a style to the supplied text component.
  /// </summary>
  private static void SetText(TextStyle textStyle, Text textComponent) {
    if (textStyle == null || textComponent == null)
      return;

    if (textStyle.Font != null)
      textComponent.font = textStyle.Font;
    textComponent.fontSize = textStyle.Size;
    textComponent.fontStyle = textStyle.Style;
    textComponent.color = textStyle.Colour;
  }
}
}
