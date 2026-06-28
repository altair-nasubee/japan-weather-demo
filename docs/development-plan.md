# Japan Weather Demo — 開発計画書

## 概要

Unity (HDRP) で作る Windows 向け天気可視化デモアプリ。  
日本各地の実況・予報データをリアルタイムで取得し、3D 空間上の日本地図と気象エフェクトで視覚的に表現する。  
ポートフォリオ・技術デモとしての完成度を目指す。

---

## 目的と見せ場

- **目的**：ポートフォリオ・技術デモ
- **見せ場①**：OpenWeatherMap の予報データを取得し、HDRP の Volumetric Clouds・VFX Graph・PhysicallyBasedSky をリアルタイムに制御する気象エフェクト
- **見せ場②**：日本地図上に都市マーカーを配置し、クリックで都市を選択するインタラクティブな地図 UI
- **見せ場③**：5 日間・3 時間刻みのタイムラインで天気の変化をアニメーション再生する時間軸 UI

---

## 実行環境

| 項目 | 値 |
|------|----|
| Unity | 6000.3.18f1 LTS |
| レンダーパイプライン | HDRP |
| プラットフォーム | Windows 64bit |
| 表示形式 | ボーダーレスウィンドウ（実質フルスクリーン） |
| ターゲット解像度 | 1920×1080 |
| Ray Tracing | ON/OFF 切り替え可能（RTX GPU 推奨） |

---

## MVP スコープ

MVP として最初に完成させる機能。

1. **天気データ取得**：OpenWeatherMap Forecast API（5 日間・3 時間刻み・40 点）
2. **日本地図**：地図テクスチャを貼った Plane + 都市マーカー 150〜200 都市
3. **都市選択**：マーカークリックで都市を選択 → API 取得 → エフェクト更新
4. **基本天気エフェクト**：雲・雨・晴れ + 時間帯連動の空（朝焼け・昼・夕焼け・夜）
5. **タイムライン UI**：DateTime 表示・再生ボタン・スライダー（画面下部固定）
6. **自由カメラ**：マウス + キーボードで回転・ズーム・パン

## MVP 後の拡張機能

優先度順。

1. 雷エフェクト（Storm コンディション時）
2. 風矢印グリッド ON/OFF（風速・風向をグリッド上にアニメーション矢印で表示）
3. 2D 検索パネル（左側固定、都道府県ツリー + テキスト検索）
4. Ray Tracing ON/OFF 切り替えボタン（右上 UI）
5. 雪エフェクト（Snow コンディション時）

---

## アーキテクチャ

### 設計方針

**ScriptableObject + C# Event による疎結合構成。**

天気データを `WeatherTimelineSO`（ScriptableObject）に集約し、C# event で各コントローラーに伝播する。  
UI・エフェクト・マップは互いを直接参照せず、イベント経由で疎結合に保つ。

### データフロー

```
OpenWeatherMap API
    ↓ (WeatherService.FetchForecast)
WeatherTimelineSO (ScriptableObject)
    ↓ (event OnSnapshotChanged)
    ├→ CloudController        — Volumetric Clouds の濃さ・形状
    ├→ PrecipitationController — 雨・雪パーティクルの密度
    ├→ SkyController          — PhysicallyBasedSky・DirectionalLight の角度
    └→ UIController           — DateTime 表示・スライダー同期
```

---

## プロジェクト構成

```
UnityProject/Assets/
├── Scenes/
│   └── MainScene.unity
├── Scripts/
│   ├── Data/
│   │   ├── WeatherTimelineSO.cs    — タイムラインデータ（ScriptableObject）
│   │   ├── WeatherSnapshot.cs      — 1 時刻分のデータ構造体
│   │   └── CityData.cs             — 都市名・座標・都道府県
│   ├── Weather/
│   │   ├── WeatherService.cs       — API 取得・パース
│   │   ├── CloudController.cs      — Volumetric Clouds 制御
│   │   ├── PrecipitationController.cs — 雨・雪 VFX Graph 制御
│   │   └── SkyController.cs        — 空・太陽光の時間帯制御
│   ├── Map/
│   │   ├── MapManager.cs           — 地図・マーカー管理
│   │   └── CityMarker.cs           — マーカー 1 個の選択処理
│   ├── Camera/
│   │   └── FreeCameraController.cs — 自由カメラ操作
│   └── UI/
│       └── TimelineUIController.cs — タイムライン UI 制御
├── Data/
│   └── Cities.json                 — 都市マスタ（150〜200 都市）
└── Resources/
    └── Textures/
        └── JapanMap.png            — 日本地図テクスチャ
```

---

## シーン構成（MainScene）

```
MainScene
├── [Map]    JapanMapPlane            — 日本地図 Plane（y=0）
├── [Map]    CityMarkers (Parent)     — 都市マーカー群
├── [Camera] MainCamera              — 自由カメラ
├── [Effects] WeatherEffectsRoot
│   ├── VolumetricCloudsVolume       — HDRP Volumetric Clouds
│   ├── RainVFX                      — VFX Graph（雨）
│   └── SnowVFX                      — VFX Graph（雪）
├── [Lighting] Sun (DirectionalLight) — 時間帯連動
├── [UI]     Canvas
│   └── TimelinePanel                — 下部固定 UI
└── [System] GameManager
```

---

## 地図と座標系

### 投影方式

**線形（等緯度経度）マッピング。** 日本のバウンディングボックスを地図 Plane の XZ 平面に線形変換する。フラットな地図テクスチャと相性が良く、デモ用途には十分。

- 緯度経度の範囲（暫定）：`lat 24〜46` / `lon 123〜146`（沖縄〜北海道を含む）。
- 使用する地図テクスチャの実際の図郭に合わせて範囲は微調整する（未決：テクスチャ素材確定後）。

```
x = Mathf.Lerp(planeMinX, planeMaxX, Mathf.InverseLerp(lonMin, lonMax, city.lon));
z = Mathf.Lerp(planeMinZ, planeMaxZ, Mathf.InverseLerp(latMin, latMax, city.lat));
y = マーカー高さ（地図 Plane 上に浮かせる固定オフセット）
```

- 変換ロジックは純粋関数として切り出し、EditMode 単体テストで緯度経度→XZ の対応を検証する。
- 地図テクスチャの図郭（範囲）とマーカー座標は必ず同じ範囲定数を共有させ、ズレを防ぐ。

---

## データ設計

### WeatherTimelineSO

```csharp
public class WeatherTimelineSO : ScriptableObject
{
    public string cityName;
    public WeatherSnapshot[] snapshots;  // 40 点（5 日 × 8 スロット/日）
    public int currentIndex;

    public event Action<WeatherSnapshot> OnSnapshotChanged;

    public void SetIndex(int index)
    {
        currentIndex = Mathf.Clamp(index, 0, snapshots.Length - 1);
        OnSnapshotChanged?.Invoke(snapshots[currentIndex]);
    }
}
```

### WeatherSnapshot

```csharp
[Serializable]
public struct WeatherSnapshot
{
    public DateTime dateTime;            // JST（UTC+9 に変換済み）
    public WeatherCondition condition;   // Clear / Cloudy / Rain / Storm / Snow
    public float cloudCoverage;          // 0〜1
    public float windSpeed;              // m/s
    public float windDirectionDeg;       // 0〜360
    public float rainIntensity;          // 0〜1
    public float temperatureCelsius;
}
```

### Cities.json（構造例）

```json
[
  { "name": "札幌",  "lat": 43.0642, "lon": 141.3468, "prefecture": "北海道" },
  { "name": "函館",  "lat": 41.7687, "lon": 140.7290, "prefecture": "北海道" },
  { "name": "東京",  "lat": 35.6895, "lon": 139.6917, "prefecture": "東京都" }
]
```

---

## 天気エフェクト仕様

### コンディション別演出

| WeatherCondition | Volumetric Clouds | 雨パーティクル | 雷 | 空の色 |
|-----------------|-------------------|--------------|-----|--------|
| Clear   | なし〜薄い | なし | なし | 青・オレンジ（時間帯連動） |
| Cloudy  | 厚い       | なし | なし | グレー |
| Rain    | 中程度     | あり（中） | なし | 暗いグレー |
| Storm   | 非常に厚い | あり（強） | あり | 暗い |
| Snow    | 薄い白     | 雪パーティクル | なし | 白みがかった青 |

### 時間帯連動

`WeatherSnapshot.dateTime` の時刻に応じて以下を補間：

- `DirectionalLight` の角度（太陽・月の位置）
- `PhysicallyBasedSky` の露光・色温度
- 朝（5〜7時）・昼（10〜14時）・夕（17〜19時）・夜（20〜4時）を滑らかに遷移

### タイムライン補間

隣接する 2 スナップショット間（3 時間）をエフェクトパラメーターで線形補間し、スライダー操作・再生中の視覚変化をなめらかにする。

---

## UI レイアウト

```
┌──────────────────────────────────────────────────────────┐
│  [検索パネル※]              3D ビュー                    │
│  ┌────────────┐                                          │
│  │ 🔍 都市検索 │                           [Ray Tracing] │
│  │────────────│                                          │
│  │ 北海道      │                                          │
│  │  > 札幌    │                                          │
│  │  > 函館    │                                          │
│  │ 東京都      │                                          │
│  │  > 東京    │                                          │
│  └────────────┘                                          │
├──────────────────────────────────────────────────────────┤
│  ◀ 2024/06/28 09:00 ▶   [▶ 再生]   [━━━━●━━━━━━━━━━━━]  │
└──────────────────────────────────────────────────────────┘
```

※ 検索パネルは MVP 後の拡張機能。MVP では地図上のマーカークリックのみで都市選択。

---

## カメラ操作

| 操作 | 動作 |
|------|------|
| マウス右ドラッグ | 回転（オービット） |
| マウスホイール | ズームイン/アウト |
| 中ボタンドラッグ | パン |
| W / A / S / D | パン移動 |
| Q / E | 高度上下 |

---

## 天気 API

**エンドポイント（5 日間・3 時間刻み予報）：**
```
GET https://api.openweathermap.org/data/2.5/forecast
    ?lat={lat}&lon={lon}&appid={API_KEY}&units=metric&cnt=40
```

- **座標指定（`lat`/`lon`）を使う。** 都市名 `q` 指定は非推奨（deprecated）で、日本語都市名のヒット精度も不安定なため使わない。`Cities.json` が保持する緯度経度をそのまま渡す。
- 無料枠：1,000 リクエスト/日（クレジットカード登録不要）。都市選択 1 回 = 1 リクエスト。
- 同一都市を再選択した場合は直近取得結果をメモリキャッシュして無駄なリクエストを避ける（簡易キャッシュ、有効期限の厳密管理は不要）。
- API キーは Unity の StreamingAssets または環境変数で管理（ソースコードに直書きしない）。

### タイムゾーン処理

- API の `dt` は **UTC**、レスポンスの `city.timezone` に UTC オフセット秒が入る。
- 日本向けデモのため、`WeatherSnapshot.dateTime` および UI 表示はすべて **JST（UTC+9）** に変換して扱う。

### 天気コード → WeatherCondition 対応

OpenWeatherMap の `weather[0].id`（コードグループ）を 5 区分にマッピングする。

| weather id | グループ | WeatherCondition |
|-----------|---------|------------------|
| 2xx | 雷雨（Thunderstorm） | Storm |
| 3xx / 5xx | 霧雨・雨（Drizzle / Rain） | Rain |
| 6xx | 雪（Snow） | Snow |
| 7xx | 大気現象（霧・もや等） | Cloudy |
| 800 | 快晴（Clear） | Clear |
| 80x | 雲量あり（Clouds） | Cloudy（801〜802 は薄い、803〜804 は厚い扱い） |

`cloudCoverage` は API の `clouds.all`（0〜100%）を 0〜1 に正規化して使用する。

---

## 初期状態とエラーハンドリング

### 初期状態

- 起動時に **東京** を初期選択し、自動で予報を取得してエフェクト・UI を初期化する。
- タイムラインの初期 `currentIndex` は 0（最初のスナップショット）。

### エラーハンドリング

| 状況 | 挙動 |
|------|------|
| ネットワーク無し / API 失敗 / タイムアウト | 画面にトースト通知でエラー表示し、**デモ用ダミーデータ**（同梱の固定スナップショット列）でエフェクト・UI を継続。アプリは破綻させない |
| API キー未設定 | 起動時に警告トーストを出し、ダミーデータモードで動作 |
| レスポンスのパース失敗 | 当該都市の更新を中断し、トースト表示。直前の表示状態を維持 |

- ダミーデータは `StreamingAssets` または `Resources` に固定 JSON として同梱し、オフライン・無キーでもデモが成立するようにする。
- 取得中はローディングインジケーター（スピナー等）を表示する。

---

## テスト方針

- **ロジックは EditMode 単体テスト（Unity Test Runner）**：
  - API レスポンスのパース（JSON → `WeatherSnapshot[]`）
  - 天気コード（`weather.id`）→ `WeatherCondition` のマッピング
  - 緯度経度 → 地図 XZ 座標の変換
  - UTC → JST 変換
  - スナップショット間の線形補間
- **見た目・挙動は Play Mode で目視確認**：エフェクト（雲・雨・空）、マーカークリック、カメラ操作、タイムライン再生・スライダー同期。
- 純ロジックは MonoBehaviour 非依存のクラス／静的関数に切り出し、テスト容易性を確保する。

---

## 実装マイルストーン

検証可能な単位で段階的に進める。各マイルストーン完了時に動作確認とコミットを行う。

| # | マイルストーン | 完了の目安（動作確認） |
|---|--------------|----------------------|
| 1 | 地図 + 自由カメラ | 日本地図 Plane が表示され、マウス/キーでカメラ操作できる |
| 2 | 都市マスタ + マーカー配置 | `Cities.json` を読み込み、緯度経度→XZ でマーカーが正しい位置に並ぶ |
| 3 | API 取得 + データ層 | 指定都市の予報を取得し `WeatherTimelineSO` に格納（エラー時ダミーデータ含む） |
| 4 | 都市選択フロー | マーカークリック → 取得 → `OnSnapshotChanged` 発火までが繋がる |
| 5 | 基本天気エフェクト | 雲・雨・晴れ + 時間帯連動の空がコンディションに応じて切り替わる |
| 6 | タイムライン UI | DateTime 表示・再生・スライダーでスナップショットを補間再生できる |
| — | （ここまでで MVP 完成） | |
| 7+ | 拡張機能 | 雷 → 風矢印 → 検索パネル → Ray Tracing 切替 → 雪（優先度順） |

---

## 未決事項

- [ ] 日本地図テクスチャの素材調達（ライセンス確認要）
- [ ] 都市マスタ（Cities.json）の作成方法（手作業 or スクリプト自動生成）
- [ ] OpenWeatherMap API キーの管理方法（ビルド配布時の扱い）
- [ ] 都市マーカーのビジュアルデザイン（3D シリンダー、ビルボード、光柱など）
