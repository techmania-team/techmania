using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Panel transition code is intentionally outside of panels, so that:
// * we can have a singleton;
// * coroutines don't stop when a panel is diabled.
//
// In contrast, dialog transition is done by itself, because it only
// needs to manipulate itself.
public class PanelTransitioner : MonoBehaviour
{
    private static PanelTransitioner instance;
    public static bool transitioning { get; private set; }

    static PanelTransitioner()
    {
        instance = null;
        transitioning = false;
    }

    void OnEnable()
    {
        instance = this;
    }

    public static void TransitionTo(Panel to,
        TransitionToPanel.Direction direction,
        Action callbackOnFinish = null)
    {
        if (transitioning) return;
        instance.StartCoroutine(instance.InternalTransitionTo(
            Panel.current, to, direction, callbackOnFinish));
    }

    // Approach gets slower as t approaches 1.
    public static float Damp(float from, float to, float t)
    {
        return Mathf.Lerp(from, to, Mathf.Pow(t, 0.6f));
    }

    // from and to may be null.
    private IEnumerator InternalTransitionTo(Panel from, Panel to,
        TransitionToPanel.Direction direction,
        Action callbackOnFinish)
    {
        // TODO: when transitioning from track setup to theme,
        // "from" is null. Why?
        // TODO: when transitioning from track setup to pattern panel,
        // track setup panel does not disappear. Why?
        transitioning = true;

        if (to != null)
        {
            BackButton backButton = to
                .GetComponentInChildren<BackButton>();
            if (backButton != null && backButton.recordTransitionSource)
            {
                backButton.GetComponent<TransitionToPanel>()
                    .target = from;
            }
        }

        const float kLength = 0.2f;
        const float kMovement = 100f;

        float toRectSource;
        if (direction == TransitionToPanel.Direction.Left)
        {
            toRectSource = -kMovement;
        }
        else
        {
            toRectSource = kMovement;
        }
        float fromRectDestination = -toRectSource;

        if (from != null)
        {
            CanvasGroup fromGroup = from.GetComponent<CanvasGroup>();
            RectTransform fromRect = from.GetComponent<RectTransform>();

            for (float time = 0f; time < kLength; time += Time.deltaTime)
            {
                float progress = time / kLength;
                fromGroup.alpha = 1f - progress;
                fromRect.anchoredPosition = new Vector2(
                    Damp(0f, fromRectDestination, progress),
                    0f);
                yield return null;
            }
            fromGroup.alpha = 0f;
            from.gameObject.SetActive(false);
        }

        if (to != null)
        {
            CanvasGroup toGroup = to.GetComponent<CanvasGroup>();
            RectTransform toRect = to.GetComponent<RectTransform>();

            to.gameObject.SetActive(true);
            toGroup.alpha = 0f;
            for (float time = 0f; time < kLength; time += Time.deltaTime)
            {
                float progress = time / kLength;
                toGroup.alpha = progress;
                toRect.anchoredPosition = new Vector2(
                    Damp(toRectSource, 0f, progress),
                    0f);
                yield return null;
            }
            toGroup.alpha = 1f;
            toRect.anchoredPosition = Vector2.zero;
        }

        transitioning = false;

        callbackOnFinish?.Invoke();
    }
}
