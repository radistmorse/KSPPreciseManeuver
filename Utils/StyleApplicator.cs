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
    Input,
    Button,
    ButtonToggle,
    Scrollbar
  }

  [SerializeField]
  private ElementTypes m_ElementType = ElementTypes.None;

  public ElementTypes ElementType {
    get { return m_ElementType; }
  }

  public void SetImage(Sprite sprite, Image.Type type) {
    Image image = GetComponent<Image>();
    if (image == null)
      return;

    image.sprite = sprite;
    image.type = type;
  }

  public void SetSelectable(Sprite normal, Sprite highlight, Sprite pressed, Sprite disabled) {
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

  public void SetToggle(Sprite normal, Sprite highlight, Sprite pressed, Sprite disabled) {
    SetSelectable(normal, highlight, pressed, disabled);

    Image toggleImage = GetComponent<Toggle>()?.graphic as Image;
    if (toggleImage != null) {
      toggleImage.sprite = pressed;
      toggleImage.type = Image.Type.Sliced;
    }
  }

  public void SetScrollbar (Sprite normal, Sprite highlight, Sprite pressed, Sprite disabled, Sprite background) {
    Scrollbar scrollbar = GetComponent<Scrollbar>();
    if (scrollbar != null) {
      scrollbar.image.sprite = normal;
      scrollbar.image.type = Image.Type.Sliced;
      scrollbar.transition = Selectable.Transition.SpriteSwap;
      SpriteState spriteState = scrollbar.spriteState;
      spriteState.highlightedSprite = highlight;
      spriteState.pressedSprite = pressed;
      spriteState.disabledSprite = disabled;
      scrollbar.spriteState = spriteState;
    }
    SetImage (background, Image.Type.Sliced);
  }
}
}
