using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FantomLib
{
    /// <summary>
    /// Android Action Controller
    ///･Call the action to Android Native.
    ///(*) Note that depending on the action, there are cases where it can not be used depending on authority (security) , URI or file path.
    ///(*) In many cases it is better to use ContentInfo.fileUri (standard URI).
    /// (Action)
    /// https://developer.android.com/reference/android/content/Intent.html#ACTION_VIEW
    /// https://developer.android.com/reference/android/provider/Settings.html#ACTION_ACCESSIBILITY_SETTINGS
    /// 
    /// 
    ///・Android 実機でアクションを実行する。
    ///※アクションによっては権限（セキュリティ）やURI、ファイルのパスによって使えない場合がある。
    ///※ContentInfo.fileUri（標準的な URI）を使った方が良い場合が多い。
    /// (Action)
    /// https://developer.android.com/reference/android/content/Intent.html#ACTION_VIEW
    /// https://developer.android.com/reference/android/provider/Settings.html#ACTION_ACCESSIBILITY_SETTINGS
    /// </summary>
    public class AndroidActionController : MonoBehaviour
    {
        //Inspector Settings
        public string action = "android.intent.action.VIEW";

        [Serializable]
        public enum ActionType
        {
            ActionOnly = -1,    //No arguments
            URI = 0,            //Action to URI
            ExtraQuery,         //Use Extra and Query to action. 
            Chooser,            //Start action with Chooser
            UriWithMimeType,    //Action to URI with MIME type
        }
        public ActionType actionType = ActionType.URI;

        //Parameters to give to the action etc.
        public string title = "";
        public string uri = "";
        public string extra = "query";
        public string query = "keyword";
        public string mimetype = "text/plain";

        [Serializable]
        public class Extra
        {
            public string name;
            public string value;
        }
        public Extra[] addExtras;


#region Properties and Local values Section

        //Create arrays to be arguments of multiple parameters from addExtras.
        private void GetAddExtrasArrays(out string[] names, out string[] values, string extra = "", string query = "")
        {
            if (string.IsNullOrEmpty(extra))
            {
                names = addExtras.Select(e => e.name).ToArray();
                values = addExtras.Select(e => e.value).ToArray();
            }
            else
            {
                names = new string[addExtras.Length + 1];
                values = new string[addExtras.Length + 1];
                names[0] = extra;
                values[0] = query;
                for (int i = 0; i < addExtras.Length; i++)
                {
                    names[i + 1] = addExtras[i].name;
                    values[i + 1] = addExtras[i].value;
                }
            }
        }

        //Check empty etc.
        private void CheckForErrors()
        {
            if (string.IsNullOrEmpty(action))
                Debug.LogError("Action is empty.");

            switch (actionType)
            {
                case ActionType.ActionOnly:
                    break;
                case ActionType.URI:
                    if (string.IsNullOrEmpty(uri))
                        Debug.LogWarning("Uri is empty.");
                    break;
                case ActionType.ExtraQuery:
                    if (string.IsNullOrEmpty(extra))
                        Debug.LogWarning("Extra is empty.");
                    break;
                case ActionType.Chooser:
                    if (string.IsNullOrEmpty(mimetype))
                        Debug.LogWarning("MIME Type is empty.");
                    break;
                case ActionType.UriWithMimeType:
                    if (string.IsNullOrEmpty(uri))
                        Debug.LogWarning("Uri is empty.");
                    if (string.IsNullOrEmpty(mimetype))
                        Debug.LogWarning("MIME Type is empty.");
                    break;
            }
        }

#endregion

        // Use this for initialization
        private void Start()
        {
#if UNITY_EDITOR
            CheckForErrors();   //Check for fatal errors (Editor only).
#endif
        }

        // Update is called once per frame
        //private void Update()
        //{

        //}

        
        //Start the action to Android
        public void StartAction()
        {
#if UNITY_EDITOR
            Debug.Log("AndroidActionControlloer.StartAction called");
#elif UNITY_ANDROID
            switch (actionType)
            {
                case ActionType.ActionOnly:
                    if (addExtras.Length > 0)
                    {
                        string[] names; string[] values;
                        GetAddExtrasArrays(out names, out values);
                        AndroidPlugin.StartAction(action, names, values);
                    }
                    else
                        AndroidPlugin.StartAction(action);
                    break;

                case ActionType.URI:
                    if (addExtras.Length > 0)
                    {
                        string[] names; string[] values;
                        GetAddExtrasArrays(out names, out values);
                        AndroidPlugin.StartActionURI(action, uri, names, values);
                    }
                    else
                        AndroidPlugin.StartActionURI(action, uri);
                    break;

                case ActionType.ExtraQuery:
                    if (addExtras.Length > 0)
                    {
                        string[] names; string[] values;
                        GetAddExtrasArrays(out names, out values, extra, query);
                        AndroidPlugin.StartAction(action, names, values);
                    }
                    else
                        AndroidPlugin.StartAction(action, extra, query);
                    break;

                case ActionType.Chooser:
                    if (addExtras.Length > 0)
                    {
                        string[] names; string[] values;
                        GetAddExtrasArrays(out names, out values, extra, query);
                        AndroidPlugin.StartActionWithChooser(action, names, values, mimetype, title);
                    }
                    else
                        AndroidPlugin.StartActionWithChooser(action, extra, query, mimetype, title);
                    break;

                case ActionType.UriWithMimeType:
                    AndroidPlugin.StartActionURI(action, uri, mimetype);
                    break;
            }
#endif
        }

        //Start the action to URI (current value will be overwritten)
        public void StartActionURI(string uri)
        {
            if (actionType != ActionType.URI && actionType != ActionType.UriWithMimeType)
                return;

            this.uri = uri;
            StartAction();
        }

        //Start the action to URI with MIME type (current value will be overwritten)
        public void StartActionUriWithMimeType(string uri, string mimetype)
        {
            if (actionType != ActionType.UriWithMimeType)
                return;

            this.uri = uri;
            this.mimetype = mimetype;
            StartAction();
        }

        //Start the action to URI with Chooser (current value will be overwritten)
        public void StartActionWithChooser(string query)
        {
            if (actionType != ActionType.Chooser)
                return;

            this.query = query;
            StartAction();
        }

    }
}
