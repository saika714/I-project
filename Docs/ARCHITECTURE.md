# アーキテクチャと配置規約

## フォルダ

```text
Assets/_SoftballConnect/
  Art/Materials/             ArcadeSurface / ArcadeTrailと旧マテリアル
  Config/                   DefaultGameConfig / DefaultVisualTheme
  Input/                    SoftballDesktop.inputactions
  Localization/             ui.json（表示文字列）
  Scenes/
    Bootstrap.unity         メニューへ遷移
    MainMenu.unity          開始と操作説明
    Stadium_PC.unity        PC打撃のComposition Root
  Scripts/
    Domain/                 エンジン非依存の幾何・設定・シミュレーション
    Application/            ゲーム進行の唯一の管理者
    Content/                ScriptableObjectと表示Prefab参照
    Input/                  Input Systemアダプタ
    Presentation/           Unityオブジェクト・球場・カメラ・キャラクター
    UI/                     UI構築・ローカライズ・状態の表示
    Editor/                 欠けたアセットの生成コマンド
  Tests/EditMode/           純粋C#の判定・進行テスト
  Tests/PlayMode/           シーン・UI・入力・Unity Updateの接続テスト

Docs/                       仕様・概要・監査・操作・設計・検証・変更一覧
.github/                    GitHub向け案内・PR/Issueテンプレート
README.md                   起動方法と文書への入口
.gitignore / .gitattributes  Git管理用。プロジェクト直下に置く
Tools/                      構成検査とUnityテスト実行
TestResults/                実測XML・プレビューPNG（Unityへインポートしない）
```

Markdown文書は`Docs/`へ集約し、Unityへの不要な文書インポートと.metaの生成を避ける。`README.md`だけを入口として直下に残す。GitHubが読み込む案内・テンプレートは例外として`.github/`へ置く。文書中の`Assets/`等のコードパスはUnityプロジェクト直下を基準とする。

未使用の旧球場・Floreswa・テンプレート・旧装備Prefabは削除済み。現行のURP設定はAssets/Settingsに維持する。削除と参照の整理は[旧アセットの整理](LEGACY_CLEANUP.md)を参照。未使用の将来用フォルダやVRシーンの空ファイルは作らない。

## 依存方向

```text
Domain (noEngineReferences)
   ↑
Application (noEngineReferences)
   ↑
Runtime: Content / Input / Presentation / UI
   ↑
Editor（Editor限定）
```

DomainとApplicationにはUnityEngine、MonoBehaviour、Scene、Input Systemへの参照がない。asmdefで逆依存を防ぐ。RuntimeのみがInput System / UGUIを参照し、XRパッケージを要求しない。PlayModeテストのURP参照はプレビュー撮影用。

| ファイル/型 | 責務 |
|---|---|
| Vec3 / BatPose / BatInputFrame | エンジン非依存の座標と入力姿勢データ |
| SimulationSettings | 寸法・球速相当の時間・バット・アシストの設定と妥当性検査 |
| DesktopSwingModel | スイング意図を固定平面のGrip/Tip・速度に変換 |
| PitchService | リリース位置・到達点・時間から投球位置を算出 |
| BaseRunningPlay | 塁の座標、打球ごとの進塁数、走者の移動とホーム到達の計算 |
| BatContactSolver | 同一時間区間の球とバット線分の掃引・接触情報・打球初速 |
| BallFlightSimulator | 重力/抵抗、地面と円形フェンスの交差、飛距離と分類 |
| GameSessionController | 入力受取、唯一の状態遷移、接触/結果の一度だけの確定 |
| DesktopBatInputSource | 専用Input Actionsを複製・有効化し意図を供給 |
| GameBootstrap | シーン入口、依存の接続、固定ステップ駆動、ユーザーコマンド |
| ArcadeArt / StadiumView / CharacterView | 旧モデルに依存しない球場・選手の生成、バット姿勢、下手投げ、打球演出 |
| PlayerViewRig / DesktopHeadPoseSource / IHeadPoseSource | 打者の原点・頭の姿勢・PCの視線操作。打球や結果による自動移動を行わない |
| StadiumEnvironment / StadiumSurfaceLibrary | 寸法に基づく球場、座席・施設と独自の表面テクスチャ |
| ArcadeSound | 接触・結果の短い合成音、ポーズ連動・リソース破棄 |
| FeedbackPresenter | セッションのスナップショットを表示しコマンドを発行 |
| LocalizationService | 外部JSONのキーから表示文字列を取得 |
| ProjectSetup | 欠けたアセットだけを作成。既存シーンを再生成しない |

## 状態・時間・接触

```text
Prepare → PitchWindup → BallInFlight → BallInPlay → Feedback
                             └──── miss ────────────┘
Feedback → Prepare（次球） / RoundResult（規定球数後）
試合モードのフェア打球: BallInPlay → BaseRunning → Feedback
Restart: どの状態からもPrepareへ明示的に初期化
```

BallInFlightにスイング時間窓・接触/ミス分岐をまとめた。Prepareは0.5秒、Windupは設定時間。結果はEnter/ボタンまで待つ。スイングは1球1回。`StateChanged`、`BallContacted`、`PlayResolved`に必要な値を載せ、StadiumViewは破棄時に購読解除する。

Unity Updateが最大0.25秒を1/120秒単位に分割してSession.Tickへ渡す。端末が極端に遅い場合はシミュレーションを遅らせ、長い1ステップのすり抜けを避ける。RigidbodyやOnCollisionに結果確定を任せず、表示と判定は同じ位置データを使う。ポーズ中はシミュレーションを停止する。頭の姿勢入力は受け取り続け、将来のHMDで視点を凍結しない。

掃引は球と線分両端を同じ補間時刻で比較する。相対移動量に応じてサンプル数を増やし、1サンプルの移動量を合計判定半径の1/4以下にする。短い固定ステップの線形近似であり、厳密な連続回転カプセル解析ではない。アシストは合計判定半径に0.10mを加えるだけで、自動命中にしない。F3/Scene Gizmosで確認可能。

打球方向はタイミング、速度は相対速度と芯からのずれ、角度は芯からのずれに基づく簡略モデル。フェア判定はz>=0かつ|x|<=z、フェンスはホーム中心の円。初回の地面/フェンス接触で停止する。フェンス上端を越えたフェア打球はHomeRun表示。反射・転がり・キャッチ・正式ルールの得点判定は後続。最大飛行時間を超えた場合は現在位置で打ち切る。

## 差し替え契約

- **Character**: CharacterView / ModelRoot配下に、テーマの色を使って成人の身長に近い仮選手を生成する。一人称の打者は手を表示し、頭・胴体を非表示にする。`ThrowArm`が投球を表し、打撃時の両手は実際のGrip/Tip座標へ追従する。旧CharacterModel参照は現在の表示では使用しない。
- **Bat**: BatRigがGrip座標を持ち、BatVisualを線分中央へ配置。標準Cylinder（高さ2、Y軸方向）を1単位のモデルとして正規化しておく。ゲーム判定は見た目のColliderを参照しない。
- **Ball**: 直径1、原点中心のモデルをBallRadius×2で拡大する。データ位置を直接表示する。
- **Stadium**: FieldGeometryはHome/Releaseの基準、StadiumVisualsは装飾。表示の変更で競技距離を変更しない。判定寸法はGameConfigから取得する。
- **Input**: `IBatInputSource.Sample(dt)`がBatPose・速度・SwingTriggered・IsSwingActiveを返し、Resetを実装する。XRは追跡姿勢を同形式へ変換して接続する。Inspectorへinterfaceを直接保存しない。
- **Content**: DefaultGameConfigがSimulation設定、入力、文字列、DefaultVisualThemeを参照。テーマは色・球場名・描画用マテリアル・効果音量を持つ。実行開始時にSimulation設定を複製し、表示用マテリアルも実行時に生成する。プレイ操作は元アセットを変更しない。

現在の表示と調整箇所は[打者視点・英語表示・スタジアム](FIRST_PERSON_STADIUM.md)を参照。

試合・練習のモード、走塁・得点、球種と得点音楽の現在の仕様は[試合・練習モード](MATCH_AND_PRACTICE.md)を参照。試合ではGameSessionControllerがBaseRunningPlayを所有し、ホーム到達時にRunsScoredを発行する。ScoreMusicはGameConfigのAudioClip参照で差し替える。

最初のスライスではシーンにGameBootstrapと設定参照を保存し、球場・UIはPlay時に生成する。Play終了後のHierarchy編集は保存されない。Inspectorで永続化する調整先はConfig/Prefabsとする。
