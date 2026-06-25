# AstroCLI

## Purpose

AstroCLIは、Codexと協力して仕様作成、goal設定、実装、テスト、次のgoal作成を繰り返すためのプロジェクトです。

## Concept

このプロジェクトでは、現在取り組む内容を `goal.md` に集約し、恒久的な運用ルールを `.agents/rules` に、再利用する作業手順を `.agents/skills` に分けて管理する。

Codexとの会話で仕様や方針を整理し、議事録として保存する。承認されたgoalだけをCodexの作業goalとして設定し、実装と検証を進める。

## Domain Knowledge

- `goal.md` は現在の作業対象を表す。
- `goal.md` には、Codexが作業を進めるために必要な目的、要件、達成条件だけを書く。
- 検討経緯や意思決定の背景は議事録に残す。
- 恒久的に守る重要事項はrulesに残す。
- 繰り返し使う作業手順はskillsに残す。
- `plan.md` は使用しない。
