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
using UnityEngine.EventSystems;

namespace KSPPreciseManeuver.UI {
  [RequireComponent (typeof (Button))]
  class RepeatButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {

    [SerializeField]
    private UnityEvent OnRepeatedClick = null;
    [SerializeField]
    private UnityEvent OnRepeatClickStart = null;
    [SerializeField]
    private UnityEvent OnRepeatClickEnd = null;

    private bool pressed = false;
    private int buttonPressedInterval = 0;

    internal void FixedUpdate () {
      if (pressed == true) {
        if (buttonPressedInterval > 20 || buttonPressedInterval == 0) {
          OnRepeatedClick.Invoke ();
        }
        buttonPressedInterval++;
      } else {
        buttonPressedInterval = 0;
      }
    }

    public void OnPointerDown (PointerEventData eventData) {
      pressed = true;
      OnRepeatClickStart?.Invoke ();
    }

    public void OnPointerUp (PointerEventData eventData) {
      pressed = false;
      OnRepeatClickEnd?.Invoke ();
    }
  }
}
