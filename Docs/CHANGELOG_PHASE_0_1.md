# Phase 0〜1 変更一覧

実装日: 2026-11-09。範囲はPC打撃の縦切りスライス。

## 追加整理: Unityバージョンと文書配置

- ユーザー環境のUnity **6000.6.8f1**へProjectVersionと対応Packagesを統一。実プロジェクトを新Editorでインポートし、URP Global Settings / ShaderGraph Settings / Player Settingsの自動移行とProjectAuditor Settingsの生成も反映。
- 開発文書7件を`Docs/`へ集約し、README・文書リンク・テストスクリプトを更新。Unity内にあった文書用metaを削除（実行用アセットから参照されていないことを確認）。
- `.gitignore`はプロジェクト直下を維持し、Unity生成物・IDE・OS・ローカルログを分類。XML/PNGの検証記録、Packagesのlock、Assetsのmetaは管理対象。
- PlayModeの再開テストを、描画なし環境でも固定時間ステップの更新を待つ方式に修正。
- 以下の「既存ファイルの変更」は初回実装時点の履歴。今回の統一でProjectVersionとPackagesも更新している。

## 追加更新: 打者視点・英語・球場

- プレイヤー原点を保つPlayerViewRigと、PC/将来のHMD向けIHeadPoseSourceを追加。右ドラッグで見回し、打球後も視点は自動移動しない。
- 英語で開始し、日本語へ切り替え可能。打球の方向・距離を表示。
- 球場をStadiumEnvironmentへ分離し、表面テクスチャ、座席・段床・ひさし、防球ネット、照明塔、ダッグアウトを生成。自作StadiumVisualPrefabの参照にも対応。
- バットの構えを持ち上げ、スイング中の表示と判定を同じ姿勢で動かす。球・判定・進行の既存ループは維持。
- 追加した主なファイル: Content/PlayerViewSettings.cs、Input/IHeadPoseSource.cs、Input/DesktopHeadPoseSource.cs、Presentation/PlayerViewRig.cs、Presentation/StadiumEnvironment.cs、Presentation/StadiumSurfaceLibrary.cs、Tests/PlayMode/FirstPersonViewTests.cs、Docs/FIRST_PERSON_STADIUM.md。C#のパスはAssets/_SoftballConnect/Scripts基準（TestsはAssets/_SoftballConnect基準）。
- GameConfig/VisualTheme、StadiumView、CharacterView、FeedbackPresenter、GameBootstrap、DesktopSwingModel、ProjectSetup、ローカライズと関連文書を更新。
- 一時コピーのUnity 6000.6.8f1でEditMode 25件・PlayMode 5件成功。詳細はVALIDATION.md。

## 今回の動作

Bootstrapからメニューへ進み、5球の投球・スイング・打球・結果・次球をプレイする。最後にミート数を表示し再挑戦できる。Space/クリック、ポーズ、リセット、メニュー、最小の日英表示、有限幅アシスト、軌道デバッグを接続した。守備や走塁・VRは実装範囲外。

## 既存ファイルの変更

- `ProjectSettings/EditorBuildSettings.asset`: 新規3シーンを有効にし、旧SampleSceneを無効で保存。既存の入力設定参照を維持。
- `.gitignore`: ローカルのTestResults/*.logを除外。結果XMLと目視用PNGは保管。
- `PROJECT_OVERVIEW.md`: 冒頭にMVPの現在地とドキュメントへのリンクを追加。元の調査記録は残す。このファイルは作業開始前から未追跡だった。
- `SOFTBALL_GAME_MASTER_SPEC.md`: 読み取りのみ。開始前から未追跡、内容変更なし。

初回実装では、既存シーン、Floreswa、ルートのマテリアル/アニメーション、URP設定、Packages、Unityの指定バージョンを保持した（Packages/Unity指定は今回の統一で更新）。実行用ファイルは以下の専用ルートにまとめ、Unityの.metaも同梱した。文書は現在`Docs/`に配置している。

## 新規ファイル

`Assets/_SoftballConnect/` 内（.metaを除く。各ファイルとフォルダにmetaが付属）:

```text
Art/Materials/BatOrange.mat
Art/Materials/SoftballYellow.mat
Art/Materials/StadiumBlue.mat
Config/DefaultGameConfig.asset
Config/DefaultVisualTheme.asset
Input/SoftballDesktop.inputactions
Localization/ui.json
Prefabs/Characters/PlaceholderCharacter.prefab
Prefabs/Equipment/Bat.prefab
Prefabs/Equipment/Softball.prefab
Scenes/Bootstrap.unity
Scenes/Legacy/SampleScene_Backup.unity
Scenes/MainMenu.unity
Scenes/Stadium_PC.unity
Scripts/Application/GameSessionController.cs
Scripts/Application/SoftballConnect.Application.asmdef
Scripts/Content/GameConfig.cs
Scripts/Content/VisualThemeConfig.cs
Scripts/Domain/BallFlightSimulator.cs
Scripts/Domain/BatContactSolver.cs
Scripts/Domain/BatInput.cs
Scripts/Domain/PitchService.cs
Scripts/Domain/SimulationSettings.cs
Scripts/Domain/SoftballConnect.Domain.asmdef
Scripts/Domain/Vec3.cs
Scripts/Editor/ProjectSetup.cs
Scripts/Editor/SoftballConnect.Editor.asmdef
Scripts/Input/DesktopBatInputSource.cs
Scripts/Presentation/CharacterView.cs
Scripts/Presentation/GameBootstrap.cs
Scripts/Presentation/StadiumView.cs
Scripts/SoftballConnect.Runtime.asmdef
Scripts/UI/FeedbackPresenter.cs
Scripts/UI/LocalizationService.cs
Tests/EditMode/SimulationTests.cs
Tests/EditMode/SoftballConnect.EditModeTests.asmdef
Tests/PlayMode/MvpFlowTests.cs
Tests/PlayMode/SoftballConnect.PlayModeTests.asmdef
```

プロジェクト直下:

```text
README.md
Docs/PROJECT_OVERVIEW.md
Docs/SOFTBALL_GAME_MASTER_SPEC.md
Docs/PROJECT_AUDIT.md
Docs/ARCHITECTURE.md
Docs/EDITOR_GUIDE.md
Docs/VALIDATION.md
Docs/CHANGELOG_PHASE_0_1.md
Tools/run-unity-tests.sh
Tools/validate_project.py
TestResults/EditMode.xml
TestResults/PlayMode.xml
TestResults/Visuals/MainMenu_EN.png
TestResults/Visuals/MainMenu_JA.png
TestResults/Visuals/Stadium_PC.png
TestResults/Visuals/Feedback_EN.png
```

## 次の作業

1. Unity 6000.6.8f1で実物キーボード/マウスによる10球以上の通し操作を行う。
2. Phase 2: モデルとバットの持ち方、下手投げ/スイングの表示アニメーション、球の視認性とカメラ、オリジナル音・演出を改善する。Domainの姿勢/接触と表示を揃えて調整する。
3. 原素材の権利とリグを確認してからPrefabを差し替え、通しプレイを再検証する。
4. Phase 3の走塁・簡易守備・学習と観客向け表示を別の縦切りスライスで追加する。競技ルールとワークショップ条件を確定する。

検証の成功数と適用範囲はVALIDATION.mdを参照。次フェーズの機能を実装済みとは扱わない。
