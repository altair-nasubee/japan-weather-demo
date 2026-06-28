# プロジェクト環境ルール

## 実行環境

- コマンドは Windows 上で実行される（PowerShell、または Git Bash）。
- リポジトリルート: `C:\work\japan-weather-demo`
- Unity プロジェクト: `C:\work\japan-weather-demo\UnityProject`（High Definition 3D / Unity 6000.3.18f1 LTS）
- シェルは PowerShell を優先する。Bash ツールも利用可能（Git for Windows 同梱）。

## ワークフロー規約

- 変更は小さくまとめ、各変更ごとに git commit する。
- コミットメッセージは英語で書く。
- 推測で実装・設定変更しない。原因が未確定なら、まず切り分けテストや最新の一次情報（公式ドキュメント・Web 検索・`--help` 等）で原因を特定し、根拠が揃ってから最小限の変更だけ行う。当て推量の変更を繰り返さない。
- ライブラリ・OS・ツールの仕様は記憶で答えず、最新情報を確認する（特に Windows の設定 UI など、バージョンで変わるもの）。

## ドキュメント

- `docs/` 以下にガイドや設計資料を置く。
- Markdown ファイルの更新後は対応するセクションを必ず確認すること。
