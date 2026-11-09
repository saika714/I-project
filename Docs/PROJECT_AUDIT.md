# Phase 0 初期監査と仕様との差分

調査日: 2026-11-09（日本時間）。`SOFTBALL_GAME_MASTER_SPEC.md`の初版v1.0を設計基準とし、ユーザーが指定したPhase 0〜1のPC打撃に実装を限定した。以下の「初期」は今回の追加前を表す。

## 旧アセット削除後の現在地

ユーザーの許可に基づき、未使用の旧球場・Floreswa・テンプレート・旧Prefab/素材を削除した。詳細は[旧アセットの整理](LEGACY_CLEANUP.md)。以下の保存・バックアップに関する記述は初期監査の履歴。

## 現在の統一環境

2026-11-09のユーザー指定によりUnity **6000.6.8f1**へ統一。URP **17.6.0**、Input System **1.20.1**、UGUI **2.6.0**、Test Framework **1.8.0**と対応するmanifest/lockを採用。新Editorの実インポートでURP Global Settings、ShaderGraph Settings、Player Settingsを移行し、ProjectAuditor Settingsを生成。開発文書は`Docs/`へ移動した。以下の表は初回実装時の履歴であり、現在の指定ではない。

## 初期調査と初回実装時の対応

| 項目 | 初期調査で確認した事実 | 今回の対応 |
|---|---|---|
| Unity | ProjectVersion.txt: 6000.5.2f1 / eb73d3b415a1 | 初回は指定を維持し6000.6.8f1の一時コピーで検証。現在は同バージョンへ統一 |
| URP | manifest: 17.5.0。PC/MobileのRPAsset・Rendererが存在 | 既存品質設定を再利用。Quest向け性能調整は後続 |
| 入力 | Input System 1.19.0、activeInputHandler=1 | 打撃専用Action Mapを追加。旧Actionsは保存 |
| その他 | Navigation 2.0.13、Timeline 1.8.12、UGUI 2.5.0、Test Framework 1.7.0 | 追加SDK・外部素材・依存パッケージなし |
| ビルド | SampleSceneだけが有効 | Bootstrap→MainMenu→Stadium_PCを有効にし、旧シーンは無効で保存 |
| C# | Readme.cs / Editor/ReadmeEditor.csの2本のみ | 新規ゲームコードはすべて専用ルートへ隔離 |
| アセンブリ | asmdefなし | Domain / Application / Runtime / Editor / EditModeTests / PlayModeTestsに分離 |
| 画面 | 初期1024×768 | プロジェクト設定は維持。UIは1600×900基準のCanvasScaler |

初期のコンパイル済みLibraryやテスト結果はリポジトリに存在しなかったため、初期環境のConsole状態は未検証。初期コードの有無はファイル調査で確認した。

## シーン・素材の監査

`Assets/Scenes/SampleScene.unity` はPrimitiveを中心とした球場。ground、Infield、塁、重複したHomePlate、ファウルライン、フェンス3枚、投手マウンド、照明、カメラ、Global Volumeを含む。Pitcher_TEMP / Batter_TEMP / Catcher_TEMPは非アクティブ。バットは仮打者の子として存在する。

旧シーンでは1塁が(7, 0.03, 7)、2塁が(0, 0.03, 14)、投手がz=15、中央フェンスがz=30。塁間は約9.9mで、新実装の仮設定と一致しない。旧球場をそのまま判定に利用すると見た目と距離設定がずれるため、**芝・土・白線のマテリアルを再利用し、設定寸法から球場を生成する**方針とした。原本のオブジェクト名や重複は原本保存を優先し変更していない。

- `GrassMaaterial.mat`（元の綴りのまま）、`DirtMaterial.mat`、`WhiteMaterial.mat`: DefaultVisualThemeから直接参照。移動・複製による既存参照の破損を避けた。
- `Assets/Settings/`: PC/MobileのURP設定とVolume Profileを保存。初期から品質分離はあるが、Quest検証済み設定ではない。
- `Assets/Floreswa/`: FBX 12個、Prefab 12個、マテリアル13個、展示シーン1個。男性モデル9体、眼鏡2個、ソファ1個など。確認したmale01_1はGeneric import設定。打撃・下手投げアニメーションとして使えるかは未検証。
- FloreswaにREADME・LICENSE・利用許諾資料は見当たらない。プロジェクト内マテリアルも由来を示す資料は未確認。今回はFloreswaを保存し、ゲーム中は新規Primitiveの共通キャラクターを使用。権利が確認できたモデルを後でVisualThemeから差し替えられる。
- 旧InputSystem_ActionsはPlayer/UIの汎用マップで、Move/Look/Attackなどを持つ。打撃コードへの接続はなかった。既存UI設定を保持し、新規SoftballDesktopのBattingマップを実行時クローンして使用する。
- Batter.controller / BatterPose.anim / Infield.controller / ground.controllerは保存。今回の打撃判定には接続していない。

## シーン保全

原本は変更せず `Scenes/Legacy/SampleScene_Backup.unity` にバックアップした。バックアップと原本のSHA-256は一致。

```text
247bf474470063840c529eecf377c640863586fec0d9a8fe1ec6e335f5c363e2
```

Floreswaの展示シーンも変更していない。既存アセットの一括移動・削除は行っていない。

## 今回埋めた差分

- 新規3シーン、共通設定・表示設定、専用入力アセット、差し替え用Prefab。
- 一元的な状態管理、再現可能な投球、Space/左クリックからのスイング姿勢、時間を揃えた掃引接触判定。
- 重力・抵抗・地面・フェンスを扱う打球、フェア/ファウル・飛距離・タイミング表示。
- 5球ラウンド、各球の結果待ち、次球・再挑戦・リセット・一時停止・メニュー復帰。
- 操作ガイド、アシストON/OFF、日英の最小UI、軌道・接触位置デバッグ、テスト。

## 仮定と後続範囲

塁間18.29m、投球距離13.11m、フェンス50m、投球到達1.5秒は試作用デフォルト。研究室大会の確定ルールや厳密な競技物理の再現ではなく、GameConfigで変更可能。最初は固定コース・固定打撃平面・直球のみで、乱数を用いない。

守備・走塁・試合得点・モード選択・観客別Canvas・音・ヒットストップ・正式な神宮モチーフ球場・VRは後続。MVPのContactsはミート数であり、試合得点や安打数ではない。最小の日英UIは早期に文字列の直書きを避けるため追加し、本格的なPhase 3チュートリアルは実装していない。
