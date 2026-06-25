# Workflow Rules

## Goal Loop

Codexとの作業ループは、次の順序で進める。

- Codexと会話して、実装または改善したい内容をgoalとして整理する。
- 会話内容の要点を議事録として `docs/notes/yyyymmdd-hhnn-note.md` に保存する。
- 現在有効な仕様と達成条件を `goal.md` に書く。
- goalの履歴を `docs/goals/yyyymmdd-hhnn-goal.md` に保存する。
- ユーザーが承認した場合のみ、`goal.md` の内容をCodexのgoalに設定する。
- Codexのgoalに到達したら、次のgoalについてユーザーと会話を始める。

## File Roles

- `README.md` には、アプリの目的、コンセプト、技術的ではないドメイン知識を書く。
- `AGENTS.md` には、Codexがrulesとskillsをどのように参照すればよいかを書く。
- `goal.md` には、Codexが作業を進めるために必要な目的、要件、達成条件だけを書く。
- `docs/notes/` には、検討経緯や意思決定の背景を書く。
- `docs/goals/` には、過去のgoalを保存する。
- `plan.md` は使用しない。

## Source Of Truth

- `goal.md` は作業サイクルごとに変わる現在値として扱う。
- 重要事項は `goal.md` ではなく `.agents/rules` または `.agents/skills` に書く。
- 恒久的に守るべき重要事項は `.agents/rules` に書く。
- 繰り返し使う作業手順、コマンド、チェックリストは `.agents/skills` に書く。
- 検討経緯や意思決定の背景は `goal.md` ではなく議事録に書く。
