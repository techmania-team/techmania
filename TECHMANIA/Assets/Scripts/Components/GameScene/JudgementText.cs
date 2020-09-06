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

    private const float kStayTime = 2f;
    private float remainingTime;
    private TextMeshProUGUI text;

    // Start is called before the first frame update
    void Start()
    {
        text = GetComponentInChildren<TextMeshProUGUI>();
        text.text = "";
    }

    // Update is called once per frame
    void Update()
    {
        if (remainingTime > 0f)
        {
            remainingTime -= Time.deltaTime;
            if (remainingTime <= 0f)
            {
                text.text = "";
            }
        }
    }

    public void Show(NoteObject n, Judgement judgement)
    {
        transform.position = n.transform.position;
        remainingTime = kStayTime;

        // text.text = judgement.ToString() + " " + Game.currentCombo;
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

        if (judgement == Judgement.RainbowMax)
        {
            // https://forum.unity.com/threads/change-textmesh-pro-face-texture-with-script.679912/
            text.fontSharedMaterial.SetTexture(ShaderUtilities.ID_FaceTex,
                rainbowTexture);
            text.color = Color.white;
        }
        else
        {
            text.fontSharedMaterial.SetTexture(ShaderUtilities.ID_FaceTex,
                null);
            switch (judgement)
            {
                case Judgement.Max:
                    text.color = maxColor;
                    break;
                case Judgement.Cool:
                    text.color = coolColor;
                    break;
                case Judgement.Good:
                    text.color = goodColor;
                    break;
                case Judgement.Miss:
                    text.color = missColor;
                    break;
                case Judgement.Break:
                    text.color = breakColor;
                    break;
            }
        }
        text.text = display;
    }
}
