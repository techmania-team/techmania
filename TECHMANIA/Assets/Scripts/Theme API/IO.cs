using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;

namespace ThemeApi
{
    [MoonSharpUserData]
    public class IO
    {
        // Callback parameter: Status, Texture2D
        public static void LoadTexture(string path,
            DynValue callback)
        {
            ResourceLoader.LoadImage(path,
                (Status status, Texture2D texture) =>
                {
                    callback.Function.Call(status, texture);
                });
        }
    }
}