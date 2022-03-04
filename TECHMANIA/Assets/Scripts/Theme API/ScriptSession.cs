using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;
using UnityEngine.UIElements;
using System;

namespace ThemeApi
{
    public class ScriptSession
    {
        private static Script session;

        public static void Prepare()
        {
            // Set up sandbox
            session = new Script(CoreModules.Preset_SoftSandbox);
            // Redirect print
            session.Options.DebugPrint = (s) => { Debug.Log(s); };
            // Register types
            UserData.RegisterType<VisualTreeAsset>();
            UserData.RegisterType<VisualElement>();
            UserData.RegisterType<ClickEvent>();
            UserData.RegisterType<PanelSettings>();
            UserData.RegisterType<StyleSheet>();
            UserData.RegisterType<UQueryState<VisualElement>>();
            UserData.RegisterType<Button>();
            UserData.RegisterType<Label>();
            UserData.RegisterType<AudioSource>();
            UserData.RegisterAssembly();
            // Preparations
            Techmania.Prepare();
            // Expose API
            session.Globals["getApi"] = (Func<int, object>)GetApi;
        }

        public static void Execute(string script)
        {
            session.DoString(script);
        }

        public static object GetApi(int version)
        {
            switch (version)
            {
                case 1:
                    return new Techmania();
                default:
                    throw new ApiNotSupportedException();
            }
        }
    }

    public class ApiNotSupportedException : Exception { }
}