# Unity Editorで起動する

## 起動

1. Unity Hubから `I-project` を **6000.6.8f1** で開く。Packagesの解決とコンパイルが終わるまで待つ。
2. Projectで `Assets/_SoftballConnect/Scenes/Bootstrap.unity` を開く。
3. Consoleを開き、コンパイルエラーがないことを確認する。Gameビューはまず16:9 / 1600×900相当を推奨。既定の1024×768でもCanvasScalerで拡縮するが、各解像度の手動確認は必要。
4. Playを押す。MainMenuへ移り、操作説明と試合・練習の選択ボタンが表示される。
5. **Gameビューをクリックしてフォーカス**し、モードを選んでStadium_PCへ進む。Enterは練習を開始する。
6. 下手投げの予告を見て、黄色のタイミング領域付近でSpace/左クリックを1回押す。試合のフェア打球ではW／↑または走るボタンで進塁する。結果表示後、Enter/次の投球で続ける。
7. 5球後の結果ボタンでラウンド集計へ進み、Enter/再挑戦でラウンドを初期化する。

Stadium_PCを直接開いてPlayしても操作可能。Bootstrapはビルド時の入口。File → Build ProfilesのScene ListでBootstrap、MainMenu、Stadium_PCの順に有効になっている。旧SampleSceneとバックアップは整理済み。

新規シーンとConfig/Prefabは同梱済み。欠落時だけ `Softball Connect → Create Missing MVP Assets` を実行する。保存確認後、不足分を生成しScene Listを整える。既にあるシーンやConfigは上書きしない。再実行でScene Listは3シーンが先頭になるため、後続フェーズでビルド構成を変える際には確認する。

現在の指定は **Unity 6000.6.8f1**。ProjectVersionとPackagesはこの環境に統一済み。文書はプロジェクト直下の`Docs/`に集約し、UnityのAssetsには実行用コンテンツのみを配置する。

メニューは英語で始まる。プレイ中は打者の目位置に視点を保ち、右ドラッグで見回せる。打球後や結果画面で視点を自動移動しない。

## 操作

| キー/操作 | 動作 |
|---|---|
| Space / 左クリック | 投球中に1回スイング。UI上のクリックはスイングしない |
| Enter / 中央ボタン | 開始、次の球、ラウンド結果、再挑戦 |
| R | どの状態でもラウンド全体をリセット。ポーズも解除 |
| Esc | 一時停止/再開 |
| M | メニューに戻る |
| A | 投球間/結果画面でアシストON/OFF。初期ON |
| 右ドラッグ | PCで首の向きを調整 |
| L | 英語（初期表示）/日本語のUI切替。シーン遷移中は選択を維持、永続保存はしない |
| F3 | デバッグ表示ON/OFF |
| W / ↑ 長押し | 試合の走塁中に走る。離すと止まる |
| 走る / 止まるボタン | 自動走塁の開始・停止 |
| P / 球種ボタン | 練習で次の球種を変更。投球中は変更不可 |

固定コース・固定打撃平面。マウスによる狙い移動は今回の範囲外。最初はリリース後約1.35秒で押すとミートしやすい。画面の球とタイミングガイドを使って調整する。早すぎる・遅すぎるスイング、見送りはミスとして次へ進める。

## 設定・アセットの編集

この節のパスは`Assets/_SoftballConnect/`を基準とする。

- `Config/DefaultGameConfig.asset`: Player Viewで目の位置・PC視野角・マウス感度を調整。SimulationのPitchDurationを大きくすると遅い球。WindupDuration、PitchDistance、BaseDistance、FenceDistance/Height、SwingDuration、AssistRadius、BallsPerRound等を設定。長さ・時間・重力は正の値、抵抗・アシストは0以上。変更後はPlayを入り直す。
- `Config/DefaultVisualTheme.asset`: 芝・土・座席・フェンスの色、座席数、球場名、音量を調整。Stadium Visual Prefabに自分の球場Prefabを指定できる。未使用の旧Prefab/マテリアル参照は削除済み。詳細はFIRST_PERSON_STADIUM.md参照。
- `Input/SoftballDesktop.inputactions`: Battingマップの操作割り当て。コードが読むアクション名は変更しない。
- `Localization/ui.json`: 日英文字列。OSフォントを動的に利用するため、日本語フォントがないPCではフォント追加・参照設計の検討が必要。フォントを外部から同梱していない。

得点曲はDefaultGameConfigのScore MusicへAudioClipを割り当てる。走塁速度・進塁距離・変化球の曲がり幅も同ConfigのSimulationから調整できる。[試合・練習モードの詳細](MATCH_AND_PRACTICE.md)を参照。

## デバッグ

Play中にF3。Gameビューに投球予定軌道とバットの掃引履歴を表示する。SceneビューでGizmosを有効にすると球の半径、接触点・法線、バット先端のアシスト半径を確認できる。表示は診断用の近似で、衝突はDomain内の全線分に対して計算する。

## テスト

Window → General → Test Runnerを開く。

1. EditModeでSoftballConnect.Testsを実行。40ケースが対象。
2. PlayModeでSoftballConnect.Testsを実行。通常のゲーム確認は8ケース。従来のシーン完走・入力/ポーズ・言語・打者視点・外部の頭の姿勢入力に、試合の走塁と得点、練習の球種選択、得点曲の差し替えを追加。ReportCaptureカテゴリは別途レポート用4枚を生成する。現在のシーンを保存してから実行する。
3. 描画ありのPlayModeでは `TestResults/Visuals/` に従来画面、`TestResults/MatchModes/` に走塁・得点・球種のPNGを保存する。1600×900の静止画確認用で、実機操作や全解像度の代替ではない。

プロジェクト直下でのバッチ実行例（Editorパスの既定値はProjectVersion.txtから決定）:

```bash
SOFTBALL_UNITY_EDITOR='/Applications/Unity/Hub/Editor/6000.6.8f1/Unity.app/Contents/MacOS/Unity' bash Tools/run-unity-tests.sh
python3 Tools/validate_project.py
```

指定Editorが既に同じプロジェクトを開いている場合、バッチ実行前に閉じる。描画なしの自動テストはスクリプトに`-nographics`を追加できるが、その場合PNGは生成されない。

検証済み環境・合格数・未検証範囲はVALIDATION.mdに記録している。異なるEditorの結果を指定バージョンの成功として扱わない。

球場・視点の詳細な編集先は[打者視点・スタジアム](FIRST_PERSON_STADIUM.md)を参照。HMD実機での動作は未検証。
