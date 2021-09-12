using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class NewTrackDialog : MonoBehaviour
{
    public TMP_InputField titleField;
    public TMP_InputField artistField;

    // Intentionally not responding to the Enter key because
    // it's an effing nightmare.

    private UnityAction<string, string> createCallback;

    public void Show(UnityAction<string, string> createCallback)
    {
        titleField.text = "";
        artistField.text = "";
        this.createCallback = createCallback;
        GetComponent<Dialog>().FadeIn();
    }

    public void OnCreateButtonClick()
    {
        createCallback?.Invoke(titleField.text, artistField.text);
    }
}
