# AstroCLI

## Purpose

AstroCLIは、コマンドラインからテキストベースの占術チャートと天文情報を出力するCLIツールです。

## Concept

最初は簡単な天文情報の出力から始め、段階的に西洋占星術、インド占星術、四柱推命、紫微斗数などの占術チャート出力へ広げる。

占星術向けの完成済みライブラリに依存しすぎず、天文計算の根拠を追える設計を重視する。Swiss Ephemerisは検証や比較対象として扱い、必要に応じて代替実装を作成する。

## Domain Knowledge

- アプリの種類はCLIツール。
- 出力はまずテキストベースにする。
- 初期機能は、簡単な天文情報の出力。
- 初期MVPでは、西洋占星術のネイタル固定で、指定日時の太陽と月の位置をJSONで標準出力に出す。
- 初期MVPの日時入力は `"2025-09-04 22:00:00 +09:00"` の形式に固定する。
- 最終的には複数の占術を扱う。
- 想定する占術は、西洋占星術、インド占星術、四柱推命、紫微斗数。
- 占術ごとに、ネイタル、トランジット、プログレス、2種類のチャートのアスペクトなどのサブカテゴリを指定できるようにする。
- 占いの種類や細かなオプションは、JSONなどの設定ファイルからインポートできるようにする。
- 毎回すべてのオプションをCLI引数で指定しなくても使える設計にする。
- 引数で指定できる項目はJSON設定ファイルでも指定できるようにする。
- JSON設定ファイルで指定できる項目はCLI引数でも指定できるようにする。
- 将来は、CLI引数で指定した内容をJSON設定ファイルとして保存できるようにする。
- 将来はタイムゾーンや時差もJSON設定ファイルから指定できるようにする。
- JSON以外の出力形式やファイル出力は将来対応とする。
- 実装言語はC#。
- .NETはサポート中の最新LTSを優先する。
- 2026-06-25時点では .NET 10 が最新LTS。
- 天文情報の取得には `cosinekitty/astronomy` を主候補として検討する。
- Swiss Ephemerisは検証用に必要な場合だけ使う。
- Swiss Ephemeris関連の既存C#リポジトリは参考にするが、必要に応じて代替を作成する。
- 占星術ライブラリを使うか、天文ライブラリから占星術出力を組み立てるかは、精度だけでなく設計思想、ライセンス、実装主権のトレードオフとして判断する。
- JPL DEやSwiss Ephemerisのような高精度暦は便利だが、出力の意味、座標系、時刻系、ハウス計算などの設計判断を明示する必要がある。

## Initial MVP

初期MVPでは、西洋占星術のネイタル固定で、指定日時の太陽と月の位置をJSONで標準出力に出す。

ビルド:

```text
DOTNET_CLI_HOME=/home/ubuntu/astrocli/.dotnet-home dotnet build tests/AstroCli.Tests/AstroCli.Tests.csproj
```

テスト:

```text
DOTNET_CLI_HOME=/home/ubuntu/astrocli/.dotnet-home dotnet test tests/AstroCli.Tests/AstroCli.Tests.csproj
```

実行例:

```text
DOTNET_CLI_HOME=/home/ubuntu/astrocli/.dotnet-home dotnet build src/AstroCli/AstroCli.csproj
src/AstroCli/bin/Debug/net10.0/astrocli "1989-07-08 05:19:00 +09:00"
```

出力には、入力日時、UTC換算日時、占術体系、チャート種別、太陽と月の黄経度数、星座、星座内度数を含める。

## Project Operation

- `goal.md` は現在の作業対象を表す。
- `goal.md` には、Codexが作業を進めるために必要な目的、要件、達成条件だけを書く。
- 検討経緯や意思決定の背景は議事録に残す。
- 恒久的に守る重要事項はrulesに残す。
- 繰り返し使う作業手順はskillsに残す。
- `plan.md` は使用しない。
