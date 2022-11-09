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
}
