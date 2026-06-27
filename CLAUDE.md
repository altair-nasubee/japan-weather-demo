# プロジェクト環境ルール

## 実行環境

- コマンドは Windows 上で実行される（PowerShell、または Git Bash）。
- リポジトリルート: `C:\work\japan-weather-demo`
- Unity プロジェクト: `C:\work\japan-weather-demo\UnityProject`（High Definition 3D / Unity 6000.3.18f1 LTS）
- シェルは PowerShell を優先する。Bash ツールも利用可能（Git for Windows 同梱）。

## ワークフロー規約

- 変更は小さくまとめ、各変更ごとに git commit する。
- コミットメッセージは日本語で書く。
- 推測で実装せず、エラー内容を確認してから修正する。

## ドキュメント

- `docs/` 以下にガイドや設計資料を置く。
- Markdown ファイルの更新後は対応するセクションを必ず確認すること。
