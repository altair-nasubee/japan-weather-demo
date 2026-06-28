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
    public DateTime dateTime;
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

**エンドポイント（5 日間予報）：**
```
GET https://api.openweathermap.org/data/2.5/forecast
    ?q={city_name},{JP}&appid={API_KEY}&units=metric&cnt=40
```

- 無料枠：1,000 リクエスト/日（クレジットカード登録不要）
- API キーは Unity の StreamingAssets または環境変数で管理（ソースコードに直書きしない）

---

## 未決事項

- [ ] 日本地図テクスチャの素材調達（ライセンス確認要）
- [ ] 都市マスタ（Cities.json）の作成方法（手作業 or スクリプト自動生成）
- [ ] OpenWeatherMap API キーの管理方法（ビルド配布時の扱い）
- [ ] 都市マーカーのビジュアルデザイン（3D シリンダー、ビルボード、光柱など）
