# Billboard Pin Markers Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 各都市の光柱マーカーを、ピンアイコン画像 + 地点名ラベルの「ビルボードピン」（常にカメラへ正対、画面上一定サイズ、ホバー/選択でラベル表示、クリックで選択色変化）に置き換える。

**Architecture:** マーカーを World-Space UGUI Canvas（`Image`=ピン / `TMP_Text`=地点名）で構成し、`CityMarker` が毎フレーム正対と画面一定サイズスケールを適用する。クリック/ホバー判定は既存 `MapManager` の Physics.Raycast を踏襲（UI EventSystem を使わずカメラ操作を阻害しない）。`MapManager.CitySelected` / `CityFocused` イベント I/F は不変のため、ドロップダウン同期・カメラフォーカスは無改修で動作する。

**Tech Stack:** Unity 6000.3.18f1 LTS / HDRP、UGUI（World-Space Canvas）、TextMeshPro（Noto Sans JP SDF）、Unity Input System、NUnit EditMode テスト、Unity MCP（プレハブ/シーン構築）。

## Global Constraints

- 実行環境は Windows（PowerShell 優先 / Git Bash 可）。リポジトリルート `C:\work\japan-weather-demo`、Unity プロジェクト `C:\work\japan-weather-demo\UnityProject`。
- コミットメッセージは英語。変更は小さくまとめ各変更ごとに commit。
- 推測で変更しない。仕様は一次情報で確認してから最小変更。
- ランタイム asmdef は `JapanWeatherDemo`（参照に `Unity.TextMeshPro`、`Unity.RenderPipelines.HighDefinition.Runtime` 等あり）。テスト asmdef は `JapanWeatherDemo.Tests.EditMode`（`JapanWeatherDemo` を参照、Editor 専用、NUnit）。
- C# スクリプト変更後は Unity MCP `read_console` でコンパイルエラーを確認してから次へ進む。
- Unity のシーン保存は Play 停止（Edit モード）中に行う（Play 中は SaveScene が失敗する）。
- Font `NotoSansJP SDF.asset` は dynamic で Play 毎に差分が出るが **コミットしない**（`git restore` で破棄）。
- 既存 EditMode テストは 48/48 green。本計画完了時も全 green を維持する。

---

### Task 1: ピンスプライト生成とインポート

**Files:**
- Create: `UnityProject/Assets/Textures/Icons/pin.png`
- Create: `UnityProject/Assets/Textures/Icons/pin.png.meta`（Unity が生成、Sprite 設定）

**Interfaces:**
- Consumes: なし
- Produces: スプライト `Assets/Textures/Icons/pin.png`（白ベース、透過、`Image.color` で tint 可能）。Task 5 のプレハブが参照する。

- [ ] **Step 1: ピン PNG を生成する**

PowerShell（`System.Drawing.Common`）で白い滴形マップピン（上が円・下が尖り、中央に穴）を描く。背景透過・アンチエイリアス。リポジトリルートで実行：

```powershell
Add-Type -AssemblyName System.Drawing
$W = 128; $H = 168
$bmp = New-Object System.Drawing.Bitmap($W, $H)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)
$white = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)

# バルーン（上部の円）＋下向きの尖り を 1 つの塗りパスで作る
$path = New-Object System.Drawing.Drawing2D.GraphicsPath
$cx = 64.0; $cy = 60.0; $r = 52.0
# 円
$path.AddEllipse([float]($cx - $r), [float]($cy - $r), [float]($r * 2), [float]($r * 2))
# 下向き三角（円の左右接点から先端へ）
$pts = @(
  (New-Object System.Drawing.PointF([float]($cx - 36), [float]($cy + 38))),
  (New-Object System.Drawing.PointF([float]$cx,        [float]156)),
  (New-Object System.Drawing.PointF([float]($cx + 36), [float]($cy + 38)))
)
$path.AddPolygon($pts)
$g.FillPath($white, $path)

# 中央の穴（透明にくり抜く）
$g.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$holeR = 20.0
$transparent = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(0,255,255,255))
$g.FillEllipse($transparent, [float]($cx - $holeR), [float]($cy - $holeR), [float]($holeR * 2), [float]($holeR * 2))

$g.Dispose()
$out = "C:\work\japan-weather-demo\UnityProject\Assets\Textures\Icons\pin.png"
$bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
Write-Output "saved $out"
```

- [ ] **Step 2: 生成を確認する**

Run（PowerShell）:
```powershell
(Get-Item "C:\work\japan-weather-demo\UnityProject\Assets\Textures\Icons\pin.png").Length
Add-Type -AssemblyName System.Drawing
$b = New-Object System.Drawing.Bitmap("C:\work\japan-weather-demo\UnityProject\Assets\Textures\Icons\pin.png"); "$($b.Width)x$($b.Height)"; $b.Dispose()
```
Expected: ファイルサイズ > 0、`128x168` と表示。

- [ ] **Step 3: Unity に取り込み Sprite として設定する**

Unity MCP で再取り込み → テクスチャタイプを Sprite に設定。
- `refresh_unity` でアセットを再インポート。
- `manage_texture`（または `manage_asset`）で `Assets/Textures/Icons/pin.png` の `textureType` を `Sprite`（2D and UI）、`alphaIsTransparency=true` に設定。既存 `icon_play.png` 等の設定（Sprite, single）に合わせる。

- [ ] **Step 4: コンソール確認**

Unity MCP `read_console`（errors）でインポートエラーが無いことを確認。
Expected: pin.png 由来のエラー無し。

- [ ] **Step 5: Commit**

```bash
cd /c/work/japan-weather-demo
git add UnityProject/Assets/Textures/Icons/pin.png UnityProject/Assets/Textures/Icons/pin.png.meta
git commit -m "feat: add white map pin sprite for billboard markers"
```

---

### Task 2: ビルボードスケール純関数（TDD）

**Files:**
- Create: `UnityProject/Assets/Scripts/Map/BillboardScale.cs`
- Test: `UnityProject/Assets/Tests/EditMode/BillboardScaleTests.cs`

**Interfaces:**
- Consumes: なし
- Produces: `JapanWeatherDemo.Map.BillboardScale.ScaleForConstantScreenSize(float distance, float scalePerUnit) -> float`。Task 3 の `CityMarker` が毎フレーム呼ぶ。

- [ ] **Step 1: 失敗するテストを書く**

`UnityProject/Assets/Tests/EditMode/BillboardScaleTests.cs`:
```csharp
using NUnit.Framework;
using JapanWeatherDemo.Map;

namespace JapanWeatherDemo.Tests
{
    public class BillboardScaleTests
    {
        [Test]
        public void Scale_IsProportionalToDistance()
        {
            // 画面上一定サイズ ⇔ ワールドスケールは距離に比例
            float s1 = BillboardScale.ScaleForConstantScreenSize(10f, 0.01f);
            float s2 = BillboardScale.ScaleForConstantScreenSize(20f, 0.01f);
            Assert.AreEqual(0.1f, s1, 1e-5f);
            Assert.AreEqual(0.2f, s2, 1e-5f);
            Assert.AreEqual(2f, s2 / s1, 1e-5f); // 距離2倍→スケール2倍
        }

        [Test]
        public void Scale_NeverNegative()
        {
            float s = BillboardScale.ScaleForConstantScreenSize(-5f, 0.01f);
            Assert.AreEqual(0f, s, 1e-5f);
        }
    }
}
```

- [ ] **Step 2: テストが失敗することを確認する**

Unity MCP `run_tests`（mode=EditMode, filter `BillboardScaleTests`）。
Expected: コンパイルエラー（`BillboardScale` 未定義）で FAIL。

- [ ] **Step 3: 最小実装を書く**

`UnityProject/Assets/Scripts/Map/BillboardScale.cs`:
```csharp
using UnityEngine;

namespace JapanWeatherDemo.Map
{
    /// <summary>ビルボードを画面上一定サイズに保つためのワールドスケール計算（純粋関数）。</summary>
    public static class BillboardScale
    {
        /// <summary>
        /// 透視投影では見た目の大きさは worldSize/distance に比例する。
        /// 画面上一定サイズにするには worldSize を distance に比例させればよい。
        /// </summary>
        /// <param name="distance">カメラからマーカーまでの距離。</param>
        /// <param name="scalePerUnit">距離 1 あたりのワールドスケール（大きさの基準）。</param>
        public static float ScaleForConstantScreenSize(float distance, float scalePerUnit)
            => Mathf.Max(0f, distance) * scalePerUnit;
    }
}
```

- [ ] **Step 4: テストが通ることを確認する**

Unity MCP `run_tests`（EditMode, filter `BillboardScaleTests`）。
Expected: 2 件 PASS。

- [ ] **Step 5: 全 EditMode テストを確認する**

Unity MCP `run_tests`（EditMode 全件）。
Expected: 50/50 PASS（既存 48 + 新規 2）。

- [ ] **Step 6: Commit**

```bash
cd /c/work/japan-weather-demo
git add UnityProject/Assets/Scripts/Map/BillboardScale.cs UnityProject/Assets/Scripts/Map/BillboardScale.cs.meta \
        UnityProject/Assets/Tests/EditMode/BillboardScaleTests.cs UnityProject/Assets/Tests/EditMode/BillboardScaleTests.cs.meta
git commit -m "feat: add billboard scale pure function with tests"
```

---

### Task 3: CityMarker をビルボードピンに書き換え

**Files:**
- Modify: `UnityProject/Assets/Scripts/Map/CityMarker.cs`（全面書き換え）

**Interfaces:**
- Consumes: `JapanWeatherDemo.Map.BillboardScale.ScaleForConstantScreenSize`（Task 2）、`JapanWeatherDemo.Data.CityData`。
- Produces:
  - `CityData City { get; }`（既存維持）
  - `event System.Action<CityMarker> Clicked`（既存維持）
  - `void Init(CityData city)`（既存維持）
  - `void SetSelected(bool value)`（既存維持。引数名のみ変更可）
  - `void NotifyClicked()`（既存維持）
  - `void SetHover(bool value)`（新規。Task 4 が呼ぶ）
  - `void SetCamera(UnityEngine.Camera c)`（新規。Task 4 の `BuildMarkers` が呼ぶ）

- [ ] **Step 1: CityMarker.cs を全面書き換え**

`UnityProject/Assets/Scripts/Map/CityMarker.cs` の全内容を以下に置き換える：
```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using JapanWeatherDemo.Data;

namespace JapanWeatherDemo.Map
{
    /// <summary>地図上の 1 都市を表すビルボードピン。常にカメラへ正対し、画面上一定サイズを保つ。
    /// ホバー/選択時に地点名を表示し、選択時はピン色を変える。クリックで選択を通知する。</summary>
    [RequireComponent(typeof(Collider))]
    public class CityMarker : MonoBehaviour
    {
        [SerializeField] private Image pin;        // ピン画像（白ベース、color で tint）
        [SerializeField] private TMP_Text label;   // 地点名（ホバー/選択時のみ表示）
        [SerializeField] private float scalePerUnit = 0.01f; // 距離1あたりのワールドスケール

        public CityData City { get; private set; }
        public event System.Action<CityMarker> Clicked;

        private readonly Color baseColor = new Color(0.95f, 0.95f, 0.95f);
        private readonly Color selectedColor = new Color(1f, 0.85f, 0.3f);

        private UnityEngine.Camera cam;
        private bool selected;
        private bool hovered;

        public void Init(CityData city)
        {
            City = city;
            name = $"Marker_{city.name}";
            if (label != null) label.text = city.name;
            selected = false;
            hovered = false;
            ApplyState();
        }

        public void SetCamera(UnityEngine.Camera c) => cam = c;

        public void SetSelected(bool value)
        {
            selected = value;
            ApplyState();
        }

        public void SetHover(bool value)
        {
            hovered = value;
            ApplyState();
        }

        // ピン色とラベル表示を現在の状態から更新する
        private void ApplyState()
        {
            if (pin != null) pin.color = selected ? selectedColor : baseColor;
            // ラベルは「選択中 または ホバー中」のときだけ表示する
            if (label != null) label.gameObject.SetActive(selected || hovered);
        }

        private void LateUpdate()
        {
            if (cam == null) cam = UnityEngine.Camera.main;
            if (cam == null) return;
            // 正対：カメラ回転をそのまま採用（テキストが鏡像にならない）
            transform.rotation = cam.transform.rotation;
            // 画面上一定サイズ：距離に比例してスケール（collider も同 transform で連動）
            float dist = Vector3.Distance(cam.transform.position, transform.position);
            float s = BillboardScale.ScaleForConstantScreenSize(dist, scalePerUnit);
            transform.localScale = new Vector3(s, s, s);
        }

        // MapManager から Raycast ヒット時に呼ばれる
        public void NotifyClicked() => Clicked?.Invoke(this);
    }
}
```

- [ ] **Step 2: コンパイル確認**

Unity MCP `read_console`（errors）。必要なら `refresh_unity`。
Expected: CityMarker.cs 由来のコンパイルエラー無し（`Image`/`TMP_Text` も解決）。

- [ ] **Step 3: 全 EditMode テストを確認する**

Unity MCP `run_tests`（EditMode 全件）。
Expected: 50/50 PASS（書き換えで既存テストが壊れていないこと）。

- [ ] **Step 4: Commit**

```bash
cd /c/work/japan-weather-demo
git add UnityProject/Assets/Scripts/Map/CityMarker.cs
git commit -m "feat: rewrite CityMarker as billboard pin (image + label, hover/select)"
```

---

### Task 4: MapManager にホバー検出を追加

**Files:**
- Modify: `UnityProject/Assets/Scripts/Map/MapManager.cs`

**Interfaces:**
- Consumes: `CityMarker.SetHover(bool)`、`CityMarker.SetCamera(UnityEngine.Camera)`、`CityMarker.NotifyClicked()`、`CityMarker.SetSelected(bool)`（Task 3）。
- Produces: `MapManager.CitySelected` / `CityFocused`（不変）。

- [ ] **Step 1: BuildMarkers にカメラ注入を追加**

`MapManager.BuildMarkers()` のループ内、`marker.Init(city);` の直後に 1 行追加：
```csharp
                marker.Init(city);
                marker.SetCamera(raycastCamera);
                marker.Clicked += OnMarkerClicked;
```

- [ ] **Step 2: Update をホバー対応に置き換える**

`MapManager.Update()` メソッド全体（現状 L46-62）を以下に置き換える：
```csharp
        private CityMarker hovered;

        private void Update()
        {
            // UI（タイムライン・情報パネル等）の上では地図の hover/クリックを無視する
            bool overUI = UnityEngine.EventSystems.EventSystem.current != null &&
                          UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

            var mouse = Mouse.current;
            if (mouse == null || overUI)
            {
                SetHover(null);
                return;
            }

            Vector2 screen = mouse.position.ReadValue();
            Ray ray = raycastCamera.ScreenPointToRay(screen);
            CityMarker hit = null;
            if (Physics.Raycast(ray, out RaycastHit h, 1000f))
                hit = h.collider.GetComponentInParent<CityMarker>();

            SetHover(hit);

            if (mouse.leftButton.wasPressedThisFrame && hit != null)
                hit.NotifyClicked();
        }

        // ホバー対象の切替を管理する
        private void SetHover(CityMarker marker)
        {
            if (hovered == marker) return;
            if (hovered != null) hovered.SetHover(false);
            hovered = marker;
            if (hovered != null) hovered.SetHover(true);
        }
```

注意: `private CityMarker hovered;` は他のフィールド（`private CityMarker selected;`）付近に置いてもよいが、上記ブロックをそのまま貼る場合はクラス内に二重定義しないこと。既存の `Update()` は完全に置換する。

- [ ] **Step 3: コンパイル確認**

Unity MCP `read_console`（errors）。
Expected: MapManager.cs 由来のコンパイルエラー無し。

- [ ] **Step 4: 全 EditMode テストを確認する**

Unity MCP `run_tests`（EditMode 全件）。
Expected: 50/50 PASS。

- [ ] **Step 5: Commit**

```bash
cd /c/work/japan-weather-demo
git add UnityProject/Assets/Scripts/Map/MapManager.cs
git commit -m "feat: add hover detection for city markers in MapManager"
```

---

### Task 5: CityMarker プレハブ再構築と MainScene 配線（Unity Editor / MCP）

**Files:**
- Modify: `UnityProject/Assets/Prefabs/CityMarker.prefab`（構造を作り直し）
- Modify: `UnityProject/Assets/Scenes/MainScene.unity`（MapManager の `markerPrefab` 参照を再設定。GUID 不変なら自動で維持）

**Interfaces:**
- Consumes: スプライト `pin.png`（Task 1）、`CityMarker`（Task 3）、`MapManager`（Task 4）、フォント `NotoSansJP SDF`。
- Produces: Play で動作するビルボードピン群。

前提: すべて **Edit モード（Play 停止）** で行う。シーン保存は Edit モードのみ。作業は Unity MCP（`manage_prefabs` / `manage_gameobject` / `manage_components` / `manage_ui`）で行う。

- [ ] **Step 1: 旧プレハブ構成を確認**

Unity MCP で `Assets/Prefabs/CityMarker.prefab` を開き、現状の子（emissive 円柱メッシュ、point light、capsule collider）を把握する。

- [ ] **Step 2: プレハブを新構成に作り直す**

`CityMarker.prefab` を次の階層にする：
- Root `CityMarker`:
  - Component `CityMarker`（Task 3）
  - Component `BoxCollider`（旧 capsule collider は削除。ピンの見た目を覆うサイズ。初期 size ≈ (1.2, 1.6, 0.2)、center ≈ (0, 0.8, 0)。Play で調整）
  - 旧 emissive 円柱 MeshRenderer / 子 point light は **削除**
  - 子 `Canvas`:
    - Component `Canvas`（Render Mode = **World Space**）
    - Component `CanvasScaler`（Dynamic Pixels Per Unit = 10 程度。Play で文字の鮮明さを見て調整）
    - `GraphicRaycaster` は **付けない**（クリックは Physics.Raycast で扱うため）
    - RectTransform: 例 幅 200 × 高さ 280 px、localScale 0.01 程度（root が距離スケールを掛けるので最終サイズは Play で `scalePerUnit` と合わせ調整）
    - 子 `Pin`:
      - Component `Image`（Source Image = `pin.png`、Color 白）
      - RectTransform: Canvas 内でピン先端が root 原点に来るよう配置（ピンを上側に置き、tip を下端へ）
    - 子 `Label`:
      - Component `TextMeshProUGUI`（Font Asset = `NotoSansJP SDF`、白、中央寄せ、ピンの上に配置）
      - 初期は非アクティブでよい（`Init`/`ApplyState` が表示制御）

- [ ] **Step 3: プレハブの参照を結線**

Root `CityMarker` コンポーネントの SerializeField を設定：
- `pin` ← 子 `Pin` の `Image`
- `label` ← 子 `Label` の `TMP_Text`
- `scalePerUnit` ← 0.01（初期値、Play で調整）

- [ ] **Step 4: MainScene の MapManager 参照を確認**

MainScene を開き、`MapManager` の `markerPrefab` が新しい `CityMarker.prefab` を指していることを確認（同一アセットの編集なら GUID 不変で維持される）。`markerY` はピンが地図に接して見える値に（旧光柱用の値なら Play で調整）。Edit モードでシーン保存。

- [ ] **Step 5: Play モードで受け入れ確認**

Unity MCP で Play 開始し、以下を目視確認（MainCamera の斜め視点で確認。トップダウン positioned-capture は露出が飛ぶため不可）：
1. 145 ピンが正しい地点に表示される。
2. カメラ回転/ズームでピンは常に正対し、画面上サイズが一定（寄っても巨大化しない）。
3. ピンにマウスを乗せると地点名が出て、外すと消える。
4. ピンをクリックすると色が変わり、地点名が出たまま（選択維持）。
5. 選択がドロップダウンに同期し、カメラがその都市にフォーカスする。
6. ピン上/間でカメラのドラッグ回転・ズームが破綻しない。

不具合があれば `scalePerUnit` / Canvas サイズ / BoxCollider サイズ / `markerY` を Edit モードで調整して再確認（推測ではなく Play の見た目を根拠に最小調整）。Play は停止してから保存。

- [ ] **Step 6: Commit**

`NotoSansJP SDF.asset` の Play 差分はコミットしない（`git restore` で破棄）。
```bash
cd /c/work/japan-weather-demo
git restore UnityProject/Assets/Fonts/NotoSansJP\ SDF.asset 2>/dev/null || true
git add UnityProject/Assets/Prefabs/CityMarker.prefab UnityProject/Assets/Scenes/MainScene.unity
git commit -m "feat: rebuild CityMarker prefab as billboard pin and wire into MainScene"
```

注意: 上記フォントのパスは実際の配置に合わせる（`Assets/Fonts/` 配下を想定。異なる場合は `git status` で確認して該当ファイルのみ restore）。

---

## 受け入れ基準（計画全体）

- EditMode テスト 50/50 green（既存 48 + BillboardScale 2）。
- Play で Task 5 Step 5 の 1〜6 をすべて満たす。
- 光柱（emissive 円柱 / 選択時 point light）が完全に除去されている。
- ドロップダウン双方向同期・カメラフォーカスが従来どおり動作（無改修）。
