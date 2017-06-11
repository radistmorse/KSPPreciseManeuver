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

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace KSPPreciseManeuver.UI {
[RequireComponent (typeof (CanvasGroup))]
public class CanvasGroupFader : MonoBehaviour {

  internal bool collapseOnFade = false;

  private CanvasGroup canvasGroup = null;
  private CanvasGroup CanvasGroup {
    get {
      if (canvasGroup == null)
        canvasGroup = GetComponent<CanvasGroup> ();
      return canvasGroup;
    }
  }
  private RectTransform rectTransform = null;
  private RectTransform RectTransform {
    get {
      if (rectTransform == null)
        rectTransform = GetComponent<RectTransform> ();
      return rectTransform;
    }
  }
  private IEnumerator fadeCoroutine;

  private float fastFadeDuration = 0.2f;
  private float slowFadeDuration = 1.0f;

  private bool isFadingIn;

  public bool IsFadingIn {
    get { return fadeCoroutine != null && isFadingIn; }
  }

  public bool IsFadingOut {
    get { return fadeCoroutine != null && !isFadingIn; }
  }

  public void SetTransparent () {
    SetState (0.0f);
    gameObject.SetActive (false);
  }

  public void FadeIn () {
    isFadingIn = true;
    gameObject.SetActive (true);
    FadeTo (1.0f, fastFadeDuration);
  }

  public void FadeClose () {
    isFadingIn = false;
    FadeTo (0.0f, fastFadeDuration, Destroy);
  }

  public void FadeOut () {
    isFadingIn = false;
    FadeTo (0.0f, fastFadeDuration, SetInactive);
  }

  private void SetInactive () {
    gameObject.SetActive (false);
  }

  public void FadeCloseSlow () {
    isFadingIn = false;
    FadeTo (0.0f, slowFadeDuration, Destroy);
  }

  private void FadeTo (float alpha, float duration, Action callback = null) {
    if (CanvasGroup == null)
      return;

    Fade (CanvasGroup.alpha, alpha, duration, callback);
  }

  private void SetState (float state) {
    state = Mathf.Clamp01 (state);
    CanvasGroup.alpha = state;
    if (collapseOnFade) {
      var scale = Vector3.one;
      scale.y = state;
      RectTransform.localScale = scale;
      if (RectTransform.parent.parent is RectTransform)
        LayoutRebuilder.MarkLayoutForRebuild (RectTransform.parent.parent as RectTransform);
    }
  }

  private void Fade (float from, float to, float duration, Action callback) {
    if (fadeCoroutine != null)
      StopCoroutine (fadeCoroutine);

    if (from == to)
      return;

    if (Math.Abs (from - to) < 0.1) {
      SetState (to);
    } else {
      fadeCoroutine = FadeCoroutine (from, to, duration, callback);
      StartCoroutine (fadeCoroutine);
    }
  }

  private IEnumerator FadeCoroutine (float from, float to, float duration, Action callback) {
    // wait for end of frame so that only the last call to fade that frame is honoured.
    yield return new WaitForEndOfFrame ();

    float progress = 0.0f;

    while (progress <= 1.0f) {
      progress += Time.deltaTime / duration;
      SetState (Mathf.Lerp (from, to, progress));
      yield return null;
    }

    callback?.Invoke ();

    fadeCoroutine = null;
  }

  protected virtual void Destroy () {
    // disable game object first due to an issue within unity 5.2.4f1 that shows a single frame at full opaque alpha just before destruction
    gameObject.SetActive (false);
    Destroy (gameObject);
  }
}
}