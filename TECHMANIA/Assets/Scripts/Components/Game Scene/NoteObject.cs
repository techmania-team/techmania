using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NoteObject : MonoBehaviour
{
    [HideInInspector]
    public Note note;
    [HideInInspector]
    public string sound;

    private Image feverOverlayImage;
    private Animator feverOverlayAnimator;

    private void Start()
    {
        feverOverlayAnimator = GetComponentInChildren<Animator>();
        feverOverlayImage = feverOverlayAnimator.GetComponent<Image>();
    }

    private void Update()
    {
        if (Game.feverState == Game.FeverState.Active)
        {
            if (!feverOverlayAnimator.enabled)
            {
                feverOverlayAnimator.enabled = true;
                feverOverlayImage.color = Color.white;
            }
            else if (Game.feverAmount < 0.1f)
            {
                feverOverlayImage.color = new Color(
                    1f, 1f, 1f, Game.feverAmount * 10f);
            }
        }
        else
        {
            if (feverOverlayAnimator.enabled)
            {
                feverOverlayAnimator.enabled = false;
                feverOverlayImage.color = Color.clear;
            }
        }
    }
}
