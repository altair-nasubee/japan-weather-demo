# Japan Weather Demo — 開発計画書

## 概要

Unity (HDRP) で作る Windows 向け天気可視化デモアプリ。  
日本各地の予報データをリアルタイムで取得し、3D 空間上の日本地図と気象エフェクトで視覚的に表現する。  
ポートフォリオ・技術デモとしての完成度を目指す。

### 実装状況（as-built、2026-06-30 時点）

| 区分 | 状態 |
|------|------|
| MVP（M1〜M6） | ✅ 完了 |
| 都市ドロップダウン + カメラフォーカス | ✅ 完了（[`spec`](superpowers/specs/2026-06-30-city-dropdown-camera-focus-design.md)） |
| ビルボードピンマーカー | ✅ 完了（光柱から置き換え済み。[`spec`](superpowers/specs/2026-06-30-billboard-pin-markers-design.md)） |
| EditMode テスト | **46 件** PASS |
| 都市マスタ | `Cities.json` **100 都市**（県庁所在地・政令市・中核市・主要市） |
| Windows ビルド配布 | 未着手（Play 動作確認までが MVP 完了基準） |

---

## 目的と見せ場

- **目的**：ポートフォリオ・技術デモ
- **見せ場①**：OpenWeatherMap の予報データを取得し、HDRP の PhysicallyBasedSky・Bloom・雲レイヤー・降水パーティクル をリアルタイムに制御する気象エフェクト
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
| 入力 | 新 Input System（Input System Package） |

---

## MVP スコープ

MVP として最初に完成させる機能。

1. **天気データ取得**：OpenWeatherMap Forecast API（5 日間・3 時間刻み・40 点）
2. **日本地図**：地図テクスチャを貼った Plane + 都市マーカー（100 都市）
3. **都市選択**：マーカークリックまたは右上ドロップダウンで都市を選択 → API 取得 → エフェクト更新（選択時にカメラが斜め見下ろし構図へフォーカス）
4. **基本天気エフェクト**：雲・雨・雪 + 時間帯連動の空（朝焼け・昼・夕焼け・夜）
5. **タイムライン UI**：DateTime 表示・再生ボタン・スライダー（画面下部固定）
6. **情報パネル**：選択中の天気コンディション・気温を表示（右上固定。都市名はドロップダウンが担当）
7. **自由カメラ**：マウス + キーボードで回転・ズーム・パン

> **MVP の完了基準は Unity エディタでの Play 動作確認まで**とする。ボーダーレス 1920×1080 での Windows ビルド・配布（Player Settings・実行ファイル化）は拡張フェーズで対応する。

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

**初期化順序：** `GameManager` が `WeatherTimelineSO` のランタイムインスタンスを保持し、各コントローラー（UI・エフェクト）はそれを購読する。

> **実装での調整（as-built）**：`Timeline` は `Awake` で生成するのではなく、**初回アクセス時に遅延生成するプロパティ**にしている（`Timeline => _timeline ??= ScriptableObject.CreateInstance<WeatherTimelineSO>()`）。これにより、どのコントローラーが先に購読しても初回アクセスでインスタンスが用意され、購読が生成より先に走る競合が原理的に起きない。Script Execution Order でも `GameManager` を先（-100）に設定しているが、生成順序への依存はこの遅延生成で解消済み。

### データフロー

```
OpenWeatherMap API
    ↓ (WeatherService.FetchForecast)
WeatherTimelineSO (ScriptableObject)
    ↓ (event OnSnapshotChanged)
    ├→ CloudController        — 雲レイヤー（半透明メッシュ）の濃さ・色
    ├→ PrecipitationController — 雨・雪パーティクルの密度
    ├→ SkyController          — PhysicallyBasedSky・DirectionalLight の角度
    ├→ TimelineUIController   — DateTime 表示・スライダー同期
    └→ InfoPanelController    — 天気・気温の表示

MapManager（マーカー配置・選択）
    ↓ (event CitySelected / CityFocused)
    ├→ GameManager            — 都市選択 → 予報取得 → タイムライン更新
    ├→ CityDropdownController — ドロップダウン ⇄ マーカーの双方向同期
    └→ CameraFocusController  — 選択地点の斜め見下ろし構図へカメラ移動
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
│   │   ├── CloudController.cs      — 雲レイヤー（半透明メッシュ）制御
│   │   ├── PrecipitationController.cs — 雨・雪 ParticleSystem 制御
│   │   └── SkyController.cs        — 空・太陽光の時間帯制御
│   ├── Map/
│   │   ├── MapManager.cs           — 地図・マーカー管理
│   │   ├── CityMarker.cs           — ビルボードピン 1 個の選択・ホバー処理
│   │   └── BillboardScale.cs       — 画面一定サイズのスケール計算（純関数）
│   ├── Camera/
│   │   ├── FreeCameraController.cs — 自由カメラ操作
│   │   ├── CameraFocusController.cs — 都市選択時のカメラ移動
│   │   └── CameraFraming.cs        — フォーカス構図の姿勢算出（純関数）
│   ├── UI/
│   │   ├── TimelineUIController.cs — タイムライン UI 制御
│   │   ├── InfoPanelController.cs  — 天気・気温の表示
│   │   ├── CityDropdownController.cs — 都市選択ドロップダウン
│   │   ├── ToastController.cs      — トースト通知
│   │   └── LoadingIndicator.cs     — 取得中スピナー
│   └── GameManager.cs              — 起動時配線・都市選択のハブ
├── StreamingAssets/
│   └── Cities.json                 — 都市マスタ（100 都市）
└── Textures/
    ├── JapanMap.png                — 日本地図テクスチャ
    └── Icons/pin.png               — マーカーピン画像
```

---

## シーン構成（MainScene）

```
MainScene
├── [Map]    JapanMapPlane            — 日本地図 Plane（y=0）
├── [Map]    CityMarkers (Parent)     — 都市マーカー群
├── [Camera] MainCamera              — 自由カメラ
├── [Effects] WeatherEffectsRoot
│   ├── CloudLayer       — 半透明メッシュ雲レイヤー（地図上空 Plane）
│   ├── RainPS                      — ParticleSystem（雨）
│   └── SnowPS                      — ParticleSystem（雪）
├── [Lighting] Sun (DirectionalLight) — 時間帯連動
├── [UI]     Canvas
│   └── TimelinePanel                — 下部固定 UI
└── [System] GameManager
```

---

## 地図と座標系

### 投影方式

**線形（等緯度経度）マッピング。** 日本のバウンディングボックスを地図 Plane の XZ 平面に線形変換する。フラットな地図テクスチャと相性が良く、デモ用途には十分。

- 緯度経度の範囲：`lat 24〜46.5` / `lon 122〜146.5`（沖縄〜北海道を含む）。地図テクスチャの図郭に合わせて微調整する。
- 等緯度経度（Plate Carrée）のため日本は実際よりやや縦長に見える（デモ用途では許容）。地図 Plane のアスペクト比は **経度幅:緯度幅** をテクスチャ図郭に一致させ、Plane の実寸と範囲定数（`MapBounds`）を必ず揃える。

```
x = Mathf.Lerp(planeMinX, planeMaxX, Mathf.InverseLerp(lonMin, lonMax, city.lon));
z = Mathf.Lerp(planeMinZ, planeMaxZ, Mathf.InverseLerp(latMin, latMax, city.lat));
y = マーカー高さ（地図 Plane 上に浮かせる固定オフセット）
```

- 変換ロジックは純粋関数として切り出し、EditMode 単体テストで緯度経度→XZ の対応を検証する。
- 地図テクスチャの図郭（範囲）とマーカー座標は必ず同じ範囲定数を共有させ、ズレを防ぐ。

### 地図テクスチャ

- **Natural Earth のラスタ地図（パブリックドメイン）を日本のバウンディングボックスにトリミングして使用。**
  - パブリックドメインのため出典表示・許諾は不要（ライセンス的に安全）。
  - 等緯度経度（Plate Carrée）系のため、上記の線形マッピングとそのまま一致する。
- 範囲定数（lat/lon の min/max）はテクスチャのトリミング範囲とマーカー配置で共有する。
- 出典: [Natural Earth](https://www.naturalearthdata.com/)（Public Domain）。

### 都市マーカー（ビジュアル）

> **実装での調整（as-built）**：MVP 時点では Emissive 光柱で表現していたが、2026-06-30 に **ビルボードピン**（World-Space UGUI Canvas + `pin.png` + 地点名ラベル）へ置き換え済み。詳細は [`billboard-pin-markers-design.md`](superpowers/specs/2026-06-30-billboard-pin-markers-design.md) を参照。

- 各都市に **ピンアイコン画像** を常時表示。ピンは常にカメラへ正対し、画面上の見た目サイズを一定に保つ（`BillboardScale` 純関数）。
- **地点名ラベル** はホバー中または選択中のみ表示（密集地での重なりを避ける）。
- 選択時はピン色を変更してハイライト。クリック／ホバー判定は `MapManager` が Physics.Raycast で行う（UI EventSystem と競合させない）。
- クリック判定用に各マーカー root へ `BoxCollider` を持たせる（`CityMarker.cs`）。

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

- 保存アセットではなく**ランタイムデータコンテナ**として扱う。実行時に `ScriptableObject.CreateInstance` で 1 インスタンスだけ生成し、都市を切り替えるたびに `snapshots` を入れ替えて再利用する。
- 各コントローラーは起動時にこのインスタンスの `OnSnapshotChanged` を購読する。

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

**データソースと生成方法：**

- [アマノ技研「地方公共団体の位置データ」](https://amano-tec.com/data/localgovernments.html)（全国 1,959 件・2026 年 1 月時点）をベースに使う。
  - **研究・商用とも無料、出典表記の要求なし**（ライセンス的に安全）。
  - CSV に jiscode・name・住所・緯度経度などを含む。
- **生成スクリプト**（`CitiesJsonGenerator` エディタ拡張）で CSV を読み込み、**県庁所在地＋政令指定都市＋中核市＋主要市**を抽出して `Cities.json` を出力する（現行 **100 都市**）。手作業は座標ミスを招くため避ける。
- 測地系は WGS84 として扱う（OpenWeatherMap・地図表示と整合。元データの測地系は取り込み時に確認する）。

---

## 天気エフェクト仕様

### コンディション別演出

| WeatherCondition | 雲レイヤー | 降水パーティクル | 雷 | 空の色 |
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

> **実装での調整（as-built）**：夜でも地図がはっきり視認できるよう、`SkyController` は夜間に光量を真っ暗にせず最低照度（intensity `0.99`）を確保する。さらに太陽が地平線下にあるときも光が地図の上面に当たるよう、照射角度（pitch）に下限（12°）を設けている。色味は夜＝青み・朝夕＝オレンジ・昼＝白のまま。HDRP では `HDAdditionalLightData.intensity`（Lux）が実効値のため、太陽光量はそちらで制御する。

### タイムライン補間

隣接する 2 スナップショット間（3 時間）を線形補間し、スライダー操作・再生中の視覚変化をなめらかにする。

**補間の責務とデータフロー：** 再生・スライダーの連続位置（小数インデックス）は `WeatherTimelineSO` が隣接 2 スナップショットを線形補間し、補間済みの 1 スナップショットを `OnSnapshotChanged` で配信する（離散ジャンプ用の `SetIndex` と同じイベント経路）。各コントローラーはイベントで受け取った値を**目標値**として毎フレーム滑らかに追従する。これにより、購読側は離散/連続を区別せず、視覚的に滑らかな変化が得られる。

---

## UI レイアウト

```
┌──────────────────────────────────────────────────────────┐
│  [検索パネル※]        3D ビュー       ┌──────────────────┐ │
│  ┌────────────┐           [Ray Tracing]│ [東京 ▼]         │ │
│  │ 🔍 都市検索 │                       │ 晴れ ☀           │ │
│  │────────────│                       │ 28.5℃            │ │
│  │ 北海道      │                       └──────────────────┘ │
│  │  > 札幌    │                        ← 情報パネル         │
│  │  > 函館    │                                            │
│  │ 東京都      │                                            │
│  │  > 東京    │                                            │
│  └────────────┘                                            │
├──────────────────────────────────────────────────────────┤
│  ◀ 2024/06/28 09:00 ▶   [▶ 再生]   [━━━━●━━━━━━━━━━━━]  │
└──────────────────────────────────────────────────────────┘
```

- **情報パネル（右上固定・MVP）**：都市選択ドロップダウン・天気コンディション・気温を表示。天気・気温は `OnSnapshotChanged` で更新。ドロップダウンは `MapManager` と双方向同期。
- **タイムラインの再生/一時停止ボタン（下部固定・MVP）**：絵文字テキストではなくアイコン Sprite（`Assets/Textures/Icons/icon_play.png` 三角・`icon_pause.png` 二本線）を `Image` に表示し、`TimelineUIController` が再生状態に応じて sprite を切り替える。
- ※ **検索パネル（左上・MVP 後の拡張機能）**：都道府県ツリー＋テキスト検索。現状はドロップダウンとマーカークリックで都市選択。

### UI の依存

- UI は TextMeshPro（uGUI）を使用する。都市名・天気コンディションなど**日本語を表示するため、CJK グリフを含む日本語 TMP フォントアセット（実装では Noto Sans JP / SIL OFL、Dynamic 生成）を用意する**（TextMeshPro 標準同梱フォントは日本語グリフを含まない）。実装初期にフォントアセットを生成しておく。

---

## カメラ操作

| 操作 | 動作 |
|------|------|
| 左クリック（都市マーカー） | 都市を選択（カメラがフォーカス移動） |
| マーカーにホバー | 地点名ラベルを表示 |
| 右上ドロップダウン | リストから都市を選択 |
| マウス右ドラッグ | 回転（オービット） |
| マウスホイール | ズームイン/アウト |
| 中ボタンドラッグ | パン |
| W / A / S / D | パン移動 |
| Q / E | 高度上下 |

- 入力は**新 Input System（Input System Package）**を使用する。マーカークリックも同パッケージのポインター入力＋Physics Raycast で判定する。
- マーカークリックの Raycast は、ポインターが UI（タイムライン・情報パネル等）の上にあるとき（`EventSystem.current.IsPointerOverGameObject()`）は無視し、UI 操作と地図クリックを競合させない。

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
- API キーはソースコードに直書きしない（後述「API キー管理と配布」参照）。

### API キー管理と配布

- **既定はダミーデータ動作。** API キーが無くてもアプリは同梱のダミーデータでデモが成立する（「初期状態とエラーハンドリング」と連動）。
- **ライブ取得はオプトイン。** `StreamingAssets/config.json`（`.gitignore` 対象、ビルド配布物にも含めない）にキーを置いたユーザーのみライブ天気が有効になる。
  - 開発中は環境変数からの読み込みも許可する（`config.json` 優先 → 環境変数 → 無ければダミーデータ）。
- キーをリポジトリ・配布ビルドに含めないため、漏洩・レート超過のリスクを回避できる。
- `config.example.json`（キー空欄のサンプル）をリポジトリに置き、設定方法を README に記載する。

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

- `cloudCoverage` は API の `clouds.all`（0〜100%）を 0〜1 に正規化して使用する。
- `rainIntensity` は API の `rain.3h`（mm/3h、無い場合は 0）を上限値で正規化（例: 10mm/3h で 1.0 とクランプ）して使用する。
- `windSpeed`・`windDirectionDeg`・`temperatureCelsius` は API の `wind.speed`・`wind.deg`・`main.temp` をそのまま使う。

---

## 初期状態とエラーハンドリング

### 初期状態

- 起動時に **東京** を初期選択し、自動で予報を取得してエフェクト・UI を初期化する。
- タイムラインの初期 `currentIndex` は 0（最初のスナップショット）。

### エラーハンドリング

| 状況 | 挙動 |
|------|------|
| ネットワーク無し / API 失敗 / タイムアウト | 画面にトースト通知でエラー表示し、**デモ用ダミーデータ**（`DummyWeather` がコード生成する固定パターンのスナップショット列）でエフェクト・UI を継続。アプリは破綻させない |
| API キー未設定 | 起動時に警告トーストを出し、ダミーデータモードで動作 |
| レスポンスのパース失敗 | 当該都市の更新を中断し、トースト表示。直前の表示状態を維持 |

- ダミーデータは `DummyWeather`（C# コード生成）で作る。現在時刻起点・都市別・多様なコンディション（晴れ／曇り／雨／雷／雪）を含み、オフライン・無キーでもデモが成立する。生成ロジックは EditMode テストで検証する。
- 取得中はローディングインジケーター（スピナー等）を表示する。

---

## テスト方針

- **ロジックは EditMode 単体テスト（Unity Test Runner、現行 46 件）**：
  - API レスポンスのパース（JSON → `WeatherSnapshot[]`）
  - 天気コード（`weather.id`）→ `WeatherCondition` のマッピング
  - 緯度経度 → 地図 XZ 座標の変換
  - UTC → JST 変換
  - スナップショット間の線形補間
  - カメラフォーカス構図計算（`CameraFraming`）
  - ビルボードスケール（`BillboardScale`）
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
| 6 | タイムライン UI + 情報パネル | DateTime 表示・再生・スライダーで補間再生でき、情報パネルに天気・気温が出る |
| — | （ここまでで MVP 完成） | ✅ 完了 |
| 7a | 都市ドロップダウン + カメラフォーカス | 右上ドロップダウンとマーカーが双方向同期し、選択時にカメラが斜め見下ろしへ移動 | ✅ 完了 |
| 7b | ビルボードピンマーカー | 光柱をピンアイコン＋地点名ラベルに置き換え、ホバー/選択表示 | ✅ 完了 |
| 8+ | 拡張機能 | 雷 → 風矢印 → 都道府県ツリー検索パネル → Ray Tracing 切替 → 雪表現強化（優先度順） |

---

## 決定事項（旧・未決事項）

- [x] **日本地図テクスチャ**：Natural Earth（パブリックドメイン）の等緯度経度地図をトリミング → 「地図テクスチャ」参照。
- [x] **都市マスタの作成方法**：アマノ技研データ（商用無料）を `CitiesJsonGenerator` で読み込み **100 都市**に絞る → 「都市マスタ」参照。
- [x] **API キーの管理・配布**：既定はダミーデータ動作、ライブは `config.json`（git 管理外）または環境変数 `OWM_API_KEY` でオプトイン → 「API キー管理と配布」参照。
- [x] **マーカービジュアル**：ビルボードピン（ピン画像 + 地点名ラベル）→ 「都市マーカー」参照。MVP 時の光柱は 2026-06-30 に置き換え済み。

### 取り込み時に確認する細部（対応状況）

- [x] アマノ技研データの測地系：地理院/数値地図由来（JGD2011≒WGS84）として変換せずそのまま使用（`CitiesJsonGenerator` コメント参照）。
- [x] Natural Earth テクスチャの実図郭に合わせた lat/lon 範囲定数：`MapBoundsSO` に反映済み。
- [x] 都市の抽出条件：`PrefectureCapitals` ＋ `ExtraMajorCities` で **100 都市**に確定（計画当初の 150〜200 より絞り込み）。
