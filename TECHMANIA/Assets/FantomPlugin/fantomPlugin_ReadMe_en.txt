http://fantom1x.blog130.fc2.com/blog-entry-293.html
Android Native Dialogs and Functions Plugin
Setup & Build Manual

･Native Plugin "fantomPlugin.aar" is required 'Minimum API Level：Android 4.2 (API 17)' or higher.

(*) It is necessary to set it to 'Android 4.4 (API 19)' or higher in order to use 'StorageLoadTextController' and 'StorageSaveTextController' to read/write text file of storage.

(*) In order to acquire the value of the sensor it is necessary to set it above the necessary API Level of each sensor. For details, refer to official document or comments such as sensor related method & constant values.
https://developer.android.com/reference/android/hardware/Sensor.html#TYPE_ACCELEROMETER

･Move the "Assets/FantomPlugin/Plugins/" folder just under "Assets/" like "Assets/Plugins/". This "Plugins" folder is a special folder for running the plugin at runtime.
(see) https://docs.unity3d.com/Manual/ScriptCompileOrderFolders.html

･Rename "AndroidManifest-FullPlugin~.xml" to "AndroidManifest.xml" when receive events of Hardware Volume buttons, Speech Recognizer with dialog, open Wifi Settings, request Bluetooth enable, read/write text file on External Storage, open Gallery, register MediaScanner, acquire change event of screen rotation, acquire sensor values, confirm Device Credentials (authentication), acquire text from QR Code Scanner.

･Depending on the function you use, Android permission is required (https://developer.android.com/guide/topics/security/permissions.html). Permission is summarized in "Assets/Plugins/Android/Permission_ReadMe.txt". Please copy the necessary permission to "AndroidManifest.xml" (It is better to delete the permission of functions not used).

･Text To Speech is required the reading engine and voice data must be installed on the smartphone.
(see) http://fantom1x.blog130.fc2.com/blog-entry-275.html#fantomPlugin_TextToSpeech_install
(Voice data: Google Play)
https://play.google.com/store/apps/details?id=com.google.android.tts
https://play.google.com/store/apps/details?id=jp.kddilabs.n2tts  (Japanese)

･To use QR code reading, QR code scanner application of ZXing (google's open source project) must be installed on the device. If it is not installed, you will be prompted to install (it will be directed to Google Play).
(Google Play)
https://play.google.com/store/apps/details?id=com.google.zxing.client.android
(ZXing open source project)
https://github.com/zxing/zxing

･Select "_Landscape", "_Portrait" or "_Sensor" of "AndroidManifest~.xml" according to the screen rotation attribute (screenOrientation) of the application.
(see) https://developer.android.com/guide/topics/manifest/activity-element.html#screen

(*) Warning "Unable to find unity activity in manifest. You need to make sure orientation attribute is set to sensorPortrait manually." can be ignored if you use anything other than Unity standard Activity (UnityPlayerActivity).
(see) https://docs.unity3d.com/ja/current/Manual/AndroidUnityPlayerActivity.html

------------------------------------------------
■About demo

･Rename "AndroidManifest_demo.xml" to "AndroidManifest.xml" when building the Demo. Also add the scene in "Assets/FantomPlugin/Demo/Scenes/" to 'Build Settings...' and switch to 'Android' with 'Switch Platform'.

･For Unity 2018.1.0 to 1.6, please build "Build System" as "Internal".

(*) In the build of Unity 2018.1.0 to 1.6, when "Build Settings" is set to "Gradle" in "Build Settings ...", it is known that the package name is not added to "AndroidManifest.xml", and a build error occurs (Unity2018.1.7 and later have been bugfixed). In that case, you can build by describing 'package="(package name of application)"' (= Edit> Project Settings> Player> Other Settings> Identification> Package Name) in the "manifest" tag of "AndroidManifest.xml" (Until Unity 2017.4.2 is added automatically).
http://fantom1x.blog130.fc2.com/#unity2018_CommandInvokationFailure_packageName

･The toggle button "Job" (C# Job System) of "CpuTest" is available in Unity 2018.1.0 or higher.

(*) The demo of 'GalleryPickTest' does not include the mesh of the whole sphere (360 degrees). If necessary, download 'Sphere100.fbx' from the following URL and set it to 'Sphere' of hierarchy 'GalleryPickTest (Script)'. Also, please set 'TextureMat' to the material of 'Sphere 100'. Because the whole sphere look from the inside, if you give a negative value to the scale X, you can invert the image (size as you like, Scale Factor of mesh=1000 x Scale of Transform=-1 in the demo video). Please refer to the following article for the setup method.

(360 degrees [whole shphere] setup)
http://fantom1x.blog130.fc2.com/blog-entry-297.html
(Mesh of whole sphere: Sphere100.fbx)
http://warapuri.com/post/131599525953/
(Demo video: Vimeo)
https://vimeo.com/255712215

-> Plug-in demo "FantomPluginDemo_QR.png" (QR code) contains an example of setup. Please also check the operation.

------------------------------------------------
■Application example

･This Plug-in functions are also used for the following applications that are actually released.
If you want to check the operation please download it from AmazonAppStore (Cardboard VR, free App).

UnityChan Flyer (Free)
http://amzn.to/2xJ4ujS

SapphiartChan Live Show! (Free)
http://amzn.to/2xKmZ7H

[*] Outside of Japanese-speaking countries, it will be localized in English automatically.

 In particular,
1. Operation hardware volume control (including Bluetooth operation) → Using 'HardVolumeController'.
2. Long Press (including Bluetooth operation) to Re-center (viewpoint reset) → Using 'LongKeyInput'.
3. Display battery information → Use 'BatteryStatusController'.
4. Language localization (switching between English and Japanese) → Using 'LocalizeString', 'LocalizeStringResource', 'LocalizeText'.
etc...

●You can also download a simple sample app on blog (from GoogleDrive).

･UnityChan In OtakuCity (UnityChan will fly over Akihabara!) 
http://fantom1x.blog130.fc2.com/blog-entry-289.html#UnityChanInOtackuityAndroid

 This is a demonstration of "EasyMusicPlayer" (simple music player, storage access function, custom dialog etc), "SmoothFollow3" (camera angle operation with fingers), "PinchInput" (pinch operation), "SwipeInput" (swipe operation). It is also becoming a coin collection game. The compatible device will be Android 4.2 or higher (you need to allow "Install application with unknown provider").

･UnityChan Voice Janken (UnityChan Speech Recognition Janken) [*Japanese only]
http://fantom1x.blog130.fc2.com/blog-entry-273.html#UnityChanVoiceJanken
It is a voice recognition scissors game. The compatible device will be Android 4.2 or higher (you need to allow "Install application with unknown provider").

･FantomPluginDemo_QR.png (QR code)
 You can download the demo that you built the latest version of the plugin with apk. An example of setting up some dummy objects on the scene is also included. Please also check the operation (QR code -> Google Drive to DL). The compatible device will be Android 4.2 or higher (you need to allow "Install application with unknown provider").

------------------------------------------------
■News!

The music library including sample song is now on sale at the Asset Store!

Seamless Loop and Short Music (FREE!)
https://www.assetstore.unity3d.com/#!/content/107732

------------------------------------------------
■Update history

(ver.1.1)
･Added PinchInput, SwipeInput, LongClickInput/LongClickEventTrigger and its demo scene (PinchSwipeTest).
･Added SmoothFollow3 (originally StandardAssets SmoothFollow) with right/left rotation angle, height and distance, and added a corresponding to pinch (PinchInput) and swipe (SwipeInput) (demo scene: used with PinchSwipeTest).
･Changed the color format conversion of 'XColor' from ColorUtility to calculation formulas(Mathf.RoundToInt()).
･Changed 'XDebug' option of lines limit.
(ver.1.2)
･Added prefab and '-Controller' script of all functions.
･Added value change callbacks to SingleChoiceDialog, MultiChoiceDialog, SwitchDialog and CustomDialog items.
･Fixed bug that XDebug's automatic newline flag (newline) was ignored. Also, cleared the text buffer (Queue) with OnDestory() when using line limit.
(ver.1.3)
･Added function to open WIFI system settings (WifiSettingController).
･Added function to make Bluetooth connection request (dialog display) (BluetoothSettingController).
･Added function to send text using Chooser application (simple text sharing function) (SendTextController).
･Added functions to read/write text files (StorageLoadTextController/StorageSaveTextController) using the Storage Access Framework (API 19 or higher).
･Added function to open the gallery application and get the path of the image file (GalleryPickController) (also load texture and save screenshot).
･Added function to register (scan) file path to MediaScanner (MediaScannerController).
(ver.1.4)
･Added function to vibrate the Vibrator (VibratorController).
･Added vibrator function to notification (NotificationController).
･Changed all extended editor scripts with 'SerializedProperty' (as the setting was sometimes not saved in the editor).
(ver.1.5)
･Added function to get (listening) status of battery temperature, health(overheat, good, etc.), remaining capacity, connection status (BatteryStatusController).
(ver.1.6)
･Added function to get (listening) change status of device orientation (OrientationStatusController).
･Added function to get (listening) sensor values (~SensorController).
(ver.1.7)
･Added prefabs and demo to open various system settings screen.
･Added 'ActionType.ActionOnly' constant(enum) and 'Action' input support function to 'AndroidActionController'.
･Added some special ActionControllers such as 'MailerController', 'DialerController', 'ApplicationDetailsSettingsController' and etc.
(ver.1.8)
･Added function to use device credentials (authentication) [*fingerprint, pattern, PIN, password, etc. depending on user setting].
･Added function to get API Level (int type) of the running device.
(ver.1.9)
･Added function to check permission granted (AndroidPlugin.CheckPermission(), ~Controller.IsPermissionGranted).
･Added support check (IsSupported~) and permission granted check (IsPermissionGranted) to several "~Controller"[*1] at startup (Start()). When it is not possible, an error message is returned to 'OnError' callback.
[*1]SpeechRecognizerDialogController, BluetoothSettingController, SpeechRecognizerController, VibratorController, HeartRateController, All other sensors (IsSupportedSensor only).
(ver.1.10)
･Added function to launch QR Code (Bar Code) Scanner and acquire text (ShowQRCodeScanner()).
･Added general constants of sensor values (SensorConstant).
(ver.1.11)
･Added multiple parameter overload to StartAction(), StartActionWithChooser(), StartActionURI(). 'AndroidActionController' also supports multiple parameters.
･Changed 'MailerController' to multiple parameter actions (because more mailers can handle it).
･Added Market (Google Play) search function (MarketSearchController).
･Added function to acquire application installation check (IsExistApplication()), application name (GetApplicationName()), application version number (GetVersionCode()), application version name (GetVersionName()).
(ver.1.12)
･Added function (LocalizeStringResource) to manage resource localization data by ID.
･Introduced the string localization (LocalizeString) in the dialog, etc. ID field was added for resource management, and some specifications were changed [*2] to correspond to the resource management function.
･Added controller for application installation check (AppInstallCheckController).･Added a permission check controller (PermissionCheckController).
･Add long key press (Back key etc.) input judgment script (LongKeyInput) (added to PinchSwipeTest demo).
[*2]Change language search to 'system language -> default setting language' only. In order to ignore the localization (= existing as it is) when the device language can not be found. Also change the font size default to 0 (= existing as it is) (* It does not change in the example of demo use, but please be careful if you are writing your own script.) Default language setting properly If so, the behavior does not change).
(ver.1.13)
･Added function to attach images to text send (SendTextController) and mailer (MailerController).
･Screen shot part only create a library (Screenshot) independently from demo (※ old demo remains as it is).
･Added editor tools insert/remove items, ID duplication, empty check editor tool in LocalizeStringResource (on inspector).
･Added 'LocalizeString' argument overload to the dynamic text setting method of some controllers.
･Added function to change localization language (LocalizeLanguageChanger).
･Added function to change language locale to speech recognization (SpeechRecognizerController, SpeechRecognizeDialogrController) and text reading (TextToSpeechController).
(ver.1.14)
･Added cancel callback to 'SingleChoiceDailogController', 'MultiChoiceDialogController', 'SliderDialogController', 'SwitchDialogController', 'CustomDialogController'.
･Changed when cancel button was pressed with 'VolumeSliderDialogController', the volume was modified to return to before change.
･Added checkbox (CheckItem) to the item type of the Custom Dialog.
･Added voltage to battery information (atteryInfo.voltage).
･Fixed 'LocalizeString.FontSizeByLanguage()' language search to 'system language -> default language' (to match 'LocalizeString.TextByLanguage()').
･Added simple language display switching function for confirming editor to 'LocalizeLanguageChanger' (.debugLanguage, .debugApplyOnStart).
･Changed the equal interval arrangement tool such as UI (ObjectArrangeTool) [support tool] to a reorderable list, and also added function to drag & drop and parallel move of position.
(ver.1.15)
･Added function (GalleyPickController) so that you can also get movie information (path etc) from the gallery.
･Added "size" (file size), "mimeType" (MIME Type) and "orientation" (rotation angle) fields to ImageInfo (image information acquisition class).
(ver.1.16)
･Added function to acquire image, audio, video, other file and folder information with the Storage Access Framework (StorageOpenImageController, StorageOpenAudioController, StorageOpenVideoController, StorageOpenFileController, StorageOpenFolderControlle).
･Added function of save file (write access) by the Storage Access Framework (StorageSaveFileController).
･"UriWithMimeType" (action to URI with MIME type designation) added to AndroidActionController. And also added a demonstration to open various files to ExternalStorageTest as a sample.
･Added simple music player sample script (EasyMusicPlayer) and demo scene (MusicPlayerExample).
(ver.1.17)
･Added function to request permission (+ show explanation of the rationale dialog). Added "requestWhenNotGranted" option to 'PermissionCheckController'.
･Change sensor's support check function to real time (no cache) with sensor controller base script 'SensorControllerBase'.
(ver.1.18)
･Added status acquisition function of CPU utilization (CpuRateController). Also added a script "CpuRateBar" (single CPU) which displays the CPU utilization as a bar and "CpuRateBarView" (multiple CPUs) to display in a list.
･Added script "FpsText.cs" to display FPS (Frame Per Second) on UI-Text. Also added that prefab "FpsMonitor".
･Add "XGizmo" to display simple gizmos of sphere, box, line, mesh.
･Added field (targetText) to read and write directly to UI-Text to StorageLoadTextController and StorageSaveTextController.
･Added CurrentIndex and IsPlaying properties to EasyMusicPlayer.
･The storage open function when EasyMusicPlayer's song was added was included in the same class (it can be added directly from the previous StorageOpenAudio Controller).
･Change the item "Use Name" in "Scene Specification" and "Scene Name / Scene Build Index" in 'SceneLoadWithKey' Inspector (internally keeping the bool value).
･Fixed invalid code paths other than Android platform (not affected when building with Android).


(*)The latest version can be downloaded from GoogleDrive on blog (*Japanese version only).
http://fantom1x.blog130.fc2.com/blog-entry-273.html

------------------------------------------------
■License of use library. etc

This plugin includes deliverables distributed under the license of Apache License, Version 2.0.
http://www.apache.org/licenses/LICENSE-2.0

ZXing ("Zebra Crossing") open source project (google). [ver.3.3.2] (QR Code Scan)
https://github.com/zxing/zxing

------------------------------------------------
Let's enjoy creative life!

By Fantom

[Blog] http://fantom1x.blog130.fc2.com/
[Unity Connect] https://connect.unity.com/u/5abd008032b30600256e8ca9
[Twitter] https://twitter.com/fantom_1x
[SoundCloud] https://soundcloud.com/user-751508071
[Picotune] http://picotune.me/?@Fantom
[Monappy] https://monappy.jp/u/Fantom
[E-Mail] fantom_1x@yahoo.co.jp

