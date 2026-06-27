# Unity × Claude Code（Windowsネイティブ構成）開発環境 セットアップガイド

Windows 11 上に **Unity 6.3 LTS** と **Windows版 Claude Code** をインストールし、両方をWindows内で動かして Unity アプリを開発するためのガイド。WSL版 Claude Code は削除せず、Python / Next.js 等の別スタック用に併存させる。

> **設計方針:** Unity と unity-mcp は Windows 上で動き、プロジェクト実体も `C:\`（Windows のファイルシステム）に置く。**Claude Code も Windows 側に置く**ことで、Windows と WSL の境界を跨がずに済み、高速・単純・安定になる。WSL 版 Claude Code から `/mnt/c` 越しに同じプロジェクトを扱うと、I/O 遅延とファイル変更検知（inotify）の不達という不利を常に負うため、本ガイドでは Claude Code も Windows 版を使う。

---

## 0. 概要・前提

### 構成イメージ

```mermaid
flowchart TB
    subgraph WIN["Windows 11"]
        Hub["Unity Hub"]
        Editor["Unity 6.3 LTS Editor<br/>Console / PlayMode"]
        CC["Claude Code<br/>(Windows native, PowerShell)"]
        Proj["C:\\work\\<Project><br/>← プロジェクト実体"]

        Hub --> Editor
        CC -- "named pipe / TCP<br/>(unity-mcp bridge)" --> Editor
        CC -- "同一FS・パス変換なし" --> Proj
        Editor -. "読み書き" .-> Proj
    end

    subgraph WSL["WSL2（別スタック用・併存）"]
        CCWSL["Claude Code → Python / Next.js<br/>※このプロジェクトでは使わない"]
    end
```



### 前提条件


| 項目                   | 状態・要件                                                                           |
| -------------------- | ------------------------------------------------------------------------------- |
| OS                   | Windows 11                                                                      |
| Unity アカウント          | 取得済み（無料の Personal でも可）                                                          |
| Claude Code サブスク     | Pro / Max / Team / Enterprise のいずれか（無料 Claude.ai プランは不可）                        |
| Git for Windows      | **必須**（git / clone / LFS / ssh。§2 で導入）                                          |
| **作業用 GitHub リポジトリ** | **Web で作成済み**であること（§4 で Clone する。空リポジトリでも可）                                     |
| Python（任意）           | statusLine（`statusline.py`、§6）を使う場合のみ必要。`winget install Python.Python.3.12`     |
| Node.js（任意）          | playwright MCP（§6）や npm 版 CC（§5 代替）を使う場合のみ必要。`winget install OpenJS.NodeJS.LTS` |
| WSL2 + WSL版CC        | 既存のまま残す（他スタック用。本ガイドでは触らない）                                                      |


> **⚠ 安全上の最重要注意 — `claude` を起動する場所:** `claude` を対話起動すると、**起動時のカレントフォルダがそのまま作業ディレクトリ**になる。`C:\Windows\System32` などシステムフォルダで起動すると、Claude Code が**システムファイルを読み書きしてしまう恐れ**がある。`**claude` は必ず作業用リポジトリのフォルダ内で起動する。** 本ガイドは「§4 でリポジトリを Clone → §6 でそのフォルダ内から `claude` を起動」という順序にしてある（§5 ではインストールのみで起動はしない）。

> **実行メモ（このガイドの進め方）:** これは人間が上から順に実施する手順書。PowerShell / git / ssh のシェル手順は Windows 版 Claude Code に実行させてもよいが、**【手動】マークの付いたステップは必ず人間が手で行う**（CC には実行できない）。手動になるのは次のいずれか:
>
> - **GUI 操作**（Unity Hub・Unity Editor・インストーラ）
> - **CC の対話操作**（`/login`・`/plugin` などスラッシュコマンド、TUI 内の選択）
> - **管理者権限が必要**な操作（CC のシェルは昇格しない）
> - **ブートストラップ**（CC 本体を入れる §5 自体。CC が使えるようになる前の手順）
> - **外部 Web 操作**（GitHub でのリポジトリ作成・SSH 公開鍵の登録等）

### 全体の流れ

```mermaid
flowchart TD
    G["GitHub Web で作業用リポジトリを作成【手動】"] --> S1["§1 Unity 導入【手動】※独立・後回し可"]
    S1 --> S2["§2 Git for Windows 導入"]
    S2 --> S3["§3 WSL→Windows へ Git/SSH 設定を移行"]
    S3 --> S4["§4 作業フォルダへリポジトリを Clone"]
    S4 --> S5["§5 Claude Code 本体を導入（起動はまだしない）"]
    S5 --> S6["§6 Clone フォルダ内で claude を起動・ログイン・設定移行"]
    S6 --> S7["§7〜 CLAUDE.md / unity-mcp / 開発"]
```



### なぜ WSL版でなく Windows版 CC か


| 観点                     | WSL版CC（`/mnt/c`経由）           | Windows版CC（ネイティブ） |
| ---------------------- | ---------------------------- | ----------------- |
| ファイルI/O・検索             | `/mnt/c` 越えで遅い（Unityは大量ファイル） | `C:\` をネイティブ参照で高速 |
| ファイル変更監視（inotify）      | Windows側変更がWSLに伝播しない（既知の制約）  | ネイティブで正常          |
| Unity / unity-mcp との距離 | WSL↔Windows境界を跨ぐ             | 同一OS内で完結・単純       |
| パス変換                   | `/mnt/c/...`↔`C:\...` 常時必要   | 不要                |


---

## 1. Windows側：Unity のインストール 【手動】

> この章は **全ステップが Unity Hub の GUI 操作**のため手動。**他のセクションから独立**しており、Unity 側の障害時などは後回しにしてよい（§2〜§6 の CC 環境構築は Unity なしで進められる）。

1-1. **Unity Hub を取得** — [unity.com/download](https://unity.com/download) から Unity Hub を入手してインストール。
1-2. **サインインとライセンス有効化** — 起動して Unity アカウントでサインイン。`Preferences → Licenses` で Personal 等を有効化。
1-3. **Editor のインストール** — `Installs` タブ →「Install Editor」→ **Unity 6.3 LTS** を選択。
1-4. **モジュール選択** — ターゲットに応じて追加。
  - **Windows Build Support (IL2CPP/Mono)** … PC 向けビルドに必要。
  - 必要に応じて **Android Build Support** / **WebGL Build Support** 等。
  - **Visual Studio は任意** — C# は Claude Code で編集するため必須ではない（Unity側のIDE連携が欲しい場合のみ）。
1-5. **インストール中は Hub を閉じない** — 閉じるとダウンロードがキャンセルされる。完了すると `Installs` タブに表示。

> 参考: [Unity 6 リリース](https://unity.com/releases/unity-6) / [Install Unity（マニュアル）](https://docs.unity3d.com/6000.3/Documentation/Manual/GettingStartedInstallingUnity.html) / [Install the Unity Hub](https://docs.unity.com/en-us/hub/install-hub)

---

## 2. Git for Windows の導入

`git` / `git clone` / Git LFS / `ssh` に必須。[git-scm.com](https://git-scm.com/download/win) から導入する。`git` 本体・`git-lfs`・**Git Credential Manager**・`ssh` を同梱しており、§3（SSH/git 設定）と §4（`git clone`）はこれに依存する。**Windows 標準にもネイティブ CC インストーラにも `git` は含まれない**ため、ここを省くと後続で詰まる。

導入後に確認:

```powershell
git --version
git lfs version
```

- 補足: Claude Code が **Bash ツール**を使えるのは「追加の利点」。これとは別に、上記 git ツールチェーン自体が必須。Git for Windows 未導入時は CC のシェルが PowerShell になる（v2.1.139 以降はネイティブ PowerShell ツールが標準）。
- `ssh-agent`（§3）に使う **Windows OpenSSH Client は Windows 11 標準**で導入済み。`ssh -V` で確認できる。
- **導入後は PowerShell を再起動**する（既存セッションには `git` が PATH 反映されない）。

---

## 3. WSL → Windows への Git / SSH 設定の移行

これまで Git 認証・SSH 鍵は WSL 側にしか無かったため、Windows 側で `git clone` / `push` できるよう移行する。**最大の落とし穴は SSH 秘密鍵のパーミッション（ACL）** で、ここを正しくしないと Windows OpenSSH が鍵を拒否する。

### コピー対象（このマシンの実態に合わせた一覧）


| 対象         | コピー元（WSL）               | コピー先（Windows）                       | 注意                                                     |
| ---------- | ----------------------- | ----------------------------------- | ------------------------------------------------------ |
| Git ユーザー設定 | `~/.gitconfig`          | `%USERPROFILE%\.gitconfig`     | name/email は移行。**LFS 行は `git lfs install` で再生成**するのが確実 |
| SSH 秘密鍵    | `~/.ssh/id_ed25519`     | `%USERPROFILE%\.ssh\id_ed25519`     | **ACL 設定が必須**（後述）                                      |
| SSH 公開鍵    | `~/.ssh/id_ed25519.pub` | `%USERPROFILE%\.ssh\id_ed25519.pub` | そのままコピー                                                |
| 既知ホスト      | `~/.ssh/known_hosts`    | `%USERPROFILE%\.ssh\known_hosts`    | 任意（ホスト再検証を省ける）                                         |
| GPG 署名鍵    | （未使用）                   | —                                   | 現状 GPG 署名なしのため対象外                                      |


> **Windows のユーザー名は WSL の `defaultuser` と異なる場合がある。** 実パスは PowerShell で `echo $env:USERPROFILE`（例 `C:\Users\altair`）を確認して読み替えること。

### 手順

**3-1. 鍵ファイルをコピー（WSL のターミナルから）**

WSL からは Windows のホームが `/mnt/c/Users/<WinUser>/` で見える。`<WinUser>` を実際の名前に置換して実行:

```bash
mkdir -p /mnt/c/Users/<WinUser>/.ssh
cp ~/.ssh/id_ed25519 ~/.ssh/id_ed25519.pub ~/.ssh/known_hosts /mnt/c/Users/<WinUser>/.ssh/
```

> これは **WSL の bash** で実行する。Windows 版 Claude Code（PowerShell）から実行させたい場合は `wsl` 経由で呼ぶ:
> `wsl bash -lc "mkdir -p /mnt/c/Users/<WinUser>/.ssh && cp ~/.ssh/id_ed25519 ~/.ssh/id_ed25519.pub ~/.ssh/known_hosts /mnt/c/Users/<WinUser>/.ssh/"`

**3-2. 秘密鍵の ACL を本人のみに制限（PowerShell）** ← 最重要

これを怠ると `Permissions for '...id_ed25519' are too open ... This private key will be ignored.`（UNPROTECTED PRIVATE KEY FILE）で拒否される。`ssh-add` も同じチェックを行うため、**必ず先に実行**する。

```powershell
icacls "$env:USERPROFILE\.ssh\id_ed25519" /inheritance:r
icacls "$env:USERPROFILE\.ssh\id_ed25519" /grant:r "$($env:USERNAME):F"
```

- `/inheritance:r` … フォルダから継承した広い権限を断ち切る。
- `/grant:r "...:F"` … 自分のアカウントだけに Full control を付与（他ユーザー・Everyone を排除）。

**3-3. ssh-agent を有効化して鍵を登録（管理者 PowerShell）** 【手動】（要管理者）

> サービス操作に**管理者昇格**が必要。CC のシェルは昇格しないため、**管理者として開いた PowerShell** で手で実行する。

```powershell
Get-Service ssh-agent | Set-Service -StartupType Automatic
Start-Service ssh-agent
ssh-add "$env:USERPROFILE\.ssh\id_ed25519"
```

**3-4. Git の identity と LFS、改行設定（PowerShell）**

```powershell
git config --global user.name  "Kiyoshi Ishiyama"
git config --global user.email "altair@nasubee.com"
git lfs install                        # LFS 使用プロジェクトのため Windows 側でも必須
git config --global core.autocrlf false  # 改行は .gitattributes に委ねる（アセット破壊を防ぐ）
```

**3-5. 疎通確認**

```powershell
ssh -T git@github.com    # "Hi <user>! You've successfully authenticated..." が出れば OK
```

> **メモ:**
>
> - 秘密鍵はマシン間で持ち回らず、**Windows 用に新しい鍵を生成**して GitHub に公開鍵を登録する運用も安全。その場合は `ssh-keygen -t ed25519 -C "altair@nasubee.com"` を PowerShell で実行し、生成された `.pub` を GitHub の SSH keys に追加する（手順 1・2 のコピーは不要、ACL は ssh-keygen が適切に設定する）。**GitHub への公開鍵登録は Web 操作のため 【手動】。**
> - HTTPS で GitHub を使う場合は Git for Windows 同梱の **Git Credential Manager** が認証を肩代わりするため、SSH 鍵は不要。

---

## 4. 作業用リポジトリを Clone

> **前提:** GitHub の Web ページで**作業用リポジトリを作成済み**であること（【手動】・外部 Web 操作）。空リポジトリでも可。これから新規 Unity プロジェクトを起こす場合は「**4-B 新規作成**」を参照。

### 配置先（共通）

```
C:\work\<ProjectName>
```

- **パスにスペース・日本語を含めない**（ツールの取り回しが楽）。短いパスは Unity の深い自動生成フォルダ（`Library\PackageCache\...`）で Windows の 260 文字制限に当たりにくく、プロフィール外なので **OneDrive 同期にも巻き込まれない**。
- ベースフォルダ `C:\work` は **`C:\` 直下作成時に一度だけ UAC（管理者確認）** が出る。エクスプローラーで作成するか、管理者 PowerShell で `mkdir C:\work`。UAC を避けたい場合は `C:\Users\<user>\work\` でも可（パスは少し長くなる）。
- Unity も Claude Code も**同じネイティブパス**を見る（`/mnt/c` 変換は不要）。

### 4-A. 既存リポジトリを Clone（推奨フロー）

§3 で移行した SSH 鍵・git 設定をそのまま使う（HTTPS の場合は初回に Git Credential Manager が認証）。

```powershell
cd C:\work
git clone git@github.com:<org>/<repo>.git    # SSH。HTTPS なら https://github.com/<org>/<repo>.git
cd <repo>
git lfs pull                                  # LFS 資産を取得（git lfs install 済みなら clone 時に自動取得）
```

- **Unity プロジェクトのリポジトリなら** → 【手動】 Unity Hub →「**Add → Add project from disk**」でクローンしたフォルダを選び、**一致する Editor バージョン（6.3 LTS）で開く**（New Project ではない。Unity Hub の GUI 操作）。バージョンが一致しないと Hub が警告するので、必要なら該当 Editor を追加インストール。
- この `cd <repo>` した**フォルダ内で、§6 へ進んで `claude` を起動する**。

### 4-B. 新規プロジェクトを作成（Clone しない場合）

4-B-1. 【手動】 Unity Hub の `Projects` →「**New Project**」で Unity 6.3 LTS のテンプレート（2D / 3D / Universal 3D(URP) 等）を選び、配置先フォルダに作成（Unity Hub の GUI 操作）。
4-B-2. Git を初期化し、作成済みの GitHub リポジトリへ接続（PowerShell、§3 の git 設定が前提）:
  ```powershell
   cd C:\work\<ProjectName>
   git init
   irm https://raw.githubusercontent.com/github/gitignore/main/Unity.gitignore -OutFile .gitignore
   git remote add origin git@github.com:<org>/<repo>.git
  ```
  - `Library/`・`Temp/`・`Obj/`・`Logs/`・`Build/` 等の自動生成物は必ず除外（巨大かつ再生成可能、CC の走査も軽くなる）。
  - 改行コードは単一OS運用なら問題になりにくいが、将来のチーム共有/WSL併用に備え `.gitattributes` に `* text=auto` を入れておくと無難。

---

## 5. Windows版 Claude Code 本体のインストール 【手動】

> ここでは **CC のバイナリを入れて PATH を通すだけ**。`**claude` の起動・ログインはまだ行わない**（システムフォルダで起動するのを避けるため。起動は §6 で作業フォルダ内から）。

5-1. **PowerShell を開く**（CMD と取り違え注意：プロンプトが `PS C:\...>` ならPowerShell）。**Windows PowerShell 5.1 / PowerShell 7（`pwsh`）どちらでも可**。Claude Code のネイティブ PowerShell ツールは PowerShell 7 を自動検出するため、**7 を入れているなら 7 を使うのが望ましい**（実体は `powershell.exe` ではなく `pwsh.exe`）。
5-2. **ネイティブインストーラを実行**（推奨。Node.js 不要・管理者権限不要・自動更新）:

   ```powershell
   irm https://claude.ai/install.ps1 | iex
   ```

   バイナリは `C:\Users\<user>\.local\bin\claude.exe`（= `~\.local\bin`）に入る。完了後にバージョン確認:

   ```powershell
   claude --version
   ```

5-3. **`claude` を PATH に追加**（インストーラが「`...\.local\bin` is not in your PATH」と表示した場合、または手順2で `claude` が見つからない場合に必要）— `~\.local\bin` を **User PATH に永続追加**する:

   ```powershell
   # User PATH に .local\bin を永続追加（重複時はスキップ）
   $bin = "$env:USERPROFILE\.local\bin"
   $userPath = [Environment]::GetEnvironmentVariable("Path","User")
   if ($userPath -notlike "*$bin*") {
     [Environment]::SetEnvironmentVariable("Path", ($userPath.TrimEnd(';') + ";" + $bin), "User")
   }
   ```

   追加したら **PowerShell を開き直して**（新しいセッションで PATH が反映される）、再確認:

   ```powershell
   claude --version
   ```

   **バージョンが出れば §5 は完了。ログイン（対話起動）は §6 まで行わない。**

### つまずきやすい点


| 症状                       | 対処                                                                     |
| ------------------------ | ---------------------------------------------------------------------- |
| `irm` は認識されない            | CMD にいる。PowerShell を開き直す                                               |
| `'&&' は有効な区切りではない`       | PowerShell にいる（`&&` は CMD 用）。コマンドを分けるか PowerShell 構文で                  |
| インストール後 `claude` が見つからない | `~\.local\bin` が PATH 未登録のことがある。上記の PATH 追加スクリプトを実行し、PowerShell を開き直す  |
| スクリプトがブロックされる            | `Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser` |


> **代替（npm版）:** Node.js 18+（22 LTS推奨）がある環境なら `npm install -g @anthropic-ai/claude-code`。更新は手動（`...@latest`）。特段の理由がなければネイティブインストーラを推奨。
>
> 参考: [Claude Code Advanced setup（公式）](https://code.claude.com/docs/en/setup)

---

## 6. Claude Code を起動（作業フォルダ内）してログイン＆設定移行

> **必ず §4 で用意した作業フォルダ内で `claude` を起動する。** `C:\Windows\System32` などシステムフォルダでは起動しない（冒頭の「⚠ 安全上の最重要注意」を参照）。

### 6-1. 作業フォルダへ移動して起動・ログイン 【手動】（対話操作）

```powershell
cd C:\work\<repo>   # ← §4 でClone/作成した作業フォルダ。System32 等では起動しない
claude
```

初回起動ではセットアップ（テーマ選択など）に続いて**ログイン方法の選択**が表示される。サブスク（Pro / Max / Team / Enterprise）のアカウントで認証する（ブラウザが開く）。すでに起動済みで再認証したい場合は CC 内で `/login` を実行。認証が済んだら `/exit`（または Ctrl+C）で一旦抜けてよい。

認証後、導入状態を診断（PowerShell、作業フォルダ内で）:

```powershell
claude doctor    # 導入種別 / 認証 / PATH / Git健全性を点検
```

### 6-2. 移植する / しない の判断（このマシンの実態）

WSL 側 Claude Code のユーザースコープ設定・プラグイン・MCP は Windows 版へ自動では引き継がれない（§7 のとおり `~/.claude` は環境ごとに別物）。**移植すべきは「設定・拡張」だけで、認証情報や履歴・キャッシュはコピーしない**（機密/マシン固有/パス依存のため）。


| 区分     | 対象                                                                                                         | 扱い                            |
| ------ | ---------------------------------------------------------------------------------------------------------- | ----------------------------- |
| ✅ 移植する | `settings.json`（model=opus / 言語=Japanese / effortLevel=high / voice / theme / statusLine / enabledPlugins） | 内容をコピー（一部パス調整）                |
| ✅ 移植する | `statusline.py`（カスタムステータスライン）                                                                              | コピー。**Windows に Python が必要**  |
| ✅ 移植する | プラグイン **superpowers**（`anthropics/claude-plugins-official`）                                                | **再インストール**（キャッシュはコピーしない）     |
| ✅ 移植する | ユーザー MCP **context7**（HTTP＋APIキー）/ **playwright**（npx）                                                     | `claude mcp add` で**再登録**     |
| ❌ しない  | `.credentials.json`（OAuth トークン）                                                                            | コピー禁止。Windows で**再ログイン**（6-1） |
| ❌ しない  | `~/.claude.json` 全体（machineID / userID / oauthAccount / Linux のプロジェクトパス等）                                  | 丸ごとコピー不可。MCP だけ再登録            |
| ❌ しない  | `projects/` `sessions/` `history.jsonl` `file-history/` `cache/` 等                                         | 履歴・セッション・パス依存。移植不要            |


### 6-3. `settings.json` を移植

`%USERPROFILE%\.claude\settings.json` を作成し、下記を貼り付ける（WSL 版とほぼ同一）:

```json
{
  "model": "opus",
  "language": "Japanese",
  "effortLevel": "high",
  "theme": "dark",
  "voice": { "enabled": true, "mode": "hold" },
  "statusLine": {
    "type": "command",
    "command": "python ~/.claude/statusline.py",
    "refreshInterval": 60
  },
  "enabledPlugins": {
    "superpowers@claude-plugins-official": true
  }
}
```

- **重要:** `statusLine.command` のパスは **`~/.claude/statusline.py` のようにフォワードスラッシュまたは `~` を使う**。Git for Windows がインストールされている環境では、CC がステータスラインコマンドを **Git Bash 経由で実行する**。Git Bash はバックスラッシュをエスケープ文字として消費するため、`C:\\Users\\...` のような Windows パスを書くと**コマンドが無音で失敗しステータスラインが表示されない**。
- `python` が PATH に無ければ絶対パス（フォワードスラッシュ）に置換: `"command": "C:/Python312/python.exe ~/.claude/statusline.py"`

### 6-4. `statusline.py` をコピー＆Windows 対応修正（WSL から）

```bash
cp ~/.claude/statusline.py /mnt/c/Users/<WinUser>/.claude/statusline.py
```

コピー後、**スクリプト冒頭に UTF-8 出力設定を追加する**（必須）。Windows の Python はデフォルトで cp932（Shift-JIS）出力になるため、スクリプト内の絵文字（⚙ ⏳ 等）が `UnicodeEncodeError` を起こしてステータスラインが動かなくなる。

`C:\Users\<WinUser>\.claude\statusline.py` を開き、**先頭2行**をこのように書き換える:

```python
#!/usr/bin/env python3
import json, sys, time, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
```

- ステータスラインには **Windows 版 Python** が必要（未導入なら `winget install Python.Python.3.12` 等）。Python を入れない場合は 6-3 の `statusLine` ブロックを削除すればよい。

### 6-5. プラグイン（superpowers）を再インストール 【手動】

起動中の Claude Code で（**スラッシュコマンドの対話操作**のため手動）:

```
/plugin marketplace add anthropics/claude-plugins-official
/plugin install superpowers@claude-plugins-official
```

6-3 の `enabledPlugins` により有効化される。`find-skills` 等のスキルもここで復元される。

### 6-6. ユーザー MCP を再登録

```powershell
# context7（HTTP・各自の API キーを指定）
claude mcp add --transport http --scope user context7 https://mcp.context7.com/mcp `
  --header "CONTEXT7_API_KEY: <your-context7-api-key>"

# playwright 前準備: Node.js を winget でインストール（未導入の場合）
winget install OpenJS.NodeJS.LTS
# インストール後は PowerShell を開き直して node / npx が認識されることを確認
# node --version  # 例: v24.x.x
# npm --version

# playwright（stdio・npx。Node.js が必要）
claude mcp add --scope user playwright -- npx @playwright/mcp@latest --browser chromium
npx @playwright/mcp@latest install-browser chrome-for-testing
```

> **メモ:**
> - `<your-context7-api-key>` は WSL 側の値を流用してよいが、鍵そのものはこのドキュメントに残さないこと。
> - playwright は Node.js が必要。JS/Python 開発を WSL でやる場合、Windows 側は `winget install OpenJS.NodeJS.LTS` で十分（fnm 等のバージョン管理ツールは不要）。
> - Chromium のインストールは `npx playwright install chromium` **ではなく** `npx @playwright/mcp@latest install-browser chrome-for-testing` を使う。MCP 付属のインストーラを使わないと `@playwright/mcp` が要求するビルド番号と合わず「Browser is not installed」エラーになる。

### 6-7. 動作確認

```powershell
claude mcp list      # context7 / playwright が出るか
claude doctor        # 設定・プラグインの健全性
```

起動中の CC で `/plugin` でプラグイン、ステータスラインの表示も確認する。

---

## 7. CLAUDE.md と「設定共有」戦略

プロジェクト直下に `CLAUDE.md` を置き、CC に環境コンテキストを伝える（Windows/PowerShell 前提）。

```markdown
# プロジェクト環境ルール

## 実行環境
- コマンドは Windows 上で実行される（PowerShell、または Git Bash）。
- プロジェクトの実体は C:\work\<Project>。
- Unity Editor は同じ Windows 上で動いている。

## Unity ファイルの扱い
- Library/ Temp/ obj/ Logs/ Build/ は自動生成物。編集・コミットしない。
- *.meta ファイルは削除・改名しない（アセット参照が壊れる）。
- C# スクリプトの追加・変更後は Unity 側のコンパイルが必要。

## ワークフロー規約
- 変更は小さくまとめ、各変更ごとに git commit する。
- コンパイル結果・エラーは unity-mcp 経由で Unity のコンソールから取得して確認する。
- 推測で書かず、コンソールのエラー内容に基づいて修正する。
```

### 設定の共有について（重要）

Windows版CC と WSL版CC は `~/.claude`（= `C:\Users\<user>\.claude` と WSL の `/home/<user>/.claude`）が**別物**で、会話履歴・undo履歴・memory・コマンド履歴は**環境ごとに独立**する。プロジェクトの識別キー（slug）もパス差で別になるため、履歴の突き合わせはできない。

→ **両環境で揃えたい設定は、リポジトリ内にコミットして共有する**のが唯一クリーンな方法:


| 共有できる（repoに入れる）                                                                         | 共有されない（環境ごと独立）                                                                     |
| --------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------- |
| `CLAUDE.md`、`.claude/commands/`、`.claude/agents/`、project スコープの `.claude/settings.json` | 会話 transcript、undo/file-history、`history.jsonl`、memory、user スコープの skills/MCP/プラグイン |


なお**コード（実ファイル）そのものは git 経由で当然共有される**。同期したいのは「CC の設定・運用ルール」であり、それは上記の repo コミットで担保する。

---

## 8. unity-mcp（CoplayDev）の導入と接続

CC が Unity のコンソール・コンパイル結果を直接読めるようにする。**Node.js 等の追加ランタイム不要。** Windows 内ローカル接続なので構成は単純。

> **§8-0〜§8-2 は Unity Editor の GUI 操作のため 【手動】**。§8-3 以降は PowerShell / CC で実行。

### 8-0. 前提：Unity プロジェクトの準備 【手動】

unity-mcp は **Unity Editor でプロジェクトが開かれている状態**で機能する。

**フォルダ構成の方針（推奨）**

git リポジトリのルートに `CLAUDE.md`・`docs/` 等が既にある場合、Unity プロジェクトはサブフォルダに分けるのが整理しやすい:

```
C:\work\<repo>\
├── CLAUDE.md
├── docs/
├── .gitignore          ← リポジトリ全体の除外（Windows OS ファイル等）
└── UnityProject\       ← Unity Hub の「保存場所」に <repo>、「プロジェクト名」に UnityProject を指定
    ├── .gitignore      ← Unity 固有の除外（Library/ Temp/ 等）
    ├── Assets\
    ├── Packages\
    └── ProjectSettings\
```

**8-0-1. `.gitignore` を準備する（PowerShell）**

リポジトリルートの `.gitignore` に Unity 固有パターン（`/[Ll]ibrary/` 等）を書いても、Unity がサブフォルダにある場合は**効かない**。Unity サブフォルダ直下に専用 `.gitignore` を作成する必要がある。

```powershell
# Unity プロジェクトのサブフォルダ内に Unity 用 .gitignore を作成
irm https://raw.githubusercontent.com/github/gitignore/main/Unity.gitignore -OutFile UnityProject\.gitignore
```

リポジトリルートの `.gitignore` には Windows OS ファイルと Cursor エディタキャッシュを追加しておく:

```
# Windows OS files
Thumbs.db
ehthumbs.db
Desktop.ini
$RECYCLE.BIN/

# Cursor editor cache
.cursor/
```

**8-0-2. Unity プロジェクトを作成する【手動】**

- **新規プロジェクトを作成する場合** — Unity Hub の `Projects →「New Project」` を開く。
  - エディターバージョン: **6000.3.x LTS**
  - テンプレート: **High Definition 3D**（レイトレーシング・ボリューメトリッククラウド等に対応した HDRP テンプレート）
  - プロジェクト名: `UnityProject`（任意）
  - 保存場所: `C:\work\<repo>`（リポジトリルート）
  - → 「プロジェクトを作成」後、`<repo>\UnityProject\` に Unity 一式が生成される。

- **既存の Unity プロジェクトを Clone 済みの場合** — Unity Hub の `Projects →「Add → Add project from disk」` で該当フォルダを追加し、対応する Editor バージョンで開く。

**8-0-3. HDRP Wizard への対応【手動】**

High Definition 3D テンプレートで新規プロジェクトを作成すると、Unity 起動時に **HDRP Wizard** が自動表示される。各セクションの対応は以下のとおり。

| セクション | 状態 | 対応 |
| -------- | ---- | ---- |
| **HDRP**（Global / Current Quality） | 全項目グリーン ✅ | 何もしない |
| **VR** | XR パッケージ未インストールのエラー ❌ | **Fix しない**（VR 非対応アプリのため不要） |
| **DXR**（レイトレーシング） | Graphics Settings はグリーン、Asset 設定に黄色警告 ⚠️ | 今は無視。天候エフェクト実装フェーズで有効化する |
| **Project Migration Quick-links** | Built-in マテリアルに関する警告 | **「Convert All Built-in Materials to HDRP」をクリック** |

操作後、ウィザードを閉じる。右上の「Show on start」チェックを外すと次回起動時に自動表示されなくなる（任意）。

> **DXR 警告の補足:** 「Screen Space Shadows / Reflection / Visual Effects Ray Tracing が HDRP High Fidelity プリセットで無効」という警告。レイトレーシングを使う際は HDRP Asset（`Assets/Settings/HDRPDefaultResources/HDRenderPipelineAsset.asset`）で該当機能を有効化する。順序は「DXR Activated → Screen Space Shadows → Screen Space Reflection → Visual Effects Ray Tracing」の依存関係があるため、上から順に有効化すること。

**8-0-4. `.gitignore` の動作確認（PowerShell）**

Unity Editor が初回コンパイルを終えたら、ドライランで実際にステージされるファイルを確認する:

```powershell
git add UnityProject/ --dry-run
```

出力に `Library/`・`Temp/`・`Logs/`・`UserSettings/`・`*.csproj`・`*.slnx` が**出ていなければ** `.gitignore` が正しく効いている。出ていた場合は `UnityProject\.gitignore` が存在するか確認する。

追跡対象として想定されるのは `Assets/`・`Packages/`・`ProjectSettings/`・`UnityProject/.gitignore`・`UnityProject/.vsconfig` のみ。

### 8-1 〜 8-5. unity-mcp のインストールと接続

8-1. 【手動】 **Unity 側にパッケージ追加** — `Window → Package Manager → +（左上）→ Add package from git URL` に貼り付け:
  ```
   https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main
  ```
  インストール完了後に **MCP for Unity Setup** ダイアログが自動表示される。2段階で進む:

  **① System Requirements の確認**
  不足しているものを winget でインストールし、「Refresh」で再確認。両方グリーンになったら「Done」をクリック。

  | 項目 | インストールコマンド（未導入の場合） |
  | ---- | ------------------------------------ |
  | Python 3.10+ | `winget install Python.Python.3.12` |
  | UV Package Manager | `winget install astral-sh.uv` |

  **② Configure MCP Clients**
  「Done」後に自動でこの画面に切り替わる。マシン上の MCP クライアントが一覧表示されるので、**Claude Code のみチェックを残して**他を外し、「**Configure Selected**」をクリック。

  > このステップで Claude Code の MCP 設定ファイルに unity-mcp サーバーが自動登録される。

8-2. 【手動】 **MCP ウィンドウを開いてサーバーを起動** — `Window → MCP for Unity → Toggle MCP Window`。

  **Server セクション:**
  - Transport: **HTTP Local**、HTTP URL: `http://127.0.0.1:8080` を確認（ポート競合時は `8090` 等へ変更）。
  - 「**Start Server**」をクリック → ● **Session Active (UnityProject)** とグリーン表示になれば起動成功。

  **Client Configuration セクション:**
  - Client ドロップダウンで「**Claude Code**」を選択し、● **Configured**（グリーン）になっていることを確認。「Not Configured」の場合は「**Configure**」をクリック。
  - 「**Install Skills**」をクリック（CC が Unity 操作に使うスキルが追加される）。

  **Auto-Start の設定（推奨）:**
  「**Advanced**」タブを開き、**「Auto-Start Server on Editor Load」にチェック**を入れる。次回以降の Unity 起動時に MCP サーバーが自動起動するようになる。

8-3. **接続確認** — 作業フォルダ内の Claude Code で:
  ```powershell
  claude mcp list
  ```
  以下のように `UnityMCP` が Connected になっていれば完了:
  ```
  context7:   https://mcp.context7.com/mcp (HTTP)              - √ Connected
  playwright: npx @playwright/mcp@latest --browser chromium    - √ Connected
  UnityMCP:   http://127.0.0.1:8080/mcp (HTTP)                - √ Connected
  ```
  起動中の CC では `/mcp` で状態確認。

8-4. **動作確認プロンプト例:**
  - 「現在のシーンに赤・青・黄のキューブを作成して」
  - 「コンソールのエラーを読んで、原因のスクリプトを修正して」

### 既知のハマりどころ


| 症状                        | 対処                                              |
| ------------------------- | ----------------------------------------------- |
| http ↔ stdio モードを切り替えた    | Claude Code を再起動して設定を再読込                        |
| 接続できない／ポートエラー             | ポートを `8090` 等に変更（`8080` 競合回避）                   |
| `claude` が見つからない          | PATH の通った環境から起動、または MCP 設定で CC の絶対パスを指定         |
| VS Code 拡張の CC で MCP が出ない | 拡張版は MCP 設定 UI 未対応。**CLI 版**の Claude Code で設定する |


> 参考: [CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp) / Wiki「[Fix Unity MCP and Claude Code](https://github.com/CoplayDev/unity-mcp/wiki/2.-Fix-Unity-MCP-and-Claude-Code)」

---

## 9. 開発ワークフロー（推奨ループ）

```mermaid
flowchart TD
    A["① Claude Code でスクリプトを編集"] --> B["② unity-mcp 経由で Unity に<br/>コンパイルさせ、コンソール出力を取得"]
    B --> C{"③ エラーは<br/>あるか？"}
    C -- "あり" --> D["CC がコンソール内容を読んで自律修正"]
    D --> B
    C -- "なし（正常コンパイル）" --> E["④ git commit"]
```



- Windows ネイティブのため**ファイル変更検知は正常**。保存後 Unity にフォーカスを移すと再コンパイルが走り、MCP 経由でトリガもできる。
- 自律修正ループで意図しない変更が入ることがあるため、動いた時点で都度 commit して巻き戻せるようにする。

---

## 10. トラブルシューティング & 付録

### Windows固有のよくある問題


| 問題                          | 対処                                                                      |
| --------------------------- | ----------------------------------------------------------------------- |
| `claude` をシステムフォルダで起動してしまった | 一旦 `/exit` で終了し、**作業用リポジトリのフォルダへ `cd` してから再起動**する（§6 参照）                |
| PowerShell スクリプトがブロック       | `Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser`  |
| `claude` 未認識                | `~\.local\bin` を User PATH に追加し PowerShell を再起動（§5）。`claude doctor` で点検 |
| PowerShellツールを使いたい          | v2.1.139 以降で標準化。段階的ロールアウトのため未適用なら opt-in                                |
| MCP ポート競合                   | 設定ポートを `8090` 等へ変更し CC を再起動                                             |
| `.meta` 不整合・参照切れ            | `.meta` を削除・改名しない。誤削除時は Unity が再生成するが参照は再設定が必要                          |
| **ステータスラインが表示されない**         | `statusLine.command` にバックスラッシュが含まれていないか確認。Git for Windows 環境では CC が Git Bash 経由でコマンドを実行するため、バックスラッシュがエスケープ文字として消費され無音で失敗する。`~/.claude/statusline.py` のようにフォワードスラッシュ・`~` で記述する（§6-3）。設定変更後は CC を再起動し、次のインタラクション後に反映される |
| **statusline.py で UnicodeEncodeError** | Windows の Python がデフォルトの cp932 で絵文字を出力しようとして失敗。スクリプト冒頭に `import io; sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')` を追加する（§6-4） |
| **playwright `Browser is not installed`** | `npx playwright install chromium` でなく `npx @playwright/mcp@latest install-browser chrome-for-testing` でインストールする。MCP のビルド番号と合わせるため（§6-6） |
| **context7 MCP の API キー間違い** | `claude mcp remove context7 --scope user` で削除後、正しいキーで `claude mcp add` を再実行 |
| **Unity 終了時に Stop Server は必要か** | 不要。Unity を普通に終了すれば MCP サーバーも自動停止する。CC がコマンド実行中に Unity を終了した場合は接続エラーが出ることがあるが、CC を再起動すれば回復する |
| **Unity 起動のたびに Start Server を押す必要があるか** | `Window → MCP for Unity → Toggle MCP Window → Advanced タブ` で **「Auto-Start Server on Editor Load」にチェック**を入れると次回以降は自動起動する |


### Claude Code の使い分け（このマシンの方針）


| 用途                       | 使う CC                       |
| ------------------------ | --------------------------- |
| この Unity プロジェクト          | **Windows版 Claude Code**    |
| Python / Next.js 等のアプリ開発 | **WSL版 Claude Code**（従来どおり） |


履歴（会話・undo・memory）は両者で独立する。設定を揃えたい場合は §7 のとおり repo 内に `CLAUDE.md` / `.claude/` をコミットして共有する。

### 付録：WSL構成（代替案）

どうしても WSL 上の Claude Code で Unity を扱いたい場合は、**プロジェクトを WSL ファイルシステム（`~/`）側に置き**、Unity からは `\\wsl$\<Distro>\home\<user>\...` で参照する。`/mnt/c` を常用する構成（Windows側にプロジェクト＋WSLのCC）は、CC が `/mnt/c` のI/O遅延・inotify不達を負うため非推奨。本ガイドが Claude Code も Windows 版にしているのはこのため。

### 参考リンク集

- Claude Code 公式セットアップ（Windows/PowerShell/Git for Windows）: [https://code.claude.com/docs/en/setup](https://code.claude.com/docs/en/setup)
- Win32-OpenSSH 各種ファイルの権限保護（SSH鍵のACL）: [https://github.com/PowerShell/Win32-OpenSSH/wiki/Security-protection-of-various-files-in-Win32-OpenSSH](https://github.com/PowerShell/Win32-OpenSSH/wiki/Security-protection-of-various-files-in-Win32-OpenSSH)
- Unity 6 リリース / サポート: [https://unity.com/releases/unity-6](https://unity.com/releases/unity-6) ・ [https://unity.com/releases/unity-6/support](https://unity.com/releases/unity-6/support)
- Install Unity（マニュアル）: [https://docs.unity3d.com/6000.3/Documentation/Manual/GettingStartedInstallingUnity.html](https://docs.unity3d.com/6000.3/Documentation/Manual/GettingStartedInstallingUnity.html)
- Unity Hub: [https://unity.com/download](https://unity.com/download) ・ [https://docs.unity.com/en-us/hub/install-hub](https://docs.unity.com/en-us/hub/install-hub)
- CoplayDev unity-mcp: [https://github.com/CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp)
- Unity 公式 MCP（参考）: [https://unity.com/blog/unity-ai-mcp-how-to-get-started](https://unity.com/blog/unity-ai-mcp-how-to-get-started)

