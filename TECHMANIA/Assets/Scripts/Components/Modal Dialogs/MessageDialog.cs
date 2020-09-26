using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MessageDialog : MonoBehaviour
{
    public TextMeshProUGUI message;

    private UnityAction closeCallback;

    public void Show(string message)
    {
        Show(message, null);
    }

    public void Show(string message, UnityAction closeCallback)
    {
        this.message.text = message;
        this.closeCallback = closeCallback;
        GetComponent<Dialog>().FadeIn();
    }

    private void OnDisable()
    {
        closeCallback?.Invoke();
    }
}
