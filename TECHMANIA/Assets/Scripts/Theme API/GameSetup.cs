using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;

namespace ThemeApi
{
    [MoonSharpUserData]
    public class GameSetup
    {
        [MoonSharpHidden]
        public GameSetup()
        {
            GameController.instance.SetSetupInstance(this);
        }

        // Track and pattern (unused in editor preview)

        // The folder that contains the track.tech file.
        public string trackFolder;
        public string patternGuid;

        // VisualElements

        public VisualElementWrap bgContainer;
        // Touch feedback uses additive shader and should be
        // drawn below notes. It is not supported at this time.
        public VisualElementWrap gameContainer;
        public VisualElementWrap vfxComboContainer;  // Unused

        // Audio

        public AudioClip assistTick;

        // Callbacks during Loading state

        [MoonSharpUserData]
        public class LoadProgress
        {
            public string fileJustLoaded;  // Relative to track folder
            public int filesLoaded;
            public int totalFiles;
        }
        // Parameter: LoadProgress
        public DynValue onLoadProgress;
        // Parameter: Status
        public DynValue onLoadError;
        // No parameter.
        public DynValue onLoadComplete;

        // Callbacks during Ongoing state

        // Parameter: GameTimer. Called every frame, in
        // Ongoing and Paused states.
        public DynValue onUpdate;
        // Parameter: Note, Judgement, ScoreKeeper.
        public DynValue onNoteResolved;
        // Parameter: ScoreKeeper.
        public DynValue onAllNotesResolved;
        // Parameter: current combo. Called when an ongoing long note
        // accumulates one more combo; has no meaningful change in
        // score.
        public DynValue onComboTick;
        // No parameter.
        public DynValue onFeverReady;
        // No parameter. Called when Fever leaves Ready state due to
        // a MISS or BREAK.
        public DynValue onFeverUnready;
        // No parameter. Called whether auto fever is on or not.
        public DynValue onFeverActivated;
        // Parameter: current Fever value from 0 to 1. Called when
        // Fever value updates from resolving a note, or when it is
        // active and depletes with time.
        public DynValue onFeverUpdate;
        // Parameter: Fever bonus.
        public DynValue onFeverEnd;
        // Parameter: ScoreKeeper. The game will be in Complete state
        // when this is called.
        public DynValue onStageClear;
        // Parameter: ScoreKeeper. The game will be in Complete state
        // when this is called.
        public DynValue onStageFailed;

        // Internal stuff

        // The folder that contains the track.tech file.
        [MoonSharpHidden]
        public string lockedTrackFolder;
        [MoonSharpHidden]
        public PerTrackOptions trackOptions;
        [MoonSharpHidden]
        public Pattern patternBeforeModifier;
        [MoonSharpHidden]
        public Pattern patternAfterModifier;
        [MoonSharpHidden]
        public Modifiers modifiers;
        [MoonSharpHidden]
        public bool anySpecialModifier;  // TODO: make copy of modifiers so we can override this in editor preview
        [MoonSharpHidden]
        public Options.Ruleset ruleset;
    }
}