# Vimshottari Dasha CLI 仕様書 — Codex実装用確定版

> **Codexへの指示**
>
> このMarkdownをPhase 1実装の正規仕様とする。一般的な占星術知識で仕様を補完・変更せず、ここに明記された計算規則・境界規則・入出力規則を優先すること。
> 実装技術（言語、ライブラリ、内部数値型、クラス構成等）は任意。ただし外部仕様と計算結果は本書に一致させること。
> LegacyChartValidatorは一時機能であり、DashaCalculator本体と依存関係を分離して、将来まとめて削除できるようにすること。

---

## 1. 目的

既存のインド占星術ホロスコープJSONを参照し、Vimshottari Dashaを最大5階層まで再計算して、指定期間の結果をJSONファイルへ出力するCLIツールを作る。

既存JSONに保存済みのDasha計算結果は計算入力として利用しない。

現フェーズでは天文計算そのものは行わず、既存JSONから出生時刻・Moon sidereal longitude・固定UTC offsetを取得する。

将来、出生日時・場所等から天文計算まで本ツールへ移行する可能性がある。

---

## 2. 対象Dasha階層

Vimshottari Dashaのみを対象とする。

最大5階層。

| levelNumber | levelName |
|---:|---|
| 1 | Mahadasha |
| 2 | Antardasha |
| 3 | Pratyantardasha |
| 4 | Sookshma Dasha |
| 5 | Prana Dasha |

各Dashaノードには `levelNumber` と `levelName` を両方出力する。

`duration` は出力しない。

---

## 3. Vimshottari lord sequence と年数

固定定義:

| Lord | Years |
|---|---:|
| Ketu | 7 |
| Venus | 20 |
| Sun | 6 |
| Moon | 10 |
| Mars | 7 |
| Rahu | 18 |
| Jupiter | 16 |
| Saturn | 19 |
| Mercury | 17 |

合計120年。

順序も固定:

```text
Ketu
Venus
Sun
Moon
Mars
Rahu
Jupiter
Saturn
Mercury
```

Mercuryの次はKetuへ戻る。

JSON上のlordは略称を使用せず、英語フルネームのみとする。

```json
"lord": "Venus"
```

---

## 4. Dasha年と期間計算

1 Dasha year:

```text
365.25 days
```

1 Vimshottari cycle:

```text
120 years = 43,830 days
```

Gregorian calendarの「年加算」は使用しない。

例:

```text
7 years = 7 × 365.25 days
```

下位Dashaの期間は再帰的に以下で計算する。

```text
childDuration = parentDuration × childLordYears / 120
```

各親Dashaは9個の子Dashaへ分割する。

子のlord sequenceは親lordから開始し、Vimshottari順で9個を並べる。

例:

```text
Mars/Mars
Mars/Rahu
Mars/Jupiter
Mars/Saturn
Mars/Mercury
Mars/Ketu
Mars/Venus
Mars/Sun
Mars/Moon
```

境界は連続させる。

```text
firstChild.startDate = parent.startDate
child[i].startDate = child[i-1].endDate
lastChild.endDate = parent.endDate
```

同一境界は内部的に同じ境界値を共有する。

---

## 5. CLI

基本形式:

```text
dasha <input-or-glob> [<input-or-glob> ...]
```

例:

```bash
dasha S.json
dasha S.json T.json K.json
dasha "*.json"
dasha S.json "T_*.json"
```

Phase 1では `--depth`、`--output` 等のCLIオプションを設けない。

計算条件は入力リクエストJSONに記述する。

---

## 6. 複数入力・glob

通常ファイルパスとglobを混在可能とする。

アプリ側でもglob展開する。

対応例:

```text
*.json
foo/*.json
```

非対応:

```text
**/*.json
```

サブディレクトリ再帰は行わない。

ルール:

- globが0件一致の場合、その引数は無視する。
- literal pathが存在しない場合、その入力はError。
- 複数引数・複数globから同じファイルが得られた場合は1回だけ処理する。
- 処理順はファイル名順。
- `*_dasha.json` は入力対象から自動除外する。
- 最終的な処理対象が0件なら `Processed: 0`、exit code 0。

---

## 7. 入力リクエストJSON

最小:

```json
{
  "source": {
    "chartFile": "./S_horoscope.json"
  }
}
```

全指定例:

```json
{
  "source": {
    "chartFile": "./S_horoscope.json"
  },
  "output": {
    "depth": 4,
    "referenceDateTime": "2026-08-12T15:00:00+09:00",
    "period": {
      "start": "2026-01-01T00:00:00+09:00",
      "end": "2028-12-31T23:59:59+09:00"
    }
  }
}
```

`output` 自体を省略可能。

未知フィールドは無視する。

既知フィールドが存在する場合、その値が仕様上不正ならError。

---

## 8. chartFile

`source.chartFile` は既存ホロスコープJSONを指定する。

- 絶対パス: そのまま使用。
- 相対パス: 入力リクエストJSONの存在するディレクトリを基準に解決。
- `~` 展開: 非対応。
- URL: 非対応。
- ローカルJSONファイルのみ対応。

---

## 9. DashaCalculator が参照する既存JSON項目

Dasha計算本体が使用する値は以下のみ。

```text
vedic_sidereal_lahiri.utcIso
vedic_sidereal_lahiri.siderealLons.Moon
input.utcOffsetHours
```

用途:

- `utcIso`: 出生絶対時刻。
- `siderealLons.Moon`: Lahiri方式の出生時Moon sidereal longitude。
- `utcOffsetHours`: 出力日時の固定UTC offset。

以下の既存計算済み情報はDashaCalculatorの計算入力として使用しない。

```text
dasha.*
vedic_sidereal_lahiri.nakshatras.Moon
```

`vedic_sidereal_lahiri.nakshatras.Moon` はLegacyChartValidatorでのみ参照する。

---

## 10. Nakshatra英語正規テーブル

出力JSONのNakshatra名はAstro-Seek表記を正規値として採用する。

| number | name |
|---:|---|
| 1 | Ashvinī |
| 2 | Bharanī |
| 3 | Kṛttikā |
| 4 | Rohinī |
| 5 | Mrigashīra |
| 6 | Ārdrā |
| 7 | Punarvasu |
| 8 | Pushya |
| 9 | Ashlesha |
| 10 | Maghā |
| 11 | Pūrva Phalgunī |
| 12 | Uttara Phalgunī |
| 13 | Hasta |
| 14 | Chitrā |
| 15 | Svātī |
| 16 | Vishākhā |
| 17 | Anurādhā |
| 18 | Jyeshtha |
| 19 | Mūla |
| 20 | Pūrva Ashādhā |
| 21 | Uttara Ashādhā |
| 22 | Shravana |
| 23 | Dhanistha |
| 24 | Shatabhisha |
| 25 | Pūrva Bhādrapadā |
| 26 | Uttara Bhādrapadā |
| 27 | Revatī |

Unicodeのダイアクリティカルマークを含め、この表記をそのまま正規値とする。

Astro-Seek上で恒星名が括弧書きされる場合でも、Nakshatra名部分のみを使用する。

---

## 11. Nakshatra lord の固定対応

Nakshatra lord は以下の9要素を3回繰り返す。

```text
Ketu, Venus, Sun, Moon, Mars, Rahu, Jupiter, Saturn, Mercury
```

したがって:

```text
1  Ashvinī      -> Ketu
2  Bharanī      -> Venus
3  Kṛttikā      -> Sun
4  Rohinī       -> Moon
5  Mrigashīra   -> Mars
6  Ārdrā        -> Rahu
7  Punarvasu    -> Jupiter
8  Pushya       -> Saturn
9  Ashlesha     -> Mercury
10 Maghā        -> Ketu
...
27 Revatī       -> Mercury
```

0始まり内部番号を `nakIndex0` とする場合:

```text
lord = VIMSHOTTARI_LORD_SEQUENCE[nakIndex0 % 9]
```

参照JSONの `nakshatras.Moon.ruler` はDashaCalculatorの入力にしない。

---

## 12. 出生時Nakshatra計算

Moon sidereal longitudeを `moonLongitude` とする。

まず `[0°, 360°)` に正規化する。

```text
moonNorm = normalize360(moonLongitude)
```

Nakshatraサイズ:

```text
nakshatraSize = 360° / 27 = 13°20′
```

境界は半開区間。

```text
Ashvinī  [0°00′, 13°20′)
Bharanī  [13°20′, 26°40′)
Kṛttikā  [26°40′, 40°00′)
...
```

ちょうど `13°20′` はBharanī。

概念式:

```text
nakIndex0 = floor(moonNorm / nakshatraSize)
number = nakIndex0 + 1

nakshatraStart = nakIndex0 * nakshatraSize

fractionCompleted
  = (moonNorm - nakshatraStart) / nakshatraSize
```

内部値:

```text
0 <= fractionCompleted < 1
```

Padaは各Nakshatraを4等分。

```text
1 Pada = 3°20′
```

こちらも半開区間。

```text
pada = floor(fractionCompleted * 4) + 1
```

`pada` は `1..4`。

出力例:

```json
"moonNakshatra": {
  "number": 11,
  "name": "Pūrva Phalgunī",
  "pada": 1,
  "lord": "Venus",
  "fractionCompleted": 0.286123456789
}
```

---

## 13. 出生時Mahadashaの決定

内部値 `fractionCompleted` を使って出生時Mahadashaを計算する。

```text
birthMahadashaLord
  = moonNakshatra.lord

birthMahadashaDuration
  = lordYears[birthMahadashaLord] * 365.25 days

elapsedAtBirth
  = birthMahadashaDuration * fractionCompleted

birthMahadashaStart
  = birthDateTime - elapsedAtBirth

birthMahadashaEnd
  = birthMahadashaStart + birthMahadashaDuration
```

したがって、出生時に進行中だった最初のMahadashaの `startDate` は出生日時より前になり得る。

出生時Mahadasha以降はVimshottari lord sequenceに従って連続的に生成する。

既存JSONに保存された `fractionCompleted`、balance、Mahadasha、Antardasha等の既存Dasha計算値は使用しない。

`initialMahadasha` のような別要約は出力しない。

必要なら出生時を含むperiodでDasha JSONを再出力する。

---

## 14. depth

指定可能:

```text
1..5
```

省略時:

```text
depth = 2
```

---

## 15. 対応cycleと境界

上限境界:

```text
cycleEnd = birthDateTime + 43,830 days
```

本ツールが保証する出力対象時間域:

```text
supportedWindow = [birthDateTime, cycleEnd)
```

Dashaノード自体は出生前から始まる場合や、`cycleEnd` より後に終わる場合がある。

これは正常。

Dashaノードの `startDate` / `endDate` は `supportedWindow` に合わせて切り詰めない。

---

## 16. period

形式:

```json
"period": {
  "start": "2026-01-01T00:00:00+09:00",
  "end": "2028-12-31T23:59:59+09:00"
}
```

`period` を指定する場合は `start` / `end` 両方必須。

offset付きISO 8601日時として解釈する。

入力offsetは出生offsetと異なってよい。絶対時刻として比較する。

periodも半開区間:

```text
[start, end)
```

有効条件:

```text
birthDateTime <= period.start < period.end <= cycleEnd
```

`period.end == cycleEnd` は許可する。

これにより、

```text
period.start = birthDateTime
period.end   = cycleEnd
```

を指定すれば、depth 1～2 のデフォルト全cycle出力と同じフィルタ範囲を明示指定できる。

---

## 17. period未指定時のデフォルト

### depth 1～2

```text
outputPeriod = [birthDateTime, cycleEnd)
```

### depth 3

`referenceDateTime` 時点で進行中のMahadasha期間を求める。

実際の `outputPeriod` は:

```text
currentMahadashaPeriod ∩ supportedWindow
```

### depth 4

```text
currentAntardashaPeriod ∩ supportedWindow
```

### depth 5

```text
currentPratyantardashaPeriod ∩ supportedWindow
```

つまり requested deepest level の2階層上にある現在Dasha全期間を基本とし、`supportedWindow` からはみ出す部分だけ `outputPeriod` 側でクリップする。

このクリップはフィルタ範囲である `meta.outputPeriod` のみに適用する。

Dashaノード自身の `startDate` / `endDate` はクリップしない。

`meta.outputPeriod` に出力された `start` / `end` を明示 `period` として再指定すれば、同じフィルタ範囲を再現できる。

---

## 18. referenceDateTime

`period` 未指定で現在Dashaを特定する必要がある場合:

1. `output.referenceDateTime`
2. 未指定なら実行時システム日時

の順で使用する。

有効範囲:

```text
birthDateTime <= referenceDateTime < cycleEnd
```

`referenceDateTime == cycleEnd` はError。

`period.end == cycleEnd` が許可されるのは、endが排他的境界だからである。

明示 `period` が存在する場合、`referenceDateTime` は `outputPeriod` 決定には使用しない。

ただし `referenceDateTime` フィールド自体が入力に存在する場合は、offset付きISO 8601として正しく解釈でき、かつ上記有効範囲内でなければErrorとする。

実際に期間決定へ使用した場合のみ `meta.referenceDateTime` を出力する。

---

## 19. Dasha期間境界

すべて半開区間:

```text
[startDate, endDate)
```

境界時刻ちょうどは次のDashaに属する。

---

## 20. period filtering

`period` / `meta.outputPeriod` はDashaノード日時を切り詰めるためには使わない。

出力対象ノードを選択するフィルタとしてのみ使う。

overlap条件:

```text
dasha.start < outputPeriod.end
AND
dasha.end > outputPeriod.start
```

overlapしたノードは元の `startDate` / `endDate` をそのまま出力する。

### nested tree

各階層で個別にoverlap判定する。

親ノードがoverlapするなら親を出力する。

`depth` がさらに深い場合、その親の子ノードのうち `outputPeriod` とoverlapする子だけを `children` に含め、同じ規則を再帰的に適用する。

例:

```text
Mahadasha       overlap -> 出力
├─ Antardasha A period外 -> 出力しない
├─ Antardasha B overlap  -> 出力
├─ Antardasha C overlap  -> 出力
└─ Antardasha D period外 -> 出力しない
```

出力対象の子が存在しない場合は `children` を省略する。

---

## 21. 出力Dashaノード

基本形:

```json
{
  "levelNumber": 3,
  "levelName": "Pratyantardasha",
  "lord": "Venus",
  "startDate": "2026-06-13T16:39:20.841+09:00",
  "endDate": "2026-08-20T03:55:50.841+09:00"
}
```

下位階層を出力する場合のみ:

```json
"children": [...]
```

を持つ。

---

## 22. 値なしフィールド

原則として値が存在しない項目は省略する。

以下のような空値は出さない。

```json
"children": []
```

```json
"warnings": []
```

```json
"something": null
```

---

## 23. 日時計算精度

内部計算ではミリ秒へ途中丸めしない。

可能な限り高い精度を保持する。

内部数値型は実装に任せる。

同一境界は共有し、

```text
previous.endDate == next.startDate
```

を保証する。

---

## 24. 日時出力精度

出力日時:

```text
YYYY-MM-DDTHH:mm:ss.SSS+09:00
```

ミリ秒3桁固定。

ミリ秒未満は切り捨てる。

```text
20.8410000 -> 20.841
20.8414999 -> 20.841
20.8419999 -> 20.841
```

四捨五入しない。

---

## 25. 出力timezone

出力JSON内の日時はすべて `input.utcOffsetHours` から得た出生チャートの固定UTC offsetに統一する。

対象:

- `birth.dateTime`
- `meta.referenceDateTime`
- `meta.outputPeriod.start`
- `meta.outputPeriod.end`
- 各Dasha `startDate`
- 各Dasha `endDate`

入力日時が `Z` や別offsetでも、絶対時刻へ変換後、出力時は出生固定offsetへ変換する。

IANA timezone / DSTはPhase 1では扱わない。

---

## 26. input.utcOffsetHours validation

`input.utcOffsetHours` は有効な固定offset `+HH:MM` / `-HH:MM` に正確に変換できる必要がある。

例:

```text
9     -> +09:00
5.5   -> +05:30
5.75  -> +05:45
-3.5  -> -03:30
```

概念的に:

```text
utcOffsetHours × 60
```

が整数分として表現できない値はError。

有効なISO 8601固定UTC offsetとして表現できない値もError。

---

## 27. birth 出力

```json
"birth": {
  "dateTime": "1989-07-08T05:19:00.000+09:00",
  "moonSiderealLongitude": 137.15712411385647,
  "moonNakshatra": {
    "number": 11,
    "name": "Pūrva Phalgunī",
    "pada": 1,
    "lord": "Venus",
    "fractionCompleted": 0.286123456789
  }
}
```

`initialMahadasha` は出力しない。

---

## 28. moonSiderealLongitude

参照元:

```text
vedic_sidereal_lahiri.siderealLons.Moon
```

現フェーズでは参照JSONの数値をそのまま出力する。

出力用の丸め・切り捨てをしない。

同じ元値をNakshatra/Dasha計算に使う。

---

## 29. fractionCompleted 出力精度

内部計算では高精度値を使う。

JSON出力時だけ、小数点以下12桁で切り捨てる。

末尾の不要な0は出力しない。

出力用に切り捨てた値をDasha計算へ再利用しない。

---

## 30. meta

基本形:

```json
"meta": {
  "dashaSystem": "Vimshottari",
  "depth": 4,
  "dashaYearDays": 365.25,
  "referenceDateTime": "2026-08-12T15:00:00.000+09:00",
  "outputPeriod": {
    "start": "...",
    "end": "..."
  }
}
```

`outputPeriod` は入力値の単純コピーではなく、デフォルト解決・supportedWindowとの共通部分計算後に、実際に採用したフィルタ範囲を記録する。

`referenceDateTime` は実際に期間決定へ使用した場合のみ出力。

以下は出力しない。

- `sourceChartFile`
- `schemaVersion`
- `currentDasha`

---

## 31. 出力JSON概形

```json
{
  "meta": {
    "dashaSystem": "Vimshottari",
    "depth": 3,
    "dashaYearDays": 365.25,
    "referenceDateTime": "2026-08-12T15:00:00.000+09:00",
    "outputPeriod": {
      "start": "2023-08-30T01:51:52.341+09:00",
      "end": "2030-08-29T19:51:52.341+09:00"
    }
  },
  "birth": {
    "dateTime": "1989-07-08T05:19:00.000+09:00",
    "moonSiderealLongitude": 137.15712411385647,
    "moonNakshatra": {
      "number": 11,
      "name": "Pūrva Phalgunī",
      "pada": 1,
      "lord": "Venus",
      "fractionCompleted": 0.286123456789
    }
  },
  "dashas": [
    {
      "levelNumber": 1,
      "levelName": "Mahadasha",
      "lord": "Mars",
      "startDate": "...",
      "endDate": "...",
      "children": [
        {
          "levelNumber": 2,
          "levelName": "Antardasha",
          "lord": "Saturn",
          "startDate": "...",
          "endDate": "..."
        }
      ]
    }
  ]
}
```

例中の日時・天体値は構造説明用であり、期待値ではない。

---

## 32. 出力ファイル

結果は標準出力ではなくファイルへ保存する。

```text
foo.json
-> foo_dasha.json
```

入力リクエストJSONと同じディレクトリへ出力。

既存出力は確認なしで上書き。

JSON形式:

- UTF-8
- 2スペースインデント
- pretty print
- 末尾改行あり

---

## 33. 古い出力ファイル

各入力ファイルの処理開始時に対応する既存 `*_dasha.json` を削除する。

結果:

```text
Success -> 新しい *_dasha.json あり
Warning -> 新しい *_dasha.json あり
Error   -> *_dasha.json なし
```

古い正常結果を今回の結果と誤認しないための仕様。

---

## 34. バッチ処理結果分類

1ファイルがErrorでも残りの処理は継続する。

ファイル単位で排他的に分類:

```text
Success
  WarningもErrorもなし

Warning
  JSON生成成功、1件以上Warningあり
  Successには含めない

Error
  処理失敗
```

---

## 35. CLI標準出力

最終集計は1行のみ。

正常のみ:

```text
Processed: 10 | Success: 10
```

Warningあり:

```text
Processed: 10 | Success: 7 | Warning: 3
```

Errorあり:

```text
Processed: 10 | Success: 7 | Warning: 2 | Error: 1
```

0件:

```text
Processed: 0
```

0件の `Warning` / `Error` 項目は表示しない。

処理対象が1件以上なら:

```text
Processed = Success + Warning + Error
```

`Warning` はWarningオブジェクト数ではなく、Warningが1件以上発生したファイル数。

---

## 36. exit code

```text
0 = Errorなし
1 = 1件以上Errorあり
```

Warningのみなら0。

Phase 1ではError種別ごとに終了コードを分けない。

---

## 37. stderr

個別Errorはプレーンテキストで標準エラーへ出す。

例:

```text
ERROR: S.json: Required field is missing: vedic_sidereal_lahiri.utcIso
```

厳密なメッセージ書式は公開仕様として固定しない。

---

# LegacyChartValidator

## 38. 役割

既存ホロスコープJSONとの整合性確認を行う一時的な検証機能。

DashaCalculator本体とは明確に分離する。

概念:

```text
CLI / Application
├─ InputLoader
├─ DashaCalculator
├─ LegacyChartValidator
└─ OutputBuilder
```

Legacy検証結果がDasha計算値を修正・補正してはならない。

将来LegacyChartValidatorを削除する場合、その内部必須チェック・日本語テーブルもまとめて削除できること。

---

## 39. Legacy必須フィールド

LegacyChartValidatorが存在するPhase 1では、少なくとも以下を必須とする。

```text
vedic_sidereal_lahiri.ayanamshaDeg
vedic_sidereal_lahiri.nakshatras.Moon
western_tropical_placidus.lons.Moon
```

`nakshatras.Moon` 内では比較対象:

```text
nakIdx
nakName
pada
ruler
```

これらが欠落した場合はError。

DashaCalculator本体の必須フィールドとは別管理にする。

---

## 40. Legacy Moon Nakshatra validation

本ツールがMoon sidereal longitudeから再計算した以下を既存JSONと比較する。

```text
nakIdx
nakName
pada
ruler
```

既存 `nakIdx` は0始まり。

新出力 `number` は1始まりなので:

```text
expected legacy nakIdx = number - 1
```

比較は完全一致。

1フィールドの不一致につき1 Warning。

---

## 41. Legacy nakName 日本語正規テーブル

この表をLegacy検証の正規値とする。

| nakIdx | number | English | Legacy nakName |
|---:|---:|---|---|
| 0 | 1 | Ashvinī | アシュヴィニー |
| 1 | 2 | Bharanī | バラニー |
| 2 | 3 | Kṛttikā | クリッティカー |
| 3 | 4 | Rohinī | ローヒニー |
| 4 | 5 | Mrigashīra | ムリガシラ |
| 5 | 6 | Ārdrā | アールドラー |
| 6 | 7 | Punarvasu | プナルヴァス |
| 7 | 8 | Pushya | プシュヤ |
| 8 | 9 | Ashlesha | アシュレーシャー |
| 9 | 10 | Maghā | マガー |
| 10 | 11 | Pūrva Phalgunī | プールヴァファルグニー |
| 11 | 12 | Uttara Phalgunī | ウッタラファルグニー |
| 12 | 13 | Hasta | ハスタ |
| 13 | 14 | Chitrā | チトラー |
| 14 | 15 | Svātī | スワーティー |
| 15 | 16 | Vishākhā | ヴィシャーカー |
| 16 | 17 | Anurādhā | アヌラーダー |
| 17 | 18 | Jyeshtha | ジェーシュタ |
| 18 | 19 | Mūla | ムーラ |
| 19 | 20 | Pūrva Ashādhā | プールヴァアシャーダー |
| 20 | 21 | Uttara Ashādhā | ウッタラアシャーダー |
| 21 | 22 | Shravana | シュラヴァナ |
| 22 | 23 | Dhanistha | ダニシュタ |
| 23 | 24 | Shatabhisha | シャタビシャー |
| 24 | 25 | Pūrva Bhādrapadā | プールヴァバドラパダー |
| 25 | 26 | Uttara Bhādrapadā | ウッタラバドラパダー |
| 26 | 27 | Revatī | レーヴァティー |

実装上、このテーブルを:

- LegacyChartValidator内部定数
- Legacy専用設定JSON

のどちらに保持してもよい。

別JSONへ切り出す場合も、この表と完全一致させる。

---

## 42. Legacy Ayanamsha validation

使用値:

```text
western_tropical_placidus.lons.Moon
vedic_sidereal_lahiri.siderealLons.Moon
vedic_sidereal_lahiri.ayanamshaDeg
```

期待値を概念的に:

```text
expectedAyanamsha
  = normalize360(tropicalMoonLongitude - siderealMoonLongitude)
```

として求める。

既存 `ayanamshaDeg` と角距離で比較する。

許容誤差:

```text
0.000001°
```

不一致ならWarning。

---

## 43. Warning出力

Warningがある場合のみトップレベル `warnings` を出力する。

例:

```json
"warnings": [
  {
    "code": "LEGACY_NAKSHATRA_MISMATCH",
    "field": "vedic_sidereal_lahiri.nakshatras.Moon.ruler",
    "expected": "Sun",
    "actual": "Moon"
  }
]
```

フィールド:

```text
code
field
expected
actual
```

`expected`: 本ツールの再計算値。

`actual`: 参照JSON値。

Ayanamsha例:

```json
{
  "code": "LEGACY_AYANAMSHA_MISMATCH",
  "field": "vedic_sidereal_lahiri.ayanamshaDeg",
  "expected": 24.123456789,
  "actual": 24.1234
}
```

Warningがなければ `warnings` 自体を省略する。

Warning数値は比較に使用した内部数値を、判定精度を失わない形で出力する。

表示用の任意丸めはしない。

---

# JSON利用上の範囲

## 44. JSONが保証するDasha範囲

出力JSONは:

```text
meta.outputPeriod
meta.depth
```

の範囲についてのみDasha結果を保証する。

JSONに存在しない期間・階層が必要なら本ツールで再出力する。

AI等に読ませる場合も:

```text
JSONに存在しない期間・階層のDashaを独自に再計算せず、
必要ならDasha JSONを再生成する
```

という利用ルールを与えることを推奨する。

`birth` 情報から理論上再計算可能なので、JSON構造だけで独自再計算を完全禁止することはできない。

---

# PG検証

## 45. 計算ロジック検証

少なくとも以下を確認する。

```text
各親Dashaが9子Dashaへ正しく分割される
最初のchild.startDate == parent.startDate
最後のchild.endDate == parent.endDate
前child.endDate == 次child.startDate
隙間がない
重複がない
Vimshottari lord順が正しい
childDuration比率が lordYears / 120
出生時Mahadasha start/end が fractionCompleted から再計算される
既存Dasha計算済み値を入力にしていない
```

---

## 46. 出力ロジック検証

少なくとも以下を確認する。

```text
depth
period
referenceDateTime
default outputPeriod
supportedWindowとの共通部分
overlap filtering
nested tree filtering
children省略
値なしフィールド省略
固定UTC offset
ミリ秒未満切り捨て
```

出力されていない期間を「計算失敗」とみなさない。

---

# Codex実装完了時の確認事項

実装完了時、最低限以下が仕様どおりであることを確認する。

- 既存JSONのDasha計算済みフィールドをDashaCalculatorが参照していない。
- Moon sidereal longitudeからNakshatra、Pada、lord、`fractionCompleted`を再計算している。
- Nakshatra lord sequenceが本仕様どおり。
- 出生時Mahadasha開始境界を `fractionCompleted` から再計算している。
- 下位Dashaが親lordから始まり9分割される。
- 1年 = 365.25日で、Gregorian calendar year加算を使っていない。
- 明示 `period = [birthDateTime, cycleEnd)` でdepth 1～2のデフォルトと同じ範囲を再現できる。
- depth 3～5のデフォルト `outputPeriod` が対象上位Dasha期間と `supportedWindow` の共通部分。
- period filteringでDashaノード日時をクリップしていない。
- nested treeで各階層のoverlapノードだけを出力している。
- JSON日時が出生チャート固定offset、ミリ秒3桁、ミリ秒未満切り捨て。
- `fractionCompleted` の12桁切り捨ては出力時のみ。
- Legacy不一致はWarning、Legacy必須フィールド欠落はError。
- WarningファイルをSuccessに含めていない。
- Error時に古い `*_dasha.json` が残らない。
- batchで1件Errorでも他ファイル処理を継続する。

---

# TODO

現時点でPhase 1実装開始に必要な仕様上の未決事項なし。

実装・検証の結果、既存JSONとの数値差分、Legacy日本語表記差異、または日時境界差異が確認された場合のみ、その差分を材料に仕様を再検討する。
