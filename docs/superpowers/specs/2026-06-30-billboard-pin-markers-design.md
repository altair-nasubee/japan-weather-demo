# 設計仕様：光柱マーカー → ビルボードピンマーカー

- 日付: 2026-06-30
- 対象: 地図上の都市マーカー表現の置き換え
- 関連: `docs/superpowers/specs/2026-06-30-city-dropdown-camera-focus-design.md`（ドロップダウン/カメラフォーカス連携）

## 1. 目的・背景

現状、各都市マーカーは「光柱」（HDRP emissive 円柱メッシュ + 選択時のみ有効な point light + capsule collider）で表現している（`Assets/Scripts/Map/CityMarker.cs` / `Assets/Prefabs/CityMarker.prefab`）。

これを廃止し、各地点に **ピンアイコン画像 + 地点名ラベル** を 2D で表示する **ビルボードマーカー**（常にカメラに正対）へ置き換える。ピンをクリックすると色が変わり選択状態が分かるようにする。

## 2. 要件

### 機能要件
- 各都市（145 地点）に **ピンアイコン画像** を常時表示する。
- ピンは **ビルボード**：常にカメラへ正対する。
- ピンの **見た目の大きさは画面上一定**（カメラ距離に依らず一定ピクセルサイズ。ズームしても巨大化しない）。
- **地点名テキスト** は、その地点が **ホバー中 または 選択中** のときだけ表示する（通常は非表示。常時 145 個表示すると密集地で重なり読めなくなるため）。
- ピンを **クリックすると選択** され、ピン色が変わって選択中と分かる。
- 既存の連携を維持：選択は `MapManager.CitySelected`（ドロップダウン双方向同期）と `MapManager.CityFocused`（カメラフォーカス）を従来どおり発火する。

### 非機能要件
- HDRP で確実に描画されること。
- カメラ操作（`FreeCameraController` のドラッグ回転/ズーム）を阻害しないこと。
- 145 マーカーで実用的なパフォーマンス（毎フレームのビルボード更新・hover 用 raycast は 1 本に限定）。

## 3. 設計判断

### レンダリング方式：ワールド空間 UGUI Canvas
各マーカーを **World-Space UGUI Canvas**（子に `Image`=ピン、`TMP_Text`=地点名）で構成する。

- 採用理由：HDRP では `SpriteRenderer` の標準スプライトシェーダが描画されない既知の問題がある。UGUI は描画パイプラインに依存せず HDRP で確実に表示できる。
- ピン色は `Image.color` の tint で変更（ピン画像は白ベースで作成）。
- 地点名は UI の `TMP_Text`（既存 Noto Sans JP SDF フォントを使用）。

### クリック/ホバー判定：Physics.Raycast（UI EventSystem を使わない）
マーカー root に `BoxCollider` を付け、`MapManager` が毎フレーム 1 本の `Physics.Raycast` でマウス下のマーカーを判定する（既存方式の踏襲）。

- 採用理由：EventSystem/GraphicRaycaster 経由にすると、`FreeCameraController` が `EventSystem.IsPointerOverGameObject()` でマウス入力をゲートしているため、ピン上でカメラドラッグが阻害される。Physics.Raycast ならマーカーは EventSystem 管理外となり、カメラ操作と両立する。
- 既存 `MapManager.Update()` も既に Physics.Raycast でクリック選択しており、方式が一貫する。

### ビルボード + 画面一定サイズ
各マーカーは毎フレーム以下を行う：
1. **正対**：`transform.rotation = camera.transform.rotation`（テキストが鏡像にならないようカメラ回転をそのまま採用）。
2. **一定サイズ**：カメラ距離に比例して `localScale` を調整し、画面上のピクセルサイズを一定に保つ。`BoxCollider` は同 transform 上にあるためスケールに連動し、当たり判定もピンの見た目に追従する。

スケール計算は純関数として切り出す（テスト容易性のため）。

## 4. コンポーネント詳細

### 4.1 ピン画像（生成物）
- `Assets/Textures/Icons/pin.png`
- 白ベースの滴形マップピン（下が尖り上が円。透過 PNG）。
- Sprite としてインポート（既存アイコンと同様の設定）。

### 4.2 `CityMarker.cs`（書き換え）
責務：1 都市のビルボードピン。カメラ正対・画面一定サイズ・選択/ホバー状態の見た目を管理し、クリックを通知する。

- 撤去：`beamLight`、`beamRenderer`、emissive/nits 関連ロジック。
- フィールド（SerializeField）：
  - `Image pin`（ピン画像）
  - `TMP_Text label`（地点名）
  - `Canvas canvas`（任意。子の World-Space Canvas）
- 公開 API（既存互換を維持）：
  - `CityData City { get; }`
  - `event Action<CityMarker> Clicked`
  - `void Init(CityData city)` — `City` 設定、`name` 設定、`label.text = city.name`、初期状態（非選択・非ホバー）。
  - `void SetSelected(bool)` — 選択色 tint。ラベル表示は「選択 or ホバー」で決定。
  - `void NotifyClicked()` — `Clicked` 発火（`MapManager` の raycast ヒット時）。
- 追加 API：
  - `void SetHover(bool)` — ホバー状態を更新。ラベル表示は「選択 or ホバー」で決定。
- 色：`baseColor`（通常）/ `selectedColor`（選択、例：黄系）。ホバー時の追加ハイライト有無は実装時に簡潔さ優先で決める（最小では「選択 or ホバーでラベル表示、色は base/selected の2値」）。
- `Update()`（または LateUpdate）：ビルボード正対 + 画面一定サイズスケール。カメラ参照は `Camera.main` か MapManager から注入。

### 4.3 ビルボードスケール純関数（新規、テスト対象）
- 配置先候補：`Assets/Scripts/Map/`（例 `BillboardScale.cs`、static）または `CityMarker` 内の static メソッド。
- 入力：カメラからマーカーまでの距離（またはカメラ位置・マーカー位置）、基準スケール係数、（必要なら）視野/参照距離。
- 出力：画面上一定サイズになる `localScale`（float）。
- EditMode テスト：距離 2 倍でスケール 2 倍など、画面サイズ一定性を検証（既存 `CameraFramingTests` と同様のパターン）。

### 4.4 `MapManager.cs`（変更）
- `BuildMarkers()`：現状維持（位置 = `GeoProjection.LatLonToXZ`、`markerY` でピンを地図上に浮かせる）。`markerY` はピン下端が地図に接するよう実装時に調整。
- `Update()`：
  - マウス下を 1 本 raycast し、当たったマーカーを **hover 対象** とする（`currentHover` を保持し、変化時に旧 hover を `SetHover(false)`、新 hover を `SetHover(true)`）。
  - UI 上（`EventSystem.IsPointerOverGameObject()`）では hover/クリックを無視。
  - 左クリックで従来どおり選択（`Select`）。
- `Select` / `SelectByName` / `CitySelected` / `CityFocused`：不変。

### 4.5 プレハブ再構築：`CityMarker.prefab`
Unity Editor（MCP）作業。
- root：`CityMarker`（書き換え後）+ `BoxCollider`（ピンの見た目を覆うサイズ）。
- 子：World-Space `Canvas`
  - `Image`（pin.png、白ベース、`Image.color` で tint）
  - `TMP_Text`（Noto Sans JP SDF、地点名、初期非表示）
- 旧 emissive 円柱メッシュ・point light は削除。
- MapManager の参照（`markerPrefab` 等）を新プレハブに再設定。

## 5. 影響範囲・非対象

### 影響
- `CityMarker.cs`、`MapManager.cs`、`CityMarker.prefab`、`MainScene`（プレハブ差し替え/参照再設定）。
- 光柱の glow（Bloom 連動）は無くなる。Bloom 設定自体（空/雲/降水）はそのまま残す。

### 非対象（変更しない）
- 天候エフェクト（空・ライト・雲・降水）。
- ドロップダウン（`CityDropdownController`）・カメラフォーカス（`CameraFocusController`）の内部ロジック（イベント I/F 維持で無改修）。
- `GeoProjection` / `MapBoundsSO` / `CityCatalog` / `Cities.json`。

## 6. テスト・受け入れ

### 自動テスト（EditMode）
- ビルボードスケール純関数のテスト（画面一定サイズ性）。
- 既存テスト（48/48）は全て green を維持。

### Play モード目視確認
- 145 ピンが正しい地点に表示される。
- カメラを回転/ズームしてもピンは正対し、画面上サイズが一定。
- ピンにマウスを乗せると地点名が表示され、外すと消える。
- ピンをクリックすると色が変わり、地点名が表示され続ける（選択維持）。
- 選択がドロップダウンに同期し、カメラがその都市にフォーカスする。
- ピン上/間でのカメラドラッグ操作が破綻しない。

## 7. 作業順序（概略・詳細は実装計画で）
1. ピン画像生成 + インポート。
2. ビルボードスケール純関数 + EditMode テスト（TDD）。
3. `CityMarker.cs` 書き換え（ビルボード/状態/ラベル）。
4. `MapManager.cs` に hover 検出追加。
5. `CityMarker.prefab` 再構築 + MainScene 参照再設定（Unity Editor）。
6. Play モード受け入れ確認。
