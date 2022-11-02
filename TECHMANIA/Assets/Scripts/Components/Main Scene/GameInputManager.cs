using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class GameInputManager
{
    private Pattern pattern;
    private ControlScheme scheme;
    private int lanes;

    private GameLayout layout;
    private NoteManager noteManager;

    private List<List<KeyCode>> keysForLane;

    public GameInputManager(
        Pattern pattern,
        GameLayout layout,
        NoteManager noteManager)
    {
        this.pattern = pattern;
        scheme = pattern.patternMetadata.controlScheme;
        lanes = pattern.patternMetadata.playableLanes;

        this.layout = layout;
        this.noteManager = noteManager;
    }

    public void Prepare()
    {
        // Prepare keycodes for keyboard input.
        keysForLane = new List<List<KeyCode>>();
        if (lanes >= 4)
        {
            keysForLane.Add(new List<KeyCode>()
            {
                KeyCode.BackQuote,
                KeyCode.Alpha0,
                KeyCode.Alpha1,
                KeyCode.Alpha2,
                KeyCode.Alpha3,
                KeyCode.Alpha4,
                KeyCode.Alpha5,
                KeyCode.Alpha6,
                KeyCode.Alpha7,
                KeyCode.Alpha8,
                KeyCode.Alpha9,
                KeyCode.Minus,
                KeyCode.Equals,
                KeyCode.KeypadDivide,
                KeyCode.KeypadMultiply,
                KeyCode.KeypadMinus
            });
        }
        if (lanes >= 3)
        {
            keysForLane.Add(new List<KeyCode>()
            {
                KeyCode.Q,
                KeyCode.W,
                KeyCode.E,
                KeyCode.R,
                KeyCode.T,
                KeyCode.Y,
                KeyCode.U,
                KeyCode.I,
                KeyCode.O,
                KeyCode.P,
                KeyCode.LeftBracket,
                KeyCode.RightBracket,
                KeyCode.Backslash,
                KeyCode.Keypad7,
                KeyCode.Keypad8,
                KeyCode.Keypad9
            });
        }
        keysForLane.Add(new List<KeyCode>()
        {
            KeyCode.A,
            KeyCode.S,
            KeyCode.D,
            KeyCode.F,
            KeyCode.G,
            KeyCode.H,
            KeyCode.J,
            KeyCode.K,
            KeyCode.L,
            KeyCode.Semicolon,
            KeyCode.Quote,
            KeyCode.Keypad4,
            KeyCode.Keypad5,
            KeyCode.Keypad6
        });
        keysForLane.Add(new List<KeyCode>()
        {
            KeyCode.Z,
            KeyCode.X,
            KeyCode.C,
            KeyCode.V,
            KeyCode.B,
            KeyCode.N,
            KeyCode.M,
            KeyCode.Comma,
            KeyCode.Period,
            KeyCode.Slash,
            KeyCode.Keypad1,
            KeyCode.Keypad2,
            KeyCode.Keypad3
        });
    }

    public void Update(GameTimer timer)
    {
        // TODO: if auto play, do nothing

        // Input handling gets a bit complicated so here's a graph.
        //
        // Touch/KM                Keys/KM           Timer
        // --------                -------           -----
        // OnFingerDown/Move[0]    OnKeyDown[1]      UpdateTime[2]
        //     |                       |                 |
        // ProcessMouseOrFingerDown    |                 |
        //     |                       |                 |
        //     -------------------------                 |
        //                |                              |
        //             HitNote                           |
        //              |   |                            |
        //              |   ------------------------------
        //              |                 |
        //       RegisterOngoing       ResolveNote
        //
        // [0] mouse is considered finger #0; finger moving between
        // lanes will cause a new finger down event.
        // [1] takes a lane number in Keys, works on any lane in KM.
        // [2] the timer will resolve notes as Breaks when the
        // time window to play them has completely passed.
        //
        //
        //
        // Parallel to the above is the handling of ongoing notes.
        //
        // Touch/KM           Keys/KM         Update
        // --------           -------         ------
        // OnFingerHeld       OnKeyHeld[2]    UpdateOngoingNotes[1]
        //      |                 |                    |
        //      -------------------                    |
        //              |                              |
        //        HitOngoingNote[0]                    |
        //              |                              |
        //              --------------------------------
        //                              |
        //                         ResolveNote
        //
        // [0] marks the note as being hit on the current frame, or
        // resolves the note if its duration has passed.
        // [1] after all input is handled, any ongoing note not
        // marked on the current frame will be resolved as Misses.
        // [2] takes a lane number in Keys, works on any lane in KM.

        // TODO: KM works on right mouse button as well.
    }

    public void Dispose()
    {
        keysForLane.Clear();
    }

    #region Transformation
    // Note: Y+ is downward.
    private static Vector2 ScreenSpaceToElementLocalSpace(
        VisualElement element, Vector2 screenSpace)
    {
        Vector2 invertedScreenSpace = new Vector2(
            screenSpace.x, Screen.height - screenSpace.y);
        Vector2 worldSpace = RuntimePanelUtils.ScreenToPanel(
            element.panel, invertedScreenSpace);
        return element.WorldToLocal(worldSpace);
    }

    private static bool ElementContainsPointInScreenSpace(
        VisualElement element, Vector2 screenSpace)
    {
        Vector2 localSpace = ScreenSpaceToElementLocalSpace(
            element, screenSpace);
        return element.ContainsPoint(localSpace);
    }
    #endregion
}
