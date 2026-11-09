# 旧アセットの整理

ユーザーが不要なlegacyの削除を許可したため、未使用参照を調べて削除した。初期監査・以前の変更記録にある「原本保存」は過去の状態であり、現在の構成はこの記録を参照する。

## 削除したもの

- `Assets/Scenes/SampleScene.unity`と`Assets/_SoftballConnect/Scenes/Legacy/`のバックアップ。
- `Assets/Floreswa/`のモデル・マテリアル・Prefab・展示シーン。
- Unityテンプレートの`Assets/TutorialInfo/`と`Assets/Readme.asset`。
- ルートの旧Batter/Infield/ground用Controller・BatterPose・芝/土/白マテリアル。
- 旧`InputSystem_Actions.inputactions`。専用の`SoftballDesktop.inputactions`を使用する。
- 新規ルート内でも現在未使用のPlaceholderCharacter、Bat/Softball Prefab、旧BatOrange/SoftballYellow/StadiumBlueマテリアル。
- VisualThemeConfigの未使用な旧Prefab/マテリアル参照フィールド。

各アセットと対応する`.meta`をセットで削除し、削除対象の73個のGUIDを保持するアセットがないことを事前確認した。旧データのうち元からGit管理されていたものは、初期コミット`0032c07`の履歴から参照できる。

## 参照と生成処理の更新

- ビルド対象はBootstrap / MainMenu / Stadium_PCの3シーン。
- 新規シーンのテンプレート参照をBootstrapへ変更。
- Project-wide入力の旧アセット参照を削除。打撃は専用Input Actions、UIはInput Systemの標準UI Actionsを利用する。
- ProjectSetupが旧シーンをコピーしたりLegacyフォルダを再作成したりしないよう更新。
- 構成検査は旧シーンとのハッシュ比較を廃止し、現在のビルドシーン・Config参照と孤立した`.meta`を検査する。

## 維持した描画設定

`SampleSceneProfile.asset`はPC/MobileのURP設定から現在も参照されていたため、削除せず**StadiumVolumeProfile.asset**へ改名した。GUIDとProfileの内容は維持し、PC/Mobileの参照を保っている。URP・品質・描画用マテリアルは現行ゲームに必要なものを残す。

開いているUnity Editorではインポート・コンパイル完了を待ち、Playを停止してからBootstrapを再生する。
