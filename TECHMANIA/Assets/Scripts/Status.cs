using System.Collections;
using System.Collections.Generic;

// Contains either an OK status or an error message.
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
}
