using System.Collections;
using System.Collections.Generic;
using TMPro.EditorUtilities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class Panel : MonoBehaviour
{
    public static Panel current;
    public Selectable defaultSelectable;
    public TransitionToPanel backButton;

    // Start is called before the first frame update
    void Start()
    {
        if (current == null) current = this;
    }

    private void OnEnable()
    {
        if (defaultSelectable != null &&
            EventSystem.current.currentSelectedGameObject == null)
        {
            EventSystem.current.SetSelectedGameObject(defaultSelectable.gameObject);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && backButton != null)
        {
            backButton.Invoke();
        }
    }
}
