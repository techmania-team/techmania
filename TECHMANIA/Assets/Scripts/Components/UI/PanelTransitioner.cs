using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelTransitioner : MonoBehaviour
{
    private static PanelTransitioner instance;
    private static bool transitioning;

    static PanelTransitioner()
    {
        instance = null;
        transitioning = false;
    }

    // Start is called before the first frame update
    void Start()
    {
        instance = this;
    }

    public static void TransitionTo(Panel to,
        TransitionToPanel.Direction direction)
    {
        if (transitioning) return;
        instance.StartCoroutine(instance.InternalTransitionTo(
            Panel.current, to, direction));
    }

    private IEnumerator InternalTransitionTo(Panel from, Panel to,
        TransitionToPanel.Direction direction)
    {
        transitioning = true;
        CanvasGroup fromGroup = from.GetComponent<CanvasGroup>();
        RectTransform fromRect = from.GetComponent<RectTransform>();
        CanvasGroup toGroup = to.GetComponent<CanvasGroup>();
        RectTransform toRect = to.GetComponent<RectTransform>();

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

        for (float time = 0f; time < kLength; time += Time.deltaTime)
        {
            float progress = time / kLength;
            fromGroup.alpha = 1f - progress;
            fromRect.anchoredPosition = new Vector2(
                Mathf.SmoothStep(0f, fromRectDestination, progress),
                0f);
            yield return null;
        }
        fromGroup.alpha = 0f;
        from.gameObject.SetActive(false);

        to.gameObject.SetActive(true);
        toGroup.alpha = 0f;
        for (float time = 0f; time < kLength; time += Time.deltaTime)
        {
            float progress = time / kLength;
            toGroup.alpha = progress;
            toRect.anchoredPosition = new Vector2(
                Mathf.SmoothStep(toRectSource, 0f, progress),
                0f);
            yield return null;
        }
        toGroup.alpha = 1f;
        toRect.anchoredPosition = Vector2.zero;

        transitioning = false;
        Panel.current = to;
    }
}
