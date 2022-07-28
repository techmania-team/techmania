using System;
using System.Collections;
using System.Collections.Generic;

// Contains either an OK status or an error message.
[MoonSharp.Interpreter.MoonSharpUserData]
public class Status
{
    public enum Code
    {
        OK,
        // A file is not found.
        NotFound,
        // An I/O error occurred when reading/writing a file.
        IOError,
        // I/O succeeded, but the file is in an unsupported format.
        FormatError,
        // All other errors.
        OtherError,
    }

    public Code codeEnum { get; private set; }
    public string code => codeEnum.ToString();

    // If not ok, this is the error message.
    public string errorMessage { get; private set; }
    // If not ok and the error involves a file, this is its path.
    public string filePath { get; private set; }

    public bool Ok() { return codeEnum == Code.OK; }

    public static Status OKStatus()
    {
        return new Status()
        {
            codeEnum = Code.OK
        };
    }

    public static Status Error(Code code,
        string message = null,
        string path = null)
    {
        UnityEngine.Debug.LogError($"An error occurred.\n" +
            $"Code: {code}\n" +
            $"Message: {message}\n" +
            $"Path: {path}");
        return new Status()
        {
            codeEnum = code,
            errorMessage = message,
            filePath = path
        };
    }

    public static Status FromException(Exception ex,
        string path = null)
    {
        Code code = Code.OtherError;
        if (ex is System.IO.FileNotFoundException ||
            ex is System.IO.DirectoryNotFoundException)
        {
            code = Code.NotFound;
        }
        else if (ex is System.IO.IOException)
        {
            code = Code.IOError;
        }
        else if (ex is System.ArgumentException ||
            ex is System.FormatException)
        {
            code = Code.FormatError;
        }

        Status status = Error(code, ex.Message, path);
        UnityEngine.Debug.LogError("Stack trace:\n" + ex.StackTrace);
        return status;
    }
}
