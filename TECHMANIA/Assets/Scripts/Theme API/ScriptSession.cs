using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;
using UnityEngine.UIElements;
using System;
using MoonSharp.VsCodeDebugger;
using System.Net.Sockets;

namespace ThemeApi
{
    public static class ScriptSession
    {
        public static Script session { get; private set; }
        public static int apiVersion { get; private set; }

        public static void Prepare()
        {
            // ==================== IMPORTANT =======================
            // IL2CPP builder may strip methods not called by any C#
            // code, causing them to be unavailable to Lua. To prevent
            // that, make sure to include in Assets/link.xml all Unity
            // and .Net classes exposed to Lua.
            // ==================== IMPORTANT =======================

            // Set up sandbox
            session = new Script(CoreModules.Preset_SoftSandbox);
            
            // Redirect print
            session.Options.DebugPrint = (s) => { Debug.Log(s); };

            // Register types
            UserData.RegisterAssembly();
            UserData.RegisterType<Dictionary<string, string>>();
            UserData.RegisterType<TimeSpan>();
            UserData.RegisterType<Rect>();
            UserData.RegisterType<Texture2D>();
            UserData.RegisterType<VisualTreeAsset>();
            UserData.RegisterType<VisualElement>();
            UserData.RegisterType<MeshGenerationContext>();
            UserData.RegisterType<MeshWriteData>();
            UserData.RegisterType<PanelSettings>();
            UserData.RegisterType<UQueryState<VisualElement>>();
            UserData.RegisterType<IStyle>();
            UserData.RegisterType<ITransform>();
            UserData.RegisterType<StyleSheet>();
            UserData.RegisterType<AudioSource>();
            UserData.RegisterType<AudioClip>();
            UserData.RegisterType<UnityEngine.Video.VideoClip>();
            UserData.RegisterType<UnityEngine.Video.VideoPlayer>();
            UserData.RegisterType<UnityEngine.TextCore.Text.FontAsset>();
            
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

            // Expose API
            session.Globals["getApi"] = (Func<int, Table>)GetApi;
        }

        public static void Execute(string script, string path = null)
        {
            if (path == null)
            {
                Debug.Log("Executing script from unknown origin");
            }
            else
            {
                Debug.Log("Executing script from file " + path);
            }
            session.DoString(script, globalContext: null, path);
        }

        public static Table GetApi(int version)
        {
            apiVersion = version;
            switch (version)
            {
                case 1:  // 2.0
                case 2:  // 2.1
                case 3:  // 2.2
                case 4:  // 2.3
                    return GetApiVersion1();
                default:
                    throw new ApiNotSupportedException();
            }
        }

        private static Table GetApiVersion1()
        {
            Table apiTable = new Table(session);

            Action<Table, Type, string> addTypeAs =
                (Table table, Type type, string key) =>
                {
                    UserData.RegisterType(type);
                    table[key] = UserData.CreateStatic(type);
                };
            Action<Table, Type> addType =
                (Table table, Type type) =>
                {
                    string key = type.Name.Substring(0, 1).ToLower()
                        + type.Name.Substring(1);
                    addTypeAs(table, type, key);
                };

            // Expose Techmania class and enums
            Techmania tm = new Techmania();
            Table tmEnums = new Table(session);
            addType(tmEnums, typeof(VisualElementWrap.EventType));
            addType(tmEnums, typeof(Options.Ruleset));
            addTypeAs(tmEnums, typeof(GameState.State), "gameState");
            addType(tmEnums, typeof(SkinType));
            addType(tmEnums, typeof(Techmania.Platform));
            addTypeAs(tmEnums, typeof(Status.Code), "statusCode");
            // Enums used by Track
            addType(tmEnums, typeof(ControlScheme));
            addType(tmEnums, typeof(NoteType));
            addType(tmEnums, typeof(CurveType));
            // Enums used by Modifiers
            addType(tmEnums, typeof(Modifiers.NoteOpacity));
            addType(tmEnums, typeof(Modifiers.ScanlineOpacity));
            addType(tmEnums, typeof(Modifiers.ScanDirection));
            addType(tmEnums, typeof(Modifiers.NotePosition));
            addType(tmEnums, typeof(Modifiers.ScanPosition));
            addType(tmEnums, typeof(Modifiers.Fever));
            addType(tmEnums, typeof(Modifiers.Keysound));
            addType(tmEnums, typeof(Modifiers.AssistTick));
            addType(tmEnums, typeof(Modifiers.SuddenDeath));
            addType(tmEnums, typeof(Modifiers.Mode));
            addType(tmEnums, typeof(Modifiers.ControlOverride));
            addType(tmEnums, typeof(Modifiers.ScrollSpeed));
            // Enums used by GameController and co.
            addType(tmEnums, typeof(Judgement));
            addType(tmEnums, typeof(PerformanceMedal));
            addType(tmEnums, typeof(ScoreKeeper.FeverState));
            // Enums used by setlists
            addType(tmEnums, typeof(Setlist.HiddenPatternCriteriaType));
            addType(tmEnums, typeof(
                Setlist.HiddenPatternCriteriaDirection));

            tm.@enum = tmEnums;
            apiTable["tm"] = UserData.Create(tm);

            // Expose .Net classes
            Table netTypes = new Table(session);
            addTypeAs(netTypes, typeof(bool), "bool");
            addTypeAs(netTypes, typeof(int), "int");
            addTypeAs(netTypes, typeof(float), "float");
            addTypeAs(netTypes, typeof(StringWrap), "string");
            addType(netTypes, typeof(DateTime));
            apiTable["net"] = netTypes;

            // Expose Unity classes
            Table unityTypes = new Table(session);
            addType(unityTypes, typeof(Time));
            // Resolution
            addType(unityTypes, typeof(Screen));
            addType(unityTypes, typeof(Resolution));
            addType(unityTypes, typeof(RefreshRate));
            // Input
            addType(unityTypes, typeof(Input));
            addType(unityTypes, typeof(Touch));
            // Math types
            addType(unityTypes, typeof(Mathf));
            addType(unityTypes, typeof(UnityEngine.Random));
            addType(unityTypes, typeof(Vector2));
            addType(unityTypes, typeof(Vector3));
            // Mesh generation
            addType(unityTypes, typeof(Vertex));
            // Styles
            addType(unityTypes, typeof(StyleBackground));
            addType(unityTypes, typeof(Background));
            addType(unityTypes, typeof(StyleColor));
            addType(unityTypes, typeof(Color));
            addType(unityTypes, typeof(Color32));
            addType(unityTypes, typeof(StyleFloat));
            addType(unityTypes, typeof(StyleInt));
            addType(unityTypes, typeof(StyleLength));
            addType(unityTypes, typeof(Length));
            addType(unityTypes, typeof(StyleTranslate));
            addType(unityTypes, typeof(Translate));
            addType(unityTypes, typeof(StyleRotate));
            addType(unityTypes, typeof(Rotate));
            addType(unityTypes, typeof(Angle));
            addType(unityTypes, typeof(StyleScale));
            addType(unityTypes, typeof(Scale));
            addType(unityTypes, typeof(StyleTransformOrigin));
            addType(unityTypes, typeof(TransformOrigin));
            // Types used by Painter2D
            addType(unityTypes, typeof(Painter2D));
            addType(unityTypes, typeof(VectorImage));
            addType(unityTypes, typeof(Gradient));
            addType(unityTypes, typeof(GradientColorKey));
            addType(unityTypes, typeof(GradientAlphaKey));

            // Expose Unity enums
            Table unityEnums = new Table(session);
            // Enums used by Options
            addType(unityEnums, typeof(FullScreenMode));
            // Enums used by Input
            addType(unityEnums, typeof(TouchPhase));
            // Enums used by styles
            addType(unityEnums, typeof(StyleKeyword));
            addType(unityEnums, typeof(LengthUnit));
            addType(unityEnums, typeof(AngleUnit));
            // Enums used by Painter2D
            addType(unityEnums, typeof(LineCap));
            addType(unityEnums, typeof(LineJoin));
            addType(unityEnums, typeof(FillRule));
            addType(unityEnums, typeof(ArcDirection));
            addType(unityEnums, typeof(ColorSpace));
            addType(unityEnums, typeof(GradientMode));
            // Enums used by events
            addType(unityEnums, typeof(PropagationPhase));
            addType(unityEnums, typeof(KeyCode));

            unityTypes["enum"] = unityEnums;
            apiTable["unity"] = unityTypes;

            return apiTable;
        }
    }

    public class ApiNotSupportedException : Exception { }
}