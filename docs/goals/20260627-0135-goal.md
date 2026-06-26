# Goal

## Objective

西洋占星術ネイタル固定のまま、JSON出力の `bodies` を主要10天体に拡張する。

## Requirements

### Scope

- 今回は西洋占星術ネイタル固定のままとする。
- CLI入力形式は現状維持とする。
- `location` 出力は現状維持とする。
- `ascendant` 出力は現状維持とする。
- 天体位置はtopocentricではなく地心基準で計算する。
- 度数表記は既存どおり60進数文字列にする。
- 各天体には、黄経、星座名、サイン内度数を含める。

### Bodies

`bodies` に次の天体を出力する。

- `sun`
- `moon`
- `mercury`
- `venus`
- `mars`
- `jupiter`
- `saturn`
- `uranus`
- `neptune`
- `pluto`

## Acceptance Criteria

- `astrocli "1989-07-08 05:19:00 +09:00" "35°41’22″N,139°41’30″E"` が成功する。
- JSON出力の `bodies` に `sun`、`moon`、`mercury`、`venus`、`mars`、`jupiter`、`saturn`、`uranus`、`neptune`、`pluto` が含まれる。
- 各 `bodies.*` には `name`、`eclipticLongitude`、`sign`、`degreeInSign` が含まれる。
- 各 `bodies.*.eclipticLongitude` は `15°40’12″` のような60進数文字列である。
- 各 `bodies.*.degreeInSign` は `15°40’12″` のような60進数文字列である。
- 既存の `inputDateTime`、`utcDateTime`、`system`、`chart`、`location`、`ascendant` 出力は維持される。
- 固定日時・固定位置のスナップショットテストで主要10天体の出力を検証する。
- Astronomy Engineとの比較テストで主要10天体の黄経を検証する。
- `dotnet build AstroCli.slnx` が成功する。
- `dotnet test AstroCli.slnx --no-build` が成功する。

## Current Status

Completed.

ユーザー承認済み。Codex goalに設定し、達成済み。
