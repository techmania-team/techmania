using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class JudgementText : MonoBehaviour
{
    private const float kStayTime = 2f;
    private float remainingTime;
    private Text text;

    // Start is called before the first frame update
    void Start()
    {
        text = GetComponentInChildren<Text>();
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
            display = MakeRainbowText(display);
        }
        text.text = display;

        switch (judgement)
        {
            case Judgement.Max:
                text.color = Color.green;
                break;
            case Judgement.Cool:
                text.color = Color.magenta;
                break;
            case Judgement.Good:
                text.color = Color.blue;
                break;
            case Judgement.Miss:
                text.color = new Color(0.5f, 0f, 0.5f);
                break;
            case Judgement.Break:
                text.color = Color.red;
                break;
        }
    }

    private string MakeRainbowText(string original)
    {
        StringBuilder builder = new StringBuilder();
        List<string> colorNames = new List<string>()
        {
            "red",
            "green",
            "blue"
        };
        for (int i = 0; i < original.Length; i++)
        {
            string c = colorNames[i % colorNames.Count];
            builder.Append($"<color={c}>{original[i]}</color>");
        }
        return builder.ToString();
    }
}
