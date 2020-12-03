using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JudgementText : MonoBehaviour
{
    public Texture rainbowTexture;
    public Color maxColor;
    public Color feverMaxColor;
    public Color coolColor;
    public Color goodColor;
    public Color missColor;
    public Color breakColor;

    private TextMeshProUGUI text;
    private Animator animator;
    private Transform noteToFollow;

    // Start is called before the first frame update
    void Start()
    {
        text = GetComponentInChildren<TextMeshProUGUI>();
        text.text = "";
        animator = text.GetComponent<Animator>();
        noteToFollow = null;
    }

    public void Show(NoteObject n, Judgement judgement)
    {
        noteToFollow = n.GetComponent<NoteAppearance>()
            .noteImage.transform;
        Follow();

        string display = "";
        switch (judgement)
        {
            case Judgement.RainbowMax:
                display = "MAX";
                break;
            default:
                display = judgement.ToString().ToUpper();
                break;
        }
        if (Game.currentCombo > 0)
        {
            display += " " + Game.currentCombo;
        }

        if (Game.feverState == Game.FeverState.Active &&
            (judgement == Judgement.RainbowMax ||
             judgement == Judgement.Max))
        {
            text.fontSharedMaterial.SetTexture(ShaderUtilities.ID_FaceTex,
                null);
            text.faceColor = feverMaxColor;
        }
        else if (judgement == Judgement.RainbowMax &&
            Game.feverState != Game.FeverState.Active)
        {
            // https://forum.unity.com/threads/change-textmesh-pro-face-texture-with-script.679912/
            text.fontSharedMaterial.SetTexture(ShaderUtilities.ID_FaceTex,
                rainbowTexture);
            // Cannot set color: Animator will control that in order
            // to set alpha.
            text.faceColor = Color.white;
        }
        else
        {
            text.fontSharedMaterial.SetTexture(ShaderUtilities.ID_FaceTex,
                null);
            switch (judgement)
            {
                case Judgement.Max:
                    text.faceColor = maxColor;
                    break;
                case Judgement.Cool:
                    text.faceColor = coolColor;
                    break;
                case Judgement.Good:
                    text.faceColor = goodColor;
                    break;
                case Judgement.Miss:
                    text.faceColor = missColor;
                    break;
                case Judgement.Break:
                    text.faceColor = breakColor;
                    break;
            }
        }
        text.text = display;
        animator.SetTrigger("Activate");
    }

    private void Update()
    {
        if (noteToFollow != null)
        {
            Follow();
        }
    }

    private void Follow()
    {
        transform.position = noteToFollow.position;
    }
}
