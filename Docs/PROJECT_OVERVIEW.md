# プロジェクト概要

Softball Connectは、研究室の交流を目的とした初心者向けソフトボール体験です。現在はマスター仕様のPhase 0〜1を基盤に、打者の目線で遊ぶPC打撃MVPを実装しています。初期表示は英語、日本語切り替えを備えています。

## 統一開発環境

| 項目 | バージョン |
|---|---|
| Unity | **6000.6.8f1** |
| URP | 17.6.0 |
| Input System | 1.20.1 |
| AI Navigation | 2.0.16 |
| Timeline | 6.6.0 |
| UGUI | 2.6.0 |
| Test Framework | 1.8.0 |

ProjectVersion.txt、Packagesのmanifest/lock、起動手順を上記環境に統一。テスト実行スクリプトはProjectVersion.txtからEditorの既定パスを決定します。

## 実装済み

- Bootstrap→MainMenu→Stadium_PCのシーン遷移。
- 投球→Space/左クリックでスイング→接触/空振り→打球飛行→結果→次球。
- 5球ラウンド、再挑戦、一時停止、リセット、メニュー復帰。
- 英語を初期表示とする日英UI、有限幅アシスト、軌道と接触位置のデバッグ。
- 打者の原点を保つ一人称視点、PCの右ドラッグ視線操作、将来のHMD用の頭の姿勢入力口。
- 芝・土の表面テクスチャ、段床と座席、防球ネット、照明塔、ダッグアウトのある球場。
- Unityに依存しないDomain/Applicationと、入力・表示・UIの分離。

新規実装は`Assets/_SoftballConnect/`に配置。未使用の旧球場・Floreswa・テンプレートを整理し、現行の球場・選手・装備を手続き生成します。自作球場はStadium Visual Prefabから差し替えられます。

## フォルダの役割

| 配置 | 内容 |
|---|---|
| Assets/_SoftballConnect | 新規ゲームのシーン、スクリプト、設定、Prefab、テスト |
| Assets/Settings | 現行のURP・Volume・品質向け設定 |
| Docs | 仕様・概要・監査・設計・操作・検証記録 |
| .github | 共同作業案内とPR/Issueテンプレート |
| Tools | 構成検査とUnityバッチテスト |
| TestResults | 成功結果XML・描画確認PNG |
| プロジェクト直下 | READMEとGit管理ファイル |

## 起動・検証・次の作業

`Assets/_SoftballConnect/Scenes/Bootstrap.unity`を開いてPlay。Enterで開始、Space/左クリックでスイング、Enterで次球に進みます。

- [Editor操作と設定](EDITOR_GUIDE.md)・[打者視点と球場の調整](FIRST_PERSON_STADIUM.md)
- [GitHub共同作業](CONTRIBUTING.md)・[開発ルール](DEVELOPMENT_RULES.md)
- [アーキテクチャ](ARCHITECTURE.md)
- [検証結果と未検証項目](VALIDATION.md)
- [初期監査とバージョン移行履歴](PROJECT_AUDIT.md)
- [マスター仕様](SOFTBALL_GAME_MASTER_SPEC.md)

次は実物入力による連続操作を確認し、Phase 2のモデル・アニメーション・視認性・演出を改善します。守備・走塁・VRは後続です。
