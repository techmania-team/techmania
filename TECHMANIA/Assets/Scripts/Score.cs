using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;

public enum PerformanceMedal
{
    NoMedal,
    AllCombo,
    PerfectPlay,
    AbsolutePerfect
}

// TODO: make this read-only to Lua.
[MoonSharpUserData]
public class Score
{
    public int totalNotes { get; private set; }
    public Dictionary<Judgement, int> notesPerJudgement
        { get; private set; }
    public bool stageFailed
    {
        get;
        [MoonSharpHidden]
        set;
    }
    public int totalFeverBonus { get; private set; }
    public int comboBonus { get; private set; }

    private int oneTimeFeverBonus;
    private bool feverActive;
    private int maxScore => Ruleset.instance.comboBonus ?
        290000 : 300000;

    [MoonSharpHidden]
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

    [MoonSharpHidden]
    public void FeverOn()
    {
        oneTimeFeverBonus = 0;
        feverActive = true;
    }

    [MoonSharpHidden]
    // Returns the Fever bonus from this Fever activation.
    public int FeverOff()
    {
        totalFeverBonus += oneTimeFeverBonus;
        feverActive = false;
        return oneTimeFeverBonus;
    }

    [MoonSharpHidden]
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

    // Does not take fever bonus and combo bonus into account.
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
        return score;
    }

    public string GetRank()
    {
        if (stageFailed) return "F";
        return ScoreToRank(CurrentScore());
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

    public bool AllNotesResolved()
    {
        int numNotesResolved = 0;
        foreach (KeyValuePair<Judgement, int> pair in
            notesPerJudgement)
        {
            numNotesResolved += pair.Value;
        }
        return numNotesResolved >= totalNotes;
    }

    public PerformanceMedal Medal()
    {
        if (notesPerJudgement[Judgement.Miss] +
            notesPerJudgement[Judgement.Break] > 0)
        {
            return PerformanceMedal.NoMedal;
        }
        if (notesPerJudgement[Judgement.Cool] +
            notesPerJudgement[Judgement.Good] > 0)
        {
            return PerformanceMedal.AllCombo;
        }
        if (notesPerJudgement[Judgement.Max] > 0)
        {
            return PerformanceMedal.PerfectPlay;
        }
        return PerformanceMedal.AbsolutePerfect;
    }

    // Assumes player did not fail.
    public static string ScoreToRank(int score)
    {
        if (score > 295000) return "S++";
        if (score > 290000) return "S+";
        if (score > 285000) return "S";
        if (score > 280000) return "A++";
        if (score > 270000) return "A+";
        if (score > 260000) return "A";
        if (score > 220000) return "B";
        return "C";
    }
}
