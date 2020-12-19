using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Circuit.Elements;

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

    enum MENU_CATEGORY {
        MAIN,
        OPTIONS,
        KEY,
        ELEMENTS,
        SCOPE_POP,
        CIRCUITS
    }

    enum MENU_ITEM {
        INVALID,

        #region [File]
        OPEN_FILE,
        SAVE_FILE,
        createsubcircuit,
        dcanalysis,
        print,
        recover,
        #endregion

        #region [Setting]
        SETUP,
        OTHER,
        #endregion

        #region [Edit]
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
        #endregion

        edit,
        sliders,
        flip,
        split,

        #region Scope
        ScopeElm,
        STACK_ALL,
        UNSTACK_ALL,
        COMBINE_ALL,
        SEPARATE_ALL,
        VIEW_IN_SCOPE,
        VIEW_IN_FLOAT_SCOPE,
        dock,
        undock,
        remove,
        removeplot,
        speed2,
        speed1_2,
        maxscale,
        stack,
        unstack,
        combine,
        selecty,
        reset,
        properties,
        #endregion

        #region Basic Element
        WireElm,
        ResistorElm,
        CapacitorElm,
        InductorElm,
        GroundElm,
        #endregion

        #region Passive Components
        PolarCapacitorElm,
        SwitchElm,
        PushSwitchElm,
        Switch2Elm,
        PotElm,
        TransformerElm,
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
        DiodeElm,
        ZenerElm,
        TransistorElm,
        NTransistorElm,
        PTransistorElm,
        MosfetElm,
        NMosfetElm,
        PMosfetElm,
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
        VoltageElm,
        DCVoltageElm,
        ACVoltageElm,
        RailElm,
        ACRailElm,
        SquareRailElm,
        ClockElm,
        SweepElm,
        AntennaElm,
        AMElm,
        FMElm,
        CurrentElm,
        NoiseElm,
        AudioInputElm,
        #endregion

        #region Outputs and Labels
        OutputElm,
        LEDElm,
        LampElm,
        TextElm,
        BoxElm,
        ProbeElm,
        OhmMeterElm,
        TestPointElm,
        AmmeterElm,
        DataRecorderElm,
        AudioOutputElm,
        LEDArrayElm,
        StopTriggerElm,
        #endregion

        #region Active Building Blocks
        OpAmpElm,
        OpAmpSwapElm,
        OpAmpRealElm,
        AnalogSwitchElm,
        AnalogSwitch2Elm,
        TriStateElm,
        SchmittElm,
        InvertingSchmittElm,
        CC2Elm,
        CC2NegElm,
        ComparatorElm,
        ComparatorSwapElm,
        OTAElm,
        VCVSElm,
        VCCSElm,
        CCVSElm,
        CCCSElm,
        OptocouplerElm,
        CustomCompositeElm,
        #endregion

        #region Logic Gates
        LogicInputElm,
        LogicOutputElm,
        InverterElm,
        NandGateElm,
        NorGateElm,
        AndGateElm,
        OrGateElm,
        XorGateElm,
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
        CustomLogicElm,
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

        _COUNT
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
        #region variable
        public List<ToolStripMenuItem> mainMenuItems = new List<ToolStripMenuItem>();
        public List<MENU_ITEM> mainMenuItemNames = new List<MENU_ITEM>();
        public MENU_ITEM[] shortcuts = new MENU_ITEM[127];
        Font menuFont = new Font("Segoe UI", 9.0f);
        CirSim mSim;
        #endregion

        public MenuItems(CirSim sim) {
            mSim = sim;
        }

        public static DUMP_ID getDumpIdFromString(string v) {
            DUMP_ID e;
            if (Enum.TryParse(v, out e)) {
                return e;
            } else {
                return DUMP_ID.INVALID;
            }
        }

        public static MENU_ITEM getItemFromString(string v) {
            MENU_ITEM e;
            if (Enum.TryParse(v, out e)) {
                return e;
            } else {
                return MENU_ITEM.INVALID;
            }
        }

        public void saveShortcuts() {
            Console.WriteLine("saveShortcuts");
            /* format: version;code1=ClassName;code2=ClassName;etc */
            string str = "1";
            for (int i = 0; i != shortcuts.Length; i++) {
                var sh = shortcuts[i];
                if (sh == MENU_ITEM.INVALID) {
                    continue;
                }
                str += ";" + i + "=" + sh;
            }
            var stor = Storage.getLocalStorageIfSupported();
            stor.setItem("shortcuts", str);
        }

        void addElementItem(ToolStripMenuItem menu, string title, MENU_ITEM item) {
            var shortcut = DUMP_ID.INVALID;
            var elm = constructElement(item, 0, 0);
            if (elm != null) {
                if (elm.NeedsShortcut) {
                    shortcut = elm.Shortcut;
                    shortcuts[(int)elm.Shortcut] = item;
                }
                elm.Delete();
            }
            ToolStripMenuItem mi;
            if (shortcut == DUMP_ID.INVALID) {
                mi = new ToolStripMenuItem();
            } else {
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
            }
            mi.Font = menuFont;
            mi.Text = title;
            mi.Click += new EventHandler((sender, e) => {
                mSim.MenuPerformed(MENU_CATEGORY.MAIN, item);
                if (null != mi.OwnerItem) {
                    for (int i = 0; i < mainMenuItems.Count; i++) {
                        if (mainMenuItems[i].Checked) {
                            mainMenuItems[i].Checked = false;
                            mainMenuItems[i].OwnerItem.BackColor = Color.Transparent;
                        }
                    }
                    mi.Checked = true;
                    mi.OwnerItem.BackColor = Color.LightGray;
                }
            });
            mainMenuItems.Add(mi);
            mainMenuItemNames.Add(item);
            menu.DropDownItems.Add(mi);
        }

        void addMenuItem(ToolStripMenuItem menu, string title, MENU_ITEM item, SHORTCUT shortCut) {
            addMenuItem(menu, title, MENU_CATEGORY.KEY, item, shortCut);
        }

        void addMenuItem(ToolStripMenuItem menu, string title, MENU_CATEGORY cat, MENU_ITEM item) {
            addMenuItem(menu, title, cat, item, new SHORTCUT(Keys.None));
        }

        void addMenuItem(ToolStripMenuItem menu, string title, MENU_CATEGORY cat, MENU_ITEM item, SHORTCUT shortCut) {
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
                mSim.MenuPerformed(cat, item);
            });
            mainMenuItems.Add(mi);
            mainMenuItemNames.Add(item);
            menu.DropDownItems.Add(mi);
        }

        public void composeMainMenu(MenuStrip mainMenuBar) {
            #region File
            var fileMenuBar = new ToolStripMenuItem();
            fileMenuBar.Text = "ファイル(F)";
            fileMenuBar.Font = menuFont;
            addMenuItem(fileMenuBar, "開く(O)", MENU_ITEM.OPEN_FILE, new SHORTCUT(Keys.O));
            fileMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addMenuItem(fileMenuBar, "上書き保存(S)", MENU_ITEM.SAVE_FILE, new SHORTCUT(Keys.S));
            addMenuItem(fileMenuBar, "名前を付けて保存(A)", MENU_ITEM.SAVE_FILE, new SHORTCUT(Keys.None));
            fileMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addMenuItem(fileMenuBar, "印刷(P)", MENU_ITEM.print, new SHORTCUT(Keys.None));
            mainMenuBar.Items.Add(fileMenuBar);
            #endregion

            #region Setting
            var settingMenuBar = new ToolStripMenuItem();
            settingMenuBar.Text = "設定(S)";
            settingMenuBar.Font = menuFont;
            addMenuItem(settingMenuBar, "セットアップ(U)", MENU_CATEGORY.CIRCUITS, MENU_ITEM.SETUP);
            addMenuItem(settingMenuBar, "その他(O)", MENU_CATEGORY.OPTIONS, MENU_ITEM.OTHER);
            mainMenuBar.Items.Add(settingMenuBar);
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
            addElementItem(basicMenuBar, "配線", MENU_ITEM.WireElm);
            addElementItem(basicMenuBar, "接地", MENU_ITEM.GroundElm);
            basicMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addElementItem(basicMenuBar, "抵抗", MENU_ITEM.ResistorElm);
            addElementItem(basicMenuBar, "コンデンサ", MENU_ITEM.CapacitorElm);
            addElementItem(basicMenuBar, "コイル", MENU_ITEM.InductorElm);
            mainMenuBar.Items.Add(new ToolStripSeparator());
            mainMenuBar.Items.Add(basicMenuBar);
            #endregion

            #region Passive Components
            var passMenuBar = new ToolStripMenuItem();
            passMenuBar.Text = "受動素子(P)";
            passMenuBar.Font = menuFont;
            addElementItem(passMenuBar, "スイッチ", MENU_ITEM.SwitchElm);
            addElementItem(passMenuBar, "プッシュスイッチ", MENU_ITEM.PushSwitchElm);
            addElementItem(passMenuBar, "切り替えスイッチ", MENU_ITEM.Switch2Elm);
            passMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addElementItem(passMenuBar, "可変抵抗", MENU_ITEM.PotElm);
            addElementItem(passMenuBar, "コンデンサ(有極性)", MENU_ITEM.PolarCapacitorElm);
            addElementItem(passMenuBar, "トランス", MENU_ITEM.TransformerElm);
            //addMenuItem(passMenuBar, "Add Tapped Transformer", ITEM.TappedTransformerElm);
            //addMenuItem(passMenuBar, "Add Transmission Line", ITEM.TransLineElm);
            //addMenuItem(passMenuBar, "Add Relay", ITEM.RelayElm);
            //addMenuItem(passMenuBar, "Add Memristor", ITEM.MemristorElm);
            //addMenuItem(passMenuBar, "Add Spark Gap", ITEM.SparkGapElm);
            //addMenuItem(passMenuBar, "Add Fuse", ITEM.FuseElm);
            //addMenuItem(passMenuBar, "Add Custom Transformer", ITEM.CustomTransformerElm);
            //addMenuItem(passMenuBar, "Add Crystal", ITEM.CrystalElm);
            mainMenuBar.Items.Add(passMenuBar);
            #endregion

            #region Active Components
            var activeMenuBar = new ToolStripMenuItem();
            activeMenuBar.Text = "能動素子(A)";
            activeMenuBar.Font = menuFont;
            addElementItem(activeMenuBar, "ダイオード", MENU_ITEM.DiodeElm);
            addElementItem(activeMenuBar, "ツェナーダイオード", MENU_ITEM.ZenerElm);
            addElementItem(activeMenuBar, "LED", MENU_ITEM.LEDElm);
            //addElementItem(activeMenuBar, "LED Array", MENU_ITEM.LEDArrayElm);
            activeMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addElementItem(activeMenuBar, "NPNトランジスタ", MENU_ITEM.NTransistorElm);
            addElementItem(activeMenuBar, "PNPトランジスタ", MENU_ITEM.PTransistorElm);
            addElementItem(activeMenuBar, "Nch MOSトランジスタ", MENU_ITEM.NMosfetElm);
            addElementItem(activeMenuBar, "Pch MOSトランジスタ", MENU_ITEM.PMosfetElm);
            //addMenuItem(activeMenuBar, "Add JFET (N-Channel)", ITEM.NJfetElm);
            //addMenuItem(activeMenuBar, "Add JFET (P-Channel)", ITEM.PJfetElm);
            //addMenuItem(activeMenuBar, "Add SCR", ITEM.SCRElm);
            //addMenuItem(activeMenuBar, "Add DIAC", ITEM.DiacElm);
            //addMenuItem(activeMenuBar, "Add TRIAC", ITEM.TriacElm);
            //addMenuItem(activeMenuBar, "Add Darlington Pair (NPN)", ITEM.NDarlingtonElm);
            //addMenuItem(activeMenuBar, "Add Darlington Pair (PNP)", ITEM.PDarlingtonElm);
            //addMenuItem(activeMenuBar, "Add Varactor/Varicap", ITEM.VaractorElm);
            //addMenuItem(activeMenuBar, "Add Tunnel Diode", ITEM.TunnelDiodeElm);
            //addMenuItem(activeMenuBar, "Add Triode", ITEM.TriodeElm);
            ////addMenuItem(activeMenuBar, "Add Photoresistor", MENU_ITEM.PhotoResistorElm);
            ////addMenuItem(activeMenuBar, "Add Thermistor", MENU_ITEM.ThermistorElm);
            activeMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addElementItem(activeMenuBar, "オペアンプ(-側が上)", MENU_ITEM.OpAmpElm);
            addElementItem(activeMenuBar, "オペアンプ(+側が上)", MENU_ITEM.OpAmpSwapElm);
            activeMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addElementItem(activeMenuBar, "フォトカプラ", MENU_ITEM.OptocouplerElm);
            mainMenuBar.Items.Add(activeMenuBar);
            #endregion

            #region Inputs and Sources
            var inputMenuBar = new ToolStripMenuItem();
            inputMenuBar.Text = "入力源(I)";
            inputMenuBar.Font = menuFont;
            addElementItem(inputMenuBar, "直流電圧源(2端子)", MENU_ITEM.DCVoltageElm);
            addElementItem(inputMenuBar, "交流電圧源(2端子)", MENU_ITEM.ACVoltageElm);
            addElementItem(inputMenuBar, "定電流源", MENU_ITEM.CurrentElm);
            inputMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addElementItem(inputMenuBar, "直流電圧源(1端子)", MENU_ITEM.RailElm);
            addElementItem(inputMenuBar, "交流電圧源(1端子)", MENU_ITEM.ACRailElm);
            inputMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addElementItem(inputMenuBar, "クロック", MENU_ITEM.ClockElm);
            addElementItem(inputMenuBar, "スイープ", MENU_ITEM.SweepElm);
            addElementItem(inputMenuBar, "AM発信器", MENU_ITEM.AMElm);
            addElementItem(inputMenuBar, "FM発信器", MENU_ITEM.FMElm);
            //addMenuItem(inputMenuBar, "Add Square Wave Source (1-terminal)", ITEM.SquareRailElm);
            //addMenuItem(inputMenuBar, "Add Antenna", ITEM.AntennaElm);
            //addMenuItem(inputMenuBar, "Add Noise Generator", ITEM.NoiseElm);
            //addMenuItem(inputMenuBar, "Add Audio Input", ITEM.AudioInputElm);
            mainMenuBar.Items.Add(inputMenuBar);
            #endregion

            #region Outputs and Labels
            var outputMenuBar = new ToolStripMenuItem();
            outputMenuBar.Text = "計測器/出力(O)";
            outputMenuBar.Font = menuFont;
            addElementItem(outputMenuBar, "電圧計", MENU_ITEM.ProbeElm);
            addElementItem(outputMenuBar, "電流計", MENU_ITEM.AmmeterElm);
            addElementItem(outputMenuBar, "音声ファイル出力", MENU_ITEM.AudioOutputElm);
            //addElementItem(outputMenuBar, "Add Ohmmeter", MENU_ITEM.OhmMeterElm);
            //outputMenuBar.DropDownItems.Add(new ToolStripSeparator());
            //addElementItem(outputMenuBar, "Add Analog Output", MENU_ITEM.OutputElm);
            //addElementItem(outputMenuBar, "Add Data Export", MENU_ITEM.DataRecorderElm);
            //outputMenuBar.DropDownItems.Add(new ToolStripSeparator());
            //addElementItem(outputMenuBar, "Add Lamp", MENU_ITEM.LampElm);
            //addElementItem(outputMenuBar, "Add Text", MENU_ITEM.TextElm);
            //addElementItem(outputMenuBar, "Add Box", MENU_ITEM.BoxElm);
            //addElementItem(outputMenuBar, "Add Test Point", MENU_ITEM.TestPointElm);
            //addElementItem(outputMenuBar, "Add Stop Trigger", MENU_ITEM.StopTriggerElm);
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
            addElementItem(gateMenuBar, "AND", MENU_ITEM.AndGateElm);
            addElementItem(gateMenuBar, "OR", MENU_ITEM.OrGateElm);
            addElementItem(gateMenuBar, "XOR", MENU_ITEM.XorGateElm);
            addElementItem(gateMenuBar, "NOT", MENU_ITEM.InverterElm);
            addElementItem(gateMenuBar, "NAND", MENU_ITEM.NandGateElm);
            addElementItem(gateMenuBar, "NOR", MENU_ITEM.NorGateElm);
            addElementItem(gateMenuBar, "カスタムロジック", MENU_ITEM.CustomLogicElm);
            gateMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addElementItem(gateMenuBar, "入力", MENU_ITEM.LogicInputElm);
            addElementItem(gateMenuBar, "出力", MENU_ITEM.LogicOutputElm);
            gateMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addElementItem(gateMenuBar, "シュミットトリガ", MENU_ITEM.SchmittElm);
            addElementItem(gateMenuBar, "シュミットトリガ(NOT)", MENU_ITEM.InvertingSchmittElm);
            addElementItem(gateMenuBar, "3ステートバッファ", MENU_ITEM.TriStateElm);
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

        public static CircuitElm constructElement(MENU_ITEM n, int x1, int y1) {
            switch (n) {
            case MENU_ITEM.ScopeElm:
                return new ScopeElm(x1, y1);

            #region Basic Element
            case MENU_ITEM.WireElm:
                return new WireElm(x1, y1);
            case MENU_ITEM.ResistorElm:
                return new ResistorElm(x1, y1);
            case MENU_ITEM.CapacitorElm:
                return new CapacitorElm(x1, y1);
            case MENU_ITEM.InductorElm:
                return new InductorElm(x1, y1);
            case MENU_ITEM.GroundElm:
                return new GroundElm(x1, y1);
            #endregion

            #region Passive Components
            case MENU_ITEM.SwitchElm:
                return new SwitchElm(x1, y1);
            case MENU_ITEM.PushSwitchElm:
                return new PushSwitchElm(x1, y1);
            case MENU_ITEM.Switch2Elm:
                return new Switch2Elm(x1, y1);
            case MENU_ITEM.PotElm:
                return new PotElm(x1, y1);
            case MENU_ITEM.PolarCapacitorElm:
                return new PolarCapacitorElm(x1, y1);
            case MENU_ITEM.TransformerElm:
                return new TransformerElm(x1, y1);
            case MENU_ITEM.TappedTransformerElm:
                return null; //(CircuitElm)new TappedTransformerElm(x1, y1);
            case MENU_ITEM.TransLineElm:
                return null; //(CircuitElm)new TransLineElm(x1, y1);
            case MENU_ITEM.RelayElm:
                return null; //(CircuitElm)new RelayElm(x1, y1);
            case MENU_ITEM.MemristorElm:
                return null; //(CircuitElm)new MemristorElm(x1, y1);
            case MENU_ITEM.SparkGapElm:
                return null; //(CircuitElm)new SparkGapElm(x1, y1);
            case MENU_ITEM.FuseElm:
                return null; //(CircuitElm)new FuseElm(x1, y1);
            case MENU_ITEM.CustomTransformerElm:
                return null; //(CircuitElm)new CustomTransformerElm(x1, y1);
            case MENU_ITEM.CrystalElm:
                return null; //(CircuitElm)new CrystalElm(x1, y1);
            #endregion

            #region Active Components
            case MENU_ITEM.DiodeElm:
                return new DiodeElm(x1, y1);
            case MENU_ITEM.ZenerElm:
                return new ZenerElm(x1, y1);
            case MENU_ITEM.LEDElm:
                return new LEDElm(x1, y1);
            case MENU_ITEM.TransistorElm:
            case MENU_ITEM.NTransistorElm:
                return new NTransistorElm(x1, y1);
            case MENU_ITEM.PTransistorElm:
                return new PTransistorElm(x1, y1);
            case MENU_ITEM.MosfetElm:
            case MENU_ITEM.NMosfetElm:
                return new NMosfetElm(x1, y1);
            case MENU_ITEM.PMosfetElm:
                return new PMosfetElm(x1, y1);
            case MENU_ITEM.JfetElm:
            case MENU_ITEM.NJfetElm:
                return null; //(CircuitElm)new NJfetElm(x1, y1);
            case MENU_ITEM.PJfetElm:
                return null; //(CircuitElm)new PJfetElm(x1, y1);
            case MENU_ITEM.SCRElm:
                return null; //(CircuitElm)new SCRElm(x1, y1);
            case MENU_ITEM.DiacElm:
                return null; //(CircuitElm)new DiacElm(x1, y1);
            case MENU_ITEM.TriacElm:
                return null; //(CircuitElm)new TriacElm(x1, y1);
            case MENU_ITEM.DarlingtonElm:
            case MENU_ITEM.NDarlingtonElm:
                return null; //(CircuitElm)new NDarlingtonElm(x1, y1);
            case MENU_ITEM.PDarlingtonElm:
                return null; //(CircuitElm)new PDarlingtonElm(x1, y1);
            case MENU_ITEM.VaractorElm:
                return null; //(CircuitElm)new VaractorElm(x1, y1);
            case MENU_ITEM.TunnelDiodeElm:
                return null; //(CircuitElm)new TunnelDiodeElm(x1, y1);
            case MENU_ITEM.TriodeElm:
                return null; //(CircuitElm)new TriodeElm(x1, y1);
            #endregion

            #region Inputs and Sources
            case MENU_ITEM.DCVoltageElm:
            case MENU_ITEM.VoltageElm:
                return new DCVoltageElm(x1, y1);
            case MENU_ITEM.ACVoltageElm:
                return new ACVoltageElm(x1, y1);
            case MENU_ITEM.RailElm:
                return new RailElm(x1, y1);
            case MENU_ITEM.ACRailElm:
                return new ACRailElm(x1, y1);
            case MENU_ITEM.SquareRailElm:
                return null; //(CircuitElm)new SquareRailElm(x1, y1);
            case MENU_ITEM.ClockElm:
                return new ClockElm(x1, y1);
            case MENU_ITEM.SweepElm:
                return new SweepElm(x1, y1);
            case MENU_ITEM.AntennaElm:
                return null; //(CircuitElm)new AntennaElm(x1, y1);
            case MENU_ITEM.AMElm:
                return new AMElm(x1, y1);
            case MENU_ITEM.FMElm:
                return new FMElm(x1, y1);
            case MENU_ITEM.CurrentElm:
                return new CurrentElm(x1, y1);
            case MENU_ITEM.NoiseElm:
                return null; //(CircuitElm)new NoiseElm(x1, y1);
            case MENU_ITEM.AudioInputElm:
                return null; //(CircuitElm)new AudioInputElm(x1, y1);
            #endregion

            #region Outputs and Labels
            case MENU_ITEM.OutputElm:
                return new OutputElm(x1, y1);
            case MENU_ITEM.LampElm:
                return null; //(CircuitElm)new LampElm(x1, y1);
            case MENU_ITEM.TextElm:
                return null; //(CircuitElm)new TextElm(x1, y1);
            case MENU_ITEM.BoxElm:
                return null; //(CircuitElm)new BoxElm(x1, y1);
            case MENU_ITEM.ProbeElm:
                return new ProbeElm(x1, y1);
            case MENU_ITEM.OhmMeterElm:
                return null; //(CircuitElm)new OhmMeterElm(x1, y1);
            case MENU_ITEM.TestPointElm:
                return null; //new TestPointElm(x1, y1);
            case MENU_ITEM.AmmeterElm:
                return new AmmeterElm(x1, y1);
            case MENU_ITEM.DataRecorderElm:
                return null; //(CircuitElm)new DataRecorderElm(x1, y1);
            case MENU_ITEM.AudioOutputElm:
                return new AudioOutputElm(x1, y1);
            case MENU_ITEM.LEDArrayElm:
                return null; //(CircuitElm)new LEDArrayElm(x1, y1);
            case MENU_ITEM.StopTriggerElm:
                return null; //(CircuitElm)new StopTriggerElm(x1, y1);
            #endregion

            #region Active Building Blocks
            case MENU_ITEM.OpAmpElm:
                return new OpAmpElm(x1, y1);
            case MENU_ITEM.OpAmpSwapElm:
                return new OpAmpSwapElm(x1, y1);
            case MENU_ITEM.OpAmpRealElm:
                return null; //(CircuitElm)new OpAmpRealElm(x1, y1);
            case MENU_ITEM.AnalogSwitchElm:
                return new AnalogSwitchElm(x1, y1);
            case MENU_ITEM.AnalogSwitch2Elm:
                return null; //(CircuitElm)new AnalogSwitch2Elm(x1, y1);
            case MENU_ITEM.SchmittElm:
                return new SchmittElm(x1, y1);
            case MENU_ITEM.InvertingSchmittElm:
                return new InvertingSchmittElm(x1, y1);
            case MENU_ITEM.CC2Elm:
                return null; //(CircuitElm)new CC2Elm(x1, y1);
            case MENU_ITEM.CC2NegElm:
                return null; //(CircuitElm)new CC2NegElm(x1, y1);
            case MENU_ITEM.ComparatorElm:
                return null; //new ComparatorElm(x1, y1);
            case MENU_ITEM.ComparatorSwapElm:
                return null; //new ComparatorSwapElm(x1, y1);
            case MENU_ITEM.OTAElm:
                return null; //(CircuitElm)new OTAElm(x1, y1);
            case MENU_ITEM.VCVSElm:
                return null; //(CircuitElm)new VCVSElm(x1, y1);
            case MENU_ITEM.VCCSElm:
                return new VCCSElm(x1, y1);
            case MENU_ITEM.CCVSElm:
                return null; //(CircuitElm)new CCVSElm(x1, y1);
            case MENU_ITEM.CCCSElm:
                return new CCCSElm(x1, y1);
            case MENU_ITEM.OptocouplerElm:
                return new OptocouplerElm(x1, y1);
            case MENU_ITEM.CustomCompositeElm:
                return new CustomCompositeElm(x1, y1);
            #endregion

            #region Logic Gates
            case MENU_ITEM.LogicInputElm:
                return new LogicInputElm(x1, y1);
            case MENU_ITEM.LogicOutputElm:
                return new LogicOutputElm(x1, y1);
            case MENU_ITEM.TriStateElm:
                return new TriStateElm(x1, y1);
            case MENU_ITEM.InverterElm:
                return new InverterElm(x1, y1);
            case MENU_ITEM.AndGateElm:
                return new AndGateElm(x1, y1);
            case MENU_ITEM.NandGateElm:
                return new NandGateElm(x1, y1);
            case MENU_ITEM.OrGateElm:
                return new OrGateElm(x1, y1);
            case MENU_ITEM.NorGateElm:
                return new NorGateElm(x1, y1);
            case MENU_ITEM.XorGateElm:
                return new XorGateElm(x1, y1);
            #endregion

            #region Digital Chips
            case MENU_ITEM.DFlipFlopElm:
                return null; //(CircuitElm)new DFlipFlopElm(x1, y1);
            case MENU_ITEM.JKFlipFlopElm:
                return null; //(CircuitElm)new JKFlipFlopElm(x1, y1);
            case MENU_ITEM.TFlipFlopElm:
                return null; //(CircuitElm)new TFlipFlopElm(x1, y1);
            case MENU_ITEM.SevenSegElm:
                return null; //(CircuitElm)new SevenSegElm(x1, y1);
            case MENU_ITEM.SevenSegDecoderElm:
                return null; //(CircuitElm)new SevenSegDecoderElm(x1, y1);
            case MENU_ITEM.MultiplexerElm:
                return null; //(CircuitElm)new MultiplexerElm(x1, y1);
            case MENU_ITEM.DeMultiplexerElm:
                return null; //(CircuitElm)new DeMultiplexerElm(x1, y1);
            case MENU_ITEM.SipoShiftElm:
                return null; //(CircuitElm)new SipoShiftElm(x1, y1);
            case MENU_ITEM.PisoShiftElm:
                return null; //(CircuitElm)new PisoShiftElm(x1, y1);
            case MENU_ITEM.CounterElm:
                return null; //(CircuitElm)new CounterElm(x1, y1);
            /* if you take out DecadeElm, it will break the menus and people's saved shortcuts */
            /* if you take out RingCounterElm, it will break subcircuits */
            case MENU_ITEM.DecadeElm:
            case MENU_ITEM.RingCounterElm:
                return new RingCounterElm(x1, y1);
            case MENU_ITEM.LatchElm:
                return null; //(CircuitElm)new LatchElm(x1, y1);
            case MENU_ITEM.SeqGenElm:
                return null; //(CircuitElm)new SeqGenElm(x1, y1);
            case MENU_ITEM.FullAdderElm:
                return null; //(CircuitElm)new FullAdderElm(x1, y1);
            case MENU_ITEM.HalfAdderElm:
                return null; //(CircuitElm)new HalfAdderElm(x1, y1);
            /* if you take out UserDefinedLogicElm, it will break people's saved shortcuts */
            case MENU_ITEM.CustomLogicElm:
            case MENU_ITEM.UserDefinedLogicElm:
                return new CustomLogicElm(x1, y1);
            case MENU_ITEM.SRAMElm:
                return null; //(CircuitElm)new SRAMElm(x1, y1);
            #endregion

            #region Analog and Hybrid Chips
            case MENU_ITEM.TimerElm:
                return null; //(CircuitElm)new TimerElm(x1, y1);
            case MENU_ITEM.PhaseCompElm:
                return null; //(CircuitElm)new PhaseCompElm(x1, y1);
            case MENU_ITEM.DACElm:
                return null; //(CircuitElm)new DACElm(x1, y1);
            case MENU_ITEM.ADCElm:
                return null; //(CircuitElm)new ADCElm(x1, y1);
            case MENU_ITEM.VCOElm:
                return null; //(CircuitElm)new VCOElm(x1, y1);
            case MENU_ITEM.MonostableElm:
                return null; //(CircuitElm)new MonostableElm(x1, y1);
            #endregion

            default:
                return null;
            }
        }

        public static CircuitElm createCe(DUMP_ID tint, int x1, int y1, int x2, int y2, int f, StringTokenizer st) {
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
