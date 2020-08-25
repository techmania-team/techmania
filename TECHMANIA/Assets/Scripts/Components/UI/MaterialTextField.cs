using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MaterialTextField : MonoBehaviour
{
    public Color miniLabelColor;
    public Color labelColor;
    public Color inputTextColor;
    public Color disabledColor;

    public GameObject miniLabelObject;
    public TextMeshProUGUI miniLabel;
    public TextMeshProUGUI label;
    public TextMeshProUGUI inputText;

    private TMP_InputField text;
    private bool interactable;
    private bool emptyText;

    // Start is called before the first frame update
    void Start()
    {
        text = GetComponent<TMP_InputField>();
        interactable = true;
        emptyText = true;
    }

    // Update is called once per frame
    void Update()
    {
        bool newInteractable = text.IsInteractable();
        if (newInteractable != interactable)
        {
            miniLabel.color = newInteractable ? miniLabelColor :
                disabledColor;
            label.color = newInteractable ? labelColor :
                disabledColor;
            inputText.color = newInteractable ? inputTextColor :
                disabledColor;
        }
        interactable = newInteractable;

        bool newEmptyText = text.text == "";
        if (newEmptyText != emptyText)
        {
            miniLabel.text = label.text;
            miniLabelObject.SetActive(!newEmptyText);
        }
        emptyText = newEmptyText;
    }
}
