using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FeverButton : MonoBehaviour
{
    public RectTransform filling;

    private Animator animator;
    
    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        SetAmount(0f);
        SetReady(false);
    }

    public void SetAmount(float amount)
    {
        filling.anchorMax = new Vector2(amount, 1f);
    }

    public void SetReady(bool ready)
    {
        animator.SetBool("Fever Ready", ready);
    }    
}
