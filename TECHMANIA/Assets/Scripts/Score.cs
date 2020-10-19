using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Notes on FEVER:
//
// Accumulation
//
// Fever += 8/(numPlayableNotes) on RMAX and MAX
// No change on COOL and GOOD
// Fever *= 0.75 on MISS
// Fever *= 0.5 on BREAK
//
// Duration: 10 seconds
//
// UI
//
// Before activation: top bar says "FEVER OFF", middle bar says nothing
// Ready: top bar pulses and alternates "FEVER" and "TOUCH",
//        middle bar says "FEVER"
// Activate: same as before activation
public class Score
{
    private int totalNotes;
    public Dictionary<Judgement, int> notesPerJudgement { get; private set; }
    public bool stageFailed;
    public int totalFeverBonus { get; private set; }
    private int oneTimeFeverBonus;
    private bool feverActive;

    public void Initialize(int totalNotes)
    {
        this.totalNotes = totalNotes;
        notesPerJudgement = new Dictionary<Judgement, int>();
        foreach (Judgement j in System.Enum.GetValues(typeof(Judgement)))
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
                    oneTimeFeverBonus += 1;
                    break;
                case Judgement.Cool:
                    oneTimeFeverBonus += 5;
                    break;
            }
        }
    }

    public int CurrentScore()
    {
        // Rainbow Max = 300,000 / totalNotes
        // Max = Rainbow Max - 1
        // Cool = Rainbow Max * 0.7
        // Good = Rainbow Max * 0.4
        // Miss/Break = 0
        float maxMultiplier = notesPerJudgement[Judgement.RainbowMax] * 1f
            + notesPerJudgement[Judgement.Max] * 1f
            + notesPerJudgement[Judgement.Cool] * 0.7f
            + notesPerJudgement[Judgement.Good] * 0.4f;
        int score = Mathf.FloorToInt(300000 * maxMultiplier / totalNotes);
        score -= notesPerJudgement[Judgement.Max];

        if (feverActive)
        {
            score += oneTimeFeverBonus;
        }
        else
        {
            score += totalFeverBonus;
        }
        return score;
    }
}
