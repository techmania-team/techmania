Permission & Attribute Manual ('AndroidManifest.xml')

https://developer.android.com/guide/topics/security/permissions.html
https://developer.android.com/guide/topics/manifest/activity-element.html
List of functions requiring permission in manifest file (AndroidManifest.xml).

(*) Depending on the function you want to use, you need to manually add permissions in 'AndroidManifest.xml'. Since all permissions have been added to the demonstration manifest file 'AndroidManifest_demo.xml', if necessary, please copy and paste it in the same way (It is better to delete the permission of functions not used).

·Recording audio permission is required for Speech Recognizer.
<uses-permission android:name="android.permission.RECORD_AUDIO />

·To write (save) to External Storage, access permission (read/write) to media storage is required.
<uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" />

·To read (load) to External Storage, access permission (read) to media storage is required (* However, it is not necessary if there is "WRITE_EXTERNAL_STORAGE").
<uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" />

·Bluetooth permission is required for Bluetooth connection request.
<uses-permission android:name="android.permission.BLUETOOTH" />

·Vibrate permission is required for using vibrator.
<uses-permission android:name="android.permission.VIBRATE"/>

·BodySensor permission is required for using heart rate sensor.
<uses-permission android:name="android.permission.BODY_SENSORS"/>

·Device orientation change status, the following attribute is required for 'activiey' tag (* Unity is set by default).
android:configChanges="orientation|screenSize"

(*)It is better to also combine the attributes of rotation.
android:screenOrientation="sensor"


----------------------------------------------------------------------------
https://developer.android.com/guide/topics/security/permissions.html
https://developer.android.com/guide/topics/manifest/activity-element.html
マニフェストファイル「AndroidManifest.xml」にパーミッション・属性が必要な機能一覧

※使いたい機能によってはパーミッションを「AndroidManifest.xml」に手動で追加する必要があります。デモのマニフェストファイル「AndroidManifest_demo.xml」には全てのパーミッションが追加してあるので、必要であれば同じようにコピペなどして追加して下さい（利用しない機能のパーミッションは削除する方が好ましいです）。

・音声認識には録音パーミッションが必要です。
<uses-permission android:name="android.permission.RECORD_AUDIO />

・ExternalStorageに書き込み（保存）を行うにはメディアストレージへのアクセス（読み書き）パーミッションが必要です。
<uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" />

・ExternalStorageから読み込みを行うにはメディアストレージへのアクセスパーミッション（読み取り）が必要です（※ただし「WRITE_EXTERNAL_STORAGE」がある場合は必要ありません）。
<uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" />

・Bluetoothの接続要求を行うにはBluetoothパーミッションが必要です。
<uses-permission android:name="android.permission.BLUETOOTH" />

・バイブレーターを利用する場合には以下のパーミッションが必要です。
<uses-permission android:name="android.permission.VIBRATE"/>

・心拍センサー（HeartRate）を利用する場合にはバイタルサインに関するセンサーデータへのアクセスパーミッションが必要です。
<uses-permission android:name="android.permission.BODY_SENSORS"/>

・画面回転の変化取得を利用するには「activiey」タグに以下の属性が必要です（※Unity のデフォルトでは設定されています）。
android:configChanges="orientation|screenSize"

※また回転の属性も合わせた方が良いです。
android:screenOrientation="sensor"


------------------------------------------------
By Fantom

[Blog] http://fantom1x.blog130.fc2.com/
[Twitter] https://twitter.com/fantom_1x
[SoundCloud] https://soundcloud.com/user-751508071
[Picotune] http://picotune.me/?@Fantom
[Monappy] https://monappy.jp/u/Fantom
[E-Mail] fantom_1x@yahoo.co.jp

