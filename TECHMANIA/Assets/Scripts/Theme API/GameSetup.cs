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

        // Track and pattern

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

        // Parameter: GameTimer
        // Not called when paused.
        public DynValue onUpdate;
        // Parameter: Note, Judgement, ScoreKeeper.
        public DynValue onNoteResolved;
        // Parameter: ScoreKeeper.
        public DynValue onAllNotesResolved;
        // No parameter.
        public DynValue onFeverReady;
        // No parameter. This is called whether auto fever is on or not.
        public DynValue onFeverActivated;
        // Parameter: current Fever value from 0 to 1. Only called
        // when fever is active.
        public DynValue onFeverUpdate;
        // Parameter: Fever bonus.
        public DynValue onFeverEnd;
        // Parameter: ScoreKeeper. The game will automatically
        // conclude immediately after calling this.
        public DynValue onStageClear;
        // Parameter: ScoreKeeper. The game will automatically
        // conclude immediately after calling this.
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
    }
}