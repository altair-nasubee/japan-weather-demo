# QGIS / GDAL で日本地図テクスチャを作る手順（Windows）

このプロジェクトの地図テクスチャ（`Assets/Resources/Textures/JapanMap.png`）を、
Natural Earth の世界地図ラスタから「日本部分だけ」正確に切り出して作るための手順書。
QGIS / GDAL を触ったことがない人向けに、インストールから順を追って書いている。

> 切り出す範囲は実装計画の `MapBounds` と一致させる：
> **緯度 24.0〜46.5 / 経度 122.0〜146.5**（沖縄〜北海道）。

---

## 0. QGIS / GDAL とは（ざっくり）

- **QGIS**：無料・オープンソースの地理情報システム（GIS）アプリ。地図データを開いて加工・書き出しできる。
- **GDAL**：地図ラスタ（画像）を変換・切り出しする定番ツール群。QGIS をインストールすると**一緒に入る**。
- 今回は GDAL の `gdal_translate` というコマンド1つ（または QGIS の GUI）で、世界地図から日本を切り出す。

GDAL コマンドは、QGIS と一緒に入る **「OSGeo4W Shell」** という専用のコマンドプロンプトから実行する
（普通の PowerShell ではなくこれを使うのがポイント）。

---

## 1. 元データ（Natural Earth）の準備

別ガイドのとおり、Natural Earth I「Shaded Relief and Water」版をダウンロード・解凍しておく。

- 推奨ファイル: `NE1_HR_LC_SR_W.zip`（1:10m・約 308 MB、解凍後 `NE1_HR_LC_SR_W.tif` = 21600×10800 px）
- 配布ページ: https://www.naturalearthdata.com/downloads/10m-raster-data/10m-natural-earth-1/
- ライセンス: パブリックドメイン（出典表示・許諾不要）

解凍してできた `NE1_HR_LC_SR_W.tif` の置き場所を決めておく（例：`C:\work\japan-weather-demo\_assets_src\`）。
**パスに日本語やスペースが入らない場所**だとコマンドが楽。

---

## 2. QGIS をインストールする

1. ダウンロードページを開く: https://qgis.org/download/
2. Windows の **「Long Term Version (LTR)」のスタンドアロンインストーラ**をダウンロードする
   （2026 時点の LTR は 3.44。初めてなら最新版 4.x より安定した LTR を推奨）。
   - 「スタンドアロンインストーラ」= インストーラ 1 個で QGIS 本体も GDAL も入る、初心者向け。
3. ダウンロードした `.msi`（または `.exe`）を実行し、案内に従ってそのままインストール（既定設定で OK）。
4. インストールが終わると、スタートメニューに **「QGIS 3.44」**（バージョン名）フォルダができる。
   その中に以下が入っている：
   - `QGIS Desktop 3.44.x` … GUI アプリ本体
   - **`OSGeo4W Shell`** … GDAL コマンドを打つための専用プロンプト（今回使う）

---

## 3. GDAL が使えるか確認する

1. スタートメニュー →「QGIS 3.44」→ **`OSGeo4W Shell`** を起動する（黒いコマンド画面が開く）。
2. 次を入力して Enter：

   ```
   gdal_translate --version
   ```

3. `GDAL 3.xx.x, released ...` のようにバージョンが表示されれば成功。
   `'gdal_translate' は…認識されていません` と出る場合は、OSGeo4W Shell ではなく普通の PowerShell を
   開いている可能性が高い。手順 2-4 の OSGeo4W Shell を使うこと。

---

## 4. 日本部分を切り出す（方法A：コマンド・推奨）

正確で速い。OSGeo4W Shell で行う。

1. OSGeo4W Shell で、元 TIF を置いたフォルダへ移動する（例）：

   ```
   cd C:\work\japan-weather-demo\_assets_src
   ```

2. 日本範囲（経度 122.0〜146.5 / 緯度 24.0〜46.5）で切り出す：

   ```
   gdal_translate -projwin 122.0 46.5 146.5 24.0 NE1_HR_LC_SR_W.tif JapanMap.tif
   ```

   - `-projwin` の数字の順番は **「左 上 右 下」＝「経度min 緯度max 経度max 緯度min」**。
   - これで `JapanMap.tif`（約 1470 × 1350 px）ができる。

3. PNG に変換する：

   ```
   gdal_translate -of PNG JapanMap.tif JapanMap.png
   ```

   - 同じフォルダに `JapanMap.png` ができる。これが完成テクスチャ。
   - （`JapanMap.png.aux.xml` という補助ファイルが一緒にできることがあるが、Unity には不要なので無視/削除して良い。）

> この方法なら範囲がきっかり 122.0〜146.5 / 24.0〜46.5 になるので、
> `MapBounds.asset` の既定値とそのまま一致する（後述の座標メモ・微調整は不要）。

---

## 5. 日本部分を切り出す（方法B：QGIS の画面操作）

コマンドが不安なら GUI でもできる。

1. `QGIS Desktop` を起動。
2. メニュー `レイヤ > レイヤを追加 > ラスタレイヤを追加` で `NE1_HR_LC_SR_W.tif` を追加。世界地図が表示される。
3. メニュー `ラスタ > 抽出 > 切り抜き（範囲指定）…`（英語UIなら `Raster > Extraction > Clip Raster by Extent…`）。
4. 「クリッピング範囲（Clipping extent）」で範囲を指定：
   - 右の「…」→「座標を手入力（Calculate from coordinates）」などで
     **X(経度) min 122.0 / max 146.5、Y(緯度) min 24.0 / max 46.5** を入力。
   - もしくは地図上でおおよそ日本を四角く囲んでもよい（その場合は精度が落ちるので方法Aを推奨）。
5. 「実行」で GeoTIFF が出力される。
6. 出力レイヤを右クリック →『エクスポート > 名前を付けて保存』→ 形式 `Rendered image (PNG)` を選び、
   `JapanMap.png` として保存。

---

## 6. Unity に配置する

1. エクスプローラーでフォルダを作る：
   `C:\work\japan-weather-demo\UnityProject\Assets\Resources\Textures\`
2. 作った `JapanMap.png` をその中にコピーする。最終パス：
   `UnityProject/Assets/Resources/Textures/JapanMap.png`
3. インポート設定（Unity 側。シーン構築時にこちらでも確認・調整する）：
   - Texture Type: `Default`
   - sRGB (Color Texture): `ON`
   - Wrap Mode: `Clamp`
   - Max Size: `2048` 以上（Large 版でも 1470px なので 2048 で収まる）

---

## 7. うまくいかないとき

- **出力 PNG が真っ黒／真っ白**：`-projwin` の数値の順番（左 上 右 下）が違う可能性。`122.0 46.5 146.5 24.0` を再確認。
- **`gdal_translate` が見つからない**：普通の PowerShell ではなく **OSGeo4W Shell** で実行しているか確認（手順 2-4）。
- **ファイルが見つからないと言われる**：`cd` で TIF のあるフォルダに移動しているか、ファイル名のスペル、パスに日本語/スペースが無いか確認。スペースを含む場合は `"..."` で囲む。
- **日本の位置がずれて見える**：方法B（GUI）でざっくり囲んだ場合に起きやすい。方法A（コマンド）で切り直すと正確。

---

## 8. 終わったら

`JapanMap.png` を配置できたら（②アマノ技研 CSV も用意できていれば一緒に）、
Claude に「**素材を置いた**」と伝える。そこから Task 1.3（MainScene＋地図 Plane）以降のシーン構築を進める。

- 方法A（コマンド）で切った場合 … `MapBounds` 調整は不要。
- 方法B（GUI）でずれた場合 … 切り出した実際の四隅の経度・緯度を控えておくと、`MapBounds.asset` の微調整に使える。
