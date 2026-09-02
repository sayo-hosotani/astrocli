# Goal

## Objective

既存のインド占星術ホロスコープJSONを参照し、Vimshottari Dashaを最大5階層まで再計算してJSONファイルへ出力するCLIツールを実装する。

## Requirements

- `astrocli dasha <input-or-glob> [<input-or-glob> ...]` を提供する。
- 入力リクエストJSONの `source.chartFile` を基準に相対パスを解決する。
- Vimshottariの固定lord順、1年=365.25日、9分割、半開区間を使用する。
- Moon sidereal longitudeからNakshatra、Pada、lord、fractionCompletedを再計算する。
- depth 1〜5、period、referenceDateTime、既定期間、overlap filteringに対応する。
- 出生時の固定UTC offset、ミリ秒3桁切り捨てで日時を出力する。
- Legacy必須フィールドを検証し、不一致はWarning、欠落・不正値はErrorとする。
- 複数入力・glob・重複排除・`*_dasha.json` 除外・バッチ集計に対応する。
- 結果を入力リクエストと同じディレクトリの `<name>_dasha.json` に保存する。
- 値がない `children` と `warnings` は出力しない。

## Acceptance Criteria

- 深さ1〜5のDashaツリーを仕様どおり生成できる。
- 明示periodと深さ別の既定期間が仕様どおりに動作する。
- Legacy検証結果がWarning/Errorとして仕様どおり分類される。
- Error時に古い出力を残さず、バッチ処理を継続する。
- READMEに使用方法を記載する。
- `dotnet build AstroCli.slnx` が成功する。
- `dotnet test AstroCli.slnx --no-build` が成功する。

## Current Status

Completed.
