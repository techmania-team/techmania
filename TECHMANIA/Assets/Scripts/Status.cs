using System;
using System.Collections;
using System.Collections.Generic;

// Contains either an OK status or an error message.
[MoonSharp.Interpreter.MoonSharpUserData]
public class Status
{
    public bool ok { get; private set; }
    public string errorMessage { get; private set; }

    public static Status OKStatus()
    {
        return new Status()
        {
            ok = true
        };
    }

    public static Status Error(string message)
    {
        return new Status()
        {
            ok = false,
            errorMessage = message
        };
    }

    // This logs the exception to the Unity console.
    public static Status Error(Exception ex)
    {
        UnityEngine.Debug.LogException(ex);
        return new Status()
        {
            ok = false,
            errorMessage = ex.Message
        };
    }
}
