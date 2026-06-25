# AGENTS

## Entry Points

Codexは作業開始時に、このファイルを入口としてプロジェクト内の情報を参照する。

## Reference Order

1. `README.md`
   - アプリの目的、コンセプト、技術的ではないドメイン知識を確認する。
2. `goal.md`
   - 現在の作業目的、要件、達成条件を確認する。
3. `.agents/rules/`
   - 作業中に常に守るプロジェクトルールを確認する。
4. `.agents/skills/`
   - 作業に対応する再利用可能な手順がある場合に参照する。

## Rules

`.agents/rules/` には、プロジェクトで常に守るルールを書く。

参照タイミング:

- 作業開始時
- goalを作成または更新するとき
- 議事録を作成するとき
- rulesとskillsの使い分けに迷ったとき
- goal達成を確認するとき

## Skills

`.agents/skills/` には、繰り返し使う作業手順を書く。

参照タイミング:

- 議事録を作成するとき
- goalを作成、履歴化、設定するとき
- ビルドするとき
- テストするとき
- goal達成を確認するとき

## Rules And Skills Distinction

- rulesには、常に守る判断基準、禁止事項、保存場所、運用方針を書く。
- skillsには、特定の作業を実行するための手順、コマンド、確認観点、成果物を書く。
- `goal.md` には現在の作業目的、要件、達成条件だけを書く。
- 検討経緯や意思決定の背景は `docs/notes/` に保存する。
- `plan.md` は使用しない。

## Archives

- 議事録: `docs/notes/yyyymmdd-hhnn-note.md`
- goal履歴: `docs/goals/yyyymmdd-hhnn-goal.md`

## Current Goal

現在有効なgoalは `goal.md` を参照する。
