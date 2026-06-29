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
    /// アマノ技研「地方公共団体の位置データ」CSV（TSV/UTF-8, r0801puboffice_utf8.csv）から
    /// 県庁所在地＋政令市＋中核市＋主要市を抽出して StreamingAssets/Cities.json を出力する。
    /// 入力はリポジトリの download_resources/ 固定パス（ダイアログ無しでメニュー実行できる）。
    /// 列: [0]jiscode [1]name [2]namekana [3]building [4]zipcode [5]address [6]tel [7]source [8]lat [9]long [10]note
    /// 座標は地理院/数値地図由来（JGD2011≒WGS84）なので変換せずそのまま使う。
    /// </summary>
    public static class CitiesJsonGenerator
    {
        const int ColJis = 0;
        const int ColName = 1;
        const int ColLat = 8;
        const int ColLon = 9;

        [MenuItem("JapanWeatherDemo/Generate Cities.json from amano CSV")]
        public static void Generate()
        {
            string repoRoot = Directory.GetParent(Application.dataPath).Parent.FullName; // .../UnityProject/Assets -> repo root
            string csvPath = Path.Combine(repoRoot, "download_resources", "r0801puboffice_utf8.csv");
            if (!File.Exists(csvPath))
            {
                Debug.LogError($"[CitiesJsonGenerator] CSV not found: {csvPath}");
                return;
            }

            var prefNames = PrefectureNames();
            var whitelist = MajorCities();
            var selected = new List<CityData>();
            var seen = new HashSet<string>();

            foreach (var line in File.ReadAllLines(csvPath).Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var c = line.Split('\t');
                if (c.Length <= ColLon) continue;
                if (!int.TryParse(c[ColJis], out int jis)) continue;
                string name = c[ColName].Trim();
                if (!float.TryParse(c[ColLat], NumberStyles.Float, CultureInfo.InvariantCulture, out float lat)) continue;
                if (!float.TryParse(c[ColLon], NumberStyles.Float, CultureInfo.InvariantCulture, out float lon)) continue;

                int prefCode = jis / 1000;
                if (!prefNames.TryGetValue(prefCode, out string prefName)) continue;

                // 東京特例: 市が無いので東京都庁(13000)を「東京」として採用
                if (jis == 13000) { Add(selected, seen, "東京", lat, lon, prefName); continue; }

                // 都道府県行(XX000)は除外（市の本庁行のみ対象）
                if (jis % 1000 == 0) continue;

                // ホワイトリストの市名に完全一致する行のみ採用（政令市の「区」行は名前が違うので自動除外）
                if (!whitelist.Contains(name)) continue;
                Add(selected, seen, name, lat, lon, prefName);
            }

            string outPath = Path.Combine(Application.streamingAssetsPath, "Cities.json");
            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            File.WriteAllText(outPath, JsonConvert.SerializeObject(selected, Formatting.Indented));
            AssetDatabase.Refresh();
            Debug.Log($"[CitiesJsonGenerator] wrote {selected.Count} cities -> {outPath}");
        }

        static void Add(List<CityData> list, HashSet<string> seen, string name, float lat, float lon, string pref)
        {
            if (!seen.Add(pref + "|" + name)) return;
            list.Add(new CityData { name = name, lat = lat, lon = lon, prefecture = pref });
        }

        static Dictionary<int, string> PrefectureNames() => new()
        {
            {1,"北海道"},{2,"青森県"},{3,"岩手県"},{4,"宮城県"},{5,"秋田県"},{6,"山形県"},{7,"福島県"},
            {8,"茨城県"},{9,"栃木県"},{10,"群馬県"},{11,"埼玉県"},{12,"千葉県"},{13,"東京都"},{14,"神奈川県"},
            {15,"新潟県"},{16,"富山県"},{17,"石川県"},{18,"福井県"},{19,"山梨県"},{20,"長野県"},{21,"岐阜県"},
            {22,"静岡県"},{23,"愛知県"},{24,"三重県"},{25,"滋賀県"},{26,"京都府"},{27,"大阪府"},{28,"兵庫県"},
            {29,"奈良県"},{30,"和歌山県"},{31,"鳥取県"},{32,"島根県"},{33,"岡山県"},{34,"広島県"},{35,"山口県"},
            {36,"徳島県"},{37,"香川県"},{38,"愛媛県"},{39,"高知県"},{40,"福岡県"},{41,"佐賀県"},{42,"長崎県"},
            {43,"熊本県"},{44,"大分県"},{45,"宮崎県"},{46,"鹿児島県"},{47,"沖縄県"},
        };

        // 県庁所在地市（東京除く）＋政令指定都市＋中核市＋主要市。重複は HashSet が吸収する。
        static HashSet<string> MajorCities() => new()
        {
            // 県庁所在地市
            "札幌市","青森市","盛岡市","仙台市","秋田市","山形市","福島市","水戸市","宇都宮市","前橋市",
            "さいたま市","千葉市","横浜市","新潟市","富山市","金沢市","福井市","甲府市","長野市","岐阜市",
            "静岡市","名古屋市","津市","大津市","京都市","大阪市","神戸市","奈良市","和歌山市","鳥取市",
            "松江市","岡山市","広島市","山口市","徳島市","高松市","松山市","高知市","福岡市","佐賀市",
            "長崎市","熊本市","大分市","宮崎市","鹿児島市","那覇市",
            // 北海道・東北の主要市
            "函館市","旭川市","釧路市","帯広市","北見市","苫小牧市","江別市",
            "八戸市","弘前市","一関市","奥州市","石巻市","大崎市","横手市","鶴岡市","酒田市",
            "郡山市","いわき市","会津若松市",
            // 関東の主要市
            "つくば市","日立市","土浦市","高崎市","太田市",
            "川越市","所沢市","越谷市","川口市","熊谷市","船橋市","柏市","市川市","松戸市","成田市",
            "川崎市","相模原市","横須賀市","藤沢市","小田原市","厚木市","鎌倉市",
            // 中部の主要市
            "長岡市","上越市","高岡市","松本市","上田市","飯田市",
            "豊橋市","岡崎市","一宮市","豊田市","春日井市","安城市","四日市市","松阪市","伊勢市",
            // 近畿の主要市
            "彦根市","堺市","東大阪市","豊中市","吹田市","高槻市","枚方市",
            "姫路市","西宮市","尼崎市","明石市","加古川市","橿原市","田辺市",
            // 中国・四国の主要市
            "米子市","出雲市","倉敷市","福山市","呉市","下関市","宇部市","岩国市",
            "鳴門市","丸亀市","今治市","新居浜市","宇和島市",
            // 九州・沖縄の主要市
            "北九州市","久留米市","飯塚市","大牟田市","唐津市","佐世保市","諫早市",
            "八代市","別府市","延岡市","都城市","霧島市","名護市","沖縄市","うるま市",
        };
    }
}
