# Build Skill

## Purpose

プロジェクトをビルドする。

## Current Command

```text
DOTNET_CLI_HOME=/home/ubuntu/astrocli/.dotnet-home dotnet build tests/AstroCli.Tests/AstroCli.Tests.csproj
```

## Notes

- `DOTNET_CLI_HOME` は、.NET CLIがホームディレクトリ配下にキャッシュを書こうとして失敗する環境を避けるため、ワークスペース内に向ける。
- テストプロジェクトをビルドすると、参照しているCLIプロジェクトもビルドされる。
- 現時点ではSolutionファイルを使用しない。

## Checkpoints

- ビルドが終了コード0で完了すること。
- `src/AstroCli/bin/Debug/net10.0/astrocli` が生成されること。

## Update Rule

ビルド可能なコードやツールチェーンを追加したときは、このskillにビルドコマンドと確認観点を追記する。
