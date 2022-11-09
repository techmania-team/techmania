using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;

[MoonSharpUserData]
// Keeps track of score, combo, HP and fever.
public class ScoreKeeper
{
    // Score
    public Score score { get; private set; }

    // Combo
    public int currentCombo { get; private set; }
    public int maxCombo { get; private set; }

    // HP
    public int maxHp => Ruleset.instance.maxHp;
    public int hp { get; private set; }

    // Fever
    public enum FeverState
    {
        Building,  // Accummulates with MAXes
        Ready,  // No longer accummulates, awaiting activation
        Active  // Decreases with time
    }
    private float feverCoefficient;
    private System.Diagnostics.Stopwatch feverTimer;
    public FeverState feverState { get; private set; }
    public float feverAmount { get; private set; }

    [MoonSharpHidden]
    public void Prepare(Pattern pattern, int firstScan, int lastScan,
        int playableNotes)
    {
        // Score.
        score = new Score();
        score.Initialize(playableNotes);

        // Combo.
        currentCombo = 0;
        maxCombo = 0;

        // HP.
        hp = maxHp;

        // Calculate Fever coefficient. The goal is for the Fever bar
        // to fill up in around 12.5 seconds.
        if (Ruleset.instance.constantFeverCoefficient)
        {
            feverCoefficient = 8f;
        }
        else
        {
            int lastPulse = (lastScan + 1 - firstScan) *
                pattern.patternMetadata.bps *
                Pattern.pulsesPerBeat;
            float trackLength = pattern.PulseToTime(lastPulse);
            feverCoefficient = trackLength / 12.5f;
        }
        Debug.Log("Fever coefficient is: " + feverCoefficient);

        // Other Fever fields.
        feverState = FeverState.Building;
        feverAmount = 0;
    }

    [MoonSharpHidden]
    public void ResolveNote(NoteType noteType, Judgement judgement)
    {
        bool missOrBreak = judgement == Judgement.Miss ||
            judgement == Judgement.Break;

        // Combo
        if (!missOrBreak)
        {
            SetCombo(currentCombo + 1);
        }
        else
        {
            SetCombo(0);
        }

        if (Modifiers.instance.mode == Modifiers.Mode.Practice)
        {
            // Score, HP and Fever don't update in Practice mode.
            return;
        }

        // Score
        score.LogNote(judgement);

        // HP
        hp += Ruleset.instance.GetHpDelta(
            judgement, noteType,
            feverState == FeverState.Active);
        // It's up to GameController to set stage failed.
        if (hp < 0) hp = 0;
        if (hp >= Ruleset.instance.maxHp)
        {
            hp = Ruleset.instance.maxHp;
        }

        // Fever
        if (!missOrBreak)
        {
            if (feverState == FeverState.Building &&
                (judgement == Judgement.RainbowMax ||
                judgement == Judgement.Max))
            {
                float feverDelta = feverCoefficient / score.totalNotes;
                if (GameController.autoPlay) feverDelta = 0f;
                if (Modifiers.instance.fever == 
                    Modifiers.Fever.FeverOff)
                {
                    feverDelta = 0;
                }

                feverAmount += feverDelta;
                if (feverAmount >= 1f)
                {
                    feverState = FeverState.Ready;
                    feverAmount = 1f;
                    // TODO: send fever ready event?
                    if (Modifiers.instance.fever
                        == Modifiers.Fever.AutoFever)
                    {
                        ActivateFever();
                    }
                }
            }
        }
        else
        {
            if (feverState == FeverState.Building ||
                feverState == FeverState.Ready)
            {
                switch (judgement)
                {
                    case Judgement.Miss:
                        feverAmount *= 0.75f;
                        break;
                    case Judgement.Break:
                        feverAmount *= 0.5f;
                        break;
                }
                feverState = FeverState.Building;
            }
        }
    }

    private void SetCombo(int combo)
    {
        currentCombo = combo;
        if (currentCombo > maxCombo)
        {
            maxCombo = currentCombo;
        }
    }

    [MoonSharpHidden]
    public void UpdateFever()
    {
        if (feverState != FeverState.Active) return;
        feverAmount = 1f -
            (float)feverTimer.Elapsed.TotalSeconds * 0.1f;
        if (feverAmount < 0f)
        {
            feverAmount = 0f;
            feverTimer.Stop();
            feverTimer = null;
            feverState = FeverState.Building;
            int feverBonus = score.FeverOff();
            // TODO: send fever end event with fever bonus.
        }
    }

    [MoonSharpHidden]
    public void Pause()
    {
        feverTimer?.Stop();
    }

    [MoonSharpHidden]
    public void Unpause()
    {
        feverTimer?.Start();
    }

    private void ActivateFever()
    {
        // TODO: send fever activated event
        feverState = FeverState.Active;
        score.FeverOn();
        feverTimer = new System.Diagnostics.Stopwatch();
        feverTimer.Start();
    }

    public void DeactivateFever()
    {
        if (feverState == FeverState.Active)
        {
            feverState = FeverState.Building;
            score.FeverOff();
        }
    }
}
