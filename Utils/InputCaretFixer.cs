using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class InputCaretFixer : MonoBehaviour, ISelectHandler {
  private bool alreadyFixed;

  public void OnSelect (BaseEventData eventData) {
    StartCoroutine (FixCaret ());
  }

  IEnumerator FixCaret () {
    if (alreadyFixed) {
      yield break;
    }

    string caretName = gameObject.name + " Input Caret";
    RectTransform caretTransorm = null;
    do {
      caretTransorm = (RectTransform)transform.Find (caretName);
      if (!caretTransorm)
        yield return null;
    } while (!caretTransorm);

    caretTransorm.anchorMin = new Vector2 (0.0f, -0.3f);

    alreadyFixed = true;
  }
}