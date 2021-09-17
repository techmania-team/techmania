using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FantomLib
{
    /// <summary>
    /// Call the Android native plugin
    /// http://fantom1x.blog130.fc2.com/blog-entry-293.html
    /// http://fantom1x.blog130.fc2.com/blog-entry-273.html
    ///(*) "fantomPlugin.aar" is required 'Minimum API Level：Android 4.2 (API 17)' or higher.
    ///(*) When using text file reading / writing to storage, it is necessary to make it more than Android 4.4 (API 19).
    ///(*) In order to acquire the value of the sensor it is necessary to set it above the necessary API Level of each sensor.
    /// For details, refer to official document or comments such as sensor related method & constant values.
    /// https://developer.android.com/reference/android/hardware/Sensor.html#TYPE_ACCELEROMETER
    ///･When using Hardware Volume Control, Speech Recognize with dialog, Wifi Settings open, Bluetooth request enable,
    /// Text file read/write to External Storage, Gallery open, Media Scanner, Screen orientation change event, Sensor values, Confirm Device Credentials, QR Code Scanner,
    /// Rename "AndroidManifest-FullPlugin~.xml" to "AndroidManifest.xml".
    ///･Permission is necessary for AndroidManifest.xml (It is summarized in Permission_ReadMe.txt) depending on the function to use.
    /// https://developer.android.com/reference/android/Manifest.permission.html
    ///･Text to Speech is required the reading engine and voice data must be installed on the device.
    /// (Google Play)
    /// https://play.google.com/store/apps/details?id=com.google.android.tts
    /// https://play.google.com/store/apps/details?id=jp.kddilabs.n2tts  (Japanese)
    /// (Installation Text To Speech)
    /// http://fantom1x.blog130.fc2.com/blog-entry-275.html#fantomPlugin_TextToSpeech_install
    ///·If ZXing's QR Code Scanner application is not in the device, a dialog prompting installation will be displayed.
    /// (Google Play)
    /// https://play.google.com/store/apps/details?id=com.google.zxing.client.android
    /// (ZXing open source project)
    /// https://github.com/zxing/zxing
    /// ==========================================================
    ///･License of use library. etc
    /// This plugin includes deliverables distributed under the license of Apache License, Version 2.0.
    /// http://www.apache.org/licenses/LICENSE-2.0
    /// ZXing ("Zebra Crossing") open source project (google). [ver.3.3.2] (QR Code Scan)
    /// https://github.com/zxing/zxing
    /// ==========================================================
    /// 
    /// 
    /// Android のネイティブプラグインを呼び出すクラス
    /// http://fantom1x.blog130.fc2.com/blog-entry-293.html
    /// http://fantom1x.blog130.fc2.com/blog-entry-273.html
    ///※Assets/Plugins/Android/fantomPlugin.aar を置く。
    ///※Minimum API Level：Android 4.2 (API 17) 以上にする。
    ///※ストレージへのテキストファイル読み書きを利用する場合は、Android 4.4 (API 19) 以上にする必要がある。
    ///※ハードウェア音量キーのイベント取得、ダイアログ付きの音声認識、WIFIの設定を開く、Bluetooth接続要求（ダイアログ）、ストレージのテキストファイルの読み書き、
    ///  ギャラリーの画像パス取得、MediaScannerの更新機能、バッテリーステータスの取得、画面回転イベントの取得、デバイス認証、QRコードスキャナを利用する場合には、
    /// 「FullPluginOnUnityPlayerActivity」を「AndroidManifest.xml」で使う必要がある（「AndroidManifest-FullPlugin～.xml」をリネームして使う）。
    ///※利用する機能によっては AndroidManifest.xml にパーミッションが必要（Permission_ReadMe.txt にまとめてある）。
    /// https://developer.android.com/reference/android/Manifest.permission.html
    ///・テキスト読み上げを使用するには端末に音声データがインストールされている必要がある。
    /// (Google Play)
    /// https://play.google.com/store/apps/details?id=com.google.android.tts
    /// https://play.google.com/store/apps/details?id=jp.kddilabs.n2tts
    /// (テキスト読み上げのインストール)
    /// http://fantom1x.blog130.fc2.com/blog-entry-275.html#fantomPlugin_TextToSpeech_install
    ///・QRコード読み取りを利用するには端末に ZXing（googleのオープンソースプロジェクト）のQRコードスキャナアプリがインストールされている必要がある。
    ///  インストールされていない場合、インストールを促すダイアログが表示される（Google Play へ誘導される）。
    /// (Google Play)
    /// https://play.google.com/store/apps/details?id=com.google.zxing.client.android
    /// (ZXing オープンソースプロジェクト)
    /// https://github.com/zxing/zxing
    /// ==========================================================
    ///・使用ライブラリのライセンス等
    /// このプラグインには Apache License, Version 2.0 のライセンスで配布されている成果物を含んでいます。
    /// http://www.apache.org/licenses/LICENSE-2.0
    /// ZXing ("Zebra Crossing") open source project (google). [ver.3.3.2] (QR Code Scan)
    /// https://github.com/zxing/zxing
    /// ==========================================================
    /// </summary>
#if UNITY_ANDROID
    public static class AndroidPlugin
    {

        //Class full path of plug-in in Java
        public const string ANDROID_PACKAGE = "jp.fantom1x.plugin.android.fantomPlugin";
        public const string ANDROID_SYSTEM = ANDROID_PACKAGE + ".AndroidSystem";



        //==========================================================
        // etc functions

        /// <summary>
        /// Release of cashe etc.
        ///(*) When using "FullPluginOnUnityPlayerActivity", it is always invoked inside the plugin at the end of the application.
        ///
        /// すべての機能、キャッシュなどのリリース
        ///※「FullPluginOnUnityPlayerActivity」を利用しているときはアプリ終了時に必ずプラグイン内部で呼ばれる。
        /// </summary>
        public static void Release()
        {
            using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
            {
                androidSystem.CallStatic(
                    "release"
                );
            }

            HardKey.ReleaseCache();
        }




        /// <summary>
        /// Get API Level (Build.VERSION.SDK_INT) of the device
        /// https://developer.android.com/guide/topics/manifest/uses-sdk-element.html#ApiLevels
        /// https://developer.android.com/reference/android/os/Build.VERSION.html#SDK_INT
        ///
        /// 
        /// デバイスの API Level (Build.VERSION.SDK_INT) を取得する
        /// </summary>
        /// <returns>API Level</returns>
        public static int GetAPILevel()
        {
            using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
            {
                return androidSystem.CallStatic<int>(
                    "getBuildVersion"
                );
            }
        }



        /// <summary>
        /// Check if the application is installed
        ///･Application is specified by package name (= Returns whether the package name exists on the device).
        ///(*) You can not get packages that are not published to the Android system (always return false).
        /// 
        /// 
        /// アプリケーションがインストールされているを調べる
        ///・アプリケーションはパッケージ名で指定する（＝端末にパッケージ名が存在しているかを返す）。
        ///※Androidシステムに公開されてないパッケージは取得できない（常にfalseが返る）。
        /// </summary>
        /// <param name="packageName">Package name (Application ID)</param>
        /// <returns>true = exist</returns>
        public static bool IsExistApplication(string packageName)
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
                    {
                        return androidSystem.CallStatic<bool>(
                            "isExistPackage",
                            context,
                            packageName
                        );
                    }
                }
            }
        }



        /// <summary>
        /// Get the label name (localized name) of the application (only if set in the application)
        /// 
        /// アプリケーション名（ローカライズされた名前）を取得する（アプリで設定されてる場合のみ）
        /// </summary>
        /// <param name="packageName">Package name (Application ID)</param>
        /// <returns>Application name</returns>
        public static string GetApplicationName(string packageName)
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
                    {
                        return androidSystem.CallStatic<string>(
                            "getApplicationName",
                            context,
                            packageName
                        );
                    }
                }
            }
        }



        /// <summary>
        /// Get version code of the application (Internal number that always increments [integer value])
        /// https://developer.android.com/studio/publish/versioning.html#appversioning
        /// 
        /// アプリのバージョン番号（常にインクリメントする内部番号[整数値]）を取得する
        /// https://developer.android.com/studio/publish/versioning.html#appversioning
        /// </summary>
        /// <param name="packageName">Package name (Application ID)</param>
        /// <returns>Version Code / failure = 0</returns>
        public static int GetVersionCode(string packageName)
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
                    {
                        return androidSystem.CallStatic<int>(
                            "getVersionCode",
                            context,
                            packageName
                        );
                    }
                }
            }
        }



        /// <summary>
        /// Get the version name of the application (Character string used as the version [number] to be displayed to the user)
        /// https://developer.android.com/studio/publish/versioning.html#appversioning
        /// 
        /// アプリのバージョン名（ユーザーに表示するバージョン[番号]として使用される文字列）を取得する
        /// https://developer.android.com/studio/publish/versioning.html#appversioning
        /// </summary>
        /// <param name="packageName">Package name (Application ID)</param>
        /// <returns>Version Name / failure = ""(empty)</returns>
        public static string GetVersionName(string packageName)
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
                    {
                        return androidSystem.CallStatic<string>(
                            "getVersionName",
                            context,
                            packageName
                        );
                    }
                }
            }
        }




        //==========================================================
        //·Permissions used in fantomPlugin are as follows:
        // プラグインで利用するパーミッションは以下の通り：
        // android.permission.RECORD_AUDIO
        // android.permission.WRITE_EXTERNAL_STORAGE (or android.permission.READ_EXTERNAL_STORAGE : When read only)
        // android.permission.BLUETOOTH
        // android.permission.VIBRATE
        // android.permission.BODY_SENSORS
        //==========================================================

        /// <summary>
        /// Returns whether permission is granted.
        /// https://developer.android.com/reference/android/Manifest.permission.html
        ///·Use "Constant Value" in the developer manual for the permission string (eg: "android.permission.RECORD_AUDIO").
        /// 
        /// パーミッションが許可（付与）されているかどうかを返す
        ///・パーミッションの文字列はデベロッパーマニュアルの「Constant Value」を使う（例："android.permission.RECORD_AUDIO"）。
        /// https://developer.android.com/reference/android/Manifest.permission.html
        /// </summary>
        /// <param name="permission">permission string (eg: "android.permission.RECORD_AUDIO")</param>
        /// <returns>true = granted / false = denied (nothing)</returns>
        public static bool CheckPermission(string permission)
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
                    {
                        return androidSystem.CallStatic<bool>(
                            "checkPermission",
                            context,
                            permission
                        );
                    }
                }
            }
        }


        //==========================================================
        //(*) API 23 (Android 6.0) or higher
        //※API 23 (Android 6.0) 以上のみ。
        //==========================================================
        /// <summary>
        /// Check permission, if not allowed (granted), give explanation of the rationale dialog and request
        ///·Result is sent to the callback with granted ("PERMISSION_GRANTED") or denied ("PERMISSION_DENIED").
        ///·Requestable permissions need to be written in "AndroidManifest.xml" in advance.
        ///·There is no request on the device before API 23 (Android 6.0), always callback only the result.
        ///·The explanation dialog of the rationale will not always appear if the user checks "Don't ask again" on request.
        ///·Use "Constant Value" in the developer manual for the permission string (eg: "android.permission.WRITE_EXTERNAL_STORAGE").
        /// https://developer.android.com/reference/android/Manifest.permission.html
        /// 
        /// パーミッションをチェックし、許可（付与）されていない場合、根拠の説明と要求ダイアログを出す
        ///・結果はコールバックに許可（"PERMISSION_GRANTED"）または拒否（"PERMISSION_DENIED"）で送られてくる。
        ///・要求できるパーミッションはあらかじめ「AndroidManifest.xml」に書かれている必要がある。
        ///・API 23 (Android 6.0) より前のデバイスでは要求は出ず、常に結果のみをコールバックする。
        ///・根拠の説明ダイアログは、要求のときユーザーが「今後表示しない」をチェックすると常に出なくなる。
        ///・パーミッションの文字列はデベロッパーマニュアルの「Constant Value」を使う（例："android.permission.WRITE_EXTERNAL_STORAGE"）。
        /// https://developer.android.com/reference/android/Manifest.permission.html
        /// </summary>
        /// <param name="permission">permission string (eg: "android.permission.WRITE_EXTERNAL_STORAGE")</param>
        /// <param name="title">Rationale dialog title</param>
        /// <param name="message">Rationale dialog message</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="callbackMethod">Method name to callback (it is in GameObject)</param>
        /// <param name="style">Style applied to rationale dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void CheckPermissionAndRequest(string permission, string title, string message, 
            string callbackGameObject, string callbackMethod, string style = "")
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "checkPermissionAndRequest",
                    context,
                    permission,
                    title,
                    message,
                    callbackGameObject,
                    callbackMethod,
                    style
                );
            }));
        }




        //==========================================================
        // Anrdoid Widget etc.
        // Anrdoid ウィジェットなど
        //==========================================================

        /// <summary>
        /// Call Android Toast
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_Toast
        /// (Toast)
        /// https://developer.android.com/reference/android/widget/Toast.html#LENGTH_LONG
        /// 
        /// Android の Toast を使用する
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_Toast
        /// (Toast)
        /// https://developer.android.com/reference/android/widget/Toast.html#LENGTH_LONG
        /// </summary>
        /// <param name="message">Message string</param>
        /// <param name="longDuration">Display length : true = 3.5s / false = 2.0s</param>
        public static void ShowToast(string message, bool longDuration = false)
        {
            if (string.IsNullOrEmpty(message))
                return;

            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "showToast",
                    context,
                    message,
                    longDuration ? 1 : 0
                );
            }));
        }



        /// <summary>
        /// Cancel if there is "Android Toast" being displayed. Ignored when not displayed.
        ///･Even when "AndroidPlugin.Release()" is called, it is executed on the native side.
        ///
        /// 表示している Toast がある場合中断する。無いときは何もしない（エラーにはならない）
        ///・AndroidPlugin.Release() が呼ばれたときも、ネイティブ側で実行される。
        /// </summary>
        public static void CancelToast()
        {
            using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
            {
                androidSystem.CallStatic(
                    "cancelToast"
                );
            }
        }



        //==========================================================
        // Android Dialogs
        // ダイアログ
        // https://developer.android.com/guide/topics/ui/dialogs.html
        // (AlertDialog)
        // https://developer.android.com/reference/android/app/AlertDialog.html
        //==========================================================


        /// <summary>
        /// Call Android "Yes/No" Dialog
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_AlertDialogYN
        ///･"Yes" -> yesValue / "No" -> noValue is returned as the argument of the callback.
        ///･When neither is pressed (clicking outside the dialog -> back to application), 
        /// nothing is returned (not doing anything).
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// "Yes"/"No" ダイアログ：Android の AlertDialog を使用する
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_AlertDialogYN
        ///・「Yes」→ yesValue / 「No」→ noValue がコールバックの引数で返される。
        ///・どちらも押されない（ダイアログ外クリック→元の画面に戻った）ときは何も返さない（何もしない）。
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="message">Message string</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="callbackMethod">Method name to callback (it is in GameObject)</param>
        /// <param name="yesCaption">String of "Yes" button</param>
        /// <param name="yesValue">Return value when "Yes" button</param>
        /// <param name="noCaption">String of "No" button</param>
        /// <param name="noValue">Return value when "No" button</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowDialog(string title, string message, string callbackGameObject, string callbackMethod,
            string yesCaption, string yesValue, string noCaption, string noValue, string style = "")
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "showDialog",
                    context,
                    title,
                    message,
                    callbackGameObject,
                    callbackMethod,
                    yesCaption,
                    yesValue,
                    noCaption,
                    noValue,
                    style
                );
            }));
        }


        /// <summary>
        /// Call Android "OK" Dialog
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_AlertDialogOK
        ///･When pressed the "OK" button or clicked outside the dialog (-> back to application) 
        /// return the same value (resultValue).
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// "OK" ダイアログ：Android の AlertDialog を使用する
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_AlertDialogOK
        ///・ボタン押下、ダイアログ外クリック（元の画面に戻った）ともに同じ戻値（resultValue）を返す。
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="message">Message string</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="callbackMethod">Method name to callback (it is in GameObject)</param>
        /// <param name="okCaption">String of "OK" button</param>
        /// <param name="resultValue">Return value when "OK" button or closed dialog</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowDialog(string title, string message, string callbackGameObject, string callbackMethod,
            string okCaption, string resultValue, string style = "")
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "showDialog",
                    context,
                    title,
                    message,
                    callbackGameObject,
                    callbackMethod,
                    okCaption,
                    resultValue,
                    style
                );
            }));
        }



        /// <summary>
        /// Call Android "Yes/No" Dialog with CheckBox
        /// http://fantom1x.blog130.fc2.com/blog-entry-279.html#fantomPlugin_AlertDialogYNwithCheck
        ///･The check status is returned as a callback argument with ", CHECKED_TRUE" or ", CHECKED_FALSE"
        /// concatenated with the return value (yesValue / noValue).
        ///･When neither is pressed (clicking outside the dialog -> back to application),
        /// nothing is returned (not doing anything).
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// チェックボックス付きの "Yes"/"No" ダイアログ：Android の AlertDialog を使用する
        /// http://fantom1x.blog130.fc2.com/blog-entry-279.html#fantomPlugin_AlertDialogYNwithCheck
        ///・チェック状態は戻り値（yesValue/noValue）に ", CHECKED_TRUE" または ", CHECKED_FALSE" が結合されてコールバックの引数で返される。
        ///・どちらも押されない（ダイアログ外クリック→元の画面に戻った）ときは何も返さない（何もしない）。
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="message">Message string</param>
        /// <param name="checkBoxText">Text string of check box</param>
        /// <param name="checkBoxTextColor">Text color of check box (0 = not specified: Color format is int32 (AARRGGBB: Android-Java))</param>
        /// <param name="defaultChecked">Initial state of check box (true = On / false = Off)</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="callbackMethod">Method name to callback (it is in GameObject)</param>
        /// <param name="yesCaption">String of "Yes" button</param>
        /// <param name="yesValue">Return value when "Yes" button</param>
        /// <param name="noCaption">String of "No" button</param>
        /// <param name="noValue">Return value when "No" button</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowDialogWithCheckBox(string title, string message, 
            string checkBoxText, int checkBoxTextColor, bool defaultChecked,
            string callbackGameObject, string callbackMethod,
            string yesCaption, string yesValue, string noCaption, string noValue, string style = "")
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "showDialogWithCheckBox",
                    context,
                    title,
                    message,
                    checkBoxText,
                    checkBoxTextColor,
                    defaultChecked,
                    callbackGameObject,
                    callbackMethod,
                    yesCaption,
                    yesValue,
                    noCaption,
                    noValue,
                    style
                );
            }));
        }


        /// <summary>
        /// Call Android "Yes/No" Dialog with CheckBox (Unity.Color overload)
        /// http://fantom1x.blog130.fc2.com/blog-entry-279.html#fantomPlugin_AlertDialogYNwithCheck
        ///･The check status is returned as a callback argument with ", CHECKED_TRUE" or ", CHECKED_FALSE"
        /// concatenated with the return value (yesValue / noValue).
        ///･When neither is pressed (clicking outside the dialog -> back to application),
        /// nothing is returned (not doing anything).
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// チェックボックス付きの "Yes"/"No" ダイアログ：Android の AlertDialog を使用する（Unity の Color 形式のオーバーロード）
        /// http://fantom1x.blog130.fc2.com/blog-entry-279.html#fantomPlugin_AlertDialogYNwithCheck
        ///・チェック状態は戻り値（yesValue/noValue）に ", CHECKED_TRUE" または ", CHECKED_FALSE" が結合されてコールバックの引数で返される。
        ///・どちらも押されない（ダイアログ外クリック→元の画面に戻った）ときは何も返さない（何もしない）。
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="message">Message string</param>
        /// <param name="checkBoxText">Text string of check box</param>
        /// <param name="checkBoxTextColor">Text color of check box (Color.clear = not specified: Not clear color)</param>
        /// <param name="defaultChecked">Initial state of check box (true = On / false = Off)</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="callbackMethod">Method name to callback (it is in GameObject)</param>
        /// <param name="yesCaption">String of "Yes" button</param>
        /// <param name="yesValue">Return value when "Yes" button</param>
        /// <param name="noCaption">String of "No" button</param>
        /// <param name="noValue">Return value when "No" button</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowDialogWithCheckBox(string title, string message,
            string checkBoxText, Color checkBoxTextColor, bool defaultChecked,
            string callbackGameObject, string callbackMethod,
            string yesCaption, string yesValue, string noCaption, string noValue, string style = "")
        {
            ShowDialogWithCheckBox(title, message, checkBoxText, checkBoxTextColor.ToIntARGB(), defaultChecked,
                callbackGameObject, callbackMethod, yesCaption, yesValue, noCaption, noValue, style);
        }



        /// <summary>
        /// Call Android "OK" Dialog with CheckBox
        /// http://fantom1x.blog130.fc2.com/blog-entry-279.html#fantomPlugin_AlertDialogOKwithCheck
        ///･The check status is returned as a callback argument with ", CHECKED_TRUE" or ", CHECKED_FALSE"
        /// concatenated with the return value (resultValue).
        ///･When pressed the "OK" button or clicked outside the dialog (-> back to application) 
        /// return the same value (resultValue).
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// チェックボックス付きの "OK" ダイアログ：Android の AlertDialog を使用する
        /// http://fantom1x.blog130.fc2.com/blog-entry-279.html#fantomPlugin_AlertDialogOKwithCheck
        ///・チェック状態は戻り値（resultValue）に ", CHECKED_TRUE"または", CHECKED_FALSE"が結合されてコールバックの引数で返される。
        ///・ボタン押下、ダイアログ外クリック（元の画面に戻った）共に同じ戻値を返す。
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="message">Message string</param>
        /// <param name="checkBoxText">Text string of check box</param>
        /// <param name="checkBoxTextColor">Text color of check box (0 = not specified: Color format is int32 (AARRGGBB: Android-Java))</param>
        /// <param name="defaultChecked">Initial state of check box (true = On / false = Off)</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="callbackMethod">Method name to callback (it is in GameObject)</param>
        /// <param name="okCaption">String of "OK" button</param>
        /// <param name="resultValue">Return value when "OK" button or the dialog is closed</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowDialogWithCheckBox(string title, string message, 
            string checkBoxText, int checkBoxTextColor, bool defaultChecked,
            string callbackGameObject, string callbackMethod,
            string okCaption, string resultValue, string style = "")
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "showDialogWithCheckBox",
                    context,
                    title,
                    message,
                    checkBoxText,
                    checkBoxTextColor,
                    defaultChecked,
                    callbackGameObject,
                    callbackMethod,
                    okCaption,
                    resultValue,
                    style
                );
            }));
        }


        /// <summary>
        /// Call Android "OK" Dialog with CheckBox (Unity.Color overload)
        /// http://fantom1x.blog130.fc2.com/blog-entry-279.html#fantomPlugin_AlertDialogOKwithCheck
        ///･The check status is returned as a callback argument with ", CHECKED_TRUE" or ", CHECKED_FALSE"
        /// concatenated with the return value (resultValue).
        ///･When pressed the "OK" button or clicked outside the dialog (-> back to application) 
        /// return the same value (resultValue).
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// チェックボックス付きの "OK" ダイアログ：Android の AlertDialog を使用する（Unity の Color 形式のオーバーロード）
        /// http://fantom1x.blog130.fc2.com/blog-entry-279.html#fantomPlugin_AlertDialogOKwithCheck
        ///・チェック状態は戻り値（resultValue）に ", CHECKED_TRUE"または", CHECKED_FALSE"が結合されてコールバックの引数で返される。
        ///・ボタン押下、ダイアログ外クリック（元の画面に戻った）共に同じ戻値を返す。
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="message">Message string</param>
        /// <param name="checkBoxText">Text string of check box</param>
        /// <param name="checkBoxTextColor">Text color of check box (Color.clear = not specified: Not clear color)</param>
        /// <param name="defaultChecked">Initial state of check box (true = On / false = Off)</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="callbackMethod">Method name to callback (it is in GameObject)</param>
        /// <param name="okCaption">String of "OK" button</param>
        /// <param name="resultValue">Return value when "OK" button or closed dialog</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowDialogWithCheckBox(string title, string message,
            string checkBoxText, Color checkBoxTextColor, bool defaultChecked,
            string callbackGameObject, string callbackMethod,
            string okCaption, string resultValue, string style = "")
        {
            ShowDialogWithCheckBox(title, message, checkBoxText, checkBoxTextColor.ToIntARGB(), defaultChecked,
                callbackGameObject, callbackMethod, okCaption, resultValue, style);
        }



        //==========================================================
        // Select Dialog (selection list)

        /// <summary>
        /// Call Android Selection list Dialog (Return index or items string)
        /// http://fantom1x.blog130.fc2.com/blog-entry-274.html#fantomPlugin_SelectDialog
        ///･There is no confirmation button.
        ///･When "OK", the string of the choice (items) is returned as it is as the argument of the callback
        /// (or return index ([*]string type) with resultIsIndex=true).
        ///･When clicked outside the dialog (-> back to application) return nothing.
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// 選択リストダイアログ：Android の AlertDialog を使用する（アイテムの文字列 または インデクスを返す）
        /// http://fantom1x.blog130.fc2.com/blog-entry-274.html#fantomPlugin_SelectDialog
        ///・選択肢の文字列がそのままコールバックの引数で返される（resultIsIndex=true でインデクス(※文字列型)で返す）。
        ///・何も押されない（ダイアログ外クリック→元の画面に戻った）ときは何も返さない（何もしない）。
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="items">Choice strings (Array)</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="callbackMethod">Method name to callback (it is in GameObject)</param>
        /// <param name="resultIsIndex">Flag to set return value as index ([*]string type)</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowSelectDialog(string title, string[] items, 
            string callbackGameObject, string callbackMethod, bool resultIsIndex = false, string style = "")
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "showSelectDialog",
                    context,
                    title,
                    items,
                    callbackGameObject,
                    callbackMethod,
                    resultIsIndex,
                    style
                );
            }));
        }


        //(*) Return result value for each item
        //※アイテムごとの結果文字列を返す
        /// <summary>
        /// Call Android Selection list Dialog (Return result value for each item)
        /// http://fantom1x.blog130.fc2.com/blog-entry-274.html#fantomPlugin_SelectDialog
        ///･There is no confirmation button.
        ///･An element of the result array (resultValues) corresponding to the sequence of choices (items)
        /// is returned as the argument of the callback.
        ///･When clicked outside the dialog (-> back to application) return nothing.
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// 選択リストダイアログ：Android の AlertDialog を使用する（アイテムごとの結果文字列を返す）
        /// http://fantom1x.blog130.fc2.com/blog-entry-274.html#fantomPlugin_SelectDialog
        ///・選択肢の並びに対応した結果配列（resultValues）の要素がコールバックの引数で返される。
        ///・何も押されない（ダイアログ外クリック→元の画面に戻った）ときは何も返さない（何もしない）。
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="items">Choice strings (Array)</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="callbackMethod">Method name to callback (it is in GameObject)</param>
        /// <param name="resultValues">The element of the selected index (items) becomes the return value</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowSelectDialog(string title, string[] items, 
            string callbackGameObject, string callbackMethod, string[] resultValues, string style = "")
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "showSelectDialog",
                    context,
                    title,
                    items,
                    callbackGameObject,
                    callbackMethod,
                    resultValues,
                    style
                );
            }));
        }



        //==========================================================
        // Single Choice Dialog

        /// <summary>
        /// Call Android Single Choice Dialog (Return index or items string)
        /// http://fantom1x.blog130.fc2.com/blog-entry-274.html#fantomPlugin_SingleChoiceDialog
        ///･When "OK", the string of the choice (items) is returned as it is as the argument of the callback
        /// (or return index ([*]string type) with resultIsIndex=true).
        ///·When canceled without "OK" or closing, the following character string is returned in the cancel callback (cancelCallbackMethod).
        ///　CANCEL_DIALOG : Cancel button pressed
        ///　CLOSE_DIALOG : It was closed without "OK" (tap outside the dialog, erase it by pressing the back key etc.)
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// 単一選択肢ダイアログ：Android の AlertDialog を使用する（アイテムの文字列 または インデクスを返す）
        /// http://fantom1x.blog130.fc2.com/blog-entry-274.html#fantomPlugin_SingleChoiceDialog
        ///・ShowSelectDialog() と基本的に変わらないがデフォルトを持ち、"OK/Cancel" ボタンがある。
        ///・"OK" のとき、選択肢の文字列がそのままコールバックの引数で返される（resultIsIndex=true でインデクス(※文字列型)で返す）。
        ///・キャンセルまたは「OK」せずに閉じられたときはキャンセルコールバック（cancelCallbackMethod）に以下の文字列が返る。
        ///　CANCEL_DIALOG : キャンセルボタンが押された
        ///　CLOSE_DIALOG : 「OK」せずに閉じられた（ダイアログ外をタップ、バックキーを押して消した等）
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="items">Choice strings (Array)</param>
        /// <param name="checkedItem">Initial value of index (0~n-1)</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="resultCallbackMethod">Method name to callback (it is in GameObject)</param>
        /// <param name="changeCallbackMethod">Method name to callback of value chaged (it is in GameObject)</param>
        /// <param name="cancelCallbackMethod">Method name to callback when canceled (it is in GameObject)</param>
        /// <param name="resultIsIndex">Flag to set return value as index ([*]string type)</param>
        /// <param name="okCaption">String of "OK" button</param>
        /// <param name="cancelCaption">String of "Cancel" button</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowSingleChoiceDialog(string title, string[] items, int checkedItem,
            string callbackGameObject, string resultCallbackMethod, string changeCallbackMethod, string cancelCallbackMethod,
            bool resultIsIndex, string okCaption = "OK", string cancelCaption = "Cancel", string style = "")
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "showSingleChoiceDialog",
                    context,
                    title,
                    items,
                    checkedItem,
                    callbackGameObject,
                    resultCallbackMethod,
                    changeCallbackMethod,
                    cancelCallbackMethod,
                    resultIsIndex,
                    okCaption,
                    cancelCaption,
                    style
                );
            }));
        }

        //(*) No cancel callback overload
        //※キャンセルコールバック無し オーバーロード
        /// <summary>
        /// Call Android Single Choice Dialog (Return index or items string)
        /// http://fantom1x.blog130.fc2.com/blog-entry-274.html#fantomPlugin_SingleChoiceDialog
        ///･When "OK", the string of the choice (items) is returned as it is as the argument of the callback
        /// (or return index ([*]string type) with resultIsIndex=true).
        ///･When "Cancel", or clicked outside the dialog (-> back to application) return nothing.
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// 単一選択肢ダイアログ：Android の AlertDialog を使用する（アイテムの文字列 または インデクスを返す）
        /// http://fantom1x.blog130.fc2.com/blog-entry-274.html#fantomPlugin_SingleChoiceDialog
        ///・ShowSelectDialog() と基本的に変わらないがデフォルトを持ち、"OK/Cancel" ボタンがある。
        ///・"OK" のとき、選択肢の文字列がそのままコールバックの引数で返される（resultIsIndex=true でインデクス(※文字列型)で返す）。
        ///・"Cancel" または 何も押されない（ダイアログ外クリック→元の画面に戻った）ときは何も返さない（何もしない）。
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="items">Choice strings (Array)</param>
        /// <param name="checkedItem">Initial value of index (0~n-1)</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="resultCallbackMethod">Method name to callback (it is in GameObject)</param>
        /// <param name="changeCallbackMethod">Method name to callback of value chaged (it is in GameObject)</param>
        /// <param name="resultIsIndex">Flag to set return value as index ([*]string type)</param>
        /// <param name="okCaption">String of "OK" button</param>
        /// <param name="cancelCaption">String of "Cancel" button</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowSingleChoiceDialog(string title, string[] items, int checkedItem,
            string callbackGameObject, string resultCallbackMethod, string changeCallbackMethod,
            bool resultIsIndex, string okCaption = "OK", string cancelCaption = "Cancel", string style = "")
        {
            ShowSingleChoiceDialog(title, items, checkedItem, 
                callbackGameObject, resultCallbackMethod, changeCallbackMethod, "",
                resultIsIndex, okCaption, cancelCaption, style);
        }

        //(*) Argument omission overload
        //※引数省略 オーバーロード
        /// <summary>
        /// Call Android Single Choice Dialog (Return index or items string)
        /// http://fantom1x.blog130.fc2.com/blog-entry-274.html#fantomPlugin_SingleChoiceDialog
        ///･When "OK", the string of the choice (items) is returned as it is as the argument of the callback
        /// (or return index ([*]string type) with resultIsIndex=true).
        ///･When "Cancel", or clicked outside the dialog (-> back to application) return nothing.
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// 単一選択肢ダイアログ：Android の AlertDialog を使用する（アイテムの文字列 または インデクスを返す）
        /// http://fantom1x.blog130.fc2.com/blog-entry-274.html#fantomPlugin_SingleChoiceDialog
        ///・ShowSelectDialog() と基本的に変わらないがデフォルトを持ち、"OK/Cancel" ボタンがある。
        ///・"OK" のとき、選択肢の文字列がそのままコールバックの引数で返される（resultIsIndex=true でインデクス(※文字列型)で返す）。
        ///・"Cancel" または 何も押されない（ダイアログ外クリック→元の画面に戻った）ときは何も返さない（何もしない）。
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="items">Choice strings (Array)</param>
        /// <param name="checkedItem">Initial value of index (0~n-1)</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="callbackMethod">Method name to callback (it is in GameObject)</param>
        /// <param name="resultIsIndex">Flag to set return value as index ([*]string type)</param>
        /// <param name="okCaption">String of "OK" button</param>
        /// <param name="cancelCaption">String of "Cancel" button</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowSingleChoiceDialog(string title, string[] items, int checkedItem,
            string callbackGameObject, string callbackMethod, bool resultIsIndex = false,
            string okCaption = "OK", string cancelCaption = "Cancel", string style = "")
        {
            ShowSingleChoiceDialog(title, items, checkedItem, 
                callbackGameObject, callbackMethod, "", "",
                resultIsIndex, okCaption, cancelCaption, style);
        }


        //(*) Return result value for each item
        //※アイテムごとの結果文字列を返す
        /// <summary>
        /// Call Android Single Choice Dialog (Return result value for each item)
        /// http://fantom1x.blog130.fc2.com/blog-entry-274.html#fantomPlugin_SingleChoiceDialog
        ///･When "OK", the element of the result array (resultValues) corresponding to the sequence of choices (items)
        /// is returned as the argument of the callback.
        ///·When canceled without "OK" or closing, the following character string is returned in the cancel callback (cancelCallbackMethod).
        ///　CANCEL_DIALOG : Cancel button pressed
        ///　CLOSE_DIALOG : It was closed without "OK" (tap outside the dialog, erase it by pressing the back key etc.)
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// 単一選択肢ダイアログ：Android の AlertDialog を使用する（アイテムごとの結果文字列を返す）
        /// http://fantom1x.blog130.fc2.com/blog-entry-274.html#fantomPlugin_SingleChoiceDialog
        ///・ShowSelectDialog() と基本的に変わらないがデフォルトを持ち、"OK/Cancel" ボタンがある。
        ///・"OK" のとき、選択肢の並びに対応した結果配列（resultValues）の要素がコールバックの引数で返される。
        ///・キャンセルまたは「OK」せずに閉じられたときはキャンセルコールバック（cancelCallbackMethod）に以下の文字列が返る。
        ///　CANCEL_DIALOG : キャンセルボタンが押された
        ///　CLOSE_DIALOG : 「OK」せずに閉じられた（ダイアログ外をタップ、バックキーを押して消した等）
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="items">Choice strings (Array)</param>
        /// <param name="checkedItem">Initial value of index (0~n-1)</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="resultCallbackMethod">Method name to callback (it is in GameObject)</param>
        /// <param name="changeCallbackMethod">Method name to callback of value chaged (it is in GameObject)</param>
        /// <param name="cancelCallbackMethod">Method name to callback when canceled (it is in GameObject)</param>
        /// <param name="resultValues">The element of the selected index (items) becomes the return value</param>
        /// <param name="okCaption">String of "OK" button</param>
        /// <param name="cancelCaption">String of "Cancel" button</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowSingleChoiceDialog(string title, string[] items, int checkedItem,
            string callbackGameObject, string resultCallbackMethod, string changeCallbackMethod, string cancelCallbackMethod,
            string[] resultValues, string okCaption = "OK", string cancelCaption = "Cancel", string style = "")
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "showSingleChoiceDialog",
                    context,
                    title,
                    items,
                    checkedItem,
                    callbackGameObject,
                    resultCallbackMethod,
                    changeCallbackMethod,
                    cancelCallbackMethod,
                    resultValues,
                    okCaption,
                    cancelCaption,
                    style
                );
            }));
        }

        //(*) Return result value for each item
        //(*) No cancel callback overload
        //※アイテムごとの結果文字列を返す
        //※キャンセルコールバック無し オーバーロード
        /// <summary>
        /// Call Android Single Choice Dialog (Return result value for each item)
        /// http://fantom1x.blog130.fc2.com/blog-entry-274.html#fantomPlugin_SingleChoiceDialog
        ///･When "OK", the element of the result array (resultValues) corresponding to the sequence of choices (items)
        /// is returned as the argument of the callback.
        ///･When "Cancel", or clicked outside the dialog (-> back to application) return nothing.
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// 単一選択肢ダイアログ：Android の AlertDialog を使用する（アイテムごとの結果文字列を返す）
        /// http://fantom1x.blog130.fc2.com/blog-entry-274.html#fantomPlugin_SingleChoiceDialog
        ///・ShowSelectDialog() と基本的に変わらないがデフォルトを持ち、"OK/Cancel" ボタンがある。
        ///・"OK" のとき、選択肢の並びに対応した結果配列（resultValues）の要素がコールバックの引数で返される。
        ///・"Cancel" または 何も押されない（ダイアログ外クリック→元の画面に戻った）ときは何も返さない（何もしない）。
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="items">Choice strings (Array)</param>
        /// <param name="checkedItem">Initial value of index (0~n-1)</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="resultCallbackMethod">Method name to callback (it is in GameObject)</param>
        /// <param name="changeCallbackMethod">Method name to callback of value chaged (it is in GameObject)</param>
        /// <param name="resultValues">The element of the selected index (items) becomes the return value</param>
        /// <param name="okCaption">String of "OK" button</param>
        /// <param name="cancelCaption">String of "Cancel" button</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowSingleChoiceDialog(string title, string[] items, int checkedItem,
            string callbackGameObject, string resultCallbackMethod, string changeCallbackMethod, 
            string[] resultValues, string okCaption = "OK", string cancelCaption = "Cancel", string style = "")
        {
            ShowSingleChoiceDialog(title, items, checkedItem, 
                callbackGameObject, resultCallbackMethod, changeCallbackMethod, "", 
                resultValues, okCaption, cancelCaption, style);
        }

        //(*) Return result value for each item
        //(*) Argument omission overload
        //※アイテムごとの結果文字列を返す
        //※引数省略 オーバーロード
        /// <summary>
        /// Call Android Single Choice Dialog (Return result value for each item)
        /// http://fantom1x.blog130.fc2.com/blog-entry-274.html#fantomPlugin_SingleChoiceDialog
        ///･When "OK", the element of the result array (resultValues) corresponding to the sequence of choices (items)
        /// is returned as the argument of the callback.
        ///･When "Cancel", or clicked outside the dialog (-> back to application) return nothing.
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// 単一選択肢ダイアログ：Android の AlertDialog を使用する（アイテムごとの結果文字列を返す）
        /// http://fantom1x.blog130.fc2.com/blog-entry-274.html#fantomPlugin_SingleChoiceDialog
        ///・ShowSelectDialog() と基本的に変わらないがデフォルトを持ち、"OK/Cancel" ボタンがある。
        ///・"OK" のとき、選択肢の並びに対応した結果配列（resultValues）の要素がコールバックの引数で返される。
        ///・"Cancel" または 何も押されない（ダイアログ外クリック→元の画面に戻った）ときは何も返さない（何もしない）。
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="items">Choice strings (Array)</param>
        /// <param name="checkedItem">Initial value of index (0~n-1)</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="callbackMethod">Method name to callback (it is in GameObject)</param>
        /// <param name="resultValues">The element of the selected index (items) becomes the return value</param>
        /// <param name="okCaption">String of "OK" button</param>
        /// <param name="cancelCaption">String of "Cancel" button</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowSingleChoiceDialog(string title, string[] items, int checkedItem,
            string callbackGameObject, string callbackMethod, string[] resultValues,
            string okCaption = "OK", string cancelCaption = "Cancel", string style = "")
        {
            ShowSingleChoiceDialog(title, items, checkedItem, 
                callbackGameObject, callbackMethod, "", "", 
                resultValues, okCaption, cancelCaption, style);
        }



        //==========================================================
        // Multi Choice Dialog

        /// <summary>
        /// Call Android Multi Choice Dialog (Return index or items string)
        /// http://fantom1x.blog130.fc2.com/blog-entry-274.html#fantomPlugin_MultiChoiceDialog
        ///･Return only those checked from multiple choices.
        ///･When "OK", the string of the choice is concatenated with a line feed ("\n") and returned as the argument of the callback
        /// (or return index ([*]string type) with resultIsIndex=true).
        ///·When canceled without "OK" or closing, the following character string is returned in the cancel callback (cancelCallbackMethod).
        ///　CANCEL_DIALOG : Cancel button pressed
        ///　CLOSE_DIALOG : It was closed without "OK" (tap outside the dialog, erase it by pressing the back key etc.)
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// 複数選択肢ダイアログ：Android の AlertDialog を使用する（アイテムの文字列 または インデクスを返す）
        /// http://fantom1x.blog130.fc2.com/blog-entry-274.html#fantomPlugin_MultiChoiceDialog
        ///・複数の選択肢の中からチェックされているものだけを返すダイアログ。スイッチダイアログ（ShowSwitchDialog()）と異なり、オフになっているものは返ってこない。
        ///・"OK" のとき、選択肢の文字列が改行("\n")で連結されてコールバックの引数で返される（resultIsIndex=true でインデクス(※文字列型)で返す）。
        ///・キャンセルまたは「OK」せずに閉じられたときはキャンセルコールバック（cancelCallbackMethod）に以下の文字列が返る。
        ///　CANCEL_DIALOG : キャンセルボタンが押された
        ///　CLOSE_DIALOG : 「OK」せずに閉じられた（ダイアログ外をタップ、バックキーを押して消した等）
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="items">Choice strings (Array)</param>
        /// <param name="checkedItems">Initial state of checked (Array) (null = nothing)</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="callbackMethod">Method name to callback (it is in GameObject)</param>
        /// <param name="changeCallbackMethod">Method name to callback of value chaged (it is in GameObject)</param>
        /// <param name="cancelCallbackMethod">Method name to callback when canceled (it is in GameObject)</param>
        /// <param name="resultIsIndex">Flag to set return value as index ([*]string type)</param>
        /// <param name="okCaption">String of "OK" button</param>
        /// <param name="cancelCaption">String of "Cancel" button</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowMultiChoiceDialog(string title, string[] items, bool[] checkedItems,
            string callbackGameObject, string callbackMethod, string changeCallbackMethod, string cancelCallbackMethod,
            bool resultIsIndex, string okCaption = "OK", string cancelCaption = "Cancel", string style = "")
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "showMultiChoiceDialog",
                    context,
                    title,
                    items,
                    checkedItems,
                    callbackGameObject,
                    callbackMethod,
                    changeCallbackMethod,
                    cancelCallbackMethod,
                    resultIsIndex,
                    okCaption,
                    cancelCaption,
                    style
                );
            }));
        }

        //(*) No cancel callback overload
        //※キャンセルコールバック無し オーバーロード
        /// <summary>
        /// Call Android Multi Choice Dialog (Return index or items string)
        /// http://fantom1x.blog130.fc2.com/blog-entry-274.html#fantomPlugin_MultiChoiceDialog
        ///･Return only those checked from multiple choices.
        ///･When "OK", the string of the choice is concatenated with a line feed ("\n") and returned as the argument of the callback
        /// (or return index ([*]string type) with resultIsIndex=true).
        ///･When "Cancel", or clicked outside the dialog (-> back to application) return nothing.
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// 複数選択肢ダイアログ：Android の AlertDialog を使用する（アイテムの文字列 または インデクスを返す）
        /// http://fantom1x.blog130.fc2.com/blog-entry-274.html#fantomPlugin_MultiChoiceDialog
        ///・複数の選択肢の中からチェックされているものだけを返すダイアログ。スイッチダイアログ（ShowSwitchDialog()）と異なり、オフになっているものは返ってこない。
        ///・"OK" のとき、選択肢の文字列が改行("\n")で連結されてコールバックの引数で返される（resultIsIndex=true でインデクス(※文字列型)で返す）。
        ///・"Cancel" または 何も押されない（ダイアログ外クリック→元の画面に戻った）ときは何も返さない（何もしない）。
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="items">Choice strings (Array)</param>
        /// <param name="checkedItems">Initial state of checked (Array) (null = nothing)</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="callbackMethod">Method name to callback (it is in GameObject)</param>
        /// <param name="changeCallbackMethod">Method name to callback of value chaged (it is in GameObject)</param>
        /// <param name="resultIsIndex">Flag to set return value as index ([*]string type)</param>
        /// <param name="okCaption">String of "OK" button</param>
        /// <param name="cancelCaption">String of "Cancel" button</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowMultiChoiceDialog(string title, string[] items, bool[] checkedItems,
            string callbackGameObject, string callbackMethod, string changeCallbackMethod, 
            bool resultIsIndex, string okCaption = "OK", string cancelCaption = "Cancel", string style = "")
        {
            ShowMultiChoiceDialog(title, items, checkedItems, 
                callbackGameObject, callbackMethod, changeCallbackMethod, "", 
                resultIsIndex, okCaption, cancelCaption, style);
        }

        //(*) Argument omission overload
        //※引数省略 オーバーロード
        /// <summary>
        /// Call Android Multi Choice Dialog (Return index or items string)
        /// http://fantom1x.blog130.fc2.com/blog-entry-274.html#fantomPlugin_MultiChoiceDialog
        ///･Return only those checked from multiple choices.
        ///･When "OK", the string of the choice is concatenated with a line feed ("\n") and returned as the argument of the callback
        /// (or return index ([*]string type) with resultIsIndex=true).
        ///･When "Cancel", or clicked outside the dialog (-> back to application) return nothing.
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// 複数選択肢ダイアログ：Android の AlertDialog を使用する（アイテムの文字列 または インデクスを返す）
        /// http://fantom1x.blog130.fc2.com/blog-entry-274.html#fantomPlugin_MultiChoiceDialog
        ///・複数の選択肢の中からチェックされているものだけを返すダイアログ。スイッチダイアログ（ShowSwitchDialog()）と異なり、オフになっているものは返ってこない。
        ///・"OK" のとき、選択肢の文字列が改行("\n")で連結されてコールバックの引数で返される（resultIsIndex=true でインデクス(※文字列型)で返す）。
        ///・"Cancel" または 何も押されない（ダイアログ外クリック→元の画面に戻った）ときは何も返さない（何もしない）。
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="items">Choice strings (Array)</param>
        /// <param name="checkedItems">Initial state of checked (Array) (null = nothing)</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="callbackMethod">Method name to callback (it is in GameObject)</param>
        /// <param name="resultIsIndex">Flag to set return value as index ([*]string type)</param>
        /// <param name="okCaption">String of "OK" button</param>
        /// <param name="cancelCaption">String of "Cancel" button</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowMultiChoiceDialog(string title, string[] items, bool[] checkedItems,
            string callbackGameObject, string callbackMethod, bool resultIsIndex = false,
            string okCaption = "OK", string cancelCaption = "Cancel", string style = "")
        {
            ShowMultiChoiceDialog(title, items, checkedItems, 
                callbackGameObject, callbackMethod, "", "",
                resultIsIndex, okCaption, cancelCaption, style);
        }


        //(*) Return result value for each item
        //※アイテムごとの結果文字列を返す
        /// <summary>
        /// Call Android Multi Choice Dialog (Return result value for each item)
        /// http://fantom1x.blog130.fc2.com/blog-entry-274.html#fantomPlugin_MultiChoiceDialog
        ///･Return only those checked from multiple choices.
        ///･When "OK", the elements of the result array (resultValues) corresponding to the sequence of choices (items)
        /// are concatenated with line feed ("\n") and returned as arguments of the callback.
        ///·When canceled without "OK" or closing, the following character string is returned in the cancel callback (cancelCallbackMethod).
        ///　CANCEL_DIALOG : Cancel button pressed
        ///　CLOSE_DIALOG : It was closed without "OK" (tap outside the dialog, erase it by pressing the back key etc.)
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// 複数選択肢ダイアログ：Android の AlertDialog を使用する（アイテムごとの結果文字列を返す）
        /// http://fantom1x.blog130.fc2.com/blog-entry-274.html#fantomPlugin_MultiChoiceDialog
        ///・複数の選択肢の中からチェックされているものだけを返すダイアログ。スイッチダイアログ（ShowSwitchDialog()）と異なり、オフになっているものは返ってこない。
        ///・"OK" のとき、選択肢の並びに対応した結果配列（resultValues）の要素が改行("\n")で連結されてコールバックの引数で返される。
        ///・キャンセルまたは「OK」せずに閉じられたときはキャンセルコールバック（cancelCallbackMethod）に以下の文字列が返る。
        ///　CANCEL_DIALOG : キャンセルボタンが押された
        ///　CLOSE_DIALOG : 「OK」せずに閉じられた（ダイアログ外をタップ、バックキーを押して消した等）
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="items">Choice strings (Array)</param>
        /// <param name="checkedItems">Initial state of checked (Array) (null = nothing)</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="resultCallbackMethod">Method name to callback (it is in GameObject)</param>
        /// <param name="changeCallbackMethod">Method name to callback of value chaged (it is in GameObject)</param>
        /// <param name="cancelCallbackMethod">Method name to callback when canceled (it is in GameObject)</param>
        /// <param name="resultValues">The element of the selected index (items) becomes the return value</param>
        /// <param name="okCaption">String of "OK" button</param>
        /// <param name="cancelCaption">String of "Cancel" button</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowMultiChoiceDialog(string title, string[] items, bool[] checkedItems,
            string callbackGameObject, string resultCallbackMethod, string changeCallbackMethod, string cancelCallbackMethod,
            string[] resultValues, string okCaption = "OK", string cancelCaption = "Cancel", string style = "")
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "showMultiChoiceDialog",
                    context,
                    title,
                    items,
                    checkedItems,
                    callbackGameObject,
                    resultCallbackMethod,
                    changeCallbackMethod,
                    cancelCallbackMethod,
                    resultValues,
                    okCaption,
                    cancelCaption,
                    style
                );
            }));
        }

        //(*) Return result value for each item, No cancel callback overload
        //※アイテムごとの結果文字列を返す, キャンセルコールバック無し オーバーロード
        /// <summary>
        /// Call Android Multi Choice Dialog (Return result value for each item)
        /// http://fantom1x.blog130.fc2.com/blog-entry-274.html#fantomPlugin_MultiChoiceDialog
        ///･Return only those checked from multiple choices.
        ///･When "OK", the elements of the result array (resultValues) corresponding to the sequence of choices (items)
        /// are concatenated with line feed ("\n") and returned as arguments of the callback.
        ///･When "Cancel", or clicked outside the dialog (-> back to application) return nothing.
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// 複数選択肢ダイアログ：Android の AlertDialog を使用する（アイテムごとの結果文字列を返す）
        /// http://fantom1x.blog130.fc2.com/blog-entry-274.html#fantomPlugin_MultiChoiceDialog
        ///・複数の選択肢の中からチェックされているものだけを返すダイアログ。スイッチダイアログ（ShowSwitchDialog()）と異なり、オフになっているものは返ってこない。
        ///・"OK" のとき、選択肢の並びに対応した結果配列（resultValues）の要素が改行("\n")で連結されてコールバックの引数で返される。
        ///・"Cancel" または 何も押されない（ダイアログ外クリック→元の画面に戻った）ときは何も返さない（何もしない）。
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="items">Choice strings (Array)</param>
        /// <param name="checkedItems">Initial state of checked (Array) (null = nothing)</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="resultCallbackMethod">Method name to callback (it is in GameObject)</param>
        /// <param name="changeCallbackMethod">Method name to callback of value chaged (it is in GameObject)</param>
        /// <param name="resultValues">The element of the selected index (items) becomes the return value</param>
        /// <param name="okCaption">String of "OK" button</param>
        /// <param name="cancelCaption">String of "Cancel" button</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowMultiChoiceDialog(string title, string[] items, bool[] checkedItems,
            string callbackGameObject, string resultCallbackMethod, string changeCallbackMethod, 
            string[] resultValues, string okCaption = "OK", string cancelCaption = "Cancel", string style = "")
        {
            ShowMultiChoiceDialog(title, items, checkedItems, 
                callbackGameObject, resultCallbackMethod, changeCallbackMethod, "", 
                resultValues, okCaption, cancelCaption, style);
        }

        //(*) Return result value for each item, Argument omission overload
        //※アイテムごとの結果文字列を返す, 引数省略 オーバーロード
        /// <summary>
        /// Call Android Multi Choice Dialog (Return result value for each item)
        /// http://fantom1x.blog130.fc2.com/blog-entry-274.html#fantomPlugin_MultiChoiceDialog
        ///･Return only those checked from multiple choices.
        ///･When "OK", the elements of the result array (resultValues) corresponding to the sequence of choices (items)
        /// are concatenated with line feed ("\n") and returned as arguments of the callback.
        ///･When "Cancel", or clicked outside the dialog (-> back to application) return nothing.
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// 複数選択肢ダイアログ：Android の AlertDialog を使用する（アイテムごとの結果文字列を返す）
        /// http://fantom1x.blog130.fc2.com/blog-entry-274.html#fantomPlugin_MultiChoiceDialog
        ///・複数の選択肢の中からチェックされているものだけを返すダイアログ。スイッチダイアログ（ShowSwitchDialog()）と異なり、オフになっているものは返ってこない。
        ///・"OK" のとき、選択肢の並びに対応した結果配列（resultValues）の要素が改行("\n")で連結されてコールバックの引数で返される。
        ///・"Cancel" または 何も押されない（ダイアログ外クリック→元の画面に戻った）ときは何も返さない（何もしない）。
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="items">Choice strings (Array)</param>
        /// <param name="checkedItems">Initial state of checked (Array) (null = nothing)</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="callbackMethod">Method name to callback (it is in GameObject)</param>
        /// <param name="resultValues">The element of the selected index (items) becomes the return value</param>
        /// <param name="okCaption">String of "OK" button</param>
        /// <param name="cancelCaption">String of "Cancel" button</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowMultiChoiceDialog(string title, string[] items, bool[] checkedItems,
            string callbackGameObject, string callbackMethod, string[] resultValues,
            string okCaption = "OK", string cancelCaption = "Cancel", string style = "")
        {
            ShowMultiChoiceDialog(title, items, checkedItems, 
                callbackGameObject, callbackMethod, "", "",
                resultValues, okCaption, cancelCaption, style);
        }



        //==========================================================
        // Switch Dialog

        /// <summary>
        /// Call Android Switch Dialog
        /// http://fantom1x.blog130.fc2.com/blog-entry-280.html#fantomPlugin_SwitchDialog
        ///･Depending on the state of the switch, a dialog for acquiring the On/Off state of each item.
        ///･When "OK", the result (true/false) corresponding to the sequence of items is concatenated with a line feed ("\n")
        /// and returned as the argument of the callback.
        ///･When a key is set for each item, the state of the switch (true/false) is returned with '=' like "key=true", "key=false".
        ///·When canceled without "OK" or closing, the following character string is returned in the cancel callback (cancelCallbackMethod).
        ///　CANCEL_DIALOG : Cancel button pressed
        ///　CLOSE_DIALOG : It was closed without "OK" (tap outside the dialog, erase it by pressing the back key etc.)
        ///(*) When use the message string, Notice that it will not be displayed if the items overflow the dialog (-> message="").
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// スイッチの状態により、各アイテムのオン・オフ状態を取得するダイアログ
        /// http://fantom1x.blog130.fc2.com/blog-entry-280.html#fantomPlugin_SwitchDialog
        ///・複数選択肢ダイアログ（ShowMultiChoiceDialog()）と異なり、オフの状態も結果として返ってくる。
        ///・"OK" のとき、アイテムの並びに対応した結果（true/false）が改行("\n")で連結されてコールバックの引数で返される。
        ///・各アイテムにキーを設定したときは "key=true", "key=false" のように '=' でスイッチの状態（true/false）が返される。
        ///・キャンセルまたは「OK」せずに閉じられたときはキャンセルコールバック（cancelCallbackMethod）に以下の文字列が返る。
        ///　CANCEL_DIALOG : キャンセルボタンが押された
        ///　CLOSE_DIALOG : 「OK」せずに閉じられた（ダイアログ外をタップ、バックキーを押して消した等）
        ///※メッセージ文字列（message）、決定ボタンはアイテムがダイアログに収まらないとき表示されないので注意（スクロール表示に専有される）
        ///  → メッセージを無くす（空文字("")にする）とボタンのみ表示できるようになる：Androidダイアログの仕様(?)。
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="message">Message string ("" = nothing)</param>
        /// <param name="items">Item strings (Array)</param>
        /// <param name="itemKeys">Item keys (Array) (null = all nothing)</param>
        /// <param name="checkedItems">Initial state of the switches (Array) (null = all off)</param>
        /// <param name="itemsTextColor">Text color of items (0 = not specified: Color format is int32 (AARRGGBB: Android-Java))</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="resultCallbackMethod">Method name to callback (it is in GameObject)</param>
        /// <param name="changeCallbackMethod">Method name to callback of value chaged (it is in GameObject)</param>
        /// <param name="cancelCallbackMethod">Method name to callback when canceled (it is in GameObject)</param>
        /// <param name="okCaption">String of "OK" button</param>
        /// <param name="cancelCaption">String of "Cancel" button</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowSwitchDialog(string title, string message, 
            string[] items, string[] itemKeys, bool[] checkedItems, int itemsTextColor,
            string callbackGameObject, string resultCallbackMethod, string changeCallbackMethod, string cancelCallbackMethod,
            string okCaption, string cancelCaption, string style)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "showSwitchDialog",
                    context,
                    title,
                    message,
                    items,
                    itemKeys,
                    checkedItems,
                    itemsTextColor,
                    callbackGameObject,
                    resultCallbackMethod,
                    changeCallbackMethod,
                    cancelCallbackMethod,
                    okCaption,
                    cancelCaption,
                    style
                );
            }));
        }

        //(*) No cancel callback overload
        //※キャンセルコールバック無し オーバーロード
        /// <summary>
        /// Call Android Switch Dialog
        /// http://fantom1x.blog130.fc2.com/blog-entry-280.html#fantomPlugin_SwitchDialog
        ///･Depending on the state of the switch, a dialog for acquiring the On/Off state of each item.
        ///･When "OK", the result (true/false) corresponding to the sequence of items is concatenated with a line feed ("\n")
        /// and returned as the argument of the callback.
        ///･When a key is set for each item, the state of the switch (true/false) is returned with '=' like "key=true", "key=false".
        ///･When "Cancel", or clicked outside the dialog (-> back to application) return nothing.
        ///(*) When use the message string, Notice that it will not be displayed if the items overflow the dialog (-> message="").
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// スイッチの状態により、各アイテムのオン・オフ状態を取得するダイアログ
        /// http://fantom1x.blog130.fc2.com/blog-entry-280.html#fantomPlugin_SwitchDialog
        ///・複数選択肢ダイアログ（ShowMultiChoiceDialog()）と異なり、オフの状態も結果として返ってくる。
        ///・"OK" のとき、アイテムの並びに対応した結果（true/false）が改行("\n")で連結されてコールバックの引数で返される。
        ///・各アイテムにキーを設定したときは "key=true", "key=false" のように '=' でスイッチの状態（true/false）が返される。
        ///・"Cancel" または 何も押されない（ダイアログ外クリック→元の画面に戻った）ときは何も返さない（何もしない）。
        ///※メッセージ文字列（message）、決定ボタンはアイテムがダイアログに収まらないとき表示されないので注意（スクロール表示に専有される）
        ///  → メッセージを無くす（空文字("")にする）とボタンのみ表示できるようになる：Androidダイアログの仕様(?)。
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="message">Message string ("" = nothing)</param>
        /// <param name="items">Item strings (Array)</param>
        /// <param name="itemKeys">Item keys (Array) (null = all nothing)</param>
        /// <param name="checkedItems">Initial state of the switches (Array) (null = all off)</param>
        /// <param name="itemsTextColor">Text color of items (0 = not specified: Color format is int32 (AARRGGBB: Android-Java))</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="resultCallbackMethod">Method name to callback (it is in GameObject)</param>
        /// <param name="changeCallbackMethod">Method name to callback of value chaged (it is in GameObject)</param>
        /// <param name="okCaption">String of "OK" button</param>
        /// <param name="cancelCaption">String of "Cancel" button</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowSwitchDialog(string title, string message, 
            string[] items, string[] itemKeys, bool[] checkedItems, int itemsTextColor,
            string callbackGameObject, string resultCallbackMethod, string changeCallbackMethod,
            string okCaption, string cancelCaption, string style)
        {
            ShowSwitchDialog(title, message, items, itemKeys, checkedItems, itemsTextColor, 
                callbackGameObject, resultCallbackMethod, changeCallbackMethod, "", 
                okCaption, cancelCaption, style);
        }

        //(*) Argument omission overload
        //※引数省略 オーバーロード
        /// <summary>
        /// Call Android Switch Dialog
        /// http://fantom1x.blog130.fc2.com/blog-entry-280.html#fantomPlugin_SwitchDialog
        ///･Depending on the state of the switch, a dialog for acquiring the On/Off state of each item.
        ///･When "OK", the result (true/false) corresponding to the sequence of items is concatenated with a line feed ("\n")
        /// and returned as the argument of the callback.
        ///･When a key is set for each item, the state of the switch (true/false) is returned with '=' like "key=true", "key=false".
        ///･When "Cancel", or clicked outside the dialog (-> back to application) return nothing.
        ///(*) When use the message string, Notice that it will not be displayed if the items overflow the dialog (-> message="").
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// スイッチの状態により、各アイテムのオン・オフ状態を取得するダイアログ
        /// http://fantom1x.blog130.fc2.com/blog-entry-280.html#fantomPlugin_SwitchDialog
        ///・複数選択肢ダイアログ（ShowMultiChoiceDialog()）と異なり、オフの状態も結果として返ってくる。
        ///・"OK" のとき、アイテムの並びに対応した結果（true/false）が改行("\n")で連結されてコールバックの引数で返される。
        ///・各アイテムにキーを設定したときは "key=true", "key=false" のように '=' でスイッチの状態（true/false）が返される。
        ///・"Cancel" または 何も押されない（ダイアログ外クリック→元の画面に戻った）ときは何も返さない（何もしない）。
        ///※メッセージ文字列（message）、決定ボタンはアイテムがダイアログに収まらないとき表示されないので注意（スクロール表示に専有される）
        ///  → メッセージを無くす（空文字("")にする）とボタンのみ表示できるようになる：Androidダイアログの仕様(?)。
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="message">Message string ("" = nothing)</param>
        /// <param name="items">Item strings (Array)</param>
        /// <param name="itemKeys">Item keys (Array) (null = all nothing)</param>
        /// <param name="checkedItems">Initial state of the switches (Array) (null = all off)</param>
        /// <param name="itemsTextColor">Text color of items (0 = not specified: Color format is int32 (AARRGGBB: Android-Java))</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="callbackMethod">Method name to callback (it is in GameObject)</param>
        /// <param name="okCaption">String of "OK" button</param>
        /// <param name="cancelCaption">String of "Cancel" button</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowSwitchDialog(string title, string message, 
            string[] items, string[] itemKeys, bool[] checkedItems, int itemsTextColor,
            string callbackGameObject, string callbackMethod, 
            string okCaption = "OK", string cancelCaption = "Cancel", string style = "")
        {
            ShowSwitchDialog(title, message, items, itemKeys, checkedItems, itemsTextColor, 
                callbackGameObject, callbackMethod, "", "", 
                okCaption, cancelCaption, style);
        }


        //(*) Unity.Color overload
        /// <summary>
        /// Call Android Switch Dialog (Unity.Color overload)
        /// http://fantom1x.blog130.fc2.com/blog-entry-280.html#fantomPlugin_SwitchDialog
        ///･Depending on the state of the switch, a dialog for acquiring the On/Off state of each item.
        ///･When "OK", the result (true/false) corresponding to the sequence of items is concatenated with a line feed ("\n")
        /// and returned as the argument of the callback.
        ///･When a key is set for each item, the state of the switch (true/false) is returned with '=' like "key=true", "key=false".
        ///·When canceled without "OK" or closing, the following character string is returned in the cancel callback (cancelCallbackMethod).
        ///　CANCEL_DIALOG : Cancel button pressed
        ///　CLOSE_DIALOG : It was closed without "OK" (tap outside the dialog, erase it by pressing the back key etc.)
        ///(*) When use the message string, Notice that it will not be displayed if the items overflow the dialog (-> message="").
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// スイッチの状態により、各アイテムのオン・オフ状態を取得するダイアログ（Unity の Color 形式のオーバーロード）
        /// http://fantom1x.blog130.fc2.com/blog-entry-280.html#fantomPlugin_SwitchDialog
        ///・複数選択肢ダイアログ（ShowMultiChoiceDialog()）と異なり、オフの状態も結果として返ってくる。
        ///・"OK" のとき、選択肢の並びに対応した結果（true/false）が改行("\n")で連結されてコールバックの引数で返される。
        ///・各アイテムにキーを設定したときは "key=true", "key=false" のように '=' でスイッチの状態（true/false）が返される。
        ///・キャンセルまたは「OK」せずに閉じられたときはキャンセルコールバック（cancelCallbackMethod）に以下の文字列が返る。
        ///　CANCEL_DIALOG : キャンセルボタンが押された
        ///　CLOSE_DIALOG : 「OK」せずに閉じられた（ダイアログ外をタップ、バックキーを押して消した等）
        ///※メッセージ文字列（message）、決定ボタンはアイテムがダイアログに収まらないとき表示されないので注意（スクロール表示に専有される）
        ///  → メッセージを無くす（空文字("")にする）とボタンのみ表示できるようになる：Androidダイアログの仕様(?)。
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="message">Message string ("" = nothing)</param>
        /// <param name="items">Item strings (Array)</param>
        /// <param name="itemKeys">Item keys (Array) (null = all nothing)</param>
        /// <param name="checkedItems">Initial state of the switches (Array) (null = all off)</param>
        /// <param name="itemsTextColor">Text color of items (Color.clear = not specified: Not clear color)</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="resultCallbackMethod">Method name to callback (it is in GameObject)</param>
        /// <param name="changeCallbackMethod">Method name to callback of value chaged (it is in GameObject)</param>
        /// <param name="cancelCallbackMethod">Method name to callback when canceled (it is in GameObject)</param>
        /// <param name="okCaption">String of "OK" button</param>
        /// <param name="cancelCaption">String of "Cancel" button</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowSwitchDialog(string title, string message, 
            string[] items, string[] itemKeys, bool[] checkedItems, Color itemsTextColor,
            string callbackGameObject, string resultCallbackMethod, string changeCallbackMethod, string cancelCallbackMethod,
            string okCaption, string cancelCaption, string style)
        {
            ShowSwitchDialog(title, message, items, itemKeys, checkedItems, itemsTextColor.ToIntARGB(), 
                callbackGameObject, resultCallbackMethod, changeCallbackMethod, cancelCallbackMethod, 
                okCaption, cancelCaption, style);
        }

        //※Unity.Color, キャンセルコールバック無し オーバーロード
        //(*) Unity.Color, No cancel callback overload
        /// <summary>
        /// Call Android Switch Dialog (Unity.Color overload)
        /// http://fantom1x.blog130.fc2.com/blog-entry-280.html#fantomPlugin_SwitchDialog
        ///･Depending on the state of the switch, a dialog for acquiring the On/Off state of each item.
        ///･When "OK", the result (true/false) corresponding to the sequence of items is concatenated with a line feed ("\n")
        /// and returned as the argument of the callback.
        ///･When a key is set for each item, the state of the switch (true/false) is returned with '=' like "key=true", "key=false".
        ///･When "Cancel", or clicked outside the dialog (-> back to application) return nothing.
        ///(*) When use the message string, Notice that it will not be displayed if the items overflow the dialog (-> message="").
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// スイッチの状態により、各アイテムのオン・オフ状態を取得するダイアログ（Unity の Color 形式のオーバーロード）
        /// http://fantom1x.blog130.fc2.com/blog-entry-280.html#fantomPlugin_SwitchDialog
        ///・複数選択肢ダイアログ（ShowMultiChoiceDialog()）と異なり、オフの状態も結果として返ってくる。
        ///・"OK" のとき、選択肢の並びに対応した結果（true/false）が改行("\n")で連結されてコールバックの引数で返される。
        ///・各アイテムにキーを設定したときは "key=true", "key=false" のように '=' でスイッチの状態（true/false）が返される。
        ///・"Cancel" または 何も押されない（ダイアログ外クリック→元の画面に戻った）ときは何も返さない（何もしない）。
        ///※メッセージ文字列（message）、決定ボタンはアイテムがダイアログに収まらないとき表示されないので注意（スクロール表示に専有される）
        ///  → メッセージを無くす（空文字("")にする）とボタンのみ表示できるようになる：Androidダイアログの仕様(?)。
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="message">Message string ("" = nothing)</param>
        /// <param name="items">Item strings (Array)</param>
        /// <param name="itemKeys">Item keys (Array) (null = all nothing)</param>
        /// <param name="checkedItems">Initial state of the switches (Array) (null = all off)</param>
        /// <param name="itemsTextColor">Text color of items (Color.clear = not specified: Not clear color)</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="resultCallbackMethod">Method name to callback (it is in GameObject)</param>
        /// <param name="changeCallbackMethod">Method name to callback of value chaged (it is in GameObject)</param>
        /// <param name="okCaption">String of "OK" button</param>
        /// <param name="cancelCaption">String of "Cancel" button</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowSwitchDialog(string title, string message, 
            string[] items, string[] itemKeys, bool[] checkedItems, Color itemsTextColor,
            string callbackGameObject, string resultCallbackMethod, string changeCallbackMethod,
            string okCaption, string cancelCaption, string style)
        {
            ShowSwitchDialog(title, message, items, itemKeys, checkedItems, itemsTextColor.ToIntARGB(), 
                callbackGameObject, resultCallbackMethod, changeCallbackMethod, "", 
                okCaption, cancelCaption, style);
        }

        //(*) Unity.Color, Argument omission overload
        //※Unity.Color, 引数省略 オーバーロード
        /// <summary>
        /// Call Android Switch Dialog (Unity.Color overload)
        /// http://fantom1x.blog130.fc2.com/blog-entry-280.html#fantomPlugin_SwitchDialog
        ///･Depending on the state of the switch, a dialog for acquiring the On/Off state of each item.
        ///･When "OK", the result (true/false) corresponding to the sequence of items is concatenated with a line feed ("\n")
        /// and returned as the argument of the callback.
        ///･When a key is set for each item, the state of the switch (true/false) is returned with '=' like "key=true", "key=false".
        ///･When "Cancel", or clicked outside the dialog (-> back to application) return nothing.
        ///(*) When use the message string, Notice that it will not be displayed if the items overflow the dialog (-> message="").
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// スイッチの状態により、各アイテムのオン・オフ状態を取得するダイアログ（Unity の Color 形式のオーバーロード）
        /// http://fantom1x.blog130.fc2.com/blog-entry-280.html#fantomPlugin_SwitchDialog
        ///・複数選択肢ダイアログ（ShowMultiChoiceDialog()）と異なり、オフの状態も結果として返ってくる。
        ///・"OK" のとき、選択肢の並びに対応した結果（true/false）が改行("\n")で連結されてコールバックの引数で返される。
        ///・各アイテムにキーを設定したときは "key=true", "key=false" のように '=' でスイッチの状態（true/false）が返される。
        ///・"Cancel" または 何も押されない（ダイアログ外クリック→元の画面に戻った）ときは何も返さない（何もしない）。
        ///※メッセージ文字列（message）、決定ボタンはアイテムがダイアログに収まらないとき表示されないので注意（スクロール表示に専有される）
        ///  → メッセージを無くす（空文字("")にする）とボタンのみ表示できるようになる：Androidダイアログの仕様(?)。
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="message">Message string ("" = nothing)</param>
        /// <param name="items">Item strings (Array)</param>
        /// <param name="itemKeys">Item keys (Array) (null = all nothing)</param>
        /// <param name="checkedItems">Initial state of the switches (Array) (null = all off)</param>
        /// <param name="itemsTextColor">Text color of items (Color.clear = not specified: Not clear color)</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="callbackMethod">Method name to callback (it is in GameObject)</param>
        /// <param name="okCaption">String of "OK" button</param>
        /// <param name="cancelCaption">String of "Cancel" button</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowSwitchDialog(string title, string message,
            string[] items, string[] itemKeys, bool[] checkedItems, Color itemsTextColor,
            string callbackGameObject, string callbackMethod,
            string okCaption = "OK", string cancelCaption = "Cancel", string style = "")
        {
            ShowSwitchDialog(title, message, items, itemKeys, checkedItems, itemsTextColor.ToIntARGB(), 
                callbackGameObject, callbackMethod, "", "", 
                okCaption, cancelCaption, style);
        }



        //==========================================================
        // Slider (Seekbar) Dialog

        /// <summary>
        /// Call Android Slider (Seekbar) Dialog
        /// http://fantom1x.blog130.fc2.com/blog-entry-281.html#fantomPlugin_SliderDialog
        ///･When "OK", the result value corresponding to the sequence of items is concatenated with a line feed ("\n")
        /// and returned as the argument of the callback.
        ///･The result value follows the setting of the number of digits after the decimal point (digits) (it becomes an integer when 0).
        ///･When a key is set for each item, the value is returned with '=' like "key=3", "key=4.5".
        ///·When canceled without "OK" or closing, the following character string is returned in the cancel callback (cancelCallbackMethod).
        ///　CANCEL_DIALOG : Cancel button pressed
        ///　CLOSE_DIALOG : It was closed without "OK" (tap outside the dialog, erase it by pressing the back key etc.)
        ///(*) When use the message string, Notice that it will not be displayed if the items overflow the dialog (-> message="").
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// スライダー（シークバー）で設定値を取得するダイアログ
        /// http://fantom1x.blog130.fc2.com/blog-entry-281.html#fantomPlugin_SliderDialog
        ///・"OK" のとき、アイテムの並びに対応した値が改行("\n")で連結されてコールバックの引数で返される。
        ///・結果の値は小数点以下の桁数（digits）の設定に従う（0 のときは整数になる）。
        ///・各アイテムにキーを設定したときは "key=3", "key=4.5" のように '=' で値が返される。
        ///・キャンセルまたは「OK」せずに閉じられたときはキャンセルコールバック（cancelCallbackMethod）に以下の文字列が返る。
        ///　CANCEL_DIALOG : キャンセルボタンが押された
        ///　CLOSE_DIALOG : 「OK」せずに閉じられた（ダイアログ外をタップ、バックキーを押して消した等）
        ///※メッセージ文字列（message）、決定ボタンはアイテムがダイアログに収まらないとき表示されないので注意（スクロール表示に専有される）
        ///  → メッセージを無くす（空文字("")にする）とボタンのみ表示できるようになる：Androidダイアログの仕様(?)。
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="message">Message string ("" = nothing)</param>
        /// <param name="items">Item strings (Array)</param>
        /// <param name="itemKeys">Item keys (Array) (null = all nothing)</param>
        /// <param name="defValues">Initial values (null = all 0 : 6 digits of integer part + 3 digits after decimal point)</param>
        /// <param name="minValues">Minimum values (null = all 0 : 6 digits of integer part + 3 digits after decimal point)</param>
        /// <param name="maxValues">Maximum values (null = all 100 : 6 digits of integer part + 3 digits after decimal point)</param>
        /// <param name="digits">Number of decimal places (0 = integer, 1~3 = after decimal point)</param>
        /// <param name="itemsTextColor">Text color of items (0 = not specified: Color format is int32 (AARRGGBB: Android-Java))</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="resultCallbackMethod">Method name to result callback when "OK" button pressed (it is in GameObject)</param>
        /// <param name="changeCallbackMethod">Method name to real-time callback when the value of the slider is changed (it is in GameObject)</param>
        /// <param name="cancelCallbackMethod">Method name to callback when canceled (it is in GameObject)</param>
        /// <param name="okCaption">String of "OK" button</param>
        /// <param name="cancelCaption">String of "Cancel" button</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowSliderDialog(string title, string message, string[] items, string[] itemKeys, 
            float[] defValues, float[] minValues, float[] maxValues, int[] digits, int itemsTextColor,
            string callbackGameObject, string resultCallbackMethod, string changeCallbackMethod, string cancelCallbackMethod,
            string okCaption, string cancelCaption, string style)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "showSeekBarDialog",
                    context,
                    title,
                    message,
                    items,
                    itemKeys,
                    defValues,
                    minValues,
                    maxValues,
                    digits,
                    itemsTextColor,
                    callbackGameObject,
                    resultCallbackMethod,
                    changeCallbackMethod,
                    cancelCallbackMethod,
                    okCaption,
                    cancelCaption,
                    style
                );
            }));
        }

        //(*) No cancel callback overload
        //※キャンセルコールバック無し オーバーロード
        /// <summary>
        /// Call Android Slider (Seekbar) Dialog
        /// http://fantom1x.blog130.fc2.com/blog-entry-281.html#fantomPlugin_SliderDialog
        ///･When "OK", the result value corresponding to the sequence of items is concatenated with a line feed ("\n")
        /// and returned as the argument of the callback.
        ///･The result value follows the setting of the number of digits after the decimal point (digits) (it becomes an integer when 0).
        ///･When a key is set for each item, the value is returned with '=' like "key=3", "key=4.5".
        ///･When "Cancel", or clicked outside the dialog (-> back to application) return nothing.
        ///(*) When use the message string, Notice that it will not be displayed if the items overflow the dialog (-> message="").
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// スライダー（シークバー）で設定値を取得するダイアログ
        /// http://fantom1x.blog130.fc2.com/blog-entry-281.html#fantomPlugin_SliderDialog
        ///・"OK" のとき、アイテムの並びに対応した値が改行("\n")で連結されてコールバックの引数で返される。
        ///・結果の値は小数点以下の桁数（digits）の設定に従う（0 のときは整数になる）。
        ///・各アイテムにキーを設定したときは "key=3", "key=4.5" のように '=' で値が返される。
        ///・"Cancel" または 何も押されない（ダイアログ外クリック→元の画面に戻った）ときは何も返さない（何もしない）。
        ///※メッセージ文字列（message）、決定ボタンはアイテムがダイアログに収まらないとき表示されないので注意（スクロール表示に専有される）
        ///  → メッセージを無くす（空文字("")にする）とボタンのみ表示できるようになる：Androidダイアログの仕様(?)。
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="message">Message string ("" = nothing)</param>
        /// <param name="items">Item strings (Array)</param>
        /// <param name="itemKeys">Item keys (Array) (null = all nothing)</param>
        /// <param name="defValues">Initial values (null = all 0 : 6 digits of integer part + 3 digits after decimal point)</param>
        /// <param name="minValues">Minimum values (null = all 0 : 6 digits of integer part + 3 digits after decimal point)</param>
        /// <param name="maxValues">Maximum values (null = all 100 : 6 digits of integer part + 3 digits after decimal point)</param>
        /// <param name="digits">Number of decimal places (0 = integer, 1~3 = after decimal point)</param>
        /// <param name="itemsTextColor">Text color of items (0 = not specified: Color format is int32 (AARRGGBB: Android-Java))</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="resultCallbackMethod">Method name to result callback when "OK" button pressed (it is in GameObject)</param>
        /// <param name="changeCallbackMethod">Method name to real-time callback when the value of the slider is changed (it is in GameObject)</param>
        /// <param name="okCaption">String of "OK" button</param>
        /// <param name="cancelCaption">String of "Cancel" button</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowSliderDialog(string title, string message, string[] items, string[] itemKeys, 
            float[] defValues, float[] minValues, float[] maxValues, int[] digits, int itemsTextColor,
            string callbackGameObject, string resultCallbackMethod, string changeCallbackMethod = "",
            string okCaption = "OK", string cancelCaption = "Cancel", string style = "")
        {
            ShowSliderDialog(title, message, items, itemKeys, 
                defValues, minValues, maxValues, digits, itemsTextColor, 
                callbackGameObject, resultCallbackMethod, changeCallbackMethod, "",
                okCaption, cancelCaption, style);
        }


        //(*) Unity.Color overload
        /// <summary>
        /// Call Android Slider Dialog (Unity.Color overload)
        /// http://fantom1x.blog130.fc2.com/blog-entry-281.html#fantomPlugin_SliderDialog
        ///･When "OK", the result value corresponding to the sequence of items is concatenated with a line feed ("\n")
        /// and returned as the argument of the callback.
        ///･The result value follows the setting of the number of digits after the decimal point (digits) (it becomes an integer when 0).
        ///･When a key is set for each item, the value is returned with '=' like "key=3", "key=4.5".
        ///·When canceled without "OK" or closing, the following character string is returned in the cancel callback (cancelCallbackMethod).
        ///　CANCEL_DIALOG : Cancel button pressed
        ///　CLOSE_DIALOG : It was closed without "OK" (tap outside the dialog, erase it by pressing the back key etc.)
        ///(*) When use the message string, Notice that it will not be displayed if the items overflow the dialog (-> message="").
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// スライダー（シークバー）で設定値を取得するダイアログ（Unity の Color 形式のオーバーロード）
        /// http://fantom1x.blog130.fc2.com/blog-entry-281.html#fantomPlugin_SliderDialog
        ///・"OK" のとき、アイテムの並びに対応した値が改行("\n")で連結されてコールバックの引数で返される。
        ///・結果の値は小数点以下の桁数（digits）の設定に従う（0 のときは整数になる）。
        ///・各アイテムにキーを設定したときは "key=3", "key=4.5" のように '=' で値が返される。
        ///・キャンセルまたは「OK」せずに閉じられたときはキャンセルコールバック（cancelCallbackMethod）に以下の文字列が返る。
        ///　CANCEL_DIALOG : キャンセルボタンが押された
        ///　CLOSE_DIALOG : 「OK」せずに閉じられた（ダイアログ外をタップ、バックキーを押して消した等）
        ///※メッセージ文字列（message）、決定ボタンはアイテムがダイアログに収まらないとき表示されないので注意（スクロール表示に専有される）
        ///  → メッセージを無くす（空文字("")にする）とボタンのみ表示できるようになる：Androidダイアログの仕様(?)。
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="message">Message string ("" = nothing)</param>
        /// <param name="items">Item strings (Array)</param>
        /// <param name="itemKeys">Item keys (Array) (null = all nothing)</param>
        /// <param name="defValues">Initial values (null = all 0 : 6 digits of integer part + 3 digits after decimal point)</param>
        /// <param name="minValues">Minimum values (null = all 0 : 6 digits of integer part + 3 digits after decimal point)</param>
        /// <param name="maxValues">Maximum values (null = all 100 : 6 digits of integer part + 3 digits after decimal point)</param>
        /// <param name="digits">Number of decimal places (0 = integer, 1~3 = after decimal point)</param>
        /// <param name="itemsTextColor">Text color of items (Color.clear = not specified: Not clear color)</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="resultCallbackMethod">Method name to result callback when "OK" button pressed (it is in GameObject)</param>
        /// <param name="changeCallbackMethod">Method name to real-time callback when the value of the slider is changed (it is in GameObject)</param>
        /// <param name="cancelCallbackMethod">Method name to callback when canceled (it is in GameObject)</param>
        /// <param name="okCaption">String of "OK" button</param>
        /// <param name="cancelCaption">String of "Cancel" button</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowSliderDialog(string title, string message, string[] items, string[] itemKeys,
            float[] defValues, float[] minValues, float[] maxValues, int[] digits, Color itemsTextColor, 
            string callbackGameObject, string resultCallbackMethod, string changeCallbackMethod, string cancelCallbackMethod,
            string okCaption, string cancelCaption, string style)
        {
            ShowSliderDialog(title, message, items, itemKeys, 
                defValues, minValues, maxValues, digits, itemsTextColor.ToIntARGB(), 
                callbackGameObject, resultCallbackMethod, changeCallbackMethod, cancelCallbackMethod,
                okCaption, cancelCaption, style);
        }

        //(*) Unity.Color, Argument omission overload
        //※Unity.Color, 引数省略 オーバーロード
        /// <summary>
        /// Call Android Slider Dialog (Unity.Color overload)
        /// http://fantom1x.blog130.fc2.com/blog-entry-281.html#fantomPlugin_SliderDialog
        ///･When "OK", the result value corresponding to the sequence of items is concatenated with a line feed ("\n")
        /// and returned as the argument of the callback.
        ///･The result value follows the setting of the number of digits after the decimal point (digits) (it becomes an integer when 0).
        ///･When a key is set for each item, the value is returned with '=' like "key=3", "key=4.5".
        ///･When "Cancel", or clicked outside the dialog (-> back to application) return nothing.
        ///(*) When use the message string, Notice that it will not be displayed if the items overflow the dialog (-> message="").
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// スライダー（シークバー）で設定値を取得するダイアログ（Unity の Color 形式のオーバーロード）
        /// http://fantom1x.blog130.fc2.com/blog-entry-281.html#fantomPlugin_SliderDialog
        ///・"OK" のとき、アイテムの並びに対応した値が改行("\n")で連結されてコールバックの引数で返される。
        ///・結果の値は小数点以下の桁数（digits）の設定に従う（0 のときは整数になる）。
        ///・各アイテムにキーを設定したときは "key=3", "key=4.5" のように '=' で値が返される。
        ///・"Cancel" または 何も押されない（ダイアログ外クリック→元の画面に戻った）ときは何も返さない（何もしない）。
        ///※メッセージ文字列（message）、決定ボタンはアイテムがダイアログに収まらないとき表示されないので注意（スクロール表示に専有される）
        ///  → メッセージを無くす（空文字("")にする）とボタンのみ表示できるようになる：Androidダイアログの仕様(?)。
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="message">Message string ("" = nothing)</param>
        /// <param name="items">Item strings (Array)</param>
        /// <param name="itemKeys">Item keys (Array) (null = all nothing)</param>
        /// <param name="defValues">Initial values (null = all 0 : 6 digits of integer part + 3 digits after decimal point)</param>
        /// <param name="minValues">Minimum values (null = all 0 : 6 digits of integer part + 3 digits after decimal point)</param>
        /// <param name="maxValues">Maximum values (null = all 100 : 6 digits of integer part + 3 digits after decimal point)</param>
        /// <param name="digits">Number of decimal places (0 = integer, 1~3 = after decimal point)</param>
        /// <param name="itemsTextColor">Text color of items (Color.clear = not specified: Not clear color)</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="resultCallbackMethod">Method name to result callback when "OK" button pressed (it is in GameObject)</param>
        /// <param name="changeCallbackMethod">Method name to real-time callback when the value of the slider is changed (it is in GameObject)</param>
        /// <param name="okCaption">String of "OK" button</param>
        /// <param name="cancelCaption">String of "Cancel" button</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowSliderDialog(string title, string message, string[] items, string[] itemKeys,
            float[] defValues, float[] minValues, float[] maxValues, int[] digits, Color itemsTextColor, 
            string callbackGameObject, string resultCallbackMethod, string changeCallbackMethod = "",
            string okCaption = "OK", string cancelCaption = "Cancel", string style = "")
        {
            ShowSliderDialog(title, message, items, itemKeys, 
                defValues, minValues, maxValues, digits, itemsTextColor.ToIntARGB(), 
                callbackGameObject, resultCallbackMethod, changeCallbackMethod, "",
                okCaption, cancelCaption, style);
        }




        //==========================================================
        // Customizable Dialog
        //·Construct each item (widget) with 'DialogItem' as an argument.
        //·Array of argument items are arranged in order from the top.
        // http://fantom1x.blog130.fc2.com/blog-entry-290.html
        // http://fantom1x.blog130.fc2.com/blog-entry-282.html
        //
        //
        // カスタマイズできるダイアログ
        //・DialogItem を引数として、各アイテム（ウィジェット）を構築する。
        //・引数アイテムの配列は上から順に配置される。
        // http://fantom1x.blog130.fc2.com/blog-entry-290.html
        // http://fantom1x.blog130.fc2.com/blog-entry-282.html
        //==========================================================

        public const string ANDROID_CUSTOM_DIALOG = ANDROID_PACKAGE + ".AndroidCustomDialog";

        /// <summary>
        /// Call Android Custom Dialog
        ///･Dialog where freely add Text, Switch, Slider, Toggle buttons and Dividing lines.
        ///･The return value is a pair of values ("key=value" + line feed ("\n")) or JSON format ("{"key":"value"}") for a key set for each item (resultIsJson=true:JSON).
        ///·When canceled without "OK" or closing, the following character string is returned in the cancel callback (cancelCallbackMethod).
        ///　CANCEL_DIALOG : Cancel button pressed
        ///　CLOSE_DIALOG : It was closed without "OK" (tap outside the dialog, erase it by pressing the back key etc.)
        ///･The parameter (dialogItems) of each item (Widgets: DivisorItem, TextItem, SwitchItem, SliderItem, ToggleItem) in one array.
        ///･Generation of each widget is arranged in order from the top. Ignored if there are invalid parameters (or no dialog is generated)
        ///(*) When use the message string, Notice that it will not be displayed if the items overflow the dialog (-> message="").
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// カスタマイズできるダイアログ：Android の AlertDialog を使用する
        ///・テキスト、スイッチ、スライダー、トグルボタン、分割線を自由に設置できるダイアログ。
        ///・戻り値は各アイテム（ウィジェット）に設定したキーに対して値のペア（"key=value"＋改行("\n")）または JSON形式（{"key":"value"}※ダブルクォートは値による）で返される。
        ///・キャンセルまたは「OK」せずに閉じられたときはキャンセルコールバック（cancelCallbackMethod）に以下の文字列が返る。
        ///　CANCEL_DIALOG : キャンセルボタンが押された
        ///　CLOSE_DIALOG : 「OK」せずに閉じられた（ダイアログ外をタップ、バックキーを押して消した等）
        ///・各アイテムのパラメタ（DialogItem）は各ウィジェット用のサブクラス（DivisorItem, TextItem, SwitchItem, SliderItem, ToggleItem）を１つの配列で渡す。
        ///・各ウィジェットの生成は上から順に配置される。不正なパラメタがあった場合は無視される（もしくはダイアログが生成されない）。
        ///※メッセージ文字列（message）、決定ボタンはアイテムがダイアログに収まらないとき表示されないので注意（スクロール表示に専有される）
        ///  → メッセージを無くす（空文字("")にする）とボタンのみ表示できるようになる：Androidダイアログの仕様(?)。
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="message">Message string ("" = nothing)</param>
        /// <param name="dialogItems">Parameters of each item (widget) (Array)</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="resultCallbackMethod">Method name to callback (it is in GameObject)</param>
        /// <param name="cancelCallbackMethod">Method name to callback when canceled (it is in GameObject)</param>
        /// <param name="resultIsJson">return value in: true=JSON format / false="key=value\n"</param>
        /// <param name="okCaption">String of "OK" button</param>
        /// <param name="cancelCaption">String of "Cancel" button</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowCustomDialog(string title, string message, DialogItem[] dialogItems,
            string callbackGameObject, string resultCallbackMethod, string cancelCallbackMethod, 
            bool resultIsJson, string okCaption, string cancelCaption, string style)
        {
            if (dialogItems == null || dialogItems.Length == 0)
                return;

            string[] jsons = dialogItems.Select(e => JsonUtility.ToJson(e)).ToArray();
            if (jsons == null || jsons.Length == 0)
                return;

            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_CUSTOM_DIALOG);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "showCustomDialog",
                    context,
                    title,
                    message,
                    jsons,
                    callbackGameObject,
                    resultCallbackMethod,
                    cancelCallbackMethod,
                    resultIsJson,
                    okCaption,
                    cancelCaption,
                    style
                );
            }));
        }

        //(*) Argument omission overload
        //※引数省略 オーバーロード
        /// <summary>
        /// Call Android Custom Dialog
        ///･Dialog where freely add Text, Switch, Slider, Toggle buttons and Dividing lines.
        ///･The return value is a pair of values ("key=value" + line feed ("\n")) or JSON format ("{"key":"value"}") for a key set for each item (resultIsJson=true:JSON).
        ///･When "Cancel", or clicked outside the dialog (-> back to application) return nothing.
        ///･The parameter (dialogItems) of each item (Widgets: DivisorItem, TextItem, SwitchItem, SliderItem, ToggleItem) in one array.
        ///･Generation of each widget is arranged in order from the top. Ignored if there are invalid parameters (or no dialog is generated)
        ///(*) When use the message string, Notice that it will not be displayed if the items overflow the dialog (-> message="").
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// カスタマイズできるダイアログ：Android の AlertDialog を使用する
        ///・テキスト、スイッチ、スライダー、トグルボタン、分割線を自由に設置できるダイアログ。
        ///・戻り値は各アイテム（ウィジェット）に設定したキーに対して値のペア（"key=value"＋改行("\n")）または JSON形式（{"key":"value"}※ダブルクォートは値による）で返される。
        ///・"Cancel" または 何も押されない（ダイアログ外クリック→元の画面に戻った）ときは何も返さない（何もしない）。
        ///・各アイテムのパラメタ（DialogItem）は各ウィジェット用のサブクラス（DivisorItem, TextItem, SwitchItem, SliderItem, ToggleItem）を１つの配列で渡す。
        ///・各ウィジェットの生成は上から順に配置される。不正なパラメタがあった場合は無視される（もしくはダイアログが生成されない）。
        ///※メッセージ文字列（message）、決定ボタンはアイテムがダイアログに収まらないとき表示されないので注意（スクロール表示に専有される）
        ///  → メッセージを無くす（空文字("")にする）とボタンのみ表示できるようになる：Androidダイアログの仕様(?)。
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="message">Message string ("" = nothing)</param>
        /// <param name="dialogItems">Parameters of each item (widget) (Array)</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="callbackMethod">Method name to callback (it is in GameObject)</param>
        /// <param name="resultIsJson">return value in: true=JSON format / false="key=value\n"</param>
        /// <param name="okCaption">String of "OK" button</param>
        /// <param name="cancelCaption">String of "Cancel" button</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowCustomDialog(string title, string message, DialogItem[] dialogItems,
            string callbackGameObject, string callbackMethod, bool resultIsJson = false,
            string okCaption = "OK", string cancelCaption = "Cancel", string style = "")
        {
            ShowCustomDialog(title, message, dialogItems, 
                callbackGameObject, callbackMethod, "",
                resultIsJson, okCaption, cancelCaption, style);
        }




        //==========================================================
        // Text Input Dialogs
        // テキスト入力ダイアログ
        //==========================================================

        /// <summary>
        /// Call Android Single line text input Dialog
        /// http://fantom1x.blog130.fc2.com/blog-entry-276.html#fantomPlugin_SingleLineTextDialog
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// 単一行テキスト入力ダイアログを使用する
        /// http://fantom1x.blog130.fc2.com/blog-entry-276.html#fantomPlugin_SingleLineTextDialog
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="message">Message string</param>
        /// <param name="text">Initial value of text string</param>
        /// <param name="maxLength">Character limit (0 = no limit)</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="callbackMethod">Method name to callback (it is in GameObject)</param>
        /// <param name="okCaption">String of "OK" button</param>
        /// <param name="cancelCaption">String of "Cancel" button</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowSingleLineTextDialog(string title, string message, string text, int maxLength,
            string callbackGameObject, string callbackMethod,
            string okCaption = "OK", string cancelCaption = "Cancel", string style = "")
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "showSingleLineTextDialog",
                    context,
                    title,
                    message,
                    text,
                    maxLength,
                    callbackGameObject,
                    callbackMethod,
                    okCaption,
                    cancelCaption,
                    style
                );
            }));
        }


        /// <summary>
        /// Call Android Single line text input Dialog (no message overload)
        /// http://fantom1x.blog130.fc2.com/blog-entry-276.html#fantomPlugin_SingleLineTextDialog
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// 単一行テキスト入力ダイアログ（メッセージなし）を使用する
        /// http://fantom1x.blog130.fc2.com/blog-entry-276.html#fantomPlugin_SingleLineTextDialog
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="text">Initial value of text string</param>
        /// <param name="maxLength">Character limit (0 = no limit)</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="callbackMethod">Method name to callback (it is in GameObject)</param>
        /// <param name="okCaption">String of "OK" button</param>
        /// <param name="cancelCaption">String of "Cancel" button</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowSingleLineTextDialog(string title, string text, int maxLength,
            string callbackGameObject, string callbackMethod,
            string okCaption = "OK", string cancelCaption = "Cancel", string style = "")
        {
            ShowSingleLineTextDialog(title, "", text, maxLength, callbackGameObject, callbackMethod, okCaption, cancelCaption, style);
        }



        /// <summary>
        /// Call Android Multi line text input Dialog
        /// http://fantom1x.blog130.fc2.com/blog-entry-276.html#fantomPlugin_MultiLineTextDialog
        ///･Text entry to include line breaks. The line feed code of the return value is unified to "\n".
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// 複数行テキスト入力ダイアログを使用する
        /// http://fantom1x.blog130.fc2.com/blog-entry-276.html#fantomPlugin_MultiLineTextDialog
        ///・改行を含めるテキスト入力。戻り値の改行コードは "\n" に統一される。
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="message">Message string</param>
        /// <param name="text">Initial value of text string</param>
        /// <param name="maxLength">Character limit (0 = no limit)</param>
        /// <param name="lines">Number of display lines</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="callbackMethod">Method name to callback (it is in GameObject)</param>
        /// <param name="okCaption">String of "OK" button</param>
        /// <param name="cancelCaption">String of "Cancel" button</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowMultiLineTextDialog(string title, string message, string text, int maxLength, int lines,
            string callbackGameObject, string callbackMethod,
            string okCaption = "OK", string cancelCaption = "Cancel", string style = "")
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "showMultiLineTextDialog",
                    context,
                    title,
                    message,
                    text,
                    maxLength,
                    lines,
                    callbackGameObject,
                    callbackMethod,
                    okCaption,
                    cancelCaption,
                    style
                );
            }));
        }


        /// <summary>
        /// Call Android Multi line text input Dialog (no message overload)
        /// http://fantom1x.blog130.fc2.com/blog-entry-276.html#fantomPlugin_MultiLineTextDialog
        ///･Text entry to include line breaks. The line feed code of the return value is unified to "\n".
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// 複数行テキスト入力ダイアログ（メッセージなし）を使用する
        /// http://fantom1x.blog130.fc2.com/blog-entry-276.html#fantomPlugin_MultiLineTextDialog
        ///・改行を含めるテキスト入力。戻り値の改行コードは "\n" に統一される。
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="text">Initial value of text string</param>
        /// <param name="maxLength">Character limit (0 = no limit)</param>
        /// <param name="lines">Number of display lines</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="callbackMethod">Method name to callback (it is in GameObject)</param>
        /// <param name="okCaption">String of "OK" button</param>
        /// <param name="cancelCaption">String of "Cancel" button</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowMultiLineTextDialog(string title, string text, int maxLength, int lines,
            string callbackGameObject, string callbackMethod,
            string okCaption = "OK", string cancelCaption = "Cancel", string style = "")
        {
            ShowMultiLineTextDialog(title, "", text, maxLength, lines, callbackGameObject, callbackMethod, okCaption, cancelCaption, style);
        }



        /// <summary>
        /// Call Android Numeric text input Dialog
        /// http://fantom1x.blog130.fc2.com/blog-entry-277.html#fantomPlugin_NumericTextDialog
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// 数値入力のテキストダイアログを使用する
        /// http://fantom1x.blog130.fc2.com/blog-entry-277.html#fantomPlugin_NumericTextDialog
        ///※内部的には float 型で処理されるため、整数の場合 6～7桁くらいまでしか利用できない（桁数が大きいと "1.0+E09" のようになるため）。
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="message">Message string</param>
        /// <param name="defValue">Initial value</param>
        /// <param name="maxLength">Character limit (0 = no limit) ([*]Including decimal point and sign)</param>
        /// <param name="enableDecimal">true=decimal possible / false=integer only</param>
        /// <param name="enableSign">Possible to input a sign ('-' or '+') at the beginning</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="callbackMethod">Method name to callback (it is in GameObject)</param>
        /// <param name="okCaption">String of "OK" button</param>
        /// <param name="cancelCaption">String of "Cancel" button</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowNumericTextDialog(string title, string message, float defValue, int maxLength, bool enableDecimal, bool enableSign,
            string callbackGameObject, string callbackMethod,
            string okCaption = "OK", string cancelCaption = "Cancel", string style = "")
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "showNumericTextDialog",
                    context,
                    title,
                    message,
                    defValue,
                    maxLength,
                    enableDecimal,
                    enableSign,
                    callbackGameObject,
                    callbackMethod,
                    okCaption,
                    cancelCaption,
                    style
                );
            }));
        }


        /// <summary>
        /// Call Android Numeric text input Dialog (no message overload)
        /// http://fantom1x.blog130.fc2.com/blog-entry-277.html#fantomPlugin_NumericTextDialog
        ///(*) Since it is processed internally as a float type, it can only be used up to about 6 to 7 digits in the case of an integer (as the number of digits is large, it will be like "1.0+E09").
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// 数値入力のテキストダイアログ（メッセージなし）を使用する
        ///※内部的には float 型で処理されるため、整数の場合 6～7桁くらいまでしか利用できない（桁数が大きいと "1.0+E09" のようになるため）。
        /// http://fantom1x.blog130.fc2.com/blog-entry-277.html#fantomPlugin_NumericTextDialog
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="defValue">Initial value</param>
        /// <param name="maxLength">Character limit (0 = no limit) ([*]Including decimal point and sign)</param>
        /// <param name="enableDecimal">true=decimal possible / false=integer only</param>
        /// <param name="enableSign">Possible to input a sign ('-' or '+') at the beginning</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="callbackMethod">Method name to callback (it is in GameObject)</param>
        /// <param name="okCaption">String of "OK" button</param>
        /// <param name="cancelCaption">String of "Cancel" button</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowNumericTextDialog(string title, float defValue, int maxLength, bool enableDecimal, bool enableSign,
            string callbackGameObject, string callbackMethod,
            string okCaption = "OK", string cancelCaption = "Cancel", string style = "")
        {
            ShowNumericTextDialog(title, "", defValue, maxLength, enableDecimal, enableSign, callbackGameObject, callbackMethod, okCaption, cancelCaption, style);
        }



        /// <summary>
        /// Call Android Alpha Numeric text input Dialog
        /// http://fantom1x.blog130.fc2.com/blog-entry-277.html#fantomPlugin_AlphaNumericTextDialog
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// 半角英数入力のテキストダイアログを使用する
        /// http://fantom1x.blog130.fc2.com/blog-entry-277.html#fantomPlugin_AlphaNumericTextDialog
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="message">Message string</param>
        /// <param name="text">Initial value of text string</param>
        /// <param name="maxLength">Character limit (0 = no limit)。</param>
        /// <param name="addChars">Additional character lists such as symbols ("_-.@": each character, "" = nothing)</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="callbackMethod">Method name to callback (it is in GameObject)</param>
        /// <param name="okCaption">String of "OK" button</param>
        /// <param name="cancelCaption">String of "Cancel" button</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowAlphaNumericTextDialog(string title, string message, string text, int maxLength, string addChars,
            string callbackGameObject, string callbackMethod,
            string okCaption = "OK", string cancelCaption = "Cancel", string style = "")
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "showAlphaNumericTextDialog",
                    context,
                    title,
                    message,
                    text,
                    maxLength,
                    addChars,
                    callbackGameObject,
                    callbackMethod,
                    okCaption,
                    cancelCaption,
                    style
                );
            }));
        }


        /// <summary>
        /// Call Android Alpha Numeric text input Dialog (no message overload)
        /// http://fantom1x.blog130.fc2.com/blog-entry-277.html#fantomPlugin_AlphaNumericTextDialog
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// 半角英数入力のテキストダイアログ（メッセージなし）を使用する
        /// http://fantom1x.blog130.fc2.com/blog-entry-277.html#fantomPlugin_AlphaNumericTextDialog
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="text">Initial value of text string</param>
        /// <param name="maxLength">Character limit (0 = no limit)。</param>
        /// <param name="addChars">Additional character lists such as symbols ("_-.@": each character, "" = nothing)</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="callbackMethod">Method name to callback (it is in GameObject)</param>
        /// <param name="okCaption">String of "OK" button</param>
        /// <param name="cancelCaption">String of "Cancel" button</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowAlphaNumericTextDialog(string title, string text, int maxLength, string addChars,
            string callbackGameObject, string callbackMethod,
            string okCaption = "OK", string cancelCaption = "Cancel", string style = "")
        {
            ShowAlphaNumericTextDialog(title, "", text, maxLength, addChars, callbackGameObject, callbackMethod, okCaption, cancelCaption, style);
        }



        /// <summary>
        /// Call Android Password text input Dialog
        /// http://fantom1x.blog130.fc2.com/blog-entry-277.html#fantomPlugin_PasswordTextDialog
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// パスワード入力のテキストダイアログを使用する
        /// http://fantom1x.blog130.fc2.com/blog-entry-277.html#fantomPlugin_PasswordTextDialog
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="message">Message string</param>
        /// <param name="text">Initial value of text string</param>
        /// <param name="maxLength">Character limit (0 = no limit)。</param>
        /// <param name="numberOnly">true=numeric only</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="callbackMethod">Method name to callback (it is in GameObject)</param>
        /// <param name="okCaption">String of "OK" button</param>
        /// <param name="cancelCaption">String of "Cancel" button</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowPasswordTextDialog(string title, string message, string text, int maxLength, bool numberOnly,
            string callbackGameObject, string callbackMethod,
            string okCaption = "OK", string cancelCaption = "Cancel", string style = "")
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "showPasswordTextDialog",
                    context,
                    title,
                    message,
                    text,
                    maxLength,
                    numberOnly,
                    callbackGameObject,
                    callbackMethod,
                    okCaption,
                    cancelCaption,
                    style
                );
            }));
        }


        /// <summary>
        /// Call Android Password text input Dialog (no message overload)
        /// http://fantom1x.blog130.fc2.com/blog-entry-277.html#fantomPlugin_PasswordTextDialog
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// パスワード入力のテキストダイアログ（メッセージなし）を使用する
        /// http://fantom1x.blog130.fc2.com/blog-entry-277.html#fantomPlugin_PasswordTextDialog
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="text">Initial value of text string</param>
        /// <param name="maxLength">Character limit (0 = no limit)。</param>
        /// <param name="numberOnly">true=numeric only</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="callbackMethod">Method name to callback (it is in GameObject)</param>
        /// <param name="okCaption">String of "OK" button</param>
        /// <param name="cancelCaption">String of "Cancel" button</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowPasswordTextDialog(string title, string text, int maxLength, bool numberOnly,
            string callbackGameObject, string callbackMethod,
            string okCaption = "OK", string cancelCaption = "Cancel", string style = "")
        {
            ShowPasswordTextDialog(title, "", text, maxLength, numberOnly, callbackGameObject, callbackMethod, okCaption, cancelCaption, style);
        }



        //==========================================================
        // Picker Dialogs
        // Picker 系ダイアログ
        //==========================================================

        /// <summary>
        /// Call Android DatePicker Dialog
        /// http://fantom1x.blog130.fc2.com/blog-entry-278.html#fantomPlugin_DataPickerDialog
        /// When pressed the "OK" button, the date is returned as a callback with the specified format (resultDateFormat).
        ///･When pressed the "Cancel" button or clicked outside the dialog (-> back to application) return nothing.
        /// (Date format [Android-Java])
        /// https://developer.android.com/reference/java/text/SimpleDateFormat.html
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// 日付選択ダイアログ：Android の DatePickerDialog を使用する
        /// http://fantom1x.blog130.fc2.com/blog-entry-278.html#fantomPlugin_DataPickerDialog
        ///・OKボタンによって、日付が指定フォーマット（resultDateFormat）で callbackGameObject の callbackMethod に返ってくる。
        ///・Cancel または、何もしない（ダイアログ外をクリック→元の画面に戻った）ときは何も返さない。
        /// (日時の書式)
        /// https://developer.android.com/reference/java/text/SimpleDateFormat.html
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="defaultDate">Initial value of date string (like "2017/01/31", "17/1/1")</param>
        /// <param name="resultDateFormat">Return date format (default: "yyyy/MM/dd"->"2017/01/03", e.g. "yy-M-d"->"17-1-3" [Android-Java])</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="callbackMethod">Method name to callback (it is in GameObject)</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowDatePickerDialog(string defaultDate, string resultDateFormat, 
            string callbackGameObject, string callbackMethod, string style = "")
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "showDatePickerDialog",
                    context,
                    defaultDate,
                    resultDateFormat,
                    callbackGameObject,
                    callbackMethod,
                    style
                );
            }));
        }


        /// <summary>
        /// Call Android TimePicker Dialog
        /// http://fantom1x.blog130.fc2.com/blog-entry-278.html#fantomPlugin_TimePickerDialog
        /// When pressed the "OK" button, the time is returned as a callback with the specified format (resultTimeFormat).
        ///･When pressed the "Cancel" button or clicked outside the dialog (-> back to application) return nothing.
        /// (Time format [Android-Java])
        /// https://developer.android.com/reference/java/text/SimpleDateFormat.html
        /// (Theme)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// 
        /// 
        /// 時刻選択ダイアログ：Android の TimePickerDialog を使用する
        /// http://fantom1x.blog130.fc2.com/blog-entry-278.html#fantomPlugin_TimePickerDialog
        ///・OKボタンによって、時刻が指定フォーマット（resultTimeFormat）で callbackGameObject の callbackMethod に返ってくる。
        ///・Cancel または、何もしない（ダイアログ外をクリック→元の画面に戻った）ときは何も返さない。
        /// (日時の書式)
        /// https://developer.android.com/reference/java/text/SimpleDateFormat.html
        /// (テーマ)
        /// https://developer.android.com/reference/android/R.style.html#Theme
        /// </summary>
        /// <param name="defaultTime">Initial value of time string (like "0:00"~"23:59")</param>
        /// <param name="resultTimeFormat">Return time format (default: "HH:mm"->"03:05", e.g. "H:mm"->"3:05" [Android-Java])</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="callbackMethod">Method name to callback (it is in GameObject)</param>
        /// <param name="style">Style applied to dialog (Theme: "android:Theme.DeviceDefault.Dialog.Alert", "android:Theme.DeviceDefault.Light.Dialog.Alert" or etc)</param>
        public static void ShowTimePickerDialog(string defaultTime, string resultTimeFormat,
            string callbackGameObject, string callbackMethod, string style = "")
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "showTimePickerDialog",
                    context,
                    defaultTime,
                    resultTimeFormat,
                    callbackGameObject,
                    callbackMethod,
                    style
                );
            }));
        }




        //==========================================================
        // Notification
        // 通知
        //==========================================================

        /// <summary>
        /// Call Android Notification
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_Notification
        ///(*) Icon in Unity is fixed with resource name "app_icon".
        ///･Put the duration in the order of the vibrator pattern array (off, on, off, on, ...) (unit: ms [millisecond = 1/1000 seconds])) / null = none
        ///
        /// 
        /// Android の Notification（通知）を使用する（表示のみ）
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_Notification
        ///※Unity でのアイコンはリソース名"app_icon"で固定（※マニフェストファイルで書き換えない限り）
        ///・バイブレーターのパターン配列は：off, on, off, on,... の順に長さを入れる（単位：ms[ミリセカンド=1/1000秒]）/ null = 無し
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="message">Message string</param>
        /// <param name="iconName">Icon resource name (Unity's default is "app_icon")</param>
        /// <param name="tag">Identification tag (The same tag is overwritten when notified consecutively)</param>
        /// <param name="showTimestamp">Add notification time display</param>
        /// <param name="vibratorPattern">Array of duration pattern / null = none</param>
        public static void ShowNotification(string title, string message, string iconName, string tag, 
            bool showTimestamp, long[] vibratorPattern)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "showNotification",
                    context,
                    title,
                    message,
                    iconName,
                    tag,
                    showTimestamp,
                    vibratorPattern
                );
            }));
        }


        //(*) OneShot (one time only) vibrator overload
        //※OneShot（1回のみ）のバイブレーターオーバーロード
        /// <summary>
        /// Call Android Notification
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_Notification
        ///(*) Icon in Unity is fixed with resource name "app_icon".
        ///･Put the duration in the order of the vibrator pattern array (off, on, off, on, ...) (unit: ms [millisecond = 1/1000 seconds])) / null = none
        ///
        /// 
        /// Android の Notification（通知）を使用する（表示のみ）
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_Notification
        /// ※Unity でのアイコンはリソース名"app_icon"で固定（※マニフェストファイルで書き換えない限り）
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="message">Message string</param>
        /// <param name="iconName">Icon resource name (Unity's default is "app_icon")</param>
        /// <param name="tag">Identification tag (The same tag is overwritten when notified consecutively)</param>
        /// <param name="showTimestamp">Add notification time display</param>
        /// <param name="vibratorDuration">Duration of vibration</param>
        public static void ShowNotification(string title, string message, string iconName, string tag, 
            bool showTimestamp, long vibratorDuration)
        {
            long[] pattern = vibratorDuration > 0 ? new long[] { 0, vibratorDuration } : null;
            ShowNotification(title, message, iconName, tag, showTimestamp, pattern);
        }


        //(*) No vibrator overload
        //※バイブレーター無しオーバーロード
        /// <summary>
        /// Call Android Notification
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_Notification
        ///(*) Icon in Unity is fixed with resource name "app_icon".
        ///
        /// 
        /// Android の Notification（通知）を使用する（表示のみ）
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_Notification
        /// ※Unity でのアイコンはリソース名"app_icon"で固定（※マニフェストファイルで書き換えない限り）
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="message">Message string</param>
        /// <param name="iconName">Icon resource name (Unity's default is "app_icon")</param>
        /// <param name="tag">Identification tag (The same tag is overwritten when notified consecutively)</param>
        /// <param name="showTimestamp">Add notification time display</param>
        public static void ShowNotification(string title, string message, string iconName = "app_icon", string tag = "tag", 
            bool showTimestamp = true)
        {
            ShowNotification(title, message, iconName, tag, showTimestamp, null);
        }



        /// <summary>
        /// Call Android Notification (Tap to take action to URI)
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_NotificationToOpenURL
        ///･(Action: Constant Value)
        /// https://developer.android.com/reference/android/content/Intent.html#ACTION_VIEW
        ///(*) Icon in Unity is fixed with resource name "app_icon".
        ///･Put the duration in the order of the vibrator pattern array (off, on, off, on, ...) (ms [millisecond = 1/1000 seconds]) / null = none
        ///
        /// 
        /// Android の Notification（通知）を使用する（タップでアクションを起こす）
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_NotificationToOpenURL
        ///・アクションは以下を参照（Constant Value を使う）
        /// https://developer.android.com/reference/android/content/Intent.html#ACTION_VIEW
        /// ※Unity でのアイコンはリソース名"app_icon"で固定（※マニフェストファイルで書き換えない限り）
        ///・バイブレーターのパターン配列は：off, on, off, on,... の順に長さを入れる（単位：ms[ミリセカンド=1/1000秒]）/ null = 無し
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="message">Message string</param>
        /// <param name="iconName">Icon resource name (Unity's default is "app_icon")</param>
        /// <param name="tag">Identification tag (The same tag is overwritten when notified consecutively)</param>
        /// <param name="action">String of Action (e.g. "android.intent.action.VIEW")</param>
        /// <param name="uri">URI to action (URL etc.)</param>
        /// <param name="showTimestamp">Add notification time display</param>
        /// <param name="vibratorPattern">Array of duration pattern / null = none</param>
        public static void ShowNotificationToActionURI(string title, string message, string iconName, string tag,
            string action, string uri, bool showTimestamp, long[] vibratorPattern)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "showNotificationToActionURI",
                    context,
                    title,
                    message,
                    iconName,
                    tag,
                    action,
                    uri,
                    showTimestamp,
                    vibratorPattern
                );
            }));
        }


        //(*) OneShot (one time only) vibrator overload
        //※OneShot（1回のみ）のバイブレーターオーバーロード
        /// <summary>
        /// Call Android Notification (Tap to take action to URI)
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_NotificationToOpenURL
        ///･(Action: Constant Value)
        /// https://developer.android.com/reference/android/content/Intent.html#ACTION_VIEW
        ///(*) Icon in Unity is fixed with resource name "app_icon".
        ///
        /// 
        /// Android の Notification（通知）を使用する（タップでアクションを起こす）
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_NotificationToOpenURL
        ///・アクションは以下を参照（Constant Value を使う）
        /// https://developer.android.com/reference/android/content/Intent.html#ACTION_VIEW
        /// ※Unity でのアイコンはリソース名"app_icon"で固定（※マニフェストファイルで書き換えない限り）
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="message">Message string</param>
        /// <param name="iconName">Icon resource name (Unity's default is "app_icon")</param>
        /// <param name="tag">Identification tag (The same tag is overwritten when notified consecutively)</param>
        /// <param name="action">String of Action (e.g. "android.intent.action.VIEW")</param>
        /// <param name="uri">URI to action (URL etc.)</param>
        /// <param name="showTimestamp">Add notification time display</param>
        /// <param name="vibratorDuration">Duration of vibration (ms [millisecond = 1/1000 seconds])</param>
        public static void ShowNotificationToActionURI(string title, string message, string iconName, string tag,
            string action, string uri, bool showTimestamp, long vibratorDuration)
        {
            long[] pattern = vibratorDuration > 0 ? new long[] { 0, vibratorDuration } : null;
            ShowNotificationToActionURI(title, message, iconName, tag, action, uri, showTimestamp, pattern);
        }


        //(*) No vibrator overload
        //※バイブレーター無しオーバーロード
        /// <summary>
        /// Call Android Notification (Tap to take action to URI)
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_NotificationToOpenURL
        ///･(Action: Constant Value)
        /// https://developer.android.com/reference/android/content/Intent.html#ACTION_VIEW
        ///(*) Icon in Unity is fixed with resource name "app_icon".
        ///
        /// 
        /// Android の Notification（通知）を使用する（タップでアクションを起こす）
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_NotificationToOpenURL
        ///・アクションは以下を参照（Constant Value を使う）
        /// https://developer.android.com/reference/android/content/Intent.html#ACTION_VIEW
        /// ※Unity でのアイコンはリソース名"app_icon"で固定（※マニフェストファイルで書き換えない限り）
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="message">Message string</param>
        /// <param name="iconName">Icon resource name (Unity's default is "app_icon")</param>
        /// <param name="tag">Identification tag (The same tag is overwritten when notified consecutively)</param>
        /// <param name="action">String of Action (e.g. "android.intent.action.VIEW")</param>
        /// <param name="uri">URI to action (URL etc.)</param>
        /// <param name="showTimestamp">Add notification time display</param>
        public static void ShowNotificationToActionURI(string title, string message, string iconName = "app_icon", string tag = "tag",
            string action = "android.intent.action.VIEW", string uri = "", bool showTimestamp = true)
        {
            ShowNotificationToActionURI(title, message, iconName, tag, action, uri, showTimestamp, null);
        }



        /// <summary>
        /// Call Android Notification (Tap to take action to open URL)
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_NotificationToOpenURL
        ///･Put the duration in the order of the vibrator pattern array (off, on, off, on, ...) (unit: ms [millisecond = 1/1000 seconds])) / null = none
        ///
        /// 
        /// Android の Notification（通知）を使用する（タップでデフォルトのブラウザでURLを開く）
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_NotificationToOpenURL
        ///・ShowNotificationToActionURI() のショートカット。
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="message">Message string</param>
        /// <param name="url">URL to open in browser</param>
        /// <param name="tag">Identification tag (The same tag is overwritten when notified consecutively)</param>
        /// <param name="showTimestamp">Add notification time display</param>
        /// <param name="vibratorPattern">Array of duration pattern / null = none</param>
        public static void ShowNotificationToOpenURL(string title, string message, string url, string tag, 
            bool showTimestamp, long[] vibratorPattern)
        {
            if (string.IsNullOrEmpty(url))
                return;

            ShowNotificationToActionURI(title, message, "app_icon", tag, "android.intent.action.VIEW", url, 
                showTimestamp, vibratorPattern);
        }


        //(*) OneShot (one time only) vibrator overload
        //※OneShot（1回のみ）のバイブレーターオーバーロード
        /// <summary>
        /// Call Android Notification (Tap to take action to open URL)
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_NotificationToOpenURL
        /// 
        /// 
        /// Android の Notification（通知）を使用する（タップでデフォルトのブラウザでURLを開く）
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_NotificationToOpenURL
        ///・ShowNotificationToActionURI() のショートカット。
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="message">Message string</param>
        /// <param name="url">URL to open in browser</param>
        /// <param name="tag">Identification tag (The same tag is overwritten when notified consecutively)</param>
        /// <param name="showTimestamp">Add notification time display</param>
        /// <param name="vibratorDuration">Duration of vibration</param>
        public static void ShowNotificationToOpenURL(string title, string message, string url, string tag, 
            bool showTimestamp, long vibratorDuration)
        {
            if (string.IsNullOrEmpty(url))
                return;

            long[] pattern = vibratorDuration > 0 ? new long[] { 0, vibratorDuration } : null;
            ShowNotificationToActionURI(title, message, "app_icon", tag, "android.intent.action.VIEW", url, 
                showTimestamp, pattern);
        }


        //(*) No vibrator overload
        //※バイブレーター無しオーバーロード
        /// <summary>
        /// Call Android Notification (Tap to take action to open URL)
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_NotificationToOpenURL
        /// 
        /// 
        /// Android の Notification（通知）を使用する（タップでデフォルトのブラウザでURLを開く）
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_NotificationToOpenURL
        ///・ShowNotificationToActionURI() のショートカット。
        /// </summary>
        /// <param name="title">Title string</param>
        /// <param name="message">Message string</param>
        /// <param name="url">URL to open in browser</param>
        /// <param name="tag">Identification tag (The same tag is overwritten when notified consecutively)</param>
        /// <param name="showTimestamp">Add notification time display</param>
        public static void ShowNotificationToOpenURL(string title, string message, string url, string tag = "tag",
            bool showTimestamp = true)
        {
            ShowNotificationToActionURI(title, message, "app_icon", tag, "android.intent.action.VIEW", url, 
                showTimestamp, null);
        }




        //==========================================================
        // Vibrator
        //(*) In API 26 (Android 8.0), this function is deprecated, so some devices may not be available.
        //(*) The following permission is necessary to use.
        // '<uses-permission android:name="android.permission.VIBRATE" />' in 'AndroidManifest.xml'
        //
        //
        // バイブレーター
        //※API 26 (Android 8.0) では、この機能は deprecated（廃止予定）となっているため、端末によっては使えないものがあるかもしれません。
        //※利用するには以下のパーミッションが必要。
        // '<uses-permission android:name="android.permission.VIBRATE" />' in 'AndroidManifest.xml'
        //==========================================================

        //(*) Required: '<uses-permission android:name="android.permission.VIBRATE" />' in 'AndroidManifest.xml'
        /// <summary>
        /// Whether the devices supports a vibrator?
        ///(*) It has nothing to do with permissions.
        ///
        /// 
        /// 端末がバイブレーターをサポートしているか否か？
        ///※パーミッションとは関係ありません。
        /// </summary>
        /// <returns>true = supported</returns>
        public static bool IsSupportedVibrator()
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
                    {
                        return androidSystem.CallStatic<bool>(
                            "hasVibrator",
                            context
                        );
                    }
                }
            }
        }


        //(*) Required: '<uses-permission android:name="android.permission.VIBRATE" />' in 'AndroidManifest.xml'
        /// <summary>
        /// Vibrate the vibrator with a pattern.
        ///･Put the duration in the order of the vibrator pattern array (off, on, off, on, ...) (unit: ms [millisecond = 1/1000 seconds])) / null = none
        ///
        /// 
        /// バイブレーターをパターンで振動させる。
        ///・振動パターンは長さ（単位：ms[ミリセカンド=1/1000秒]）で、off, on, off, on,... の順に配列に入れる。
        /// </summary>
        /// <param name="pattern">Array of duration pattern / null = none</param>
        /// <param name="isLoop">true = do loop</param>
        public static void StartVibrator(long[] pattern, bool isLoop = false)
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
                    {
                        androidSystem.CallStatic(
                            "startVibrator",
                            context,
                            pattern,
                            isLoop
                        );
                    }
                }
            }
        }


        //(*) Required: '<uses-permission android:name="android.permission.VIBRATE" />' in 'AndroidManifest.xml'
        /// <summary>
        /// Vibrate the vibrator only once.
        /// 
        /// バイブレーターを１度だけ振動させる。
        /// </summary>
        /// <param name="duration">Duration of vibration (ms [millisecond = 1/1000 seconds])</param>
        public static void StartVibrator(long duration)
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
                    {
                        androidSystem.CallStatic(
                            "startVibrator",
                            context,
                            duration
                        );
                    }
                }
            }
        }


        //(*) Required: '<uses-permission android:name="android.permission.VIBRATE" />' in 'AndroidManifest.xml'
        /// <summary>
        /// Interrupt vibration of the vibrator
        /// 
        /// バイブレーターの振動を中断させる
        /// </summary>
        public static void CancelVibrator()
        {
            using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
            {
                androidSystem.CallStatic(
                    "cancelVibrator"
                );
            }
        }





        //==========================================================
        // Start something action that does not return result value
        // 投げっぱなしのアクションなどの起動
        //==========================================================

        //(*) No argument overload
        //※引数無しオーバーロード
        /// <summary>
        /// Start something Action (no return value)
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_OpenURL
        ///･(Action: Constant Value)
        /// https://developer.android.com/reference/android/content/Intent.html#ACTION_VIEW
        /// https://developer.android.com/reference/android/provider/Settings.html#ACTION_ACCESSIBILITY_SETTINGS
        /// 
        /// 
        /// アクティビティからのアクション起動（戻値はなし）
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_OpenURL
        ///・アクションは以下を参照（Constant Value を使う）
        /// https://developer.android.com/reference/android/content/Intent.html#ACTION_VIEW
        /// https://developer.android.com/reference/android/provider/Settings.html#ACTION_ACCESSIBILITY_SETTINGS
        /// </summary>
        /// <param name="action">Starting of Action (e.g. "android.settings.SETTINGS")</param>
        public static void StartAction(string action)
        {
            StartAction(action, "", "");
        }


        /// <summary>
        /// Start something Action (no return value)
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_OpenURL
        ///･(Action: Constant Value)
        /// https://developer.android.com/reference/android/content/Intent.html#ACTION_VIEW
        /// https://developer.android.com/reference/android/provider/Settings.html#ACTION_ACCESSIBILITY_SETTINGS
        /// 
        /// 
        /// アクティビティからのアクション起動（戻値はなし）
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_OpenURL
        ///・アクションは以下を参照（Constant Value を使う）
        /// https://developer.android.com/reference/android/content/Intent.html#ACTION_VIEW
        /// https://developer.android.com/reference/android/provider/Settings.html#ACTION_ACCESSIBILITY_SETTINGS
        /// </summary>
        /// <param name="action">Starting of Action (e.g. "android.intent.action.WEB_SEARCH")</param>
        /// <param name="extra">Parameter name to give to the Action (e.g. "query")</param>
        /// <param name="query">Value to give to the Action</param>
        public static void StartAction(string action, string extra, string query)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "startAction",
                    context,
                    action,
                    extra,
                    query
                );
            }));
        }


        //(*) multiple parameter overload
        //※複数パラメタオーバーロード
        /// <summary>
        /// Start something Action
        ///･(Action: Constant Value)
        /// https://developer.android.com/reference/android/content/Intent.html#ACTION_VIEW
        /// https://developer.android.com/reference/android/provider/Settings.html#ACTION_ACCESSIBILITY_SETTINGS
        /// 
        /// 
        /// アクティビティからのアクション起動（戻値はなし）
        ///・アクションは以下を参照（Constant Value を使う）
        /// https://developer.android.com/reference/android/content/Intent.html#ACTION_VIEW
        /// https://developer.android.com/reference/android/provider/Settings.html#ACTION_ACCESSIBILITY_SETTINGS
        /// </summary>
        /// <param name="action">Starting of Action (e.g. "android.intent.action.WEB_SEARCH")</param>
        /// <param name="extra">Parameter name to give to the Action (e.g. "query")</param>
        /// <param name="query">Value to give to the Action</param>
        public static void StartAction(string action, string[] extra, string[] query)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "startAction",
                    context,
                    action,
                    extra,
                    query
                );
            }));
        }


        //(*) add MIME Type overload
        //※MIME Type 追加オーバーロード
        /// <summary>
        /// Start something Action (no return value)
        ///･(Action: Constant Value)
        /// https://developer.android.com/reference/android/content/Intent.html#ACTION_VIEW
        /// https://developer.android.com/reference/android/provider/Settings.html#ACTION_ACCESSIBILITY_SETTINGS
        /// 
        /// 
        /// アクティビティからのアクション起動（戻値はなし）
        ///・アクションは以下を参照（Constant Value を使う）
        /// https://developer.android.com/reference/android/content/Intent.html#ACTION_VIEW
        /// https://developer.android.com/reference/android/provider/Settings.html#ACTION_ACCESSIBILITY_SETTINGS
        /// </summary>
        /// <param name="action">Starting of Action (e.g. "android.intent.action.SEND")</param>
        /// <param name="extra">Parameter name to give to the Action (e.g. "android.intent.extra.TEXT")</param>
        /// <param name="query">Value to give to the Action</param>
        /// <param name="mimetype">MIME Type (e.g. "text/plain")</param>
        public static void StartAction(string action, string extra, string query, string mimetype)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "startAction",
                    context,
                    action,
                    extra,
                    query,
                    mimetype
                );
            }));
        }


        //(*) add MIME Type overload
        //※MIME Type 追加オーバーロード
        /// <summary>
        /// Start something Action (no return value)
        ///･(Action: Constant Value)
        /// https://developer.android.com/reference/android/content/Intent.html#ACTION_VIEW
        /// https://developer.android.com/reference/android/provider/Settings.html#ACTION_ACCESSIBILITY_SETTINGS
        /// 
        /// 
        /// アクティビティからのアクション起動（戻値はなし）
        ///・アクションは以下を参照（Constant Value を使う）
        /// https://developer.android.com/reference/android/content/Intent.html#ACTION_VIEW
        /// https://developer.android.com/reference/android/provider/Settings.html#ACTION_ACCESSIBILITY_SETTINGS
        /// </summary>
        /// <param name="action">Starting of Action (e.g. "android.intent.action.SEND")</param>
        /// <param name="extra">Parameter name to give to the Action (e.g. "android.intent.extra.TEXT")</param>
        /// <param name="query">Value to give to the Action</param>
        /// <param name="mimetype">MIME Type (e.g. "text/plain")</param>
        public static void StartAction(string action, string[] extra, string[] query, string mimetype)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "startAction",
                    context,
                    action,
                    extra,
                    query,
                    mimetype
                );
            }));
        }



        /// <summary>
        /// Start action with Chooser (application selection widget)
        /// 
        /// Chooser（アプリ選択ウィジェット）でアクション起動する
        /// </summary>
        /// <param name="action">String of Action (e.g. "android.intent.action.SEND")</param>
        /// <param name="extra">Parameter name to give to the Action (e.g. "android.intent.extra.TEXT")</param>
        /// <param name="query">Value to give to the Action</param>
        /// <param name="mimetype">MIME Type (e.g. "text/plain")</param>
        /// <param name="title">Title to display in Chooser (Empty -> "Select an application")</param>
        public static void StartActionWithChooser(string action, string extra, string query, string mimetype, string title)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "startActionWithChooser",
                    context,
                    action,
                    extra,
                    query,
                    mimetype,
                    title
                );
            }));
        }


        //(*) multiple parameter overload
        //※複数パラメタオーバーロード
        /// <summary>
        /// Start action with Chooser (application selection widget)
        /// 
        /// Chooser（アプリ選択ウィジェット）でアクション起動する
        /// </summary>
        /// <param name="action">String of Action (e.g. "android.intent.action.SEND")</param>
        /// <param name="extra">Parameter name to give to the Action (e.g. "android.intent.extra.TEXT")</param>
        /// <param name="query">Value to give to the Action</param>
        /// <param name="mimetype">MIME Type (e.g. "text/plain")</param>
        /// <param name="title">Title to display in Chooser (Empty -> "Select an application")</param>
        public static void StartActionWithChooser(string action, string[] extra, string[] query, string mimetype, string title)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "startActionWithChooser",
                    context,
                    action,
                    extra,
                    query,
                    mimetype,
                    title
                );
            }));
        }



        /// <summary>
        /// Start Action to URI (no return value)
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_OpenURL
        ///･(Action: Constant Value)
        /// https://developer.android.com/reference/android/content/Intent.html#ACTION_VIEW
        /// 
        /// アクティビティからのURIへのアクション起動（戻値はなし）
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_OpenURL
        ///・アクションは以下を参照（Constant Value を使う）
        /// https://developer.android.com/reference/android/content/Intent.html#ACTION_VIEW
        /// 
        /// (ex)
        /// StartActionURI("android.intent.action.VIEW", "geo:37.7749,-122.4194?q=restaurants");   //Google Map (Search word: restaurants)
        /// StartActionURI("android.intent.action.VIEW", "google.streetview:cbll=29.9774614,31.1329645&cbp=0,30,0,0,-15");   //Street View
        /// StartActionURI("android.intent.action.SENDTO", "mailto:xxx@example.com");   //Launch mailer
        /// https://developers.google.com/maps/documentation/android-api/intents
        /// </summary>
        /// <param name="action">String of Action (e.g. "android.intent.action.VIEW")</param>
        /// <param name="uri">URI to action (URL etc.)</param>
        public static void StartActionURI(string action, string uri)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "startActionURI",
                    context,
                    action,
                    uri
                );
            }));
        }


        //(*) multiple parameter overload
        //※複数パラメタオーバーロード
        /// <summary>
        /// Start Action to URI
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_OpenURL
        ///･(Action: Constant Value)
        /// https://developer.android.com/reference/android/content/Intent.html#ACTION_VIEW
        /// 
        /// 
        /// アクティビティからのURIへのアクション起動（戻値はなし）
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_OpenURL
        ///・アクションは以下を参照（Constant Value を使う）
        /// https://developer.android.com/reference/android/content/Intent.html#ACTION_VIEW
        /// </summary>
        /// <param name="action">String of Action (e.g. "android.intent.action.VIEW")</param>
        /// <param name="uri">URI to action (URL etc.)</param>
        /// <param name="extra">Parameter name to give to the Action (e.g. "android.intent.extra.TEXT")</param>
        /// <param name="query">Value to give to the Action</param>
        public static void StartActionURI(string action, string uri, string[] extra, string[] query)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "startActionURI",
                    context,
                    action,
                    uri,
                    extra,
                    query
                );
            }));
        }



        /// <summary>
        /// Start Action to URI with MIME type (no return value)
        ///･(Action: Constant Value)
        /// https://developer.android.com/reference/android/content/Intent.html#ACTION_VIEW
        /// 
        /// アクティビティから URI に MIME type 指定付きのアクション起動（戻値はなし）
        ///・アクションは以下を参照（Constant Value を使う）
        /// https://developer.android.com/reference/android/content/Intent.html#ACTION_VIEW
        /// </summary>
        /// <param name="action">String of Action (e.g. "android.intent.action.VIEW")</param>
        /// <param name="uri">URI to action (URL etc.)</param>
        /// <param name="mimeType">MIME type ("application/pdf" etc.)</param>
        public static void StartActionURI(string action, string uri, string mimeType)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "startActionURI",
                    context,
                    action,
                    uri,
                    mimeType
                );
            }));
        }



        //==========================================================
        // Below, shortcut method of action
        // 以下、アクションのショートカット的なメソッド
        //==========================================================

        /// <summary>
        /// Open URL (Launch Browser)
        ///･The browser application differs depending on the setting of the system.
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_OpenURL
        ///･Same as StartActionURI("android.intent.action.VIEW", "URL")
        ///
        /// 
        /// URLを開く（起動アプリは端末の設定による）
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_OpenURL
        ///・StartActionURI("android.intent.action.VIEW", "開くURL") と同じ。
        /// </summary>
        /// <param name="url">URL to open in browser</param>
        public static void StartOpenURL(string url)
        {
            StartActionURI("android.intent.action.VIEW", url);
        }


        /// <summary>
        /// Start Web Search
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_OpenURL
        ///･The search application differs depending on the setting of the system.
        ///･Same as StartAction("android.intent.action.WEB_SEARCH", "query", "keyword")
        ///
        /// 
        /// Web Search の起動。端末の設定によって検索アプリは異なる。
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_OpenURL
        ///・StartAction("android.intent.action.WEB_SEARCH", "query", "検索キーワード") と同じ。
        /// </summary>
        /// <param name="query">Search keyword</param>
        public static void StartWebSearch(string query)
        {
            StartAction("android.intent.action.WEB_SEARCH", "query", query);
        }


        /// <summary>
        /// Send text to the corresponding application
        ///·Use it for sharing text (Twitter etc.).
        /// 
        /// 対応アプリにテキストを送信する
        ///・テキストのシェア（Twitter など）に利用する。
        /// </summary>
        /// <param name="text">Text to send</param>
        public static void StartActionSendText(string text)
        {
            StartAction("android.intent.action.SEND", "android.intent.extra.TEXT", text, "text/plain");
        }


        /// <summary>
        /// Use Chooser (application selection widget) to send text to other applications.
        ///·Use it for sharing text (Twitter etc.).
        ///
        /// Chooser（アプリ選択ウィジェット）を使ってテキストを他のアプリに送信する
        ///・テキストのシェア（Twitter など）に利用する。
        /// </summary>
        /// <param name="text">Text to send</param>
        /// <param name="chooserTitle">Chooser に表示するタイトル（省略="Select an application"）</param>
        public static void StartActionSendText(string text, string chooserTitle)
        {
            StartActionWithChooser("android.intent.action.SEND", "android.intent.extra.TEXT", text, "text/plain", chooserTitle);
        }


        /// <summary>
        /// Send text with attachment file to the corresponding application
        ///·Use it for sharing text (Twitter etc.).
        /// 
        /// 対応アプリにテキストと添付ファイルを送信する
        ///・テキストのシェア（Twitter など）に利用する。
        /// </summary>
        /// <param name="text">Text to send</param>
        /// <param name="attachmentURI">URI of attachment ("content://~")</param>
        public static void StartActionSendTextWithAttachment(string text, string attachmentURI)
        {
            StartAction("android.intent.action.SEND", 
                new string[] { "android.intent.extra.TEXT", "android.intent.extra.STREAM" }, 
                new string[] { text, attachmentURI }, 
                "text/plain");
        }


        /// <summary>
        /// Use Chooser (application selection widget) to send text with attachment file to other applications.
        ///·Use it for sharing text (Twitter etc.).
        ///
        /// Chooser（アプリ選択ウィジェット）を使ってテキストに添付ファイルを付けて他のアプリに送信する
        ///・テキストのシェア（Twitter など）に利用する。
        /// </summary>
        /// <param name="text">Text to send</param>
        /// <param name="chooserTitle">Display text of Chooser title (empty="Select an application")</param>
        /// <param name="attachmentURI">URI of attachment ("content://~")</param>
        public static void StartActionSendTextWithAttachment(string text, string chooserTitle, string attachmentURI)
        {
            StartActionWithChooser("android.intent.action.SEND",
                new string[] { "android.intent.extra.TEXT", "android.intent.extra.STREAM" },
                new string[] { text, attachmentURI }, 
                "text/plain", 
                chooserTitle);
        }


        /// <summary>
        /// Send text only email
        /// 
        /// テキストのみのメール送信
        /// </summary>
        /// <param name="mail">Mail address</param>
        /// <param name="subject">Mail subject</param>
        /// <param name="body">Mail body (message text)</param>
        public static void StartActionSendMail(string mail, string subject, string body)
        {
            string[] extra = { "android.intent.extra.SUBJECT", "android.intent.extra.TEXT" };
            string[] query = { subject, body };
            StartActionURI("android.intent.action.SENDTO", "mailto:" + mail, extra, query);
        }



        /// <summary>
        /// Display specified applications on Google Play
        /// (*) Google Play must be installed.
        /// 
        /// Google Play で指定アプリケーションを表示する
        /// ※Google Play がインストールされている必要がある。
        /// </summary>
        /// <param name="packageName">Package name (Application ID)</param>
        public static void ShowMarketDetails(string packageName)
        {
            string uri = "market://details?id=" + packageName;
            StartActionURI("android.intent.action.VIEW", uri);
        }


        /// <summary>
        /// Search keywords on Google Play
        /// (*) Google Play must be installed.
        /// 
        /// Google Play でキーワード検索をする
        /// ※Google Play がインストールされている必要がある。
        /// </summary>
        /// <param name="keyword">Search keywords</param>
        public static void StartMarketSearch(string keyword)
        {
            string uri = "market://search?q=" + keyword;
            StartActionURI("android.intent.action.VIEW", uri);
        }




        //==========================================================
        // Media Scanner
        //·If you save your own files etc, you need to notify the Android system.
        // If you can not see the saved file, it may become visible if you register the path to Media Scanner.
        //
        //・ファイルなどを独自に保存した場合は、Android システムに通知する必要があります。
        //　保存したファイルが見えないときは、Media Scanner にパスを登録すると、見えるようになることがあります。
        //==========================================================

        /// <summary>
        /// Scan (recognize) files with MediaScanner (single file)
        ///·The scanned path name and URI are returned in the callback.
        /// 
        /// MediaScanner でファイルをスキャン（認識）させる（単一ファイル）
        ///・コールバックにはスキャン完了したパス名やURIが返る。
        /// </summary>
        /// <param name="path">Scan target path (absolute path)</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="completeCallbackMethod">Method name to call back completion</param>
        /// <param name="resultIsJson">return value in: true=JSON format / false=path</param>
        public static void StartMediaScanner(string path, string callbackGameObject, string completeCallbackMethod, bool resultIsJson = false)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "startActionMediaScanner",
                    context,
                    path,
                    callbackGameObject,
                    completeCallbackMethod,
                    resultIsJson
                );
            }));
        }


        //(*) No callback overload
        //※コールバック無しオーバーロード
        /// <summary>
        /// Scan (recognize) files with MediaScanner (single file)
        /// 
        /// MediaScanner でファイルをスキャン（認識）させる（単一ファイル）
        /// </summary>
        /// <param name="path">Scan target path (absolute path)</param>
        public static void StartMediaScanner(string path)
        {
            StartMediaScanner(path, "", "", false);
        }


        /// <summary>
        /// Scan (recognize) files with MediaScanner (multiple file)
        ///·Completion callback is sent each path.
        /// 
        /// MediaScanner でファイルをスキャン（認識）させる（複数ファイル）
        ///・完了コールバックはパスごとに送られる。
        /// </summary>
        /// <param name="paths">Array of scan target path (absolute path)</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="completeCallbackMethod">Method name to call back completion</param>
        /// <param name="resultIsJson">return value in: true=JSON format / false=path</param>
        public static void StartMediaScanner(string[] paths, string callbackGameObject, string completeCallbackMethod, bool resultIsJson = false)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "startActionMediaScanner",
                    context,
                    paths,
                    callbackGameObject,
                    completeCallbackMethod,
                    resultIsJson
                );
            }));
        }


        //(*) No callback overload
        //※コールバック無しオーバーロード
        /// <summary>
        /// Scan (recognize) files with MediaScanner (multiple file)
        /// 
        /// MediaScanner でファイルをスキャン（認識）させる（複数ファイル）
        /// </summary>
        /// <param name="paths">Array of scan target path (absolute path)</param>
        public static void StartMediaScanner(string[] paths)
        {
            StartMediaScanner(paths, "", "", false);
        }



        /// <summary>
        /// Get the URI ("content://~") of the image file
        ///･Generally it is possible to acquire only shared files. It can not be acquired depending on security protection, directory location, etc.
        ///·If it was able to acquire, it is a form of "content://~". When it fails, it becomes empty character ("").
        ///
        /// 画像ファイルの URI ("content://~") を取得する
        ///・一般的には共有されているファイルのみ取得できる。セキュリティ保護やディレクトリ位置などによっては取得できない。
        ///・取得できた場合は "content://~" の形式。失敗したときは空文字（""）になる。
        /// </summary>
        /// <param name="path">Absolute path</param>
        /// <returns>success = "content://~" / failure = ""</returns>
        public static string GetImageURI(string path)
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
                    {
                        return androidSystem.CallStatic<string>(
                            "getImageContentURI",
                            context,
                            path
                        );
                    }
                }
            }
        }




        //==========================================================
        // Call system settings etc.
        //==========================================================

        /// <summary>
        /// Open Wifi system settings screen
        ///·Callbacks can basically be regarded as 'CLOSED_WIFI_SETTINGS' (closed) only (on / off will not be returned).
        ///(*) Required "AndroidManifest-FullPlugin~.xml" renamed "AndroidManifest.xml".
        ///(*) If callback is unnecessary, it can also be opened by 'StartAction("android.settings.WIFI_IP_SETTINGS")'.
        ///    In that case, you do not need a plug-in override "AndroidManifest.xml".
        /// 
        /// Wifi のシステム設定画面を開く
        ///・コールバックは基本的に "CLOSED_WIFI_SETTINGS"（閉じられた）のみと考えて良い（オン/オフは返らない）。
        ///※「AndroidManifest-FullPlugin～.xml」をリネームした「AndroidManifest.xml」が必要。
        ///※コールバックが不要な場合、StartAction("android.settings.WIFI_IP_SETTINGS") でも開くことができます。
        ///　その場合、プラグインオーバーライドした「AndroidManifest.xml」は必要ありません。
        /// </summary>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="resultCallbackMethod">Method name to callback the result</param>
        public static void OpenWifiSettings(string callbackGameObject, string resultCallbackMethod)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "openWifiSettings",
                    context,
                    callbackGameObject,
                    resultCallbackMethod
                );
            }));
        }


        //(*) No callback overload
        //※コールバック無しオーバーロード
        /// <summary>
        /// Open Wifi system settings screen (No callback overload)
        /// 
        /// Wifi のシステム設定画面を開く（コールバック無しオーバーロード）
        /// </summary>
        public static void OpenWifiSettings()
        {
            OpenWifiSettings("", "");
        }



        //(*) Permission Denial -> Required: '<uses-permission android:name="android.permission.BLUETOOTH" />' in 'AndroidManifest.xml'
        /// <summary>
        /// Start Bluetooth connection check and request
        ///·When current is on -> "SUCCESS_BLUETOOTH_ENABLE" is returned in the callback.
        ///·When current is off -> A connection request dialog appears
        /// -> "Yes" returns "SUCCESS_BLUETOOTH_ENABLE" for callback, "CANCEL_BLUETOOTH_ENABLE" on "No".
        ///·If "ERROR_BLUETOOTH_ADAPTER_NOT_AVAILABLE" returns in the callback, it can not be used in the system.
        ///(*) 'BLUETOOTH' permission is necessary for 'AndroidManifest.xml' (If it does not exist, "Permission Denial" appears in the callback).
        ///
        /// 
        /// Bluetooth の接続確認＆要求を実行する
        ///・元がオンのとき → コールバックに "SUCCESS_BLUETOOTH_ENABLE" が返る。
        ///・元がオフのとき → 接続要求のダイアログが出る
        ///  → 「はい」でコールバックに "SUCCESS_BLUETOOTH_ENABLE", 「いいえ」で "CANCEL_BLUETOOTH_ENABLE" が返る。
        ///・コールバックに "ERROR_BLUETOOTH_ADAPTER_NOT_AVAILABLE" が出たら、システムで利用できない。
        ///※'AndroidManifest.xml' に "BLUETOOTH" パーミッションが必要（無い場合、コールバックに "Permission Denial" が出る）。
        /// </summary>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="resultCallbackMethod">Method name to callback the result</param>
        public static void StartBluetoothRequestEnable(string callbackGameObject, string resultCallbackMethod)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "startBluetoothRequestEnable",
                    context,
                    callbackGameObject,
                    resultCallbackMethod
                );
            }));
        }


        //(*) Required: '<uses-permission android:name="android.permission.BLUETOOTH" />' in 'AndroidManifest.xml'
        //(*) No callback overload
        //※コールバック無しオーバーロード
        /// <summary>
        /// Start Bluetooth connection check and request (No callback overload)
        ///·When current is on -> Do nothing.
        ///·When current is off -> A connection request dialog appears -> "Yes" turns on.
        ///(*) 'BLUETOOTH' permission is necessary for 'AndroidManifest.xml' (If it does not exist, "Permission Denial" appears in the callback).
        ///
        /// 
        /// Bluetooth の接続確認＆要求を実行する（コールバック無しオーバーロード）
        ///・元がオンのとき → 何もしない。
        ///・元がオフのとき → 接続要求のダイアログが出る → 「はい」でオンになる。
        ///※'AndroidManifest.xml' に "BLUETOOTH" パーミッションが必要。
        /// </summary>
        public static void StartBluetoothRequestEnable()
        {
            StartBluetoothRequestEnable("", "");
        }



        //==========================================================
        // Open system default application, use etc.
        // システムデフォルトのアプリを開く・利用など
        //==========================================================

        //==========================================================
        //(*) API 19 (Android4.4) or higher is recommended
        //(*) When using External Storage, the following permission is required (unnecessary when "WRITE_EXTERNAL_STORAGE" exists).
        // '<uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" />' in 'AndroidManifest.xml'
        //
        //※API 19(Android 4.4)以上推奨。
        //※External Storage を利用するときには、以下のパーミッションが必要です（"WRITE_EXTERNAL_STORAGE" がある場合は不要）。
        // '<uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" />' in 'AndroidManifest.xml'
        //==========================================================
        /// <summary>
        /// Open the gallery and get the path
        ///(*) API 19 (Android 4.4) or higher is recommended. Because paths that can be get by API Level are different, paths are not always returned.
        ///(*) The URI returned by the application you use is different, so be careful as the information you can get is different.
        /// (The format like "content://media/external/images/media/(ID)" is the best (Standard application), in the case of application specific URI, information may be restricted)
        ///·When it succeeds, a string is returned in JSON format as '{"path":"(path)","width":(width),"height":(height)}' (It can be convert with JsonUtility).
        /// Note that the size (width, height) and other information may not be possible to acquire it depending on the storage state and using application (it becomes 0).
        ///·If it fails, the following error message is returned.
        /// ERROR_GALLERY_GET_PATH_FAILURE : Path get failed
        /// CANCEL_GALLERY : Closed without selection
        /// 
        /// 
        /// ギャラリーを開いてパスを取得する
        ///※API 19(Android 4.4)以上推奨。API Level で取得できるパスが異なるため、必ずしもパスが返ってくるとは限らない。
        ///※利用するアプリによって返される URI は異なり、取得できる情報が違うので注意
        ///（"content://media/external/images/media/(ID)" のような書式が一番良い（標準アプリ）。アプリ特有の URI の場合、情報が制限される可能性あり）。
        ///・成功したときは JSON形式で '{"path":"(パス)","width":(幅),"height":(高さ)}' のように文字列が返る（JsonUtility で取得できる）。
        ///  サイズ（width, height）、その他の情報は保存状態・取得アプリによっては取得できない可能性があるので注意（0 になる）。
        ///・失敗したときは以下のエラーメッセージが返る。
        /// ERROR_GALLERY_GET_PATH_FAILURE：パスの取得に失敗
        /// CANCEL_GALLERY：選択せずにギャラリーが閉じられた
        /// </summary>
        /// <param name="callbackGameObject">結果をコールバックするヒエラルキーの GameObject 名</param>
        /// <param name="resultCallbackMethod">結果（パス）をコールバックするヒエラルキーのメソッド名</param>
        /// <param name="errorCallbackMethod">エラー（キャンセル）をコールバックするヒエラルキーのメソッド名</param>
        public static void OpenGallery(string callbackGameObject, string resultCallbackMethod, string errorCallbackMethod)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "openGallery",
                    context,
                    callbackGameObject,
                    resultCallbackMethod,
                    errorCallbackMethod
                );
            }));
        }


        //==========================================================
        //(*) API 19 (Android4.4) or higher is recommended
        //(*) When using External Storage, the following permission is required (unnecessary when "WRITE_EXTERNAL_STORAGE" exists).
        // '<uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" />' in 'AndroidManifest.xml'
        //
        //※API 19(Android 4.4)以上推奨。
        //※External Storage を利用するときには、以下のパーミッションが必要です（"WRITE_EXTERNAL_STORAGE" がある場合は不要）。
        // '<uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" />' in 'AndroidManifest.xml'
        //==========================================================
        /// <summary>
        /// Open the gallery and get the path for video
        ///(*) API 19 (Android 4.4) or higher is recommended. Because paths that can be get by API Level are different, paths are not always returned.
        ///(*) The URI returned by the application you use is different, so be careful as the information you can get is different.
        /// (The format like "content://media/external/video/media/(ID)" is the best (Standard application), in the case of application specific URI, information may be restricted)
        ///·When it succeeds, a string is returned in JSON format as '{"path":"(path)","width":(width),"height":(height)}' (It can be convert with JsonUtility).
        /// Note that the size (width, height) and other information may not be possible to acquire it depending on the storage state and using application (it becomes 0).
        ///·If it fails, the following error message is returned.
        /// ERROR_GALLERY_GET_PATH_FAILURE : Path get failed
        /// CANCEL_GALLERY : Closed without selection
        /// 
        /// 
        /// ギャラリーを開いて動画のパスを取得する
        ///※API 19(Android 4.4)以上推奨。API Level で取得できるパスが異なるため、必ずしもパスが返ってくるとは限らない。
        ///※利用するアプリによって返される URI は異なり、取得できる情報が違うので注意
        ///（"content://media/external/video/media/(ID)" のような書式が一番良い（標準アプリ）。アプリ特有の URI の場合、情報が制限される可能性あり）。
        ///・成功したときは JSON形式で '{"path":"(パス)","width":(幅),"height":(高さ)}' のように文字列が返る（JsonUtility で取得できる）。
        ///  サイズ（width, height）、その他の情報は保存状態・取得アプリによっては取得できない可能性があるので注意（0 になる）。
        ///・失敗したときは以下のエラーメッセージが返る。
        /// ERROR_GALLERY_GET_PATH_FAILURE：パスの取得に失敗
        /// CANCEL_GALLERY：選択せずにギャラリーが閉じられた
        /// </summary>
        /// <param name="callbackGameObject">結果をコールバックするヒエラルキーの GameObject 名</param>
        /// <param name="resultCallbackMethod">結果（パス）をコールバックするヒエラルキーのメソッド名</param>
        /// <param name="errorCallbackMethod">エラー（キャンセル）をコールバックするヒエラルキーのメソッド名</param>
        public static void OpenGalleryVideo(string callbackGameObject, string resultCallbackMethod, string errorCallbackMethod)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "openGalleryVideo",
                    context,
                    callbackGameObject,
                    resultCallbackMethod,
                    errorCallbackMethod
                );
            }));
        }




        //==========================================================
        // Using the storage access framework
        //(*) API 19 (Android4.4) or higher
        // https://developer.android.com/guide/topics/providers/document-provider.html
        //·MIME Type may not work depending on the provider (storage).
        //(*) Note that the information that can be obtained by the provider (storage) is different.
        //·Information that can be acquired in order of Local storage > SD card > Cloud storage becomes more.
        //
        // ストレージアクセス機能を利用する
        //※API 19 (Android4.4) 以上
        // https://developer.android.com/guide/topics/providers/document-provider.html
        //・MIME Type はプロバイダ（ストレージ）によって効かない場合がある。
        //※プロバイダ（ストレージ）によって取得できる情報は異なるので注意。
        //・ローカルストレージ ＞ SDカード ＞ クラウドストレージ の順に取得できる情報が多くなる。
        //==========================================================

        //Default charset encoding
        //デフォルトの文字エンコード
        const string DEFAULT_ENCODING = "UTF-8";
        const string DEFAULT_TEXT_MIME_TYPE = "text/plain";

        //==========================================================
        //(*) API 19 (Android4.4) or higher
        //※API 19 (Android 4.4) 以上のみ。
        //==========================================================
        /// <summary>
        /// Select and load a text file with the storage access framework.
        ///(*) API 19 (Android4.4) or higher
        ///·When it succeeds, the text read in 'resultCallbackMethod' is returned.
        ///·If it fails, the following error message is returned.
        /// CANCEL_LOAD_TEXT : Closed without selection
        /// UnsupportedEncodingException : Unsupported character encoding
        ///·MIME Type may not work depending on the provider (storage).
        /// 
        /// 
        /// ストレージアクセス機能でテキストファイルを選択し、ロードする。
        ///※API 19 (Android 4.4) 以上のみ。
        ///・成功したときは「resultCallbackMethod」に読み込んだテキストが返る。
        ///・失敗したときは以下のエラーメッセージが返る。
        /// CANCEL_LOAD_TEXT：選択せずに閉じられた
        /// UnsupportedEncodingException：サポートされてない文字エンコード
        ///・MIME Type はプロバイダ（ストレージ）によって効かない場合がある。
        /// </summary>
        /// <param name="encoding">文字エンコード（"UTF-8", "Shift_JIS", "EUC-JP", "JIS" など）</param>
        /// <param name="mimeTypes">MIME Type ("text/plain", "text/csv" など)</param>
        /// <param name="callbackGameObject">結果をコールバックするヒエラルキーの GameObject 名</param>
        /// <param name="resultCallbackMethod">結果（読み込んだテキスト）をコールバックするメソッド名</param>
        /// <param name="errorCallbackMethod">エラー（キャンセル）をコールバックするメソッド名</param>
        public static void OpenStorageAndLoadText(string encoding, string[] mimeTypes,
            string callbackGameObject, string resultCallbackMethod, string errorCallbackMethod)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "openDocumentAndLoadText",
                    context,
                    encoding,
                    mimeTypes,
                    callbackGameObject,
                    resultCallbackMethod,
                    errorCallbackMethod
                );
            }));
        }


        //==========================================================
        //(*) API 19 (Android4.4) or higher
        //※API 19 (Android 4.4) 以上のみ。
        //※MIME type = "text/plain" オーバーロード
        //==========================================================
        /// <summary>
        /// Select and load a text file with the storage access framework.
        ///(*) API 19 (Android4.4) or higher
        ///·When it succeeds, the text read in 'resultCallbackMethod' is returned.
        ///·If it fails, the following error message is returned.
        /// CANCEL_LOAD_TEXT : Closed without selection
        /// UnsupportedEncodingException : Unsupported character encoding
        /// 
        /// 
        /// ストレージアクセス機能でテキストファイルを選択し、ロードする。
        ///※API 19 (Android 4.4) 以上のみ。
        ///・成功したときは「resultCallbackMethod」に読み込んだテキストが返る。
        ///・失敗したときは以下のエラーメッセージが返る。
        /// CANCEL_LOAD_TEXT：選択せずに閉じられた
        /// UnsupportedEncodingException：サポートされてない文字エンコード
        /// </summary>
        /// <param name="encoding">文字エンコード（"UTF-8", "Shift_JIS", "EUC-JP", "JIS" など）</param>
        /// <param name="callbackGameObject">結果をコールバックするヒエラルキーの GameObject 名</param>
        /// <param name="resultCallbackMethod">結果（読み込んだテキスト）をコールバックするメソッド名</param>
        /// <param name="errorCallbackMethod">エラー（キャンセル）をコールバックするメソッド名</param>
        public static void OpenStorageAndLoadText(string encoding,
            string callbackGameObject, string resultCallbackMethod, string errorCallbackMethod)
        {
            OpenStorageAndLoadText(encoding, new string[] { DEFAULT_TEXT_MIME_TYPE },
                callbackGameObject, resultCallbackMethod, errorCallbackMethod);
        }


        //==========================================================
        //(*) API 19 (Android4.4) or higher
        //(*) UTF-8 encoding overload
        //※API 19 (Android 4.4) 以上のみ。
        //※UTF-8 エンコード オーバーロード
        //==========================================================
        /// <summary>
        /// Select and load a text file with the storage access framework (Character encoding : UTF-8).
        ///(*) API 19 (Android4.4) or higher
        ///·When it succeeds, the text read in 'resultCallbackMethod' is returned.
        ///·If it fails, the following error message is returned.
        /// CANCEL_LOAD_TEXT : Closed without selection
        /// UnsupportedEncodingException : Unsupported character encoding
        /// 
        /// 
        /// ストレージアクセス機能でテキストファイルを選択し、ロードする（文字エンコード：UTF-8）。
        ///※API 19 (Android 4.4) 以上のみ。
        ///・成功したときは「resultCallbackMethod」に読み込んだテキストが返る。
        ///・失敗したときは以下のエラーメッセージが返る。
        /// CANCEL_LOAD_TEXT：選択せずに閉じられた
        /// UnsupportedEncodingException：サポートされてない文字エンコード
        /// </summary>
        /// <param name="callbackGameObject">結果をコールバックするヒエラルキーの GameObject 名</param>
        /// <param name="resultCallbackMethod">結果（読み込んだテキスト）をコールバックするメソッド名</param>
        /// <param name="errorCallbackMethod">エラー（キャンセル）をコールバックするメソッド名</param>
        public static void OpenStorageAndLoadText(string callbackGameObject, string resultCallbackMethod, string errorCallbackMethod)
        {
            OpenStorageAndLoadText(DEFAULT_ENCODING, new string[] { DEFAULT_TEXT_MIME_TYPE },
                callbackGameObject, resultCallbackMethod, errorCallbackMethod);
        }




        //==========================================================
        //(*) API 19 (Android4.4) or higher
        //※API 19 (Android 4.4) 以上のみ。
        //==========================================================
        /// <summary>
        /// Select the directory in the storage access framework and save it.
        ///(*) API 19 (Android4.4) or higher
        ///·When it succeeds, the file name saved in 'resultCallbackMethod' is returned.
        ///·If it fails, the following error message is returned.
        /// CANCEL_SAVE_TEXT : Closed without selection
        /// UnsupportedEncodingException : Unsupported character encoding
        /// ERROR_CREATE_DOCUMENT_WRITE_ACCESS_DENIED : Write access denied (Android system specifications).
        ///·MIME Type may not work depending on the provider (storage).
        /// 
        /// 
        /// ストレージアクセス機能でディレクトリを指定し、保存する。
        ///※API 19 (Android 4.4) 以上のみ。
        ///・成功したときは「resultCallbackMethod」に保存したファイル名が返る。
        ///・失敗したときは以下のエラーメッセージが返る。
        /// CANCEL_SAVE_TEXT：選択せずに閉じられた
        /// ERROR_CREATE_DOCUMENT_WRITE_ACCESS_DENIED：書き込みアクセスが拒否された（Android システムの仕様）。
        /// UnsupportedEncodingException：サポートされてない文字エンコード
        ///・MIME Type はプロバイダ（ストレージ）によって効かない場合がある。
        /// </summary>
        /// <param name="fileName">デフォルトのファイル名（"NewDocument.txt" など。ディレクトリ名は含まない）</param>
        /// <param name="text">保存するテキスト</param>
        /// <param name="encoding">文字エンコード（"UTF-8", "Shift_JIS", "EUC-JP", "JIS" など）</param>
        /// <param name="mimeTypes">MIME Type ("text/plain", "text/csv" など)</param>
        /// <param name="callbackGameObject">結果をコールバックするヒエラルキーの GameObject 名</param>
        /// <param name="resultCallbackMethod">結果（保存したファイル名）をコールバックするヒエラルキーのメソッド名</param>
        /// <param name="errorCallbackMethod">エラー（キャンセル）をコールバックするヒエラルキーのメソッド名</param>
        public static void OpenStorageAndSaveText(string fileName, string text, string encoding, string[] mimeTypes,
            string callbackGameObject, string resultCallbackMethod, string errorCallbackMethod)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "createDocumentAndSaveText",
                    context,
                    fileName,
                    text,
                    encoding,
                    mimeTypes,
                    callbackGameObject,
                    resultCallbackMethod,
                    errorCallbackMethod
                );
            }));
        }


        //==========================================================
        //(*) API 19 (Android4.4) or higher
        //※API 19 (Android 4.4) 以上のみ。
        //※MIME type = "text/plain" オーバーロード
        //==========================================================
        /// <summary>
        /// Select the directory in the storage access framework and save it.
        ///(*) API 19 (Android4.4) or higher
        ///·When it succeeds, the file name saved in 'resultCallbackMethod' is returned.
        ///·If it fails, the following error message is returned.
        /// CANCEL_SAVE_TEXT : Closed without selection
        /// ERROR_CREATE_DOCUMENT_WRITE_ACCESS_DENIED : Write access denied (Android system specifications).
        /// UnsupportedEncodingException : Unsupported character encoding
        /// 
        /// 
        /// ストレージアクセス機能でディレクトリを指定し、保存する。
        ///※API 19 (Android 4.4) 以上のみ。
        ///・成功したときは「resultCallbackMethod」に保存したファイル名が返る。
        ///・失敗したときは以下のエラーメッセージが返る。
        /// CANCEL_SAVE_TEXT：選択せずに閉じられた
        /// ERROR_CREATE_DOCUMENT_WRITE_ACCESS_DENIED：書き込みアクセスが拒否された（Android システムの仕様）。
        /// UnsupportedEncodingException：サポートされてない文字エンコード
        /// </summary>
        /// <param name="fileName">デフォルトのファイル名（"NewDocument.txt" など。ディレクトリ名は含まない）</param>
        /// <param name="text">保存するテキスト</param>
        /// <param name="encoding">文字エンコード（"UTF-8", "Shift_JIS", "EUC-JP", "JIS" など）</param>
        /// <param name="callbackGameObject">結果をコールバックするヒエラルキーの GameObject 名</param>
        /// <param name="resultCallbackMethod">結果（保存したファイル名）をコールバックするヒエラルキーのメソッド名</param>
        /// <param name="errorCallbackMethod">エラー（キャンセル）をコールバックするヒエラルキーのメソッド名</param>
        public static void OpenStorageAndSaveText(string fileName, string text, string encoding,
            string callbackGameObject, string resultCallbackMethod, string errorCallbackMethod)
        {
            OpenStorageAndSaveText(fileName, text, encoding, new string[] { DEFAULT_TEXT_MIME_TYPE },
                callbackGameObject, resultCallbackMethod, errorCallbackMethod);
        }


        //==========================================================
        //(*) API 19 (Android4.4) or higher
        //(*) UTF-8 encoding overload
        //※API 19 (Android 4.4) 以上のみ。
        //※UTF-8 エンコード オーバーロード
        //==========================================================
        /// <summary>
        /// Select the directory in the storage access framework and save it (Character encoding : UTF-8).
        ///(*) API 19 (Android4.4) or higher
        ///·When it succeeds, the file name saved in 'resultCallbackMethod' is returned.
        ///·If it fails, the following error message is returned.
        /// CANCEL_SAVE_TEXT : Closed without selection
        /// ERROR_CREATE_DOCUMENT_WRITE_ACCESS_DENIED : Write access denied (Android system specifications).
        /// UnsupportedEncodingException : Unsupported character encoding
        /// 
        /// 
        /// ストレージアクセス機能でディレクトリを指定し、保存する（文字エンコード：UTF-8）。
        ///※API 19 (Android 4.4) 以上のみ。
        ///・成功したときは「resultCallbackMethod」に保存したファイル名が返る。
        ///・失敗したときは以下のエラーメッセージが返る。
        /// CANCEL_SAVE_TEXT：選択せずに閉じられた
        /// ERROR_CREATE_DOCUMENT_WRITE_ACCESS_DENIED：書き込みアクセスが拒否された（Android システムの仕様）。
        /// UnsupportedEncodingException：サポートされてない文字エンコード
        /// </summary>
        /// <param name="fileName">デフォルトのファイル名（"NewDocument.txt" など。ディレクトリ名は含まない）</param>
        /// <param name="text">保存するテキスト</param>
        /// <param name="callbackGameObject">結果をコールバックするヒエラルキーの GameObject 名</param>
        /// <param name="resultCallbackMethod">結果（保存したファイル名）をコールバックするヒエラルキーのメソッド名</param>
        /// <param name="errorCallbackMethod">エラー（キャンセル）をコールバックするヒエラルキーのメソッド名</param>
        public static void OpenStorageAndSaveText(string fileName, string text,
            string callbackGameObject, string resultCallbackMethod, string errorCallbackMethod)
        {
            OpenStorageAndSaveText(fileName, text, DEFAULT_ENCODING, new string[] { DEFAULT_TEXT_MIME_TYPE },
                callbackGameObject, resultCallbackMethod, errorCallbackMethod);
        }




        //==========================================================
        //(*) API 19 (Android4.4) or higher
        //※API 19 (Android 4.4) 以上のみ。
        //==========================================================
        /// <summary>
        /// Select a file with the storage access framework and acquire information or path.
        ///(*) API 19 (Android4.4) or higher
        ///·When it succeeds, information is returned to "resultCallbackMethod" in path or JSON format.
        ///·If it fails, the following error message is returned.
        /// CANCEL_OPEN_DOCUMENT : Closed without selection
        /// ERROR_OPEN_DOCUMENT_GET_PATH_FAILURE : Path acquisition failed (when resultIsJson = false)
        ///·MIME Type may not work depending on the provider (storage).
        /// 
        /// 
        /// ストレージアクセス機能でファイルを選択し、パス等の情報を取得する。
        ///※API 19 (Android 4.4) 以上のみ。
        ///・成功したときは「resultCallbackMethod」にパスまたはJSON形式で情報が返る。
        ///・失敗したときは以下のエラーメッセージが返る。
        /// CANCEL_OPEN_DOCUMENT：選択せずに閉じられた
        /// ERROR_OPEN_DOCUMENT_GET_PATH_FAILURE：パスの取得に失敗（resultIsJson = false のとき）
        ///・MIME Type はプロバイダ（ストレージ）によって効かない場合がある。
        /// </summary>
        /// <param name="mimeTypes">MIME Type ("*/*", "text/csv" など)</param>
        /// <param name="callbackGameObject">結果をコールバックするヒエラルキーの GameObject 名</param>
        /// <param name="resultCallbackMethod">結果（保存したファイル名）をコールバックするヒエラルキーのメソッド名</param>
        /// <param name="errorCallbackMethod">エラー（キャンセル）をコールバックするヒエラルキーのメソッド名</param>
        /// <param name="resultIsJson">true = 戻り値をJSON形式（～Info に対応）にする/ false = パスのみ</param>
        public static void OpenStorageFile(string[] mimeTypes, string callbackGameObject,
            string resultCallbackMethod, string errorCallbackMethod, bool resultIsJson = false)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "openDocument",
                    context,
                    mimeTypes,
                    callbackGameObject,
                    resultCallbackMethod,
                    errorCallbackMethod,
                    resultIsJson
                );
            }));
        }


        //==========================================================
        //(*) API 19 (Android4.4) or higher
        //(*) All image types overload
        //※API 19 (Android 4.4) 以上のみ。
        //※すべての画像タイプオーバーロード
        //==========================================================
        /// <summary>
        /// Select a file with the storage access framework and acquire information or path.
        ///(*) API 19 (Android4.4) or higher
        ///·When it succeeds, information is returned to "resultCallbackMethod" in path or JSON format.
        ///·If it fails, the following error message is returned.
        /// CANCEL_OPEN_DOCUMENT : Closed without selection
        /// ERROR_OPEN_DOCUMENT_GET_PATH_FAILURE : Path acquisition failed (when resultIsJson = false)
        /// 
        /// 
        /// ストレージアクセス機能でファイルを選択し、パス等の情報を取得する。
        ///※API 19 (Android 4.4) 以上のみ。
        ///・成功したときは「resultCallbackMethod」にパスまたはJSON形式で情報が返る。
        ///・失敗したときは以下のエラーメッセージが返る。
        /// CANCEL_OPEN_DOCUMENT：選択せずに閉じられた
        /// ERROR_OPEN_DOCUMENT_GET_PATH_FAILURE：パスの取得に失敗（resultIsJson = false のとき）
        /// </summary>
        /// <param name="callbackGameObject">結果をコールバックするヒエラルキーの GameObject 名</param>
        /// <param name="resultCallbackMethod">結果（保存したファイル名）をコールバックするヒエラルキーのメソッド名</param>
        /// <param name="errorCallbackMethod">エラー（キャンセル）をコールバックするヒエラルキーのメソッド名</param>
        /// <param name="resultIsJson">true = 戻り値をJSON形式（～Info に対応）にする/ false = パスのみ</param>
        public static void OpenStorageFile(string callbackGameObject,
            string resultCallbackMethod, string errorCallbackMethod, bool resultIsJson = false)
        {
            OpenStorageFile(new string[] { AndroidMimeType.File.All }, 
                callbackGameObject, resultCallbackMethod, errorCallbackMethod, resultIsJson);
        }




        //==========================================================
        //(*) API 19 (Android4.4) or higher
        //※API 19 (Android 4.4) 以上のみ。
        //==========================================================
        /// <summary>
        /// Select a image file with the storage access framework and acquire information or path.
        ///(*) API 19 (Android4.4) or higher
        ///·When it succeeds, information is returned to "resultCallbackMethod" in path or JSON format.
        ///·If it fails, the following error message is returned.
        /// CANCEL_OPEN_DOCUMENT : Closed without selection
        /// ERROR_OPEN_DOCUMENT_GET_PATH_FAILURE : Path acquisition failed (when resultIsJson = false)
        ///·MIME Type may not work depending on the provider (storage).
        ///(*) Note that the information that can be obtained by the provider (storage) is different.
        ///·Information that can be acquired in order of Local storage > SD card > Cloud storage becomes more.
        /// 
        /// 
        /// ストレージアクセス機能で画像ファイルを選択し、パス等の情報を取得する。
        ///※API 19 (Android 4.4) 以上のみ。
        ///・成功したときは「resultCallbackMethod」にパスまたはJSON形式で情報が返る。
        ///・失敗したときは以下のエラーメッセージが返る。
        /// CANCEL_OPEN_DOCUMENT：選択せずに閉じられた
        /// ERROR_OPEN_DOCUMENT_GET_PATH_FAILURE：パスの取得に失敗（resultIsJson = false のとき）
        ///・MIME Type はプロバイダ（ストレージ）によって効かない場合がある。
        ///※プロバイダ（ストレージ）によって取得できる情報は異なるので注意。
        ///・ローカルストレージ ＞ SDカード ＞ クラウドストレージ の順に取得できる情報が多くなる。
        /// </summary>
        /// <param name="mimeTypes">MIME Type ("image/*", "image/png" など)</param>
        /// <param name="callbackGameObject">結果をコールバックするヒエラルキーの GameObject 名</param>
        /// <param name="resultCallbackMethod">結果（保存したファイル名）をコールバックするヒエラルキーのメソッド名</param>
        /// <param name="errorCallbackMethod">エラー（キャンセル）をコールバックするヒエラルキーのメソッド名</param>
        public static void OpenStorageImage(string[] mimeTypes, string callbackGameObject,
            string resultCallbackMethod, string errorCallbackMethod)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "openDocumentImage",
                    context,
                    mimeTypes,
                    callbackGameObject,
                    resultCallbackMethod,
                    errorCallbackMethod
                );
            }));
        }


        //==========================================================
        //(*) API 19 (Android4.4) or higher
        //(*) All image types overload
        //※API 19 (Android 4.4) 以上のみ。
        //※すべての画像タイプオーバーロード
        //==========================================================
        /// <summary>
        /// Select a image file with the storage access framework and acquire information or path.
        ///(*) API 19 (Android4.4) or higher
        ///·When it succeeds, information is returned to "resultCallbackMethod" in path or JSON format.
        ///·If it fails, the following error message is returned.
        /// CANCEL_OPEN_DOCUMENT : Closed without selection
        /// ERROR_OPEN_DOCUMENT_GET_PATH_FAILURE : Path acquisition failed (when resultIsJson = false)
        ///(*) Note that the information that can be obtained by the provider (storage) is different.
        ///·Information that can be acquired in order of Local storage > SD card > Cloud storage becomes more.
        /// 
        /// 
        /// ストレージアクセス機能で画像ファイルを選択し、パス等の情報を取得する。
        ///※API 19 (Android 4.4) 以上のみ。
        ///・成功したときは「resultCallbackMethod」にパスまたはJSON形式で情報が返る。
        ///・失敗したときは以下のエラーメッセージが返る。
        /// CANCEL_OPEN_DOCUMENT：選択せずに閉じられた
        /// ERROR_OPEN_DOCUMENT_GET_PATH_FAILURE：パスの取得に失敗（resultIsJson = false のとき）
        ///※プロバイダ（ストレージ）によって取得できる情報は異なるので注意。
        ///・ローカルストレージ ＞ SDカード ＞ クラウドストレージ の順に取得できる情報が多くなる。
        /// </summary>
        /// <param name="callbackGameObject">結果をコールバックするヒエラルキーの GameObject 名</param>
        /// <param name="resultCallbackMethod">結果（保存したファイル名）をコールバックするヒエラルキーのメソッド名</param>
        /// <param name="errorCallbackMethod">エラー（キャンセル）をコールバックするヒエラルキーのメソッド名</param>
        public static void OpenStorageImage(string callbackGameObject, string resultCallbackMethod, string errorCallbackMethod)
        {
            OpenStorageImage(new string[] { AndroidMimeType.Image.All },
                callbackGameObject, resultCallbackMethod, errorCallbackMethod);
        }



        //==========================================================
        //(*) API 19 (Android4.4) or higher
        //※API 19 (Android 4.4) 以上のみ。
        //==========================================================
        /// <summary>
        /// Select a audio file with the storage access framework and acquire information or path.
        ///(*) API 19 (Android4.4) or higher
        ///·When it succeeds, information is returned to "resultCallbackMethod" in path or JSON format.
        ///·If it fails, the following error message is returned.
        /// CANCEL_OPEN_DOCUMENT : Closed without selection
        /// ERROR_OPEN_DOCUMENT_GET_PATH_FAILURE : Path acquisition failed (when resultIsJson = false)
        ///·MIME Type may not work depending on the provider (storage).
        ///(*) Note that the information that can be obtained by the provider (storage) is different.
        ///·Information that can be acquired in order of Local storage > SD card > Cloud storage becomes more.
        /// 
        /// 
        /// ストレージアクセス機能で音声ファイルを選択し、パス等の情報を取得する。
        ///※API 19 (Android 4.4) 以上のみ。
        ///・成功したときは「resultCallbackMethod」にパスまたはJSON形式で情報が返る。
        ///・失敗したときは以下のエラーメッセージが返る。
        /// CANCEL_OPEN_DOCUMENT：選択せずに閉じられた
        /// ERROR_OPEN_DOCUMENT_GET_PATH_FAILURE：パスの取得に失敗（resultIsJson = false のとき）
        ///・MIME Type はプロバイダ（ストレージ）によって効かない場合がある。
        ///※プロバイダ（ストレージ）によって取得できる情報は異なるので注意。
        ///・ローカルストレージ ＞ SDカード ＞ クラウドストレージ の順に取得できる情報が多くなる。
        /// </summary>
        /// <param name="mimeTypes">MIME Type ("audio/*", "audio/mpeg" など)</param>
        /// <param name="callbackGameObject">結果をコールバックするヒエラルキーの GameObject 名</param>
        /// <param name="resultCallbackMethod">結果（保存したファイル名）をコールバックするヒエラルキーのメソッド名</param>
        /// <param name="errorCallbackMethod">エラー（キャンセル）をコールバックするヒエラルキーのメソッド名</param>
        public static void OpenStorageAudio(string[] mimeTypes, string callbackGameObject,
            string resultCallbackMethod, string errorCallbackMethod)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "openDocumentAudio",
                    context,
                    mimeTypes,
                    callbackGameObject,
                    resultCallbackMethod,
                    errorCallbackMethod
                );
            }));
        }


        //==========================================================
        //(*) API 19 (Android4.4) or higher
        //(*) All audio types overload
        //※API 19 (Android 4.4) 以上のみ。
        //※すべての音声タイプオーバーロード
        //==========================================================
        /// <summary>
        /// Select a audio file with the storage access framework and acquire information or path.
        ///(*) API 19 (Android4.4) or higher
        ///·When it succeeds, information is returned to "resultCallbackMethod" in path or JSON format.
        ///·If it fails, the following error message is returned.
        /// CANCEL_OPEN_DOCUMENT : Closed without selection
        /// ERROR_OPEN_DOCUMENT_GET_PATH_FAILURE : Path acquisition failed (when resultIsJson = false)
        ///(*) Note that the information that can be obtained by the provider (storage) is different.
        ///·Information that can be acquired in order of Local storage > SD card > Cloud storage becomes more.
        /// 
        /// 
        /// ストレージアクセス機能で音声ファイルを選択し、パス等の情報を取得する。
        ///※API 19 (Android 4.4) 以上のみ。
        ///・成功したときは「resultCallbackMethod」にパスまたはJSON形式で情報が返る。
        ///・失敗したときは以下のエラーメッセージが返る。
        /// CANCEL_OPEN_DOCUMENT：選択せずに閉じられた
        /// ERROR_OPEN_DOCUMENT_GET_PATH_FAILURE：パスの取得に失敗（resultIsJson = false のとき）
        ///※プロバイダ（ストレージ）によって取得できる情報は異なるので注意。
        ///・ローカルストレージ ＞ SDカード ＞ クラウドストレージ の順に取得できる情報が多くなる。
        /// </summary>
        /// <param name="callbackGameObject">結果をコールバックするヒエラルキーの GameObject 名</param>
        /// <param name="resultCallbackMethod">結果（保存したファイル名）をコールバックするヒエラルキーのメソッド名</param>
        /// <param name="errorCallbackMethod">エラー（キャンセル）をコールバックするヒエラルキーのメソッド名</param>
        public static void OpenStorageAudio(string callbackGameObject, string resultCallbackMethod, string errorCallbackMethod)
        {
            OpenStorageAudio(new string[] { AndroidMimeType.Audio.All },
                callbackGameObject, resultCallbackMethod, errorCallbackMethod);
        }



        //==========================================================
        //(*) API 19 (Android4.4) or higher
        //※API 19 (Android 4.4) 以上のみ。
        //==========================================================
        /// <summary>
        /// Select a video file with the storage access framework and acquire information or path.
        ///(*) API 19 (Android4.4) or higher
        ///·When it succeeds, information is returned to "resultCallbackMethod" in path or JSON format.
        ///·If it fails, the following error message is returned.
        /// CANCEL_OPEN_DOCUMENT : Closed without selection
        /// ERROR_OPEN_DOCUMENT_GET_PATH_FAILURE : Path acquisition failed (when resultIsJson = false)
        ///·MIME Type may not work depending on the provider (storage).
        ///(*) Note that the information that can be obtained by the provider (storage) is different.
        ///·Information that can be acquired in order of Local storage > SD card > Cloud storage becomes more.
        /// 
        /// 
        /// ストレージアクセス機能で動画ファイルを選択し、パス等の情報を取得する。
        ///※API 19 (Android 4.4) 以上のみ。
        ///・成功したときは「resultCallbackMethod」にパスまたはJSON形式で情報が返る。
        ///・失敗したときは以下のエラーメッセージが返る。
        /// CANCEL_OPEN_DOCUMENT：選択せずに閉じられた
        /// ERROR_OPEN_DOCUMENT_GET_PATH_FAILURE：パスの取得に失敗（resultIsJson = false のとき）
        ///・MIME Type はプロバイダ（ストレージ）によって効かない場合がある。
        ///※プロバイダ（ストレージ）によって取得できる情報は異なるので注意。
        ///・ローカルストレージ ＞ SDカード ＞ クラウドストレージ の順に取得できる情報が多くなる。
        /// </summary>
        /// <param name="mimeTypes">MIME Type ("video/*", "video/mp4" など)</param>
        /// <param name="callbackGameObject">結果をコールバックするヒエラルキーの GameObject 名</param>
        /// <param name="resultCallbackMethod">結果（保存したファイル名）をコールバックするヒエラルキーのメソッド名</param>
        /// <param name="errorCallbackMethod">エラー（キャンセル）をコールバックするヒエラルキーのメソッド名</param>
        public static void OpenStorageVideo(string[] mimeTypes, string callbackGameObject,
            string resultCallbackMethod, string errorCallbackMethod)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "openDocumentVideo",
                    context,
                    mimeTypes,
                    callbackGameObject,
                    resultCallbackMethod,
                    errorCallbackMethod
                );
            }));
        }


        //==========================================================
        //(*) API 19 (Android4.4) or higher
        //(*) All video types overload
        //※API 19 (Android 4.4) 以上のみ。
        //※すべての動画タイプオーバーロード
        //==========================================================
        /// <summary>
        /// Select a video file with the storage access framework and acquire information or path.
        ///(*) API 19 (Android4.4) or higher
        ///·When it succeeds, information is returned to "resultCallbackMethod" in path or JSON format.
        ///·If it fails, the following error message is returned.
        /// CANCEL_OPEN_DOCUMENT : Closed without selection
        /// ERROR_OPEN_DOCUMENT_GET_PATH_FAILURE : Path acquisition failed (when resultIsJson = false)
        ///(*) Note that the information that can be obtained by the provider (storage) is different.
        ///·Information that can be acquired in order of Local storage > SD card > Cloud storage becomes more.
        /// 
        /// 
        /// ストレージアクセス機能で動画ファイルを選択し、パス等の情報を取得する。
        ///※API 19 (Android 4.4) 以上のみ。
        ///・成功したときは「resultCallbackMethod」にパスまたはJSON形式で情報が返る。
        ///・失敗したときは以下のエラーメッセージが返る。
        /// CANCEL_OPEN_DOCUMENT：選択せずに閉じられた
        /// ERROR_OPEN_DOCUMENT_GET_PATH_FAILURE：パスの取得に失敗（resultIsJson = false のとき）
        ///※プロバイダ（ストレージ）によって取得できる情報は異なるので注意。
        ///・ローカルストレージ ＞ SDカード ＞ クラウドストレージ の順に取得できる情報が多くなる。
        /// </summary>
        /// <param name="callbackGameObject">結果をコールバックするヒエラルキーの GameObject 名</param>
        /// <param name="resultCallbackMethod">結果（保存したファイル名）をコールバックするヒエラルキーのメソッド名</param>
        /// <param name="errorCallbackMethod">エラー（キャンセル）をコールバックするヒエラルキーのメソッド名</param>
        public static void OpenStorageVideo(string callbackGameObject, string resultCallbackMethod, string errorCallbackMethod)
        {
            OpenStorageVideo(new string[] { AndroidMimeType.Video.All },
                callbackGameObject, resultCallbackMethod, errorCallbackMethod);
        }




        //==========================================================
        //(*) API 19 (Android4.4) or higher
        //(*) Only the local storage can be saved directly from Unity.
        //    To save the text file on the SD card, use 'OpenStorageAndSaveText()'.
        //
        //※API 19 (Android 4.4) 以上のみ。
        //※Unity から直接保存できるのはローカルストレージのみになります。
        //　SDカードにテキストファイルを保存するには OpenStorageAndSaveText() を使って下さい。
        //==========================================================
        /// <summary>
        /// Select a file for save with the storage access framework and acquire information or path.
        ///(*) API 19 (Android4.4) or higher
        ///·Basically from Unity you can save directly to Local storage only (security reason, external storage write is limited).
        ///·When it succeeds, information is returned to "resultCallbackMethod" in path or JSON format.
        ///·If it fails, the following error message is returned.
        /// CANCEL_CREATE_DOCUMENT : Closed without selection
        /// ERROR_CREATE_DOCUMENT_GET_PATH_FAILURE : Path acquisition failed
        /// ERROR_CREATE_DOCUMENT_WRITE_ACCESS_DENIED : Write access denied (Android system specifications).
        ///·MIME Type may not work depending on the provider (storage).
        /// 
        /// 
        /// ストレージアクセス機能で保存先ファイルを選択し、パス等の情報を取得する。
        ///※API 19 (Android 4.4) 以上のみ。
        ///・基本的に Unity からはローカルストレージにのみ直接保存できる（セキュリティ上、外部ストレージ書き込みは制限される）。
        ///・成功したときは「resultCallbackMethod」にパスまたはJSON形式で情報が返る。
        ///・失敗したときは以下のエラーメッセージが返る。
        /// CANCEL_CREATE_DOCUMENT：選択せずに閉じられた
        /// ERROR_CREATE_DOCUMENT_GET_PATH_FAILURE：パスの取得に失敗
        /// ERROR_CREATE_DOCUMENT_WRITE_ACCESS_DENIED：書き込みアクセスが拒否された（Android システムの仕様）。
        ///・MIME Type はプロバイダ（ストレージ）によって効かない場合がある。
        /// </summary>
        /// <param name="fileName">デフォルトのファイル名（"NewDocument.txt" など。ディレクトリ名は含まない）</param>
        /// <param name="mimeTypes">MIME Type ("text/plain", "text/csv" など)</param>
        /// <param name="callbackGameObject">結果をコールバックするヒエラルキーの GameObject 名</param>
        /// <param name="resultCallbackMethod">結果（保存したファイル名）をコールバックするヒエラルキーのメソッド名</param>
        /// <param name="errorCallbackMethod">エラー（キャンセル）をコールバックするヒエラルキーのメソッド名</param>
        /// <param name="resultIsJson">true = 戻り値をJSON形式（～Info に対応）にする/ false = パスのみ</param>
        public static void OpenStorageForSave(string fileName, string[] mimeTypes, string callbackGameObject, 
            string resultCallbackMethod, string errorCallbackMethod, bool resultIsJson = false)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "createDocument",
                    context,
                    fileName,
                    mimeTypes,
                    callbackGameObject,
                    resultCallbackMethod,
                    errorCallbackMethod,
                    resultIsJson
                );
            }));
        }




        //==========================================================
        //(*) API 21 (Android 5.0) or higher
        //※API 21 (Android 5.0) 以上のみ。
        //==========================================================
        /// <summary>
        /// Select a folder with the storage access framework and acquire information or path.
        ///(*) API 19 (Android4.4) or higher
        ///·When it succeeds, information is returned to "resultCallbackMethod" in path or JSON format.
        ///·If it fails, the following error message is returned.
        /// CANCEL_OPEN_DOCUMENT : Closed without selection
        /// ERROR_OPEN_DOCUMENT_GET_PATH_FAILURE : Path acquisition failed (when resultIsJson = false)
        /// 
        /// 
        /// ストレージアクセス機能でフォルダを選択し、パス等の情報を取得する。
        ///※API 19 (Android 4.4) 以上のみ。
        ///・成功したときは「resultCallbackMethod」にパスまたはJSON形式で情報が返る。
        ///・失敗したときは以下のエラーメッセージが返る。
        /// CANCEL_OPEN_DOCUMENT：選択せずに閉じられた
        /// ERROR_OPEN_DOCUMENT_GET_PATH_FAILURE：パスの取得に失敗（resultIsJson = false のとき）
        /// </summary>
        /// <param name="callbackGameObject">結果をコールバックするヒエラルキーの GameObject 名</param>
        /// <param name="resultCallbackMethod">結果（保存したファイル名）をコールバックするヒエラルキーのメソッド名</param>
        /// <param name="errorCallbackMethod">エラー（キャンセル）をコールバックするヒエラルキーのメソッド名</param>
        /// <param name="resultIsJson">true = 戻り値をJSON形式（～Info に対応）にする/ false = パスのみ</param>
        public static void OpenStorageFolder(string callbackGameObject, 
            string resultCallbackMethod, string errorCallbackMethod, bool resultIsJson = false)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "openDocumentTree",
                    context,
                    callbackGameObject,
                    resultCallbackMethod,
                    errorCallbackMethod,
                    resultIsJson
                );
            }));
        }





        //==========================================================
        // Get status of External Storage
        //(*) Although it was an SD card before API 19 (Android4.4), it became virtual storage (internal storage) after API 19.
        //·before API 19 : "mnt/sdcard/" or "sdcard/" etc. (Depends on model)
        //·after  API 19 : "/storage/emulated/0" like (Depends on model(?))
        //(*) To write to External Storage, permission is necessary for 'AndroidManifest.xml'.
        // <uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" />
        //(*) It can be "READ_EXTERNAL_STORAGE" for reading only.
        //
        //
        // External Storage のステータス取得
        //※API 19(Android4.4)より前は SDカードだったが、API 19 以降は 仮想ストレージ（内部ストレージ）となった。
        //・API 19 より前： "mnt/sdcard/" or "sdcard/" など（端末による）
        //・API 19 以降 ： "/storage/emulated/0" のようになる（端末による(?)）
        //※External Storage に書き込みをするには、「AndroidManifest.xml」にパーミッションが必要。
        // <uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" />
        //※読み取りだけなら "READ_EXTERNAL_STORAGE" でも良い。
        //==========================================================

        /// <summary>
        /// Returns whether the primary shared/external storage media is emulated.
        /// 仮想ストレージが存在するか？
        /// </summary>
        /// <returns>true = storage media is emulated</returns>
        public static bool IsExternalStorageEmulated()
        {
            using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
            {
                return androidSystem.CallStatic<bool>(
                    "isExternalStorageEmulated"
                );
            }
        }

        /// <summary>
        /// Returns whether the primary shared/external storage media is physically removable (like SD card).
        /// 外部(仮想)ストレージが物理的に脱着できるか？（SDカードのように）
        /// </summary>
        /// <returns>true if the storage device can be removed (such as an SD card), or false if the storage device is built in and cannot be physically removed.</returns>
        public static bool IsExternalStorageRemovable()
        {
            using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
            {
                return androidSystem.CallStatic<bool>(
                    "isExternalStorageRemovable"
                );
            }
        }

        /// <summary>
        /// Check for External Storage mounted.
        /// 外部(仮想)ストレージがマウントされているか否か？（読み書き属性は関係ない）
        /// </summary>
        /// <returns>true = mounted</returns>
        public static bool IsExternalStorageMounted()
        {
            using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
            {
                return androidSystem.CallStatic<bool>(
                    "isExternalStorageMounted"
                );
            }
        }

        /// <summary>
        /// Check for External Storage mounted and readonly.
        /// 外部(仮想)ストレージがマウントされていて、読み込みのみ属性か？
        /// </summary>
        /// <returns>true = mounted and readonly</returns>
        public static bool IsExternalStorageMountedReadOnly()
        {
            using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
            {
                return androidSystem.CallStatic<bool>(
                    "isExternalStorageMountedReadOnly"
                );
            }
        }

        //(*) To write to External Storage, required '<uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" />' in 'AndroidManifest.xml'
        //※書き込みをするには、'<uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" />' が必要。
        /// <summary>
        /// Check for External Storage mounted and read/write.
        /// 外部(仮想)ストレージがマウントされていて、読み書き属性か？
        /// </summary>
        /// <returns>true = mounted and read/write</returns>
        public static bool IsExternalStorageMountedReadWrite()
        {
            using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
            {
                return androidSystem.CallStatic<bool>(
                    "isExternalStorageMountedReadWrite"
                );
            }
        }

        /// <summary>
        /// Returns the current state of the primary shared/external storage media.
        /// 外部(仮想)ストレージのステータスを取得する
        /// https://developer.android.com/reference/android/os/Environment.html#MEDIA_BAD_REMOVAL
        /// </summary>
        /// <returns>one of "unknown", "removed", "unmounted", "checking", "nofs", "mounted", "mounted_ro", "shared", "bad_removal", or "unmountable".</returns>
        public static string GetExternalStorageState()
        {
            using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
            {
                return androidSystem.CallStatic<string>(
                    "getExternalStorageState"
                );
            }
        }

        /// <summary>
        /// Return the primary shared/external storage directory.
        /// 外部(仮想)ストレージディレクトリを返す。
        /// </summary>
        /// <returns>returns directory path like "/storage/emulated/0"</returns>
        public static string GetExternalStorageDirectory()
        {
            using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
            {
                return androidSystem.CallStatic<string>(
                    "getExternalStorageDirectory"
                );
            }
        }

        /// <summary>
        /// Returns the 'Alarms' directory of shared/external storage directory.
        /// 外部(仮想)ストレージの「Alarms」ディレクトリを返す。
        /// </summary>
        /// <returns>returns directory path like "/storage/emulated/0/Alarms"</returns>
        public static string GetExternalStorageDirectoryAlarms()
        {
            using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
            {
                return androidSystem.CallStatic<string>(
                    "getExternalStorageDirectoryAlarms"
                );
            }
        }

        /// <summary>
        /// Returns the 'DCIM' directory of shared/external storage directory.
        /// 外部(仮想)ストレージの「DCIM」ディレクトリを返す。
        /// </summary>
        /// <returns>returns directory path like "/storage/emulated/0/DCIM"</returns>
        public static string GetExternalStorageDirectoryDCIM()
        {
            using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
            {
                return androidSystem.CallStatic<string>(
                    "getExternalStorageDirectoryDCIM"
                );
            }
        }

        //(*) API 19 or higher (Note: before that all empty characters(""))
        //※API 19以上（それより前は全て空文字（""）になるので注意）
        /// <summary>
        /// Returns the 'Documents' directory of shared/external storage directory.
        /// (*) API 19 or higher (Note: before that all empty characters(""))
        /// 
        /// 外部(仮想)ストレージの「Documents」ディレクトリを返す。
        /// ※API 19(Android4.4)以上で利用可（それより前は全て空文字（""））
        /// </summary>
        /// <returns>returns directory path like "/storage/emulated/0/Documents"</returns>
        public static string GetExternalStorageDirectoryDocuments()
        {
            using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
            {
                return androidSystem.CallStatic<string>(
                    "getExternalStorageDirectoryDocuments"
                );
            }
        }

        /// <summary>
        /// Returns the 'Downloads' directory of shared/external storage directory.
        /// 外部(仮想)ストレージの「Downloads」ディレクトリを返す。
        /// </summary>
        /// <returns>returns directory path like "/storage/emulated/0/Downloads"</returns>
        public static string GetExternalStorageDirectoryDownloads()
        {
            using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
            {
                return androidSystem.CallStatic<string>(
                    "getExternalStorageDirectoryDownloads"
                );
            }
        }

        /// <summary>
        /// Returns the 'Movies' directory of shared/external storage directory.
        /// 外部(仮想)ストレージの「Movies」ディレクトリを返す。
        /// </summary>
        /// <returns>returns directory path like "/storage/emulated/0/Movies"</returns>
        public static string GetExternalStorageDirectoryMovies()
        {
            using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
            {
                return androidSystem.CallStatic<string>(
                    "getExternalStorageDirectoryMovies"
                );
            }
        }

        /// <summary>
        /// Returns the 'Music' directory of shared/external storage directory.
        /// 外部(仮想)ストレージの「Music」ディレクトリを返す。
        /// </summary>
        /// <returns>returns directory path like "/storage/emulated/0/Music"</returns>
        public static string GetExternalStorageDirectoryMusic()
        {
            using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
            {
                return androidSystem.CallStatic<string>(
                    "getExternalStorageDirectoryMusic"
                );
            }
        }

        /// <summary>
        /// Returns the 'Notifications' directory of shared/external storage directory.
        /// 外部(仮想)ストレージの「Notifications」ディレクトリを返す。
        /// </summary>
        /// <returns>returns directory path like "/storage/emulated/0/Notifications"</returns>
        public static string GetExternalStorageDirectoryNotifications()
        {
            using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
            {
                return androidSystem.CallStatic<string>(
                    "getExternalStorageDirectoryNotifications"
                );
            }
        }

        /// <summary>
        /// Returns the 'Pictures' directory of shared/external storage directory.
        /// 外部(仮想)ストレージの「Pictures」ディレクトリを返す。
        /// </summary>
        /// <returns>returns directory path like "/storage/emulated/0/Pictures"</returns>
        public static string GetExternalStorageDirectoryPictures()
        {
            using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
            {
                return androidSystem.CallStatic<string>(
                    "getExternalStorageDirectoryPictures"
                );
            }
        }

        /// <summary>
        /// Returns the 'Podcasts' directory of shared/external storage directory.
        /// 外部(仮想)ストレージの「Podcasts」ディレクトリを返す。
        /// </summary>
        /// <returns>returns directory path like "/storage/emulated/0/Podcasts"</returns>
        public static string GetExternalStorageDirectoryPodcasts()
        {
            using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
            {
                return androidSystem.CallStatic<string>(
                    "getExternalStorageDirectoryPodcasts"
                );
            }
        }

        /// <summary>
        /// Returns the 'Ringtones' directory of shared/external storage directory.
        /// 外部(仮想)ストレージの「Ringtones」ディレクトリを返す。
        /// </summary>
        /// <returns>returns directory path like "/storage/emulated/0/Ringtones"</returns>
        public static string GetExternalStorageDirectoryRingtones()
        {
            using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
            {
                return androidSystem.CallStatic<string>(
                    "getExternalStorageDirectoryRingtones"
                );
            }
        }




        //==========================================================
        // Speech Recognizer
        //(*) Required 'uses-permission android:name="android.permission.RECORD_AUDIO' in 'AndroidManifest.xml'.
        //(*) When using dialog (Google), rename "AndroidManifest-FullPlugin~.xml" to "AndroidManifest.xml" and use it.
        // http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_SpeechRecognizerDialog
        // http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_SpeechRecognizerListener
        // (Locale string)
        //･Format：BCP47 (e.g. "en-US", "ja-JP") [This plug-in can also be "en_US", "ja_JP" (under bar).]
        // https://developer.android.com/reference/android/speech/RecognizerIntent.html#EXTRA_LANGUAGE
        // (Locale list)
        // http://fantom1x.blog130.fc2.com/blog-entry-295.html
        //
        //
        // 音声認識
        //※AndroidManifest.xml に '<uses-permission android:name="android.permission.RECORD_AUDIO />'タグが必要。
        //※音声認識でダイアログ(Google)を表示するには、「AndroidManifest-FullPlugin~.xml」を「AndroidManifest.xml」にリネームして使う。
        // http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_SpeechRecognizerDialog
        // http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_SpeechRecognizerListener
        // (言語設定)
        //･Format：BCP47 (例: "en-US", "ja-JP") [このプラグインでは "en_US", "ja_JP" (アンダーバー)でも可能]
        // https://developer.android.com/reference/android/speech/RecognizerIntent.html#EXTRA_LANGUAGE
        // (Locale 一覧)
        // http://fantom1x.blog130.fc2.com/blog-entry-295.html
        //==========================================================


        //(*) Required: '<uses-permission android:name="android.permission.RECORD_AUDIO />' in 'AndroidManifest.xml'
        /// <summary>
        /// Does the system support speech recognizer?
        /// (*) It has nothing to do with permissions and language.
        /// 
        /// 
        /// 端末が音声認識をサポートしてるか？
        ///※パーミッション、言語とは関係ありません。
        /// </summary>
        /// <returns>true = supported</returns>
        public static bool IsSupportedSpeechRecognizer()
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
                    {
                        return androidSystem.CallStatic<bool>(
                            "isSupportedSpeechRecognizer",
                            context
                        );
                    }
                }
            }
        }



        //(*) Required: '<uses-permission android:name="android.permission.RECORD_AUDIO />' in 'AndroidManifest.xml'
        /// <summary>
        /// Call Android Speech Recognizer Dialog (Google)
        /// http://fantom1x.blog130.fc2.com/blog-entry-284.html
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_SpeechRecognizerDialog
        ///(*) Rename "AndroidManifest-FullPlugin~.xml" to "AndroidManifest.xml" and use it.
        ///(*) Required 'uses-permission android:name="android.permission.RECORD_AUDIO' in 'AndroidManifest.xml'.
        ///･The recognized strings are concatenated with line feed ("\n") and returned as arguments of the callback.
        /// If it fails, it returns an error code string (like "ERROR_").
        ///･(Error code string)
        /// https://developer.android.com/reference/android/speech/SpeechRecognizer.html#ERROR_AUDIO
        /// ERROR_NO_MATCH : Could not recognize it.
        /// ERROR_SPEECH_TIMEOUT ： Wait for speech time out.
        /// ERROR_RECOGNIZER_BUSY : System is busy (When going on continuously etc.)
        /// ERROR_INSUFFICIENT_PERMISSIONS : There is no permission tag "RECORD_AUDIO" in "AndroidManifest.xml".
        /// ERROR_NETWORK : Could not connect to the network.
        /// (Locale string)
        ///･Format：BCP47 (e.g. "en-US", "ja-JP") [This plug-in can also be "en_US", "ja_JP" (under bar).]
        /// https://developer.android.com/reference/android/speech/RecognizerIntent.html#EXTRA_LANGUAGE
        /// (Locale list)
        /// http://fantom1x.blog130.fc2.com/blog-entry-295.html
        /// 
        /// 
        /// Android の SpeechRecognizer（音声認識：ダイアログを表示）を使用する（結果だけを受け取る）
        /// http://fantom1x.blog130.fc2.com/blog-entry-284.html
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_SpeechRecognizerDialog
        ///※「AndroidManifest-FullPlugin~.xml」を「AndroidManifest.xml」にリネームして使う。
        ///※「AndroidManifest.xml」に 'uses-permission android:name="android.permission.RECORD_AUDIO' タグが必要。
        ///・結果は認識できた文字列を改行区切り（"\n"）で返す。失敗したときはエラーコード（文字列 "ERROR_"）を返す。
        /// (エラーコード（文字列）)
        /// https://developer.android.com/reference/android/speech/SpeechRecognizer.html#ERROR_AUDIO
        /// ERROR_NO_MATCH：認識できなかった
        /// ERROR_SPEECH_TIMEOUT：タイムアウト
        /// ERROR_RECOGNIZER_BUSY：ビジー状態（連続に行ったときなど）
        /// ERROR_INSUFFICIENT_PERMISSIONS：AndroidManifest.xml に "RECORD_AUDIO" のバーミッションがない
        /// ERROR_NETWORK：ネットワークに繋がらない
        /// (言語設定)
        ///・Format：BCP47 (例: "en-US", "ja-JP") [このプラグインでは "en_US", "ja_JP" (アンダーバー)でも可能]
        /// https://developer.android.com/reference/android/speech/RecognizerIntent.html#EXTRA_LANGUAGE
        /// (Locale 一覧)
        /// http://fantom1x.blog130.fc2.com/blog-entry-295.html
        /// </summary>
        /// <param name="locale">Locale string (e.g. "ja", "en", "en_GB" etc.）</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="resultCallbackMethod">Method name to result callback (it is in GameObject)</param>
        /// <param name="message">Message string</param>
        public static void ShowSpeechRecognizer(string locale, string callbackGameObject, string resultCallbackMethod, string message)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "showSpeechRecognizer",
                    context,
                    locale,
                    callbackGameObject,
                    resultCallbackMethod,
                    message
                );
            }));
        }


        //(*) System language overload
        //※システムの言語オーバーロード
        //(*) Required: '<uses-permission android:name="android.permission.RECORD_AUDIO />' in 'AndroidManifest.xml'
        /// <summary>
        /// Call Android Speech Recognizer Dialog (Google)
        /// http://fantom1x.blog130.fc2.com/blog-entry-284.html
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_SpeechRecognizerDialog
        ///(*) Rename "AndroidManifest-FullPlugin~.xml" to "AndroidManifest.xml" and use it.
        ///(*) Required 'uses-permission android:name="android.permission.RECORD_AUDIO' in 'AndroidManifest.xml'.
        ///･The recognized strings are concatenated with line feed ("\n") and returned as arguments of the callback.
        /// If it fails, it returns an error code string (like "ERROR_").
        ///･(Error code string)
        /// https://developer.android.com/reference/android/speech/SpeechRecognizer.html#ERROR_AUDIO
        /// ERROR_NO_MATCH : Could not recognize it.
        /// ERROR_SPEECH_TIMEOUT ： Wait for speech time out.
        /// ERROR_RECOGNIZER_BUSY : System is busy (When going on continuously etc.)
        /// ERROR_INSUFFICIENT_PERMISSIONS : There is no permission tag "RECORD_AUDIO" in "AndroidManifest.xml".
        /// ERROR_NETWORK : Could not connect to the network.
        /// 
        /// 
        /// Android の SpeechRecognizer（音声認識：ダイアログを表示）を使用する（結果だけを受け取る）
        /// http://fantom1x.blog130.fc2.com/blog-entry-284.html
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_SpeechRecognizerDialog
        ///※「AndroidManifest-FullPlugin~.xml」を「AndroidManifest.xml」にリネームして使う。
        ///※「AndroidManifest.xml」に 'uses-permission android:name="android.permission.RECORD_AUDIO' タグが必要。
        ///・結果は認識できた文字列を改行区切り（"\n"）で返す。失敗したときはエラーコード（文字列 "ERROR_"）を返す。
        /// (エラーコード（文字列）)
        /// https://developer.android.com/reference/android/speech/SpeechRecognizer.html#ERROR_AUDIO
        /// ERROR_NO_MATCH：認識できなかった
        /// ERROR_SPEECH_TIMEOUT：タイムアウト
        /// ERROR_RECOGNIZER_BUSY：ビジー状態（連続に行ったときなど）
        /// ERROR_INSUFFICIENT_PERMISSIONS：AndroidManifest.xml に "RECORD_AUDIO" のバーミッションがない
        /// ERROR_NETWORK：ネットワークに繋がらない
        /// </summary>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="resultCallbackMethod">Method name to result callback (it is in GameObject)</param>
        /// <param name="message">Message string</param>
        public static void ShowSpeechRecognizer(string callbackGameObject, string resultCallbackMethod, string message = "")
        {
            ShowSpeechRecognizer("", callbackGameObject, resultCallbackMethod, message);
        }



        //(*) Required: '<uses-permission android:name="android.permission.RECORD_AUDIO />' in 'AndroidManifest.xml'
        /// <summary>
        /// Call Android Speech Recognizer without Dialog (Receive events and results)
        /// http://fantom1x.blog130.fc2.com/blog-entry-284.html
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_SpeechRecognizerListener
        ///(*) Required 'uses-permission android:name="android.permission.RECORD_AUDIO' in 'AndroidManifest.xml'.
        ///･The recognized strings are concatenated with line feed ("\n") and returned as arguments of the callback.
        /// If it fails, it returns an error code string (like "ERROR_").
        ///･(Error code string)
        /// https://developer.android.com/reference/android/speech/SpeechRecognizer.html#ERROR_AUDIO
        /// ERROR_NO_MATCH : Could not recognize it.
        /// ERROR_SPEECH_TIMEOUT ： Wait for speech time out.
        /// ERROR_RECOGNIZER_BUSY : System is busy (When going on continuously etc.)
        /// ERROR_INSUFFICIENT_PERMISSIONS : There is no permission tag "RECORD_AUDIO" in "AndroidManifest.xml".
        /// ERROR_NETWORK : Could not connect to the network.
        /// (Locale string)
        ///･Format：BCP47 (e.g. "en-US", "ja-JP") [This plug-in can also be "en_US", "ja_JP" (under bar).]
        /// https://developer.android.com/reference/android/speech/RecognizerIntent.html#EXTRA_LANGUAGE
        /// (Locale list)
        /// http://fantom1x.blog130.fc2.com/blog-entry-295.html
        /// 
        /// 
        /// Android の SpeechRecognizer（音声認識：ダイアログなし）を使用する（イベントを取得して結果を受け取る）
        /// http://fantom1x.blog130.fc2.com/blog-entry-284.html
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_SpeechRecognizerListener
        ///※「AndroidManifest.xml」に 'uses-permission android:name="android.permission.RECORD_AUDIO' タグが必要。
        ///・結果は認識できた文字列を改行区切り（"\n"）で返す。失敗したときはエラーコード（文字列 "ERROR_"）を返す。
        /// https://developer.android.com/reference/android/speech/SpeechRecognizer.html#ERROR_AUDIO
        /// (エラーコード（文字列）)
        /// ERROR_NO_MATCH：認識できなかった
        /// ERROR_SPEECH_TIMEOUT：タイムアウト
        /// ERROR_RECOGNIZER_BUSY：ビジー状態（連続に行ったときなど）
        /// ERROR_INSUFFICIENT_PERMISSIONS：AndroidManifest.xml に "RECORD_AUDIO" のバーミッションがない
        /// ERROR_NETWORK：ネットワークに繋がらない
        /// (言語設定)
        ///・Format：BCP47 (例: "en-US", "ja-JP") [このプラグインでは "en_US", "ja_JP" (アンダーバー)でも可能]
        /// https://developer.android.com/reference/android/speech/RecognizerIntent.html#EXTRA_LANGUAGE
        /// (Locale 一覧)
        /// http://fantom1x.blog130.fc2.com/blog-entry-295.html
        /// </summary>
        /// <param name="locale">Locale string (e.g. "ja", "en", "en_GB" etc.）</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="resultCallbackMethod">Method name to callback when recognition is successful (it is in GameObject)</param>
        /// <param name="errorCallbackMethod">Method name to callback when recognition is failure or error (it is in GameObject)</param>
        /// <param name="readyCallbackMethod">Method name to callback when start waiting for speech recognition (Always "onReadyForSpeech" is returned) (it is in GameObject)</param>
        /// <param name="beginCallbackMethod">Method name to callback when the first voice entered the microphone (Always "onBeginningOfSpeech" is returned) (it is in GameObject)</param>
        public static void StartSpeechRecognizer(string locale, 
            string callbackGameObject, string resultCallbackMethod, string errorCallbackMethod,
            string readyCallbackMethod, string beginCallbackMethod)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "startSpeechRecognizer",
                    context,
                    locale,
                    callbackGameObject,
                    resultCallbackMethod,
                    errorCallbackMethod,
                    readyCallbackMethod,
                    beginCallbackMethod
                );
            }));
        }


        //(*) System language overload
        //※システムの言語オーバーロード
        //(*) Required: '<uses-permission android:name="android.permission.RECORD_AUDIO />' in 'AndroidManifest.xml'
        /// <summary>
        /// Call Android Speech Recognizer without Dialog (Receive events and results)
        /// http://fantom1x.blog130.fc2.com/blog-entry-284.html
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_SpeechRecognizerListener
        ///(*) Required 'uses-permission android:name="android.permission.RECORD_AUDIO' in 'AndroidManifest.xml'.
        ///･The recognized strings are concatenated with line feed ("\n") and returned as arguments of the callback.
        /// If it fails, it returns an error code string (like "ERROR_").
        ///･(Error code string)
        /// https://developer.android.com/reference/android/speech/SpeechRecognizer.html#ERROR_AUDIO
        /// ERROR_NO_MATCH : Could not recognize it.
        /// ERROR_SPEECH_TIMEOUT ： Wait for speech time out.
        /// ERROR_RECOGNIZER_BUSY : System is busy (When going on continuously etc.)
        /// ERROR_INSUFFICIENT_PERMISSIONS : There is no permission tag "RECORD_AUDIO" in "AndroidManifest.xml".
        /// ERROR_NETWORK : Could not connect to the network.
        /// 
        /// 
        /// Android の SpeechRecognizer（音声認識：ダイアログなし）を使用する（イベントを取得して結果を受け取る）
        /// http://fantom1x.blog130.fc2.com/blog-entry-284.html
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_SpeechRecognizerListener
        ///※「AndroidManifest.xml」に 'uses-permission android:name="android.permission.RECORD_AUDIO' タグが必要。
        ///・結果は認識できた文字列を改行区切り（"\n"）で返す。失敗したときはエラーコード（文字列 "ERROR_"）を返す。
        /// https://developer.android.com/reference/android/speech/SpeechRecognizer.html#ERROR_AUDIO
        /// (エラーコード（文字列）)
        /// ERROR_NO_MATCH：認識できなかった
        /// ERROR_SPEECH_TIMEOUT：タイムアウト
        /// ERROR_RECOGNIZER_BUSY：ビジー状態（連続に行ったときなど）
        /// ERROR_INSUFFICIENT_PERMISSIONS：AndroidManifest.xml に "RECORD_AUDIO" のバーミッションがない
        /// ERROR_NETWORK：ネットワークに繋がらない
        /// </summary>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="resultCallbackMethod">Method name to callback when recognition is successful (it is in GameObject)</param>
        /// <param name="errorCallbackMethod">Method name to callback when recognition is failure or error (it is in GameObject)</param>
        /// <param name="readyCallbackMethod">Method name to callback when start waiting for speech recognition (Always "onReadyForSpeech" is returned) (it is in GameObject)</param>
        /// <param name="beginCallbackMethod">Method name to callback when the first voice entered the microphone (Always "onBeginningOfSpeech" is returned) (it is in GameObject)</param>
        public static void StartSpeechRecognizer(string callbackGameObject, string resultCallbackMethod, string errorCallbackMethod,
            string readyCallbackMethod = "", string beginCallbackMethod = "")
        {
            StartSpeechRecognizer("", callbackGameObject, 
                resultCallbackMethod, errorCallbackMethod, readyCallbackMethod, beginCallbackMethod);
        }


        //(*) Required: '<uses-permission android:name="android.permission.RECORD_AUDIO />' in 'AndroidManifest.xml'
        /// <summary>
        /// Release speech recognizer instance (Also use it to interrupt)
        ///･Even when "AndroidPlugin.Release()" is called, it is executed on the native side.
        /// 
        /// 音声認識のオブジェクトをリリース（中断するのにも使用する）
        ///※AndroidPlugin.Release() でも解放される。
        /// </summary>
        public static void ReleaseSpeechRecognizer()
        {
            using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
            {
                androidSystem.CallStatic(
                    "releaseSpeechRecognizer"
                );
            }
        }





        //==========================================================
        // Text To Speech
        // http://fantom1x.blog130.fc2.com/blog-entry-275.html
        //(*) Reading engine and voice data must be installed on the device in order to use it.
        // The following are confirmed operation.
        // https://play.google.com/store/apps/details?id=com.google.android.tts
        // https://play.google.com/store/apps/details?id=jp.kddilabs.n2tts
        // (Locale string)
        //･Format：BCP47 (e.g. "en-US", "ja-JP") [This plug-in can also be "en_US", "ja_JP" (under bar).]
        // https://developer.android.com/reference/android/speech/RecognizerIntent.html#EXTRA_LANGUAGE
        // (Locale list)
        // http://fantom1x.blog130.fc2.com/blog-entry-295.html
        //
        //
        // テキスト読み上げ
        // http://fantom1x.blog130.fc2.com/blog-entry-275.html
        //※この機能を利用するには読み上げエンジンとボイスデータがインストールされている必要があります。
        // 以下は動作確認されています。
        // https://play.google.com/store/apps/details?id=com.google.android.tts
        // https://play.google.com/store/apps/details?id=jp.kddilabs.n2tts  (Japanese only)
        // (言語設定)
        //･Format：BCP47 (例: "en-US", "ja-JP") [このプラグインでは "en_US", "ja_JP" (アンダーバー)でも可能]
        // https://developer.android.com/reference/android/speech/RecognizerIntent.html#EXTRA_LANGUAGE
        // (Locale 一覧)
        // http://fantom1x.blog130.fc2.com/blog-entry-295.html
        //==========================================================


        /// <summary>
        /// Initialize Text To Speech (First time only. It is necessary also when language is changed)
        /// http://fantom1x.blog130.fc2.com/blog-entry-275.html#fantomPlugin_TextToSpeech
        ///･When it is available at the first startup, "SUCCESS_INIT" status is returned to the callback method (statusCallbackMethod).
        ///･(Error code string)
        /// https://developer.android.com/reference/android/speech/tts/TextToSpeech.html#ERROR
        /// Moreover, the following error occurs:
        /// ERROR_LOCALE_NOT_AVAILABLE : There is no voice data corresponding to the system language setting
        /// ERROR_INIT : Initialization failed
        /// ERROR_UTTERANCEPROGRESS_LISTENER_REGISTER : Event listener registration failed
        /// ERROR_UTTERANCEPROGRESS_LISTENER : Some kind of error of event acquisition
        /// (Locale string)
        ///･Format：BCP47 (e.g. "en-US", "ja-JP") [This plug-in can also be "en_US", "ja_JP" (under bar).]
        /// https://developer.android.com/reference/android/speech/RecognizerIntent.html#EXTRA_LANGUAGE
        /// (Locale list)
        /// http://fantom1x.blog130.fc2.com/blog-entry-295.html
        /// 
        /// 
        /// テキスト読み上げを初期化する（起動初回のみ。言語を変更した場合も必要）
        /// http://fantom1x.blog130.fc2.com/blog-entry-275.html#fantomPlugin_TextToSpeech
        ///・初回起動時には使用可能なとき、コールバックメソッド（statusCallbackMethod ）に "SUCCESS_INIT" ステータスが返ってくる。
        ///・エラーコード（文字列）は以下を参照
        /// https://developer.android.com/reference/android/speech/tts/TextToSpeech.html#ERROR
        /// また上記とは別に以下のエラーが返ることもある。
        /// ERROR_LOCALE_NOT_AVAILABLE：端末の言語設定に対応した音声データがない。
        /// ERROR_INIT：初期化に失敗（=UNKOWN と同義）。
        /// ERROR_UTTERANCEPROGRESS_LISTENER_REGISTER：イベントリスナー登録に失敗。
        /// ERROR_UTTERANCEPROGRESS_LISTENER：イベント取得の何らかのエラー（=UNKOWN と同義）。
        /// (言語設定)
        ///・Format：BCP47 (例: "en-US", "ja-JP") [このプラグインでは "en_US", "ja_JP" (アンダーバー)でも可能]
        /// https://developer.android.com/reference/android/speech/RecognizerIntent.html#EXTRA_LANGUAGE
        /// (Locale 一覧)
        /// http://fantom1x.blog130.fc2.com/blog-entry-295.html
        /// </summary>
        /// <param name="locale">Locale string (e.g. "ja", "en", "en_GB" etc.）</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="statusCallbackMethod">Method name to callback when returns status (including error)</param>
        public static void InitTextToSpeech(string locale, string callbackGameObject, string statusCallbackMethod)
        {
            //Since the start status is returned only for the first time, it is just called with a dummy (empty character) (like a shortcut)
            //起動ステータスは初回のみ返ってくるのでダミー（空文字）で呼び出しているだけ（ショートカットのようなもの）
            StartTextToSpeech("", locale, callbackGameObject, statusCallbackMethod, "", "", "");
        }


        /// <summary>
        /// Initialize Text To Speech (First time only)
        /// http://fantom1x.blog130.fc2.com/blog-entry-275.html#fantomPlugin_TextToSpeech
        ///･When it is available at the first startup, "SUCCESS_INIT" status is returned to the callback method (statusCallbackMethod).
        ///･(Error code string)
        /// https://developer.android.com/reference/android/speech/tts/TextToSpeech.html#ERROR
        /// Moreover, the following error occurs:
        /// ERROR_LOCALE_NOT_AVAILABLE : There is no voice data corresponding to the system language setting
        /// ERROR_INIT : Initialization failed
        /// ERROR_UTTERANCEPROGRESS_LISTENER_REGISTER : Event listener registration failed
        /// ERROR_UTTERANCEPROGRESS_LISTENER : Some kind of error of event acquisition
        /// 
        /// 
        /// テキスト読み上げを初期化する（起動初回のみ）
        /// http://fantom1x.blog130.fc2.com/blog-entry-275.html#fantomPlugin_TextToSpeech
        ///・初回起動時には使用可能なとき、コールバックメソッド（statusCallbackMethod ）に "SUCCESS_INIT" ステータスが返ってくる。
        ///・エラーコード（文字列）は以下を参照
        /// https://developer.android.com/reference/android/speech/tts/TextToSpeech.html#ERROR
        /// また上記とは別に以下のエラーが返ることもある。
        /// ERROR_LOCALE_NOT_AVAILABLE：端末の言語設定に対応した音声データがない。
        /// ERROR_INIT：初期化に失敗（=UNKOWN と同義）。
        /// ERROR_UTTERANCEPROGRESS_LISTENER_REGISTER：イベントリスナー登録に失敗。
        /// ERROR_UTTERANCEPROGRESS_LISTENER：イベント取得の何らかのエラー（=UNKOWN と同義）。
        /// </summary>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="statusCallbackMethod">Method name to callback when returns status (including error)</param>
        public static void InitTextToSpeech(string callbackGameObject = "", string statusCallbackMethod = "")
        {
            //Since the start status is returned only for the first time, it is just called with a dummy (empty character) (like a shortcut)
            //起動ステータスは初回のみ返ってくるのでダミー（空文字）で呼び出しているだけ（ショートカットのようなもの）
            StartTextToSpeech("", "", callbackGameObject, statusCallbackMethod, "", "", "");
        }
        //↓タイプミス
        [Obsolete("This method name is a typo. Please use 'InitTextToSpeech' instead.")]
        public static void InitSpeechRecognizer(string callbackGameObject = "", string statusCallbackMethod = "")
        {
            StartTextToSpeech("", "", callbackGameObject, statusCallbackMethod, "", "", "");
        }



        /// <summary>
        /// Call Android Text To Speech
        /// http://fantom1x.blog130.fc2.com/blog-entry-275.html#fantomPlugin_TextToSpeech
        ///･When it is available at the first startup, "SUCCESS_INIT" status is returned to the callback method (statusCallbackMethod).
        ///(*) Interrupted event callback (stopCallbackMethod) can only be operated with API 23 (Android 6.0) or higher.
        ///    However, it seems that the behavior varies depending on the system, so be careful.
        ///･(Error code string)
        /// https://developer.android.com/reference/android/speech/tts/TextToSpeech.html#ERROR
        /// Moreover, the following error occurs:
        /// ERROR_LOCALE_NOT_AVAILABLE : There is no voice data corresponding to the system language setting
        /// ERROR_INIT : Initialization failed
        /// ERROR_UTTERANCEPROGRESS_LISTENER_REGISTER : Event listener registration failed
        /// ERROR_UTTERANCEPROGRESS_LISTENER : Some kind of error of event acquisition
        /// (Locale string)
        ///･Format：BCP47 (e.g. "en-US", "ja-JP") [This plug-in can also be "en_US", "ja_JP" (under bar).]
        /// https://developer.android.com/reference/android/speech/RecognizerIntent.html#EXTRA_LANGUAGE
        /// (Locale list)
        /// http://fantom1x.blog130.fc2.com/blog-entry-295.html
        /// 
        /// 
        /// テキスト読み上げを開始する
        /// http://fantom1x.blog130.fc2.com/blog-entry-275.html#fantomPlugin_TextToSpeech
        ///・初回起動時には使用可能なとき、コールバックメソッド（statusCallbackMethod ）に "SUCCESS_INIT" ステータスが返ってくる。
        ///※中断イベントコールバック（stopCallbackMethod）は API23(Android6.0) 以上でのみ動作可。ただし端末により挙動が異なるそうなので使用する際には注意。
        ///・エラーコード（文字列）は以下を参照
        /// https://developer.android.com/reference/android/speech/tts/TextToSpeech.html#ERROR
        /// また上記とは別に以下のエラーが返ることもある。
        /// ERROR_LOCALE_NOT_AVAILABLE：端末の言語設定に対応した音声データがない。
        /// ERROR_INIT：初期化に失敗（=UNKOWN と同義）。
        /// ERROR_UTTERANCEPROGRESS_LISTENER_REGISTER：イベントリスナー登録に失敗。
        /// ERROR_UTTERANCEPROGRESS_LISTENER：イベント取得の何らかのエラー（=UNKOWN と同義）。
        /// (言語設定)
        ///・Format：BCP47 (例: "en-US", "ja-JP") [このプラグインでは "en_US", "ja_JP" (アンダーバー)でも可能]
        /// https://developer.android.com/reference/android/speech/RecognizerIntent.html#EXTRA_LANGUAGE
        /// (Locale 一覧)
        /// http://fantom1x.blog130.fc2.com/blog-entry-295.html
        /// </summary>
        /// <param name="message">Reading text string</param>
        /// <param name="locale">Locale string (e.g. "ja", "en", "en_GB" etc.）</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="statusCallbackMethod">Method name to callback when returns status (including error)</param>
        /// <param name="startCallbackMethod">Method name to callback when start reading text (Always "onStart" is returned) (it is in GameObject)</param>
        /// <param name="doneCallbackMethod">Method name to callback when finish reading text (Always "onDone" is returned) (it is in GameObject)</param>
        /// <param name="stopCallbackMethod">Method name to callback when interrupted reading text (During playback is "INTERRUPTED". Other than always "onStop" is returned) (it is in GameObject)</param>
        public static void StartTextToSpeech(string message, string locale, string callbackGameObject, string statusCallbackMethod,
                                    string startCallbackMethod, string doneCallbackMethod, string stopCallbackMethod)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "startTextToSpeech",
                    context,
                    message,
                    locale,
                    callbackGameObject,
                    statusCallbackMethod,
                    startCallbackMethod,
                    doneCallbackMethod,
                    stopCallbackMethod
                );
            }));
        }


        /// <summary>
        /// Call Android Text To Speech
        /// http://fantom1x.blog130.fc2.com/blog-entry-275.html#fantomPlugin_TextToSpeech
        ///･When it is available at the first startup, "SUCCESS_INIT" status is returned to the callback method (statusCallbackMethod).
        ///(*) Interrupted event callback (stopCallbackMethod) can only be operated with API 23 (Android 6.0) or higher.
        ///    However, it seems that the behavior varies depending on the system, so be careful.
        ///･(Error code string)
        /// https://developer.android.com/reference/android/speech/tts/TextToSpeech.html#ERROR
        /// Moreover, the following error occurs:
        /// ERROR_LOCALE_NOT_AVAILABLE : There is no voice data corresponding to the system language setting
        /// ERROR_INIT : Initialization failed
        /// ERROR_UTTERANCEPROGRESS_LISTENER_REGISTER : Event listener registration failed
        /// ERROR_UTTERANCEPROGRESS_LISTENER : Some kind of error of event acquisition
        /// 
        /// 
        /// テキスト読み上げを開始する
        /// http://fantom1x.blog130.fc2.com/blog-entry-275.html#fantomPlugin_TextToSpeech
        ///・初回起動時には使用可能なとき、コールバックメソッド（statusCallbackMethod ）に "SUCCESS_INIT" ステータスが返ってくる。
        ///※中断イベントコールバック（stopCallbackMethod）は API23(Android6.0) 以上でのみ動作可。ただし端末により挙動が異なるそうなので使用する際には注意。
        ///・エラーコード（文字列）は以下を参照
        /// https://developer.android.com/reference/android/speech/tts/TextToSpeech.html#ERROR
        /// また上記とは別に以下のエラーが返ることもある。
        /// ERROR_LOCALE_NOT_AVAILABLE：端末の言語設定に対応した音声データがない。
        /// ERROR_INIT：初期化に失敗（=UNKOWN と同義）。
        /// ERROR_UTTERANCEPROGRESS_LISTENER_REGISTER：イベントリスナー登録に失敗。
        /// ERROR_UTTERANCEPROGRESS_LISTENER：イベント取得の何らかのエラー（=UNKOWN と同義）。
        /// </summary>
        /// <param name="message">Reading text string</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="statusCallbackMethod">Method name to callback when returns status (including error)</param>
        /// <param name="startCallbackMethod">Method name to callback when start reading text (Always "onStart" is returned) (it is in GameObject)</param>
        /// <param name="doneCallbackMethod">Method name to callback when finish reading text (Always "onDone" is returned) (it is in GameObject)</param>
        /// <param name="stopCallbackMethod">Method name to callback when interrupted reading text (During playback is "INTERRUPTED". Other than always "onStop" is returned) (it is in GameObject)</param>
        public static void StartTextToSpeech(string message, string callbackGameObject = "", string statusCallbackMethod = "",
                                    string startCallbackMethod = "", string doneCallbackMethod = "", string stopCallbackMethod = "")
        {
            StartTextToSpeech(message, "", 
                callbackGameObject, statusCallbackMethod, startCallbackMethod, doneCallbackMethod, stopCallbackMethod);
        }



        /// <summary>
        /// Interruption of text reading (Do not release resource)
        ///(*) When changing languages, it is necessary to release with 'ReleaseTextToSpeech()'.
        /// 
        /// テキスト読み上げを中断する（リソース解放はしない）
        ///※言語を変更する際は、ReleaseTextToSpeech() で解放する必要がある。
        /// </summary>
        public static void StopTextToSpeech()
        {
            using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
            {
                androidSystem.CallStatic(
                    "stopTextToSpeech"
                );
            }
        }


        /// <summary>
        /// Release Text To Speech instance (Resource release)
        ///(*) It is necessary also when changing languages.
        ///･Even when "AndroidPlugin.Release()" is called, it is executed on the native side.
        ///
        /// 
        /// テキスト読み上げのオブジェクトをリリース（リソース解放する）
        ///※言語を変更する際にも必要になる。
        ///・AndroidPlugin.Release() でも解放される。
        /// </summary>
        public static void ReleaseTextToSpeech()
        {
            using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
            {
                androidSystem.CallStatic(
                    "releaseTextToSpeech"
                );
            }
        }



        /// <summary>
        /// Get utterance Speed (1.0f: Normal speed)
        /// 
        /// 発声速度を取得する（1.0f が通常速度）
        /// </summary>
        /// <returns>0.5f~1.0f~2.0f</returns>
        public static float GetTextToSpeechSpeed()
        {
            using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
            {
                return androidSystem.CallStatic<float>(
                    "getTextToSpeechSpeed"
                );
            }
        }


        /// <summary>
        /// Set utterance Speed (1.0f: Normal speed)
        /// 
        /// 発声速度を設定する（1.0f が通常速度）
        /// </summary>
        /// <param name="newSpeed">0.5f~1.0f~2.0f</param>
        /// <returns>Speed after setting : 0.5f~1.0f~2.0f</returns>
        public static float SetTextToSpeechSpeed(float newSpeed)
        {
            using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
            {
                return androidSystem.CallStatic<float>(
                    "setTextToSpeechSpeed",
                    newSpeed
                );
            }
        }


        /// <summary>
        /// Add utterance Speed (1.0f: Normal speed)
        /// 
        /// 発声速度を加減する（1.0f が通常速度）
        /// </summary>
        /// <param name="addSpeed">0.5f~1.0f~2.0f</param>
        /// <returns>Speed after addition : 0.5f~1.0f~2.0f</returns>
        public static float AddTextToSpeechSpeed(float addSpeed)
        {
            using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
            {
                return androidSystem.CallStatic<float>(
                    "addTextToSpeechSpeed",
                    addSpeed
                );
            }
        }


        /// <summary>
        /// Reset utterance Speed (-> 1.0f: Normal speed)
        ///･Same as SetTextToSpeechSpeed(1f)
        ///
        /// 発声速度を元に戻す（オリジナルの速度）
        ///・SetTextToSpeechSpeed(1f) のショートカット（オリジナル=0f の間違いを防ぐため）。
        /// </summary>
        public static float ResetTextToSpeechSpeed()
        {
            return SetTextToSpeechSpeed(1.0f);  //1.0f is normal speed  //1.0f が通常速度
        }



        /// <summary>
        /// Get utterance Pitch (1.0f: Normal pitch)
        /// 
        /// 発声音程を取得する（1.0f が通常速度）
        /// </summary>
        /// <returns>0.5f~1.0f~2.0f</returns>
        public static float GetTextToSpeechPitch()
        {
            using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
            {
                return androidSystem.CallStatic<float>(
                    "getTextToSpeechPitch"
                );
            }
        }


        /// <summary>
        /// Set utterance Pitch (1.0f: Normal pitch)
        /// 
        /// 発声音程を設定する（1.0f が通常音程）
        /// </summary>
        /// <param name="newPitch">0.5f~1.0f~2.0f</param>
        /// <returns>Pitch after setting : 0.5f~1.0f~2.0f</returns>
        public static float SetTextToSpeechPitch(float newPitch)
        {
            using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
            {
                return androidSystem.CallStatic<float>(
                    "setTextToSpeechPitch",
                    newPitch
                );
            }
        }


        /// <summary>
        /// Add utterance Pitch (1.0f: Normal pitch)
        /// 
        /// 発声音程を加減する（1.0f が通常音程）
        /// </summary>
        /// <param name="addPitch">Pitch to be added: 0.5f~1.0f~2.0f</param>
        /// <returns>Pitch after addition : 0.5f~1.0f~2.0f</returns>
        public static float AddTextToSpeechPitch(float addPitch)
        {
            using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
            {
                return androidSystem.CallStatic<float>(
                    "addTextToSpeechPitch",
                    addPitch
                );
            }
        }


        /// <summary>
        /// Reset utterance Pitch (-> 1.0f: Normal Pitch)
        ///･Same as SetTextToSpeechPitch(1f)
        ///
        /// 発声音程を元に戻す（オリジナルの音程）
        ///・SetTextToSpeechPitch(1f) のショートカット（オリジナル=0f の間違いを防ぐため）。
        /// </summary>
        public static float ResetTextToSpeechPitch()
        {
            return SetTextToSpeechPitch(1.0f);  //1.0f is normal pitch  //1.0f が通常音程
        }





        //========================================================================
        // Listening status
        //(*) Unreleased listening is a cause of memory leak, so it is better to always release it when not using it.
        //(*) Application 'Pause -> Resume' with 'remove -> set' is unnecessary (automatically stopped → resumed).
        //(*) It is released even when 'AndroidPlugin.Release()' (It is automatically released even when the application is quit).
        //
        //
        // ステータスのリスニング
        //※リスニングの未解放はメモリリークの原因となるため、利用しないときは常に解放する方が良い。
        //※コールバックの登録は唯一となる（常に上書き。最後に登録したものが有効となる）。
        //※AndroidPlugin.Release() 時でも解放される（アプリケーション終了時にも自動的に解放される）。
        // (※「AndroidManifest-FullPlugin_～.xml」をリネームして使う)
        //========================================================================

        /// <summary>
        /// Start listening to the status of the battery
        ///(*) Callback registration becomes unique (always overwritten, the last registered one will be valid).
        ///(*) It is unnecessary in the application resume(return from pause)  (automatically restart).
        ///
        /// 
        /// バッテリーのステータスリスニングを開始する
        ///※コールバックの登録はユニークとなる（常に上書き。最後に登録したものが有効となる）。
        ///※アプリケーションの Resume(Pauseから復帰)では不要（自動的に再開される）。
        /// </summary>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="callbackMethod">Method name to callback when returns status</param>
        public static void StartBatteryStatusListening(string callbackGameObject, string callbackMethod)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "startBatteryListening",
                    context,
                    callbackGameObject,
                    callbackMethod
                );
            }));
        }


        /// <summary>
        /// Stop listening to the status of the battery (release)
        ///(*) Unreleased listening is a cause of memory leak, so it is better to always release it when not using it.
        ///(*) It is unnecessary in the application pause (automatically stop).
        ///
        /// 
        /// バッテリーのステータスリスニングを停止（解放）する
        ///※リスニングの未解放はメモリリークの原因となるため、利用しないときは常に解放する方が良い。
        ///※アプリケーションの Pause(一時停止)では不要（自動的に停止される）。
        /// </summary>
        public static void StopBatteryStatusListening()
        {
            using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
            {
                androidSystem.CallStatic(
                    "stopBatteryListening"
                );
            }
        }




        /// <summary>
        /// Start listening to the status of the CPU Rate
        ///(*) Note that the measurement interval is shorter the higher the load.
        ///(*) If the load of the application is too high, there is a possibility that acquisition of information may fail (there are cases where some of them are missing).
        ///(*) Callback registration becomes unique (always overwritten, the last registered one will be valid).
        ///(*) It is unnecessary in the application resume(return from pause)  (automatically restart).
        ///
        /// 
        /// CPU使用率ののステータスリスニングを開始する
        ///※計測間隔は短いほど負荷が高いので注意。
        ///※あまりにアプリの負荷が高いときは、情報取得に失敗する可能性がある（一部抜け落ちることがある）。
        ///※コールバックの登録はユニークとなる（常に上書き。最後に登録したものが有効となる）。
        ///※アプリケーションの Resume(Pauseから復帰)では不要（自動的に再開される）。
        /// </summary>
        /// <param name="callbackGameObject">コールバックするヒエラルキーの GameObject 名</param>
        /// <param name="callbackMethod">コールバックするメソッド名</param>
        /// <param name="interval">計測間隔（1～600）[秒]</param>
        public static void StartCpuRateListening(string callbackGameObject, string callbackMethod, float interval)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "startCpuRateListening",
                    context,
                    callbackGameObject,
                    callbackMethod,
                    interval
                );
            }));
        }


        //(*) Without measurement interval (current value) specification overload
        //※計測間隔指定なし（現在のまま）のオーバーロード
        /// <summary>
        /// Start listening to the status of the CPU Rate
        ///(*) If the load of the application is too high, there is a possibility that acquisition of information may fail (there are cases where some of them are missing).
        ///(*) Callback registration becomes unique (always overwritten, the last registered one will be valid).
        ///(*) It is unnecessary in the application resume(return from pause)  (automatically restart).
        ///
        /// 
        /// CPU使用率ののステータスリスニングを開始する
        ///※あまりにアプリの負荷が高いときは、情報取得に失敗する可能性がある（一部抜け落ちることがある）。
        ///※コールバックの登録はユニークとなる（常に上書き。最後に登録したものが有効となる）。
        ///※アプリケーションの Resume(Pauseから復帰)では不要（自動的に再開される）。
        /// </summary>
        /// <param name="callbackGameObject">コールバックするヒエラルキーの GameObject 名</param>
        /// <param name="callbackMethod">コールバックするメソッド名</param>
        public static void StartCpuRateListening(string callbackGameObject, string callbackMethod)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "startCpuRateListening",
                    context,
                    callbackGameObject,
                    callbackMethod
                );
            }));
        }


        /// <summary>
        /// Stop listening to the status of the CPU Rate (release)
        ///(*) Unreleased listening is a cause of memory leak, so it is better to always release it when not using it.
        ///(*) It is unnecessary in the application pause (automatically stop).
        ///
        /// 
        /// CPU使用率ののステータスリスニングを停止（解放）する
        ///※リスニングの未解放はメモリリークの原因となるため、利用しないときは常に解放する方が良い。
        ///※アプリケーションの Pause(一時停止)では不要（自動的に停止される）。
        /// </summary>
        public static void StopCpuRateListening()
        {
            using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
            {
                androidSystem.CallStatic(
                    "stopCpuRateListening"
                );
            }
        }




        //========================================================================
        // Get Android's configuration change
        //·The changed status is transmitted from Android when the configuration changes.
        //(*) Application 'Pause -> Resume' with 'remove -> set' is unnecessary (automatically stopped → resumed).
        //(*) It is released even when 'AndroidPlugin.Release()' (It is automatically released even when the application is quit).
        // (*Requied: rename and use 'AndroidManifest-FullPlugin_~.xml')
        //
        //
        // Android のコンフィグの変化を取得する
        //・コンフィグが変化したタイミングで Android から変化後のステータスが送信される。
        //※コールバックの登録は唯一となる（常に上書き。最後に登録したものが有効となる）。
        //※アプリケーションの Pause → Resume での削除→再登録は不要（自動的に停止→再開される）。
        //※AndroidPlugin.Release() 時でも解放される（アプリケーション終了時にも自動的に解放される）。
        // (※「AndroidManifest-FullPlugin_～.xml」をリネームして使う)
        //========================================================================

        /// <summary>
        /// Register screen rotation callback
        ///(*) Callback registration becomes unique (always overwritten, the last registered one will be valid).
        ///(*) The following attribute is required for "activity" tag of "AndroidManifest.xml" (* In the case of Unity, it is added by default).
        /// 'android:configChanges="orientation|screenSize"'
        ///·Normally, for applications that rotate the screen in four directions, add the following attributes to the 'activity' tag of 'AndroidManifest.xml'.
        /// 'android:screenOrientation="sensor"'
        ///(* Included by default in 'AndroidManifest-FullPlugin_Sensor.xml')
        /// https://developer.android.com/guide/topics/manifest/activity-element.html
        /// 
        /// 
        /// 画面回転のコールバックを登録する
        ///※コールバックの登録はユニークとなる（常に上書き。最後に登録したものが有効となる）。
        ///※「AndroidManifest.xml」の「activity」タグに以下の属性が必要である（※Unity の場合、デフォルトで追加されている）。
        /// 'android:configChanges="orientation|screenSize"'
        ///・通常、４方向に画面回転するアプリには「AndroidManifest.xml」の「activity」タグに以下の属性を付ける。
        /// 'android:screenOrientation="sensor"'
        /// (※「AndroidManifest-FullPlugin_Sensor.xml」にはデフォルトで含まれている)
        /// https://developer.android.com/guide/topics/manifest/activity-element.html
        /// </summary>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="callbackMethod">Method name to callback when returns status</param>
        public static void SetOrientationChangeListener(string callbackGameObject, string callbackMethod)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "setOrientationChangeListener",
                    context,
                    callbackGameObject,
                    callbackMethod
                );
            }));
        }


        /// <summary>
        /// Release screen rotation callback
        ///(*)'AndroidPlugin.Release()' is also released.
        ///
        /// 画面回転のコールバックの解除
        ///※AndroidPlugin.Release() でも解除される。
        /// </summary>
        public static void RemoveOrientationChangeListener()
        {
            using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
            {
                androidSystem.CallStatic(
                    "removeOrientationChangeListener"
                );
            }
        }





        //========================================================================
        // Get values of Android sensor
        // http://fantom1x.blog130.fc2.com/blog-entry-294.html
        //·The sensor can choose the acquisition interval, but the higher the speed, the higher the load, so the minimum speed (200 ms (5 fps)) is recommended.
        //·Input.acceleration, gyro, compass etc, those that can be obtained by Unity, it seems that the load is light.
        //(*) Note that the installation of each sensor differs depending on the device. It is better to filter with sensors that are available for Google Play.
        // (Using Google Play filters to target specific sensor configurations)
        //  https://developer.android.com/guide/topics/sensors/sensors_overview.html#sensors-configs
        //(*) Note that use of each sensor is also restricted by API Level (as noted in the 'SensorType' comment).
        //  https://developer.android.com/reference/android/hardware/Sensor.html#TYPE_ACCELEROMETER
        //  https://developer.android.com/guide/topics/manifest/uses-sdk-element.html#ApiLevels
        //(*) Depending on the sensor, permissions may be required (as noted in the 'SensorType' comment).
        //(*) Callback registration is unique for each sensor (always overwritten, the last one registered will be valid).
        //(*) Application 'Pause -> Resume' with 'remove -> set' is unnecessary (automatically stopped → resumed).
        //(*) It is released even when 'AndroidPlugin.Release()' (It is automatically released even when the application is quit).
        // (Requied: rename and use 'AndroidManifest-FullPlugin_~.xml')
        //
        //
        // Android のセンサーの値を取得する
        // http://fantom1x.blog130.fc2.com/blog-entry-294.html
        //・センサーは取得間隔を選べるが、速度が速いほど負荷が高くなるため、最低速度（200ms (5fps)）で良い。
        //・Input.acceleration, gyro, compass 等、Unity で取得できるものは、そちらの方が負荷が軽いと思われる。
        //※各センサーの搭載は端末の種類によって違うので注意。Google Play では利用できるセンサーでフィルタリングした方が良い。
        // (Using Google Play filters to target specific sensor configurations)
        //  https://developer.android.com/guide/topics/sensors/sensors_overview.html#sensors-configs
        //※各センサーの利用は API Level でも制限されるので注意（SensorType のコメントに記してある）。
        //  https://developer.android.com/reference/android/hardware/Sensor.html#TYPE_ACCELEROMETER
        //  https://developer.android.com/guide/topics/manifest/uses-sdk-element.html#ApiLevels
        //※センサーによってはパーミッションが必要になることがある（SensorType のコメントに記してある）。
        //※コールバックの登録はセンサーごとにユニークとなる（常に上書き。最後に登録したものが有効となる）。
        //※アプリケーションの Pause → Resume での削除→再登録は不要（自動的に停止→再開される）。
        // (※「AndroidManifest-FullPlugin_～.xml」をリネームして使う)
        //========================================================================


        /// <summary>
        /// Get supported for each sensor
        /// 
        /// 各センサーのサポートを取得
        /// </summary>
        /// <param name="sensorType">Sensor type constant</param>
        /// <returns>true = supported</returns>
        public static bool IsSupportedSensor(int sensorType)
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
                    {
                        return androidSystem.CallStatic<bool>(
                            "isSupportedSensor",
                            context,
                            sensorType
                        );
                    }
                }
            }
        }

        //(*) SensorType overload
        /// <summary>
        /// Get supported for each sensor
        /// 
        /// 各センサーのサポートを取得
        /// </summary>
        /// <param name="sensorType">Sensor type constant</param>
        /// <returns>true = supported</returns>
        public static bool IsSupportedSensor(SensorType sensorType)
        {
            return IsSupportedSensor((int)sensorType);
        }


        /// <summary>
        /// Register listener for each sensor
        ///·Sensor type constant
        /// https://developer.android.com/reference/android/hardware/Sensor.html#TYPE_ACCELEROMETER
        ///·Sensor delay constant (Detection Interval) Constant is preferably (3: Normal [200 ms]) (* to reduce load).
        /// https://developer.android.com/reference/android/hardware/SensorManager.html#SENSOR_DELAY_FASTEST
        ///(*) Callback registration is unique for each sensor (always overwritten, the last one registered will be valid).
        ///(*) Application 'Pause -> Resume' with 'remove -> set' is unnecessary (automatically stopped → resumed).
        ///(*) It is unnecessary in the application resume(return from pause)  (automatically restart).
        ///
        ///
        /// 各センサーのリスナー登録
        ///・センサー種類定数
        /// https://developer.android.com/reference/android/hardware/Sensor.html#TYPE_ACCELEROMETER
        ///・センサー速度(検出間隔)定数はなるべく（3: Normal[200ms]）が良い（※負荷を軽減するため）。
        /// https://developer.android.com/reference/android/hardware/SensorManager.html#SENSOR_DELAY_FASTEST
        ///※コールバックの登録はセンサーごとにユニークとなる（常に上書き。最後に登録したものが有効となる）。
        ///※アプリケーションの Resume(Pauseから復帰)では不要（自動的に再開される）。
        /// </summary>
        /// <param name="sensorType">Sensor type constant</param>
        /// <param name="sensorDelay">Sensor delay constant</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="callbackMethod">Method name to callback when returns status</param>
        public static void SetSensorListener(int sensorType, int sensorDelay, string callbackGameObject, string callbackMethod)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "setSensorListener",
                    context,
                    sensorType,
                    sensorDelay,
                    callbackGameObject,
                    callbackMethod
                );
            }));
        }

        //(*) SensorType overload
        /// <summary>
        /// Register callback for each sensor
        ///·Sensor type constant
        /// https://developer.android.com/reference/android/hardware/Sensor.html#TYPE_ACCELEROMETER
        ///·Sensor delay constant (Detection Interval) Constant is preferably (3: Normal [200 ms]) (* to reduce load).
        /// https://developer.android.com/reference/android/hardware/SensorManager.html#SENSOR_DELAY_FASTEST
        ///(*) Callback registration is unique for each sensor (always overwritten, the last one registered will be valid).
        ///(*) Application 'Pause -> Resume' with 'remove -> set' is unnecessary (automatically stopped → resumed).
        ///(*) It is unnecessary in the application resume(return from pause)  (automatically restart).
        ///
        ///
        /// 各センサーのリスナー登録
        ///・センサー種類定数
        /// https://developer.android.com/reference/android/hardware/Sensor.html#TYPE_ACCELEROMETER
        ///・センサー速度(検出間隔)定数はなるべく（3: Normal[200ms]）が良い（※負荷を軽減するため）。
        /// https://developer.android.com/reference/android/hardware/SensorManager.html#SENSOR_DELAY_FASTEST
        ///※コールバックの登録はセンサーごとにユニークとなる（常に上書き。最後に登録したものが有効となる）。
        ///※アプリケーションの Resume(Pauseから復帰)では不要（自動的に再開される）。
        /// </summary>
        /// <param name="sensorType">Sensor type constant</param>
        /// <param name="sensorDelay">Sensor delay constant</param>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="callbackMethod">Method name to callback when returns status</param>
        public static void SetSensorListener(SensorType sensorType, SensorDelay sensorDelay, string callbackGameObject, string callbackMethod)
        {
            SetSensorListener((int)sensorType, (int)sensorDelay, callbackGameObject, callbackMethod);
        }


        /// <summary>
        /// Release callback for each sensor
        ///(*)'AndroidPlugin.Release()' is also released.
        ///(*) It is unnecessary in the application pause (automatically stop).
        ///
        ///
        /// 各センサーの解除
        ///※AndroidPlugin.Release() でも解除される。
        ///※アプリケーションの Pause(一時停止)では不要（自動的に停止される）。
        /// </summary>
        /// <param name="sensorType">Sensor type constant</param>
        public static void RemoveSensorListener(int sensorType)
        {
            using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
            {
                androidSystem.CallStatic(
                    "removeSensorListener",
                    sensorType
                );
            }
        }

        //SensorType overload
        /// <summary>
        /// Release callback for each sensor
        ///(*)'AndroidPlugin.Release()' is also released.
        ///(*) It is unnecessary in the application pause (automatically stop).
        ///
        ///
        /// 各センサーの解除
        ///※AndroidPlugin.Release() でも解除される。
        ///※アプリケーションの Pause(一時停止)では不要（自動的に停止される）。
        /// </summary>
        /// <param name="sensorType">Sensor type constant</param>
        public static void RemoveSensorListener(SensorType sensorType)
        {
            RemoveSensorListener((int)sensorType);
        }


        /// <summary>
        /// Release callback for all sensors
        ///(*)'AndroidPlugin.Release()' is also released.
        ///
        /// 
        /// 全センサーの解除
        ///※AndroidPlugin.Release() でも解除される。
        /// </summary>
        public static void ReleaseSensors()
        {
            using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
            {
                androidSystem.CallStatic(
                    "releaseSensors"
                );
            }
        }




        //==========================================================
        // Confirm Device Credentials (fingerprint, pattern, PIN, password, etc. depending on user device setting)
        //(*) API 21 (Android 5.0) or higher
        //
        // デバイス認証（指紋・パターン・PIN・パスワード等。端末の設定による）
        //※API 21 (Android 5.0) 以上
        //==========================================================


        /// <summary>
        /// Whether Confirm Device Credentials (fingerprint, pattern, PIN, password, etc. depending on user device setting) is available (API 21 or higher)
        ///・It is false when unavailable device or security setting is turned off.
        ///(*) It is always false when it is below API 21 (Android 5.0).
        /// 
        /// 
        /// デバイス認証（指紋・パターン・PIN・パスワード等。端末の設定による）が利用可能かどうかを取得する（API 21 以上）
        ///・利用できないデバイス、またはセキュリティ設定がオフになっているとき false となる。
        ///※API 21 (Android 5.0) より下のときは常に false になる。
        /// </summary>
        /// <returns>true = supported</returns>
        public static bool IsSupportedDeviceCredentials()
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
                    {
                        return androidSystem.CallStatic<bool>(
                            "isSupportedCredentials",
                            context
                        );
                    }
                }
            }
        }


        /// <summary>
        /// Show Confirm Device Credentials screen (fingerprint, pattern, PIN, password, etc. depending on user device setting) and returns the authentication result. (API 21 or higher)
        ///·The following status string is returned in the result (JSON format).
        /// SUCCESS_CREDENTIALS : Authentication success
        /// UNAUTHORIZED_CREDENTIALS : Authentication failure or cancel.
        /// ERROR_NOT_SUPPORTED : Device authentication can not be used. Or security is turned off.
        ///·Session ID (string) should contain unique and random character string each time.
        ///(*) message (description character string) may not be reflected by the device.
        ///(*) Title and message are basically empty (because messages are set automatically by system language).
        ///
        ///
        /// デバイス認証（指紋・パターン・PIN・パスワード等。端末の設定による）を開き、結果を返す（API 21 以上）
        ///・結果は以下のステータス文字列が返る（JSON形式）。
        /// SUCCESS_CREDENTIALS : 認証成功。
        /// UNAUTHORIZED_CREDENTIALS : 認証失敗。またはキャンセル。
        /// ERROR_NOT_SUPPORTED : デバイス認証が利用できない。またはセキュリティがオフになっている。
        ///・セッションID文字列は毎回ユニークでランダムな文字列を含んだ方が良い。
        ///※message（メッセージ文字列）はデバイスによって反映されない場合がある。
        ///※title, message は基本的に空で良い（システム言語によって自動でメッセージが設定されるため）。
        /// </summary>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="callbackMethod">Method name to callback when returns status</param>
        /// <param name="sessionID">Unique SessionID string</param>
        /// <param name="title">Title string (When empty, it becomes default of the device)</param>
        /// <param name="message">Message string (When empty, it becomes default of the device)</param>
        public static void ShowDeviceCredentials(string callbackGameObject, string callbackMethod, string sessionID, string title = "", string message = "")
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "showCredentials",
                    context,
                    callbackGameObject,
                    callbackMethod,
                    sessionID,
                    title,
                    message
                );
            }));
        }


        //Character string list (used for JSON)
        const string ASCII = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz!#$%&*+-.=?@^_`|~";
        const int ASCII_LENGTH = 24;    //Random ASCII character string part length     //ランダムなアスキー文字列部分の長さ

        //Generate random SessionID string
        internal static string GenerateSessionID()
        {
            string s = "";
            for (int i = 0; i < ASCII_LENGTH; i++)
                s += ASCII.Substring(Random.Range(0, ASCII.Length), 1);
            return DateTime.Now.Ticks + " " + s;
        }





        // ==========================================================
        // Get text from the QR code scanner.
        // ZXing ("Zebra Crossing") open source project (google). [ver.3.3.2] (QR Code Scan)
        // https://github.com/zxing/zxing
        // This plugin includes deliverables distributed under the license of Apache License, Version 2.0.
        // http://www.apache.org/licenses/LICENSE-2.0
        // 
        //
        // QRコードスキャナからテキストを取得する
        // ZXing ("Zebra Crossing") open source project (google). [ver.3.3.2] (QR Code Scan)
        // https://github.com/zxing/zxing
        // このプラグインには Apache License, Version 2.0 のライセンスで配布されている成果物を含んでいます。
        // http://www.apache.org/licenses/LICENSE-2.0
        // ==========================================================


        /// <summary>
        /// Launch QR Code (Bar Code) Scanner to acquire text.
        /// Using ZXing ("Zebra Crossing") open source project (google). [ver.3.3.2]
        /// https://github.com/zxing/zxing
        /// (Apache License, Version 2.0)
        /// http://www.apache.org/licenses/LICENSE-2.0
        ///·Launch the QR Code Scanner application of ZXing and obtain the result text.
        ///·When cancellation or acquisition fails, it returns a empty character ("").
        ///·If ZXing's QR Code Scanner application is not in the device, a dialog prompting installation will be displayed.
        /// https://play.google.com/store/apps/details?id=com.google.zxing.client.android
        /// 
        /// 
        /// QRコード(バーコード)スキャナを起動してテキストを取得する。
        /// ZXing オープンソースプロジェクト（google）を利用。[ver.3.3.2]
        /// https://github.com/zxing/zxing
        /// (Apache License, Version 2.0)
        /// http://www.apache.org/licenses/LICENSE-2.0
        ///・ZXing の QRコードスキャナアプリを起動し、結果のテキストを取得する。
        ///・キャンセルまたは取得失敗したときなどは、空文字（""）を返す。
        ///・端末に ZXing の QRコードスキャナアプリが入ってない場合は、インストールを促すダイアログが表示される。
        /// https://play.google.com/store/apps/details?id=com.google.zxing.client.android
        /// </summary>
        /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
        /// <param name="callbackMethod">Method name to callback (it is in GameObject)</param>
        public static void ShowQRCodeScanner(string callbackGameObject, string callbackMethod)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM);
            context.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                androidSystem.CallStatic(
                    "showQRCodeScannerZXing",
                    context,
                    callbackGameObject,
                    callbackMethod
                );
            }));
        }





        //==========================================================
        // Control of Hardware Volume settings
        // ハードウェア音量設定の取得・設定など
        //==========================================================

        /// <summary>
        /// Get Hardware Volume (Media volume only)
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_HardVolumeGetSet
        ///･0~max (max:15? Depends on system?)
        ///
        /// 
        /// ハードウェア音量（メディア音量）を取得する
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_HardVolumeGetSet
        ///・0～max (max:15?：システムによるかも)
        /// </summary>
        /// <returns>Current Hardware Volume</returns>
        public static int GetMediaVolume()
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
                    {
                        return androidSystem.CallStatic<int>(
                            "getMediaVolume",
                            context
                        );
                    }
                }
            }
        }



        /// <summary>
        /// Get Maximum Hardware Volume (Media volume only)
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_HardVolumeGetSet
        ///･0~max (max:15? Depends on system?)
        ///
        /// 
        /// ハードウェア音量（メディア音量）の最大音量を取得する
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_HardVolumeGetSet
        ///・0～max (max:15?：システムによるかも)
        /// </summary>
        /// <returns>Maximum Hardware Volume</returns>
        public static int GetMediaMaxVolume()
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
                    {
                        return androidSystem.CallStatic<int>(
                            "getMediaMaxVolume",
                            context
                        );
                    }
                }
            }
        }



        /// <summary>
        /// Set Hardware Volume (Media volume only)
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_HardVolumeGetSet
        ///･0~max (max:15? Depends on system?)
        ///
        /// 
        /// ハードウェア音量（メディア音量）を設定する
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_HardVolumeGetSet
        ///・0～max (max:15?：システムによるかも)
        /// </summary>
        /// <param name="volume">new Hardware Volume</param>
        /// <param name="showUI">true=display system UI</param>
        /// <returns>Hardware Volume after setting</returns>
        public static int SetMediaVolume(int volume, bool showUI)
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
                    {
                        return androidSystem.CallStatic<int>(
                            "setMediaVolume",
                            context,
                            volume,
                            showUI
                        );
                    }
                }
            }
        }



        /// <summary>
        /// Add Hardware Volume (Media volume only)
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_HardVolumeGetSet
        ///･0~max (max:15? Depends on system?)
        ///
        /// 
        /// ハードウェア音量（メディア音量）を変化（加算）させる
        /// http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_HardVolumeGetSet
        ///・0～max (max:15?：システムによるかも)
        /// </summary>
        /// <param name="addVol">Volume to be added</param>
        /// <param name="showUI">true=display system UI</param>
        /// <returns>Hardware Volume after addition</returns>
        public static int AddMediaVolume(int addVol, bool showUI)
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    using (AndroidJavaClass androidSystem = new AndroidJavaClass(ANDROID_SYSTEM))
                    {
                        return androidSystem.CallStatic<int>(
                            "addMediaVolume",
                            context,
                            addVol,
                            showUI
                        );
                    }
                }
            }
        }





        //========================================================================
        // Get Hardware button press event 
        //･Rename "AndroidManifest-FullPlugin~.xml" to "AndroidManifest.xml" when receive events of hardware volume button.
        //･Cache objects for Android access to monitor input.
        // Therefore, it is better to register the listener with "OnEnable()" and release it with "OnDisable()".
        // http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_HardVolumeListener
        //
        //
        // ハードウェアキーの押下イベントを取得する
        //・ハードウェア音量操作のイベントを受け取るには、HardVolKeyOnUnityPlayerActivity または FullPluginOnUnityPlayerActivity を使う。
        //  → AndroidManifest.xml の起動 activity タグに「android:name="jp.fantom1x.plugin.android.fantomPlugin.HardVolKeyOnUnityPlayerActivity"」を指定する。
        //・入力を監視するためAndroidアクセス用オブジェクトをキャッシュする。そのためリスナーを OnEnable()で登録, OnDisable()で解除するのが良い。
        // http://fantom1x.blog130.fc2.com/blog-entry-273.html#fantomPlugin_HardVolumeListener
        //========================================================================


        /// <summary>
        /// Get Hardware button press event 
        /// 
        /// ハードウェアキーの取得など
        /// </summary>
        public static class HardKey
        {
            //Class full path of plug-in in Java
            public const string ANDROID_HARDKEY = ANDROID_PACKAGE + ".AndroidHardKey";


            //For Android's AndroidHardKey class acquisition (cached)
            //Android の AndroidHardKey クラス取得用（キャッシュ）
            private static AndroidJavaClass mAndroidHardKey;    //Cached Object

            private static AndroidJavaClass AndroidHardKey {
                get {
                    if (mAndroidHardKey == null)
                    {
                        mAndroidHardKey = new AndroidJavaClass(ANDROID_HARDKEY);
                    }
                    return mAndroidHardKey;
                }
            }


            /// <summary>
            /// Release cashed object
            ///･Also called "AndroidPlugin.Release()".
            ///
            /// キャッシュのリリース
            ///・AndroidPlugin.Release() からも呼ばれる。
            /// </summary>
            public static void ReleaseCache()
            {
                if (mAndroidHardKey != null)
                {
                    mAndroidHardKey.Dispose();
                    mAndroidHardKey = null;
                }
            }


            /// <summary>
            /// Release all listeners
            ///･Also called "AndroidPlugin.Release()".
            ///
            /// キャッシュ（リスナー）の全解除
            ///※AndroidPlugin.Release() でも解放される。
            /// </summary>
            public static void RemoveAllListeners()
            {
                RemoveKeyVolumeUpListener();
                RemoveKeyVolumeDownListener();
                ReleaseCache();
            }



            /// <summary>
            /// Register the callback (listener) when the hardware volume (media volume only) is increased.
            ///･Only one callback can be registered.
            ///
            /// 
            /// ハードウェア音量（メディア音量）を上げたときのコールバック（リスナー）を登録する
            ///・コールバックは１つのみ。
            /// </summary>
            /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
            /// <param name="callbackMethod">Method name to callback (it is in GameObject)</param>
            /// <param name="resultValue">Return value when volume is raised</param>
            public static void SetKeyVolumeUpListener(string callbackGameObject, string callbackMethod, string resultValue)
            {
                AndroidHardKey.CallStatic(
                    "setKeyVolumeUpListener",
                    callbackGameObject,
                    callbackMethod,
                    resultValue
                );
            }



            /// <summary>
            /// Register the callback (listener) when the hardware volume (media volume only) is decreased.
            ///･Only one callback can be registered.
            ///
            /// ハードウェア音量（メディア音量）を下げたときのコールバック（リスナー）を登録する
            ///・コールバックは１つのみ。
            /// </summary>
            /// <param name="callbackGameObject">GameObject name in hierarchy to callback</param>
            /// <param name="callbackMethod">Method name to callback (it is in GameObject)</param>
            /// <param name="resultValue">Return value when the volume is lowered</param>
            public static void SetKeyVolumeDownListener(string callbackGameObject, string callbackMethod, string resultValue)
            {
                AndroidHardKey.CallStatic(
                    "setKeyVolumeDownListener",
                    callbackGameObject,
                    callbackMethod,
                    resultValue
                );
            }



            /// <summary>
            /// Release the callback (listener) when the hardware volume (media volume only) is increased.
            ///･Also called "AndroidPlugin.Release()".
            ///
            /// ハードウェア音量（メディア音量）を上げたときのコールバック（リスナー）を解除する
            ///※AndroidPlugin.Release() でも解放される。
            /// </summary>
            public static void RemoveKeyVolumeUpListener()
            {
                AndroidHardKey.CallStatic(
                    "releaseKeyVolumeUpListener"
                );
            }



            /// <summary>
            /// Release the callback (listener) when the hardware volume (media volume only) is decreased.
            ///･Also called "AndroidPlugin.Release()".
            ///
            /// ハードウェア音量（メディア音量）を下げたときのコールバック（リスナー）を解除する
            ///※AndroidPlugin.Release() でも解放される。
            /// </summary>
            public static void RemoveKeyVolumeDownListener()
            {
                AndroidHardKey.CallStatic(
                    "releaseKeyVolumeDownListener"
                );
            }



            /// <summary>
            /// Set whether to control media volume by the smartphone itself by hardware buttons.
            ///･To disable the volume buttons on the smartphone, set it to 'false' if you want to operate only from the Unity side.
            ///･It is possible to receive the event and volume control with "SetMediaVolume()" or "AddMediaVolume()".
            ///
            /// 
            /// ハードウェアキーによる端末自身での音量操作可否を設定する
            ///・端末での音量操作を無効にし、Unity 側からのみ操作したいときは false に設定する。
            /// → イベントを受信して SetMediaVolume(), AddMediaVolume() で音量操作することは可能。
            /// </summary>
            /// <param name="enable">true=Volume operation on smartphone / false=Disable volume operation at the smartphone</param>
            public static void SetVolumeOperation(bool enable)
            {
                AndroidHardKey.CallStatic(
                    "setVolumeOperation",
                    enable
                );
            }
        }

    }
#endif



    //==========================================================
    // Item (widget) instance parameters class for Custom Dialog
    // カスタムダイアログ用のアイテム（ウィジェット）インスタンスパラメタ格納クラス
    //==========================================================

    //Types of items (widgets)
    //アイテム（ウィジェット）の種類
    [Serializable]
    public enum DialogItemType
    {
        Divisor,    //Divsor (View)         //分割線
        Text,       //Android-TextView      //テキスト
        Switch,     //Android-Switch        //スイッチ
        Slider,     //Android-SeekBar       //スライダー（シークバー）
        Toggle,     //Android-RadioButton   //トグル選択（ラジオボタン）
        Check,      //Android-CheckBox      //チェックボックス
    }

    //Base class for parameters of each item
    //各アイテムのパラメタ格納用 ベースクラス
    [Serializable]
    public abstract class DialogItem
    {
        public string type;         //Types of items (widgets)                  //種類
        public string key = "";     //Key to be associated with return value    //戻り値に関連付けるキー

        public DialogItem() { }

        public DialogItem(DialogItemType type, string key = "")
        {
            this.type = type.ToString();
            this.key = key;
        }

        public DialogItem Clone()
        {
            return (DialogItem)MemberwiseClone();
        }
    }

    //Parameters for Dividing Line
    //分割線用パラメタ格納クラス
    [Serializable]
    public class DivisorItem : DialogItem
    {
        public float lineHeight = 1;    //Line width (unit: dp)     //線の太さ（単位:dp）
        public int lineColor = 0;       //Line color (0 = not specified: Color format is int32 (AARRGGBB: Android-Java))    //線の色（0:指定なし）

        public DivisorItem(float lineHeight, int lineColor = 0)
            : base(DialogItemType.Divisor)
        {
            this.lineHeight = lineHeight;
            this.lineColor = lineColor;
        }

        //Unity.Color overload
        public DivisorItem(float lineHeight, Color lineColor)
            : this(lineHeight, XColor.ToIntARGB(lineColor)) { }

        public new DivisorItem Clone()
        {
            return (DivisorItem)MemberwiseClone();
        }
    }

    //Parameters for Text
    //テキスト用パラメタ格納クラス
    [Serializable]
    public class TextItem : DialogItem
    {
        public string text = "";        //Text string       //テキスト文字列
        public int textColor = 0;       //Text color (0 = not specified: Color format is int32 (AARRGGBB: Android-Java))        //文字色（0:指定なし）
        public int backgroundColor = 0; //Background color (0 = not specified: Color format is int32 (AARRGGBB: Android-Java))  //背景色（0:指定なし）
        public string align = "";       //Text alignment ("" = not specified, "center", "right" or "left")                      //文字揃え（"": 指定なし, "center", "right", "left" のいずれか）

        public TextItem(string text, int textColor = 0, int backgroundColor = 0, string align = "")
            : base(DialogItemType.Text)
        {
            this.text = text;
            this.textColor = textColor;
            this.backgroundColor = backgroundColor;
            this.align = align;
        }

        //Unity.Color overload
        public TextItem(string text, Color textColor, Color backgroundColor, string align = "")
            : this(text, XColor.ToIntARGB(textColor), XColor.ToIntARGB(backgroundColor), align) { }

        public TextItem(string text, Color textColor)
            : this(text, XColor.ToIntARGB(textColor), 0, "") { }

        public new TextItem Clone()
        {
            return (TextItem)MemberwiseClone();
        }
    }

    //Parameters for Switch
    //スイッチ用パラメタ格納クラス
    [Serializable]
    public class SwitchItem : DialogItem
    {
        public string text = "";        //Text string       //テキスト文字列
        public bool defChecked;         //Initial state of switch (true = On / false = Off)     //初期オン・オフ
        public int textColor = 0;       //Text color (0 = not specified: Color format is int32 (AARRGGBB: Android-Java))    //0:指定なし
        public string changeCallbackMethod = ""; //Method name to real-time callback when the value of the switch is changed (it is in GameObject)  //リアルタイムにスイッチを押したときのコールバックメソッド名。「OK」のときの結果とは別に送信される（戻り値は "key=value" のみ）。

        public SwitchItem(string text, string key, bool defChecked, int textColor = 0, string changeCallbackMethod = "")
            : base(DialogItemType.Switch, key)
        {
            this.text = text;
            this.defChecked = defChecked;
            this.textColor = textColor;
            this.changeCallbackMethod = changeCallbackMethod;
        }

        //Unity.Color overload
        public SwitchItem(string text, string key, bool defChecked, Color textColor, string changeCallbackMethod = "")
            : this(text, key, defChecked, XColor.ToIntARGB(textColor), changeCallbackMethod) { }

        public new SwitchItem Clone()
        {
            return (SwitchItem)MemberwiseClone();
        }
    }

    //Parameters for CheckBox
    //(*) Basically just look different, CheckItem has the same function as SwitchItem.
    //チェックボックス用パラメタ格納クラス
    //※基本的に見た目が違うだけで、SwitchItem と機能は同じです。
    [Serializable]
    public class CheckItem : DialogItem
    {
        public string text = "";        //Text string               //テキスト文字列
        public bool defChecked;         //initial on/off            //初期オン・オフ
        public int textColor = 0;       //Text color (0 = default)  //テキスト文字色（0:指定なし）

        //Method name to real-time callback when the value of the checkbox is changed (it is in GameObject). It is transmitted separately from the result of "OK" (return value is "key=value" only).
        //リアルタイムにチェック切り替えのコールバックメソッド名。「OK」のときの結果とは別に送信される（戻り値は "key=value" のみ）。
        public string changeCallbackMethod = "";

        public CheckItem(string text, string key, bool defChecked, int textColor = 0, string changeCallbackMethod = "")
            : base(DialogItemType.Check, key)
        {
            this.text = text;
            this.defChecked = defChecked;
            this.textColor = textColor;
            this.changeCallbackMethod = changeCallbackMethod;
        }

        //Color オーバーロード
        public CheckItem(string text, string key, bool defChecked, Color textColor, string changeCallbackMethod = "")
            : this(text, key, defChecked, XColor.ToIntARGB(textColor), changeCallbackMethod) { }

        public new CheckItem Clone()
        {
            return (CheckItem)MemberwiseClone();
        }
    }

    //Parameters for Slider
    //スライダー用パラメタ格納クラス
    [Serializable]
    public class SliderItem : DialogItem
    {
        public string text = "";        //Text string       //テキスト文字列
        public float value;             //Initial value     //初期値
        public float min;               //Minimum value     //取り得る最小値
        public float max;               //Maximum value     //取り得る最大値
        public int digit;               //Number of decimal places (0 = integer, 1~3 = after decimal point)                 //小数点以下桁数（0:整数, 1～3:小数点以下桁数）
        public int textColor = 0;       //Text color (0 = not specified: Color format is int32 (AARRGGBB: Android-Java))    //文字色（0:指定なし）
        public string changeCallbackMethod = ""; //Method name to real-time callback when the value of the slider is changed (it is in GameObject)  //リアルタイムにスライダーで値を変化させたときのコールバックメソッド名。「OK」のときの結果とは別に送信される（戻り値は "key=value" のみ）。

        public SliderItem(string text, string key, float value, float min = 0, float max = 100, int digit = 0, int textColor = 0, string changeCallbackMethod = "")
            : base(DialogItemType.Slider, key)
        {
            this.text = text;
            this.value = value;
            this.min = min;
            this.max = max;
            this.digit = digit;
            this.textColor = textColor;
            this.changeCallbackMethod = changeCallbackMethod;
        }

        //Unity.Color overload
        public SliderItem(string text, string key, float value, float min, float max, int digit, Color textColor, string changeCallbackMethod = "")
            : this(text, key, value, min, max, digit, XColor.ToIntARGB(textColor), changeCallbackMethod) { }

        public new SliderItem Clone()
        {
            return (SliderItem)MemberwiseClone();
        }
    }

    //トグルボタン（グループ）用パラメタ格納クラス
    //Parameters for Toggle (Group)
    [Serializable]
    public class ToggleItem : DialogItem
    {
        public string[] items;          //Item strings (Array)                                      //選択肢テキスト
        public string[] values;         //Value for each item (Array)                               //各選択肢に対する値。（文字列）戻り値になる。
        public string defValue = "";    //Initial value ([*]Prioritize checkedItem)                 //デフォルトの値（※checkedItem より優先）
        public int checkedItem;         //Initial index (nothing defValue or not found defValue)    //デフォルトのインデクス（※defValueが未設定 or 見つからないとき反映）
        public int textColor = 0;       //Text color (0 = not specified: Color format is int32 (AARRGGBB: Android-Java))    //文字色（0:指定なし）
        public string changeCallbackMethod = ""; //Method name to real-time callback when the value of the toggle is changed (it is in GameObject)  //リアルタイムにトグルボタンを押したときのコールバックメソッド名。「OK」のときの結果とは別に送信される（戻り値は "key=value" のみ）。

        //Initial value constructor
        public ToggleItem(string[] items, string key, string[] values, string defValue, int textColor = 0, string changeCallbackMethod = "")
            : base(DialogItemType.Toggle, key)
        {
            this.items = items;
            this.values = values;
            this.defValue = defValue;
            this.textColor = textColor;
            this.changeCallbackMethod = changeCallbackMethod;
        }

        //Initial index constructor
        public ToggleItem(string[] items, string key, string[] values, int checkedItem, int textColor = 0, string changeCallbackMethod = "")
            : base(DialogItemType.Toggle, key)
        {
            this.items = items;
            this.values = values;
            this.checkedItem = checkedItem;
            this.textColor = textColor;
            this.changeCallbackMethod = changeCallbackMethod;
        }

        //Unity.Color overload
        public ToggleItem(string[] items, string key, string[] values, string defValue, Color textColor, string changeCallbackMethod = "")
            : this(items, key, values, defValue, XColor.ToIntARGB(textColor), changeCallbackMethod) { }

        public ToggleItem(string[] items, string key, string[] values, int checkedItem, Color textColor, string changeCallbackMethod = "")
            : this(items, key, values, checkedItem, XColor.ToIntARGB(textColor), changeCallbackMethod) { }

        public new ToggleItem Clone()
        {
            ToggleItem obj = (ToggleItem)MemberwiseClone();
            obj.items = (string[])items.Clone();
            obj.values = (string[])values.Clone();
            return obj;
        }
    }


    //==========================================================
    // For data acquisition (Mainly used for conversion from JSON)
    //·Information that can be acquired varies depending on the provider (storage).
    // In order of Local storage > SD card > Cloud storage becomes more.
    //(*) Since the function is largely limited in cloud storage, it is usually better to download it to local storage and handle it.
    //
    // データ取得用クラス（主にJSONからの変換に使用する）
    //・取得できる情報はプロバイダ（ストレージ）によって違うので注意。
    //  ローカルストレージ ＞ SDカード ＞ クラウドストレージ の順に取得できる情報が多くなる。
    //※クラウドストレージでは大幅に機能が制限されるので、通常はローカルストレージにダウンロードして扱う方が良い。
    //==========================================================

    //For content status
    //コンテンツ情報用
    [Serializable]
    public class ContentInfo
    {
        public string path = "";        //Absolute file path
        public string name = "";        //file (folder) name
        public string uri = "";         //URI returned by the application           //アプリが返す URI
        public string fileUri = "";     //like "content://media/external/file/~"    //ローカルストレージが返す URI
        public string mimeType = "";    //e.g. "text/plain"
        public long size;               //File size [byte]

        public ContentInfo() { }

        public ContentInfo(string path, string uri)
        {
            this.path = path;
            this.uri = uri;
        }

        public override string ToString()
        {
            return JsonUtility.ToJson(this);    //for debug
        }
    }


    //(*) Based on ContentInfo
    //For image information (Fields that failed to get are 0 or null / empty)
    //画像情報用（取得に失敗したフィールドは 0 または null、空になる）
    [Serializable]
    public class ImageInfo : ContentInfo
    {
        public int width;               //(*) It may be impossible to acquire in cloud storage.
        public int height;              //(*) It may be impossible to acquire in cloud storage.

        //(*) Note that the following information is not likely to be acquired depending on the application (returned URI) and storage state.
        //※以下の情報はアプリ（返される URI）や保存状態によって、取得できない可能性が高いので注意。
        public int orientation;         //Image rotation angle [degrees] (not set = 0)  //画像回転角度 [度] (なし = 0)

        public ImageInfo() { }

        public ImageInfo(string path, int width, int height)
            : this(path, "", width, height) { }

        public ImageInfo(string path, string uri, int width, int height) {
            this.path = path;
            this.uri = uri;
            this.width = width;
            this.height = height;
        }

        public override string ToString()
        {
            return JsonUtility.ToJson(this);    //for debug
        }
    }

    //(*) Based on ContentInfo
    //For video information (Fields that failed to get are 0 or null / empty)
    //動画情報用（取得に失敗したフィールドは 0 または null / 空になる）
    [Serializable]
    public class VideoInfo : ContentInfo
    {
        public int width;               //(*) It may be impossible to acquire in cloud storage.
        public int height;              //(*) It may be impossible to acquire in cloud storage.
        public long duration;           //Video duration [ms]  (*) It may be impossible to acquire in cloud storage.

        //(*) Note that the following information is not likely to be acquired depending on the application (returned URI) and storage state.
        //※以下の情報はアプリ（返される URI）や保存状態によって、取得できない可能性が高いので注意。
        public bool is360video;         //true = 360 degrees video, false = normal video, not set or acquisition failure.   //true = 306度動画, false = 通常動画、フラグがセットされてない、または取得失敗
        public string title;
        public string artist;

        public VideoInfo() { }

        public VideoInfo(string path, int width, int height)
            : this(path, "", width, height) { }

        public VideoInfo(string path, string uri, int width, int height) {
            this.path = path;
            this.uri = uri;
            this.width = width;
            this.height = height;
        }

        public override string ToString()
        {
            return JsonUtility.ToJson(this);    //for debug
        }
    }

    //(*) Based on ContentInfo
    //For audio information (Fields that failed to get are 0 or null / empty)
    //音声情報用（取得に失敗したフィールドは 0 または null / 空になる）
    [Serializable]
    public class AudioInfo : ContentInfo
    {
        public long duration;           //Audio duration [ms]  (*) It may be impossible to acquire in cloud storage.

        //(*) Note that the following information is not likely to be acquired depending on the API (returned URI) and storage state.
        //※以下の情報はアプリ（返される URI）や保存状態によって、取得できない可能性が高いので注意。
        public string title;
        public string artist;

        public AudioInfo() { }

        public AudioInfo(string path)
            : this(path, "") { }

        public AudioInfo(string path, string uri) {
            this.path = path;
            this.uri = uri;
        }

        public override string ToString()
        {
            return JsonUtility.ToJson(this);    //for debug
        }
    }



    //For battery status information
    //バッテリーステータス用
    [Serializable]
    public class BatteryInfo
    {
        public string timestamp;    //Time when information was obtained.           //情報を取得した時刻
        public int level;           //The remaining battery capacity.               //残量
        public int scale;           //Maximum amount of battery.                    //最大量
        public int percent;         //％（level/scale*100）(= UnityEngine.SystemInfo.batteryLevel*100)
        public string status;       //Charge state (= UnityEngine.BatteryStatus)    //充電状態を表す
        public string health;       //Battery condition.                            //コンディションを表す
        public float temperature;   //Battery temperature (℃).                     //バッテリ温度（℃）
        public float voltage;       //The current battery voltage level.(V)         //電圧 (V)

        public override string ToString()
        {
            return JsonUtility.ToJson(this);    //for debug
        }
    }


    //For CPU rate status
    //CPU 使用率ステータス用
    //･ratio : (CPU0 + CPU1 +...) = 100% (There is an error after the decimal point)      //（小数点以下の誤差あり）
    //･(user + nice + system + idle) = 100% (There is an error after the decimal point)   //（小数点以下の誤差あり）
    [Serializable]
    public class CpuRateInfo
    {
        public string name;         //"cpu0"~
        public float ratio;         //The ratio of each core when the total utilization of all cores is taken as 100%. [%]      //全てのコアの使用率の合計を100%としたときの、各コアの比率。[%]
        public float user;          //CPU utilization used at the user level (application).[%]                                  //ユーザレベル（アプリケーション）で使用されたCPU使用率。[%]
        public float nice;          //Priority (nice value) The CPU usage rate due to execution of the set user process.        //優先度（nice値）設定されたユーザプロセス実行によるCPU使用率。[%]
        public float system;        //CPU utilization used at the system level (kernel).                                        //システムレベル（カーネル）で使用されたCPU使用率。
        public float idle;          //Percentage of CPU not being used.                                                         //CPUが使用されていない割合。

        public override string ToString()
        {
            return JsonUtility.ToJson(this);    //for debug
        }
    }


    //For Confirm Device Credentials status
    //認証ステータス用
    [Serializable]
    public class CredentialInfo
    {
        public string status;      //Result status
        public string sessionID;   //Session ID (string)
    }




    //==========================================================
    // Contant Values etc.
    // 定数など

    /// <summary>
    /// Sensor Type constant (* value can not be changed)
    ///･Basically the type of sensor is defined as 'int' type, and the return value is defined as 'float[]'.
    /// If there is a newly added sensor that is not here, it seems that if you add an ID to 'SensorType' you can use it as it is (Except for TriggerEvent type).
    ///·Because plugin loads native calls, it seems better if it can be used to 'Input.acceleration', '.gyro' etc., by Unity built-in.
    ///(Sensor Type)
    /// https://developer.android.com/reference/android/hardware/Sensor.html#TYPE_ACCELEROMETER
    ///(Sensor Values)
    /// https://developer.android.com/reference/android/hardware/SensorEvent.html#values
    ///(Using Google Play filters to target specific sensor configurations)
    /// https://developer.android.com/guide/topics/sensors/sensors_overview.html#sensors-configs
    ///(Sensors Overview)
    /// https://developer.android.com/guide/topics/sensors/sensors_overview.html
    ///(API Level)
    /// https://developer.android.com/guide/topics/manifest/uses-sdk-element.html#ApiLevels
    /// 
    /// 
    /// センサー種類定数（※値の変更は不可）
    ///・基本的にセンサーの種類は"int型"で定義されており、戻り値は"float[]"で定義されている。
    /// ここには無い新しく追加されたセンサーがあった場合、"SensorType"にIDを追加すればそのまま利用できる場合がある（TriggerEvent タイプを除く）。
    ///・プラグインではネイティブ呼び出しの負荷がかかるため、Input.acceleration, .gyro 等、Unity で取得できるものは、そちらの方が良いと思われる。
    ///(センサーの種類)
    /// https://developer.android.com/reference/android/hardware/Sensor.html#TYPE_ACCELEROMETER
    ///(センサーの戻り値)
    /// https://developer.android.com/reference/android/hardware/SensorEvent.html#values
    ///(Google Play などには利用できるセンサーでフィルタリングした方が良い)
    /// https://developer.android.com/guide/topics/sensors/sensors_overview.html#sensors-configs
    ///(Sensors Overview)
    /// https://developer.android.com/guide/topics/sensors/sensors_overview.html
    ///(API Level)
    /// https://developer.android.com/guide/topics/manifest/uses-sdk-element.html#ApiLevels
    /// </summary>
    [Serializable]
    public enum SensorType
    {
        None = 0,                           //(dummy, not set)
        Accelerometer = 1,                  //[m/s^2] (≒ Input.acceleration * -10)              //加速度
        MagneticField = 2,                  //[uT] (= Input.compass.rawVector)                   //磁気
        Orientation = 3,                    //[degree] (deprecated in API level 20）             //方向 [度] (※API 20 で廃止）
        Gyroscope = 4,                      //[rad/s]                                            //角速度
        Light = 5,                          //[lux]                                              //照度
        Pressure = 6,                       //[hPa]                                              //気圧  
        Proximity = 8,                      //[cm]                                               //近接 
        Gravity = 9,                        //[m/s^2]                                            //重力
        LinearAcceleration = 10,            //[m/s^2] (= Accelerometer - Gravity)                //線形加速度
        RotationVector = 11,                //[vector] (*[0]~[2]:API 9, [3][4]:API 18 or higher) //デバイスの回転ベクトル
        RelativeHumidity = 12,              //[%] (*API 20 or higher)                            //湿度
        AmbientTemperature = 13,            //[℃]                                               //気温
        MagneticFieldUncalibrated = 14,     //[uT] (*API 18 or higher)                           //未較正の磁気
        GameRotationVector = 15,            //[vector] (*API 18 or higher)                       //地磁気を使用しない回転ベクトル
        GyroscopeUncalibrated = 16,         //[rad/s] (*API 18 or higher)                        //未較正の角速度
        SignificantMotion = 17,             //[1 only] (*API 18 or higher)                       //動作継続トリガ
        StepDetector = 18,                  //[1 only] (*API 19 or higher)                       //歩行トリガ
        StepCounter = 19,                   //[steps (system boot)] (*API 19 or higher)          //歩数 [通算歩数]
        GeomagneticRotationVector = 20,     //[vector] (*API 19 or higher)                       //地磁気の回転ベクトル
        HeartRate = 21,                     //[bpm](*API 20 or higher. Required permission：'android.permission.BODY_SENSORS') //毎分の心拍数
        Pose6DOF = 28,                      //[quaternion, translation] (*API 24 or higher)      //デバイスポーズ
        StationaryDetect = 29,              //[1 only] (*API 24 or higher)                       //静止検出トリガ
        MotionDetect = 30,                  //[1 only] (*API 24 or higher)                       //動作検出トリガ
        HeartBeat = 31,                     //[confidence=0~1] (*API 24 or higher)               //心拍ピーク検出
        LowLatencyOffbodyDetect = 34,       //[0 (device is off-body) or 1 (device is on-body)] (*API 26 or higher)  //デバイス装着検出
        AccelerometerUncalibrated = 35,     //[m/s^2] (*API 26 or higher)                        //未較正の加速度
    }


    /// <summary>
    /// Sensor detection speed constant (* value can not be changed)
    ///·Normal is recommended, since plug-in loads native calls and return value conversion.
    ///(*) In fact, it is received 'faster' (depending on the type of sensor).
    ///(Sensor Delay)
    /// https://developer.android.com/reference/android/hardware/SensorManager.html#SENSOR_DELAY_FASTEST
    /// 
    /// 
    /// センサーの検出速度定数（※値の変更は不可）
    ///・プラグインではネイティブ呼び出し・戻り値の変換の負荷がかかるため、Normal を推奨。
    ///※実際には「より速く」受信される（センサーの種類による）。
    ///(センサー速度)
    /// https://developer.android.com/reference/android/hardware/SensorManager.html#SENSOR_DELAY_FASTEST
    /// </summary>
    [Serializable]
    public enum SensorDelay
    {
        Fastest = 0,    //0ms [*Not recommended as it will result in high load]
        Game    = 1,    //20ms (50fps)
        UI      = 2,    //66.6ms (15fps)
        Normal  = 3,    //200ms (5fps) [*Recommended]
    }


    /// <summary>
    /// For acquisition sensor value
    /// 
    /// センサーの戻り値取得用
    /// </summary>
    [Serializable]
    public class SensorInfo
    {
        public int type;
        public float[] values;
    }


    /// <summary>
    /// General constants of sensor values
    /// 
    /// センサー値の一般的な定数
    /// 
    /// https://developer.android.com/reference/android/hardware/SensorManager.html#GRAVITY_DEATH_STAR_I
    /// </summary>
    public class SensorConstant
    {
        public class Light
        {
            // Maximum luminance of sunlight in lux
            public static readonly float SunlightMax  = 120000.0f;
            // luminance of sunlight in lux
            public static readonly float Sunlight     = 110000.0f;
            // luminance in shade in lux
            public static readonly float Shade        = 20000.0f;
            // luminance under an overcast sky in lux
            public static readonly float Overcast     = 10000.0f;
            // luminance at sunrise in lux
            public static readonly float Sunrise      = 400.0f;
            // luminance under a cloudy sky in lux
            public static readonly float Cloudy       = 100.0f;
            // luminance at night with full moon in lux
            public static readonly float Fullmoon     = 0.25f;
            // luminance at night with no moon in lux
            public static readonly float NoMoon       = 0.001f;
        }

        public class Gravity
        {
            // Standard gravity (g) on Earth. This value is equivalent to 1G
            public static readonly float Standard          = 9.80665f;
            // Sun's gravity in SI units (m/s^2)
            public static readonly float Sun               = 275.0f;
            // Mercury's gravity in SI units (m/s^2)
            public static readonly float Mercury           = 3.70f;
            // Venus' gravity in SI units (m/s^2)
            public static readonly float Venus             = 8.87f;
            // Earth's gravity in SI units (m/s^2)
            public static readonly float Earth             = 9.80665f;
            // The Moon's gravity in SI units (m/s^2)
            public static readonly float Moon              = 1.6f;
            // Mars' gravity in SI units (m/s^2)
            public static readonly float Mars              = 3.71f;
            // Jupiter's gravity in SI units (m/s^2)
            public static readonly float Jupiter           = 23.12f;
            // Saturn's gravity in SI units (m/s^2)
            public static readonly float Saturn            = 8.96f;
            // Uranus' gravity in SI units (m/s^2)
            public static readonly float Uranus            = 8.69f;
            // Neptune's gravity in SI units (m/s^2)
            public static readonly float Neptune           = 11.0f;
            // Pluto's gravity in SI units (m/s^2)
            public static readonly float Pluto             = 0.6f;
            // Gravity (estimate) on the first Death Star in Empire units (m/s^2)
            public static readonly float DeathStarInEmpire = 0.000000353036145f;
            // Gravity on the island
            public static readonly float TheIsland         = 4.815162342f;
        }

        public class MagneticField
        {
            // Maximum magnetic field on Earth's surface
            public static readonly float EarthMax = 60.0f;
            // Minimum magnetic field on Earth's surface
            public static readonly float EarthMin = 30.0f;
        }

        public class Pressure
        {
            // Standard atmosphere, or average sea-level pressure in hPa (millibar)
            public static readonly float StandardAtmosphere = 1013.25f;
        }
    }



    /// <summary>
    /// For 'action' input support
    ///(*) Actions have a request API Level, and some are available depending on the model of the device, others are not (sometimes it is duplicated).
    ///(*) You can add something that is not here.
    ///(*) Only index 'None' must be [0]. Otherwise, you can change the order.
    /// 
    /// アクション文字列の入力支援用
    ///※アクションは要求API Levelがあり、デバイスのモデルによっても利用できるものとそうでないものがある（重複してる場合もある）。
    ///※ここに無いものを追加しても構わない。
    ////※'None' のみインデクスが [0] である必要がある。それ以外は順序を替えても構わない。
    /// </summary>
    [Serializable]
    public class ActionString
    {
        public static readonly string None = "(None)";   //default for display

        public static readonly string[] ConstantValues =
        {
            None, //dummy, not set. (*Do not change index:[0])

            //https://developer.android.com/reference/android/content/Intent.html#ACTION_VIEW
            "android.intent.action.VIEW",
            "android.intent.action.EDIT",
            "android.intent.action.WEB_SEARCH",
            "android.intent.action.SEND",
            "android.intent.action.SENDTO",
            "android.intent.action.CALL_BUTTON",
            "android.intent.action.DIAL",
            "android.intent.action.SET_WALLPAPER",
            "android.intent.action.MANAGE_NETWORK_USAGE",
            "android.intent.action.POWER_USAGE_SUMMARY",

            //https://developer.android.com/reference/android/provider/Settings.html#ACTION_ACCESSIBILITY_SETTINGS
            "android.settings.ACCESSIBILITY_SETTINGS",
            "android.settings.ADD_ACCOUNT_SETTINGS",
            "android.settings.AIRPLANE_MODE_SETTINGS",
            "android.settings.APN_SETTINGS",
            "android.settings.APPLICATION_DETAILS_SETTINGS",
            "android.settings.APPLICATION_DEVELOPMENT_SETTINGS",
            "android.settings.APPLICATION_SETTINGS",
            "android.settings.APP_NOTIFICATION_SETTINGS",   //API 26
            "android.settings.BATTERY_SAVER_SETTINGS",      //API 22
            "android.settings.BLUETOOTH_SETTINGS",
            "android.settings.CAPTIONING_SETTINGS",         //API 19
            "android.settings.CAST_SETTINGS",               //API 21
            "android.settings.CHANNEL_NOTIFICATION_SETTING",//API 26
            "android.settings.DATA_ROAMING_SETTINGS",
            "android.settings.DATE_SETTINGS",
            "android.settings.DEVICE_INFO_SETTINGS",
            "android.settings.DEVICE_INFO_SETTINGS",
            "android.settings.DREAM_SETTINGS",              //API 18
            "android.settings.HARD_KEYBOARD_SETTINGS",      //API 24
            "android.settings.HOME_SETTINGS",               //API 21
            "android.settings.IGNORE_BACKGROUND_DATA_RESTRICTIONS_SETTINGS",//API 24
            "android.settings.IGNORE_BATTERY_OPTIMIZATION_SETTINGS",        //API 23
            "android.settings.INPUT_METHOD_SETTINGS",
            "android.settings.INPUT_METHOD_SUBTYPE_SETTINGS",
            "android.settings.INTERNAL_STORAGE_SETTINGS",
            "android.settings.LOCALE_SETTINGS",
            "android.settings.LOCATION_SOURCE_SETTINGS",
            "android.settings.MANAGE_ALL_APPLICATIONS_SETTINGS",
            "android.settings.MANAGE_APPLICATIONS_SETTINGS",
            "android.settings.MANAGE_DEFAULT_APPS_SETTINGS",    //API 24
            "android.settings.action.MANAGE_OVERLAY_PERMISSION",//API 23
            "android.settings.MANAGE_UNKNOWN_APP_SOURCES",      //API 26
            "android.settings.action.MANAGE_WRITE_SETTINGS",    //API 23
            "android.settings.MEMORY_CARD_SETTINGS",
            "android.settings.NETWORK_OPERATOR_SETTINGS",
            "android.settings.NFCSHARING_SETTINGS",
            "android.settings.NFC_PAYMENT_SETTINGS",        //API 19
            "android.settings.NFC_SETTINGS",
            "android.settings.NIGHT_DISPLAY_SETTINGS",      //API 26
            "android.settings.ACTION_NOTIFICATION_LISTENER_SETTINGS",   //API 22
            "android.settings.NOTIFICATION_POLICY_ACCESS_SETTINGS",     //API 23
            "android.settings.ACTION_PRINT_SETTINGS",       //API 19
            "android.settings.PRIVACY_SETTINGS",
            "android.settings.QUICK_LAUNCH_SETTINGS",
            "android.search.action.SEARCH_SETTINGS",
            "android.settings.SECURITY_SETTINGS",
            "android.settings.SETTINGS",
            "android.settings.SHOW_REGULATORY_INFO",        //API 21
            "android.settings.SOUND_SETTINGS",
            "android.settings.SYNC_SETTINGS",
            "android.settings.USAGE_ACCESS_SETTING",        //API 21
            "android.settings.USER_DICTIONARY_SETTINGS",
            "android.settings.VOICE_INPUT_SETTINGS",        //API 21
            "android.settings.VPN_SETTINGS",                //API 24
            "android.settings.VR_LISTENER_SETTINGS",        //API 24
            "android.settings.WEBVIEW_SETTINGS",            //API 24
            "android.settings.WIFI_IP_SETTINGS",
            "android.settings.WIFI_SETTINGS",
            "android.settings.WIRELESS_SETTINGS",
            "android.settings.ZEN_MODE_PRIORITY_SETTINGS",  //API 26
        };
    }

}

