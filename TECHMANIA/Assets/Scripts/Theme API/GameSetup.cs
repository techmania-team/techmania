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
        public VisualElementWrap gameContainer;
        public VisualElementWrap vfxComboContainer;  // Unused

        // Callbacks

        // Parameter: new state as string
        public DynValue onStateChange;

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

        // Callbacks during Ongoing state

        // No parameter, theme should read game state on its own.
        public DynValue onUpdate;
        // No parameter.
        public DynValue onNoteResolved;
        // Parameter: current Fever value from 0 to 1.
        public DynValue onFeverUpdate;
        // Parameter: Fever bonus.
        public DynValue onFeverEnd;

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