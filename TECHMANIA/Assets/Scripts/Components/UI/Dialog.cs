using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class Dialog : MonoBehaviour
{
    public GameObject defaultSelected;

    private CanvasGroup previousGroup;
    private GameObject previousSelected;
    private CanvasGroup currentGroup;
    private bool transitioning;

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
        previousGroup = Panel.current.GetComponent<CanvasGroup>();
        previousGroup.interactable = false;
        previousSelected = EventSystem.current.currentSelectedGameObject;

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

        const float kLength = 0.2f;
        const float kMovement = 100f;

        for (float time = 0f; time < kLength; time += Time.deltaTime)
        {
            float progress = time / kLength;
            currentGroup.alpha = progress;
            rect.anchoredPosition = new Vector2(
                0f,
                PanelTransitioner.Damp(-kMovement, 0f, progress));
            yield return null;
        }

        currentGroup.alpha = 1f;
        rect.anchoredPosition = Vector2.zero;
        transitioning = false;

        if (defaultSelected != null)
        {
            EventSystem.current.SetSelectedGameObject(defaultSelected);
        }
    }

    private IEnumerator InternalFadeOut()
    {
        transitioning = true;
        RectTransform rect = GetComponent<RectTransform>();

        const float kLength = 0.2f;
        const float kMovement = 100f;

        for (float time = 0f; time < kLength; time += Time.deltaTime)
        {
            float progress = time / kLength;
            currentGroup.alpha = 1f - progress;
            rect.anchoredPosition = new Vector2(
                0f,
                PanelTransitioner.Damp(0f, -kMovement, progress));
            yield return null;
        }

        currentGroup.alpha = 0f;
        previousGroup.interactable = true;
        EventSystem.current.SetSelectedGameObject(previousSelected);
        transitioning = false;
        gameObject.SetActive(false);
    }
}
