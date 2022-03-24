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
        public static Script session { get; private set; }

        public static void Prepare()
        {
            // Set up sandbox
            session = new Script(CoreModules.Preset_SoftSandbox);
            // Redirect print
            session.Options.DebugPrint = (s) => { Debug.Log(s); };
            // Register types
            UserData.RegisterType<VisualTreeAsset>();
            UserData.RegisterType<VisualElement>();
            UserData.RegisterType<PanelSettings>();
            UserData.RegisterType<UQueryState<VisualElement>>();
            UserData.RegisterType<IStyle>();
            UserData.RegisterType<ITransform>();
            UserData.RegisterType<StyleSheet>();
            UserData.RegisterType<StyleLength>();
            UserData.RegisterType<StyleFloat>();
            UserData.RegisterType<AudioSource>();
            foreach (VisualElementWrap.EventType typeEnum in
                Enum.GetValues(typeof(VisualElementWrap.EventType)))
            {
                try
                {
                    Type type = VisualElementWrap.EventTypeEnumToType(
                        typeEnum);
                    UserData.RegisterType(type);
                }
                catch (Exception)
                {
                    // Skip unsupported event types
                    continue;
                }
            }
            UserData.RegisterType<Time>();
            UserData.RegisterType<Mathf>();
            UserData.RegisterType<Vector3>();
            UserData.RegisterAssembly();
            // Preparations
            Techmania.Prepare();
            // Expose API
            session.Globals["getApi"] = (Func<int, object>)GetApi;
            // Expose Unity classes
            session.Globals["time"] = UserData.CreateStatic<Time>();
            session.Globals["math"] = UserData.CreateStatic<Mathf>();
            session.Globals["vector3"] =
                UserData.CreateStatic<Vector3>();
            // Expose enums
            session.Globals["audioChannel"] = 
                UserData.CreateStatic<AudioManager.Channel>();
            session.Globals["eventType"] =
                UserData.CreateStatic<VisualElementWrap.EventType>();
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