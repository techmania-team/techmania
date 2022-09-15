using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;

namespace ThemeApi
{
    [MoonSharpUserData]
    public class GameSetup
    {
        // Track and pattern

        // The folder that contains the track.tech file.
        public string trackFolder;
        public string patternGuid;

        // VisualElements

        public VisualElementWrap bgContainer;
        public VisualElementWrap gameContainer;

        // Callbacks

        // Parameter: new state as string
        public DynValue onStateChange;

        // Callbacks during Loading state

        // Parameter: custom class with the file just loaded, number
        // of files loaded, total number of files.
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
    }
}