using Circuit.Elements;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit {
    struct SHORTCUT {
        public Keys Key { get; private set; }
        public string Name { get; private set; }

        public SHORTCUT(Keys k, bool c = true, bool s = false, bool a = false) {
            Key = k;
            Name = "";
            if (k != Keys.None) {
                Name = name(k);
                if (a || 0 < (k & Keys.Alt)) {
                    Key |= Keys.Alt;
                    Name += " + Alt";
                }
                if (c || 0 < (k & Keys.Control)) {
                    Key |= Keys.Control;
                    Name += " + Ctrl";
                }
                if (s || 0 < (k & Keys.Shift)) {
                    Key |= Keys.Shift;
                    Name += " + Shift";
                }
            }
        }

        static string name(Keys k) {
            switch(k) {
            case Keys.D0:
            case Keys.D1:
            case Keys.D2:
            case Keys.D3:
            case Keys.D4:
            case Keys.D5:
            case Keys.D6:
            case Keys.D7:
            case Keys.D8:
            case Keys.D9:
                return k.ToString().Replace("D", "");
            case Keys.Delete:
                return "Del";
            case Keys.Oemplus:
                return "+";
            case Keys.OemMinus:
                return "-";
            default:
                return k.ToString();
            }
        }

        static SHORTCUT create(Keys k = Keys.None, bool c = false, bool s = false, bool a = false) {
            return new SHORTCUT(k, c, s, a);
        }

        public static SHORTCUT fromDumpId(DUMP_ID id) {
            switch (id) {
            case DUMP_ID.INVALID:
                return create();
            case DUMP_ID.GROUND:
                return create(Keys.G, true, false, false);
            case DUMP_ID.WIRE:
                return create(Keys.W, true, false, false);
            case DUMP_ID.RESISTOR:
                return create(Keys.R, true, true, false);
            case DUMP_ID.INDUCTOR:
                return create(Keys.L, true, true, false);
            case DUMP_ID.CAPACITOR:
                return create(Keys.C, true, true, false);
            case DUMP_ID.DIODE:
                return create(Keys.D, true, true, false);
            case DUMP_ID.BIPOLER_NPN:
                return create(Keys.N, true, false, false);
            case DUMP_ID.BIPOLER_PNP:
                return create(Keys.P, true, false, false);
            case DUMP_ID.NMOS:
                return create(Keys.N, true, true, false);
            case DUMP_ID.PMOS:
                return create(Keys.P, true, true, false);
            default:
                return create();
            }
        }
    }

    enum MENU_ITEM {
        OPEN_NEW,
        OPEN_FILE,
        SAVE_FILE,
        CREATE_MODULE,
        DC_ANALYSIS,
        PRINT,
        RECOVER,

        SELECT,
        CUT,
        COPY,
        PASTE,
        SELECT_ALL,
        DUPLICATE,
        DELETE,
        UNDO,
        REDO,
        ZOOM_IN,
        ZOOM_OUT,
        ZOOM_100,
        CENTER_CIRCUIT,

        STACK_ALL,
        UNSTACK_ALL,
        COMBINE_ALL,
        SEPARATE_ALL
    }

    enum ELEMENTS {
        INVALID,
        SCOPE,

        #region Basic Element
        WIRE,
        GROUND,
        RESISTOR,
        CAPACITOR,
        CAPACITOR_POLER,
        INDUCTOR,
        #endregion

        #region Passive Components
        SWITCH,
        SWITCH_PUSH,
        SWITCH_TERM,
        POT,
        TRANSFORMER,
        TappedTransformerElm,
        TransLineElm,
        RelayElm,
        MemristorElm,
        SparkGapElm,
        FuseElm,
        CustomTransformerElm,
        CrystalElm,
        #endregion

        #region Active Components
        DIODE,
        ZENER,
        LED,
        TRANSISTOR,
        TRANSISTOR_N,
        TRANSISTOR_P,
        MOSFET,
        MOSFET_N,
        MOSFET_P,
        JfetElm,
        NJfetElm,
        PJfetElm,
        SCRElm,
        DiacElm,
        TriacElm,
        DarlingtonElm,
        NDarlingtonElm,
        PDarlingtonElm,
        VaractorElm,
        TunnelDiodeElm,
        TriodeElm,
        PhotoResistorElm,
        ThermistorElm,
        #endregion

        #region Inputs and Sources
        VOLTAGE_DC,
        VOLTAGE_AC,
        RAIL_DC,
        RAIL_AC,
        CLOCK,
        SWEEP,
        OSC_AM,
        OSC_FM,
        CURRENT,
        SquareRailElm,
        AntennaElm,
        NoiseElm,
        AudioInputElm,
        #endregion

        #region Outputs and Labels
        OUTPUT,
        PROBE,
        AMMETER,
        OhmMeterElm,
        DataRecorderElm,
        OUTPUT_AUDIO,
        LampElm,
        TextElm,
        BoxElm,
        TestPointElm,
        LEDArrayElm,
        StopTriggerElm,
        #endregion

        #region Active Building Blocks
        OPAMP,
        OPAMP_SWAP,
        OpAmpRealElm,
        AnalogSwitchElm,
        AnalogSwitch2Elm,
        TRISTATE,
        SCHMITT,
        SCHMITT_INV,
        CC2Elm,
        CC2NegElm,
        ComparatorElm,
        ComparatorSwapElm,
        OTAElm,
        VCVSElm,
        VCCSElm,
        CCVSElm,
        CCCSElm,
        OPTOCOUPLER,
        CustomCompositeElm,
        #endregion

        #region Logic Gates
        LOGIC_INPUT,
        LOGIC_OUTPUT,
        NOT_GATE,
        NAND_GATE,
        NOR_GATE,
        AND_GATE,
        OR_GATE,
        XOR_GATE,
        #endregion

        #region Digital Chips
        DFlipFlopElm,
        JKFlipFlopElm,
        TFlipFlopElm,
        SevenSegElm,
        SevenSegDecoderElm,
        MultiplexerElm,
        DeMultiplexerElm,
        SipoShiftElm,
        PisoShiftElm,
        CounterElm,
        DecadeElm,
        RingCounterElm,
        LatchElm,
        SeqGenElm,
        FullAdderElm,
        HalfAdderElm,
        CUSTOM_LOGIC,
        UserDefinedLogicElm,
        SRAMElm,
        #endregion

        #region Analog and Hybrid Chips
        TimerElm,
        PhaseCompElm,
        DACElm,
        ADCElm,
        VCOElm,
        MonostableElm,
        #endregion
    }

    enum DUMP_ID {
        INVALID = 0,
        OPAMP = 'a',
        CAPACITOR = 'c',
        DIODE = 'd',
        MOSFET = 'f',
        GROUND = 'g',
        CURRENT = 'i',
        INDUCTOR = 'l',
        BIPOLER_NPN = 'n',
        BIPOLER_PNP = 'p',
        TRANSISTOR = 't',
        VOLTAGE = 'v',
        WIRE = 'w',
        RESISTOR = 'r',
        GRAPHIC = 'G',
        INVERT = 'I',
        LOGIC_I = 'L',
        LOGIC_O = 'M',
        NMOS = 'N',
        OUTPUT = 'O',
        PMOS = 'P',
        RAIL = 'R',
        SWITCH = 's',
        SWITCH2 = 'S',
        TRANSFORMER = 'T',
        PROBE = '>',
        AND_GATE = 150,
        NAND_GATE = 151,
        OR_GATE = 152,
        NOR_GATE = 153,
        XOR_GATE = 154,
        ANALOG_SW = 159,
        LED = 162,
        RING_COUNTER = 163,
        SWEEP = 170,
        POT = 174,
        TRISTATE = 180,
        SCHMITT = 182,
        INVERT_SCHMITT = 183,
        AM = 200,
        FM = 201,
        LABELED_NODE = 207,
        CUSTOM_LOGIC = 208,
        CAPACITOR_POLAR = 209,
        WAVE_OUT = 211,
        VCCS = 213,
        CCCS = 215,
        AMMETER = 370,
        COMPARATOR = 401,
        SCOPE = 403,
        OPTO_COUPLER = 407,
        CUSTOM_COMPOSITE = 410
    }

    class MenuItems {
        CirSim mSim;
        List<ToolStripMenuItem> mMainMenuItems = new List<ToolStripMenuItem>();
        Font menuFont = new Font("Segoe UI", 9.0f);

        public MenuItems(CirSim sim) {
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

        public static ELEMENTS GetItemFromString(string v) {
            ELEMENTS e;
            if (Enum.TryParse(v, out e)) {
                return e;
            } else {
                return ELEMENTS.INVALID;
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

        void addElementItem(ToolStripMenuItem menu, string title, ELEMENTS item) {
            var elm = ConstructElement(item, 0, 0);
            if (elm != null) {
                elm.Delete();
            }
            ToolStripMenuItem mi;
            var sc = SHORTCUT.fromDumpId(elm.Shortcut);
            if (sc.Key == Keys.None) {
                mi = new ToolStripMenuItem();
            } else {
                mi = new ToolStripMenuItem() {
                    ShowShortcutKeys = true,
                    ShortcutKeys = sc.Key,
                    ShortcutKeyDisplayString = sc.Name
                };
            }
            mi.Font = menuFont;
            mi.Text = title;
            mi.Click += new EventHandler((sender, e) => {
                mSim.Performed(item);
                if (null != mi.OwnerItem) {
                    for (int i = 0; i < mMainMenuItems.Count; i++) {
                        if (mMainMenuItems[i].Checked) {
                            mMainMenuItems[i].Checked = false;
                            mMainMenuItems[i].OwnerItem.BackColor = Color.Transparent;
                        }
                    }
                    mi.Checked = true;
                    mi.OwnerItem.BackColor = Color.LightGray;
                }
            });
            mMainMenuItems.Add(mi);
            menu.DropDownItems.Add(mi);
        }

        void addMenuItem(ToolStripMenuItem menu, string title, MENU_ITEM item, SHORTCUT shortCut) {
            ToolStripMenuItem mi;
            if (shortCut.Key == Keys.None) {
                mi = new ToolStripMenuItem() {
                    Font = menuFont,
                    Text = title
                };
            } else {
                mi = new ToolStripMenuItem() {
                    Font = menuFont,
                    Text = title,
                    ShowShortcutKeys = true,
                    ShortcutKeys = shortCut.Key,
                    ShortcutKeyDisplayString = shortCut.Name,
                };
            }
            mi.Click += new EventHandler((sender, e) => {
                mSim.Performed(item);
            });
            mMainMenuItems.Add(mi);
            menu.DropDownItems.Add(mi);
        }

        public void ComposeMainMenu(MenuStrip mainMenuBar) {
            #region File
            var fileMenuBar = new ToolStripMenuItem();
            fileMenuBar.Text = "ファイル(F)";
            fileMenuBar.Font = menuFont;
            addMenuItem(fileMenuBar, "新規作成(N)", MENU_ITEM.OPEN_NEW, new SHORTCUT(Keys.None));
            fileMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addMenuItem(fileMenuBar, "開く(O)", MENU_ITEM.OPEN_FILE, new SHORTCUT(Keys.O));
            addMenuItem(fileMenuBar, "再読み込み(R)", MENU_ITEM.RECOVER, new SHORTCUT(Keys.None));
            fileMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addMenuItem(fileMenuBar, "上書き保存(S)", MENU_ITEM.SAVE_FILE, new SHORTCUT(Keys.S));
            addMenuItem(fileMenuBar, "名前を付けて保存(A)", MENU_ITEM.SAVE_FILE, new SHORTCUT(Keys.None));
            fileMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addMenuItem(fileMenuBar, "モジュールを作成(M)", MENU_ITEM.CREATE_MODULE, new SHORTCUT(Keys.None));
            fileMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addMenuItem(fileMenuBar, "印刷(P)", MENU_ITEM.PRINT, new SHORTCUT(Keys.None));
            mainMenuBar.Items.Add(fileMenuBar);
            #endregion

            #region Edit
            var editMenuBar = new ToolStripMenuItem();
            editMenuBar.Text = "編集(E)";
            editMenuBar.Font = menuFont;
            addMenuItem(editMenuBar, "切り取り(T)", MENU_ITEM.CUT, new SHORTCUT(Keys.X));
            addMenuItem(editMenuBar, "コピー(C)", MENU_ITEM.COPY, new SHORTCUT(Keys.C));
            addMenuItem(editMenuBar, "貼り付け(P)", MENU_ITEM.PASTE, new SHORTCUT(Keys.V));
            addMenuItem(editMenuBar, "全選択(A)", MENU_ITEM.SELECT_ALL, new SHORTCUT(Keys.A));
            addMenuItem(editMenuBar, "複製(D)", MENU_ITEM.DUPLICATE, new SHORTCUT(Keys.D));
            addMenuItem(editMenuBar, "削除(L)", MENU_ITEM.DELETE, new SHORTCUT(Keys.Delete, false));
            /* */
            editMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addMenuItem(editMenuBar, "元に戻す(U)", MENU_ITEM.UNDO, new SHORTCUT(Keys.Z));
            addMenuItem(editMenuBar, "やり直し(R)", MENU_ITEM.REDO, new SHORTCUT(Keys.Y));
            /* */
            editMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addMenuItem(editMenuBar, "拡大(I)", MENU_ITEM.ZOOM_IN, new SHORTCUT(Keys.Oemplus));
            addMenuItem(editMenuBar, "縮小(O)", MENU_ITEM.ZOOM_OUT, new SHORTCUT(Keys.OemMinus));
            addMenuItem(editMenuBar, "原寸大(L)", MENU_ITEM.ZOOM_100, new SHORTCUT(Keys.D0));
            addMenuItem(editMenuBar, "中心に移動(E)", MENU_ITEM.CENTER_CIRCUIT, new SHORTCUT(Keys.E));
            mainMenuBar.Items.Add(editMenuBar);
            #endregion

            #region Basic Element
            var basicMenuBar = new ToolStripMenuItem();
            basicMenuBar.Text = "基本(B)";
            basicMenuBar.Font = menuFont;
            addElementItem(basicMenuBar, "配線", ELEMENTS.WIRE);
            addElementItem(basicMenuBar, "接地", ELEMENTS.GROUND);
            basicMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addElementItem(basicMenuBar, "抵抗", ELEMENTS.RESISTOR);
            addElementItem(basicMenuBar, "コンデンサ", ELEMENTS.CAPACITOR);
            addElementItem(basicMenuBar, "コンデンサ(有極性)", ELEMENTS.CAPACITOR_POLER);
            addElementItem(basicMenuBar, "コイル", ELEMENTS.INDUCTOR);
            mainMenuBar.Items.Add(new ToolStripSeparator());
            mainMenuBar.Items.Add(basicMenuBar);
            #endregion

            #region Passive Components
            var passMenuBar = new ToolStripMenuItem();
            passMenuBar.Text = "受動素子(P)";
            passMenuBar.Font = menuFont;
            addElementItem(passMenuBar, "スイッチ", ELEMENTS.SWITCH);
            addElementItem(passMenuBar, "プッシュスイッチ", ELEMENTS.SWITCH_PUSH);
            addElementItem(passMenuBar, "切り替えスイッチ", ELEMENTS.SWITCH_TERM);
            passMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addElementItem(passMenuBar, "可変抵抗", ELEMENTS.POT);
            addElementItem(passMenuBar, "トランス", ELEMENTS.TRANSFORMER);
            //addMenuItem(passMenuBar, "Add Tapped Transformer", ELEMENTS.TappedTransformerElm);
            //addMenuItem(passMenuBar, "Add Transmission Line", ELEMENTS.TransLineElm);
            //addMenuItem(passMenuBar, "Add Relay", ELEMENTS.RelayElm);
            //addMenuItem(passMenuBar, "Add Memristor", ELEMENTS.MemristorElm);
            //addMenuItem(passMenuBar, "Add Spark Gap", ELEMENTS.SparkGapElm);
            //addMenuItem(passMenuBar, "Add Fuse", ELEMENTS.FuseElm);
            //addMenuItem(passMenuBar, "Add Custom Transformer", ELEMENTS.CustomTransformerElm);
            //addMenuItem(passMenuBar, "Add Crystal", ELEMENTS.CrystalElm);
            mainMenuBar.Items.Add(passMenuBar);
            #endregion

            #region Active Components
            var activeMenuBar = new ToolStripMenuItem();
            activeMenuBar.Text = "能動素子(A)";
            activeMenuBar.Font = menuFont;
            addElementItem(activeMenuBar, "ダイオード", ELEMENTS.DIODE);
            addElementItem(activeMenuBar, "ツェナーダイオード", ELEMENTS.ZENER);
            addElementItem(activeMenuBar, "LED", ELEMENTS.LED);
            //addElementItem(activeMenuBar, "LED Array", MENU_ITEM.LEDArrayElm);
            activeMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addElementItem(activeMenuBar, "NPNトランジスタ", ELEMENTS.TRANSISTOR_N);
            addElementItem(activeMenuBar, "PNPトランジスタ", ELEMENTS.TRANSISTOR_P);
            addElementItem(activeMenuBar, "Nch MOSトランジスタ", ELEMENTS.MOSFET_N);
            addElementItem(activeMenuBar, "Pch MOSトランジスタ", ELEMENTS.MOSFET_P);
            //addMenuItem(activeMenuBar, "Add JFET (N-Channel)", ELEMENTS.NJfetElm);
            //addMenuItem(activeMenuBar, "Add JFET (P-Channel)", ELEMENTS.PJfetElm);
            //addMenuItem(activeMenuBar, "Add SCR", ELEMENTS.SCRElm);
            //addMenuItem(activeMenuBar, "Add DIAC", ELEMENTS.DiacElm);
            //addMenuItem(activeMenuBar, "Add TRIAC", ELEMENTS.TriacElm);
            //addMenuItem(activeMenuBar, "Add Darlington Pair (NPN)", ELEMENTS.NDarlingtonElm);
            //addMenuItem(activeMenuBar, "Add Darlington Pair (PNP)", ELEMENTS.PDarlingtonElm);
            //addMenuItem(activeMenuBar, "Add Varactor/Varicap", ELEMENTS.VaractorElm);
            //addMenuItem(activeMenuBar, "Add Tunnel Diode", ELEMENTS.TunnelDiodeElm);
            //addMenuItem(activeMenuBar, "Add Triode", ELEMENTS.TriodeElm);
            ////addMenuItem(activeMenuBar, "Add Photoresistor", ELEMENTS.PhotoResistorElm);
            ////addMenuItem(activeMenuBar, "Add Thermistor", ELEMENTS.ThermistorElm);
            activeMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addElementItem(activeMenuBar, "オペアンプ(-側が上)", ELEMENTS.OPAMP);
            addElementItem(activeMenuBar, "オペアンプ(+側が上)", ELEMENTS.OPAMP_SWAP);
            activeMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addElementItem(activeMenuBar, "フォトカプラ", ELEMENTS.OPTOCOUPLER);
            mainMenuBar.Items.Add(activeMenuBar);
            #endregion

            #region Inputs and Sources
            var inputMenuBar = new ToolStripMenuItem();
            inputMenuBar.Text = "入力源(I)";
            inputMenuBar.Font = menuFont;
            addElementItem(inputMenuBar, "直流電圧源(2端子)", ELEMENTS.VOLTAGE_DC);
            addElementItem(inputMenuBar, "交流電圧源(2端子)", ELEMENTS.VOLTAGE_AC);
            addElementItem(inputMenuBar, "定電流源", ELEMENTS.CURRENT);
            inputMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addElementItem(inputMenuBar, "直流電圧源(1端子)", ELEMENTS.RAIL_DC);
            addElementItem(inputMenuBar, "交流電圧源(1端子)", ELEMENTS.RAIL_AC);
            inputMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addElementItem(inputMenuBar, "クロック", ELEMENTS.CLOCK);
            addElementItem(inputMenuBar, "スイープ", ELEMENTS.SWEEP);
            addElementItem(inputMenuBar, "AM発信器", ELEMENTS.OSC_AM);
            addElementItem(inputMenuBar, "FM発信器", ELEMENTS.OSC_FM);
            //addMenuItem(inputMenuBar, "Add Square Wave Source (1-terminal)", ELEMENTS.SquareRailElm);
            //addMenuItem(inputMenuBar, "Add Antenna", ELEMENTS.AntennaElm);
            //addMenuItem(inputMenuBar, "Add Noise Generator", ELEMENTS.NoiseElm);
            //addMenuItem(inputMenuBar, "Add Audio Input", ELEMENTS.AudioInputElm);
            mainMenuBar.Items.Add(inputMenuBar);
            #endregion

            #region Outputs and Labels
            var outputMenuBar = new ToolStripMenuItem();
            outputMenuBar.Text = "計測器/出力(O)";
            outputMenuBar.Font = menuFont;
            addElementItem(outputMenuBar, "出力ピン", ELEMENTS.OUTPUT);
            outputMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addElementItem(outputMenuBar, "電圧計", ELEMENTS.PROBE);
            addElementItem(outputMenuBar, "電流計", ELEMENTS.AMMETER);
            //addElementItem(outputMenuBar, "Add Ohmmeter", ELEMENTS.OhmMeterElm);
            outputMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addElementItem(outputMenuBar, "音声ファイル出力", ELEMENTS.OUTPUT_AUDIO);
            //addElementItem(outputMenuBar, "Add Data Export", ELEMENTS.DataRecorderElm);
            //outputMenuBar.DropDownItems.Add(new ToolStripSeparator());
            //addElementItem(outputMenuBar, "Add Lamp", ELEMENTS.LampElm);
            //addElementItem(outputMenuBar, "Add Text", ELEMENTS.TextElm);
            //addElementItem(outputMenuBar, "Add Box", ELEMENTS.BoxElm);
            //addElementItem(outputMenuBar, "Add Test Point", ELEMENTS.TestPointElm);
            //addElementItem(outputMenuBar, "Add Stop Trigger", ELEMENTS.StopTriggerElm);
            mainMenuBar.Items.Add(outputMenuBar);
            #endregion

            #region Active Building Blocks
            //var activeBlocMenuBar = new ToolStripMenuItem();
            //activeBlocMenuBar.Text = "Active Building Blocks(C)";
            //activeBlocMenuBar.Font = menuFont;
            //addMenuItem(activeBlocMenuBar, "Add Op Amp (real)", ITEM.OpAmpRealElm);
            //addMenuItem(activeBlocMenuBar, "Add Analog Switch (SPST)", ITEM.AnalogSwitchElm);
            //addMenuItem(activeBlocMenuBar, "Add Analog Switch (SPDT)", ITEM.AnalogSwitch2Elm);
            //addMenuItem(activeBlocMenuBar, "Add CCII+", ITEM.CC2Elm);
            //addMenuItem(activeBlocMenuBar, "Add CCII-", ITEM.CC2NegElm);
            //addMenuItem(activeBlocMenuBar, "Add OTA (LM13700 style)", ITEM.OTAElm);
            //addMenuItem(activeBlocMenuBar, "Add Voltage-Controlled Voltage Source", ITEM.VCVSElm);
            //addMenuItem(activeBlocMenuBar, "Add Voltage-Controlled Current Source", ITEM.VCCSElm);
            //addMenuItem(activeBlocMenuBar, "Add Current-Controlled Voltage Source", ITEM.CCVSElm);
            //addMenuItem(activeBlocMenuBar, "Add Current-Controlled Current Source", ITEM.CCCSElm);
            //addMenuItem(activeBlocMenuBar, "Add Subcircuit Instance", ITEM.CustomCompositeElm);
            //mainMenuBar.Items.Add(activeBlocMenuBar);
            #endregion

            #region Logic Gates
            var gateMenuBar = new ToolStripMenuItem();
            gateMenuBar.Text = "論理回路(L)";
            gateMenuBar.Font = menuFont;
            addElementItem(gateMenuBar, "AND", ELEMENTS.AND_GATE);
            addElementItem(gateMenuBar, "OR", ELEMENTS.OR_GATE);
            addElementItem(gateMenuBar, "XOR", ELEMENTS.XOR_GATE);
            addElementItem(gateMenuBar, "NOT", ELEMENTS.NOT_GATE);
            addElementItem(gateMenuBar, "NAND", ELEMENTS.NAND_GATE);
            addElementItem(gateMenuBar, "NOR", ELEMENTS.NOR_GATE);
            addElementItem(gateMenuBar, "カスタムロジック", ELEMENTS.CUSTOM_LOGIC);
            gateMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addElementItem(gateMenuBar, "入力", ELEMENTS.LOGIC_INPUT);
            addElementItem(gateMenuBar, "出力", ELEMENTS.LOGIC_OUTPUT);
            gateMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addElementItem(gateMenuBar, "シュミットトリガ", ELEMENTS.SCHMITT);
            addElementItem(gateMenuBar, "シュミットトリガ(NOT)", ELEMENTS.SCHMITT_INV);
            addElementItem(gateMenuBar, "3ステートバッファ", ELEMENTS.TRISTATE);
            mainMenuBar.Items.Add(gateMenuBar);
            #endregion

            #region Digital Chips
            //var chipMenuBar = new ToolStripMenuItem();
            //chipMenuBar.Text = "Digital Chips(D)";
            //chipMenuBar.Font = menuFont;
            //addMenuItem(chipMenuBar, "Add D Flip-Flop", ITEM.DFlipFlopElm);
            //addMenuItem(chipMenuBar, "Add JK Flip-Flop", ITEM.JKFlipFlopElm);
            //addMenuItem(chipMenuBar, "Add T Flip-Flop", ITEM.TFlipFlopElm);
            //addMenuItem(chipMenuBar, "Add 7 Segment LED", ITEM.SevenSegElm);
            //addMenuItem(chipMenuBar, "Add 7 Segment Decoder", ITEM.SevenSegDecoderElm);
            //addMenuItem(chipMenuBar, "Add Multiplexer", ITEM.MultiplexerElm);
            //addMenuItem(chipMenuBar, "Add Demultiplexer", ITEM.DeMultiplexerElm);
            //addMenuItem(chipMenuBar, "Add SIPO shift register", ITEM.SipoShiftElm);
            //addMenuItem(chipMenuBar, "Add PISO shift register", ITEM.PisoShiftElm);
            //addMenuItem(chipMenuBar, "Add Counter", ITEM.CounterElm);
            //addElementItem(chipMenuBar, "Add Ring Counter", MENU_ITEM.DecadeElm);
            //addMenuItem(chipMenuBar, "Add Latch", ITEM.LatchElm);
            //addMenuItem(chipMenuBar, "Add Sequence generator", ITEM.SeqGenElm);
            //addMenuItem(chipMenuBar, "Add Full Adder", ITEM.FullAdderElm);
            //addMenuItem(chipMenuBar, "Add Half Adder", ITEM.HalfAdderElm);
            //addMenuItem(chipMenuBar, "Add Static RAM", ITEM.SRAMElm);
            //mainMenuBar.Items.Add(chipMenuBar);
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

        public static CircuitElm ConstructElement(ELEMENTS n, int x1, int y1) {
            switch (n) {
            #region Basic Element
            case ELEMENTS.WIRE:
                return new WireElm(x1, y1);
            case ELEMENTS.RESISTOR:
                return new ResistorElm(x1, y1);
            case ELEMENTS.CAPACITOR:
                return new CapacitorElm(x1, y1);
            case ELEMENTS.INDUCTOR:
                return new InductorElm(x1, y1);
            case ELEMENTS.GROUND:
                return new GroundElm(x1, y1);
            #endregion

            #region Passive Components
            case ELEMENTS.SWITCH:
                return new SwitchElm(x1, y1);
            case ELEMENTS.SWITCH_PUSH:
                return new PushSwitchElm(x1, y1);
            case ELEMENTS.SWITCH_TERM:
                return new Switch2Elm(x1, y1);
            case ELEMENTS.POT:
                return new PotElm(x1, y1);
            case ELEMENTS.CAPACITOR_POLER:
                return new PolarCapacitorElm(x1, y1);
            case ELEMENTS.TRANSFORMER:
                return new TransformerElm(x1, y1);
            case ELEMENTS.TappedTransformerElm:
                return null; //(CircuitElm)new TappedTransformerElm(x1, y1);
            case ELEMENTS.TransLineElm:
                return null; //(CircuitElm)new TransLineElm(x1, y1);
            case ELEMENTS.RelayElm:
                return null; //(CircuitElm)new RelayElm(x1, y1);
            case ELEMENTS.MemristorElm:
                return null; //(CircuitElm)new MemristorElm(x1, y1);
            case ELEMENTS.SparkGapElm:
                return null; //(CircuitElm)new SparkGapElm(x1, y1);
            case ELEMENTS.FuseElm:
                return null; //(CircuitElm)new FuseElm(x1, y1);
            case ELEMENTS.CustomTransformerElm:
                return null; //(CircuitElm)new CustomTransformerElm(x1, y1);
            case ELEMENTS.CrystalElm:
                return null; //(CircuitElm)new CrystalElm(x1, y1);
            #endregion

            #region Active Components
            case ELEMENTS.DIODE:
                return new DiodeElm(x1, y1);
            case ELEMENTS.ZENER:
                return new ZenerElm(x1, y1);
            case ELEMENTS.LED:
                return new LEDElm(x1, y1);
            case ELEMENTS.TRANSISTOR:
            case ELEMENTS.TRANSISTOR_N:
                return new NTransistorElm(x1, y1);
            case ELEMENTS.TRANSISTOR_P:
                return new PTransistorElm(x1, y1);
            case ELEMENTS.MOSFET:
            case ELEMENTS.MOSFET_N:
                return new NMosfetElm(x1, y1);
            case ELEMENTS.MOSFET_P:
                return new PMosfetElm(x1, y1);
            case ELEMENTS.JfetElm:
            case ELEMENTS.NJfetElm:
                return null; //(CircuitElm)new NJfetElm(x1, y1);
            case ELEMENTS.PJfetElm:
                return null; //(CircuitElm)new PJfetElm(x1, y1);
            case ELEMENTS.SCRElm:
                return null; //(CircuitElm)new SCRElm(x1, y1);
            case ELEMENTS.DiacElm:
                return null; //(CircuitElm)new DiacElm(x1, y1);
            case ELEMENTS.TriacElm:
                return null; //(CircuitElm)new TriacElm(x1, y1);
            case ELEMENTS.DarlingtonElm:
            case ELEMENTS.NDarlingtonElm:
                return null; //(CircuitElm)new NDarlingtonElm(x1, y1);
            case ELEMENTS.PDarlingtonElm:
                return null; //(CircuitElm)new PDarlingtonElm(x1, y1);
            case ELEMENTS.VaractorElm:
                return null; //(CircuitElm)new VaractorElm(x1, y1);
            case ELEMENTS.TunnelDiodeElm:
                return null; //(CircuitElm)new TunnelDiodeElm(x1, y1);
            case ELEMENTS.TriodeElm:
                return null; //(CircuitElm)new TriodeElm(x1, y1);
            #endregion

            #region Inputs and Sources
            case ELEMENTS.VOLTAGE_DC:
                return new DCVoltageElm(x1, y1);
            case ELEMENTS.VOLTAGE_AC:
                return new ACVoltageElm(x1, y1);
            case ELEMENTS.RAIL_DC:
                return new RailElm(x1, y1);
            case ELEMENTS.RAIL_AC:
                return new ACRailElm(x1, y1);
            case ELEMENTS.SquareRailElm:
                return null; //(CircuitElm)new SquareRailElm(x1, y1);
            case ELEMENTS.CLOCK:
                return new ClockElm(x1, y1);
            case ELEMENTS.SWEEP:
                return new SweepElm(x1, y1);
            case ELEMENTS.AntennaElm:
                return null; //(CircuitElm)new AntennaElm(x1, y1);
            case ELEMENTS.OSC_AM:
                return new AMElm(x1, y1);
            case ELEMENTS.OSC_FM:
                return new FMElm(x1, y1);
            case ELEMENTS.CURRENT:
                return new CurrentElm(x1, y1);
            case ELEMENTS.NoiseElm:
                return null; //(CircuitElm)new NoiseElm(x1, y1);
            case ELEMENTS.AudioInputElm:
                return null; //(CircuitElm)new AudioInputElm(x1, y1);
            #endregion

            #region Outputs and Labels
            case ELEMENTS.OUTPUT:
                return new LabeledNodeElm(x1, y1);
            case ELEMENTS.PROBE:
                return new ProbeElm(x1, y1);
            case ELEMENTS.AMMETER:
                return new AmmeterElm(x1, y1);
            case ELEMENTS.OhmMeterElm:
                return null; //(CircuitElm)new OhmMeterElm(x1, y1);
            case ELEMENTS.DataRecorderElm:
                return null; //(CircuitElm)new DataRecorderElm(x1, y1);
            case ELEMENTS.OUTPUT_AUDIO:
                return new AudioOutputElm(x1, y1);
            case ELEMENTS.LampElm:
                return null; //(CircuitElm)new LampElm(x1, y1);
            case ELEMENTS.TextElm:
                return null; //(CircuitElm)new TextElm(x1, y1);
            case ELEMENTS.BoxElm:
                return null; //(CircuitElm)new BoxElm(x1, y1);
            case ELEMENTS.TestPointElm:
                return null; //new TestPointElm(x1, y1);
            case ELEMENTS.LEDArrayElm:
                return null; //(CircuitElm)new LEDArrayElm(x1, y1);
            case ELEMENTS.StopTriggerElm:
                return null; //(CircuitElm)new StopTriggerElm(x1, y1);
            #endregion

            #region Active Building Blocks
            case ELEMENTS.OPAMP:
                return new OpAmpElm(x1, y1);
            case ELEMENTS.OPAMP_SWAP:
                return new OpAmpSwapElm(x1, y1);
            case ELEMENTS.OpAmpRealElm:
                return null; //(CircuitElm)new OpAmpRealElm(x1, y1);
            case ELEMENTS.AnalogSwitchElm:
                return new AnalogSwitchElm(x1, y1);
            case ELEMENTS.AnalogSwitch2Elm:
                return null; //(CircuitElm)new AnalogSwitch2Elm(x1, y1);
            case ELEMENTS.SCHMITT:
                return new SchmittElm(x1, y1);
            case ELEMENTS.SCHMITT_INV:
                return new InvertingSchmittElm(x1, y1);
            case ELEMENTS.CC2Elm:
                return null; //(CircuitElm)new CC2Elm(x1, y1);
            case ELEMENTS.CC2NegElm:
                return null; //(CircuitElm)new CC2NegElm(x1, y1);
            case ELEMENTS.ComparatorElm:
                return null; //new ComparatorElm(x1, y1);
            case ELEMENTS.ComparatorSwapElm:
                return null; //new ComparatorSwapElm(x1, y1);
            case ELEMENTS.OTAElm:
                return null; //(CircuitElm)new OTAElm(x1, y1);
            case ELEMENTS.VCVSElm:
                return null; //(CircuitElm)new VCVSElm(x1, y1);
            case ELEMENTS.VCCSElm:
                return new VCCSElm(x1, y1);
            case ELEMENTS.CCVSElm:
                return null; //(CircuitElm)new CCVSElm(x1, y1);
            case ELEMENTS.CCCSElm:
                return new CCCSElm(x1, y1);
            case ELEMENTS.OPTOCOUPLER:
                return new OptocouplerElm(x1, y1);
            case ELEMENTS.CustomCompositeElm:
                return new CustomCompositeElm(x1, y1);
            #endregion

            #region Logic Gates
            case ELEMENTS.LOGIC_INPUT:
                return new LogicInputElm(x1, y1);
            case ELEMENTS.LOGIC_OUTPUT:
                return new LogicOutputElm(x1, y1);
            case ELEMENTS.TRISTATE:
                return new TriStateElm(x1, y1);
            case ELEMENTS.NOT_GATE:
                return new InverterElm(x1, y1);
            case ELEMENTS.AND_GATE:
                return new AndGateElm(x1, y1);
            case ELEMENTS.NAND_GATE:
                return new NandGateElm(x1, y1);
            case ELEMENTS.OR_GATE:
                return new OrGateElm(x1, y1);
            case ELEMENTS.NOR_GATE:
                return new NorGateElm(x1, y1);
            case ELEMENTS.XOR_GATE:
                return new XorGateElm(x1, y1);
            #endregion

            #region Digital Chips
            case ELEMENTS.DFlipFlopElm:
                return null; //(CircuitElm)new DFlipFlopElm(x1, y1);
            case ELEMENTS.JKFlipFlopElm:
                return null; //(CircuitElm)new JKFlipFlopElm(x1, y1);
            case ELEMENTS.TFlipFlopElm:
                return null; //(CircuitElm)new TFlipFlopElm(x1, y1);
            case ELEMENTS.SevenSegElm:
                return null; //(CircuitElm)new SevenSegElm(x1, y1);
            case ELEMENTS.SevenSegDecoderElm:
                return null; //(CircuitElm)new SevenSegDecoderElm(x1, y1);
            case ELEMENTS.MultiplexerElm:
                return null; //(CircuitElm)new MultiplexerElm(x1, y1);
            case ELEMENTS.DeMultiplexerElm:
                return null; //(CircuitElm)new DeMultiplexerElm(x1, y1);
            case ELEMENTS.SipoShiftElm:
                return null; //(CircuitElm)new SipoShiftElm(x1, y1);
            case ELEMENTS.PisoShiftElm:
                return null; //(CircuitElm)new PisoShiftElm(x1, y1);
            case ELEMENTS.CounterElm:
                return null; //(CircuitElm)new CounterElm(x1, y1);
            /* if you take out DecadeElm, it will break the menus and people's saved shortcuts */
            /* if you take out RingCounterElm, it will break subcircuits */
            case ELEMENTS.DecadeElm:
            case ELEMENTS.RingCounterElm:
                return new RingCounterElm(x1, y1);
            case ELEMENTS.LatchElm:
                return null; //(CircuitElm)new LatchElm(x1, y1);
            case ELEMENTS.SeqGenElm:
                return null; //(CircuitElm)new SeqGenElm(x1, y1);
            case ELEMENTS.FullAdderElm:
                return null; //(CircuitElm)new FullAdderElm(x1, y1);
            case ELEMENTS.HalfAdderElm:
                return null; //(CircuitElm)new HalfAdderElm(x1, y1);
            /* if you take out UserDefinedLogicElm, it will break people's saved shortcuts */
            case ELEMENTS.CUSTOM_LOGIC:
            case ELEMENTS.UserDefinedLogicElm:
                return new CustomLogicElm(x1, y1);
            case ELEMENTS.SRAMElm:
                return null; //(CircuitElm)new SRAMElm(x1, y1);
            #endregion

            #region Analog and Hybrid Chips
            case ELEMENTS.TimerElm:
                return null; //(CircuitElm)new TimerElm(x1, y1);
            case ELEMENTS.PhaseCompElm:
                return null; //(CircuitElm)new PhaseCompElm(x1, y1);
            case ELEMENTS.DACElm:
                return null; //(CircuitElm)new DACElm(x1, y1);
            case ELEMENTS.ADCElm:
                return null; //(CircuitElm)new ADCElm(x1, y1);
            case ELEMENTS.VCOElm:
                return null; //(CircuitElm)new VCOElm(x1, y1);
            case ELEMENTS.MonostableElm:
                return null; //(CircuitElm)new MonostableElm(x1, y1);
            #endregion

            case ELEMENTS.SCOPE:
                return new ScopeElm(x1, y1);
            default:
                return null;
            }
        }

        public static CircuitElm CreateCe(DUMP_ID tint, int x1, int y1, int x2, int y2, int f, StringTokenizer st) {
            switch (tint) {
            //case 'A': return new AntennaElm(x1, y1, x2, y2, f, st);
            case DUMP_ID.INVERT: return new InverterElm(x1, y1, x2, y2, f, st);
            case DUMP_ID.LOGIC_I: return new LogicInputElm(x1, y1, x2, y2, f, st);
            case DUMP_ID.LOGIC_O: return new LogicOutputElm(x1, y1, x2, y2, f, st);
            case DUMP_ID.OUTPUT: return new OutputElm(x1, y1, x2, y2, f, st);
            case DUMP_ID.RAIL: return new RailElm(x1, y1, x2, y2, f, st);
            case DUMP_ID.SWITCH2: return new Switch2Elm(x1, y1, x2, y2, f, st);
            case DUMP_ID.TRANSFORMER: return new TransformerElm(x1, y1, x2, y2, f, st);
            case DUMP_ID.OPAMP: return new OpAmpElm(x1, y1, x2, y2, f, st);
            //case 'b': return new BoxElm(x1, y1, x2, y2, f, st);
            case DUMP_ID.CAPACITOR: return new CapacitorElm(x1, y1, x2, y2, f, st);
            case DUMP_ID.DIODE: return new DiodeElm(x1, y1, x2, y2, f, st);
            case DUMP_ID.MOSFET: return new MosfetElm(x1, y1, x2, y2, f, st);
            case DUMP_ID.GROUND: return new GroundElm(x1, y1, x2, y2, f, st);
            case DUMP_ID.CURRENT: return new CurrentElm(x1, y1, x2, y2, f, st);
            //case 'j': return new JfetElm(x1, y1, x2, y2, f, st);
            case DUMP_ID.INDUCTOR: return new InductorElm(x1, y1, x2, y2, f, st);
            //case 'm': return new MemristorElm(x1, y1, x2, y2, f, st);
            //case 'n': return new NoiseElm(x1, y1, x2, y2, f, st);
            case DUMP_ID.PROBE: return new ProbeElm(x1, y1, x2, y2, f, st);
            case DUMP_ID.RESISTOR: return new ResistorElm(x1, y1, x2, y2, f, st);
            case DUMP_ID.SWITCH: return new SwitchElm(x1, y1, x2, y2, f, st);
            case DUMP_ID.TRANSISTOR: return new TransistorElm(x1, y1, x2, y2, f, st);
            case DUMP_ID.VOLTAGE: return new VoltageElm(x1, y1, x2, y2, f, st);
            case DUMP_ID.WIRE: return new WireElm(x1, y1, x2, y2, f, st);
            //case 'x': return new TextElm(x1, y1, x2, y2, f, st);
            //case 'z': return new ZenerElm(x1, y1, x2, y2, f, st);
            case DUMP_ID.AND_GATE: return new AndGateElm(x1, y1, x2, y2, f, st);
            case DUMP_ID.NAND_GATE: return new NandGateElm(x1, y1, x2, y2, f, st);
            case DUMP_ID.OR_GATE: return new OrGateElm(x1, y1, x2, y2, f, st);
            case DUMP_ID.NOR_GATE: return new NorGateElm(x1, y1, x2, y2, f, st);
            case DUMP_ID.XOR_GATE: return new XorGateElm(x1, y1, x2, y2, f, st);
            //case 155: return new DFlipFlopElm(x1, y1, x2, y2, f, st);
            //case 156: return new JKFlipFlopElm(x1, y1, x2, y2, f, st);
            //case 157: return new SevenSegElm(x1, y1, x2, y2, f, st);
            //case 158: return new VCOElm(x1, y1, x2, y2, f, st);
            case DUMP_ID.ANALOG_SW: return new AnalogSwitchElm(x1, y1, x2, y2, f, st);
            //case 160: return new AnalogSwitch2Elm(x1, y1, x2, y2, f, st);
            //case 161: return new PhaseCompElm(x1, y1, x2, y2, f, st);
            case DUMP_ID.LED: return new LEDElm(x1, y1, x2, y2, f, st);
            case DUMP_ID.RING_COUNTER: return new RingCounterElm(x1, y1, x2, y2, f, st);
            //case 164: return new CounterElm(x1, y1, x2, y2, f, st);
            //case 165: return new TimerElm(x1, y1, x2, y2, f, st);
            //case 166: return new DACElm(x1, y1, x2, y2, f, st);
            //case 167: return new ADCElm(x1, y1, x2, y2, f, st);
            //case 168: return new LatchElm(x1, y1, x2, y2, f, st);
            //case 169: return new TappedTransformerElm(x1, y1, x2, y2, f, st);
            case DUMP_ID.SWEEP: return new SweepElm(x1, y1, x2, y2, f, st);
            //case 171: return new TransLineElm(x1, y1, x2, y2, f, st);
            //case 173: return new TriodeElm(x1, y1, x2, y2, f, st);
            case DUMP_ID.POT: return new PotElm(x1, y1, x2, y2, f, st);
            //case 175: return new TunnelDiodeElm(x1, y1, x2, y2, f, st);
            //case 176: return new VaractorElm(x1, y1, x2, y2, f, st);
            //case 177: return new SCRElm(x1, y1, x2, y2, f, st);
            //case 178: return new RelayElm(x1, y1, x2, y2, f, st);
            //case 179: return new CC2Elm(x1, y1, x2, y2, f, st);
            case DUMP_ID.TRISTATE: return new TriStateElm(x1, y1, x2, y2, f, st);
            //case 181: return new LampElm(x1, y1, x2, y2, f, st);
            case DUMP_ID.SCHMITT: return new SchmittElm(x1, y1, x2, y2, f, st);
            case DUMP_ID.INVERT_SCHMITT: return new InvertingSchmittElm(x1, y1, x2, y2, f, st);
            //case 184: return new MultiplexerElm(x1, y1, x2, y2, f, st);
            //case 185: return new DeMultiplexerElm(x1, y1, x2, y2, f, st);
            //case 186: return new PisoShiftElm(x1, y1, x2, y2, f, st);
            //case 187: return new SparkGapElm(x1, y1, x2, y2, f, st);
            //case 188: return new SeqGenElm(x1, y1, x2, y2, f, st);
            //case 189: return new SipoShiftElm(x1, y1, x2, y2, f, st);
            //case 193: return new TFlipFlopElm(x1, y1, x2, y2, f, st);
            //case 194: return new MonostableElm(x1, y1, x2, y2, f, st);
            //case 195: return new HalfAdderElm(x1, y1, x2, y2, f, st);
            //case 196: return new FullAdderElm(x1, y1, x2, y2, f, st);
            //case 197: return new SevenSegDecoderElm(x1, y1, x2, y2, f, st);
            case DUMP_ID.AM: return new AMElm(x1, y1, x2, y2, f, st);
            case DUMP_ID.FM: return new FMElm(x1, y1, x2, y2, f, st);
            //case 203: return new DiacElm(x1, y1, x2, y2, f, st);
            //case 206: return new TriacElm(x1, y1, x2, y2, f, st);
            case DUMP_ID.LABELED_NODE: return new LabeledNodeElm(x1, y1, x2, y2, f, st);
            case DUMP_ID.CUSTOM_LOGIC: return new CustomLogicElm(x1, y1, x2, y2, f, st);
            case DUMP_ID.CAPACITOR_POLAR: return new PolarCapacitorElm(x1, y1, x2, y2, f, st);
            //case 210: return new DataRecorderElm(x1, y1, x2, y2, f, st);
            case DUMP_ID.WAVE_OUT: return new AudioOutputElm(x1, y1, x2, y2, f, st);
            //case 212: return new VCVSElm(x1, y1, x2, y2, f, st);
            case DUMP_ID.VCCS: return new VCCSElm(x1, y1, x2, y2, f, st);
            //case 214: return new CCVSElm(x1, y1, x2, y2, f, st);
            case DUMP_ID.CCCS: return new CCCSElm(x1, y1, x2, y2, f, st);
            //case 216: return new OhmMeterElm(x1, y1, x2, y2, f, st);
            //case 368: return new TestPointElm(x1, y1, x2, y2, f, st);
            case DUMP_ID.AMMETER: return new AmmeterElm(x1, y1, x2, y2, f, st);
            //case 400: return new DarlingtonElm(x1, y1, x2, y2, f, st);
            //case DUMP_ID.COMPARATOR: return new ComparatorElm(x1, y1, x2, y2, f, st);
            //case 402: return new OTAElm(x1, y1, x2, y2, f, st);
            case DUMP_ID.SCOPE: return new ScopeElm(x1, y1, x2, y2, f, st);
            //case 404: return new FuseElm(x1, y1, x2, y2, f, st);
            //case 405: return new LEDArrayElm(x1, y1, x2, y2, f, st);
            //case 406: return new CustomTransformerElm(x1, y1, x2, y2, f, st);
            case DUMP_ID.OPTO_COUPLER: return new OptocouplerElm(x1, y1, x2, y2, f, st);
            //case 408: return new StopTriggerElm(x1, y1, x2, y2, f, st);
            //case 409: return new OpAmpRealElm(x1, y1, x2, y2, f, st);
            case DUMP_ID.CUSTOM_COMPOSITE: return new CustomCompositeElm(x1, y1, x2, y2, f, st);
            //case 411: return new AudioInputElm(x1, y1, x2, y2, f, st);
            //case 412: return new CrystalElm(x1, y1, x2, y2, f, st);
            //case 413: return new SRAMElm(x1, y1, x2, y2, f, st);
            }
            return null;
        }
    }
}
