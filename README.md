# Japan Weather Demo 🗾🌦️

日本各地の天気予報をリアルタイムに取得し、**3D の日本地図と気象エフェクト**で可視化する Unity（HDRP）デモアプリです。
ポートフォリオ／技術デモとして、外部 API 連携・疎結合アーキテクチャ・自動テスト・HDRP の表現力を一つの題材にまとめています。

![Unity](https://img.shields.io/badge/Unity-6000.3_LTS-black?logo=unity)
![Render Pipeline](https://img.shields.io/badge/Render-HDRP_17.3-blue)
![Tests](https://img.shields.io/badge/EditMode_Tests-46_passing-brightgreen)
![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey)

---

## 画面プレビュー

| 地図と都市マーカー | 天気エフェクト（雨） | タイムライン再生 |
|---|---|---|
| ![地図とマーカー](docs/images/city-markers-image.png) | ![降水エフェクト](docs/images/rain-effect-image.png) | ![タイムライン](docs/images/weather-timeline.gif) |

---

## このアプリでできること

- 🗺️ **インタラクティブな日本地図** — 3D 空間に日本地図を表示し、自由カメラ（回転・ズーム・パン）で俯瞰
- 📍 **100 都市のビルボードピン** — 県庁所在地・政令市・中核市・主要都市を地図上に配置。ホバーで地点名表示、クリックで選択
- 🔽 **都市選択ドロップダウン** — 右上パネルからリストで都市を切り替え（マーカー選択と双方向同期）
- 📷 **カメラ自動フォーカス** — 都市選択時に、その地点の斜め見下ろし構図へスムーズに移動
- 🌤️ **リアルタイム天気取得** — 選択した都市の 5 日間（3 時間刻み・40 点）予報を OpenWeatherMap から取得
- ☀️🌧️❄️ **天気エフェクト** — コンディション（晴／曇／雨／雷雨／雪）に応じて、空・雲・降水・太陽光がリアルタイムに変化
- 🕒 **時間帯連動の空** — 時刻に応じて太陽角度・光量・色が朝→昼→夕→夜へ滑らかに遷移
- ▶️ **タイムライン再生** — スライダー／再生ボタンで 5 日間の天気変化をアニメーション再生（スナップショット間を線形補間）
- ℹ️ **情報パネル** — 選択中の天気・気温を日本語表示
- 🔌 **オフライン耐性** — API キーが無い／通信失敗時も、同梱のダミーデータとトースト通知でデモが成立

---

## 技術的ハイライト（アピールポイント）

- **疎結合アーキテクチャ**：天気データを `WeatherTimelineSO`（ScriptableObject）に集約し、**C# イベント（`OnSnapshotChanged`）** で各コントローラー（雲・降水・空・UI）へ一方向に伝播。UI・エフェクト・マップは互いを直接参照しません。
- **テスト駆動した純ロジック**：座標変換・JSON パース・天気コード分類・UTC→JST 変換・スナップショット補間・カメラ構図計算・ビルボードスケールなどを MonoBehaviour 非依存の純関数に切り出し、**EditMode ユニットテスト 46 件**で検証。
- **外部 API 連携とエラーハンドリング**：`UnityWebRequest` で OpenWeatherMap を取得・パースし、メモリキャッシュ。キー未設定／失敗／タイムアウト時は**ダミーデータへフォールバック**してアプリを破綻させない設計。
- **HDRP の表現**：PhysicallyBasedSky・Bloom・半透明雲レイヤー・ParticleSystem による降水で、天気の雰囲気を演出。
- **データ駆動**：地図テクスチャ（Natural Earth）と都市マスタ（CSV → `Cities.json` をエディタ拡張で生成）を、共通の範囲定数で緯度経度 ↔ ワールド座標に変換。
- **安全な API キー管理**：キーはリポジトリに含めず、`StreamingAssets/config.json`（Git 管理外）または環境変数 `OWM_API_KEY` でオプトイン。

---

## 使い方

### 必要環境
- Unity **6000.3.18f1 LTS**（HDRP）
- Windows

### セットアップ
1. リポジトリをクローン
   ```bash
   git clone <repository-url>
   ```
2. Unity Hub から `UnityProject/` を開く
3. （任意）**ライブ天気を使う場合**：[OpenWeatherMap](https://openweathermap.org/api) の API キーを取得し、`UnityProject/Assets/StreamingAssets/config.example.json` をコピーして `config.json` を作成、キーを記入
   ```json
   { "apiKey": "あなたのAPIキー" }
   ```
   環境変数 `OWM_API_KEY` でも指定できます（`config.json` より優先度は低い）。
   > API キーが無くても、同梱のダミーデータで動作します（起動時に通知が出ます）。
4. `Assets/Scenes/MainScene.unity` を開いて **Play**

### 操作
| 操作 | 動作 |
|------|------|
| 左クリック（都市マーカー） | 都市を選択して天気を取得（カメラがフォーカス移動） |
| マーカーにホバー | 地点名ラベルを表示 |
| 右上ドロップダウン | リストから都市を選択 |
| マウス右ドラッグ | カメラ回転 |
| マウスホイール | ズーム |
| 中ドラッグ / WASD | パン |
| Q / E | 高度上下 |
| 下部スライダー・再生ボタン | タイムライン操作 |

---

## 技術スタック

| 分類 | 使用技術 |
|------|----------|
| エンジン | Unity 6000.3.18f1 LTS |
| レンダリング | HDRP 17.3（PhysicallyBasedSky / Bloom） |
| 入力 | Input System 1.19 |
| UI | uGUI + TextMeshPro（日本語：Noto Sans JP） |
| JSON | Newtonsoft.Json |
| テスト | Unity Test Framework（NUnit, EditMode） |
| 外部 API | OpenWeatherMap 5 day / 3 hour Forecast |

---

## アーキテクチャ

```
OpenWeatherMap API
    │  WeatherService.FetchForecast（取得・パース・キャッシュ・ダミー fallback）
    ▼
WeatherTimelineSO（ScriptableObject／ランタイムデータコンテナ）
    │  event OnSnapshotChanged
    ├─▶ CloudController           — 半透明メッシュ雲レイヤーの濃さ・色
    ├─▶ PrecipitationController   — 雨・雪 ParticleSystem の放出量
    ├─▶ SkyController             — PhysicallyBasedSky・太陽光（時間帯連動）
    ├─▶ TimelineUIController      — DateTime 表示・スライダー同期
    └─▶ InfoPanelController       — 天気・気温の表示

MapManager（マーカー配置・選択）
    │  event CitySelected / CityFocused
    ├─▶ GameManager               — 都市選択 → 予報取得 → タイムライン更新
    ├─▶ CityDropdownController    — ドロップダウン ⇄ マーカーの双方向同期
    └─▶ CameraFocusController     — 選択地点の斜め見下ろし構図へカメラ移動
```

### コンポーネント構成（`UnityProject/Assets/Scripts/`）

| 層 | 主なスクリプト | 役割 |
|----|---------------|------|
| **Data** | `WeatherTimelineSO` / `WeatherSnapshot` / `WeatherCondition` / `CityData` / `CityCatalog` / `MapBoundsSO` / `GeoProjection` | データ構造・座標変換（純ロジック中心） |
| **Weather** | `WeatherService` / `OwmDto` / `WeatherParser` / `ConditionMapper` / `TimeZoneUtil` / `DummyWeather` / `ApiKeyResolver` / `SnapshotInterpolator` / `SunAngle` / `CloudController` / `PrecipitationController` / `SkyController` | 取得・解析・補間・エフェクト制御 |
| **Map** | `MapManager` / `CityMarker` / `BillboardScale` | ビルボードピン配置・選択・画面一定サイズ |
| **Camera** | `FreeCameraController` / `CameraFocusController` / `CameraFraming` | 自由カメラ・都市フォーカス移動 |
| **UI** | `TimelineUIController` / `InfoPanelController` / `CityDropdownController` / `ToastController` / `LoadingIndicator` | タイムライン・情報パネル・都市選択・通知 |
| **System** | `GameManager` | 起動時配線・初期都市選択のハブ |
| **Editor** | `CitiesJsonGenerator` | CSV → `Cities.json` 生成（エディタ拡張） |

---

## テスト

純ロジックは EditMode ユニットテストで検証しています（`Window > General > Test Runner`）。

- 緯度経度 → ワールド座標変換 / 天気コード分類 / OWM JSON パース / UTC→JST 変換 / スナップショット線形補間 / 太陽角度 / タイムラインデータ / API キー解決 / ダミーデータ生成 / 都市マスタ読込 / カメラ構図計算 / ビルボードスケール … **計 46 件 PASS**

---

## データ出典・ライセンス

| アセット | 出典 | ライセンス |
|----------|------|-----------|
| 地図テクスチャ | [Natural Earth](https://www.naturalearthdata.com/) | パブリックドメイン |
| 都市マスタ | [アマノ技研「地方公共団体の位置データ」](https://amano-tec.com/data/localgovernments.html) | 研究・商用無料 |
| 日本語フォント | [Noto Sans JP](https://fonts.google.com/noto/specimen/Noto+Sans+JP) | SIL Open Font License 1.1 |
| 天気データ | [OpenWeatherMap](https://openweathermap.org/) | API 利用規約に準拠 |

> 地図テクスチャの作成手順は [`docs/qgis-gdal-japan-map-guide.md`](docs/qgis-gdal-japan-map-guide.md) を参照。

---

## 今後の拡張（MVP 後）

雷エフェクト / 風矢印グリッド / 都道府県ツリー検索パネル / Ray Tracing 切替 / 雪の表現強化 など。

---

## 設計ドキュメント

- 開発計画書（仕様）: [`docs/development-plan.md`](docs/development-plan.md)
- 実装計画: [`docs/superpowers/plans/2026-06-28-japan-weather-mvp.md`](docs/superpowers/plans/2026-06-28-japan-weather-mvp.md)
- ビルボードピン設計: [`docs/superpowers/specs/2026-06-30-billboard-pin-markers-design.md`](docs/superpowers/specs/2026-06-30-billboard-pin-markers-design.md)
- 都市ドロップダウン＋カメラフォーカス設計: [`docs/superpowers/specs/2026-06-30-city-dropdown-camera-focus-design.md`](docs/superpowers/specs/2026-06-30-city-dropdown-camera-focus-design.md)

---

## 開発環境構築手順

- UnityとWindows版Claude Codeのインストールおよび開発環境構築手順： [docs/unity-wsl-claudecode-setup-guide.md](docs/unity-wsl-claudecode-setup-guide.md)
- Cursor × Unity C# 開発環境 セットアップガイド： [docs/cursor-unity-csharp-setup-guide.md](docs/cursor-unity-csharp-setup-guide.md)

---

