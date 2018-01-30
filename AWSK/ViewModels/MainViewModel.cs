﻿using AWSK.Stores;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;

namespace AWSK.ViewModels
{
	class MainViewModel
	{
		#region プロパティ(ReactiveProperty)
		// trueにすると画面を閉じる
		public ReactiveProperty<bool> CloseWindow { get; } = new ReactiveProperty<bool>(false);
		// 基地航空隊を飛ばしたか？
		public ReactiveProperty<bool> BasedAirUnit1Flg { get; } = new ReactiveProperty<bool>(true);
		public ReactiveProperty<bool> BasedAirUnit2Flg { get; } = new ReactiveProperty<bool>(false);
		public ReactiveProperty<bool> BasedAirUnit3Flg { get; } = new ReactiveProperty<bool>(false);
		// 基地航空隊の装備の選択番号
		public ReactiveProperty<int> BasedAirUnitIndex11 { get; } = new ReactiveProperty<int>(0);
		public ReactiveProperty<int> BasedAirUnitIndex12 { get; } = new ReactiveProperty<int>(0);
		public ReactiveProperty<int> BasedAirUnitIndex13 { get; } = new ReactiveProperty<int>(0);
		public ReactiveProperty<int> BasedAirUnitIndex14 { get; } = new ReactiveProperty<int>(0);
		public ReactiveProperty<int> BasedAirUnitIndex21 { get; } = new ReactiveProperty<int>(0);
		public ReactiveProperty<int> BasedAirUnitIndex22 { get; } = new ReactiveProperty<int>(0);
		public ReactiveProperty<int> BasedAirUnitIndex23 { get; } = new ReactiveProperty<int>(0);
		public ReactiveProperty<int> BasedAirUnitIndex24 { get; } = new ReactiveProperty<int>(0);
		public ReactiveProperty<int> BasedAirUnitIndex31 { get; } = new ReactiveProperty<int>(0);
		public ReactiveProperty<int> BasedAirUnitIndex32 { get; } = new ReactiveProperty<int>(0);
		public ReactiveProperty<int> BasedAirUnitIndex33 { get; } = new ReactiveProperty<int>(0);
		public ReactiveProperty<int> BasedAirUnitIndex34 { get; } = new ReactiveProperty<int>(0);
		// 基地航空隊の艦載機熟練度
		public ReactiveProperty<int> BasedAirUnitMas11 { get; } = new ReactiveProperty<int>(7);
		public ReactiveProperty<int> BasedAirUnitMas12 { get; } = new ReactiveProperty<int>(7);
		public ReactiveProperty<int> BasedAirUnitMas13 { get; } = new ReactiveProperty<int>(7);
		public ReactiveProperty<int> BasedAirUnitMas14 { get; } = new ReactiveProperty<int>(7);
		public ReactiveProperty<int> BasedAirUnitMas21 { get; } = new ReactiveProperty<int>(7);
		public ReactiveProperty<int> BasedAirUnitMas22 { get; } = new ReactiveProperty<int>(7);
		public ReactiveProperty<int> BasedAirUnitMas23 { get; } = new ReactiveProperty<int>(7);
		public ReactiveProperty<int> BasedAirUnitMas24 { get; } = new ReactiveProperty<int>(7);
		public ReactiveProperty<int> BasedAirUnitMas31 { get; } = new ReactiveProperty<int>(7);
		public ReactiveProperty<int> BasedAirUnitMas32 { get; } = new ReactiveProperty<int>(7);
		public ReactiveProperty<int> BasedAirUnitMas33 { get; } = new ReactiveProperty<int>(7);
		public ReactiveProperty<int> BasedAirUnitMas34 { get; } = new ReactiveProperty<int>(7);
		// 基地航空隊の装備改修度
		public ReactiveProperty<int> BasedAirUnitRf11 { get; } = new ReactiveProperty<int>(0);
		public ReactiveProperty<int> BasedAirUnitRf12 { get; } = new ReactiveProperty<int>(0);
		public ReactiveProperty<int> BasedAirUnitRf13 { get; } = new ReactiveProperty<int>(0);
		public ReactiveProperty<int> BasedAirUnitRf14 { get; } = new ReactiveProperty<int>(0);
		public ReactiveProperty<int> BasedAirUnitRf21 { get; } = new ReactiveProperty<int>(0);
		public ReactiveProperty<int> BasedAirUnitRf22 { get; } = new ReactiveProperty<int>(0);
		public ReactiveProperty<int> BasedAirUnitRf23 { get; } = new ReactiveProperty<int>(0);
		public ReactiveProperty<int> BasedAirUnitRf24 { get; } = new ReactiveProperty<int>(0);
		public ReactiveProperty<int> BasedAirUnitRf31 { get; } = new ReactiveProperty<int>(0);
		public ReactiveProperty<int> BasedAirUnitRf32 { get; } = new ReactiveProperty<int>(0);
		public ReactiveProperty<int> BasedAirUnitRf33 { get; } = new ReactiveProperty<int>(0);
		public ReactiveProperty<int> BasedAirUnitRf34 { get; } = new ReactiveProperty<int>(0);
		// 敵艦隊の艦の選択番号
		public ReactiveProperty<int> EnemyUnitIndex1 { get; } = new ReactiveProperty<int>(0);
		public ReactiveProperty<int> EnemyUnitIndex2 { get; } = new ReactiveProperty<int>(0);
		public ReactiveProperty<int> EnemyUnitIndex3 { get; } = new ReactiveProperty<int>(0);
		public ReactiveProperty<int> EnemyUnitIndex4 { get; } = new ReactiveProperty<int>(0);
		public ReactiveProperty<int> EnemyUnitIndex5 { get; } = new ReactiveProperty<int>(0);
		public ReactiveProperty<int> EnemyUnitIndex6 { get; } = new ReactiveProperty<int>(0);
		#endregion
		#region プロパティ(ReadOnlyReactiveCollection)
		// 艦載機熟練度の一覧
		public ReadOnlyReactiveCollection<string> MasList { get; }
		// 装備改修度の一覧
		public ReadOnlyReactiveCollection<string> RfList { get; }
		// 深海棲艦の艦名一覧
		public ReadOnlyReactiveCollection<string> EnemyList { get; }
		// 基地航空隊に使用できる装備の一覧
		public ReadOnlyReactiveCollection<string> BasedAirUnitList { get; }
		#endregion

		#region コマンド
		// シミュレーションを実行
		public ReactiveCommand RunSimulationCommand { get; }
		#endregion

		// その他初期化用コード
		private async void Initialize() {
			// データベースの初期化
			var status = await DataStore.Initialize();
			switch (status) {
			case DataStoreStatus.Exist:
				break;
			case DataStoreStatus.Success:
				MessageBox.Show("ダウンロードに成功しました。", "AWSK");
				break;
			case DataStoreStatus.Failed:
				MessageBox.Show("ダウンロードに失敗しました。", "AWSK");
				CloseWindow.Value = true;
				return;
			}
		}
		// 基地航空隊のデータを取得
		private List<List<WeaponData>> BasedAirUnitData() {
			// 準備
			var basedAirUnitFlgList = new[] { BasedAirUnit1Flg.Value, BasedAirUnit2Flg.Value, BasedAirUnit3Flg.Value };
			var basedAirUnitIndex = new[] {
					BasedAirUnitIndex11.Value, BasedAirUnitIndex12.Value, BasedAirUnitIndex13.Value, BasedAirUnitIndex14.Value,
					BasedAirUnitIndex21.Value, BasedAirUnitIndex22.Value, BasedAirUnitIndex23.Value, BasedAirUnitIndex24.Value,
					BasedAirUnitIndex31.Value, BasedAirUnitIndex32.Value, BasedAirUnitIndex33.Value, BasedAirUnitIndex34.Value,
				};
			var basedAirUnitMas = new[] {
					BasedAirUnitMas11.Value, BasedAirUnitMas12.Value, BasedAirUnitMas13.Value, BasedAirUnitMas14.Value,
					BasedAirUnitMas21.Value, BasedAirUnitMas22.Value, BasedAirUnitMas23.Value, BasedAirUnitMas24.Value,
					BasedAirUnitMas31.Value, BasedAirUnitMas32.Value, BasedAirUnitMas33.Value, BasedAirUnitMas34.Value,
				};
			var basedAirUnitRf = new[] {
					BasedAirUnitRf11.Value, BasedAirUnitRf12.Value, BasedAirUnitRf13.Value, BasedAirUnitRf14.Value,
					BasedAirUnitRf21.Value, BasedAirUnitRf22.Value, BasedAirUnitRf23.Value, BasedAirUnitRf24.Value,
					BasedAirUnitRf31.Value, BasedAirUnitRf32.Value, BasedAirUnitRf33.Value, BasedAirUnitRf34.Value,
				};
			// 作成
			var basedAirUnitData = new List<List<WeaponData>>();
			for (int i = 0; i < basedAirUnitFlgList.Count(); ++i) {
				// チェックを入れてない編成は無視する
				if (!basedAirUnitFlgList[i])
					continue;
				//
				var temp = new List<WeaponData>();
				for (int j = 0; j < 4; ++j) {
					int index = i * 4 + j;
					// 「なし」が選択されている装備は無視する
					if (basedAirUnitIndex[index] == 0)
						continue;
					// 装備名を取り出す
					string name = BasedAirUnitList[basedAirUnitIndex[index]];
					// 装備名から装備情報を得る
					var weapon = DataStore.WeaponDataByName(name);
					weapon.Mas = basedAirUnitMas[index];
					weapon.Rf = basedAirUnitRf[index];
					temp.Add(weapon);
				}
				if (temp.Count > 0)
					basedAirUnitData.Add(temp);
			}
			return basedAirUnitData;
		}

		// クリップボードからインポート
		public void ImportClipboardText() {
			try {
				// クリップボードから文字列を取得する
				string clipboardString = Clipboard.GetText();
				if (clipboardString == null)
					throw new Exception();
				// クリップボードの文字列をJSONとしてパースし、艦隊データに変換する
				var fleetData = DataStore.ParseFleetData(clipboardString);
				MessageBox.Show(fleetData.ToString(), "AWSK");
			} catch (Exception e) {
				Console.WriteLine(e.ToString());
				MessageBox.Show("クリップボードから艦隊データを取得できませんでした。", "AWSK");
			}
		}
		// 航空戦をシミュレートする
		public void RunSimulation() {
			// 基地航空隊のデータを取得する
			var basedAirUnitData = BasedAirUnitData();
			return;
		}

		// コンストラクタ
		public MainViewModel() {
			// その他初期化
			Initialize();
			// プロパティ・コマンドを設定
			{
				var oc = new ObservableCollection<string>(new List<string> {
					"--", "|", "||", "|||", "/", "//", "///", ">>"
				});
				MasList = oc.ToReadOnlyReactiveCollection();
			}
			{
				var oc = new ObservableCollection<string>(new List<string> {
					"0", "1", "2", "3", "4", "5",
					"6", "7", "8", "9", "10"});
				RfList = oc.ToReadOnlyReactiveCollection();
			}
			{
				var list = DataStore.EnemyNameList();
				// 敵艦名のリストから、新たに表示用リストを作成
				var list2 = new List<string>();
				list2.Add("なし");
				foreach (string name in list) {
					int count = list.Where(p => p == name).Count();
					int count2 = list2.Where(p => p.Contains(name)).Count();
					if (count > 1) {
						list2.Add($"{name}-{count2+1}");
					} else {
						list2.Add(name);
					}
				}
				var oc = new ObservableCollection<string>(list2);
				EnemyList = oc.ToReadOnlyReactiveCollection();
			}
			{
				var oc = new ObservableCollection<string>(DataStore.BasedAirUnitNameList());
				BasedAirUnitList = oc.ToReadOnlyReactiveCollection();
			}
			RunSimulationCommand = new[] { BasedAirUnit1Flg, BasedAirUnit1Flg, BasedAirUnit1Flg }
				.CombineLatest(x => x.Any(y => y)).ToReactiveCommand();
			RunSimulationCommand.Subscribe(RunSimulation);
		}
	}
}

/* 入力サンプル：
{"version":4,"f1":{"s1":{"id":"426","lv":99,"luck":-1,"items":{"i1":{"id":122,"rf":"10"},"i2":{"id":122,"rf":"10"},"i3":{"id":106,"rf":"10"},"i4":{"id":43,"rf":0}}},"s2":{"id":"149","lv":99,"luck":-1,"items":{"i1":{"id":103,"rf":"10"},"i2":{"id":103,"rf":"10"},"i3":{"id":59,"rf":"10","mas":7},"i4":{"id":36,"rf":"10"},"ix":{"id":43,"rf":0}}},"s3":{"id":"118","lv":99,"luck":-1,"items":{"i1":{"id":41,"rf":0},"i2":{"id":50,"rf":"10"},"i3":{"id":50,"rf":"10"}}},"s4":{"id":"119","lv":99,"luck":-1,"items":{"i1":{"id":41,"rf":0},"i2":{"id":50,"rf":"10"},"i3":{"id":50,"rf":"10"}}},"s5":{"id":"278","lv":99,"luck":-1,"items":{"i3":{"id":157,"rf":"10","mas":"7"},"i2":{"id":110,"rf":0,"mas":"7"},"i1":{"id":144,"rf":0,"mas":"7"},"i4":{"id":212,"rf":0,"mas":"7"}}},"s6":{"id":"467","lv":99,"luck":-1,"items":{"i1":{"id":144,"rf":0,"mas":"7"},"i2":{"id":93,"rf":0,"mas":"7"},"i3":{"id":100,"rf":0,"mas":"7"},"i4":{"id":110,"rf":0,"mas":"7"}}}},"f2":{"s1":{}}}
*/
