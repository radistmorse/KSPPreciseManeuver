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

namespace KSPPreciseManeuver.UI {
[RequireComponent(typeof(CanvasGroup))]
public class CanvasGroupFader : MonoBehaviour {
  private CanvasGroup m_CanvasGroup;
  private IEnumerator m_FadeCoroutine;

  public bool IsFading {
    get { return m_FadeCoroutine != null; }
  }

  /// <summary>
  ///     Fades the canvas group to a specified alpha using the supplied blocking state during fade with optional callback.
  /// </summary>
  public void FadeTo(float alpha, float duration, Action callback = null) {
    if (m_CanvasGroup == null)
      return;

    Fade(m_CanvasGroup.alpha, alpha, duration, callback);
  }

  /// <summary>
  ///     Sets the alpha value of the canvas group.
  /// </summary>
  public void SetAlpha(float alpha) {
    if (m_CanvasGroup == null)
      return;

    alpha = Mathf.Clamp01(alpha);
    m_CanvasGroup.alpha = alpha;
  }

  protected virtual void Awake() {
    // cache components
    m_CanvasGroup = GetComponent<CanvasGroup>();
  }

  /// <summary>
  ///     Starts a fade from one alpha value to another with callback.
  /// </summary>
  private void Fade(float from, float to, float duration, Action callback) {
    if (m_FadeCoroutine != null)
      StopCoroutine(m_FadeCoroutine);

    m_FadeCoroutine = FadeCoroutine(from, to, duration, callback);
    StartCoroutine(m_FadeCoroutine);
  }

  /// <summary>
  ///     Coroutine that handles the fading.
  /// </summary>
  private IEnumerator FadeCoroutine(float from, float to, float duration, Action callback) {
    // wait for end of frame so that only the last call to fade that frame is honoured.
    yield return new WaitForEndOfFrame();

    float progress = 0.0f;

    while (progress <= 1.0f) {
      progress += Time.deltaTime / duration;
      SetAlpha(Mathf.Lerp(from, to, progress));
      yield return null;
    }

    callback?.Invoke();

    m_FadeCoroutine = null;
  }
}
}
