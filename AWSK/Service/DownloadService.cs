﻿using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using AWSK.Models;
using Codeplex.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static AWSK.Constant;

namespace AWSK.Service {
    /// <summary>
    /// データをダウンロードするクラス
    /// </summary>
    class DownloadService {
        /// <summary>
        /// 自分自身の唯一のinstance
        /// </summary>
        static DownloadService singleton = null;

        /// <summary>
        /// privateコンストラクタ
        /// </summary>
        DownloadService() { }

        /// <summary>
        /// 唯一のinstanceを返す(Singletonパターン)
        /// </summary>
        /// <returns></returns>
        public static DownloadService instance {
            get {
                if (singleton == null) {
                    singleton = new DownloadService();
                }
                return singleton;
            }
        }

        /// <summary>
        /// 装備リストをデッキビルダーからダウンロードする
        /// (性質上、基地航空隊の距離を取得できない)
        /// </summary>
        /// <returns>装備リスト</returns>
        public async Task<List<Weapon>> downloadWeaponDataFromDeckBuilderAsync() {
            var result = new List<Weapon>();
            using (var client = new HttpClient()) {
                // テキストデータダウンロード
                string rawData = await client.GetStringAsync("http://kancolle-calc.net/data/itemdata.js");

                // 余計な文字を削除
                rawData = rawData.Replace("var gItems = ", "");

                // JSONとしてパース
                var obj = DynamicJson.Parse(rawData);

                // パース結果を取得
                foreach (var weapon in obj) {
                    // ID・装備名・対空値を取得
                    int id = (int)weapon.id;
                    string name = (string)weapon.name;
                    int antiAir = (int)weapon.aac;

                    // 装備種は力技で処理する
                    var rawType = weapon.type;
                    WeaponType type;
                    if (rawType[0] == 3 && rawType[1] == 5 && rawType[2] == 6) {
                        type = WeaponType.PF;
                    } else if (rawType[0] == 3 && rawType[1] == 5 && rawType[2] == 7) {
                        type = WeaponType.PB;
                    } else if (rawType[0] == 3 && rawType[1] == 5 && rawType[2] == 8) {
                        type = WeaponType.PA;
                    } else if (rawType[0] == 3 && rawType[1] == 40 && rawType[2] == 57) {
                        type = WeaponType.JPB;
                    } else if (rawType[0] == 5 && rawType[1] == 7 && rawType[2] == 9) {
                        type = WeaponType.PS;
                    } else if (rawType[0] == 5 && rawType[1] == 7 && rawType[2] == 10) {
                        type = WeaponType.WS;
                    } else if (rawType[0] == 5 && rawType[1] == 36 && rawType[2] == 45) {
                        type = WeaponType.WF;
                    } else if (rawType[0] == 5 && rawType[1] == 43 && rawType[2] == 11) {
                        type = WeaponType.WB;
                    } else if (rawType[0] == 17 && rawType[1] == 33 && rawType[2] == 41) {
                        type = WeaponType.LFB;
                    } else if (rawType[0] == 21 && rawType[1] == 28 && rawType[2] == 47) {
                        type = WeaponType.LA;
                    } else if (rawType[0] == 22 && rawType[1] == 39 && rawType[2] == 47) {
                        type = WeaponType.LB;
                    } else if (rawType[0] == 22 && rawType[1] == 39 && rawType[2] == 48) {
                        type = WeaponType.LF;
                    } else {
                        type = WeaponType.Other;
                    }

                    // 局戦・陸戦は迎撃値を読み取る
                    int intercept = 0;
                    if (type == WeaponType.LF) {
                        intercept = (int)weapon.evasion;
                    }

                    /// 戦闘行動半径の処理
                    /// (情報内に含まれていないので取得しようがない。なので0固定)
                    int basedAirUnitRange = 0;

                    // 艦娘用の装備か？
                    bool forKammusuFlg = id <= 500;

                    // 追記する
                    result.Add(new Weapon(id, name, type, antiAir, intercept, basedAirUnitRange, forKammusuFlg));
                }
            }
            return result;
        }

        /// <summary>
        /// 艦娘リストをデッキビルダーからダウンロードする
        /// (性質上、詳細な情報を取得できない敵艦もある)
        /// </summary>
        /// <returns>艦娘リスト</returns>
        public async Task<List<KeyValuePair<Kammusu, List<int>>>> downloadKammusuDataFromDeckBuilderAsync() {
            var result = new List<KeyValuePair<Kammusu, List<int>>>();
            using (var client = new HttpClient()) {
                // テキストデータをダウンロード
                string rawData = await client.GetStringAsync("http://kancolle-calc.net/data/shipdata.js");

                // 余計な文字を削除
                rawData = rawData.Replace("var gShips = ", "");

                // JSONとしてパース
                var obj = DynamicJson.Parse(rawData);

                // パース結果を取得
                foreach (var kammusu in obj) {
                    // 艦船IDや艦名などを取得
                    int id = int.Parse(kammusu.id);
                    if (id > 1900)
                        continue;
                    string name = (string)kammusu.name;
                    if (name == "なし")
                        continue;
                    int antiAir = (int)kammusu.max_aac;
                    int slotCount = (int)kammusu.slot;
                    var slot = kammusu.carry;
                    var defaultWeapon = kammusu.equip;

                    // 艦娘か？
                    bool kammusuFlg = id <= 1500;

                    // 艦種データを変換する
                    string rawType = (string)kammusu.type;
                    var type = KammusuTypeReverseDic.ContainsKey(rawType) ? KammusuTypeReverseDic[rawType] : KammusuType.Other;
                    //深海棲艦における「航空戦艦」には陸上型も混じっているので個別に対策する
                    if (!kammusuFlg && AFSet.Contains(name)) {
                        type = KammusuType.AF;
                    }

                    // Kammusuクラスとしてまとめる
                    var kammusuData = new Kammusu(id, name, type, antiAir, new List<int>(), kammusuFlg);
                    var defaultWeaponData = new List<int>();
                    foreach (var s in slot) {
                        kammusuData.SlotList.Add((int)s);
                    }
                    foreach (var w in defaultWeapon) {
                        defaultWeaponData.Add((int)w);
                    }
                    int temp = defaultWeaponData.Count;
                    for (int i = temp; i < slotCount; ++i) {
                        defaultWeaponData.Add(0);
                    }

                    // データの整合性がおかしいものは追記しない
                    if (kammusuData.SlotList.Count != slotCount) {
                        Console.WriteLine($"不完全データを検知→ id:{id} name:{name}");
                        continue;
                    }

                    // 追記する
                    result.Add(new KeyValuePair<Kammusu, List<int>>(kammusuData, defaultWeaponData));
                }
            }
            return result;
        }

        /// <summary>
        /// 装備リストをデッキビルダーからダウンロードする
        /// </summary>
        /// <returns>装備リスト</returns>
        public async Task<List<Weapon>> downloadWeaponDataFromWikiaAsync() {
            var result = new List<Weapon>();

            // 艦娘の装備
            var basedAirUnitRangeHash = new Dictionary<string, int>();
            if (System.IO.File.Exists(@"Resource\BasedAirUnitRange.csv")) {
                using (var sr = new System.IO.StreamReader(@"Resource\BasedAirUnitRange.csv")) {
                    while (!sr.EndOfStream) {
                        // 1行読み込み、カンマ毎に区切る
                        string line = sr.ReadLine();
                        string[] values = line.Split(',');
                        // 行数がおかしい場合は飛ばす
                        if (values.Count() < 2)
                            continue;
                        // ヘッダー行は飛ばす
                        if (values[0] == "名称")
                            continue;
                        // データを読み取る
                        string name = values[0];
                        int id = int.Parse(values[1]);
                        basedAirUnitRangeHash[name] = id;
                    }
                }
            }
            using (var client = new HttpClient()) {
                // テキストデータダウンロード
                string rawData = await client.GetStringAsync("http://kancolle.wikia.com/wiki/Equipment");

                // テキストデータをパース
                var doc = default(IHtmlDocument);
                var parser = new HtmlParser();
                doc = parser.Parse(rawData);
                var tempSelect = doc.QuerySelectorAll("table.wikitable > tbody > tr");
                foreach (var record in tempSelect) {
                    // 1行を読み出す
                    var tdList = record.GetElementsByTagName("td").ToList();
                    if (tdList.Count < 9) {
                        continue;
                    }

                    // IDを取得する
                    string rawId = tdList[0].TextContent;
                    int id = int.Parse(rawId);

                    // 装備名を取得する
                    string rawName = tdList[2].TextContent.Replace(tdList[2].GetElementsByTagName("a").First().TextContent, "");
                    string name = Regex.Replace(rawName, "(^ |\n)", "");
                    if (name == "") {
                        name = Regex.Replace(tdList[2].GetElementsByTagName("a").First().TextContent, "(^ |\n)", "");
                    }

                    // 装備種を取得する
                    string rawType = Regex.Replace(tdList[3].TextContent, "\n", "");
                    WeaponType type;
                    if (WeaponTypeReverseDicWikia.ContainsKey(rawType)) {
                        type = WeaponTypeReverseDicWikia[rawType];
                        if (name == "爆装一式戦 隼III型改(65戦隊)") {
                            type = WeaponType.LB;
                        }
                    } else {
                        type = WeaponType.Other;
                    }

                    // その他の項目を取得する
                    int antiAir = 0, intersept = 0;
                    var rawStatIcons = tdList[4].GetElementsByTagName("a")
                            .Select(e => e.GetAttribute("href"))
                            .Select(t => Regex.Replace(t, ".*/([A-Za-z_]+)\\.png.*", "$1"))
                            .ToList();
                    string rawStatValues = Regex.Replace(tdList[4].InnerHtml, "(<a.*?</a>|<span.*?\">|</span>|\n| )", "");
                    rawStatValues = Regex.Replace(rawStatValues, "<br>", ",");
                    string[] rawStatValues2 = rawStatValues.Split(',');
                    var rawStatDic = rawStatIcons.Zip(rawStatValues2, (f, s) => new KeyValuePair<string, string>(f, s))
                        .ToDictionary(pair => pair.Key, pair => pair.Value);
                    if (rawStatDic.ContainsKey("Icon_AA")) {
                        antiAir = int.Parse(rawStatDic["Icon_AA"].Replace("+", ""));
                    }
                    if (rawStatDic.ContainsKey("Icon_Interception")) {
                        intersept = int.Parse(rawStatDic["Icon_AA"].Replace("+", ""));
                    }

                    // 戦闘行動半径を取得する
                    int basedAirUnitRange = 0;
                    if (type != WeaponType.Other) {
                        if (basedAirUnitRangeHash.ContainsKey(name)) {
                            basedAirUnitRange = basedAirUnitRangeHash[name];
                        } else {
                            string url = tdList[2].GetElementsByTagName("a").First().GetAttribute("href");
                            string rawData2 = await client.GetStringAsync($"http://kancolle.wikia.com{url}");
                            var doc2 = default(IHtmlDocument);
                            var parser2 = new HtmlParser();
                            doc2 = parser2.Parse(rawData2);
                            var tempElement = doc2.QuerySelectorAll("div.mw-content-text > table.infobox > tbody > tr").ToList()[1];
                            basedAirUnitRange = int.Parse(tempElement.GetElementsByTagName("b")
                                .Where(e => e.TextContent.Contains("Combat Radius: "))
                                .First().TextContent.Replace("Combat Radius: ", ""));
                        }
                    }

                    // 追記する
                    Console.WriteLine(name);
                    result.Add(new Weapon(id, name, type, antiAir, intersept, basedAirUnitRange, true));
                }
            }

            // 深海棲艦の装備
            using (var client = new HttpClient()) {
                // テキストデータダウンロード
                string rawData = await client.GetStringAsync("http://kancolle.wikia.com/wiki/List_of_equipment_used_by_the_enemy");

                // テキストデータをパース
                var doc = default(IHtmlDocument);
                var parser = new HtmlParser();
                doc = parser.Parse(rawData);
                var tempSelect = doc.QuerySelectorAll("table.wikitable > tbody > tr");
                foreach (var record in tempSelect) {
                    // 1行を読み出す
                    var tdList = record.GetElementsByTagName("td").ToList();
                    if (tdList.Count < 6) {
                        continue;
                    }

                    // IDを取得する
                    string rawId = tdList[0].TextContent;
                    int id = int.Parse(rawId);

                    // 装備名を取得する
                    string rawName = tdList[2].TextContent.Replace(tdList[2].GetElementsByTagName("a").First().TextContent, "");
                    string name = Regex.Replace(rawName, "(^ |\n)", "");

                    // 装備種を取得する
                    string rawType = Regex.Replace(tdList[3].TextContent, "\n", "");
                    WeaponType type;
                    if (WeaponTypeReverseDicWikia.ContainsKey(rawType)) {
                        type = WeaponTypeReverseDicWikia[rawType];
                    } else {
                        type = WeaponType.Other;
                    }

                    // その他の項目を取得する
                    int antiAir = 0;
                    var rawStatIcons = tdList[4].GetElementsByTagName("a")
                            .Select(e => e.GetAttribute("href"))
                            .Select(t => Regex.Replace(t, ".*/([A-Za-z_]+)\\.png.*", "$1"))
                            .ToList();
                    string rawStatValues = Regex.Replace(tdList[4].InnerHtml, "(<a.*?</a>|<span.*?\">|</span>|\n| )", "");
                    rawStatValues = Regex.Replace(rawStatValues, "<br>", ",");
                    string[] rawStatValues2 = rawStatValues.Split(',');
                    var rawStatDic = rawStatIcons.Zip(rawStatValues2, (f, s) => new KeyValuePair<string, string>(f, s))
                        .ToDictionary(pair => pair.Key, pair => pair.Value);
                    if (rawStatDic.ContainsKey("Icon_AA")) {
                        antiAir = int.Parse(rawStatDic["Icon_AA"].Replace("+", ""));
                    }

                    // 追記する
                    Console.WriteLine(name);
                    result.Add(new Weapon(id, name, type, antiAir, 0, 0, false));
                }
            }
            return result;
        }

        /// <summary>
        /// 艦娘リストをWikiaからダウンロードする
        /// </summary>
        /// <returns>艦娘リスト</returns>
        public async Task<List<KeyValuePair<Kammusu, List<int>>>> downloadKammusuDataFromWikiaAsync() {
            var result = new List<KeyValuePair<Kammusu, List<int>>>();
            // 装備URL→装備番号のリストを取得する
            var weaponUtlToId = new Dictionary<string, int>();
            using (var client = new HttpClient()) {
                // テキストデータをダウンロード
                string rawData = await client.GetStringAsync("http://kancolle.wikia.com/wiki/List_of_equipment_used_by_the_enemy");

                // テキストデータをパース
                var doc = default(IHtmlDocument);
                var parser = new HtmlParser();
                doc = parser.Parse(rawData);
                var tempSelect = doc.QuerySelectorAll("table.wikitable > tbody > tr");
                foreach (var record in tempSelect) {
                    var tdList = record.GetElementsByTagName("td").ToList();
                    if (tdList.Count < 6) {
                        continue;
                    }
                    string rawId = tdList[0].TextContent;
                    string rawName = tdList[2].GetElementsByTagName("a").First().GetAttribute("href");
                    weaponUtlToId[rawName] = int.Parse(rawId);
                }
            }

            // 深海棲艦のリストを取得し、記録する
            using (var client = new HttpClient()) {
                // テキストデータをダウンロード
                string rawData = await client.GetStringAsync("http://kancolle.wikia.com/wiki/Enemy_Vessels/Full");

                // テキストデータをパース
                var doc = default(IHtmlDocument);
                var parser = new HtmlParser();
                doc = parser.Parse(rawData);
                var tempSelect = doc.QuerySelectorAll("div > table > tbody > tr");
                foreach(var record in tempSelect) {
                    var tdList = record.GetElementsByTagName("td").ToList();
                    if (tdList.Count < 20) {
                        continue;
                    }
                    string rawId = tdList[1].TextContent;
                    string rawName = tdList[4].TextContent.Replace("\n", "").Replace(" ", "");
                    string rawType = tdList[0].TextContent.Replace("\n", "").Replace(" ", "");
                    string rawAntiAir = tdList[9].TextContent;
                    var rawSlot = tdList[18].TextContent.Replace("\n", "").Split(',').ToList();
                    if (rawSlot.Count == 1 && rawSlot[0] == "") {
                        rawSlot = new List<string>();
                    }
                    if (rawSlot.Count >= 1 && rawSlot[0].Contains("?")) {
                        continue;
                    }
                    var rawDefaultWeapon = tdList[19].GetElementsByTagName("a").Select(e => e.GetAttribute("href")).ToList();
                    var kammusu = new Kammusu(int.Parse(rawId), rawName,
                        KammusuTypeReverseDicWikia[rawType], int.Parse(rawAntiAir),
                        rawSlot.Select(s => int.Parse(s)).ToList(), false);
                    Console.WriteLine(rawName);
                    var defaultWeaponList = new List<int>();
                    foreach(string url in rawDefaultWeapon) {
                        if (!Regex.IsMatch(url, "^/wiki")) {
                            continue;
                        }
                        if (!weaponUtlToId.ContainsKey(url)) {
                            string rawData2 = await client.GetStringAsync($"http://kancolle.wikia.com{url}");
                            Console.WriteLine(url);
                            var doc2 = default(IHtmlDocument);
                            var parser2 = new HtmlParser();
                            doc2 = parser2.Parse(rawData2);
                            var tempElement = doc2.QuerySelectorAll("table.infobox > tbody > tr > td > p > b")
                                .Where(e => e.TextContent.Contains("No.")).First();
                            int no = int.Parse(Regex.Replace(tempElement.TextContent, ".*No.(\\d+).*", "$1"));
                            weaponUtlToId[url] = no;
                        }
                        defaultWeaponList.Add(weaponUtlToId[url]);
                    }
                    result.Add(new KeyValuePair<Kammusu, List<int>>(kammusu, defaultWeaponList));
                }
            }
            return result;
        }

        /// <summary>
        /// 艦娘リストをKammusuPatch.csvから取得する(上書き用)
        /// </summary>
        /// <returns>艦娘リスト</returns>
        public List<KeyValuePair<Kammusu, List<int>>> downloadKammusuDataFromLocalFile() {
            var result = new List<KeyValuePair<Kammusu, List<int>>>();
            if (System.IO.File.Exists(@"Resource\KammusuPatch.csv")) {
                using (var sr = new System.IO.StreamReader(@"Resource\KammusuPatch.csv")) {
                    while (!sr.EndOfStream) {
                        // 1行読み込み、カンマ毎に区切る
                        string line = sr.ReadLine();
                        string[] values = line.Split(',');
                        // 行数がおかしい場合は飛ばす
                        if (values.Count() < 16)
                            continue;
                        // ヘッダー行は飛ばす
                        if (values[0] == "id")
                            continue;
                        // データを読み取る
                        int id = int.Parse(values[0]);
                        string name = values[1];
                        var type = KammusuTypeReverseDic.ContainsKey(values[2]) ? KammusuTypeReverseDic[values[2]] : KammusuType.Other;
                        int antiAir = int.Parse(values[3]);
                        int slotSize = int.Parse(values[4]);
                        bool kammusuFlg = int.Parse(values[15]) == 1;
                        var kammusu = new Kammusu(id, name, type, antiAir, new List<int>(), kammusuFlg);
                        var defaultWeaponList = new List<int>();
                        for(int i = 0; i < slotSize; ++i) {
                            int slot = int.Parse(values[5 + i]);
                            int defaultWeapon = int.Parse(values[10 + i]);
                            kammusu.SlotList.Add(slot);
                            defaultWeaponList.Add(defaultWeapon);
                        }
                        result.Add(new KeyValuePair<Kammusu, List<int>>(kammusu, defaultWeaponList));
                    }
                }
            }
            return result;
        }
    }
}
