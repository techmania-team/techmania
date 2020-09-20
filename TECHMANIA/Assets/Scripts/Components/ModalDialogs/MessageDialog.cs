using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MessageDialog : MonoBehaviour
{
    public TextMeshProUGUI message;

    public void Show(string message)
    {
        this.message.text = message;
        GetComponent<Dialog>().FadeIn();
    }
}
