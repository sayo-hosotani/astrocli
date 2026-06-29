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
- 初期MVPでは、西洋占星術のネイタル固定で、指定日時・指定位置の主要10天体、True Node、小惑星、ASC（上昇点）の位置をJSONで標準出力に出す。
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
- 天文情報の取得には `SharpAstrology.SwissEph` のMoshierモードを使用する。
- Moshierモードでは、Swiss Ephemeris `.se1` ファイル、JPL `.eph` ファイル、C由来のネイティブDLLは使用しない。
- `SharpAstrology.SwissEph` はAGPL-3.0であり、このライセンス制約を承知のうえで使用する。
- 占星術ライブラリを使うか、天文ライブラリから占星術出力を組み立てるかは、精度だけでなく設計思想、ライセンス、実装主権のトレードオフとして判断する。
- JPL DEやSwiss Ephemerisのような高精度暦は便利だが、出力の意味、座標系、時刻系、ハウス計算などの設計判断を明示する必要がある。
- 初期MVPでは、太陽、月、水星、金星、火星、木星、土星、天王星、海王星、冥王星、小惑星の位置はtopocentricではなく地心基準で計算する。位置情報はASCや将来のハウス計算など、観測地点の地平線・子午線が関係する要素に使う。
- 小惑星はJPL Horizonsから太陽中心EQJ状態ベクトルを取得し、`CosineKitty.AstronomyEngine` の `GravitySimulator` と座標変換で地心黄経を算出する。
- 通常チャート出力で対応する小惑星は、キロン、セレス、パラス、ジュノー、ベスタ。
- 小惑星計算の想定日時範囲は、おおむね1950年から2100年。

## Initial MVP

初期MVPでは、西洋占星術のネイタル固定で、指定日時・指定位置の主要10天体、True Node、小惑星、ASC（上昇点）の位置をJSONで標準出力に出す。

ビルド:

```text
dotnet build AstroCli.slnx
```

テスト:

```text
dotnet test AstroCli.slnx
```

実行例:

```text
dotnet build src/AstroCli/AstroCli.csproj
src/AstroCli/bin/Debug/net10.0/astrocli "1989-07-08 05:19:00 +09:00" "35°41’22″N,139°41’30″E"
```

位置情報は `"latitude,longitude"` の1引数で指定する。緯度は `度°分’秒″N/S`、経度は `度°分’秒″E/W` の60進法で指定する。

出力には、入力日時、UTC換算日時、占術体系、チャート種別、位置情報、ASC、プラシーダスの12ハウスカスプ、主要10天体、True Nodeのノースノード/サウスノード、小惑星の黄経度数、星座、星座内度数を含める。天体位置、ノード、ASC、ハウスカスプは `SharpAstrology.SwissEph` のMoshierモードで計算する。小惑星はJPL Horizonsから取得した状態ベクトルを `CosineKitty.AstronomyEngine` で地心黄経へ変換して計算する。

度数は60進数文字列で出力する。

例:

```json
{
  "location": {
    "latitude": "35°41’22″N",
    "longitude": "139°41’30″E"
  },
  "ascendant": {
    "name": "ascendant",
    "eclipticLongitude": "114°29’59″",
    "sign": "Cancer",
    "degreeInSign": "24°29’59″"
  },
  "houses": {
    "system": "placidus",
    "cusps": {
      "house1": {
        "name": "house1",
        "eclipticLongitude": "114°29’59″",
        "sign": "Cancer",
        "degreeInSign": "24°29’59″"
      },
      "house10": {
        "name": "house10",
        "eclipticLongitude": "11°06’51″",
        "sign": "Aries",
        "degreeInSign": "11°06’51″"
      }
    }
  },
  "bodies": {
    "sun": {
      "name": "sun",
      "eclipticLongitude": "105°40’31″",
      "sign": "Cancer",
      "degreeInSign": "15°40’31″"
    },
    "pluto": {
      "name": "pluto",
      "eclipticLongitude": "222°25’53″",
      "sign": "Scorpio",
      "degreeInSign": "12°25’53″"
    },
    "northNode": {
      "name": "northNode",
      "eclipticLongitude": "326°24’19″",
      "sign": "Aquarius",
      "degreeInSign": "26°24’19″"
    },
    "southNode": {
      "name": "southNode",
      "eclipticLongitude": "146°24’19″",
      "sign": "Leo",
      "degreeInSign": "26°24’19″"
    },
    "chiron": {
      "name": "chiron",
      "eclipticLongitude": "95°12’34″",
      "sign": "Cancer",
      "degreeInSign": "5°12’34″"
    },
    "ceres": {
      "name": "ceres",
      "eclipticLongitude": "123°45’56″",
      "sign": "Leo",
      "degreeInSign": "3°45’56″"
    }
  }
}
```

## Asteroid Tool

既知小惑星は `asteroid` サブコマンドでJPL Horizons APIから状態ベクトルを取得する。

出力は `CosineKitty.AstronomyEngine` の `GravitySimulator` に渡すための値だけにする。太陽中心、EQJ、位置AU、速度AU/dayの状態ベクトルをJSONで出力する。

対象小惑星は、キロン、セレス、パラス、ジュノー、ベスタに固定する。`asteroid` サブコマンドは、指定日時について、この5件を常にまとめて取得する。

| 小惑星 | 英名 | Horizons command |
| --- | --- | --- |
| キロン | chiron | `2060;` |
| セレス | ceres | `1;` |
| パラス | pallas | `2;` |
| ジュノー | juno | `3;` |
| ベスタ | vesta | `4;` |

標準出力にJSONを出す例:

```text
src/AstroCli/bin/Debug/net10.0/astrocli asteroid --at "2026-06-29 22:00:00 +09:00"
```

ファイルにJSONを出す例:

```text
src/AstroCli/bin/Debug/net10.0/astrocli asteroid --at "2026-06-29 22:00:00 +09:00" --output result.json
```

出力例:

```json
{
  "inputDateTime": "2026-06-29 22:00:00 +09:00",
  "utcDateTime": "2026-06-29T13:00:00Z",
  "asteroids": [
    {
      "id": "キロン",
      "horizonsCommand": "2060;",
      "stateVector": {
        "epoch": "2026-06-29T13:00:00Z",
        "origin": "sun",
        "frame": "EQJ",
        "positionUnit": "AU",
        "velocityUnit": "AU/day",
        "x": 1.0,
        "y": 2.0,
        "z": 3.0,
        "vx": 0.001,
        "vy": 0.002,
        "vz": 0.003
      }
    }
  ]
}
```

## Project Operation

- `goal.md` は現在の作業対象を表す。
- `goal.md` には、Codexが作業を進めるために必要な目的、要件、達成条件だけを書く。
- 検討経緯や意思決定の背景は議事録に残す。
- 恒久的に守る重要事項はrulesに残す。
- 繰り返し使う作業手順はskillsに残す。
- `plan.md` は使用しない。
