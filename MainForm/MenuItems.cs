using Circuit;

namespace MainForm {
	public class MenuItems {
		public enum ID {
			OPEN_NEW,
			OPEN_FILE,
			SAVE_FILE,
			OVERWRITE,
			PDF,

			SELECT,
			CUT,
			COPY,
			PASTE,
			SELECT_ALL,
			DELETE,
			UNDO,
			REDO,
			CENTER_CIRCUIT
		}

		MainForm mSim;
		List<ToolStripMenuItem> mMainMenuItems = new List<ToolStripMenuItem>();
		Font menuFont = new Font("Segoe UI", 9.0f);

		public MenuItems(MainForm sim) {
			mSim = sim;
		}

		public static DUMP_ID GetDumpIdFromString(string v) {
			DUMP_ID e;
			if (Enum.TryParse(v, out e)) {
				return e;
			} else {
				return DUMP_ID.INVALID;
			}
		}

		public void AllUnchecked() {
			for (int i = 0; i < mMainMenuItems.Count; i++) {
				if (mMainMenuItems[i].Checked) {
					mMainMenuItems[i].Checked = false;
					mMainMenuItems[i].OwnerItem.BackColor = Color.Transparent;
				}
			}
		}

		ToolStripMenuItem addElementItem(ToolStripMenuItem menu, string title, DUMP_ID item, SHORTCUT shortcut = new SHORTCUT()) {
			var elm = SymbolMenu.Construct(item);
			if (elm == null) {
				return null;
			} else {
				elm.Delete();
			}
			ToolStripMenuItem mi;
			if (shortcut.Key == Keys.None) {
				mi = new ToolStripMenuItem();
			} else {
				mi = new ToolStripMenuItem() {
					ShowShortcutKeys = true,
					ShortcutKeys = shortcut.Key,
					ShortcutKeyDisplayString = shortcut.Name
				};
			}
			mi.Font = menuFont;
			mi.Text = title;
			mi.Click += new EventHandler((sender, e) => {
				mSim.AddElement(item);
				if (null != mi.OwnerItem) {
					for (int i = 0; i < mMainMenuItems.Count; i++) {
						if (mMainMenuItems[i].Checked) {
							mMainMenuItems[i].Checked = false;
							mMainMenuItems[i].OwnerItem.BackColor = Color.Transparent;
						}
					}
					mi.Checked = true;
					mi.OwnerItem.BackColor = Color.LightBlue;
				}
			});
			mMainMenuItems.Add(mi);
			menu.DropDownItems.Add(mi);
			return mi;
		}

		void addMenuItem(ToolStripMenuItem menu, string title, ID item, SHORTCUT shortCut) {
			ToolStripMenuItem mi;
			if (shortCut.Key == Keys.None) {
				mi = new ToolStripMenuItem() {
					Font = menuFont,
					Text = title
				};
			} else {
				switch (shortCut.Key) {
				case Keys.Escape:
					mi = new ToolStripMenuItem() {
						Font = menuFont,
						Text = title,
						ShowShortcutKeys = true,
						ShortcutKeyDisplayString = shortCut.Name,
					};
					break;
				default:
					mi = new ToolStripMenuItem() {
						Font = menuFont,
						Text = title,
						ShowShortcutKeys = true,
						ShortcutKeys = shortCut.Key,
						ShortcutKeyDisplayString = shortCut.Name,
					};
					break;
				}
				mi.Click += new EventHandler((s, e) => { mSim.Performed(item); });
				mMainMenuItems.Add(mi);
				menu.DropDownItems.Add(mi);
			}
		}

		public void ComposeMainMenu(MenuStrip mainMenuBar) {
			#region File
			var fileMenuBar = new ToolStripMenuItem();
			fileMenuBar.Text = "ファイル(F)";
			fileMenuBar.Font = menuFont;
			addMenuItem(fileMenuBar, "新規作成(N)", ID.OPEN_NEW, new SHORTCUT(Keys.N));
			fileMenuBar.DropDownItems.Add(new ToolStripSeparator());
			addMenuItem(fileMenuBar, "開く(O)", ID.OPEN_FILE, new SHORTCUT(Keys.O));
			fileMenuBar.DropDownItems.Add(new ToolStripSeparator());
			addMenuItem(fileMenuBar, "上書き保存(S)", ID.OVERWRITE, new SHORTCUT(Keys.S));
			addMenuItem(fileMenuBar, "名前を付けて保存(A)", ID.SAVE_FILE, new SHORTCUT(Keys.None));
			fileMenuBar.DropDownItems.Add(new ToolStripSeparator());
			addMenuItem(fileMenuBar, "PDF出力(P)", ID.PDF, new SHORTCUT(Keys.P));
			mainMenuBar.Items.Add(fileMenuBar);
			#endregion

			#region Edit
			var editMenuBar = new ToolStripMenuItem();
			editMenuBar.Text = "編集(E)";
			editMenuBar.Font = menuFont;
			addMenuItem(editMenuBar, "選択", ID.SELECT, new SHORTCUT(Keys.Escape, false));
			addMenuItem(editMenuBar, "全選択", ID.SELECT_ALL, new SHORTCUT(Keys.A));
			addMenuItem(editMenuBar, "切り取り", ID.CUT, new SHORTCUT(Keys.X));
			addMenuItem(editMenuBar, "コピー", ID.COPY, new SHORTCUT(Keys.C));
			addMenuItem(editMenuBar, "貼り付け", ID.PASTE, new SHORTCUT(Keys.V));
			addMenuItem(editMenuBar, "削除", ID.DELETE, new SHORTCUT(Keys.Delete, false));
			editMenuBar.DropDownItems.Add(new ToolStripSeparator());
			addMenuItem(editMenuBar, "元に戻す", ID.UNDO, new SHORTCUT(Keys.Z));
			addMenuItem(editMenuBar, "やり直し", ID.REDO, new SHORTCUT(Keys.Y));
			editMenuBar.DropDownItems.Add(new ToolStripSeparator());
			addMenuItem(editMenuBar, "中心に移動", ID.CENTER_CIRCUIT, new SHORTCUT(Keys.E));
			editMenuBar.DropDownItems.Add(new ToolStripSeparator());
			addElementItem(editMenuBar, "配線", DUMP_ID.WIRE, new SHORTCUT(Keys.W));
			addElementItem(editMenuBar, "接地", DUMP_ID.GROUND, new SHORTCUT(Keys.G));
			editMenuBar.DropDownItems.Add(new ToolStripSeparator());
			addElementItem(editMenuBar, "テキスト", DUMP_ID.TEXT, new SHORTCUT(Keys.T));
			addElementItem(editMenuBar, "矩形", DUMP_ID.BOX, new SHORTCUT(Keys.B));
			mainMenuBar.Items.Add(editMenuBar);
			#endregion

			mainMenuBar.Items.Add(new ToolStripSeparator());
			mainMenuBar.Items.Add(new ToolStripSeparator());

			#region Passive Element
			var passiveMenuBar = new ToolStripMenuItem();
			passiveMenuBar.Text = "受動素子(P)";
			passiveMenuBar.Font = menuFont;
			addElementItem(passiveMenuBar, "抵抗", DUMP_ID.RESISTOR, new SHORTCUT(Keys.F1, false));
			addElementItem(passiveMenuBar, "キャパシタ", DUMP_ID.CAPACITOR, new SHORTCUT(Keys.F2, false));
			addElementItem(passiveMenuBar, "インダクタ", DUMP_ID.INDUCTOR, new SHORTCUT(Keys.F3, false));
			passiveMenuBar.DropDownItems.Add(new ToolStripSeparator());
			addElementItem(passiveMenuBar, "可変抵抗", DUMP_ID.POT);
			addElementItem(passiveMenuBar, "キャパシタ(有極性)", DUMP_ID.CAPACITOR_POLAR);
			addElementItem(passiveMenuBar, "トランス", DUMP_ID.TRANSFORMER);
			mainMenuBar.Items.Add(passiveMenuBar);
			#endregion

			#region Active Components
			var activeMenuBar = new ToolStripMenuItem();
			activeMenuBar.Text = "能動素子(A)";
			activeMenuBar.Font = menuFont;
			addElementItem(activeMenuBar, "ダイオード", DUMP_ID.DIODE);
			addElementItem(activeMenuBar, "ツェナーダイオード", DUMP_ID.ZENER);
			addElementItem(activeMenuBar, "可変容量ダイオード ", DUMP_ID.VARACTOR);
			addElementItem(activeMenuBar, "LED", DUMP_ID.LED);
			activeMenuBar.DropDownItems.Add(new ToolStripSeparator());
			addElementItem(activeMenuBar, "NPNトランジスタ", DUMP_ID.TRANSISTOR_N);
			addElementItem(activeMenuBar, "PNPトランジスタ", DUMP_ID.TRANSISTOR_P);
			addElementItem(activeMenuBar, "Nch MOSFET", DUMP_ID.MOSFET_N);
			addElementItem(activeMenuBar, "Pch MOSFET", DUMP_ID.MOSFET_P);
			addElementItem(activeMenuBar, "Nch JFET", DUMP_ID.JFET_N);
			addElementItem(activeMenuBar, "Pch JFET", DUMP_ID.JFET_P);
			activeMenuBar.DropDownItems.Add(new ToolStripSeparator());
			addElementItem(activeMenuBar, "オペアンプ(-側が上)", DUMP_ID.OPAMP);
			addElementItem(activeMenuBar, "オペアンプ(+側が上)", DUMP_ID.OPAMP_SWAP);
			activeMenuBar.DropDownItems.Add(new ToolStripSeparator());
			addElementItem(activeMenuBar, "フォトカプラ", DUMP_ID.OPTO_COUPLER);
			addElementItem(activeMenuBar, "アナログスイッチ", DUMP_ID.ANALOG_SW);
			mainMenuBar.Items.Add(activeMenuBar);
			#endregion

			#region Inputs and Sources
			var inputMenuBar = new ToolStripMenuItem();
			inputMenuBar.Text = "入力(I)";
			inputMenuBar.Font = menuFont;
			addElementItem(inputMenuBar, "2端子電圧源(直流)", DUMP_ID.DC);
			addElementItem(inputMenuBar, "2端子電圧源(正弦波)", DUMP_ID.AC);
			addElementItem(inputMenuBar, "2端子電圧源(パルス)", DUMP_ID.VOLTAGE);
			inputMenuBar.DropDownItems.Add(new ToolStripSeparator());
			addElementItem(inputMenuBar, "1端子電圧源(直流)", DUMP_ID.RAIL_DC);
			addElementItem(inputMenuBar, "1端子電圧源(正弦波)", DUMP_ID.RAIL_AC);
			addElementItem(inputMenuBar, "1端子電圧源(パルス)", DUMP_ID.RAIL);
			inputMenuBar.DropDownItems.Add(new ToolStripSeparator());
			addElementItem(inputMenuBar, "クロック", DUMP_ID.CLOCK);
			addElementItem(inputMenuBar, "スイープ", DUMP_ID.SWEEP);
			addElementItem(inputMenuBar, "ノイズ", DUMP_ID.NOISE);
			inputMenuBar.DropDownItems.Add(new ToolStripSeparator());
			addElementItem(inputMenuBar, "AM発信器", DUMP_ID.AM);
			addElementItem(inputMenuBar, "FM発信器", DUMP_ID.FM);
			inputMenuBar.DropDownItems.Add(new ToolStripSeparator());
			addElementItem(inputMenuBar, "定電流源", DUMP_ID.CURRENT);
			mainMenuBar.Items.Add(inputMenuBar);
			#endregion

			#region Outputs and Labels
			var outputMenuBar = new ToolStripMenuItem();
			outputMenuBar.Text = "計測器/出力(O)";
			outputMenuBar.Font = menuFont;
			addElementItem(outputMenuBar, "出力ピン", DUMP_ID.LABELED_NODE);
			outputMenuBar.DropDownItems.Add(new ToolStripSeparator());
			addElementItem(outputMenuBar, "電圧計(2端子)", DUMP_ID.VOLTMETER);
			addElementItem(outputMenuBar, "電圧計(1端子)", DUMP_ID.VOLTMETER1);
			addElementItem(outputMenuBar, "電流計", DUMP_ID.AMMETER);
			outputMenuBar.DropDownItems.Add(new ToolStripSeparator());
			addElementItem(outputMenuBar, "データ出力", DUMP_ID.DATA_RECORDER);
			//addElementItem(outputMenuBar, "音声ファイル出力", DUMP_ID.OUTPUT_AUDIO);
			outputMenuBar.DropDownItems.Add(new ToolStripSeparator());
			addElementItem(outputMenuBar, "停止トリガー", DUMP_ID.STOP_TRIGGER);
			mainMenuBar.Items.Add(outputMenuBar);
			#endregion

			#region Switch
			var switchMenuBar = new ToolStripMenuItem();
			switchMenuBar.Text = "スイッチ(S)";
			switchMenuBar.Font = menuFont;
			addElementItem(switchMenuBar, "スイッチ", DUMP_ID.SWITCH);
			addElementItem(switchMenuBar, "切り替えスイッチ", DUMP_ID.SWITCH_MULTI);
			addElementItem(switchMenuBar, "プッシュスイッチ(C接点)", DUMP_ID.SWITCH_PUSH_C);
			addElementItem(switchMenuBar, "プッシュスイッチ(NC)", DUMP_ID.SWITCH_PUSH_NC);
			addElementItem(switchMenuBar, "プッシュスイッチ(NO)", DUMP_ID.SWITCH_PUSH_NO);
			mainMenuBar.Items.Add(switchMenuBar);
			#endregion

			#region Logic Gates
			var gateMenuBar = new ToolStripMenuItem();
			gateMenuBar.Text = "論理ゲート(G)";
			gateMenuBar.Font = menuFont;
			addElementItem(gateMenuBar, "入力", DUMP_ID.LOGIC_I);
			addElementItem(gateMenuBar, "出力", DUMP_ID.LOGIC_O);
			gateMenuBar.DropDownItems.Add(new ToolStripSeparator());
			addElementItem(gateMenuBar, "3ステートバッファ", DUMP_ID.TRISTATE);
			addElementItem(gateMenuBar, "シュミットトリガ", DUMP_ID.SCHMITT);
			addElementItem(gateMenuBar, "シュミットトリガ(NOT)", DUMP_ID.INVERT_SCHMITT);
			gateMenuBar.DropDownItems.Add(new ToolStripSeparator());
			addElementItem(gateMenuBar, "AND", DUMP_ID.AND_GATE);
			addElementItem(gateMenuBar, "OR", DUMP_ID.OR_GATE);
			addElementItem(gateMenuBar, "NOT", DUMP_ID.INVERT);
			addElementItem(gateMenuBar, "NAND", DUMP_ID.NAND_GATE);
			addElementItem(gateMenuBar, "NOR", DUMP_ID.NOR_GATE);
			addElementItem(gateMenuBar, "XOR", DUMP_ID.XOR_GATE);
			mainMenuBar.Items.Add(gateMenuBar);
			#endregion

			#region Logic ICs
			//var logicIcMenuBar = new ToolStripMenuItem();
			//logicIcMenuBar.Text = "論理IC(L)";
			//logicIcMenuBar.Font = menuFont;
			//addElementItem(logicIcMenuBar, "全加算器", ELEMENTS.FullAdderElm);
			//addElementItem(logicIcMenuBar, "半加算器", ELEMENTS.HalfAdderElm);
			//logicIcMenuBar.DropDownItems.Add(new ToolStripSeparator());
			//addElementItem(logicIcMenuBar, "ラッチ", ELEMENTS.LatchElm);
			//addElementItem(logicIcMenuBar, "マルチプレクサ", ELEMENTS.MultiplexerElm);
			//addElementItem(logicIcMenuBar, "デマルチプレクサ", ELEMENTS.DeMultiplexerElm);
			//logicIcMenuBar.DropDownItems.Add(new ToolStripSeparator());
			//addElementItem(logicIcMenuBar, "フリップフロップ(D)", ELEMENTS.DFlipFlopElm);
			//addElementItem(logicIcMenuBar, "フリップフロップ(JK)", ELEMENTS.JKFlipFlopElm);
			//addElementItem(logicIcMenuBar, "フリップフロップ(T)", ELEMENTS.TFlipFlopElm);
			//logicIcMenuBar.DropDownItems.Add(new ToolStripSeparator());
			//addElementItem(logicIcMenuBar, "カウンタ", ELEMENTS.CounterElm);
			//addElementItem(logicIcMenuBar, "リングカウンタ", ELEMENTS.RingCounterElm);
			//addElementItem(logicIcMenuBar, "シフトレジスタ(SIPO)", ELEMENTS.SipoShiftElm);
			//addElementItem(logicIcMenuBar, "シフトレジスタ(PISO)", ELEMENTS.PisoShiftElm);
			//addElementItem(logicIcMenuBar, "SRAM", ELEMENTS.SRAMElm);
			//logicIcMenuBar.DropDownItems.Add(new ToolStripSeparator());
			//addElementItem(logicIcMenuBar, "カスタムロジック", ELEMENTS.CUSTOM_LOGIC);
			//mainMenuBar.Items.Add(logicIcMenuBar);
			#endregion

			#region Active Building Blocks
			//var activeBlockMenuBar = new ToolStripMenuItem();
			//activeBlockMenuBar.Text = "モジュール(M)";
			//activeBlockMenuBar.Font = menuFont;
			//addElementItem(activeBlocMenuBar, "Add Op Amp (real)", ITEM.OpAmpRealElm);
			//addElementItem(activeBlocMenuBar, "Add CCII+", ITEM.CC2Elm);
			//addElementItem(activeBlocMenuBar, "Add CCII-", ITEM.CC2NegElm);
			//addElementItem(activeBlocMenuBar, "Add OTA (LM13700 style)", ITEM.OTAElm);
			//addElementItem(activeBlocMenuBar, "Add Voltage-Controlled Voltage Source", ITEM.VCVSElm);
			//addElementItem(activeBlockMenuBar, "Add Voltage-Controlled Current Source", ELEMENTS.VCCS);
			//addElementItem(activeBlocMenuBar, "Add Current-Controlled Voltage Source", ITEM.CCVSElm);
			//addElementItem(activeBlockMenuBar, "Add Current-Controlled Current Source", ELEMENTS.CCCS);
			//mainMenuBar.Items.Add(activeBlockMenuBar);
			#endregion

			#region Analog and Hybrid Chips
			//var achipMenuBar = new ToolStripMenuItem();
			//achipMenuBar.Text = "Analog and Hybrid Chips(N)";
			//achipMenuBar.Font = menuFont;
			//addMenuItem(achipMenuBar, "Add 555 Timer", ITEM.TimerElm);
			//addMenuItem(achipMenuBar, "Add Phase Comparator", ITEM.PhaseCompElm);
			//addMenuItem(achipMenuBar, "Add DAC", ITEM.DACElm);
			//addMenuItem(achipMenuBar, "Add ADC", ITEM.ADCElm);
			//addMenuItem(achipMenuBar, "Add VCO", ITEM.VCOElm);
			//addMenuItem(achipMenuBar, "Add Monostable", ITEM.MonostableElm);
			//mainMenuBar.Items.Add(achipMenuBar);
			#endregion
		}
	}
}
