http://fantom1x.blog130.fc2.com/blog-entry-273.html
http://fantom1x.blog130.fc2.com/blog-entry-293.html
Android Native Dialogs and Functions Plugin
セットアップ＆ビルド マニュアル

･ネイティブプラグイン "fantomPlugin.aar" は「Minimum API Level：Android 4.2 (API 17)」以上で使用して下さい。

(※) ストレージのテキストファイル読み書き機能「StorageLoadTextController」「StorageSaveTextController」を利用するには「Android 4.4 (API 19)」以上にする必要があります。

(※) センサーの値を取得するには各センサーの要求 API Level 以上にする必要があります。詳細は公式のドキュメントまたは、センサー関連メソッド・定数などのコメントを参照して下さい。
https://developer.android.com/reference/android/hardware/Sensor.html#TYPE_ACCELEROMETER

･"Assets/FantomPlugin/Plugins/" フォルダを "Assets/Plugins/" のように "Assets/" 直下に移動して下さい。この「Plugins」フォルダはランタイムでプラグインを稼働させるための特殊なフォルダとなります。
(参照) https://docs.unity3d.com/ja/current/Manual/ScriptCompileOrderFolders.html

･ハードウェア音量キーのイベント取得、ダイアログ付きの音声認識、WIFIの設定を開く、Bluetooth接続要求（ダイアログ）、ストレージのテキストファイルの読み書き、ギャラリーの画像パス取得、MediaScannerの更新機能、バッテリーのステータス取得、画面回転の変化取得、センサーの値取得、デバイス認証、QRコードスキャナからのテキスト取得を利用する場合には "AndroidManifest-FullPlugin~.xml" を "AndroidManifest.xml" にリネームして使用して下さい。

・使用する機能によっては Android のパーミッションが必要になります（https://developer.android.com/guide/topics/security/permissions.html）。パーミッションについては「Assets/Plugins/Android/Permission_ReadMe.txt」にまとめてあります。必要なパーミッションを「AndroidManifest.xml」にコピペして下さい（利用しない機能のパーミッションは削除する方が好ましいです）。

･テキスト読み上げを使用するには、端末に読み上げエンジンと音声データがインストールされている必要があります。
(テキスト読み上げのインストール)
http://fantom1x.blog130.fc2.com/blog-entry-275.html#fantomPlugin_TextToSpeech_install
(音声データ：Google Play)
https://play.google.com/store/apps/details?id=com.google.android.tts
https://play.google.com/store/apps/details?id=jp.kddilabs.n2tts

・QRコード読み取りを利用するには端末に ZXing（googleのオープンソースプロジェクト）のQRコードスキャナアプリがインストールされている必要があります。インストールされていない場合、インストールを促すダイアログが表示されます（Google Play へ誘導されます）。
(Google Play)
https://play.google.com/store/apps/details?id=com.google.zxing.client.android
(ZXing オープンソースプロジェクト)
https://github.com/zxing/zxing

･"AndroidManifest~.xml"の"_Landscape" または "_Portrait","_Sensor" はアプリの画面回転の属性(screenOrientation)に合わせて選択して下さい。
(参照) https://developer.android.com/guide/topics/manifest/activity-element.html#screen

(※) 警告「Unable to find unity activity in manifest. You need to make sure orientation attribute is set to sensorPortrait manually.」は Unityの標準のアクティビティ(UnityPlayerActivity)以外を使うと出るので無視して下さい。

------------------------------------------------
■デモについて

･デモをビルドするときは "AndroidManifest_demo.xml" を "AndroidManifest.xml" にリネームして使って下さい。また「Build Settings...」に「Assets/FantomPlugin/Demo/Scenes/」にあるシーンを追加して、「Switch Platform」で「Android」にしてビルドして下さい。

・Unity2018.1.0～1.6では「Build System」を「Internal」にしてビルドして下さい。

※Unity2018.1.0～1.6でのビルドにおいて、「Build Settings...」で「Build System」を「Gradle」にした場合、「AndroidManifest.xml」にパッケージ名が追加されず、ビルドエラーが出ることが確認されてます（Unity2018.1.7以降はバグFixされてます）。その場合、「AndroidManifest.xml」の「manifest」タグに「package="(アプリのパッケージ名)"」（= Edit＞Project Settings＞Player＞Other Settings＞Identification＞Package Name）を記述すればビルドできます（Unity2017.4.2までは自動で追加されます）。
http://fantom1x.blog130.fc2.com/#unity2018_CommandInvokationFailure_packageName

・「CpuTest」のトグルボタン「Job」（C#Job System）は Unity2018.1.0 以降で使用可能になります。

※「GalleryPickTest」のデモには全天球のメッシュ（360 degrees）は含まれてません。必要であれば以下のURLから「Sphere100.fbx」ダウンロードし、ヒエラルキーの「GalleryPickTest(Script)」の「Sphere」にセットして下さい。また「Sphere100」の Material に「TextureMat」をセットして下さい。全天球は内側から覗く感じになるので、スケールの X にマイナス値を与えると画像を反転できます（大きさは任意。デモビデオではメッシュの Scale Factor=1000×Transform の Scale=-1）。セットアップ方法は以下の記事を参考にして下さい。

(360度「全天球」のセットアップ)
http://fantom1x.blog130.fc2.com/blog-entry-297.html
(全天球メッシュ：Sphere100.fbx)
http://warapuri.com/post/131599525953/
(Demo video：Vimeo)
https://vimeo.com/255712215

→ プラグインのデモ「FantomPluginDemo_QR.png」（QRコード）にはセットアップした例が入っています。動作確認にもどうぞ。

------------------------------------------------
■アプリ利用例

・実際にリリースされている以下のアプリにもプラグインの機能を利用しています。動作確認してみたい方は AmazonAppStore からダウンロードして下さい（Cardboard VR, 無料アプリ）。

UnityChan Flyer (Free)
http://amzn.to/2xJ4ujS

SapphiartChan Live Show! (Free)
http://amzn.to/2xKmZ7H

※日本語圏以外では、自動で英語にローカライズされます。

　具体的には、
1. ハードウェア音量操作（Bluetooth操作も含む）→ HardVolumeController を利用。
2. 長押し（Bluetooth操作も含む）でリセンター（視点リセット）→ LongKeyInput を利用。
3. バッテリー情報の表示 → BatteryStatusController を利用。
4. 言語ローカライズ（英語、日本語の切替）→ LocalizeString, LocalizeStringResource, LocalizeText を利用。
など…

●ブログにて簡単なサンプルアプリもダウンロードできます（GoogleDriveから）。

・UnityChan In OtakuCity（ユニティちゃんがアキバ上空を飛ぶ！）
http://fantom1x.blog130.fc2.com/blog-entry-289.html#UnityChanInOtackuityAndroid

「EasyMusicPlayer」（簡易音楽プレイヤー、ストレージアクセス機能、カスタムダイアログ等）、「SmoothFollow3」（指でカメラアングル操作）、「PinchInput」（ピンチ操作）、「SwipeInput」（スワイプ操作）のデモです。またコイン集めゲームにもなっています。対応端末は Android 4.2 以上になります（「提供元不明のアプリをインストール」を許可する必要があります）。

・UnityChan Voice Janken（ユニティちゃんと音声認識じゃんけん）[*Japanese only]
http://fantom1x.blog130.fc2.com/blog-entry-273.html#UnityChanVoiceJanken
音声認識じゃんけんゲームです。対応端末は Android 4.2 以上になります（「提供元不明のアプリをインストール」を許可する必要があります）。

・FantomPluginDemo_QR.png（QRコード）
　プラグインの最新版をビルドしたデモを apk でダウンロードできます。シーン上のいくつかのダミーオブジェクトをセットアップした例も入っています。動作確認にもどうぞ（QRコード→Google Drive からDL）。対応端末は Android 4.2 以上になります（「提供元不明のアプリをインストール」を許可する必要があります）。

------------------------------------------------
■News!

アセットストアにてサンプルの楽曲を含む音楽ライブラリが公開中！

Seamless Loop and Short Music (FREE!)
https://www.assetstore.unity3d.com/#!/content/107732

------------------------------------------------
■更新履歴

(ver.1.1)
・ピンチ（PinchInput）,スワイプ（SwipeInput）,ロングタップ（LongClickInput/LongClickEventTrigger）とそのデモシーン（PinchSwipeTest）を追加。
・SmoothFollow3（元は StandardAssets の SmoothFollow）に左右回転アングルと高さと距離の遠近機能を追加し、ピンチ（PinchInput）やスワイプ（SwipeInput）にも対応させた改造版（SmoothFollow3）を追加（デモシーン：PinchSwipeTest で使用）。
・XColor の色形式変換を ColorUtility から計算式(Mathf.RoundToInt())に変更。
・XDebug に行数制限を追加。
(ver.1.2)
・おおよそ全ての機能のプレファブ＆「～Controller」スクリプトを追加。
・単一選択（SingleChoiceDialog）、複数選択（MultiChoiceDialog）、スイッチダイアログ（SwitchDialog）、カスタムダイアログのアイテムに値変化のコールバックを追加。
・XDebug の自動改行フラグ(newline)が無視されていた不具合を修正。また、行数制限を使用しているときに、OnDestory() でテキストのバッファ（Queue）をクリアするようにした。
(ver.1.3)
・WIFIのシステム設定を開く機能（WifiSettingController）を追加。
・Bluetoothの接続要求（ダイアログ表示）をする機能（BluetoothSettingController）を追加。
・アプリChooserを利用してテキストを送信する（簡易的なテキストのシェア）（SendTextController）機能を追加。
・ストレージアクセス機能（API19以上）を利用して、テキストファイルの保存と読み込み機能（StorageLoadTextController/StorageSaveTextController）を追加。
・ギャラリーアプリを起動して、画像ファイルのパスを取得する機能（GalleryPickController）を追加（サンプルとしてテクスチャへのロードとスクリーンショットを追加）。
・ファイルパスをMediaScannerに登録（更新）する機能（MediaScannerController）を追加。
(ver.1.4)
・バイブレーターの機能を追加（VibratorController）。
・通知（NotificationController）にもバイブレーター機能を追加。
・全ての拡張エディタスクリプトを「SerializedProperty」に置き換え（エディタ上で設定が保存されないことがあったので）。
(ver.1.5)
・バッテリーの温度、コンディション(オーバーヒート、良好、等)、残量、接続状態のステータス取得（リスニング）を追加（BatteryStatusController）。
(ver.1.6)
・画面回転の変化イベント取得（OrientationStatusController）を追加。
・センサーの値の取得機能（～SensorController）を追加。
(ver.1.7)
・各システム設定画面を開くプレファブとデモを追加。
・AndroidActionControllerに「ActionType.ActionOnly」定数(enum)とアクションの入力支援機能を追加。
・MailerController, DialerController, ApplicationDetailsSettingsController 等、専用のアクションコントローラをいくつか追加。
(ver.1.8)
・デバイス認証（指紋・パターン・PIN・パスワード等。ユーザーの設定による）の利用機能を追加。
・実行中デバイスの API Level（int型）の取得機能を追加。
(ver.1.9)
・パーミッション付与のチェック（AndroidPlugin.CheckPermission(), ～Controller.IsPermissionGranted）機能を追加。
・いくつかの「～Controller」[*1] に、起動時（Start()）にサポートのチェック（IsSupported～）とパーミッションの付与チェック（IsPermissionGranted）を追加。不可のとき「OnError」コールバックにエラーメッセージを返すようにした。
[*1]SpeechRecognizerDialogController, BluetoothSettingController, SpeechRecognizerController, VibratorController, HeartRateController, その他全てのセンサー（IsSupportedSensorのみ）
(ver.1.10)
・QRコード(バーコード)スキャナを起動しテキストを取得する（ShowQRCodeScanner()）機能を追加。
・センサー値の一般的な定数（SensorConstant）を追加。
(ver.1.11)
・StartAction(), StartActionWithChooser(), StartActionURI() に複数パラメタオーバーロードを追加。AndroidActionController も複数パラメタ対応。
・MailerController を複数パラメタアクションに変更（より多くのメーラーが対応できるため）。
・マーケット（Google Play）検索機能を追加（MarketSearchController）。
・アプリのインストールチェック（IsExistApplication()）、アプリ名（GetApplicationName()）、アプリバージョン番号（GetVersionCode()）、アプリバージョン名（GetVersionName()）の取得機能を追加。
(ver.1.12)
・文字列のローカライズデータをIDでリソース管理する機能（LocalizeStringResource）を追加。
・文字列ローカライズ（LocalizeString）をダイアログ等に導入。リソース管理用にIDフィールドを追加。リソース管理用機能に対応させるため、一部仕様を変更した[*2]。
・アプリのインストールチェックのコントローラ（AppInstallCheckController）を追加。
・パーミッションチェックのコントローラ（PermissionCheckController）を追加。
・キーの長押し（Backキー等）入力判定スクリプト（LongKeyInput）を追加（PinchSwipeTestデモに追加）。
[*2]言語検索を「システム言語→デフォルト設定言語」のみに変更。端末の言語が見つからないとき、ローカライズを無視（＝既存のまま）させるため。またフォントサイズのデフォルトも 0（＝既存のまま）に変更（※デモの使用例では何ら変わりませんが、独自にスクリプトを組んでいる場合は注意して下さい。適切にデフォルト言語設定がされていれば、挙動に変わりはありません）。
(ver.1.13)
・テキスト送信（SendTextController）、メーラー送信（MailerController）に画像を添付する機能を追加。
・スクリーンショット部分のみデモから独立して、ライブラリ（Screenshot）として追加（※旧デモはそのまま）。
・LocalizeStringResource にアイテムの追加・削除、IDの重複・空のチェックのエディタツール（インスペクタ上）を追加。
・いくつかのコントローラの動的テキスト設定メソッド（.Show(),.Send() 等）に「LocalizeString」引数のオーバーロードを追加。
・ローカライズの言語変更機能（LocalizeLanguageChanger）を追加。
・音声認識（SpeechRecognizerController, SpeechRecognizeDialogrController）とテキスト読み上げ（TextToSpeechController）に言語ロケール指定機能を追加。
(ver.1.14)
・単一選択ダイアログ、複数選択ダイアログ、スライダーダイアログ、スイッチダイアログ、カスタムダイアログにキャンセルコールバックを追加（SingleChoiceDailogController, MultiChoiceDialogController, SliderDialogController, SwitchDialogController, CustomDialogController）。
・音量スライダーダイアログ（VolumeSliderDialogController）でキャンセルボタンが押されたとき、音量が変更前に戻るように修正。
・カスタムダイアログのアイテム種類にチェックボックス（CheckItem）を追加。
・バッテリー情報に電圧を追加（BatteryInfo.voltage）。
・LocalizeString.FontSizeByLanguage() の言語検索を「システム言語→デフォルト言語」に修正（LocalizeString.TextByLanguage() と合わせるため）。
・LocalizeLanguageChanger にエディタ確認用の簡易言語表示切替機能を追加（.debugLanguage, .debugApplyOnStart）。
・UI等の等間隔整列ツール（ObjectArrangeTool）[補助ツール] を並べ替えリストに変更し、ドラッグ＆ドロップや平行移動機能も追加。
(ver.1.15)
・ギャラリーから動画情報（パス等）も取得できるように機能追加（GalleyPickController）。
・ImageInfo（画像情報取得用クラス）に「size」(ファイルサイズ)、「mimeType」(MIME Type) 、「orientation」(画像回転角度) のフィールドを追加。
(ver.1.16)
・ストレージアクセス機能にて画像、音声、動画、それ以外のファイル、フォルダの情報を取得する機能を追加（StorageOpenImageController, StorageOpenAudioController, StorageOpenVideoController, StorageOpenFileController, StorageOpenFolderController）。
・ストレージアクセス機能にてファイル保存（書き込みアクセス）の機能を追加（StorageSaveFileController）。
・AndroidActionController に「UriWithMimeType」（MIME type指定でのURIへのアクション）を追加。またサンプルとして ExternalStorageTest に様々なファイルを開くデモを追加。
・簡易的な音楽プレイヤーのサンプルスクリプト（EasyMusicPlayer）とシーン（MusicPlayerExample）を追加。
(ver.1.17)
・パーミッションのリクエスト（＋その根拠のダイアログの表示）をする機能を追加。「PermissionCheckController」に「requestWhenNotGranted」オプションを追加。
・センサーコントローラのベーススクリプト「SensorControllerBase」でセンサーのサポートチェック機能をリアルタイム（キャッシュなし）に変更。
(ver.1.18)
・CPU使用率のステータス取得機能を追加（CpuRateController）。またそのCPU使用率をバーとして表示するスクリプト「CpuRateBar」（単一CPU）と一覧で表示する「CpuRateBarView」（複数CPU）を追加。
・UI-Text 上で FPS（Frame Per Second）を表示するスクリプト「FpsText.cs」を追加。またそのプレファブ「FpsMonitor」を追加。
・球・箱・線・メッシュ状の簡易ギズモを表示する「XGizmo」を追加。
・StorageLoadTextController と StorageSaveTextController に直接 UI.Text に読み書きするフィールド（targetText）を追加。
・EasyMusicPlayer に CurrentIndex と IsPlaying のプロパティを追加。
・EasyMusicPlayer の曲追加時のストレージオープン機能を同クラスに内包した（以前の StorageOpenAudioController から追加もそのまま使えます）。
・SceneLoadWithKey のインスペクタで「Use Name」の項目を「Scene Specification」で「Scene Name / Scene Build Index」で切り替えるように変更（内部的には bool 値のまま）。
・Android以外の無効コードパスを修正（Androidでビルドしている場合は影響ありません）。


※最新版はブログにて GoogleDrive からダウンロードできます（※日本語版のみ）。
http://fantom1x.blog130.fc2.com/blog-entry-273.html

------------------------------------------------
■使用ライブラリのライセンス等

このプラグインには Apache License, Version 2.0 のライセンスで配布されている成果物を含んでいます。
http://www.apache.org/licenses/LICENSE-2.0

ZXing ("Zebra Crossing") open source project (google). [ver.3.3.2] (QR Code Scan)
https://github.com/zxing/zxing

------------------------------------------------
それではよりよい作品の手助けになることを、心から願っています。

By Fantom

[Blog] http://fantom1x.blog130.fc2.com/
[Unity Connect] https://connect.unity.com/u/5abd008032b30600256e8ca9
[Twitter] https://twitter.com/fantom_1x
[SoundCloud] https://soundcloud.com/user-751508071
[Picotune] http://picotune.me/?@Fantom
[Monappy] https://monappy.jp/u/Fantom
[E-Mail] fantom_1x@yahoo.co.jp

