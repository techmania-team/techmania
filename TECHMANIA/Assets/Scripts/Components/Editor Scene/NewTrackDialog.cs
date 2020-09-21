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

    public static event UnityAction<string, string> CreateButtonClicked;

    // Intentionally not responding to the Enter or Tab keys because
    // it's an effing nightmare.

    public void OnCreateButtonClick()
    {
        CreateButtonClicked?.Invoke(titleField.text, artistField.text);
    }
}
