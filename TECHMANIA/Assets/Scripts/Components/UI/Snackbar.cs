using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Snackbar : MonoBehaviour
{
    public TextMeshProUGUI message;
    private Animator animator;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void Show(string message)
    {
        this.message.text = message;
        animator.SetTrigger("Activate");
    }

    public void Dismiss()
    {
        animator.SetTrigger("Dismiss");
    }
}
