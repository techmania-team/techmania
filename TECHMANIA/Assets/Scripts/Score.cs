using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Score
{
    private int totalNotes;
    public Dictionary<Judgement, int> notesPerJudgement
    { get; private set; }
    public bool stageFailed;
    public int totalFeverBonus { get; private set; }
    public int comboBonus { get; private set; }

    private int oneTimeFeverBonus;
    private bool feverActive;
    private int maxScore => Ruleset.instance.comboBonus ?
        290000 : 300000;

    public void Initialize(int totalNotes)
    {
        this.totalNotes = totalNotes;
        notesPerJudgement = new Dictionary<Judgement, int>();
        foreach (Judgement j in System.Enum.GetValues(
            typeof(Judgement)))
        {
            notesPerJudgement.Add(j, 0);
        }
        stageFailed = false;
        totalFeverBonus = 0;
        oneTimeFeverBonus = 0;
        feverActive = false;
    }

    public void FeverOn()
    {
        oneTimeFeverBonus = 0;
        feverActive = true;
    }

    // Returns the Fever bonus from this Fever activation.
    public int FeverOff()
    {
        totalFeverBonus += oneTimeFeverBonus;
        feverActive = false;
        return oneTimeFeverBonus;
    }

    public void LogNote(Judgement j)
    {
        notesPerJudgement[j]++;
        if (feverActive)
        {
            switch (j)
            {
                case Judgement.Max:
                    oneTimeFeverBonus += 
                        Ruleset.instance.feverBonusOnMax;
                    break;
                case Judgement.Cool:
                    oneTimeFeverBonus += 
                        Ruleset.instance.feverBonusOnCool;
                    break;
                case Judgement.Good:
                    oneTimeFeverBonus +=
                        Ruleset.instance.feverBonusOnGood;
                    break;
            }
        }
    }

    public int CurrentScore()
    {
        if (totalNotes == 0) return 0;

        // Rainbow Max = maxScore / totalNotes
        // Max = Rainbow Max - 1
        // Cool = Rainbow Max * 0.7
        // Good = Rainbow Max * 0.4
        // Miss/Break = 0
        float maxMultiplier =
            notesPerJudgement[Judgement.RainbowMax] * 1f
            + notesPerJudgement[Judgement.Max] * 1f
            + notesPerJudgement[Judgement.Cool] * 0.7f
            + notesPerJudgement[Judgement.Good] * 0.4f;
        int score = Mathf.FloorToInt(
            maxScore * maxMultiplier / totalNotes);
        score -= notesPerJudgement[Judgement.Max];
        score += totalFeverBonus;
        if (feverActive)
        {
            score += oneTimeFeverBonus;
        }
        return score;
    }

    public void CalculateComboBonus()
    {
        if (!Ruleset.instance.comboBonus)
        {
            comboBonus = 0;
            return;
        }

        if (notesPerJudgement[Judgement.RainbowMax] +
            notesPerJudgement[Judgement.Max] == totalNotes)
        {
            comboBonus = 10000;
        }
        else
        {
            int missAndBreaks = notesPerJudgement[Judgement.Miss] +
                notesPerJudgement[Judgement.Break];
            // Why, just why.
            comboBonus = Mathf.FloorToInt(
                (float)(7800 * missAndBreaks + 9800) *
                (totalNotes - missAndBreaks) /
                (missAndBreaks + 1) /
                totalNotes);
        }
    }
}
