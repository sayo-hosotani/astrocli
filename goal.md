# Goal

## Objective

西洋占星術ネイタル固定のまま、天体位置とASCの計算エンジンをSharpAstrology.SwissEph Moshierへ切り替える。

## Requirements

### Scope

- 今回は西洋占星術ネイタル固定のままとする。
- CLI入力形式は現状維持とする。
- `location` 出力は現状維持とする。
- `ascendant` 出力は現状維持とする。
- `bodies` の主要10天体出力は維持する。
- 天体位置はtopocentricではなく地心基準で計算する。
- 度数表記は既存どおり60進数文字列にする。
- 各天体には、黄経、星座名、サイン内度数を含める。
- ハウスカスプ追加は今回のscopeに含めない。

### Engine

- `SharpAstrology.SwissEph` を使用する。
- `SharpAstrology.SwissEph` はMoshierモードで使用する。
- Swiss Ephemeris `.se1` ファイルは使用しない。
- JPL `.eph` ファイルは使用しない。
- C由来のネイティブDLLは使用しない。
- AGPL-3.0のライセンス制約は承知のうえで進める。
- 現在のAstronomy Engine実装は `astronomy-engine` ブランチを退避先として扱う。
- Moshierの精度が不十分な場合は、`astronomy-engine` ブランチを参照して戻す可能性を残す。

### Bodies

`bodies` には引き続き次の天体を出力する。

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
- JSON出力の構造は既存仕様を維持する。
- JSON出力の `bodies` に `sun`、`moon`、`mercury`、`venus`、`mars`、`jupiter`、`saturn`、`uranus`、`neptune`、`pluto` が含まれる。
- 各 `bodies.*` には `name`、`eclipticLongitude`、`sign`、`degreeInSign` が含まれる。
- 各 `bodies.*.eclipticLongitude` は `15°40’12″` のような60進数文字列である。
- 各 `bodies.*.degreeInSign` は `15°40’12″` のような60進数文字列である。
- 既存の `inputDateTime`、`utcDateTime`、`system`、`chart`、`location`、`ascendant` 出力は維持される。
- 固定日時・固定位置のスナップショットテストをSharpAstrology.SwissEph Moshierの値へ更新する。
- 主要10天体の黄経がSharpAstrology.SwissEph Moshierから計算されることをテストする。
- ASCがSharpAstrology.SwissEph Moshierまたは同ライブラリのハウス計算由来で計算されることをテストする。
- `CosineKitty.AstronomyEngine` への依存が不要になっていれば削除する。
- READMEに計算エンジンがSharpAstrology.SwissEph Moshierであることを記載する。
- `dotnet build AstroCli.slnx` が成功する。
- `dotnet test AstroCli.slnx --no-build` が成功する。

## Current Status

Completed.

ユーザー承認済み。Codex goalに設定し、達成済み。
