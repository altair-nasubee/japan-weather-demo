# 都市選択ドロップダウン + カメラフォーカス — 設計書

- 作成日: 2026-06-30
- 状態: ✅ **実装完了**（[`plans/2026-06-30-city-dropdown-camera-focus.md`](../plans/2026-06-30-city-dropdown-camera-focus.md)）
- 対象リポジトリ: `C:\work\japan-weather-demo`（Unity HDRP / 6000.3.18f1 LTS）

## 目的

MVP 時点では右上の情報パネルが選択中の都市名（「東京」など）を **テキスト表示** しているだけで、都市の切り替えは地図上のマーカークリックに限られていた。これを **ドロップダウンリスト** に置き換え、リストから各地点を選択できるようにした。さらに、選択した地点へ **カメラがスムーズにズームジャンプ** し、その地点の天候エフェクト（空・雲・降水）が見やすい構図へ寄る。

## スコープ

### 含むもの

- 右上情報パネルの都市名テキストを `TMP_Dropdown` に置き換える。
- ドロップダウンからの都市選択で既存の選択フロー（予報取得 → タイムライン/エフェクト更新）を駆動する。
- 地図マーカークリックとドロップダウンの **双方向連動**（片方で選ぶと他方の表示も同期）。
- 都市選択時、カメラが選択地点上空の **斜め見下ろし構図** へスムーズ（約 0.7 秒・ease in-out）に移動する。
- カメラ姿勢算出を純粋関数に切り出し EditMode テストで検証する。

### 含まないもの（YAGNI）

- 都道府県ツリー UI・テキスト検索パネル（開発計画書の MVP 後拡張機能のまま据え置き）。
- ドロップダウンのグループ表示（`TMP_Dropdown` はネイティブにグループ非対応のため、フラットな並びとする）。
- カメラ移動完了後の追従・自動オービット等の追加演出。

## 決定事項（ブレインストーミングでの合意）

| 論点 | 決定 |
|------|------|
| ドロップダウンの置き場所 | 右上情報パネルの **都市名表示を置き換える** |
| マーカークリック選択 | **残す**（ドロップダウンと双方向連動） |
| カメラ遷移 | **スムーズアニメーション**（約 0.7 秒・ease in-out） |
| 寄った先の構図 | **斜め見下ろし**（pitch 約 55°、現 MainCamera と整合） |
| カメラ実装場所 | 新規 `CameraFocusController` に **分離**（`FreeCameraController` は拡張しない） |
| 都市ワールド座標の取得 | `MapManager` が選択マーカーの **実ワールド位置を通知**（`GeoProjection` 再計算しない） |
| ドロップダウンの並び | `Cities.json` の並び（jiscode 順 ≒ 北→南・都道府県グループ）を維持 |

## アーキテクチャ

既存の「ScriptableObject + C# Event による疎結合」構成を踏襲する。新規コンポーネントは `MapManager` のイベントを購読するだけで、互いを直接参照しない。

### データフロー

```
[ドロップダウン選択] ─┐
                      ├→ MapManager.Select(marker)
[マーカークリック]  ─┘        ├→ CitySelected(CityData)
                              │     ├→ GameManager → WeatherService 取得 → WeatherTimelineSO 更新 → 各エフェクト/UI
                              │     └→ CityDropdownController が現在値を同期（SetValueWithoutNotify）
                              └→ CityFocused(Vector3 worldPos)
                                    └→ CameraFocusController が斜め見下ろし構図へスムーズ移動
```

`GameManager` 経由の既存フロー（予報取得・タイムライン更新・エフェクト反映）は **一切変更しない**。今回の追加は「選択手段の追加（ドロップダウン）」「選択の同期」「カメラ移動」の 3 点に閉じる。

## コンポーネント詳細

### 1. `CityDropdownController`（新規・`Scripts/UI/`）

責務: ドロップダウン UI と都市選択の橋渡し。

- 依存: `MapManager`（`SerializeField`）、`TMP_Dropdown`（`SerializeField`）。
- 起動時:
  - `CityCatalog.LoadFromStreamingAssets()`（`MapManager` と同一ソース）で都市リストを取得。`Cities.json` の並びを維持。
  - `dropdown.ClearOptions()` → 都市名で `AddOptions`。option index ↔ `CityData`（または都市名）の対応表（`List<string> cityNames`）を保持。
  - `dropdown.onValueChanged` に `OnDropdownChanged` を登録。
  - `mapManager.CitySelected` を購読し `OnCitySelected` を登録。
- `OnDropdownChanged(int index)`: `mapManager.SelectByName(cityNames[index])` を呼ぶ。
- `OnCitySelected(CityData city)`: `cityNames.IndexOf(city.name)` を求め、`dropdown.SetValueWithoutNotify(index)`（**無限ループ防止**）。`RefreshShownValue()` を呼ぶ。
- ループ整合性: ドロップダウン操作 → `SelectByName` → `Select` → `CitySelected` 発火 → `SetValueWithoutNotify` で値が一致するため `onValueChanged` は再発火しない。

### 2. `CameraFocusController`（新規・`Scripts/Camera/`）

責務: 選択都市上空の斜め見下ろし構図へ、カメラをスムーズに移動する。

- 依存: `MapManager`（`SerializeField`）、`FreeCameraController`（`SerializeField`、移動中の入力無効化用）、自身の `transform`。
- パラメータ（`SerializeField`、Inspector 調整可）:
  - `height`（目標の高さ）、`backDistance`（注視点からの後退距離。pitch を決める）、`duration = 0.7f`、`lookAtYOffset`（注視点の地図からの浮かせ量）。
- `mapManager.CityFocused`（`Vector3` worldPos）を購読。
- 受信時:
  - 純粋関数 `CameraFraming.ComputeFocusPose(target, height, backDistance)` で目標 (position, rotation) を算出。
  - 進行中のコルーチンがあれば停止し、新規に補間コルーチンを開始。
  - コルーチン: `FreeCameraController.enabled = false` → `duration` 秒かけて現在姿勢から目標姿勢へ ease in-out 補間（位置は `Vector3.Lerp`、回転は `Quaternion.Slerp`、`t` は smoothstep）→ 完了後 `FreeCameraController.enabled = true`。
- 移動後は自由カメラ操作がそのまま使える（注視点固定の追従はしない）。

### 3. `CameraFraming`（新規・純粋関数クラス・`Scripts/Camera/`）

責務: テスト可能なカメラ姿勢算出。MonoBehaviour 非依存の静的クラス。

```csharp
public static class CameraFraming
{
    /// <summary>
    /// target（注視点）に対し、height だけ上空・backDistance だけ -Z 方向に引いた
    /// 斜め見下ろしのカメラ姿勢を返す。rotation は target を向く。
    /// </summary>
    public static (Vector3 position, Quaternion rotation) ComputeFocusPose(
        Vector3 target, float height, float backDistance);
}
```

- position = `target + new Vector3(0, height, -backDistance)`。
- rotation = `Quaternion.LookRotation(target - position)`。
- pitch（見下ろし角）は `height` と `backDistance` の比で決まる（例: height=14, backDistance=10 で約 54.5°）。

### 4. `MapManager`（小改修・`Scripts/Map/`）

- 既存 `event Action<CityData> CitySelected` はそのまま。
- 追加: `event Action<Vector3> CityFocused`。`Select(CityMarker marker)` の末尾で `CityFocused?.Invoke(marker.transform.position)` を発火。
- 既存の選択ロジック・マーカー生成は変更しない。

### 5. `InfoPanelController`（小改修・`Scripts/UI/`）

- 都市名表示はドロップダウンが担うため、`cityLabel` への代入を削除（フィールド自体も不要なら削除）。
- 天気コンディション・気温の表示（`OnSnapshotChanged` 経由）は **据え置き**。

## エラーハンドリング / エッジケース

- 都市名がドロップダウンに見つからない（`IndexOf` が -1）: `SetValueWithoutNotify` をスキップし現在値を維持（通常は起きないが防御的に）。
- カメラ移動中に別の都市が選択された: 進行中コルーチンを停止し、新しい目標へ振り直す（最後の選択が勝つ）。
- 起動時の初期選択（東京）: `GameManager.Start` が `SelectByName("東京")` を呼ぶため、`CityFocused` も発火し、起動直後にカメラが東京へ寄る。ドロップダウンの初期値も東京に同期される。

## テスト方針

プロジェクト既存方針（純ロジックは EditMode、見た目は Play-mode 目視）を踏襲する。

### EditMode 単体テスト（`CameraFramingTests`）

- `ComputeFocusPose` の position が `target + (0, height, -backDistance)` であること。
- rotation の forward が `(target - position).normalized` と一致すること（注視点を向く）。
- 既知の height/backDistance で pitch が期待角度（許容誤差内）であること。

### Play-mode 目視確認（完了）

- ドロップダウンから都市を選ぶ → 予報取得 → カメラがその都市へスムーズに寄り、天候エフェクトが見える。
- 地図マーカーをクリック → ドロップダウンの現在値が同期する。
- ドロップダウンで選択 → マーカーのハイライトが同期する。
- カメラ移動完了後、自由カメラ操作（回転・ズーム・パン）が効く。

## Unity エディタ作業（実施済み）

> 注: シーン編集・保存は Play 停止中のみ可能（Play 中の SaveScene は失敗する）。

1. InfoPanel の都市名 `TMP_Text` を `TMP_Dropdown` に差し替え（または都市名箇所へ Dropdown を配置）。日本語表示のため TMP フォントに Noto Sans JP を割り当てる。
2. `CityDropdownController` を Canvas 配下に付与し、`MapManager` と `TMP_Dropdown` を配線。
3. MainCamera に `CameraFocusController` を付与し、`MapManager` / `FreeCameraController` を配線、構図パラメータを調整。
4. EventSystem は既存のものを流用（既に配置済み）。

## 影響範囲まとめ

| ファイル | 変更種別 |
|----------|----------|
| `Scripts/UI/CityDropdownController.cs` | 新規 |
| `Scripts/Camera/CameraFocusController.cs` | 新規 |
| `Scripts/Camera/CameraFraming.cs` | 新規（純粋関数） |
| `Tests/EditMode/CameraFramingTests.cs` | 新規 |
| `Scripts/Map/MapManager.cs` | `CityFocused` イベント追加 |
| `Scripts/UI/InfoPanelController.cs` | 都市名表示を削除 |
| `MainScene.unity` / InfoPanel / MainCamera | エディタ配線 |
