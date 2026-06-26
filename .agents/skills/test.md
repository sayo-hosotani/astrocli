# Test Skill

## Purpose

プロジェクトをテストする。

## Current Command

```text
dotnet test AstroCli.slnx
```

## Notes

- VSTestはローカルソケットを作成するため、サンドボックス環境では権限エラーになる場合がある。その場合はwritable rootsと `dotnet` のprefix ruleを確認する。
- テストでは、固定日時 `"1989-07-08 05:19:00 +09:00"` と固定位置 `"35°41’22″N,139°41’30″E"` のJSON出力、Astronomy Engineとの比較、位置情報パース、不正入力のエラー動作を確認する。

## Documentation Check

ドキュメント運用を変更したときは、少なくとも次を確認する。

1. `rg --files` で必要なファイルが存在すること。
2. `test ! -e plan.md` で、`plan.md` が存在しないことを確認する。
3. `rg "plan.md" --hidden -g '!.git'` で、`plan.md` への参照が「使用しない」というルール、goalの達成条件、または議事録上の履歴だけであることを確認する。
4. `goal.md` に検討経緯が含まれていないことを確認する。

## Update Rule

テスト可能なコードやツールチェーンを追加したときは、このskillにテストコマンドと確認観点を追記する。
