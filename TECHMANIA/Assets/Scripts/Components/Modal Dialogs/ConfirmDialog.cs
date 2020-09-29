using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ConfirmDialog : MonoBehaviour
{
    public TextMeshProUGUI message;
    public TextMeshProUGUI confirmButtonText;
    public TextMeshProUGUI cancelButtonText;

    private UnityAction confirmCallback;

    public void Show(string message,
        string confirmButtonText,
        string cancelButtonText,
        UnityAction confirmCallback)
    {
        this.message.text = message;
        this.confirmButtonText.text = confirmButtonText;
        this.cancelButtonText.text = cancelButtonText;
        this.confirmCallback = confirmCallback;
        GetComponent<Dialog>().FadeIn();
    }

    public void OnConfirmButtonClick()
    {
        confirmCallback?.Invoke();
    }
}
