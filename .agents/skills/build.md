# Build Skill

## Purpose

プロジェクトをビルドする。

## Current Command

```text
dotnet build AstroCli.slnx
```

## Notes

- `.slnx` をビルドすると、CLIプロジェクトとテストプロジェクトの両方がビルドされる。
- サンドボックス環境でホームディレクトリ配下への書き込みやVSTestのソケット作成が失敗する場合は、writable rootsと `dotnet` のprefix ruleを確認する。

## Checkpoints

- ビルドが終了コード0で完了すること。
- `src/AstroCli/bin/Debug/net10.0/astrocli` が生成されること。

## Update Rule

ビルド可能なコードやツールチェーンを追加したときは、このskillにビルドコマンドと確認観点を追記する。
