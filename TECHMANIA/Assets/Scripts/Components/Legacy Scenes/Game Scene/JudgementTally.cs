using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class JudgementTally : MonoBehaviour
{
    public TextMeshProUGUI rMax;
    public TextMeshProUGUI max;
    public TextMeshProUGUI cool;
    public TextMeshProUGUI good;
    public TextMeshProUGUI miss;
    public TextMeshProUGUI breakDisplay;

    public void Refresh(Score score)
    {
        rMax.text = score.notesPerJudgement[Judgement.RainbowMax]
            .ToString();
        max.text = score.notesPerJudgement[Judgement.Max]
            .ToString();
        cool.text = score.notesPerJudgement[Judgement.Cool]
            .ToString();
        good.text = score.notesPerJudgement[Judgement.Good]
            .ToString();
        miss.text = score.notesPerJudgement[Judgement.Miss]
            .ToString();
        breakDisplay.text = score.notesPerJudgement[Judgement.Break]
            .ToString();
    }
}
