# 都市選択ドロップダウン + カメラフォーカス Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 右上情報パネルの都市名表示を都市選択ドロップダウンに置き換え、選択時にカメラが対象地点の斜め見下ろし構図へスムーズに移動するようにする。

**Architecture:** 既存の「ScriptableObject + C# Event 疎結合」構成を踏襲。新規 `CameraFocusController`・`CityDropdownController` は `MapManager` のイベントを購読するだけで互いを直接参照しない。カメラ姿勢算出は純粋関数 `CameraFraming.ComputeFocusPose` に切り出し EditMode でテストする。予報取得〜エフェクト更新の既存フローは変更しない。

**Tech Stack:** Unity 6000.3.18f1 LTS / HDRP / C# / Input System / TextMeshPro / Unity Test Runner (EditMode, NUnit)

設計書: `docs/superpowers/specs/2026-06-30-city-dropdown-camera-focus-design.md`

## Global Constraints

- コマンドは Windows（PowerShell / Git Bash）で実行。リポジトリルート `C:\work\japan-weather-demo`、Unity プロジェクト `UnityProject/`。
- コミットメッセージは英語。各タスク完了ごとにコミットする（**コミット前にユーザーの了承を求める**）。
- 推測で変更しない。原因未確定なら一次情報で特定してから最小変更。
- ランタイムスクリプトは単一 asmdef `JapanWeatherDemo`（rootNamespace `JapanWeatherDemo`）に属する。カメラ系の名前空間は `JapanWeatherDemo.CameraControl`、UI は `JapanWeatherDemo.UI`、データは `JapanWeatherDemo.Data`、マップは `JapanWeatherDemo.Map`。
- EditMode テストは `UnityProject/Assets/Tests/EditMode/`（asmdef `JapanWeatherDemo.Tests.EditMode`、rootNamespace `JapanWeatherDemo.Tests`、`JapanWeatherDemo` を参照済み）。
- スクリプト新規作成/変更後は Unity を refresh し `read_console` でコンパイルエラーが無いことを確認してから次へ進む（新規型はコンパイル成功後にのみ使用可能）。
- シーン編集・保存は **Play 停止中** のみ行う（Play 中の SaveScene は失敗する）。
- 日本語 UI 表示には TMP フォント `NotoSansJP SDF`（Dynamic）を割り当てる。`NotoSansJP SDF.asset` は Play のたびに差分が出るが **コミットしない**（`git restore` で破棄）。

---

### Task 1: CameraFraming 純粋関数（カメラ姿勢算出） + EditMode テスト

注視点（target）に対し、`height` だけ上空・`backDistance` だけ -Z 方向に引いた斜め見下ろしのカメラ姿勢 (position, rotation) を返す純粋関数。MonoBehaviour 非依存でテスト可能。

**Files:**
- Create: `UnityProject/Assets/Scripts/Camera/CameraFraming.cs`
- Test: `UnityProject/Assets/Tests/EditMode/CameraFramingTests.cs`

**Interfaces:**
- Consumes: `UnityEngine.Vector3`, `UnityEngine.Quaternion`
- Produces: `JapanWeatherDemo.CameraControl.CameraFraming.ComputeFocusPose(Vector3 target, float height, float backDistance)` → `(Vector3 position, Quaternion rotation)`

- [ ] **Step 1: 失敗するテストを書く**

Create `UnityProject/Assets/Tests/EditMode/CameraFramingTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;
using JapanWeatherDemo.CameraControl;

namespace JapanWeatherDemo.Tests
{
    public class CameraFramingTests
    {
        [Test]
        public void Position_IsAboveAndBehindTarget()
        {
            var target = new Vector3(3f, 0.1f, -2f);
            var (pos, _) = CameraFraming.ComputeFocusPose(target, 14f, 10f);
            Assert.AreEqual(3f, pos.x, 1e-4f);
            Assert.AreEqual(0.1f + 14f, pos.y, 1e-4f);
            Assert.AreEqual(-2f - 10f, pos.z, 1e-4f);
        }

        [Test]
        public void Rotation_LooksAtTarget()
        {
            var target = new Vector3(3f, 0.1f, -2f);
            var (pos, rot) = CameraFraming.ComputeFocusPose(target, 14f, 10f);
            Vector3 fwd = rot * Vector3.forward;
            Vector3 expected = (target - pos).normalized;
            Assert.AreEqual(expected.x, fwd.x, 1e-4f);
            Assert.AreEqual(expected.y, fwd.y, 1e-4f);
            Assert.AreEqual(expected.z, fwd.z, 1e-4f);
        }

        [Test]
        public void Pitch_MatchesHeightToDistanceRatio()
        {
            var target = Vector3.zero;
            var (_, rot) = CameraFraming.ComputeFocusPose(target, 14f, 10f);
            Vector3 fwd = rot * Vector3.forward;
            Vector3 flat = new Vector3(fwd.x, 0f, fwd.z);
            float pitch = Vector3.Angle(fwd, flat);
            float expected = Mathf.Atan2(14f, 10f) * Mathf.Rad2Deg; // ≈ 54.46°
            Assert.AreEqual(expected, pitch, 1e-2f);
        }
    }
}
```

- [ ] **Step 2: テストが失敗することを確認**

Unity を refresh し、Unity Test Runner で EditMode を実行（MCP: `run_tests` mode=`EditMode`、filter `CameraFramingTests`）。
Expected: コンパイルエラー（`CameraFraming` が存在しない）でテストがビルド不可 = FAIL。

- [ ] **Step 3: 最小実装を書く**

Create `UnityProject/Assets/Scripts/Camera/CameraFraming.cs`:

```csharp
using UnityEngine;

namespace JapanWeatherDemo.CameraControl
{
    /// <summary>都市フォーカス時のカメラ姿勢を算出する純粋関数。</summary>
    public static class CameraFraming
    {
        /// <summary>
        /// target（注視点）に対し height だけ上空・backDistance だけ -Z に引いた
        /// 斜め見下ろしのカメラ姿勢を返す。rotation は target を向く。
        /// </summary>
        public static (Vector3 position, Quaternion rotation) ComputeFocusPose(
            Vector3 target, float height, float backDistance)
        {
            Vector3 position = target + new Vector3(0f, height, -backDistance);
            Quaternion rotation = Quaternion.LookRotation(target - position);
            return (position, rotation);
        }
    }
}
```

- [ ] **Step 4: テストが通ることを確認**

Unity を refresh し `read_console` でコンパイルエラーが無いことを確認。EditMode テストを実行（filter `CameraFramingTests`）。
Expected: 3 件すべて PASS。既存テストも緑のまま（全体実行で 45+3=48 緑）。

- [ ] **Step 5: コミット**

```bash
cd "C:\work\japan-weather-demo"
git add UnityProject/Assets/Scripts/Camera/CameraFraming.cs UnityProject/Assets/Scripts/Camera/CameraFraming.cs.meta UnityProject/Assets/Tests/EditMode/CameraFramingTests.cs UnityProject/Assets/Tests/EditMode/CameraFramingTests.cs.meta
git commit -m "feat: add CameraFraming focus-pose pure function with tests"
```

---

### Task 2: MapManager.CityFocused イベント + CameraFocusController

`MapManager` に選択マーカーのワールド位置を通知する `CityFocused` イベントを追加し、それを購読してカメラをスムーズ移動させる `CameraFocusController` を作る。コルーチン/MonoBehaviour 依存のため検証は Play-mode 目視。

**Files:**
- Modify: `UnityProject/Assets/Scripts/Map/MapManager.cs`（`CityFocused` イベント追加 + `Select` で発火）
- Create: `UnityProject/Assets/Scripts/Camera/CameraFocusController.cs`
- Scene: `UnityProject/Assets/Scenes/MainScene.unity`（MainCamera に `CameraFocusController` を付与・配線）

**Interfaces:**
- Consumes: `JapanWeatherDemo.CameraControl.CameraFraming.ComputeFocusPose(Vector3, float, float)`、`JapanWeatherDemo.CameraControl.FreeCameraController`
- Produces: `JapanWeatherDemo.Map.MapManager.CityFocused`（`event System.Action<Vector3>`、選択マーカーの `transform.position` を渡す）

- [ ] **Step 1: MapManager に CityFocused イベントを追加**

Modify `UnityProject/Assets/Scripts/Map/MapManager.cs`。既存の `public event System.Action<CityData> CitySelected;` の直後に追加:

```csharp
        /// <summary>選択マーカーのワールド位置を通知する（カメラフォーカス用）。</summary>
        public event System.Action<Vector3> CityFocused;
```

そして `Select(CityMarker marker)` の末尾、`CitySelected?.Invoke(marker.City);` の直後に追加:

```csharp
            CityFocused?.Invoke(marker.transform.position);
```

- [ ] **Step 2: CameraFocusController を作成**

Create `UnityProject/Assets/Scripts/Camera/CameraFocusController.cs`:

```csharp
using System.Collections;
using UnityEngine;
using JapanWeatherDemo.Map;

namespace JapanWeatherDemo.CameraControl
{
    /// <summary>選択都市の上空・斜め見下ろし構図へカメラをスムーズに移動する。</summary>
    public class CameraFocusController : MonoBehaviour
    {
        [SerializeField] private MapManager mapManager;
        [SerializeField] private FreeCameraController freeCamera;
        [SerializeField] private float height = 14f;
        [SerializeField] private float backDistance = 10f;
        [SerializeField] private float lookAtYOffset = 0f;
        [SerializeField] private float duration = 0.7f;

        private Coroutine moving;

        private void OnEnable()
        {
            if (mapManager != null) mapManager.CityFocused += OnCityFocused;
        }

        private void OnDisable()
        {
            if (mapManager != null) mapManager.CityFocused -= OnCityFocused;
        }

        private void OnCityFocused(Vector3 worldPos)
        {
            Vector3 target = worldPos + Vector3.up * lookAtYOffset;
            var (pos, rot) = CameraFraming.ComputeFocusPose(target, height, backDistance);
            if (moving != null) StopCoroutine(moving);
            moving = StartCoroutine(MoveTo(pos, rot));
        }

        private IEnumerator MoveTo(Vector3 targetPos, Quaternion targetRot)
        {
            // 移動中は自由カメラ入力を無効化して競合を防ぐ
            if (freeCamera != null) freeCamera.enabled = false;
            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.0001f, duration);
                float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                transform.position = Vector3.Lerp(startPos, targetPos, e);
                transform.rotation = Quaternion.Slerp(startRot, targetRot, e);
                yield return null;
            }
            transform.position = targetPos;
            transform.rotation = targetRot;
            if (freeCamera != null) freeCamera.enabled = true;
            moving = null;
        }
    }
}
```

- [ ] **Step 3: コンパイル確認**

Unity を refresh し `read_console` でエラーが無いことを確認。
Expected: コンパイルエラー無し。EditMode テストも全緑のまま（filter なしで全実行）。

- [ ] **Step 4: シーン配線（Play 停止中）**

MainScene を Edit モードで開く。
1. `MainCamera` を選択し、`CameraFocusController` を Add Component。
2. `mapManager` に MapManager を持つ GameObject を割り当て。
3. `freeCamera` に MainCamera 上の `FreeCameraController` を割り当て。
4. パラメータは既定値（height 14 / backDistance 10 / duration 0.7）のまま。
5. シーンを保存。

- [ ] **Step 5: Play-mode 目視確認**

Play を開始。
- 起動時、初期選択（東京）へカメラがスムーズに寄ること。
- 地図上の別マーカーをクリック → カメラがその都市の斜め見下ろし構図へ約 0.7 秒で移動し、天候エフェクト（空・雲・降水）が見えること。
- 移動完了後、右ドラッグ/ホイール/WASD で自由カメラ操作ができること（`FreeCameraController` が再有効化されている）。
Play を停止。

- [ ] **Step 6: コミット**

```bash
cd "C:\work\japan-weather-demo"
git restore UnityProject/Assets/Resources/Fonts/NotoSansJP\ SDF.asset 2>/dev/null || true
git add UnityProject/Assets/Scripts/Map/MapManager.cs UnityProject/Assets/Scripts/Camera/CameraFocusController.cs UnityProject/Assets/Scripts/Camera/CameraFocusController.cs.meta UnityProject/Assets/Scenes/MainScene.unity
git commit -m "feat: smooth camera focus on city selection"
```

---

### Task 3: CityDropdownController + InfoPanel ドロップダウン化

右上情報パネルの都市名 `TMP_Text` を `TMP_Dropdown` に置き換え、ドロップダウン⇄マーカーの双方向連動を実装する。`InfoPanelController` から都市名表示の責務を外す。MonoBehaviour/UI 依存のため検証は Play-mode 目視。

**Files:**
- Create: `UnityProject/Assets/Scripts/UI/CityDropdownController.cs`
- Modify: `UnityProject/Assets/Scripts/UI/InfoPanelController.cs`（都市名表示を削除）
- Scene: `UnityProject/Assets/Scenes/MainScene.unity`（InfoPanel の都市名を Dropdown に差し替え・配線）

**Interfaces:**
- Consumes: `JapanWeatherDemo.Map.MapManager.CitySelected`（`event Action<CityData>`）、`JapanWeatherDemo.Map.MapManager.SelectByName(string)`、`JapanWeatherDemo.Data.CityCatalog.LoadFromStreamingAssets()`、`TMPro.TMP_Dropdown`
- Produces: `JapanWeatherDemo.UI.CityDropdownController`（シーン配線用 MonoBehaviour）

- [ ] **Step 1: CityDropdownController を作成**

Create `UnityProject/Assets/Scripts/UI/CityDropdownController.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using JapanWeatherDemo.Data;
using JapanWeatherDemo.Map;

namespace JapanWeatherDemo.UI
{
    /// <summary>右上情報パネルの都市選択ドロップダウン。マーカー選択と双方向連動する。</summary>
    public class CityDropdownController : MonoBehaviour
    {
        [SerializeField] private MapManager mapManager;
        [SerializeField] private TMP_Dropdown dropdown;

        private readonly List<string> cityNames = new();

        private void Awake()
        {
            // MapManager と同じソース・同じ並び（Cities.json 順 ≒ 北→南）で選択肢を生成
            cityNames.Clear();
            foreach (var c in CityCatalog.LoadFromStreamingAssets())
                cityNames.Add(c.name);

            dropdown.ClearOptions();
            dropdown.AddOptions(cityNames);
        }

        private void OnEnable()
        {
            dropdown.onValueChanged.AddListener(OnDropdownChanged);
            if (mapManager != null) mapManager.CitySelected += OnCitySelected;
        }

        private void OnDisable()
        {
            dropdown.onValueChanged.RemoveListener(OnDropdownChanged);
            if (mapManager != null) mapManager.CitySelected -= OnCitySelected;
        }

        // ドロップダウン操作 → 既存の選択フローを駆動
        private void OnDropdownChanged(int index)
        {
            if (index < 0 || index >= cityNames.Count) return;
            mapManager.SelectByName(cityNames[index]);
        }

        // マーカー選択など外部からの選択 → ドロップダウン現在値を同期（無限ループ防止）
        private void OnCitySelected(CityData city)
        {
            int index = cityNames.IndexOf(city.name);
            if (index < 0) return;
            dropdown.SetValueWithoutNotify(index);
            dropdown.RefreshShownValue();
        }
    }
}
```

- [ ] **Step 2: InfoPanelController から都市名表示を削除**

Modify `UnityProject/Assets/Scripts/UI/InfoPanelController.cs`。`cityLabel` フィールドとその代入を削除し、天気・気温表示のみ残す。

`[SerializeField] private TMP_Text cityLabel;` の行を削除。`OnSnapshot` を以下に変更:

```csharp
        private void OnSnapshot(WeatherSnapshot s)
        {
            if (conditionLabel != null) conditionLabel.text = ToJapanese(s.condition);
            if (temperatureLabel != null) temperatureLabel.text = $"{s.temperatureCelsius:0.0}℃";
        }
```

- [ ] **Step 3: コンパイル確認**

Unity を refresh し `read_console` でエラーが無いことを確認。
Expected: コンパイルエラー無し。EditMode テストも全緑のまま。

- [ ] **Step 4: シーン配線（Play 停止中）**

MainScene を Edit モードで開く。
1. InfoPanel 内の都市名 `TMP_Text` GameObject を削除し、同じ位置に `TMP_Dropdown`（UI > Dropdown - TextMeshPro）を配置（または既存都市名箇所に差し替え）。
2. Dropdown の `Label`・`Item Label` の Font Asset に `NotoSansJP SDF` を割り当て（日本語表示のため）。
3. `CityDropdownController` を InfoPanel（または Canvas 配下）に Add Component し、`mapManager`・`dropdown` を割り当て。
4. 旧 `InfoPanelController` の `cityLabel` 参照が外れていることを確認（フィールド削除済みのため Inspector から消える）。
5. シーンを保存。

- [ ] **Step 5: Play-mode 目視確認**

Play を開始。
- 起動時、ドロップダウンの現在値が初期選択（東京）に一致していること。
- ドロップダウンから別都市を選ぶ → 予報取得 → 情報パネルの天気・気温が更新され、カメラがその都市へ移動すること。
- 地図マーカーをクリック → ドロップダウンの現在値がその都市に同期すること。
- ドロップダウンで選択 → 対象マーカーのハイライト（選択色）が同期すること。
- ドロップダウン操作後に `onValueChanged` の再帰発火やエラーが出ないこと（`read_console` 確認）。
Play を停止。

- [ ] **Step 6: コミット**

```bash
cd "C:\work\japan-weather-demo"
git restore UnityProject/Assets/Resources/Fonts/NotoSansJP\ SDF.asset 2>/dev/null || true
git add UnityProject/Assets/Scripts/UI/CityDropdownController.cs UnityProject/Assets/Scripts/UI/CityDropdownController.cs.meta UnityProject/Assets/Scripts/UI/InfoPanelController.cs UnityProject/Assets/Scenes/MainScene.unity
git commit -m "feat: city selection dropdown synced with map markers"
```

---

## 完了基準

- EditMode テスト全緑（既存 45 + `CameraFramingTests` 3 = 48）。
- ドロップダウンとマーカーが双方向連動し、選択時にカメラが斜め見下ろし構図へスムーズ移動する（Play-mode 目視）。
- 既存の予報取得〜エフェクト/タイムライン更新フローに退行が無い。
- `progress.md` に本機能の進捗を追記。
