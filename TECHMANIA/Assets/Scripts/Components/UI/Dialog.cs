using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class Dialog : MonoBehaviour
{
    private CanvasGroup previousGroup;
    private CanvasGroup currentGroup;
    private bool transitioning;
    protected Vector2 restingAnchoredPosition;
    protected const float kFadeDistance = 100f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Cancel"))
        {
            FadeOut();
        }
    }

    public void FadeIn()
    {
        previousGroup = Panel.current?.GetComponent<CanvasGroup>();
        if (previousGroup != null)
        {
            previousGroup.interactable = false;
        }

        currentGroup = GetComponent<CanvasGroup>();
        currentGroup.alpha = 0f;

        gameObject.SetActive(true);

        StartCoroutine(InternalFadeIn());
    }

    public void FadeOut()
    {
        if (transitioning) return;
        MenuSfx.instance.PlayBackSound();
        StartCoroutine(InternalFadeOut());
    }

    private IEnumerator InternalFadeIn()
    {
        transitioning = true;
        RectTransform rect = GetComponent<RectTransform>();
        restingAnchoredPosition = rect.anchoredPosition;

        const float kLength = 0.2f;

        for (float time = 0f; time < kLength; time += Time.deltaTime)
        {
            float progress = time / kLength;
            currentGroup.alpha = progress;
            rect.anchoredPosition = FadeInStep(progress);
            yield return null;
        }

        currentGroup.alpha = 1f;
        rect.anchoredPosition = restingAnchoredPosition;
        transitioning = false;
    }

    protected virtual Vector2 FadeInStep(float progress)
    {
        return new Vector2(
            0f,
            PanelTransitioner.Damp(-kFadeDistance, 0f, progress));
    }

    private IEnumerator InternalFadeOut()
    {
        transitioning = true;
        RectTransform rect = GetComponent<RectTransform>();

        const float kLength = 0.2f;

        for (float time = 0f; time < kLength; time += Time.deltaTime)
        {
            float progress = time / kLength;
            currentGroup.alpha = 1f - progress;
            rect.anchoredPosition = FadeOutStep(progress);
            yield return null;
        }

        currentGroup.alpha = 0f;
        if (previousGroup != null)
        {
            previousGroup.interactable = true;
        }
        transitioning = false;
        rect.anchoredPosition = restingAnchoredPosition;
        gameObject.SetActive(false);
    }

    protected virtual Vector2 FadeOutStep(float progress)
    {
        return new Vector2(
            0f,
            PanelTransitioner.Damp(0f, -kFadeDistance, progress));
    }
}
