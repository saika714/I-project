# レポート用スクリーンショット

4枚とも**1920×1080 / PNG / 16:9**。Unity 6000.6.8f1で現在のゲームを実行・描画して撮影した。HMD実機の撮影ではない。

| 図 | 画像 | キャプション例 |
|---|---|---|
| 1 | [01_stadium.png](Images/01_stadium.png) | 開発中のソフトボール球場全景。内野・外野、観客席、防球ネット、照明塔、ダッグアウトを配置した。 |
| 2 | [02_pitch.png](Images/02_pitch.png) | 打者の目線で、投手からボールが飛来する投球シーン。 |
| 3 | [03_contact.png](Images/03_contact.png) | バットとボールの接触を確認する打撃シーン。頭の向きを打点へ向けて撮影した。 |
| 4 | [04_ui.png](Images/04_ui.png) | 操作説明と英語・日本語切り替えを備えた開始画面。初期表示は英語とした。 |

## 撮影条件

- 図1のみ、レポート用の独立した俯瞰カメラを使用。ゲーム中の打者視点は変更していない。
- 図2は投球の飛行中、図3は接触が確定した最初の時点を、実際のGameSessionControllerから再現して撮影した。
- 図1〜3では説明用に画面UIを非表示にし、図4では通常の開始UIを表示した。
- 画像の合成・生成AIによる補描は行っていない。球場・選手・ボールはゲーム内の手続き生成モデル。

## 再撮影

Unity Test RunnerのPlayModeで`ReportCaptureTests.ExportStadiumPitchContactAndUi`を実行する。通常の`Tools/run-unity-tests.sh`はReportCaptureカテゴリを除外し、ゲーム確認5件だけを実行する。撮影には描画が必要なため、`-nographics`を付けない。

macOSでのバッチ例（同じプロジェクトを開いたEditorは閉じてから実行）:

```bash
/Applications/Unity/Hub/Editor/6000.6.8f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath "$PWD" -runTests -testPlatform PlayMode \
  -testCategory ReportCapture \
  -testResults TestResults/ReportCapture.xml -logFile TestResults/ReportCapture.log
```

配布用の`report_screenshots.zip`はローカルに生成し、Gitでは元の4枚のPNGとこの説明を管理する。
