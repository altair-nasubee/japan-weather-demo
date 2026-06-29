# Japan Weather Demo — MVP 実装計画

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** OpenWeatherMap の予報データを取得し、HDRP の日本地図上で都市を選択して気象エフェクトとタイムラインで可視化する Windows デモアプリの MVP を完成させる。

**Architecture:** 天気データを `WeatherTimelineSO`（ランタイム ScriptableObject）に集約し、C# event `OnSnapshotChanged` で各コントローラー（雲・降水・空・タイムライン UI・情報パネル）へ疎結合に伝播する。純ロジック（座標変換・JSON パース・天気コード分類・UTC→JST 変換・スナップショット補間）は MonoBehaviour 非依存の静的クラスに切り出し、EditMode 単体テストで TDD する。シーン構築・エフェクト・UI など見た目部分は Play Mode 目視で確認する。

**Tech Stack:** Unity 6000.3.18f1 LTS / HDRP 17.3.0 / Input System 1.19.0 / Newtonsoft JSON (com.unity.nuget.newtonsoft-json) / Test Framework (NUnit, EditMode) / VFX Graph / UnityWebRequest

## Global Constraints

これらは全タスクの暗黙の要件。各タスクで違反しないこと。

- **Unity バージョン:** 6000.3.18f1 LTS、レンダーパイプライン HDRP。
- **入力:** 新 Input System（`com.unity.inputsystem` 1.19.0）のみ使用。旧 `UnityEngine.Input` は使わない。
- **名前空間:** ランタイムは `JapanWeatherDemo`、サブ機能は `JapanWeatherDemo.Data` / `.Weather` / `.Map` / `.CameraControl` / `.UI`。エディタ拡張は `JapanWeatherDemo.Editor`。テストは `JapanWeatherDemo.Tests`。
  - ※ `Camera` は `UnityEngine.Camera` と衝突するため名前空間は `CameraControl` を使う（フォルダ名は `Camera/` のまま）。
- **JSON:** パースは Newtonsoft（`Newtonsoft.Json`）を使用。`JsonUtility` は入れ子・配列ルートに弱いため使わない。
- **座標系/測地系:** 緯度経度は WGS84。地図の範囲定数（lat/lon の min/max と Plane の XZ 範囲）は `MapBoundsSO` 1 箇所に集約し、テクスチャのトリミング範囲とマーカー配置で必ず共有する。
- **時刻:** `WeatherSnapshot.dateTime` と UI 表示はすべて JST（UTC+9）。API の `dt`（UTC 秒）＋ `city.timezone`（オフセット秒）から変換する。
- **API キー:** ソースに直書き禁止。既定はダミーデータ動作。ライブ取得は `StreamingAssets/config.json`（`.gitignore` 対象）または環境変数 `OWM_API_KEY` でオプトイン（config.json 優先 → 環境変数 → 無ければダミー）。
- **コミット:** 各タスク末尾でコミット。コミットメッセージは英語。
- **テスト方針:** 純ロジックは EditMode 単体テストで TDD。MonoBehaviour/シーン/エフェクト/UI は Play Mode 目視確認（手順を明記）。

---

## ファイル構造

実装で作成・変更するファイルと責務。

**ランタイムスクリプト（`UnityProject/Assets/Scripts/`）**

| ファイル | 責務 |
|---------|------|
| `JapanWeatherDemo.asmdef` | ランタイムアセンブリ定義（Newtonsoft 参照） |
| `Data/MapBoundsSO.cs` | 地図範囲定数（lat/lon min/max、Plane XZ 範囲）の ScriptableObject |
| `Data/GeoProjection.cs` | lat/lon → 地図 XZ の純関数（静的） |
| `Data/CityData.cs` | 都市 1 件（name/lat/lon/prefecture）の `[Serializable]` 構造体 |
| `Data/CityCatalog.cs` | `Cities.json` の読み込み・保持（`StreamingAssets` から） |
| `Data/WeatherCondition.cs` | 天気区分 enum（Clear/Cloudy/Rain/Storm/Snow） |
| `Data/WeatherSnapshot.cs` | 1 時刻分のデータ構造体 |
| `Data/WeatherTimelineSO.cs` | タイムラインデータ＋`OnSnapshotChanged` イベント |
| `Weather/OwmDto.cs` | OpenWeatherMap レスポンスの DTO（Newtonsoft デシリアライズ用） |
| `Weather/ConditionMapper.cs` | `weather.id` → `WeatherCondition` の純関数 |
| `Weather/WeatherParser.cs` | OWM JSON → `WeatherSnapshot[]`（UTC→JST・正規化込み）の純関数 |
| `Weather/DummyWeather.cs` | 同梱ダミースナップショット列の生成（オフライン/無キー用） |
| `Weather/ApiKeyResolver.cs` | config.json → 環境変数 → 無し、のキー解決（純ロジック） |
| `Weather/WeatherService.cs` | UnityWebRequest 取得・パース・メモリキャッシュ・エラー時ダミー |
| `Weather/SnapshotInterpolator.cs` | 隣接スナップショット間の線形補間（純関数） |
| `Weather/SunAngle.cs` | JST 時刻 → 太陽の高度/方位角（純関数） |
| `Weather/CloudController.cs` | Volumetric Clouds 制御（`OnSnapshotChanged` 購読） |
| `Weather/PrecipitationController.cs` | 雨・雪 VFX 制御（同上） |
| `Weather/SkyController.cs` | PhysicallyBasedSky・DirectionalLight の時間帯制御（同上） |
| `Map/MapManager.cs` | `CityCatalog` からマーカー（光柱）を生成・配置・選択管理 |
| `Map/CityMarker.cs` | マーカー 1 個。クリック判定と選択通知 |
| `Camera/FreeCameraController.cs` | 自由カメラ（Input System、回転/ズーム/パン） |
| `UI/TimelineUIController.cs` | DateTime 表示・再生・スライダー同期 |
| `UI/InfoPanelController.cs` | 都市名・気温・天気コンディション表示 |
| `UI/ToastController.cs` | エラー/警告トースト表示 |
| `GameManager.cs` | 起動時配線・初期都市（東京）選択・依存の橋渡し |

**エディタ拡張（`UnityProject/Assets/Scripts/Editor/`）**

| ファイル | 責務 |
|---------|------|
| `JapanWeatherDemo.Editor.asmdef` | エディタアセンブリ定義 |
| `CitiesJsonGenerator.cs` | アマノ技研 CSV → 抽出 → `Cities.json` 出力のエディタメニュー |

**テスト（`UnityProject/Assets/Tests/EditMode/`）**

| ファイル | 責務 |
|---------|------|
| `JapanWeatherDemo.Tests.EditMode.asmdef` | EditMode テストアセンブリ定義 |
| `GeoProjectionTests.cs` / `ConditionMapperTests.cs` / `WeatherParserTests.cs` / `TimeZoneTests.cs` / `SnapshotInterpolatorTests.cs` / `SunAngleTests.cs` / `WeatherTimelineSOTests.cs` / `ApiKeyResolverTests.cs` / `DummyWeatherTests.cs` | 各純ロジックの単体テスト |

**データ/アセット**

| パス | 内容 |
|------|------|
| `UnityProject/Assets/Scenes/MainScene.unity` | メインシーン |
| `UnityProject/Assets/StreamingAssets/Cities.json` | 都市マスタ（150〜200 都市） |
| `UnityProject/Assets/StreamingAssets/DummyForecast.json` | ダミー予報（任意・コード生成で代替可） |
| `UnityProject/Assets/StreamingAssets/config.example.json` | API キー設定サンプル（キー空欄） |
| `UnityProject/Assets/Settings/MapBounds.asset` | `MapBoundsSO` インスタンス |
| `UnityProject/Assets/Resources/Textures/JapanMap.png` | Natural Earth トリミング地図 |
| `UnityProject/Assets/Materials/JapanMap.mat` | 地図 Plane 用マテリアル |

---

## Milestone 1 — 地図 + 自由カメラ

**完了の目安:** MainScene を開いて Play すると日本地図 Plane が表示され、マウス（右ドラッグ回転・ホイールズーム・中ドラッグパン）と WASD/QE でカメラを操作できる。

### Task 1.1: アセンブリ定義とテスト基盤 ✅ 実装済み

純ロジックを TDD するための土台。ランタイム/エディタ/テストの 3 アセンブリを作り、空のサンプルテストが緑になることを確認する。

**Files:**
- Create: `UnityProject/Assets/Scripts/JapanWeatherDemo.asmdef`
- Create: `UnityProject/Assets/Scripts/Editor/JapanWeatherDemo.Editor.asmdef`
- Create: `UnityProject/Assets/Tests/EditMode/JapanWeatherDemo.Tests.EditMode.asmdef`
- Create: `UnityProject/Assets/Tests/EditMode/SmokeTest.cs`

**Interfaces:**
- Consumes: なし
- Produces: アセンブリ `JapanWeatherDemo`（ランタイム）、`JapanWeatherDemo.Editor`、`JapanWeatherDemo.Tests.EditMode`。以降の全タスクのコードはこれらに属する。

- [x] **Step 1: ランタイム asmdef を作成**

> 注: `Unity.RenderPipelines.Core.Runtime` は `UnityEngine.Rendering.Volume`（CloudController が使用）の型解決に必須。当初これが抜けて CS0246 になり追加した（commit b865aeb）。

`UnityProject/Assets/Scripts/JapanWeatherDemo.asmdef`:

```json
{
  "name": "JapanWeatherDemo",
  "rootNamespace": "JapanWeatherDemo",
  "references": [
    "Unity.InputSystem",
    "Unity.RenderPipelines.Core.Runtime",
    "Unity.RenderPipelines.HighDefinition.Runtime",
    "Unity.VisualEffectGraph.Runtime",
    "Unity.TextMeshPro"
  ],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": true,
  "precompiledReferences": [
    "Newtonsoft.Json.dll"
  ],
  "autoReferenced": true,
  "defineConstraints": [],
  "versionDefines": [],
  "noEngineReferences": false
}
```

- [x] **Step 2: エディタ asmdef を作成**

`UnityProject/Assets/Scripts/Editor/JapanWeatherDemo.Editor.asmdef`:

```json
{
  "name": "JapanWeatherDemo.Editor",
  "rootNamespace": "JapanWeatherDemo.Editor",
  "references": [
    "JapanWeatherDemo"
  ],
  "includePlatforms": [
    "Editor"
  ],
  "excludePlatforms": [],
  "overrideReferences": true,
  "precompiledReferences": [
    "Newtonsoft.Json.dll"
  ],
  "autoReferenced": true
}
```

- [x] **Step 3: テスト asmdef を作成**

`UnityProject/Assets/Tests/EditMode/JapanWeatherDemo.Tests.EditMode.asmdef`:

```json
{
  "name": "JapanWeatherDemo.Tests.EditMode",
  "rootNamespace": "JapanWeatherDemo.Tests",
  "references": [
    "JapanWeatherDemo",
    "UnityEngine.TestRunner",
    "UnityEditor.TestRunner"
  ],
  "includePlatforms": [
    "Editor"
  ],
  "excludePlatforms": [],
  "overrideReferences": true,
  "precompiledReferences": [
    "nunit.framework.dll",
    "Newtonsoft.Json.dll"
  ],
  "defineConstraints": [
    "UNITY_INCLUDE_TESTS"
  ],
  "autoReferenced": false
}
```

- [x] **Step 4: スモークテストを書く**

`UnityProject/Assets/Tests/EditMode/SmokeTest.cs`:

```csharp
using NUnit.Framework;

namespace JapanWeatherDemo.Tests
{
    public class SmokeTest
    {
        [Test]
        public void TestAssembly_Compiles_And_Runs()
        {
            Assert.AreEqual(4, 2 + 2);
        }
    }
}
```

- [x] **Step 5: テストを実行して緑を確認**

MCP for Unity を使う場合は Unity の Test Runner を実行（unity-mcp-skill 参照）。手動の場合は Unity エディタの `Window > General > Test Runner > EditMode > Run All`。
Expected: `SmokeTest.TestAssembly_Compiles_And_Runs` が PASS。コンソールにコンパイルエラーが無いこと（特に Newtonsoft 参照解決）。

- [x] **Step 6: コミット**

```bash
git add UnityProject/Assets/Scripts UnityProject/Assets/Tests
git commit -m "chore: add runtime/editor/test assembly definitions"
```

### Task 1.2: 地図範囲定数（MapBoundsSO）✅ 実装済み

地図テクスチャのトリミング範囲とマーカー配置で共有する範囲定数を ScriptableObject に集約する。値は Natural Earth テクスチャの実図郭に合わせて後で微調整するため、初期値は spec の範囲を採用。

**Files:**
- Create: `UnityProject/Assets/Scripts/Data/MapBoundsSO.cs`
- Create（エディタ操作）: `UnityProject/Assets/Settings/MapBounds.asset`

**Interfaces:**
- Consumes: なし
- Produces:
  - `JapanWeatherDemo.Data.MapBoundsSO`（ScriptableObject）
    - `float latMin, latMax, lonMin, lonMax`
    - `float planeMinX, planeMaxX, planeMinZ, planeMaxZ`
    - これらは `GeoProjection`（Task 2.1）が消費する。

- [x] **Step 1: MapBoundsSO を実装**

`UnityProject/Assets/Scripts/Data/MapBoundsSO.cs`:

```csharp
using UnityEngine;

namespace JapanWeatherDemo.Data
{
    /// <summary>
    /// 地図の緯度経度範囲とワールド XZ 範囲を 1 箇所に集約する定数アセット。
    /// テクスチャのトリミング範囲とマーカー配置でこのアセットを共有する。
    /// </summary>
    [CreateAssetMenu(fileName = "MapBounds", menuName = "JapanWeatherDemo/Map Bounds")]
    public class MapBoundsSO : ScriptableObject
    {
        [Header("緯度経度範囲（WGS84）")]
        public float latMin = 24.0f;
        public float latMax = 46.5f;
        public float lonMin = 122.0f;
        public float lonMax = 146.5f;

        [Header("地図 Plane のワールド XZ 範囲")]
        public float planeMinX = -10f;
        public float planeMaxX = 10f;
        public float planeMinZ = -10f;
        public float planeMaxZ = 10f;
    }
}
```

- [x] **Step 2: コンパイルを確認**

Unity エディタにフォーカスして再コンパイル。Expected: コンソールにエラー無し。

- [x] **Step 3: アセットを生成**

Project ウィンドウで `Assets/Settings` を右クリック → `Create > JapanWeatherDemo > Map Bounds` → 名前 `MapBounds`。
（MCP を使う場合はメニュー実行で同等の `MapBounds.asset` を作成。）
初期値はインスペクタ既定のまま（テクスチャ確定後に Task 1.3 / 2.3 で微調整）。

- [x] **Step 4: コミット**

```bash
git add UnityProject/Assets/Scripts/Data/MapBoundsSO.cs UnityProject/Assets/Settings/MapBounds.asset
git commit -m "feat: add MapBoundsSO for shared lat/lon and plane extents"
```

### Task 1.3: MainScene と日本地図 Plane ✅ 実装済み

> **実装メモ（実態）:** MCP for Unity で構築。コミット 643c075（初期構築）→ de0efe6（露出 Volume 追加・向き仮修正）→ c47aaf8（向き最終確定）。下記は実際に行った手順。

Natural Earth のパブリックドメイン地図を日本のバウンディングボックスに切り出して Plane に貼り、`MapBounds` の Plane 範囲と一致させる。

**Files:**
- Created: `UnityProject/Assets/Scenes/MainScene.unity`
- Created: `UnityProject/Assets/Materials/JapanMap.mat`（HDRP/Lit）
- Created: `UnityProject/Assets/Settings/GlobalVolume.asset`（HDRP Volume プロファイル）
- Placed: `UnityProject/Assets/Resources/Textures/JapanMap.png`（gdal で日本 bbox に切り出し済み、1470×1350、別ガイド `docs/qgis-gdal-japan-map-guide.md` 参照）
- `UnityProject/Assets/Settings/MapBounds.asset`: 既定値（lat 24〜46.5 / lon 122〜146.5）のまま（gdal `-projwin` 切り出しが厳密に一致するため調整不要）

**Interfaces:**
- Consumes: `MapBoundsSO`（Task 1.2）
- Produces: シーン階層に `JapanMapPlane`（y=0）、`Sun`、`MainCamera`、`Global Volume`。

- [x] **Step 1: 地図テクスチャ**

`docs/qgis-gdal-japan-map-guide.md` の手順で Natural Earth I（Shaded Relief and Water）を `gdal_translate -projwin 122.0 46.5 146.5 24.0` で切り出し、PNG 化して `Assets/Resources/Textures/JapanMap.png` に配置（1470×1350）。`-projwin` 切り出しなら範囲が `MapBounds` 既定値と厳密一致する。
- Import Settings: `Texture Type = Default`、`sRGB = ON`、`Max Size` 2048 以上。

- [x] **Step 2: MapBounds は既定のまま**

gdal で厳密に切り出したので `MapBounds.asset`（lat 24〜46.5 / lon 122〜146.5）の調整は不要。

- [x] **Step 3: MainScene と必須オブジェクト**

`Assets/Scenes/MainScene.unity` を新規作成し、以下を配置：
- `Sun`: Directional Light。**HDRP では `HDAdditionalLightData` を明示追加し、`intensity = 100000`（Lux）にする**（無い／弱いと描画が破綻・暗転）。rotation 例 (50, -30, 0)。
- `MainCamera`: Camera + `FreeCameraController`（Task 1.4）。**`HDAdditionalCameraData` を明示追加**。tag `MainCamera`、pos (0,18,-12)、rot (55,0,0)。
- `Global Volume`: `Volume`（isGlobal）+ プロファイル `Settings/GlobalVolume.asset`。**`Exposure` を Mode=Fixed / Fixed Exposure=EV15 にする**（無いと露出が暴れて白飛び・暗転する）。`VisualEnvironment` + `PhysicallyBasedSky` + `VolumetricClouds` は M5 で追加（それまで背景は黒）。

> **HDRP の落とし穴:** スクリプト/MCP で Light・Camera を作ると `HDAdditional*Data` が自動付与されない場合がある。明示的に AddComponent すること。

- [x] **Step 4: 地図 Plane を配置（向き補正が重要）**

- `JapanMapPlane`: Plane プリミティブ、pos (0,0,0)、**scale (2,1,2) = 20×20 ユニット**（`MapBounds` の planeMin/Max ±10 と一致）。
- マテリアル `JapanMap.mat`（HDRP/Lit、Base Map = `JapanMap.png`）を割当て。
- **地図の向き補正（必須）:** Unity の Plane UV と Plate Carrée 画像の関係で、素のまま（tiling 1,1）だと南北・東西が両方反転する。Base Map を **tiling `(-1, -1)` / offset `(1, 1)`（180°反転）** に設定して、北=+Z・東=+X（`GeoProjection` と一致）にする。

- [x] **Step 5: 向きの検証（目視＋実測）**

MainCamera の斜め俯瞰で、北=奥・朝鮮半島=左上・東京=本州太平洋側を確認。確実を期すなら、東京/札幌/那覇の `GeoProjection` 座標にデバッグ球を一時的に置き、地図上の実位置と一致するかで検証する（検証後に球は削除）。
> **MCP の注意:** 真上からの positioned-capture スクリーンショットは露出が効かず白飛びする。向き確認は MainCamera の斜め俯瞰スクショで行うこと。

- [x] **Step 6: コミット済み**

コミット 643c075 / de0efe6 / c47aaf8。

### Task 1.4: 自由カメラ（FreeCameraController）✅ 実装済み

> **実装メモ:** スクリプトはコミット b4c5430。MainCamera へのアタッチは Task 1.3 で実施済み。Play でマウス（右ドラッグ回転／ホイールズーム／中ドラッグパン）とキーボード（WASD パン／QE 高度）の操作を確認済み（良好）。

Input System で回転（右ドラッグ）・ズーム（ホイール）・パン（中ドラッグ / WASD）・高度（Q/E）を行う。マウス入力はポインターデバイスから直接読む簡易実装（専用 InputActions アセットは任意）。

**Files:**
- Create: `UnityProject/Assets/Scripts/Camera/FreeCameraController.cs`

**Interfaces:**
- Consumes: なし（`UnityEngine.Camera` にアタッチ）
- Produces: `JapanWeatherDemo.CameraControl.FreeCameraController`（MonoBehaviour）。Raycast 用途で Task 4.1 が同カメラを利用。

- [x] **Step 1: FreeCameraController を実装**

`UnityProject/Assets/Scripts/Camera/FreeCameraController.cs`:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

namespace JapanWeatherDemo.CameraControl
{
    /// <summary>
    /// Input System を用いた自由カメラ。右ドラッグでオービット回転、
    /// ホイールでズーム、中ドラッグ/WASD でパン、Q/E で高度。
    /// </summary>
    public class FreeCameraController : MonoBehaviour
    {
        [SerializeField] private float orbitSpeed = 0.2f;
        [SerializeField] private float panSpeed = 0.02f;
        [SerializeField] private float keyPanSpeed = 10f;
        [SerializeField] private float zoomSpeed = 2f;
        [SerializeField] private float altitudeSpeed = 8f;

        private void Update()
        {
            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            if (mouse == null) return;

            Vector2 mouseDelta = mouse.delta.ReadValue();

            // 右ドラッグ: オービット回転（注視点を中心に）
            if (mouse.rightButton.isPressed)
            {
                transform.RotateAround(transform.position, Vector3.up, mouseDelta.x * orbitSpeed);
                transform.RotateAround(transform.position, transform.right, -mouseDelta.y * orbitSpeed);
            }

            // 中ドラッグ: パン
            if (mouse.middleButton.isPressed)
            {
                Vector3 pan = (-transform.right * mouseDelta.x - transform.up * mouseDelta.y) * panSpeed;
                transform.position += pan;
            }

            // ホイール: ズーム（前後移動）
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                transform.position += transform.forward * Mathf.Sign(scroll) * zoomSpeed;
            }

            if (keyboard == null) return;

            // WASD: 水平パン、Q/E: 高度
            Vector3 move = Vector3.zero;
            if (keyboard.wKey.isPressed) move += transform.forward;
            if (keyboard.sKey.isPressed) move -= transform.forward;
            if (keyboard.dKey.isPressed) move += transform.right;
            if (keyboard.aKey.isPressed) move -= transform.right;
            move.y = 0f;
            if (keyboard.eKey.isPressed) move += Vector3.up;
            if (keyboard.qKey.isPressed) move -= Vector3.up;
            transform.position += move * keyPanSpeed * Time.deltaTime;
        }
    }
}
```

- [x] **Step 2: コンパイルを確認**

Expected: コンソールにエラー無し。

- [x] **Step 3: シーンに適用**

`MainCamera` に `FreeCameraController` をアタッチ。初期 `Position`/`Rotation` を地図全体が俯瞰できる位置（例 `Position=(0,18,-12)`, `Rotation=(55,0,0)`）に設定。

- [x] **Step 4: Play して操作確認**

Play し、右ドラッグで回転・ホイールでズーム・中ドラッグと WASD でパン・Q/E で上下できること。Expected: 各操作が効き、地図が画面外に飛ばない範囲で操作可能。

- [x] **Step 5: コミット**

```bash
git add UnityProject/Assets/Scripts/Camera/FreeCameraController.cs UnityProject/Assets/Scenes/MainScene.unity
git commit -m "feat: add free camera controller using Input System"
```

---

## Milestone 2 — 都市マスタ + マーカー配置

**完了の目安:** `Cities.json`（150〜200 都市）を読み込み、`GeoProjection` による lat/lon→XZ 変換で各都市の位置に光柱マーカーが正しく並ぶ。札幌が北、那覇が南西、東京が中央付近に見える。

### Task 2.1: 座標変換（GeoProjection）— TDD ✅ 実装済み

lat/lon → 地図 XZ の純関数。`MapBoundsSO` の範囲を使い、線形（等緯度経度）変換する。テスト容易性のため `MapBoundsSO` ではなくプレーンな値を引数に取るオーバーロードも用意する。

**Files:**
- Create: `UnityProject/Assets/Scripts/Data/GeoProjection.cs`
- Test: `UnityProject/Assets/Tests/EditMode/GeoProjectionTests.cs`

**Interfaces:**
- Consumes: `MapBoundsSO`（Task 1.2）
- Produces:
  - `JapanWeatherDemo.Data.GeoProjection`（静的クラス）
    - `static Vector2 LatLonToXZ(float lat, float lon, float latMin, float latMax, float lonMin, float lonMax, float planeMinX, float planeMaxX, float planeMinZ, float planeMaxZ)` — 戻り値 `(x, z)`
    - `static Vector2 LatLonToXZ(float lat, float lon, MapBoundsSO b)` — 上記に委譲
  - Task 2.3 の `MapManager` がこれを使う。

- [x] **Step 1: 失敗するテストを書く**

`UnityProject/Assets/Tests/EditMode/GeoProjectionTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;
using JapanWeatherDemo.Data;

namespace JapanWeatherDemo.Tests
{
    public class GeoProjectionTests
    {
        // 範囲: lat[20,40] lon[120,140] → plane X[-10,10] Z[-10,10]
        const float LatMin = 20f, LatMax = 40f, LonMin = 120f, LonMax = 140f;
        const float PX0 = -10f, PX1 = 10f, PZ0 = -10f, PZ1 = 10f;

        static Vector2 Project(float lat, float lon) =>
            GeoProjection.LatLonToXZ(lat, lon, LatMin, LatMax, LonMin, LonMax, PX0, PX1, PZ0, PZ1);

        [Test]
        public void SouthWestCorner_MapsTo_MinXMinZ()
        {
            Vector2 p = Project(LatMin, LonMin);
            Assert.AreEqual(-10f, p.x, 1e-4f);
            Assert.AreEqual(-10f, p.y, 1e-4f); // p.y は Z
        }

        [Test]
        public void NorthEastCorner_MapsTo_MaxXMaxZ()
        {
            Vector2 p = Project(LatMax, LonMax);
            Assert.AreEqual(10f, p.x, 1e-4f);
            Assert.AreEqual(10f, p.y, 1e-4f);
        }

        [Test]
        public void Center_MapsTo_Origin()
        {
            Vector2 p = Project(30f, 130f);
            Assert.AreEqual(0f, p.x, 1e-4f);
            Assert.AreEqual(0f, p.y, 1e-4f);
        }

        [Test]
        public void HigherLatitude_GivesLargerZ()
        {
            Assert.Less(Project(25f, 130f).y, Project(35f, 130f).y);
        }
    }
}
```

- [x] **Step 2: テストを実行して失敗を確認**

Test Runner で `GeoProjectionTests` を実行。Expected: コンパイルエラー（`GeoProjection` 未定義）で FAIL。

- [x] **Step 3: GeoProjection を実装**

`UnityProject/Assets/Scripts/Data/GeoProjection.cs`:

```csharp
using UnityEngine;

namespace JapanWeatherDemo.Data
{
    /// <summary>
    /// 緯度経度を地図 Plane 上の XZ 座標へ線形（等緯度経度）変換する純関数。
    /// 戻り値 Vector2 の x が World X、y が World Z を表す。
    /// </summary>
    public static class GeoProjection
    {
        public static Vector2 LatLonToXZ(
            float lat, float lon,
            float latMin, float latMax, float lonMin, float lonMax,
            float planeMinX, float planeMaxX, float planeMinZ, float planeMaxZ)
        {
            float x = Mathf.Lerp(planeMinX, planeMaxX, Mathf.InverseLerp(lonMin, lonMax, lon));
            float z = Mathf.Lerp(planeMinZ, planeMaxZ, Mathf.InverseLerp(latMin, latMax, lat));
            return new Vector2(x, z);
        }

        public static Vector2 LatLonToXZ(float lat, float lon, MapBoundsSO b)
        {
            return LatLonToXZ(lat, lon,
                b.latMin, b.latMax, b.lonMin, b.lonMax,
                b.planeMinX, b.planeMaxX, b.planeMinZ, b.planeMaxZ);
        }
    }
}
```

- [x] **Step 4: テストを実行して緑を確認**

Expected: `GeoProjectionTests` の 4 テストが PASS。

- [x] **Step 5: コミット**

```bash
git add UnityProject/Assets/Scripts/Data/GeoProjection.cs UnityProject/Assets/Tests/EditMode/GeoProjectionTests.cs
git commit -m "feat: add GeoProjection lat/lon to XZ with tests"
```

### Task 2.2: 都市データ構造と Cities.json 生成 ✅ 実装済み

`CityData` 構造体と、アマノ技研 CSV から都市を抽出して `Cities.json` を出力するエディタ拡張、そして実行時にそれを読む `CityCatalog`。

> **実装メモ（実態）:**
> - Step 1〜4（`CityData` / `CityCatalog` / `CityCatalogTests`）は計画どおり実装（コミット **44f635e**、テスト 2/2 緑）。
> - Step 5〜8（`CitiesJsonGenerator` と `Cities.json`）は、**実データ（アマノ技研 `r0801puboffice_utf8.csv`）が計画の想定（カンマ区切り・prefecture 列あり）と違ったため作り直した**（コミット **7cea1be**、**145 都市**生成）。実コードは `Assets/Scripts/Editor/CitiesJsonGenerator.cs` を参照。実態の要点：
>   - **タブ区切り（TSV）**。列は `[0]jiscode [1]name [2]namekana [3]building [4]zipcode [5]address [6]tel [7]source [8]lat [9]long [10]note`。
>   - **prefecture 列は無い** → `jiscode / 1000` を都道府県コードにしてマップで都道府県名を導出。
>   - **東京特例**: 市が無いので東京都庁（jiscode 13000）を `name="東京"` として採用。
>   - 県庁所在地＋政令市＋中核市＋主要市の**ホワイトリスト**に一致する市の本庁行のみ採用（政令市の「区」行は名前不一致で自動除外）。
>   - 入力 CSV は `download_resources/r0801puboffice_utf8.csv` 固定パス（ダイアログ無しでメニュー `JapanWeatherDemo/Generate Cities.json from amano CSV` から実行）。`download_resources/` は `.gitignore` 済み。
>   - 座標は地理院/数値地図由来（JGD2011≒WGS84）でそのまま使用（変換不要）。
> - **下記 Step 5 の逐語コードは当初案**（カンマ区切り・prefecture 列前提）であり、実装では使っていない。参考として残す。

**Files:**
- Create: `UnityProject/Assets/Scripts/Data/CityData.cs`
- Create: `UnityProject/Assets/Scripts/Data/CityCatalog.cs`
- Create: `UnityProject/Assets/Scripts/Editor/CitiesJsonGenerator.cs`
- Create（生成物）: `UnityProject/Assets/StreamingAssets/Cities.json`
- Test: `UnityProject/Assets/Tests/EditMode/CityCatalogTests.cs`

**Interfaces:**
- Consumes: なし
- Produces:
  - `JapanWeatherDemo.Data.CityData`（`[Serializable]`）: `public string name; public float lat; public float lon; public string prefecture;`
  - `JapanWeatherDemo.Data.CityCatalog`（静的）: `static List<CityData> LoadFromJson(string json)` と `static List<CityData> LoadFromStreamingAssets(string fileName = "Cities.json")`
  - Task 2.3 `MapManager` と Task 7（初期都市検索）が利用。

- [x] **Step 1: CityData と失敗するテストを書く**

`UnityProject/Assets/Scripts/Data/CityData.cs`:

```csharp
using System;

namespace JapanWeatherDemo.Data
{
    [Serializable]
    public struct CityData
    {
        public string name;
        public float lat;
        public float lon;
        public string prefecture;
    }
}
```

`UnityProject/Assets/Tests/EditMode/CityCatalogTests.cs`:

```csharp
using NUnit.Framework;
using System.Collections.Generic;
using JapanWeatherDemo.Data;

namespace JapanWeatherDemo.Tests
{
    public class CityCatalogTests
    {
        [Test]
        public void LoadFromJson_ParsesArray()
        {
            string json = "[{\"name\":\"東京\",\"lat\":35.6895,\"lon\":139.6917,\"prefecture\":\"東京都\"}," +
                          "{\"name\":\"札幌\",\"lat\":43.0642,\"lon\":141.3468,\"prefecture\":\"北海道\"}]";
            List<CityData> cities = CityCatalog.LoadFromJson(json);
            Assert.AreEqual(2, cities.Count);
            Assert.AreEqual("東京", cities[0].name);
            Assert.AreEqual(35.6895f, cities[0].lat, 1e-4f);
            Assert.AreEqual("北海道", cities[1].prefecture);
        }

        [Test]
        public void LoadFromJson_EmptyArray_ReturnsEmptyList()
        {
            Assert.AreEqual(0, CityCatalog.LoadFromJson("[]").Count);
        }
    }
}
```

- [x] **Step 2: テストを実行して失敗を確認**

Expected: `CityCatalog` 未定義で FAIL。

- [x] **Step 3: CityCatalog を実装**

`UnityProject/Assets/Scripts/Data/CityCatalog.cs`:

```csharp
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace JapanWeatherDemo.Data
{
    /// <summary>都市マスタ（Cities.json）の読み込み。</summary>
    public static class CityCatalog
    {
        public static List<CityData> LoadFromJson(string json)
        {
            var list = JsonConvert.DeserializeObject<List<CityData>>(json);
            return list ?? new List<CityData>();
        }

        public static List<CityData> LoadFromStreamingAssets(string fileName = "Cities.json")
        {
            string path = Path.Combine(Application.streamingAssetsPath, fileName);
            if (!File.Exists(path))
            {
                Debug.LogError($"[CityCatalog] not found: {path}");
                return new List<CityData>();
            }
            return LoadFromJson(File.ReadAllText(path));
        }
    }
}
```

- [x] **Step 4: テストを実行して緑を確認**

Expected: `CityCatalogTests` の 2 テストが PASS。

- [x] **Step 5: 生成スクリプト（CitiesJsonGenerator）を実装**

`UnityProject/Assets/Scripts/Editor/CitiesJsonGenerator.cs`:

```csharp
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using JapanWeatherDemo.Data;

namespace JapanWeatherDemo.Editor
{
    /// <summary>
    /// アマノ技研「地方公共団体の位置データ」CSV から
    /// 県庁所在地＋政令指定都市＋県内主要都市を抽出して Cities.json を出力する。
    /// CSV の列構成は取り込み時に確認し、下の列インデックス定数を合わせること。
    /// </summary>
    public static class CitiesJsonGenerator
    {
        // ▼ CSV を開いて実際の列順に合わせる（取り込み時に確認する細部）
        const int ColName = 2;        // 自治体名
        const int ColPrefecture = 1;  // 都道府県名
        const int ColLat = 6;         // 緯度（10進・WGS84 を想定。日本測地系なら要変換）
        const int ColLon = 7;         // 経度
        const bool HasHeader = true;

        [MenuItem("JapanWeatherDemo/Generate Cities.json from CSV...")]
        public static void Generate()
        {
            string csvPath = EditorUtility.OpenFilePanel("アマノ技研 CSV を選択", "", "csv");
            if (string.IsNullOrEmpty(csvPath)) return;

            // 含める都道府県の代表都市（県庁所在地）。政令市・主要都市は名前一致で追加採用。
            var capitals = PrefectureCapitals();          // prefecture -> capital city name
            var extraMajors = ExtraMajorCities();          // 追加で含める主要都市名

            var lines = File.ReadAllLines(csvPath);
            var selected = new List<CityData>();
            var seen = new HashSet<string>();

            foreach (var line in lines.Skip(HasHeader ? 1 : 0))
            {
                var cols = SplitCsv(line);
                if (cols.Count <= Mathf.Max(ColName, Mathf.Max(ColLat, ColLon))) continue;

                string name = cols[ColName].Trim();
                string pref = cols[ColPrefecture].Trim();
                if (!float.TryParse(cols[ColLat], NumberStyles.Float, CultureInfo.InvariantCulture, out float lat)) continue;
                if (!float.TryParse(cols[ColLon], NumberStyles.Float, CultureInfo.InvariantCulture, out float lon)) continue;

                bool isCapital = capitals.TryGetValue(pref, out string cap) && name.StartsWith(cap);
                bool isMajor = extraMajors.Contains(name);
                if (!isCapital && !isMajor) continue;

                string key = pref + "|" + name;
                if (!seen.Add(key)) continue;
                selected.Add(new CityData { name = name, lat = lat, lon = lon, prefecture = pref });
            }

            // 150〜200 件に収まらない場合は extraMajors を調整する。
            Debug.Log($"[CitiesJsonGenerator] selected {selected.Count} cities");

            string outPath = Path.Combine(Application.streamingAssetsPath, "Cities.json");
            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            File.WriteAllText(outPath, JsonConvert.SerializeObject(selected, Formatting.Indented));
            AssetDatabase.Refresh();
            Debug.Log($"[CitiesJsonGenerator] wrote {outPath}");
        }

        static List<string> SplitCsv(string line)
        {
            // 簡易 CSV 分割（引用符なしの素データ前提）。引用符付きなら適宜拡張。
            return line.Split(',').ToList();
        }

        static Dictionary<string, string> PrefectureCapitals()
        {
            // 47 都道府県の県庁所在地。北海道=札幌、東京都=東京（新宿区）等。
            return new Dictionary<string, string>
            {
                {"北海道","札幌"},{"青森県","青森"},{"岩手県","盛岡"},{"宮城県","仙台"},
                {"秋田県","秋田"},{"山形県","山形"},{"福島県","福島"},{"茨城県","水戸"},
                {"栃木県","宇都宮"},{"群馬県","前橋"},{"埼玉県","さいたま"},{"千葉県","千葉"},
                {"東京都","新宿"},{"神奈川県","横浜"},{"新潟県","新潟"},{"富山県","富山"},
                {"石川県","金沢"},{"福井県","福井"},{"山梨県","甲府"},{"長野県","長野"},
                {"岐阜県","岐阜"},{"静岡県","静岡"},{"愛知県","名古屋"},{"三重県","津"},
                {"滋賀県","大津"},{"京都府","京都"},{"大阪府","大阪"},{"兵庫県","神戸"},
                {"奈良県","奈良"},{"和歌山県","和歌山"},{"鳥取県","鳥取"},{"島根県","松江"},
                {"岡山県","岡山"},{"広島県","広島"},{"山口県","山口"},{"徳島県","徳島"},
                {"香川県","高松"},{"愛媛県","松山"},{"高知県","高知"},{"福岡県","福岡"},
                {"佐賀県","佐賀"},{"長崎県","長崎"},{"熊本県","熊本"},{"大分県","大分"},
                {"宮崎県","宮崎"},{"鹿児島県","鹿児島"},{"沖縄県","那覇"},
            };
        }

        static HashSet<string> ExtraMajorCities()
        {
            // 政令指定都市・県内主要都市・観光拠点など（150〜200 件に調整するためのホワイトリスト）。
            return new HashSet<string>
            {
                "函館","旭川","釧路","八戸","郡山","いわき","つくば","高崎","川越","船橋","柏",
                "八王子","町田","川崎","相模原","藤沢","長岡","上越","松本","豊橋","岡崎","一宮",
                "四日市","堺","東大阪","姫路","西宮","尼崎","和歌山","倉敷","福山","下関","北九州",
                "久留米","佐世保","うるま","石垣","浜松","沼津","日光"
            };
        }
    }
}
```

- [x] **Step 6: CSV を取り込み Cities.json を生成**

1. アマノ技研「地方公共団体の位置データ」CSV（https://amano-tec.com/data/localgovernments.html ・商用無料・出典表記不要）をダウンロード。
2. CSV を開き、列順（名称・都道府県・緯度・経度）を確認して `CitiesJsonGenerator` の `Col*` 定数を実データに合わせる。**測地系（WGS84 か日本測地系か）も確認し、日本測地系なら Tokyo→WGS84 変換を追加する。**
3. Unity メニュー `JapanWeatherDemo > Generate Cities.json from CSV...` を実行し CSV を選択。
4. コンソールの `selected N cities` が 150〜200 に収まるよう `ExtraMajorCities()` を調整して再実行（`horizontal_placeholder` は削除し、実在の主要都市名に置き換える）。

- [x] **Step 7: 生成物を目視確認**

`Assets/StreamingAssets/Cities.json` を開き、東京・札幌・那覇・大阪などの座標が妥当（東京 lat≈35.69, lon≈139.69）であること。

- [x] **Step 8: コミット**

```bash
git add UnityProject/Assets/Scripts/Data/CityData.cs UnityProject/Assets/Scripts/Data/CityCatalog.cs UnityProject/Assets/Tests/EditMode/CityCatalogTests.cs UnityProject/Assets/Scripts/Editor/CitiesJsonGenerator.cs UnityProject/Assets/StreamingAssets/Cities.json
git commit -m "feat: add city data model, catalog loader and Cities.json generator"
```

### Task 2.3: マーカー配置（MapManager / CityMarker）✅ 実装済み（プレハブ＋シーン配置＋Play 確認済み commit b480555、MCP 構築、emissive 5000/20000 nits）

`Cities.json` を読み、`GeoProjection` で各都市の XZ を求めて光柱マーカーを配置する。クリック判定用コライダーを持たせる。選択状態の見た目強調は本タスクで API を用意し、Task 4 で配線する。

**Files:**
- Create: `UnityProject/Assets/Scripts/Map/CityMarker.cs`
- Create: `UnityProject/Assets/Scripts/Map/MapManager.cs`
- Create（プレハブ）: `UnityProject/Assets/Prefabs/CityMarker.prefab`
- Modify: `UnityProject/Assets/Scenes/MainScene.unity`（`MapManager` 配置）

**Interfaces:**
- Consumes: `CityCatalog`, `GeoProjection`, `MapBoundsSO`
- Produces:
  - `JapanWeatherDemo.Map.CityMarker`（MonoBehaviour）:
    - `CityData City { get; }`
    - `void Init(CityData city)`
    - `void SetSelected(bool selected)` — 光柱の色/強度を切り替え
    - `event System.Action<CityMarker> Clicked`
  - `JapanWeatherDemo.Map.MapManager`（MonoBehaviour）:
    - `IReadOnlyList<CityMarker> Markers { get; }`
    - `event System.Action<CityData> CitySelected`
    - `void SelectByName(string cityName)`
  - Task 4.1 が `MapManager.CitySelected` を購読し、`SelectByName` で初期都市（東京）を選ぶ。

- [x] **Step 1: CityMarker を実装**

`UnityProject/Assets/Scripts/Map/CityMarker.cs`:

```csharp
using UnityEngine;
using JapanWeatherDemo.Data;

namespace JapanWeatherDemo.Map
{
    /// <summary>地図上の 1 都市を表す光柱マーカー。クリックで選択を通知する。</summary>
    [RequireComponent(typeof(Collider))]
    public class CityMarker : MonoBehaviour
    {
        [SerializeField] private Light beamLight;          // 光柱の核となるライト（任意）
        [SerializeField] private Renderer beamRenderer;    // 光柱メッシュ（円柱など）

        public CityData City { get; private set; }
        public event System.Action<CityMarker> Clicked;

        private Color baseColor = new Color(0.4f, 0.7f, 1f);
        private Color selectedColor = new Color(1f, 0.85f, 0.3f);

        public void Init(CityData city)
        {
            City = city;
            name = $"Marker_{city.name}";
            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            Color c = selected ? selectedColor : baseColor;
            float emission = selected ? 6f : 2f;
            // 全都市は Emissive メッシュで表現する
            if (beamRenderer != null)
                beamRenderer.material.SetColor("_EmissiveColor", c * emission);
            // リアルタイム Light は選択都市のみ有効化する（HDRP で 150〜200 個の常時ライトは負荷が高いため）
            if (beamLight != null)
            {
                beamLight.enabled = selected;
                beamLight.color = c;
                beamLight.intensity = 8f;
            }
        }

        // MapManager から Raycast ヒット時に呼ばれる
        public void NotifyClicked() => Clicked?.Invoke(this);
    }
}
```

- [x] **Step 2: MapManager を実装**

`UnityProject/Assets/Scripts/Map/MapManager.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using JapanWeatherDemo.Data;

namespace JapanWeatherDemo.Map
{
    /// <summary>都市マスタを読み、光柱マーカーを配置・選択管理する。</summary>
    public class MapManager : MonoBehaviour
    {
        [SerializeField] private MapBoundsSO bounds;
        [SerializeField] private CityMarker markerPrefab;
        [SerializeField] private Transform markerParent;
        [SerializeField] private float markerY = 0.1f;
        [SerializeField] private UnityEngine.Camera raycastCamera;

        private readonly List<CityMarker> markers = new();
        private CityMarker selected;

        public IReadOnlyList<CityMarker> Markers => markers;
        public event System.Action<CityData> CitySelected;

        private void Awake()
        {
            if (raycastCamera == null) raycastCamera = UnityEngine.Camera.main;
            BuildMarkers();
        }

        private void BuildMarkers()
        {
            var cities = CityCatalog.LoadFromStreamingAssets();
            foreach (var city in cities)
            {
                Vector2 xz = GeoProjection.LatLonToXZ(city.lat, city.lon, bounds);
                var marker = Instantiate(markerPrefab, markerParent);
                marker.transform.position = new Vector3(xz.x, markerY, xz.y);
                marker.Init(city);
                marker.Clicked += OnMarkerClicked;
                markers.Add(marker);
            }
        }

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

            // UI（タイムライン・情報パネル等）の上では地図クリックを無視する
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

            Vector2 screen = mouse.position.ReadValue();
            Ray ray = raycastCamera.ScreenPointToRay(screen);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                var marker = hit.collider.GetComponentInParent<CityMarker>();
                if (marker != null) marker.NotifyClicked();
            }
        }

        private void OnMarkerClicked(CityMarker marker) => Select(marker);

        public void SelectByName(string cityName)
        {
            var m = markers.Find(x => x.City.name == cityName);
            if (m != null) Select(m);
        }

        private void Select(CityMarker marker)
        {
            if (selected != null) selected.SetSelected(false);
            selected = marker;
            selected.SetSelected(true);
            CitySelected?.Invoke(marker.City);
        }
    }
}
```

- [x] **Step 3: コンパイルを確認**

Expected: コンソールにエラー無し。

- [x] **Step 4: 光柱マーカーのプレハブを作る**

- 空 GameObject `CityMarker` を作成し `CityMarker.cs` をアタッチ。
- 子に細い `Cylinder`（Scale 例 `(0.05, 1.5, 0.05)`、`Position.y` を半分上げる）を置き、HDRP/Lit のエミッシブマテリアルを割当て → `beamRenderer` に設定。`Collider` はクリック判定用に Cylinder の Capsule/Box でよい（`CityMarker` の `RequireComponent(Collider)` を満たすようルート or 子に付与し、`GetComponentInParent` で拾えるようにする）。
- 子に `Light`（Spot/Point、上向き）を追加 → `beamLight` に設定し、**初期状態は `enabled = false`**（選択時のみ有効化される）。
- `Assets/Prefabs/CityMarker.prefab` として保存。

- [x] **Step 5: シーンに MapManager を配置**

- 空 GameObject `MapManager` を作成し `MapManager.cs` をアタッチ。
- `bounds` に `MapBounds.asset`、`markerPrefab` に `CityMarker.prefab`、`raycastCamera` に `MainCamera` を設定。
- 子に空 GameObject `CityMarkers` を作り `markerParent` に設定（シーン階層 `[Map]/CityMarkers`）。

- [x] **Step 6: Play して配置を目視確認**

Play し、150〜200 本の光柱が日本地図上の正しい位置に並ぶこと。札幌が北、那覇が南西、東京が中央付近。Expected: マーカーが海上にずれていない（ずれる場合は `MapBounds` の範囲とテクスチャ図郭・Plane 実寸の不一致を疑う）。

- [x] **Step 7: コミット**

```bash
git add UnityProject/Assets/Scripts/Map UnityProject/Assets/Prefabs/CityMarker.prefab UnityProject/Assets/Scenes/MainScene.unity
git commit -m "feat: place city beam markers from catalog via GeoProjection"
```

---

## Milestone 3 — API 取得 + データ層

**完了の目安:** 指定都市の lat/lon から OpenWeatherMap の 5 日間予報を取得して `WeatherSnapshot[]`（40 点・JST）にパースし `WeatherTimelineSO` に格納できる。キー未設定・通信失敗時は同梱ダミーデータで成立する。純ロジックはすべて EditMode テストが緑。

### Task 3.1: 天気区分とスナップショット構造 ✅ 実装済み

**Files:**
- Create: `UnityProject/Assets/Scripts/Data/WeatherCondition.cs`
- Create: `UnityProject/Assets/Scripts/Data/WeatherSnapshot.cs`

**Interfaces:**
- Consumes: なし
- Produces:
  - `JapanWeatherDemo.Data.WeatherCondition`（enum）: `Clear, Cloudy, Rain, Storm, Snow`
  - `JapanWeatherDemo.Data.WeatherSnapshot`（`[Serializable]` struct）: `DateTime dateTime; WeatherCondition condition; float cloudCoverage; float windSpeed; float windDirectionDeg; float rainIntensity; float temperatureCelsius;`
  - Task 3.2〜3.7、Milestone 5/6 が消費。

- [x] **Step 1: WeatherCondition を実装**

`UnityProject/Assets/Scripts/Data/WeatherCondition.cs`:

```csharp
namespace JapanWeatherDemo.Data
{
    public enum WeatherCondition
    {
        Clear,
        Cloudy,
        Rain,
        Storm,
        Snow
    }
}
```

- [x] **Step 2: WeatherSnapshot を実装**

`UnityProject/Assets/Scripts/Data/WeatherSnapshot.cs`:

```csharp
using System;

namespace JapanWeatherDemo.Data
{
    [Serializable]
    public struct WeatherSnapshot
    {
        public DateTime dateTime;          // JST（UTC+9 に変換済み）
        public WeatherCondition condition; // Clear / Cloudy / Rain / Storm / Snow
        public float cloudCoverage;        // 0〜1
        public float windSpeed;            // m/s
        public float windDirectionDeg;     // 0〜360
        public float rainIntensity;        // 0〜1
        public float temperatureCelsius;
    }
}
```

- [x] **Step 3: コンパイルを確認**

Expected: コンソールにエラー無し。

- [x] **Step 4: コミット**

```bash
git add UnityProject/Assets/Scripts/Data/WeatherCondition.cs UnityProject/Assets/Scripts/Data/WeatherSnapshot.cs
git commit -m "feat: add WeatherCondition enum and WeatherSnapshot struct"
```

### Task 3.2: 天気コード分類（ConditionMapper）— TDD ✅ 実装済み

OpenWeatherMap の `weather[0].id` を 5 区分にマッピングする純関数。

**Files:**
- Create: `UnityProject/Assets/Scripts/Weather/ConditionMapper.cs`
- Test: `UnityProject/Assets/Tests/EditMode/ConditionMapperTests.cs`

**Interfaces:**
- Consumes: `WeatherCondition`
- Produces: `JapanWeatherDemo.Weather.ConditionMapper.FromOwmId(int id) -> WeatherCondition`
  - Task 3.3 `WeatherParser` が使う。

- [x] **Step 1: 失敗するテストを書く**

`UnityProject/Assets/Tests/EditMode/ConditionMapperTests.cs`:

```csharp
using NUnit.Framework;
using JapanWeatherDemo.Data;
using JapanWeatherDemo.Weather;

namespace JapanWeatherDemo.Tests
{
    public class ConditionMapperTests
    {
        [TestCase(200, WeatherCondition.Storm)]   // 2xx 雷雨
        [TestCase(232, WeatherCondition.Storm)]
        [TestCase(300, WeatherCondition.Rain)]    // 3xx 霧雨
        [TestCase(500, WeatherCondition.Rain)]    // 5xx 雨
        [TestCase(531, WeatherCondition.Rain)]
        [TestCase(600, WeatherCondition.Snow)]    // 6xx 雪
        [TestCase(622, WeatherCondition.Snow)]
        [TestCase(701, WeatherCondition.Cloudy)]  // 7xx 大気現象
        [TestCase(781, WeatherCondition.Cloudy)]
        [TestCase(800, WeatherCondition.Clear)]   // 快晴
        [TestCase(801, WeatherCondition.Cloudy)]  // 雲量あり
        [TestCase(804, WeatherCondition.Cloudy)]
        public void FromOwmId_MapsToExpected(int id, WeatherCondition expected)
        {
            Assert.AreEqual(expected, ConditionMapper.FromOwmId(id));
        }
    }
}
```

- [x] **Step 2: テストを実行して失敗を確認**

Expected: `ConditionMapper` 未定義で FAIL。

- [x] **Step 3: ConditionMapper を実装**

`UnityProject/Assets/Scripts/Weather/ConditionMapper.cs`:

```csharp
using JapanWeatherDemo.Data;

namespace JapanWeatherDemo.Weather
{
    /// <summary>OpenWeatherMap weather[0].id を WeatherCondition 5 区分へ分類する。</summary>
    public static class ConditionMapper
    {
        public static WeatherCondition FromOwmId(int id)
        {
            if (id >= 200 && id < 300) return WeatherCondition.Storm;
            if (id >= 300 && id < 600) return WeatherCondition.Rain;   // 3xx 霧雨 + 5xx 雨
            if (id >= 600 && id < 700) return WeatherCondition.Snow;
            if (id >= 700 && id < 800) return WeatherCondition.Cloudy;  // 大気現象
            if (id == 800) return WeatherCondition.Clear;
            if (id > 800 && id < 900) return WeatherCondition.Cloudy;   // 80x 雲量あり
            return WeatherCondition.Cloudy;                             // 未知は曇り扱い
        }
    }
}
```

- [x] **Step 4: テストを実行して緑を確認**

Expected: `ConditionMapperTests` の全ケースが PASS。

- [x] **Step 5: コミット**

```bash
git add UnityProject/Assets/Scripts/Weather/ConditionMapper.cs UnityProject/Assets/Tests/EditMode/ConditionMapperTests.cs
git commit -m "feat: add ConditionMapper from OWM weather id with tests"
```

### Task 3.3: UTC→JST 変換（TimeZoneUtil）— TDD ✅ 実装済み

API の `dt`（UTC エポック秒）と `city.timezone`（オフセット秒）から JST の `DateTime` を作る純関数。日本向けデモのため固定で UTC+9 にする方針だが、`timezone` を尊重しつつ JST 表示へ正規化する。

**Files:**
- Create: `UnityProject/Assets/Scripts/Weather/TimeZoneUtil.cs`
- Test: `UnityProject/Assets/Tests/EditMode/TimeZoneTests.cs`

**Interfaces:**
- Consumes: なし
- Produces:
  - `JapanWeatherDemo.Weather.TimeZoneUtil.UnixUtcToJst(long unixSeconds) -> DateTime`（Kind=Unspecified、UTC+9 加算済み）
  - Task 3.3b `WeatherParser` が使う。

- [x] **Step 1: 失敗するテストを書く**

`UnityProject/Assets/Tests/EditMode/TimeZoneTests.cs`:

```csharp
using NUnit.Framework;
using System;
using JapanWeatherDemo.Weather;

namespace JapanWeatherDemo.Tests
{
    public class TimeZoneTests
    {
        [Test]
        public void UnixUtcToJst_AddsNineHours()
        {
            // 2024-01-01T00:00:00Z = 1704067200。JST では 09:00。
            DateTime jst = TimeZoneUtil.UnixUtcToJst(1704067200L);
            Assert.AreEqual(2024, jst.Year);
            Assert.AreEqual(1, jst.Month);
            Assert.AreEqual(1, jst.Day);
            Assert.AreEqual(9, jst.Hour);
            Assert.AreEqual(0, jst.Minute);
        }

        [Test]
        public void UnixUtcToJst_CrossesDateBoundary()
        {
            // 2024-01-01T20:00:00Z = 1704139200。JST では 翌 05:00。
            DateTime jst = TimeZoneUtil.UnixUtcToJst(1704139200L);
            Assert.AreEqual(2, jst.Day);
            Assert.AreEqual(5, jst.Hour);
        }
    }
}
```

- [x] **Step 2: テストを実行して失敗を確認**

Expected: `TimeZoneUtil` 未定義で FAIL。

- [x] **Step 3: TimeZoneUtil を実装**

`UnityProject/Assets/Scripts/Weather/TimeZoneUtil.cs`:

```csharp
using System;

namespace JapanWeatherDemo.Weather
{
    /// <summary>UNIX UTC 秒を JST(UTC+9) の DateTime に変換する。</summary>
    public static class TimeZoneUtil
    {
        const int JstOffsetHours = 9;

        public static DateTime UnixUtcToJst(long unixSeconds)
        {
            DateTime utc = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;
            return DateTime.SpecifyKind(utc.AddHours(JstOffsetHours), DateTimeKind.Unspecified);
        }
    }
}
```

- [x] **Step 4: テストを実行して緑を確認**

Expected: `TimeZoneTests` の 2 テストが PASS。

- [x] **Step 5: コミット**

```bash
git add UnityProject/Assets/Scripts/Weather/TimeZoneUtil.cs UnityProject/Assets/Tests/EditMode/TimeZoneTests.cs
git commit -m "feat: add UTC to JST conversion with tests"
```

### Task 3.4: レスポンス DTO とパース（OwmDto / WeatherParser）— TDD ✅ 実装済み

OWM forecast JSON を Newtonsoft で DTO にデシリアライズし、`WeatherSnapshot[]` に変換する。`cloudCoverage` は `clouds.all/100`、`rainIntensity` は `rain.3h` を 10mm/3h=1.0 でクランプ正規化。

**Files:**
- Create: `UnityProject/Assets/Scripts/Weather/OwmDto.cs`
- Create: `UnityProject/Assets/Scripts/Weather/WeatherParser.cs`
- Test: `UnityProject/Assets/Tests/EditMode/WeatherParserTests.cs`

**Interfaces:**
- Consumes: `WeatherSnapshot`, `ConditionMapper`, `TimeZoneUtil`
- Produces:
  - `JapanWeatherDemo.Weather.WeatherParser.Parse(string json) -> WeatherSnapshot[]`
  - `JapanWeatherDemo.Weather.WeatherParser.RainMaxMmPer3h = 10f`（正規化上限・定数）
  - Task 3.6 `WeatherService` が使う。

- [x] **Step 1: 失敗するテストを書く**

`UnityProject/Assets/Tests/EditMode/WeatherParserTests.cs`:

```csharp
using NUnit.Framework;
using JapanWeatherDemo.Data;
using JapanWeatherDemo.Weather;

namespace JapanWeatherDemo.Tests
{
    public class WeatherParserTests
    {
        const string Json = @"{
          ""list"": [
            { ""dt"": 1704067200, ""main"": { ""temp"": 5.5 },
              ""weather"": [ { ""id"": 800 } ], ""clouds"": { ""all"": 10 },
              ""wind"": { ""speed"": 3.2, ""deg"": 90 } },
            { ""dt"": 1704078000, ""main"": { ""temp"": 7.0 },
              ""weather"": [ { ""id"": 500 } ], ""clouds"": { ""all"": 80 },
              ""wind"": { ""speed"": 5.0, ""deg"": 180 }, ""rain"": { ""3h"": 5.0 } }
          ],
          ""city"": { ""name"": ""Tokyo"", ""timezone"": 32400 }
        }";

        [Test]
        public void Parse_ReturnsAllSnapshots()
        {
            var snaps = WeatherParser.Parse(Json);
            Assert.AreEqual(2, snaps.Length);
        }

        [Test]
        public void Parse_MapsConditionAndCloud()
        {
            var snaps = WeatherParser.Parse(Json);
            Assert.AreEqual(WeatherCondition.Clear, snaps[0].condition);
            Assert.AreEqual(0.1f, snaps[0].cloudCoverage, 1e-4f);
            Assert.AreEqual(WeatherCondition.Rain, snaps[1].condition);
            Assert.AreEqual(0.8f, snaps[1].cloudCoverage, 1e-4f);
        }

        [Test]
        public void Parse_NormalizesRain_AndDefaultsZero()
        {
            var snaps = WeatherParser.Parse(Json);
            Assert.AreEqual(0f, snaps[0].rainIntensity, 1e-4f);     // rain 無し → 0
            Assert.AreEqual(0.5f, snaps[1].rainIntensity, 1e-4f);   // 5mm/3h ÷ 10 = 0.5
        }

        [Test]
        public void Parse_ConvertsToJst()
        {
            var snaps = WeatherParser.Parse(Json);
            Assert.AreEqual(9, snaps[0].dateTime.Hour);  // 00:00Z → 09:00 JST
        }

        [Test]
        public void Parse_CopiesWindAndTemp()
        {
            var snaps = WeatherParser.Parse(Json);
            Assert.AreEqual(3.2f, snaps[0].windSpeed, 1e-4f);
            Assert.AreEqual(90f, snaps[0].windDirectionDeg, 1e-4f);
            Assert.AreEqual(5.5f, snaps[0].temperatureCelsius, 1e-4f);
        }
    }
}
```

- [x] **Step 2: テストを実行して失敗を確認**

Expected: `OwmDto` / `WeatherParser` 未定義で FAIL。

- [x] **Step 3: OwmDto を実装**

`UnityProject/Assets/Scripts/Weather/OwmDto.cs`:

```csharp
using System.Collections.Generic;
using Newtonsoft.Json;

namespace JapanWeatherDemo.Weather
{
    /// <summary>OpenWeatherMap /forecast レスポンスのデシリアライズ用 DTO。</summary>
    public class OwmForecastResponse
    {
        [JsonProperty("list")] public List<OwmListItem> List { get; set; }
        [JsonProperty("city")] public OwmCity City { get; set; }
    }

    public class OwmCity
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("timezone")] public int Timezone { get; set; }
    }

    public class OwmListItem
    {
        [JsonProperty("dt")] public long Dt { get; set; }
        [JsonProperty("main")] public OwmMain Main { get; set; }
        [JsonProperty("weather")] public List<OwmWeather> Weather { get; set; }
        [JsonProperty("clouds")] public OwmClouds Clouds { get; set; }
        [JsonProperty("wind")] public OwmWind Wind { get; set; }
        [JsonProperty("rain")] public OwmRain Rain { get; set; }
    }

    public class OwmMain { [JsonProperty("temp")] public float Temp { get; set; } }
    public class OwmWeather { [JsonProperty("id")] public int Id { get; set; } }
    public class OwmClouds { [JsonProperty("all")] public float All { get; set; } }
    public class OwmWind { [JsonProperty("speed")] public float Speed { get; set; } [JsonProperty("deg")] public float Deg { get; set; } }
    public class OwmRain { [JsonProperty("3h")] public float ThreeHour { get; set; } }
}
```

- [x] **Step 4: WeatherParser を実装**

`UnityProject/Assets/Scripts/Weather/WeatherParser.cs`:

```csharp
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using JapanWeatherDemo.Data;

namespace JapanWeatherDemo.Weather
{
    /// <summary>OWM forecast JSON を WeatherSnapshot[] に変換する純関数。</summary>
    public static class WeatherParser
    {
        public const float RainMaxMmPer3h = 10f;

        public static WeatherSnapshot[] Parse(string json)
        {
            var res = JsonConvert.DeserializeObject<OwmForecastResponse>(json);
            if (res?.List == null) return new WeatherSnapshot[0];

            var snaps = new List<WeatherSnapshot>(res.List.Count);
            foreach (var item in res.List)
            {
                int id = (item.Weather != null && item.Weather.Count > 0) ? item.Weather[0].Id : 800;
                float rain3h = item.Rain != null ? item.Rain.ThreeHour : 0f;

                snaps.Add(new WeatherSnapshot
                {
                    dateTime = TimeZoneUtil.UnixUtcToJst(item.Dt),
                    condition = ConditionMapper.FromOwmId(id),
                    cloudCoverage = Mathf.Clamp01((item.Clouds?.All ?? 0f) / 100f),
                    windSpeed = item.Wind?.Speed ?? 0f,
                    windDirectionDeg = item.Wind?.Deg ?? 0f,
                    rainIntensity = Mathf.Clamp01(rain3h / RainMaxMmPer3h),
                    temperatureCelsius = item.Main?.Temp ?? 0f
                });
            }
            return snaps.ToArray();
        }
    }
}
```

- [x] **Step 5: テストを実行して緑を確認**

Expected: `WeatherParserTests` の 5 テストが PASS。

- [x] **Step 6: コミット**

```bash
git add UnityProject/Assets/Scripts/Weather/OwmDto.cs UnityProject/Assets/Scripts/Weather/WeatherParser.cs UnityProject/Assets/Tests/EditMode/WeatherParserTests.cs
git commit -m "feat: parse OWM forecast JSON into WeatherSnapshot array with tests"
```

### Task 3.5: タイムラインデータ（WeatherTimelineSO）— TDD ✅ 実装済み

ランタイム ScriptableObject。`snapshots` を差し替え、`SetIndex` で `OnSnapshotChanged` を発火する。`currentIndex` は範囲クランプ。

**Files:**
- Create: `UnityProject/Assets/Scripts/Data/WeatherTimelineSO.cs`
- Test: `UnityProject/Assets/Tests/EditMode/WeatherTimelineSOTests.cs`

**Interfaces:**
- Consumes: `WeatherSnapshot`
- Produces:
  - `JapanWeatherDemo.Data.WeatherTimelineSO`（ScriptableObject）:
    - `string cityName; WeatherSnapshot[] snapshots; int currentIndex;`
    - `event Action<WeatherSnapshot> OnSnapshotChanged;`
    - `void SetData(string cityName, WeatherSnapshot[] snapshots)` — index を 0 にして発火
    - `void SetIndex(int index)` — クランプして発火
    - `int Count { get; }`
  - Milestone 4/5/6 の全コントローラーが `OnSnapshotChanged` を購読。

- [x] **Step 1: 失敗するテストを書く**

`UnityProject/Assets/Tests/EditMode/WeatherTimelineSOTests.cs`:

```csharp
using NUnit.Framework;
using System;
using UnityEngine;
using JapanWeatherDemo.Data;

namespace JapanWeatherDemo.Tests
{
    public class WeatherTimelineSOTests
    {
        static WeatherSnapshot Snap(float temp) => new WeatherSnapshot
        {
            dateTime = new DateTime(2024, 1, 1, 9, 0, 0),
            condition = WeatherCondition.Clear,
            temperatureCelsius = temp
        };

        [Test]
        public void SetData_FiresEvent_WithFirstSnapshot()
        {
            var so = ScriptableObject.CreateInstance<WeatherTimelineSO>();
            WeatherSnapshot? got = null;
            so.OnSnapshotChanged += s => got = s;

            so.SetData("東京", new[] { Snap(1f), Snap(2f) });

            Assert.AreEqual(0, so.currentIndex);
            Assert.IsTrue(got.HasValue);
            Assert.AreEqual(1f, got.Value.temperatureCelsius, 1e-4f);
        }

        [Test]
        public void SetIndex_ClampsAndFires()
        {
            var so = ScriptableObject.CreateInstance<WeatherTimelineSO>();
            so.SetData("東京", new[] { Snap(1f), Snap(2f), Snap(3f) });
            WeatherSnapshot? got = null;
            so.OnSnapshotChanged += s => got = s;

            so.SetIndex(99);

            Assert.AreEqual(2, so.currentIndex);            // クランプ
            Assert.AreEqual(3f, got.Value.temperatureCelsius, 1e-4f);
        }

        [Test]
        public void Count_ReflectsSnapshots()
        {
            var so = ScriptableObject.CreateInstance<WeatherTimelineSO>();
            so.SetData("東京", new[] { Snap(1f), Snap(2f) });
            Assert.AreEqual(2, so.Count);
        }
    }
}
```

- [x] **Step 2: テストを実行して失敗を確認**

Expected: `WeatherTimelineSO` 未定義で FAIL。

- [x] **Step 3: WeatherTimelineSO を実装**

`UnityProject/Assets/Scripts/Data/WeatherTimelineSO.cs`:

```csharp
using System;
using UnityEngine;

namespace JapanWeatherDemo.Data
{
    /// <summary>
    /// 天気タイムラインのランタイムデータコンテナ。保存アセットではなく
    /// CreateInstance で 1 個だけ生成し、都市切替で snapshots を差し替えて再利用する。
    /// </summary>
    public class WeatherTimelineSO : ScriptableObject
    {
        public string cityName;
        public WeatherSnapshot[] snapshots = new WeatherSnapshot[0];
        public int currentIndex;

        public event Action<WeatherSnapshot> OnSnapshotChanged;

        public int Count => snapshots != null ? snapshots.Length : 0;

        public void SetData(string cityName, WeatherSnapshot[] snapshots)
        {
            this.cityName = cityName;
            this.snapshots = snapshots ?? new WeatherSnapshot[0];
            currentIndex = 0;
            if (Count > 0) OnSnapshotChanged?.Invoke(this.snapshots[0]);
        }

        public void SetIndex(int index)
        {
            if (Count == 0) return;
            currentIndex = Mathf.Clamp(index, 0, snapshots.Length - 1);
            OnSnapshotChanged?.Invoke(snapshots[currentIndex]);
        }
    }
}
```

- [x] **Step 4: テストを実行して緑を確認**

Expected: `WeatherTimelineSOTests` の 3 テストが PASS。

- [x] **Step 5: コミット**

```bash
git add UnityProject/Assets/Scripts/Data/WeatherTimelineSO.cs UnityProject/Assets/Tests/EditMode/WeatherTimelineSOTests.cs
git commit -m "feat: add WeatherTimelineSO runtime container with event and tests"
```

### Task 3.6: ダミーデータ（DummyWeather）— TDD ✅ 実装済み

オフライン・無キーでもデモが成立する固定スナップショット列（40 点・3 時間刻み・JST）をコード生成する。多様なコンディションを含める。

**Files:**
- Create: `UnityProject/Assets/Scripts/Weather/DummyWeather.cs`
- Test: `UnityProject/Assets/Tests/EditMode/DummyWeatherTests.cs`

**Interfaces:**
- Consumes: `WeatherSnapshot`, `WeatherCondition`
- Produces: `JapanWeatherDemo.Weather.DummyWeather.Generate(string cityName, DateTime startJst) -> WeatherSnapshot[]`（長さ 40）
  - Task 3.7 `WeatherService` がフォールバックで使う。

- [x] **Step 1: 失敗するテストを書く**

`UnityProject/Assets/Tests/EditMode/DummyWeatherTests.cs`:

```csharp
using NUnit.Framework;
using System;
using System.Linq;
using JapanWeatherDemo.Data;
using JapanWeatherDemo.Weather;

namespace JapanWeatherDemo.Tests
{
    public class DummyWeatherTests
    {
        [Test]
        public void Generate_Returns40Snapshots_3HoursApart()
        {
            var start = new DateTime(2024, 6, 28, 9, 0, 0);
            var snaps = DummyWeather.Generate("東京", start);
            Assert.AreEqual(40, snaps.Length);
            Assert.AreEqual(start, snaps[0].dateTime);
            Assert.AreEqual(start.AddHours(3), snaps[1].dateTime);
            Assert.AreEqual(start.AddHours(3 * 39), snaps[39].dateTime);
        }

        [Test]
        public void Generate_ContainsVariedConditions()
        {
            var snaps = DummyWeather.Generate("東京", new DateTime(2024, 6, 28, 9, 0, 0));
            int distinct = snaps.Select(s => s.condition).Distinct().Count();
            Assert.GreaterOrEqual(distinct, 3);
        }
    }
}
```

- [x] **Step 2: テストを実行して失敗を確認**

Expected: `DummyWeather` 未定義で FAIL。

- [x] **Step 3: DummyWeather を実装**

`UnityProject/Assets/Scripts/Weather/DummyWeather.cs`:

```csharp
using System;
using JapanWeatherDemo.Data;

namespace JapanWeatherDemo.Weather
{
    /// <summary>オフライン・無キー時のデモ用固定スナップショット列を生成する。</summary>
    public static class DummyWeather
    {
        // 40 点ぶんのコンディションを周期的に変化させ、見せ場を作る。
        static readonly WeatherCondition[] Pattern =
        {
            WeatherCondition.Clear, WeatherCondition.Clear, WeatherCondition.Cloudy,
            WeatherCondition.Rain, WeatherCondition.Storm, WeatherCondition.Rain,
            WeatherCondition.Cloudy, WeatherCondition.Snow
        };

        public static WeatherSnapshot[] Generate(string cityName, DateTime startJst)
        {
            var snaps = new WeatherSnapshot[40];
            for (int i = 0; i < 40; i++)
            {
                var cond = Pattern[i % Pattern.Length];
                snaps[i] = new WeatherSnapshot
                {
                    dateTime = startJst.AddHours(3 * i),
                    condition = cond,
                    cloudCoverage = CloudFor(cond),
                    windSpeed = 2f + (i % 5),
                    windDirectionDeg = (i * 30) % 360,
                    rainIntensity = cond == WeatherCondition.Rain ? 0.5f : (cond == WeatherCondition.Storm ? 0.9f : 0f),
                    temperatureCelsius = 15f + 8f * (float)Math.Sin(i * Math.PI / 8.0)
                };
            }
            return snaps;
        }

        static float CloudFor(WeatherCondition c) => c switch
        {
            WeatherCondition.Clear => 0.1f,
            WeatherCondition.Cloudy => 0.8f,
            WeatherCondition.Rain => 0.7f,
            WeatherCondition.Storm => 1.0f,
            WeatherCondition.Snow => 0.5f,
            _ => 0.5f
        };
    }
}
```

- [x] **Step 4: テストを実行して緑を確認**

Expected: `DummyWeatherTests` の 2 テストが PASS。

- [x] **Step 5: コミット**

```bash
git add UnityProject/Assets/Scripts/Weather/DummyWeather.cs UnityProject/Assets/Tests/EditMode/DummyWeatherTests.cs
git commit -m "feat: add offline dummy weather generator with tests"
```

### Task 3.7: API キー解決と取得サービス（ApiKeyResolver / WeatherService）✅ 実装済み（ライブ取得の Play 確認は M4 で）

キー解決ロジック（config.json → 環境変数 → 無し）を純ロジックで TDD し、`WeatherService` は UnityWebRequest で取得・パース・メモリキャッシュ・失敗時ダミーを担う。

**Files:**
- Create: `UnityProject/Assets/Scripts/Weather/ApiKeyResolver.cs`
- Create: `UnityProject/Assets/Scripts/Weather/WeatherService.cs`
- Create: `UnityProject/Assets/StreamingAssets/config.example.json`
- Modify: `UnityProject/.gitignore`（`config.json` を無視）※リポジトリルートの .gitignore
- Test: `UnityProject/Assets/Tests/EditMode/ApiKeyResolverTests.cs`

**Interfaces:**
- Consumes: `WeatherParser`, `DummyWeather`, `WeatherTimelineSO`, `CityData`
- Produces:
  - `JapanWeatherDemo.Weather.ApiKeyResolver.Resolve(string configJsonOrNull, Func<string,string> envGetter) -> string`（無ければ空文字）
  - `JapanWeatherDemo.Weather.WeatherService`（MonoBehaviour）:
    - `void FetchForecast(CityData city, Action<WeatherSnapshot[]> onResult, Action<string> onError)`
    - 内部でキャッシュ（`Dictionary<string,WeatherSnapshot[]>`、キー=`name`）。失敗時は `onError` を呼びつつ `DummyWeather.Generate` を `onResult` で返す。
  - Task 4.1 `GameManager` が利用。

- [x] **Step 1: ApiKeyResolver の失敗するテストを書く**

`UnityProject/Assets/Tests/EditMode/ApiKeyResolverTests.cs`:

```csharp
using NUnit.Framework;
using JapanWeatherDemo.Weather;

namespace JapanWeatherDemo.Tests
{
    public class ApiKeyResolverTests
    {
        [Test]
        public void Resolve_PrefersConfigJson()
        {
            string json = "{\"apiKey\":\"FROM_CONFIG\"}";
            string key = ApiKeyResolver.Resolve(json, name => "FROM_ENV");
            Assert.AreEqual("FROM_CONFIG", key);
        }

        [Test]
        public void Resolve_FallsBackToEnv_WhenConfigMissing()
        {
            string key = ApiKeyResolver.Resolve(null, name => name == "OWM_API_KEY" ? "FROM_ENV" : "");
            Assert.AreEqual("FROM_ENV", key);
        }

        [Test]
        public void Resolve_FallsBackToEnv_WhenConfigKeyEmpty()
        {
            string key = ApiKeyResolver.Resolve("{\"apiKey\":\"\"}", name => "FROM_ENV");
            Assert.AreEqual("FROM_ENV", key);
        }

        [Test]
        public void Resolve_ReturnsEmpty_WhenNothingSet()
        {
            string key = ApiKeyResolver.Resolve(null, name => "");
            Assert.AreEqual("", key);
        }
    }
}
```

- [x] **Step 2: テストを実行して失敗を確認**

Expected: `ApiKeyResolver` 未定義で FAIL。

- [x] **Step 3: ApiKeyResolver を実装**

`UnityProject/Assets/Scripts/Weather/ApiKeyResolver.cs`:

```csharp
using System;
using Newtonsoft.Json.Linq;

namespace JapanWeatherDemo.Weather
{
    /// <summary>config.json → 環境変数 OWM_API_KEY の順に API キーを解決する。</summary>
    public static class ApiKeyResolver
    {
        public const string EnvVarName = "OWM_API_KEY";

        public static string Resolve(string configJsonOrNull, Func<string, string> envGetter)
        {
            if (!string.IsNullOrEmpty(configJsonOrNull))
            {
                try
                {
                    var key = JObject.Parse(configJsonOrNull)["apiKey"]?.ToString();
                    if (!string.IsNullOrEmpty(key)) return key;
                }
                catch { /* 不正 JSON は無視して次へ */ }
            }
            string env = envGetter?.Invoke(EnvVarName);
            return string.IsNullOrEmpty(env) ? "" : env;
        }
    }
}
```

- [x] **Step 4: テストを実行して緑を確認**

Expected: `ApiKeyResolverTests` の 4 テストが PASS。

- [x] **Step 5: WeatherService を実装**

`UnityProject/Assets/Scripts/Weather/WeatherService.cs`:

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using JapanWeatherDemo.Data;

namespace JapanWeatherDemo.Weather
{
    /// <summary>OpenWeatherMap から予報を取得する。キー無し/失敗時はダミーで成立させる。</summary>
    public class WeatherService : MonoBehaviour
    {
        const string Endpoint = "https://api.openweathermap.org/data/2.5/forecast";
        const int Cnt = 40;

        private string apiKey = "";
        private readonly Dictionary<string, WeatherSnapshot[]> cache = new();

        public bool HasApiKey => !string.IsNullOrEmpty(apiKey);

        private void Awake() => apiKey = ResolveKey();

        private string ResolveKey()
        {
            string configPath = Path.Combine(Application.streamingAssetsPath, "config.json");
            string json = File.Exists(configPath) ? File.ReadAllText(configPath) : null;
            return ApiKeyResolver.Resolve(json, Environment.GetEnvironmentVariable);
        }

        public void FetchForecast(CityData city, Action<WeatherSnapshot[]> onResult, Action<string> onError)
        {
            if (cache.TryGetValue(city.name, out var cached)) { onResult?.Invoke(cached); return; }

            if (!HasApiKey)
            {
                onError?.Invoke("API キー未設定: ダミーデータで表示します");
                var dummy = DummyWeather.Generate(city.name, DateTime.Now);
                cache[city.name] = dummy;
                onResult?.Invoke(dummy);
                return;
            }
            StartCoroutine(FetchRoutine(city, onResult, onError));
        }

        private IEnumerator FetchRoutine(CityData city, Action<WeatherSnapshot[]> onResult, Action<string> onError)
        {
            string url = $"{Endpoint}?lat={city.lat}&lon={city.lon}&appid={apiKey}&units=metric&cnt={Cnt}";
            using var req = UnityWebRequest.Get(url);
            req.timeout = 10;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"取得失敗: {req.error}。ダミーデータで表示します");
                var dummy = DummyWeather.Generate(city.name, DateTime.Now);
                onResult?.Invoke(dummy);
                yield break;
            }

            WeatherSnapshot[] snaps;
            try { snaps = WeatherParser.Parse(req.downloadHandler.text); }
            catch (Exception e)
            {
                onError?.Invoke($"パース失敗: {e.Message}");
                yield break; // 直前の表示を維持
            }

            cache[city.name] = snaps;
            onResult?.Invoke(snaps);
        }
    }
}
```

- [x] **Step 6: config.example.json と .gitignore**

`UnityProject/Assets/StreamingAssets/config.example.json`:

```json
{
  "apiKey": ""
}
```

リポジトリルートまたは `UnityProject/.gitignore` に以下を追記（`config.json` 本体と Unity が作る meta を無視）:

```
# OpenWeatherMap API key (do not commit)
UnityProject/Assets/StreamingAssets/config.json
UnityProject/Assets/StreamingAssets/config.json.meta
```

- [x] **Step 7: コンパイル確認（WeatherService は Play Mode 統合で後検証）**

Expected: コンソールにエラー無し。実取得は Task 4 のフローで確認する。

- [x] **Step 8: コミット**

```bash
git add UnityProject/Assets/Scripts/Weather/ApiKeyResolver.cs UnityProject/Assets/Scripts/Weather/WeatherService.cs UnityProject/Assets/Tests/EditMode/ApiKeyResolverTests.cs UnityProject/Assets/StreamingAssets/config.example.json .gitignore UnityProject/.gitignore
git commit -m "feat: add API key resolver and weather fetch service with dummy fallback"
```

---

## Milestone 4 — 都市選択フロー

**完了の目安:** 起動時に東京が自動選択され予報が取得・格納される。地図上のマーカーをクリックすると、その都市の予報取得 → `WeatherTimelineSO.SetData` → `OnSnapshotChanged` 発火までが繋がる（購読側はまだ無くてもログで確認できる）。

### Task 4.1: GameManager による配線 ✅ 実装済み（commit 8373589、東京自動選択→取得→Timeline city=東京 count=40）

`MapManager.CitySelected` を購読し、`WeatherService.FetchForecast` を呼び、結果を `WeatherTimelineSO.SetData` に流す。起動時に東京を初期選択する。`WeatherTimelineSO` は `CreateInstance` で 1 個だけ生成して再利用。

**Files:**
- Create: `UnityProject/Assets/Scripts/GameManager.cs`
- Modify: `UnityProject/Assets/Scenes/MainScene.unity`（`GameManager` 配置・参照割当）

**Interfaces:**
- Consumes: `MapManager`（Task 2.3）, `WeatherService`（Task 3.7）, `WeatherTimelineSO`（Task 3.5）, `CityData`
- Produces:
  - `JapanWeatherDemo.GameManager`（MonoBehaviour）:
    - `WeatherTimelineSO Timeline { get; }` — Milestone 5/6 のコントローラーがこのインスタンスの `OnSnapshotChanged` を購読する
    - `[SerializeField] string initialCityName = "東京"`
    - `void SelectCity(CityData city)`（内部）
    - `event Action<string> StatusMessage` — エラー/警告メッセージ（Task 6.3 のトーストが購読）

- [x] **Step 1: GameManager を実装**

`UnityProject/Assets/Scripts/GameManager.cs`:

```csharp
using System;
using UnityEngine;
using JapanWeatherDemo.Data;
using JapanWeatherDemo.Map;
using JapanWeatherDemo.Weather;

namespace JapanWeatherDemo
{
    /// <summary>起動時配線のハブ。都市選択 → 取得 → タイムライン更新を仲介する。</summary>
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private MapManager mapManager;
        [SerializeField] private WeatherService weatherService;
        [SerializeField] private string initialCityName = "東京";

        public WeatherTimelineSO Timeline { get; private set; }
        public event Action<string> StatusMessage;

        private void Awake()
        {
            Timeline = ScriptableObject.CreateInstance<WeatherTimelineSO>();
        }

        private void OnEnable()
        {
            if (mapManager != null) mapManager.CitySelected += SelectCity;
        }

        private void OnDisable()
        {
            if (mapManager != null) mapManager.CitySelected -= SelectCity;
        }

        private void Start()
        {
            // 初期都市（東京）を選択。マーカー側にも選択ハイライトを反映させる。
            mapManager.SelectByName(initialCityName);
        }

        private void SelectCity(CityData city)
        {
            weatherService.FetchForecast(
                city,
                snaps =>
                {
                    Timeline.SetData(city.name, snaps);
                    Debug.Log($"[GameManager] {city.name}: {snaps.Length} snapshots loaded");
                },
                error => StatusMessage?.Invoke(error));
        }
    }
}
```

- [x] **Step 2: コンパイルを確認**

Expected: コンソールにエラー無し。

- [x] **Step 3: シーンに GameManager を配置**

- 空 GameObject `GameManager` を作成し `GameManager.cs` をアタッチ。
- `mapManager` に `MapManager`、`weatherService` に `WeatherService`（同じく空 GameObject にアタッチ）を割当て。
- `MapManager` の `markerPrefab`/`bounds`/`raycastCamera` 等が設定済みであることを再確認。

- [x] **Step 4: Play してフローを目視確認**

Play し、コンソールに `[GameManager] 東京: 40 snapshots loaded`（無キーなら直前に警告ログ＋ダミー 40 点）が出ること。地図上で別の都市マーカーをクリックすると、その都市名で同様のログが出て、選択マーカーの光柱がハイライトされること。
Expected: クリックごとに対応都市のスナップショット数ログが出る。キー設定済みなら実データ、未設定ならダミー。

- [x] **Step 5: コミット**

```bash
git add UnityProject/Assets/Scripts/GameManager.cs UnityProject/Assets/Scenes/MainScene.unity
git commit -m "feat: wire city selection to weather fetch and timeline"
```

---

## Milestone 5 — 基本天気エフェクト

**完了の目安:** 選択都市・タイムライン位置に応じて雲・雨・晴れと時間帯連動の空（朝焼け・昼・夕焼け・夜）が切り替わる。コントローラーは `Timeline.OnSnapshotChanged` を購読し、受け取った目標値へ毎フレーム滑らかに追従する。

> **配線の前提:** 各コントローラーは `[SerializeField] GameManager gameManager` を持ち、`OnEnable` で `gameManager.Timeline.OnSnapshotChanged += OnSnapshot` を購読、`OnDisable` で解除する。`GameManager.Awake` が先に `Timeline` を生成するよう、`GameManager` の Script Execution Order を他コントローラーより前にする（`Edit > Project Settings > Script Execution Order` で `GameManager` を -100 に設定）。

### Task 5.1: スナップショット補間（SnapshotInterpolator）— TDD ✅ 実装済み

隣接 2 スナップショット間の線形補間（数値）と最寄りコンディション選択。再生・スライダーの中間表現に使う純関数。

**Files:**
- Create: `UnityProject/Assets/Scripts/Weather/SnapshotInterpolator.cs`
- Test: `UnityProject/Assets/Tests/EditMode/SnapshotInterpolatorTests.cs`

**Interfaces:**
- Consumes: `WeatherSnapshot`, `WeatherCondition`
- Produces: `JapanWeatherDemo.Weather.SnapshotInterpolator.Lerp(WeatherSnapshot a, WeatherSnapshot b, float t) -> WeatherSnapshot`
  - 数値（cloudCoverage/windSpeed/windDirectionDeg/rainIntensity/temperatureCelsius）は線形補間、`dateTime` も線形補間、`condition` は `t < 0.5 ? a : b`。
  - Task 6.1 `TimelineUIController` が使う。

- [x] **Step 1: 失敗するテストを書く**

`UnityProject/Assets/Tests/EditMode/SnapshotInterpolatorTests.cs`:

```csharp
using NUnit.Framework;
using System;
using JapanWeatherDemo.Data;
using JapanWeatherDemo.Weather;

namespace JapanWeatherDemo.Tests
{
    public class SnapshotInterpolatorTests
    {
        static WeatherSnapshot A() => new WeatherSnapshot
        {
            dateTime = new DateTime(2024, 1, 1, 0, 0, 0),
            condition = WeatherCondition.Clear,
            cloudCoverage = 0f, windSpeed = 0f, rainIntensity = 0f, temperatureCelsius = 10f
        };
        static WeatherSnapshot B() => new WeatherSnapshot
        {
            dateTime = new DateTime(2024, 1, 1, 3, 0, 0),
            condition = WeatherCondition.Rain,
            cloudCoverage = 1f, windSpeed = 10f, rainIntensity = 1f, temperatureCelsius = 20f
        };

        [Test]
        public void Lerp_Midpoint_AveragesNumbers()
        {
            var m = SnapshotInterpolator.Lerp(A(), B(), 0.5f);
            Assert.AreEqual(0.5f, m.cloudCoverage, 1e-4f);
            Assert.AreEqual(5f, m.windSpeed, 1e-4f);
            Assert.AreEqual(15f, m.temperatureCelsius, 1e-4f);
        }

        [Test]
        public void Lerp_ConditionSwitchesAtHalf()
        {
            Assert.AreEqual(WeatherCondition.Clear, SnapshotInterpolator.Lerp(A(), B(), 0.49f).condition);
            Assert.AreEqual(WeatherCondition.Rain, SnapshotInterpolator.Lerp(A(), B(), 0.5f).condition);
        }

        [Test]
        public void Lerp_InterpolatesDateTime()
        {
            var m = SnapshotInterpolator.Lerp(A(), B(), 0.5f);
            Assert.AreEqual(new DateTime(2024, 1, 1, 1, 30, 0), m.dateTime);
        }
    }
}
```

- [x] **Step 2: テストを実行して失敗を確認**

Expected: `SnapshotInterpolator` 未定義で FAIL。

- [x] **Step 3: SnapshotInterpolator を実装**

`UnityProject/Assets/Scripts/Weather/SnapshotInterpolator.cs`:

```csharp
using System;
using UnityEngine;
using JapanWeatherDemo.Data;

namespace JapanWeatherDemo.Weather
{
    /// <summary>隣接スナップショット間を線形補間する純関数。</summary>
    public static class SnapshotInterpolator
    {
        public static WeatherSnapshot Lerp(WeatherSnapshot a, WeatherSnapshot b, float t)
        {
            t = Mathf.Clamp01(t);
            long ticks = (long)(a.dateTime.Ticks + (b.dateTime.Ticks - a.dateTime.Ticks) * (double)t);
            return new WeatherSnapshot
            {
                dateTime = new DateTime(ticks),
                condition = t < 0.5f ? a.condition : b.condition,
                cloudCoverage = Mathf.Lerp(a.cloudCoverage, b.cloudCoverage, t),
                windSpeed = Mathf.Lerp(a.windSpeed, b.windSpeed, t),
                windDirectionDeg = Mathf.Lerp(a.windDirectionDeg, b.windDirectionDeg, t),
                rainIntensity = Mathf.Lerp(a.rainIntensity, b.rainIntensity, t),
                temperatureCelsius = Mathf.Lerp(a.temperatureCelsius, b.temperatureCelsius, t)
            };
        }
    }
}
```

- [x] **Step 4: テストを実行して緑を確認**

Expected: `SnapshotInterpolatorTests` の 3 テストが PASS。

- [x] **Step 5: コミット**

```bash
git add UnityProject/Assets/Scripts/Weather/SnapshotInterpolator.cs UnityProject/Assets/Tests/EditMode/SnapshotInterpolatorTests.cs
git commit -m "feat: add snapshot linear interpolation with tests"
```

### Task 5.2: 太陽角度（SunAngle）— TDD ✅ 実装済み

JST 時刻（小数時）→ 太陽の高度角（elevation）を返す純関数。`SkyController` が `DirectionalLight` の角度に使う。

**Files:**
- Create: `UnityProject/Assets/Scripts/Weather/SunAngle.cs`
- Test: `UnityProject/Assets/Tests/EditMode/SunAngleTests.cs`

**Interfaces:**
- Consumes: なし
- Produces: `JapanWeatherDemo.Weather.SunAngle.ElevationDeg(float hour) -> float`（-90〜+90、6 時=0、12 時=+90、18 時=0、0 時=-90）
  - Task 5.5 `SkyController` が使う。

- [x] **Step 1: 失敗するテストを書く**

`UnityProject/Assets/Tests/EditMode/SunAngleTests.cs`:

```csharp
using NUnit.Framework;
using JapanWeatherDemo.Weather;

namespace JapanWeatherDemo.Tests
{
    public class SunAngleTests
    {
        [TestCase(0f, -90f)]
        [TestCase(6f, 0f)]
        [TestCase(12f, 90f)]
        [TestCase(18f, 0f)]
        public void ElevationDeg_KeyHours(float hour, float expected)
        {
            Assert.AreEqual(expected, SunAngle.ElevationDeg(hour), 1e-3f);
        }

        [Test]
        public void ElevationDeg_NightIsNegative()
        {
            Assert.Less(SunAngle.ElevationDeg(3f), 0f);
            Assert.Less(SunAngle.ElevationDeg(21f), 0f);
        }
    }
}
```

- [x] **Step 2: テストを実行して失敗を確認**

Expected: `SunAngle` 未定義で FAIL。

- [x] **Step 3: SunAngle を実装**

`UnityProject/Assets/Scripts/Weather/SunAngle.cs`:

```csharp
using UnityEngine;

namespace JapanWeatherDemo.Weather
{
    /// <summary>時刻（JST 小数時）から太陽の高度角を返す簡易モデル。</summary>
    public static class SunAngle
    {
        // elevation = -90 * cos(2π * hour/24)。0 時=-90、6 時=0、12 時=+90、18 時=0。
        public static float ElevationDeg(float hour)
        {
            return -90f * Mathf.Cos(2f * Mathf.PI * hour / 24f);
        }
    }
}
```

- [x] **Step 4: テストを実行して緑を確認**

Expected: `SunAngleTests` の全ケースが PASS。

- [x] **Step 5: コミット**

```bash
git add UnityProject/Assets/Scripts/Weather/SunAngle.cs UnityProject/Assets/Tests/EditMode/SunAngleTests.cs
git commit -m "feat: add sun elevation model with tests"
```

### Task 5.3: 雲（CloudController）✅ 実装済み ※方式変更: Volumetric Clouds→半透明メッシュ雲レイヤー（commit f8b3815、実コード/CloudLayer.mat 優先。下の Step は旧 VC 版で不採用）

HDRP Volumetric Clouds の濃さを `cloudCoverage` に追従させる。目標値へ毎フレーム滑らかに補間。

**Files:**
- Create: `UnityProject/Assets/Scripts/Weather/CloudController.cs`
- Modify: `UnityProject/Assets/Scenes/MainScene.unity`（Global Volume に Volumetric Clouds を追加）

**Interfaces:**
- Consumes: `GameManager.Timeline.OnSnapshotChanged`, `WeatherSnapshot`
- Produces: `JapanWeatherDemo.Weather.CloudController`（MonoBehaviour）

- [x] **Step 1: CloudController を実装**

`UnityProject/Assets/Scripts/Weather/CloudController.cs`:

```csharp
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using JapanWeatherDemo.Data;

namespace JapanWeatherDemo.Weather
{
    /// <summary>Volumetric Clouds の密度を cloudCoverage に滑らかに追従させる。</summary>
    public class CloudController : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Volume volume;
        [SerializeField] private float followSpeed = 1.5f;
        [SerializeField] private float maxDensity = 1f;

        private VolumetricClouds clouds;
        private float targetCoverage;
        private float currentCoverage;

        private void OnEnable()
        {
            if (volume != null && volume.profile.TryGet(out clouds)) { }
            if (gameManager != null) gameManager.Timeline.OnSnapshotChanged += OnSnapshot;
        }

        private void OnDisable()
        {
            if (gameManager != null && gameManager.Timeline != null)
                gameManager.Timeline.OnSnapshotChanged -= OnSnapshot;
        }

        private void OnSnapshot(WeatherSnapshot s) => targetCoverage = s.cloudCoverage;

        private void Update()
        {
            if (clouds == null) return;
            currentCoverage = Mathf.MoveTowards(currentCoverage, targetCoverage, followSpeed * Time.deltaTime);
            clouds.densityMultiplier.value = currentCoverage * maxDensity;
            clouds.enable.value = currentCoverage > 0.02f;
        }
    }
}
```

- [x] **Step 2: コンパイルを確認**

Expected: コンソールにエラー無し。

- [x] **Step 3: シーンに Volumetric Clouds を追加**

- `MainScene` の Global Volume プロファイルに `Add Override > Sky > Volumetric Clouds` を追加し、`Enable` と `Density Multiplier` を上書き有効化。Local Clouds が必要なら有効化。
- 空 GameObject `WeatherEffectsRoot` を作り、子に `CloudController` をアタッチ（または既存 GameObject に付与）。`gameManager` と `volume`（Global Volume）を割当て。

- [x] **Step 4: Play して目視確認**

Play し、ダミー/実データのコンディション変化（Clear↔Cloudy↔Storm）に応じて雲量が滑らかに増減すること。Expected: Clear で薄く/消え、Storm で非常に厚くなる。

- [x] **Step 5: コミット**

```bash
git add UnityProject/Assets/Scripts/Weather/CloudController.cs UnityProject/Assets/Scenes/MainScene.unity
git commit -m "feat: add cloud controller driving volumetric clouds"
```

### Task 5.4: 降水（PrecipitationController）✅ 実装済み ※方式変更: VFX Graph→ParticleSystem（commit ca4a73b、実コード/Rain・SnowParticle.mat 優先。下の Step は旧 VFX 版で不採用）

雨・雪の VFX Graph の放出量を `rainIntensity` とコンディションに追従させる。雨用 VFX と雪用 VFX を切り替える。

**Files:**
- Create: `UnityProject/Assets/Scripts/Weather/PrecipitationController.cs`
- Create（VFX）: `UnityProject/Assets/VFX/RainVFX.vfx`, `UnityProject/Assets/VFX/SnowVFX.vfx`
- Modify: `UnityProject/Assets/Scenes/MainScene.unity`

**Interfaces:**
- Consumes: `GameManager.Timeline.OnSnapshotChanged`, `WeatherSnapshot`, `WeatherCondition`
- Produces: `JapanWeatherDemo.Weather.PrecipitationController`（MonoBehaviour）。VFX には公開プロパティ `SpawnRate`（float, Exposed）を持たせる。

- [x] **Step 1: PrecipitationController を実装**

`UnityProject/Assets/Scripts/Weather/PrecipitationController.cs`:

```csharp
using UnityEngine;
using UnityEngine.VFX;
using JapanWeatherDemo.Data;

namespace JapanWeatherDemo.Weather
{
    /// <summary>rainIntensity とコンディションに応じて雨/雪 VFX の放出量を制御する。</summary>
    public class PrecipitationController : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private VisualEffect rainVfx;
        [SerializeField] private VisualEffect snowVfx;
        [SerializeField] private float maxRainRate = 4000f;
        [SerializeField] private float maxSnowRate = 1500f;
        [SerializeField] private float followSpeed = 6000f;

        private static readonly int SpawnRateId = Shader.PropertyToID("SpawnRate");
        private float targetRain, targetSnow, curRain, curSnow;

        private void OnEnable()
        {
            if (gameManager != null) gameManager.Timeline.OnSnapshotChanged += OnSnapshot;
        }

        private void OnDisable()
        {
            if (gameManager != null && gameManager.Timeline != null)
                gameManager.Timeline.OnSnapshotChanged -= OnSnapshot;
        }

        private void OnSnapshot(WeatherSnapshot s)
        {
            bool isSnow = s.condition == WeatherCondition.Snow;
            bool isRainy = s.condition == WeatherCondition.Rain || s.condition == WeatherCondition.Storm;
            targetRain = isRainy ? Mathf.Clamp01(s.rainIntensity) * maxRainRate : 0f;
            targetSnow = isSnow ? maxSnowRate : 0f;
        }

        private void Update()
        {
            curRain = Mathf.MoveTowards(curRain, targetRain, followSpeed * Time.deltaTime);
            curSnow = Mathf.MoveTowards(curSnow, targetSnow, followSpeed * Time.deltaTime);
            if (rainVfx != null) rainVfx.SetFloat(SpawnRateId, curRain);
            if (snowVfx != null) snowVfx.SetFloat(SpawnRateId, curSnow);
        }
    }
}
```

- [x] **Step 2: コンパイルを確認**

Expected: コンソールにエラー無し。

- [x] **Step 3: 雨/雪 VFX を作成**

- `Assets/VFX/RainVFX.vfx` を作成（`Create > Visual Effects > Visual Effect Graph`）。Spawn に `Constant Spawn Rate` を置き、Rate を Exposed な `float SpawnRate`（Blackboard で公開）にバインド。落下方向は -Y、雨らしい細長いパーティクル。カメラに追従する広い放出ボックス。
- `Assets/VFX/SnowVFX.vfx` を同様に作成（白く、ゆっくり、ふわふわ）。
- シーンに `RainVFX`/`SnowVFX` GameObject を配置し、`PrecipitationController`（`WeatherEffectsRoot` 配下）の `rainVfx`/`snowVfx` と `gameManager` を割当て。

- [x] **Step 4: Play して目視確認**

Play し、Rain/Storm で雨が降り（Storm でより強く）、Snow で雪に切り替わり、Clear/Cloudy で止むこと。Expected: コンディション遷移に応じて放出量が滑らかに増減・切替。

- [x] **Step 5: コミット**

```bash
git add UnityProject/Assets/Scripts/Weather/PrecipitationController.cs UnityProject/Assets/VFX UnityProject/Assets/Scenes/MainScene.unity
git commit -m "feat: add precipitation controller with rain and snow VFX"
```

### Task 5.5: 空と太陽（SkyController）✅ 実装済み（commit 1f920dc、GlobalVolume に PBS+Exposure Fixed EV15+Bloom、HDAdditionalLightData の Lux で太陽制御）

`WeatherSnapshot.dateTime` の時刻から `SunAngle` で `DirectionalLight` の角度を決め、コンディションで空の露光/色味を変える。朝・昼・夕・夜を滑らかに遷移。

**Files:**
- Create: `UnityProject/Assets/Scripts/Weather/SkyController.cs`
- Modify: `UnityProject/Assets/Scenes/MainScene.unity`（PhysicallyBasedSky を Volume に追加、`Sun` 参照）

**Interfaces:**
- Consumes: `GameManager.Timeline.OnSnapshotChanged`, `WeatherSnapshot`, `SunAngle`
- Produces: `JapanWeatherDemo.Weather.SkyController`（MonoBehaviour）

- [x] **Step 1: SkyController を実装**

`UnityProject/Assets/Scripts/Weather/SkyController.cs`:

```csharp
using UnityEngine;
using JapanWeatherDemo.Data;

namespace JapanWeatherDemo.Weather
{
    /// <summary>時刻で太陽角度を、コンディションで光の強さ・色を制御する。</summary>
    public class SkyController : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Light sun;
        [SerializeField] private float followSpeed = 2f;
        [SerializeField] private float sunYaw = 170f; // 南中方向（南向き）

        private float targetElevation, curElevation;
        private float targetIntensity, curIntensity;
        private Color targetColor = Color.white, curColor = Color.white;

        private void OnEnable()
        {
            if (gameManager != null) gameManager.Timeline.OnSnapshotChanged += OnSnapshot;
        }

        private void OnDisable()
        {
            if (gameManager != null && gameManager.Timeline != null)
                gameManager.Timeline.OnSnapshotChanged -= OnSnapshot;
        }

        private void OnSnapshot(WeatherSnapshot s)
        {
            float hour = s.dateTime.Hour + s.dateTime.Minute / 60f;
            targetElevation = SunAngle.ElevationDeg(hour);

            // コンディションで光量と色温度の目標を決める
            float cloudDim = Mathf.Lerp(1f, 0.35f, s.cloudCoverage);
            targetIntensity = Mathf.Max(0f, targetElevation > 0 ? cloudDim : 0.05f);

            // 朝夕はオレンジ寄り、昼は白、夜は青み
            if (targetElevation <= 0f) targetColor = new Color(0.4f, 0.5f, 0.8f);      // 夜
            else if (targetElevation < 20f) targetColor = new Color(1f, 0.6f, 0.35f);  // 朝夕焼け
            else targetColor = Color.white;                                            // 昼
        }

        private void Update()
        {
            if (sun == null) return;
            curElevation = Mathf.MoveTowards(curElevation, targetElevation, followSpeed * 30f * Time.deltaTime);
            curIntensity = Mathf.MoveTowards(curIntensity, targetIntensity, followSpeed * Time.deltaTime);
            curColor = Color.Lerp(curColor, targetColor, followSpeed * Time.deltaTime);

            sun.transform.rotation = Quaternion.Euler(curElevation, sunYaw, 0f);
            sun.intensity = curIntensity * 3.14f; // HDRP の Lux スケールに合わせ調整
            sun.color = curColor;
        }
    }
}
```

- [x] **Step 2: コンパイルを確認**

Expected: コンソールにエラー無し。

- [x] **Step 3: シーンに PhysicallyBasedSky を追加**

- Global Volume に `Add Override > Sky > Physically Based Sky`（または HDRP テンプレ既定の空）を追加。Visual Environment の Sky type を一致させる。
- `SkyController` を `WeatherEffectsRoot` 配下に追加し、`gameManager` と `sun`（シーンの `Sun` DirectionalLight）を割当て。

- [x] **Step 4: Play して目視確認**

Play し、タイムライン上で時刻が朝→昼→夕→夜と進む（または異なる時刻のスナップに切替）と、太陽の角度・明るさ・色が滑らかに変化すること。曇り/雨で全体が暗くなること。Expected: 夜は暗く青み、朝夕はオレンジ、昼は明るい白。

- [x] **Step 5: コミット**

```bash
git add UnityProject/Assets/Scripts/Weather/SkyController.cs UnityProject/Assets/Scenes/MainScene.unity
git commit -m "feat: add sky controller for time-of-day sun and condition lighting"
```

---

## Milestone 6 — タイムライン UI + 情報パネル + エラー処理（MVP 完成）

**完了の目安:** 画面下部のタイムラインで DateTime 表示・再生・スライダー操作ができ、再生中はエフェクトが補間されて滑らかに変化する。右上の情報パネルに都市名・気温・天気コンディションが出る。無キー/通信失敗時はトーストが出てダミーで継続し、起動時に東京が選択される。

> **日本語フォントの前提（M6 のすべての UI タスク共通）:** 都市名・天気コンディションは日本語を含む。TextMeshPro 標準同梱フォントは CJK グリフを含まないため、**日本語 TMP フォントアセットを 1 つ作成**しておく（`Window > TextMeshPro > Font Asset Creator` で日本語対応フォント＝例: Noto Sans JP 等＝から生成。文字セットは常用漢字＋都市名・都道府県名を含む文字範囲）。M6 で配置する各 `TMP_Text` にこのフォントアセットを割り当てること。割り当てを忘れると日本語が「□」で表示される。

### Task 6.1: 連続位置補間 API とタイムライン UI ✅ 実装済み（commit 241c3b0、下部 TimelinePanel、日本語 TMP、MCP 構築）

`WeatherTimelineSO` に連続位置（float）から補間スナップを発火する API を追加し、`TimelineUIController` で再生・スライダーを実装する。

**Files:**
- Modify: `UnityProject/Assets/Scripts/Data/WeatherTimelineSO.cs`（`SetContinuousIndex` 追加）
- Modify: `UnityProject/Assets/Tests/EditMode/WeatherTimelineSOTests.cs`（テスト追加）
- Create: `UnityProject/Assets/Scripts/UI/TimelineUIController.cs`
- Modify: `UnityProject/Assets/Scenes/MainScene.unity`（Canvas / TimelinePanel）

**Interfaces:**
- Consumes: `WeatherSnapshot`, `SnapshotInterpolator`, `GameManager.Timeline`
- Produces:
  - `WeatherTimelineSO.SetContinuousIndex(float pos)` — `pos` を `[0, Count-1]` にクランプし、`floor` と `frac` で `SnapshotInterpolator.Lerp` した補間スナップで `OnSnapshotChanged` を発火。`currentIndex` は `Mathf.RoundToInt(pos)`。
  - `JapanWeatherDemo.UI.TimelineUIController`（MonoBehaviour）

- [x] **Step 1: WeatherTimelineSO のテストを追加（失敗を確認）**

`WeatherTimelineSOTests.cs` の class 内に追記:

```csharp
        [Test]
        public void SetContinuousIndex_InterpolatesBetweenSnapshots()
        {
            var so = ScriptableObject.CreateInstance<WeatherTimelineSO>();
            so.SetData("東京", new[] { Snap(10f), Snap(20f) });
            WeatherSnapshot? got = null;
            so.OnSnapshotChanged += s => got = s;

            so.SetContinuousIndex(0.5f);

            Assert.AreEqual(15f, got.Value.temperatureCelsius, 1e-3f);
        }

        [Test]
        public void SetContinuousIndex_ClampsToRange()
        {
            var so = ScriptableObject.CreateInstance<WeatherTimelineSO>();
            so.SetData("東京", new[] { Snap(10f), Snap(20f) });
            WeatherSnapshot? got = null;
            so.OnSnapshotChanged += s => got = s;

            so.SetContinuousIndex(5f);

            Assert.AreEqual(20f, got.Value.temperatureCelsius, 1e-3f);
        }
```

Test Runner 実行。Expected: `SetContinuousIndex` 未定義で FAIL。

- [x] **Step 2: WeatherTimelineSO に SetContinuousIndex を実装**

`WeatherTimelineSO.cs` の `using` に `using JapanWeatherDemo.Weather;` を追加し、クラス内に追記:

```csharp
        /// <summary>連続位置（0〜Count-1）から補間したスナップショットを発火する。</summary>
        public void SetContinuousIndex(float pos)
        {
            if (Count == 0) return;
            pos = Mathf.Clamp(pos, 0f, snapshots.Length - 1);
            int i = Mathf.FloorToInt(pos);
            int next = Mathf.Min(i + 1, snapshots.Length - 1);
            float frac = pos - i;
            currentIndex = Mathf.RoundToInt(pos);
            OnSnapshotChanged?.Invoke(SnapshotInterpolator.Lerp(snapshots[i], snapshots[next], frac));
        }
```

> 注: `Data` が `Weather` を参照する循環に見えるが、両者は同一アセンブリ `JapanWeatherDemo` 内なので問題ない。

- [x] **Step 3: テストを実行して緑を確認**

Expected: `WeatherTimelineSOTests` の全テスト（追加 2 件含む）が PASS。

- [x] **Step 4: TimelineUIController を実装**

`UnityProject/Assets/Scripts/UI/TimelineUIController.cs`:

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using JapanWeatherDemo.Data;

namespace JapanWeatherDemo.UI
{
    /// <summary>下部タイムライン。再生・スライダーで連続位置を制御し DateTime を表示する。</summary>
    public class TimelineUIController : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Slider slider;
        [SerializeField] private Button playButton;
        [SerializeField] private TMP_Text dateTimeLabel;
        [SerializeField] private TMP_Text playButtonLabel;
        [SerializeField] private float secondsPerSnapshot = 1.0f; // 再生速度

        private bool isPlaying;
        private float pos;
        private bool suppressSliderCallback;

        private WeatherTimelineSO Timeline => gameManager != null ? gameManager.Timeline : null;

        private void OnEnable()
        {
            if (Timeline != null) Timeline.OnSnapshotChanged += OnSnapshot;
            if (slider != null) slider.onValueChanged.AddListener(OnSliderChanged);
            if (playButton != null) playButton.onClick.AddListener(TogglePlay);
        }

        private void OnDisable()
        {
            if (Timeline != null) Timeline.OnSnapshotChanged -= OnSnapshot;
            if (slider != null) slider.onValueChanged.RemoveListener(OnSliderChanged);
            if (playButton != null) playButton.onClick.RemoveListener(TogglePlay);
        }

        // 新しい都市データが入ったらスライダーの範囲を更新する
        public void ConfigureForCurrentData()
        {
            if (Timeline == null || Timeline.Count == 0 || slider == null) return;
            suppressSliderCallback = true;
            slider.minValue = 0;
            slider.maxValue = Timeline.Count - 1;
            slider.value = 0;
            suppressSliderCallback = false;
            pos = 0f;
        }

        private void OnSliderChanged(float v)
        {
            if (suppressSliderCallback) return;
            pos = v;
            Timeline.SetContinuousIndex(pos);
        }

        private void TogglePlay()
        {
            isPlaying = !isPlaying;
            if (playButtonLabel != null) playButtonLabel.text = isPlaying ? "❚❚ 停止" : "▶ 再生";
        }

        private void Update()
        {
            if (!isPlaying || Timeline == null || Timeline.Count < 2) return;
            pos += Time.deltaTime / Mathf.Max(0.01f, secondsPerSnapshot);
            if (pos >= Timeline.Count - 1) { pos = Timeline.Count - 1; isPlaying = false; if (playButtonLabel != null) playButtonLabel.text = "▶ 再生"; }

            suppressSliderCallback = true;
            if (slider != null) slider.value = pos;
            suppressSliderCallback = false;
            Timeline.SetContinuousIndex(pos);
        }

        private void OnSnapshot(WeatherSnapshot s)
        {
            if (dateTimeLabel != null) dateTimeLabel.text = s.dateTime.ToString("yyyy/MM/dd HH:mm");
        }
    }
}
```

- [x] **Step 5: GameManager から UI 再設定を呼ぶ（Modify）**

`GameManager.cs` に `[SerializeField] private TimelineUIController timelineUI;` を追加し、`SelectCity` の `Timeline.SetData(...)` 直後に `if (timelineUI != null) timelineUI.ConfigureForCurrentData();` を追記する。

```csharp
        // SelectCity 内、onResult コールバックの SetData 直後
        Timeline.SetData(city.name, snaps);
        if (timelineUI != null) timelineUI.ConfigureForCurrentData();
```

- [x] **Step 6: シーンに TimelinePanel を構築**

- `Canvas`（Screen Space - Overlay）を作り、子に `TimelinePanel`（下部固定: アンカー下、横いっぱい）。
- `TimelinePanel` 内に `Slider`、`Button`（子 TMP_Text `playButtonLabel`）、`TMP_Text dateTimeLabel`（"2024/06/28 09:00"）を配置。
- 空 GameObject or Canvas に `TimelineUIController` をアタッチし、各参照（`gameManager`/`slider`/`playButton`/`dateTimeLabel`/`playButtonLabel`）を割当て。`GameManager.timelineUI` にこの参照を割当て。

- [x] **Step 7: Play して目視確認**

Play し、再生ボタンで時刻が進みエフェクトが滑らかに変化、スライダーで任意時刻にジャンプでき、DateTime ラベルが JST で更新されること。都市を切り替えるとスライダー範囲が再設定され先頭に戻ること。Expected: 再生・スライダー・ラベルが連動。

- [x] **Step 8: コミット**

```bash
git add UnityProject/Assets/Scripts/Data/WeatherTimelineSO.cs UnityProject/Assets/Tests/EditMode/WeatherTimelineSOTests.cs UnityProject/Assets/Scripts/UI/TimelineUIController.cs UnityProject/Assets/Scripts/GameManager.cs UnityProject/Assets/Scenes/MainScene.unity
git commit -m "feat: add timeline UI with playback and continuous interpolation"
```

### Task 6.2: 情報パネル（InfoPanelController）✅ 実装済み（commit 2d06f0a、右上、Noto Sans JP/OFL、TMP Essentials 導入）

右上固定。選択中の都市名・気温・天気コンディションを `OnSnapshotChanged` で更新する。

**Files:**
- Create: `UnityProject/Assets/Scripts/UI/InfoPanelController.cs`
- Modify: `UnityProject/Assets/Scenes/MainScene.unity`（InfoPanel）

**Interfaces:**
- Consumes: `GameManager.Timeline`（`cityName` と `OnSnapshotChanged`）, `WeatherSnapshot`, `WeatherCondition`
- Produces: `JapanWeatherDemo.UI.InfoPanelController`（MonoBehaviour）

- [x] **Step 1: InfoPanelController を実装**

`UnityProject/Assets/Scripts/UI/InfoPanelController.cs`:

```csharp
using UnityEngine;
using TMPro;
using JapanWeatherDemo.Data;

namespace JapanWeatherDemo.UI
{
    /// <summary>右上の情報パネル。都市名・天気・気温を表示する。</summary>
    public class InfoPanelController : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private TMP_Text cityLabel;
        [SerializeField] private TMP_Text conditionLabel;
        [SerializeField] private TMP_Text temperatureLabel;

        private void OnEnable()
        {
            if (gameManager != null) gameManager.Timeline.OnSnapshotChanged += OnSnapshot;
        }

        private void OnDisable()
        {
            if (gameManager != null && gameManager.Timeline != null)
                gameManager.Timeline.OnSnapshotChanged -= OnSnapshot;
        }

        private void OnSnapshot(WeatherSnapshot s)
        {
            if (cityLabel != null) cityLabel.text = gameManager.Timeline.cityName;
            if (conditionLabel != null) conditionLabel.text = ToJapanese(s.condition);
            if (temperatureLabel != null) temperatureLabel.text = $"{s.temperatureCelsius:0.0}℃";
        }

        private static string ToJapanese(WeatherCondition c) => c switch
        {
            WeatherCondition.Clear => "晴れ ☀",
            WeatherCondition.Cloudy => "曇り ☁",
            WeatherCondition.Rain => "雨 ☂",
            WeatherCondition.Storm => "雷雨 ⚡",
            WeatherCondition.Snow => "雪 ❄",
            _ => "—"
        };
    }
}
```

- [x] **Step 2: コンパイルを確認**

Expected: コンソールにエラー無し。

- [x] **Step 3: シーンに InfoPanel を構築**

- `Canvas` の子に `InfoPanel`（右上アンカー）を作り、`cityLabel`/`conditionLabel`/`temperatureLabel`（TMP_Text）を縦に配置。
- `InfoPanelController` をアタッチし `gameManager` と 3 ラベルを割当て。

- [x] **Step 4: Play して目視確認**

Play し、起動時に「東京 / 晴れ ☀ / 28.5℃」のように表示され、別都市クリックや再生で内容が更新されること。Expected: 都市名・天気・気温が選択とタイムラインに追従。

- [x] **Step 5: コミット**

```bash
git add UnityProject/Assets/Scripts/UI/InfoPanelController.cs UnityProject/Assets/Scenes/MainScene.unity
git commit -m "feat: add info panel showing city, condition and temperature"
```

### Task 6.3: トースト通知・ローディング・初期状態 ✅ 実装済み（commit bc4323f、Toast+Spinner、EventSystem=InputSystemUIInputModule、ライブ取得も確認）

エラー/警告のトースト、取得中ローディングインジケーター、起動時の東京初期選択を仕上げる。

**Files:**
- Create: `UnityProject/Assets/Scripts/UI/ToastController.cs`
- Modify: `UnityProject/Assets/Scripts/GameManager.cs`（`LoadingChanged` イベント追加・fetch 前後で発火）
- Modify: `UnityProject/Assets/Scenes/MainScene.unity`（Toast・Spinner UI）

**Interfaces:**
- Consumes: `GameManager.StatusMessage`, `GameManager.LoadingChanged`
- Produces:
  - `GameManager.event Action<bool> LoadingChanged`
  - `JapanWeatherDemo.UI.ToastController`（MonoBehaviour）: `void Show(string message)`

- [x] **Step 1: GameManager に LoadingChanged を追加（Modify）**

`GameManager.cs` に追記し、`SelectCity` を更新:

```csharp
        public event Action<bool> LoadingChanged;

        private void SelectCity(CityData city)
        {
            LoadingChanged?.Invoke(true);
            weatherService.FetchForecast(
                city,
                snaps =>
                {
                    Timeline.SetData(city.name, snaps);
                    if (timelineUI != null) timelineUI.ConfigureForCurrentData();
                    LoadingChanged?.Invoke(false);
                },
                error =>
                {
                    StatusMessage?.Invoke(error);
                    LoadingChanged?.Invoke(false);
                });
        }
```

> 注: 無キー/失敗時は `WeatherService` が `onError` の後に `onResult`（ダミー）も呼ぶため、`LoadingChanged(false)` は両コールバックで呼ばれるが冪等で問題ない。

- [x] **Step 2: ToastController を実装**

`UnityProject/Assets/Scripts/UI/ToastController.cs`:

```csharp
using System.Collections;
using UnityEngine;
using TMPro;

namespace JapanWeatherDemo.UI
{
    /// <summary>画面上部に一定時間メッセージを表示して自動で消えるトースト。</summary>
    public class ToastController : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text label;
        [SerializeField] private float showSeconds = 3f;

        private Coroutine routine;

        private void OnEnable()
        {
            if (gameManager != null) gameManager.StatusMessage += Show;
            if (group != null) group.alpha = 0f;
        }

        private void OnDisable()
        {
            if (gameManager != null) gameManager.StatusMessage -= Show;
        }

        public void Show(string message)
        {
            if (label != null) label.text = message;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(ShowRoutine());
        }

        private IEnumerator ShowRoutine()
        {
            if (group != null) group.alpha = 1f;
            yield return new WaitForSeconds(showSeconds);
            float t = 0f;
            while (t < 1f && group != null) { t += Time.deltaTime; group.alpha = 1f - t; yield return null; }
            if (group != null) group.alpha = 0f;
        }
    }
}
```

- [x] **Step 3: ローディングスピナーを実装（Modify or 小コンポーネント）**

`Canvas` の子に `Spinner`（回転する Image）を作り、`LoadingChanged` で表示/非表示を切り替える小スクリプト。`ToastController.cs` と同じ `UI` 名前空間に `LoadingIndicator.cs` を追加:

`UnityProject/Assets/Scripts/UI/LoadingIndicator.cs`:

```csharp
using UnityEngine;

namespace JapanWeatherDemo.UI
{
    /// <summary>取得中に回転表示するローディングインジケーター。</summary>
    public class LoadingIndicator : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private RectTransform spinner;
        [SerializeField] private float degPerSecond = 180f;

        private bool loading;

        private void OnEnable()
        {
            if (gameManager != null) gameManager.LoadingChanged += OnLoading;
            SetVisible(false);
        }

        private void OnDisable()
        {
            if (gameManager != null) gameManager.LoadingChanged -= OnLoading;
        }

        private void OnLoading(bool isLoading) { loading = isLoading; SetVisible(isLoading); }

        private void SetVisible(bool v) { if (spinner != null) spinner.gameObject.SetActive(v); }

        private void Update()
        {
            if (loading && spinner != null) spinner.Rotate(0f, 0f, -degPerSecond * Time.deltaTime);
        }
    }
}
```

- [x] **Step 4: シーンに Toast / Spinner を構築し配線**

- `Canvas` の子に `Toast`（上部中央、`CanvasGroup` + 背景 + TMP_Text）を作り、`ToastController` をアタッチして `gameManager`/`group`/`label` を割当て。
- `Canvas` の子に `Spinner`（中央、回転 Image）を作り、`LoadingIndicator` をアタッチして `gameManager`/`spinner` を割当て。

- [x] **Step 5: Play して全体を目視確認（受け入れ確認）**

1. キー未設定で Play → 起動直後に「API キー未設定: ダミーデータで表示します」トーストが出て、東京が初期選択され情報パネル・エフェクトが動く。
2. （任意）`StreamingAssets/config.json` に有効キーを置いて Play → 実データで東京の予報が表示され、取得中はスピナーが回る。
3. 機内モード等でネットを切って有効キーのまま Play → 「取得失敗…ダミーデータで表示します」トースト後にダミーで継続。
4. マーカークリック・再生・スライダー・カメラ操作がすべて動く。

Expected: いずれの場合もアプリが破綻せず、トースト/スピナー/初期都市が仕様どおり。

- [x] **Step 6: コミット**

```bash
git add UnityProject/Assets/Scripts/UI/ToastController.cs UnityProject/Assets/Scripts/UI/LoadingIndicator.cs UnityProject/Assets/Scripts/GameManager.cs UnityProject/Assets/Scenes/MainScene.unity
git commit -m "feat: add toast notifications, loading indicator and initial state"
```

---

## MVP 完成チェックリスト

すべての Play Mode 確認が通り、EditMode テスト（`GeoProjection` / `ConditionMapper` / `TimeZone` / `WeatherParser` / `WeatherTimelineSO` / `DummyWeather` / `ApiKeyResolver` / `SnapshotInterpolator` / `SunAngle` / `CityCatalog`）が全緑であること。

- [x] M1: 地図表示＋自由カメラ操作
- [x] M2: 150〜200 都市の光柱マーカーが正しい位置に配置
- [x] M3: 予報取得→`WeatherSnapshot[]`（JST）→`WeatherTimelineSO`、無キー/失敗でダミー成立
- [x] M4: マーカークリック→取得→`OnSnapshotChanged` フロー
- [x] M5: 雲・雨・雪・時間帯連動の空がコンディションに追従
- [x] M6: タイムライン再生・スライダー・情報パネル・トースト・初期都市（東京）
- [x] README に API キー設定手順（`config.example.json` → `config.json`）を記載

---

## 実装フェーズで確定する細部（spec より引き継ぎ）

- **アマノ技研データの測地系**（Task 2.2 Step 6）: WGS84 か日本測地系かを CSV 取り込み時に確認し、日本測地系なら WGS84 へ変換。
- **Natural Earth テクスチャの実図郭**（Task 1.3 Step 2）: トリミング四隅の lat/lon を `MapBounds` に厳密反映。
- **150〜200 都市の抽出条件**（Task 2.2 Step 6）: `PrefectureCapitals` ＋ `ExtraMajorCities` を調整し件数を範囲内に。
- **HDRP の空種別**（Task 5.5）: テンプレ既定の Sky type と `SkyController` の前提（PhysicallyBasedSky）を一致させる。

