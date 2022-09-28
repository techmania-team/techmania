using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public static GameController instance { get; private set; }
    private ThemeApi.GameSetup setup;
    private ThemeApi.GameState state;

    public void SetSetupInstance(ThemeApi.GameSetup s)
    {
        setup = s;
    }
    public void SetStateInstance(ThemeApi.GameState s)
    {
        state = s;
    }

    // Start is called before the first frame update
    void Start()
    {
        instance = this;
    }

    public void BeginLoading()
    {
        // Load and lock down the track and pattern so the theme
        // can't change them later.
        setup.lockedTrackFolder = string.Copy(setup.trackFolder);

        string trackPath = Paths.Combine(setup.lockedTrackFolder,
            Paths.kTrackFilename);
        Track track;
        try
        {
            track = Track.LoadFromFile(trackPath) as Track;
        }
        catch (Exception ex)
        {
            setup.onLoadError.Function.Call(Status.FromException(ex));
            state.SetState(ThemeApi.GameState.State.LoadError);
            return;
        }

        setup.trackOptions = Options.instance.GetPerTrackOptions(
            track.trackMetadata.guid);

        setup.patternBeforeModifier = null;
        foreach (Pattern p in track.patterns)
        {
            if (p.patternMetadata.guid == setup.patternGuid)
            {
                setup.patternBeforeModifier = p;
                break;
            }
        }
        if (setup.patternBeforeModifier == null)
        {
            setup.onLoadError.Function.Call(Status.Error(
                Status.Code.NotFound,
                "The specified pattern is not found in the track."));
            state.SetState(ThemeApi.GameState.State.LoadError);
            return;
        }
        setup.patternAfterModifier = setup.patternBeforeModifier
            .ApplyModifiers(Modifiers.instance);

        // Load options.
        Options.RefreshInstance();
        if (Options.instance.rulesetEnum == Options.Ruleset.Custom)
        {
            Ruleset.LoadCustomRuleset();
        }

        // Start the load sequence.
        StartCoroutine(LoadSequence());
    }

    private IEnumerator LoadSequence()
    {
        yield return null;

        // Load complete; wait on theme to begin game.
        state.SetState(ThemeApi.GameState.State.LoadComplete);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
