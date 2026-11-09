# GitHubでの共同作業ルール

このプロジェクトは`main`と短期間の作業ブランチで開発する。ゲームの実装・アセット編集の規約は[開発ルール](DEVELOPMENT_RULES.md)、起動方法は[Editorガイド](EDITOR_GUIDE.md)を参照する。

## 基本ルール

- `main`には直接pushせず、作業ブランチからPull Request（PR）を作る。
- 1つのIssue・PRは1つの目的に絞る。機能修正と無関係なフォルダ移動・整形・素材更新を混ぜない。
- 原則として作業前にIssueへ目的・完了条件・担当者・編集予定のシーン/Prefabを記載する。軽微な誤字修正はIssueなしのPRでもよい。
- 作業途中はDraft PRで共有し、未検証の項目を明記する。
- 作成者以外の1名以上の承認と指摘の解決を確認してマージする。追加修正が入った場合は、その内容を再確認してもらう。
- 共有ブランチの履歴を書き換えない。force push・`main`の削除・他人の未コミット変更の破棄を行わない。

これらはチームの運用ルール。文書の追加だけではGitHub側の強制設定は有効にならない。保護設定の状況は後述する。

## ブランチとコミット

| 用途 | ブランチ名の例 |
|---|---|
| 機能追加 | `feat/12-pitch-guide` |
| 不具合修正 | `fix/18-retry-reset` |
| 素材・見た目 | `art/23-batter-model` |
| 文書 | `docs/25-team-rules` |
| 整理・環境・テスト | `chore/30-package-update`、`test/31-contact-boundary` |

数字は対応するIssue番号。Issueがない軽微な変更は数字を省略してよい。長期間の`develop`ブランチは作らず、機能ごとに`main`へ戻す。

コミットは`種類: 変更内容`とする。種類は`feat` / `fix` / `art` / `docs` / `refactor` / `test` / `chore`を使い、日本語の説明でよい。

```text
fix: リトライ時に打球トレイルを初期化
docs: Unityアセットの共同編集手順を追加
```

## 通常の作業手順

以下はGitリポジトリのルートである`I-project/`で実行する。まず`git status`を見て、自分の未コミット変更を保存する。他人の変更や由来不明の変更が残っている場合は、そのまま別作業と混ぜず、担当者を確認する。

```bash
git status
git switch main
git pull --ff-only origin main
git switch -c feat/12-pitch-guide
```

実装・確認後は、今回の変更だけをステージして内容を確認する。アセット追加時は対応する`.meta`も含める。

```bash
git add <今回変更したファイルと対応するmeta>
git diff --cached
git diff --cached --check
git commit -m "feat: 投球ガイドを追加"
git push -u origin feat/12-pitch-guide
```

`git add`の例の山括弧は説明用。実際のファイル名へ置き換える。作業が進んだらGitHubで作業ブランチから`main`へのPRを作成する。GitHub Desktop等でも同じブランチ・差分確認・PRの流れに従う。

`main`の変更を取り込む場合は、自分の作業をコミットしてUnity Editorを閉じてから次を実行する。共有後の履歴を保つため、通常はmergeで同期する。

```bash
git fetch origin
git merge origin/main
```

競合した場合は担当者と変更意図を確認する。特に`.unity` / `.prefab` / `.asset` / `.meta`を、内容を確認せず片方の版で一括上書きしない。解決後はUnityで参照切れと動作を確認する。

## PRとレビュー

PRには次を記載する。

1. 解決する問題と変更後の動作。関連Issueがあれば番号も記載。
2. 主な変更と確認手順。見た目を変えた場合は画像や短い動画。
3. 実行したチェックと結果。実行していないものは理由付きで「未実施」。
4. シーン/Prefab/共通設定/パッケージ/素材利用条件への影響。

レビュアーは目的・設計の責務・参照の保全・検証結果を確認する。質問や修正依頼は具体的な箇所と理由を書く。単なる未検証を合格として扱わない。

承認・指摘解決・必要な確認が揃ったら、原則Squash mergeで1つの変更として取り込む。タイトルを変更の目的に合わせ、関連Issueを完了する。作業ブランチは他の人が使用していないことを確認して削除し、各自の`main`を更新する。

## マージ前の確認範囲

| 変更 | 必要な確認 |
|---|---|
| 文書・テンプレートのみ | `python3 Tools/validate_project.py`、差分・リンク確認。Unityテストの再実行は不要 |
| Domain / Application / 入力 / 進行 | 構成検査、Unityコンパイル、関連EditMode/PlayModeテスト、影響するPC操作 |
| UI / 表示 / 素材 / シーン / Prefab | 構成検査、Unityコンパイル、該当シーンのPlay、画像確認。日英UIに影響する場合は両方確認 |
| Unity / Packages / 共通設定 | 変更を事前共有し専用PRへ分離。インポート・コンパイル・全EditMode/PlayModeと起動を確認 |

Unity Test Runnerまたは`bash Tools/run-unity-tests.sh`を使う。現在の対象・結果と未検証範囲は[検証記録](VALIDATION.md)を参照する。操作・構成・仕様が変わったら対応する`Docs/`も同じPRで更新する。

## GitHub側で管理者が設定する項目

今回、GitHubのリモート設定は変更・確認していない。管理者は利用プラン・権限で設定可能な範囲を確認し、`main`のrulesetまたはブランチ保護に次を設定する。

- マージ前のPRと、作成者以外の1名以上の承認。
- レビュー会話の解決。新しい修正が入った場合の再承認。
- `main`のforce pushと削除の禁止。
- Squash mergeの利用。

CIは現時点で追加していない。チェックを必須化する際は、先に実際に動作するCIを導入し、存在するチェック名だけを指定する。Unityライセンスが必要な自動実行は、利用方法を決めてから導入する。

保護の利用条件と設定項目は[GitHubの公式ガイド](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches)を参照する。

## テンプレートの配置

`.github/CONTRIBUTING.md`を案内入口にし、PRとIssueのテンプレートを`.github/`へ配置する。GitHubでテンプレートを使うには、これらをリモートの既定ブランチへ反映する必要がある。配置仕様は[コントリビューションガイド](https://docs.github.com/en/communities/setting-up-your-project-for-healthy-contributions/setting-guidelines-for-repository-contributors)、[PRテンプレート](https://docs.github.com/en/communities/using-templates-to-encourage-useful-issues-and-pull-requests/creating-a-pull-request-template-for-your-repository)、[Issue/PRテンプレート](https://docs.github.com/en/communities/using-templates-to-encourage-useful-issues-and-pull-requests/about-issue-and-pull-request-templates)の公式説明に沿う。
