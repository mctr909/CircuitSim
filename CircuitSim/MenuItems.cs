using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using Circuit.UI;
using Circuit.UI.Passive;
using Circuit.UI.Active;
using Circuit.UI.Input;
using Circuit.UI.Output;
using Circuit.UI.Gate;
//using Circuit.UI.Logic;
using Circuit.UI.Custom;
using static System.Windows.Forms.ToolStripItem;

namespace Circuit {
    public enum MENU_ITEM {
        OPEN_NEW,
        OPEN_FILE,
        SAVE_FILE,
        OVERWRITE,
        CREATE_MODULE,
        PRINT,

        SELECT,
        CUT,
        COPY,
        PASTE,
        SELECT_ALL,
        DELETE,
        UNDO,
        REDO,
        CENTER_CIRCUIT,

        STACK_ALL,
        UNSTACK_ALL,
        COMBINE_ALL,
        SEPARATE_ALL
    }

    public enum ELEMENTS {
        INVALID,
        SCOPE,

        #region Passive Components
        WIRE,
        GROUND,
        SWITCH,
        SWITCH_PUSH_C,
        SWITCH_PUSH_NC,
        SWITCH_PUSH_NO,
        SWITCH_MULTI,
        RESISTOR,
        POT,
        CAPACITOR,
        CAPACITOR_POLER,
        INDUCTOR,
        TRANSFORMER,
        #endregion

        #region Active Components
        DIODE,
        ZENER,
        VARACTOR,
        LED,
        TRANSISTOR_N,
        TRANSISTOR_P,
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
        TunnelDiodeElm,
        TriodeElm,
        PhotoResistorElm,
        ThermistorElm,
        #endregion

        #region Inputs and Sources
        VOLTAGE,
        VOLTAGE_DC,
        VOLTAGE_SIN,
        RAIL,
        RAIL_DC,
        RAIL_SIN,
        CLOCK,
        SWEEP,
        OSC_AM,
        OSC_FM,
        CURRENT,
        SquareRailElm,
        AntennaElm,
        NOISE,
        AudioInputElm,
        #endregion

        #region Outputs and Labels
        OUTPUT,
        VOLTMETER2,
        VOLTMETER1,
        AMMETER,
        DATA_RECORDER,
        OUTPUT_AUDIO,
        LampElm,
        TEXT,
        BOX,
        TestPointElm,
        LEDArrayElm,
        STOP_TRIGGER,
        #endregion

        #region Active Building Blocks
        OPAMP,
        OPAMP_SWAP,
        OpAmpRealElm,
        ANALOG_SWITCH,
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
        VCCS,
        CCVSElm,
        CCCS,
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

    public enum DUMP_ID {
        INVALID = 0,
        ADJUSTABLE = 38,
        OPAMP = 'a',
        BOX = 'b',
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
        PMOS = 'P',
        RAIL = 'R',
        SWITCH = 's',
        SWITCH_MULTI = 'S',
        TRANSFORMER = 'T',
        TEXT = 'x',
        VOLTMETER = '>',
        VOLTMETER1 = 'V',
        AND_GATE = 150,
        NAND_GATE = 151,
        OR_GATE = 152,
        NOR_GATE = 153,
        XOR_GATE = 154,
        FLIP_FLOP_D = 155,
        FLIP_FLOP_JK = 156,
        FLIP_FLOP_T = 193,
        ANALOG_SW = 159,
        LED = 162,
        RING_COUNTER = 163,
        COUNTER = 164,
        LATCH = 168,
        SWEEP = 170,
        POT = 174,
        VARACTOR = 176,
        ZENER = 177,
        TRISTATE = 180,
        SCHMITT = 182,
        INVERT_SCHMITT = 183,
        MULTIPLEXER = 184,
        DEMULTIPLEXER = 185,
        SHIFT_REGISTER_PISO = 186,
        SHIFT_REGISTER_SIPO = 189,
        HALF_ADDER = 195,
        FULL_ADDER = 196,
        AM = 200,
        FM = 201,
        DC,
        AC,
        RAIL_DC,
        RAIL_AC,
        LABELED_NODE = 207,
        CUSTOM_LOGIC = 208,
        CAPACITOR_POLAR = 209,
        DATA_RECORDER = 210,
        WAVE_OUT = 211,
        VCCS = 213,
        CCCS = 215,
        AMMETER = 370,
        COMPARATOR = 401,
        SCOPE = 403,
        OPTO_COUPLER = 407,
        STOP_TRIGGER = 408,
        CUSTOM_COMPOSITE = 410,
        SRAM = 413,
    }

    public class MenuItems {
        CirSimForm mSim;
        List<ToolStripMenuItem> mMainMenuItems = new List<ToolStripMenuItem>();
        Font menuFont = new Font("Segoe UI", 9.0f);

        public ToolStripMenuItem tsmWire = null;

        public MenuItems(CirSimForm sim) {
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

        ToolStripMenuItem addElementItem(ToolStripMenuItem menu, string title, ELEMENTS item) {
            var elm = ConstructElement(item);
            if (elm == null) {
                return null;
            } else {
                elm.Delete();
            }
            ToolStripMenuItem mi;
            var sc = shortcut(elm.Shortcut);
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
                    mi.OwnerItem.BackColor = Color.LightBlue;
                }
            });
            mMainMenuItems.Add(mi);
            menu.DropDownItems.Add(mi);
            return mi;
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

        SHORTCUT shortcut(DUMP_ID id) {
            switch (id) {
            case DUMP_ID.WIRE:
                return new SHORTCUT(Keys.F1, false);
            case DUMP_ID.RESISTOR:
                return new SHORTCUT(Keys.F2, false);
            case DUMP_ID.CAPACITOR:
                return new SHORTCUT(Keys.F3, false);
            case DUMP_ID.INDUCTOR:
                return new SHORTCUT(Keys.F4, false);
            case DUMP_ID.GROUND:
                return new SHORTCUT(Keys.F5, false);
            case DUMP_ID.INVALID:
            default:
                return new SHORTCUT();
            }
        }

        public void ComposeMainMenu(MenuStrip mainMenuBar) {
            #region File
            var fileMenuBar = new ToolStripMenuItem();
            fileMenuBar.Text = "ファイル(F)";
            fileMenuBar.Font = menuFont;
            addMenuItem(fileMenuBar, "新規作成(N)", MENU_ITEM.OPEN_NEW, new SHORTCUT(Keys.None));
            fileMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addMenuItem(fileMenuBar, "開く(O)", MENU_ITEM.OPEN_FILE, new SHORTCUT(Keys.O));
            fileMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addMenuItem(fileMenuBar, "上書き保存(S)", MENU_ITEM.OVERWRITE, new SHORTCUT(Keys.S));
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
            addMenuItem(editMenuBar, "全選択(A)", MENU_ITEM.SELECT_ALL, new SHORTCUT(Keys.A));
            addMenuItem(editMenuBar, "切り取り(T)", MENU_ITEM.CUT, new SHORTCUT(Keys.X));
            addMenuItem(editMenuBar, "コピー(C)", MENU_ITEM.COPY, new SHORTCUT(Keys.C));
            addMenuItem(editMenuBar, "貼り付け(P)", MENU_ITEM.PASTE, new SHORTCUT(Keys.V));
            addMenuItem(editMenuBar, "削除(L)", MENU_ITEM.DELETE, new SHORTCUT(Keys.Delete, false));
            editMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addMenuItem(editMenuBar, "元に戻す(U)", MENU_ITEM.UNDO, new SHORTCUT(Keys.Z));
            addMenuItem(editMenuBar, "やり直し(R)", MENU_ITEM.REDO, new SHORTCUT(Keys.Y));
            editMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addMenuItem(editMenuBar, "中心に移動(E)", MENU_ITEM.CENTER_CIRCUIT, new SHORTCUT(Keys.E));
            mainMenuBar.Items.Add(editMenuBar);
            #endregion

            mainMenuBar.Items.Add(new ToolStripSeparator());
            mainMenuBar.Items.Add(new ToolStripSeparator());

            #region Basic Element
            var basicMenuBar = new ToolStripMenuItem();
            basicMenuBar.Text = "基本(B)";
            basicMenuBar.Font = menuFont;
            tsmWire = addElementItem(basicMenuBar, "配線", ELEMENTS.WIRE);
            addElementItem(basicMenuBar, "抵抗", ELEMENTS.RESISTOR);
            addElementItem(basicMenuBar, "コンデンサ", ELEMENTS.CAPACITOR);
            addElementItem(basicMenuBar, "コイル", ELEMENTS.INDUCTOR);
            addElementItem(basicMenuBar, "接地", ELEMENTS.GROUND);
            basicMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addElementItem(basicMenuBar, "矩形", ELEMENTS.BOX);
            addElementItem(basicMenuBar, "テキスト", ELEMENTS.TEXT);
            mainMenuBar.Items.Add(basicMenuBar);
            #endregion

            #region Passive Components
            var passMenuBar = new ToolStripMenuItem();
            passMenuBar.Text = "受動素子(P)";
            passMenuBar.Font = menuFont;
            addElementItem(passMenuBar, "可変抵抗", ELEMENTS.POT);
            addElementItem(passMenuBar, "コンデンサ(有極性)", ELEMENTS.CAPACITOR_POLER);
            addElementItem(passMenuBar, "トランス", ELEMENTS.TRANSFORMER);
            mainMenuBar.Items.Add(passMenuBar);
            #endregion

            #region Active Components
            var activeMenuBar = new ToolStripMenuItem();
            activeMenuBar.Text = "能動素子(A)";
            activeMenuBar.Font = menuFont;
            addElementItem(activeMenuBar, "ダイオード", ELEMENTS.DIODE);
            addElementItem(activeMenuBar, "ツェナーダイオード", ELEMENTS.ZENER);
            addElementItem(activeMenuBar, "可変容量ダイオード ", ELEMENTS.VARACTOR);
            addElementItem(activeMenuBar, "LED", ELEMENTS.LED);
            activeMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addElementItem(activeMenuBar, "NPNトランジスタ", ELEMENTS.TRANSISTOR_N);
            addElementItem(activeMenuBar, "PNPトランジスタ", ELEMENTS.TRANSISTOR_P);
            addElementItem(activeMenuBar, "Nch MOSトランジスタ", ELEMENTS.MOSFET_N);
            addElementItem(activeMenuBar, "Pch MOSトランジスタ", ELEMENTS.MOSFET_P);
            activeMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addElementItem(activeMenuBar, "オペアンプ(-側が上)", ELEMENTS.OPAMP);
            addElementItem(activeMenuBar, "オペアンプ(+側が上)", ELEMENTS.OPAMP_SWAP);
            activeMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addElementItem(activeMenuBar, "フォトカプラ", ELEMENTS.OPTOCOUPLER);
            addElementItem(activeMenuBar, "アナログスイッチ", ELEMENTS.ANALOG_SWITCH);
            mainMenuBar.Items.Add(activeMenuBar);
            #endregion

            #region Switch
            var switchMenuBar = new ToolStripMenuItem();
            switchMenuBar.Text = "スイッチ(S)";
            switchMenuBar.Font = menuFont;
            addElementItem(switchMenuBar, "スイッチ", ELEMENTS.SWITCH);
            addElementItem(switchMenuBar, "切り替えスイッチ", ELEMENTS.SWITCH_MULTI);
            addElementItem(switchMenuBar, "プッシュスイッチ(C接点)", ELEMENTS.SWITCH_PUSH_C);
            addElementItem(switchMenuBar, "プッシュスイッチ(NC)", ELEMENTS.SWITCH_PUSH_NC);
            addElementItem(switchMenuBar, "プッシュスイッチ(NO)", ELEMENTS.SWITCH_PUSH_NO);
            mainMenuBar.Items.Add(switchMenuBar);
            #endregion

            #region Inputs and Sources
            var inputMenuBar = new ToolStripMenuItem();
            inputMenuBar.Text = "入力(I)";
            inputMenuBar.Font = menuFont;
            addElementItem(inputMenuBar, "2端子電圧源(直流)", ELEMENTS.VOLTAGE_DC);
            addElementItem(inputMenuBar, "2端子電圧源(正弦波)", ELEMENTS.VOLTAGE_SIN);
            addElementItem(inputMenuBar, "2端子電圧源(パルス)", ELEMENTS.VOLTAGE);
            inputMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addElementItem(inputMenuBar, "1端子電圧源(直流)", ELEMENTS.RAIL_DC);
            addElementItem(inputMenuBar, "1端子電圧源(正弦波)", ELEMENTS.RAIL_SIN);
            addElementItem(inputMenuBar, "1端子電圧源(パルス)", ELEMENTS.RAIL);
            inputMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addElementItem(inputMenuBar, "クロック", ELEMENTS.CLOCK);
            addElementItem(inputMenuBar, "スイープ", ELEMENTS.SWEEP);
            addElementItem(inputMenuBar, "ノイズ", ELEMENTS.NOISE);
            inputMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addElementItem(inputMenuBar, "AM発信器", ELEMENTS.OSC_AM);
            addElementItem(inputMenuBar, "FM発信器", ELEMENTS.OSC_FM);
            inputMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addElementItem(inputMenuBar, "定電流源", ELEMENTS.CURRENT);
            mainMenuBar.Items.Add(inputMenuBar);
            #endregion

            #region Outputs and Labels
            var outputMenuBar = new ToolStripMenuItem();
            outputMenuBar.Text = "計測器/出力(O)";
            outputMenuBar.Font = menuFont;
            addElementItem(outputMenuBar, "出力ピン", ELEMENTS.OUTPUT);
            outputMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addElementItem(outputMenuBar, "電圧計(2端子)", ELEMENTS.VOLTMETER2);
            addElementItem(outputMenuBar, "電圧計(1端子)", ELEMENTS.VOLTMETER1);
            addElementItem(outputMenuBar, "電流計", ELEMENTS.AMMETER);
            outputMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addElementItem(outputMenuBar, "データ出力", ELEMENTS.DATA_RECORDER);
            addElementItem(outputMenuBar, "音声ファイル出力", ELEMENTS.OUTPUT_AUDIO);
            outputMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addElementItem(outputMenuBar, "停止トリガー", ELEMENTS.STOP_TRIGGER);
            mainMenuBar.Items.Add(outputMenuBar);
            #endregion

            #region Logic Gates
            var gateMenuBar = new ToolStripMenuItem();
            gateMenuBar.Text = "論理ゲート(G)";
            gateMenuBar.Font = menuFont;
            addElementItem(gateMenuBar, "入力", ELEMENTS.LOGIC_INPUT);
            addElementItem(gateMenuBar, "出力", ELEMENTS.LOGIC_OUTPUT);
            gateMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addElementItem(gateMenuBar, "3ステートバッファ", ELEMENTS.TRISTATE);
            addElementItem(gateMenuBar, "シュミットトリガ", ELEMENTS.SCHMITT);
            addElementItem(gateMenuBar, "シュミットトリガ(NOT)", ELEMENTS.SCHMITT_INV);
            gateMenuBar.DropDownItems.Add(new ToolStripSeparator());
            addElementItem(gateMenuBar, "AND", ELEMENTS.AND_GATE);
            addElementItem(gateMenuBar, "OR", ELEMENTS.OR_GATE);
            addElementItem(gateMenuBar, "NOT", ELEMENTS.NOT_GATE);
            addElementItem(gateMenuBar, "NAND", ELEMENTS.NAND_GATE);
            addElementItem(gateMenuBar, "NOR", ELEMENTS.NOR_GATE);
            addElementItem(gateMenuBar, "XOR", ELEMENTS.XOR_GATE);
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
            //addElementItem(activeBlocMenuBar, "Add Analog Switch (SPDT)", ITEM.AnalogSwitch2Elm);
            //addElementItem(activeBlocMenuBar, "Add CCII+", ITEM.CC2Elm);
            //addElementItem(activeBlocMenuBar, "Add CCII-", ITEM.CC2NegElm);
            //addElementItem(activeBlocMenuBar, "Add OTA (LM13700 style)", ITEM.OTAElm);
            //addElementItem(activeBlocMenuBar, "Add Voltage-Controlled Voltage Source", ITEM.VCVSElm);
            //addElementItem(activeBlockMenuBar, "Add Voltage-Controlled Current Source", ELEMENTS.VCCS);
            //addElementItem(activeBlocMenuBar, "Add Current-Controlled Voltage Source", ITEM.CCVSElm);
            //addElementItem(activeBlockMenuBar, "Add Current-Controlled Current Source", ELEMENTS.CCCS);
            //addElementItem(activeBlockMenuBar, "カスタムモジュール", ELEMENTS.CustomCompositeElm);
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

        public static BaseUI ConstructElement(ELEMENTS n, Point pos = new Point()) {
            switch (n) {
            #region Passive Components
            case ELEMENTS.WIRE:
                return new Wire(pos);
            case ELEMENTS.GROUND:
                return new Ground(pos);
            case ELEMENTS.SWITCH:
                return new Switch(pos);
            case ELEMENTS.SWITCH_PUSH_C:
                return new SwitchPushC(pos);
            case ELEMENTS.SWITCH_PUSH_NC:
                return new SwitchPushNC(pos);
            case ELEMENTS.SWITCH_PUSH_NO:
                return new SwitchPushNO(pos);
            case ELEMENTS.SWITCH_MULTI:
                return new SwitchMulti(pos);
            case ELEMENTS.RESISTOR:
                return new Resistor(pos);
            case ELEMENTS.POT:
                return new Pot(pos);
            case ELEMENTS.CAPACITOR:
                return new Capacitor(pos);
            case ELEMENTS.CAPACITOR_POLER:
                return new CapacitorPolar(pos);
            case ELEMENTS.INDUCTOR:
                return new Inductor(pos);
            case ELEMENTS.TRANSFORMER:
                return new Transformer(pos);
            #endregion

            #region Active Components
            case ELEMENTS.DIODE:
                return new Diode(pos, "D");
            case ELEMENTS.ZENER:
                return new DiodeZener(pos);
            case ELEMENTS.VARACTOR:
                return new DiodeVaractor(pos);
            case ELEMENTS.LED:
                return new DiodeLED(pos);
            case ELEMENTS.TRANSISTOR_N:
                return new TransistorN(pos);
            case ELEMENTS.TRANSISTOR_P:
                return new TransistorP(pos);
            case ELEMENTS.MOSFET_N:
                return new MosfetN(pos);
            case ELEMENTS.MOSFET_P:
                return new MosfetP(pos);
            case ELEMENTS.OPAMP:
                return new OpAmp(pos);
            case ELEMENTS.OPAMP_SWAP:
                return new OpAmpSwap(pos);
            case ELEMENTS.ANALOG_SWITCH:
                return new AnalogSwitch(pos);
            case ELEMENTS.OPTOCOUPLER:
                return new Optocoupler(pos);
            #endregion

            #region Inputs and Sources
            case ELEMENTS.VOLTAGE_DC:
                return new VoltageDC(pos);
            case ELEMENTS.VOLTAGE_SIN:
                return new VoltageAC(pos);
            case ELEMENTS.VOLTAGE:
                return new Voltage(pos, Elements.Input.ElmVoltage.WAVEFORM.PULSE_MONOPOLE);
            case ELEMENTS.CURRENT:
                return new Current(pos);
            case ELEMENTS.RAIL_DC:
                return new RailDC(pos);
            case ELEMENTS.RAIL_SIN:
                return new RailAC(pos);
            case ELEMENTS.RAIL:
                return new Rail(pos, Elements.Input.ElmVoltage.WAVEFORM.PULSE_MONOPOLE);
            case ELEMENTS.CLOCK:
                return new RailClock(pos);
            case ELEMENTS.SWEEP:
                return new Sweep(pos);
            case ELEMENTS.OSC_AM:
                return new AM(pos);
            case ELEMENTS.OSC_FM:
                return new FM(pos);
            case ELEMENTS.NOISE:
                return new RailNoise(pos);
            #endregion

            #region Outputs and Labels
            case ELEMENTS.OUTPUT:
                return new LabeledNode(pos);
            case ELEMENTS.VOLTMETER2:
                return new VoltMeter(pos);
            case ELEMENTS.VOLTMETER1:
                return new VoltMeter1Term(pos);
            case ELEMENTS.AMMETER:
                return new Ammeter(pos);
            case ELEMENTS.DATA_RECORDER:
                return new DataRecorder(pos);
            case ELEMENTS.STOP_TRIGGER:
                return new StopTrigger(pos);
            case ELEMENTS.SCOPE:
                return new Scope(pos);
            #endregion

            #region Active Building Blocks
            //case ELEMENTS.OpAmpRealElm:
            //    return null; //(CircuitElm)new OpAmpRealElm(x1, y1);
            //case ELEMENTS.AnalogSwitch2Elm:
            //    return null; //(CircuitElm)new AnalogSwitch2Elm(x1, y1);
            //case ELEMENTS.CC2Elm:
            //    return null; //(CircuitElm)new CC2Elm(x1, y1);
            //case ELEMENTS.CC2NegElm:
            //    return null; //(CircuitElm)new CC2NegElm(x1, y1);
            //case ELEMENTS.ComparatorElm:
            //    return null; //new ComparatorElm(x1, y1);
            //case ELEMENTS.ComparatorSwapElm:
            //    return null; //new ComparatorSwapElm(x1, y1);
            //case ELEMENTS.OTAElm:
            //    return null; //(CircuitElm)new OTAElm(x1, y1);
            //case ELEMENTS.VCVSElm:
            //    return null; //(CircuitElm)new VCVSElm(x1, y1);
            case ELEMENTS.VCCS:
                return new VCCS(pos);
            case ELEMENTS.CCCS:
                return new CCCS(pos);
            //case ELEMENTS.CCVSElm:
            //    return null; //(CircuitElm)new CCVSElm(x1, y1);
            //case ELEMENTS.CustomCompositeElm:
            //    return new CustomCompositeElm(pos);
            #endregion

            #region Logic Gates
            case ELEMENTS.LOGIC_INPUT:
                return new LogicInput(pos);
            case ELEMENTS.LOGIC_OUTPUT:
                return new LogicOutput(pos);
            case ELEMENTS.TRISTATE:
                return new TriState(pos);
            case ELEMENTS.SCHMITT_INV:
                return new InvertingSchmitt(pos);
            case ELEMENTS.SCHMITT:
                return new Schmitt(pos);
            case ELEMENTS.NOT_GATE:
                return new Inverter(pos);
            case ELEMENTS.AND_GATE:
                return new GateAnd(pos);
            case ELEMENTS.NAND_GATE:
                return new GateNand(pos);
            case ELEMENTS.OR_GATE:
                return new GateOr(pos);
            case ELEMENTS.NOR_GATE:
                return new GateNor(pos);
            case ELEMENTS.XOR_GATE:
                return new GateXor(pos);
            #endregion

            #region Digital Chips
            //case ELEMENTS.DFlipFlopElm:
            //    return new DFlipFlopElm(pos);
            //case ELEMENTS.JKFlipFlopElm:
            //    return new JKFlipFlopElm(pos);
            //case ELEMENTS.TFlipFlopElm:
            //    return new TFlipFlopElm(pos);
            //case ELEMENTS.SevenSegElm:
            //    return null; //(CircuitElm)new SevenSegElm(x1, y1);
            //case ELEMENTS.SevenSegDecoderElm:
            //    return null; //(CircuitElm)new SevenSegDecoderElm(x1, y1);
            //case ELEMENTS.MultiplexerElm:
            //    return new MultiplexerElm(pos);
            //case ELEMENTS.DeMultiplexerElm:
            //    return new DeMultiplexerElm(pos);
            //case ELEMENTS.SipoShiftElm:
            //    return new SipoShiftElm(pos);
            //case ELEMENTS.PisoShiftElm:
            //    return new PisoShiftElm(pos);
            //case ELEMENTS.CounterElm:
            //    return new CounterElm(pos);
            ///* if you take out DecadeElm, it will break the menus and people's saved shortcuts */
            ///* if you take out RingCounterElm, it will break subcircuits */
            //case ELEMENTS.DecadeElm:
            //case ELEMENTS.RingCounterElm:
            //    return new RingCounterElm(pos);
            //case ELEMENTS.LatchElm:
            //    return new LatchElm(pos);
            //case ELEMENTS.SeqGenElm:
            //    return null; //(CircuitElm)new SeqGenElm(x1, y1);
            //case ELEMENTS.FullAdderElm:
            //    return new FullAdderElm(pos);
            //case ELEMENTS.HalfAdderElm:
            //    return new HalfAdderElm(pos);
            ///* if you take out UserDefinedLogicElm, it will break people's saved shortcuts */
            //case ELEMENTS.CUSTOM_LOGIC:
            //case ELEMENTS.UserDefinedLogicElm:
            //    return new CustomLogicElm(pos);
            //case ELEMENTS.SRAMElm:
            //    return new SRAMElm(pos);
            #endregion

            case ELEMENTS.BOX:
                return new GraphicBox(pos);
            case ELEMENTS.TEXT:
                return new GraphicText(pos);

            default:
                return null;
            }
        }

        public static BaseUI CreateCe(DUMP_ID tint, Point p1, Point p2, int f, StringTokenizer st) {
            switch (tint) {
            #region Passive Components
            case DUMP_ID.WIRE:
                return new Wire(p1, p2, f, st);
            case DUMP_ID.GROUND:
                return new Ground(p1, p2, f, st);
            case DUMP_ID.SWITCH:
                return new Switch(p1, p2, f, st);
            case DUMP_ID.SWITCH_MULTI:
                return new SwitchMulti(p1, p2, f, st);
            case DUMP_ID.RESISTOR:
                return new Resistor(p1, p2, f, st);
            case DUMP_ID.POT:
                return new Pot(p1, p2, f, st);
            case DUMP_ID.CAPACITOR:
                return new Capacitor(p1, p2, f, st);
            case DUMP_ID.CAPACITOR_POLAR:
                return new CapacitorPolar(p1, p2, f, st);
            case DUMP_ID.INDUCTOR:
                return new Inductor(p1, p2, f, st);
            case DUMP_ID.TRANSFORMER:
                return new Transformer(p1, p2, f, st);
            #endregion

            #region Active Components
            case DUMP_ID.DIODE:
                return new Diode(p1, p2, f, st);
            case DUMP_ID.ZENER:
                return new DiodeZener(p1, p2, f, st);
            case DUMP_ID.VARACTOR:
                return new DiodeVaractor(p1, p2, f, st);
            case DUMP_ID.LED:
                return new DiodeLED(p1, p2, f, st);
            case DUMP_ID.TRANSISTOR:
                return new Transistor(p1, p2, f, st);
            case DUMP_ID.MOSFET:
                return new Mosfet(p1, p2, f, st);
            case DUMP_ID.OPAMP:
                return new OpAmp(p1, p2, f, st);
            case DUMP_ID.OPTO_COUPLER:
                return new Optocoupler(p1, p2, f, st);
            case DUMP_ID.ANALOG_SW:
                return new AnalogSwitch(p1, p2, f, st);
            #endregion

            #region Inputs and Sources
            case DUMP_ID.VOLTAGE:
                return new Voltage(p1, p2, f, st);
            case DUMP_ID.DC:
                return new VoltageDC(p1, p2, f, st);
            case DUMP_ID.AC:
                return new VoltageAC(p1, p2, f, st);
            case DUMP_ID.CURRENT:
                return new Current(p1, p2, f, st);
            case DUMP_ID.RAIL:
                return new Rail(p1, p2, f, st);
            case DUMP_ID.RAIL_DC:
                return new RailDC(p1, p2, f, st);
            case DUMP_ID.RAIL_AC:
                return new RailAC(p1, p2, f, st);
            case DUMP_ID.VCCS:
                return new VCCS(p1, p2, f, st);
            case DUMP_ID.CCCS:
                return new CCCS(p1, p2, f, st);
            case DUMP_ID.SWEEP:
                return new Sweep(p1, p2, f, st);
            case DUMP_ID.AM:
                return new AM(p1, p2, f, st);
            case DUMP_ID.FM:
                return new FM(p1, p2, f, st);
            #endregion

            #region Outputs and Labels
            case DUMP_ID.VOLTMETER:
                return new VoltMeter(p1, p2, f, st);
            case DUMP_ID.VOLTMETER1:
                return new VoltMeter1Term(p1, p2, f, st);
            case DUMP_ID.AMMETER:
                return new Ammeter(p1, p2, f, st);
            case DUMP_ID.LABELED_NODE:
                return new LabeledNode(p1, p2, f, st);
            //case DUMP_ID.WAVE_OUT:
            //    return new AudioOutputElm(p1, p2, f, st);
            case DUMP_ID.DATA_RECORDER:
                return new DataRecorder(p1, p2, f, st);
            case DUMP_ID.SCOPE:
                return new Scope(p1, p2, f, st);
            case DUMP_ID.STOP_TRIGGER:
                return new StopTrigger(p1, p2, f, st);
            #endregion

            #region Logic Gates
            case DUMP_ID.LOGIC_I:
                return new LogicInput(p1, p2, f, st);
            case DUMP_ID.LOGIC_O:
                return new LogicOutput(p1, p2, f, st);
            case DUMP_ID.TRISTATE:
                return new TriState(p1, p2, f, st);
            case DUMP_ID.INVERT_SCHMITT:
                return new InvertingSchmitt(p1, p2, f, st);
            case DUMP_ID.SCHMITT:
                return new Schmitt(p1, p2, f, st);
            case DUMP_ID.AND_GATE:
                return new GateAnd(p1, p2, f, st);
            case DUMP_ID.NAND_GATE:
                return new GateNand(p1, p2, f, st);
            case DUMP_ID.OR_GATE:
                return new GateOr(p1, p2, f, st);
            case DUMP_ID.NOR_GATE:
                return new GateNor(p1, p2, f, st);
            case DUMP_ID.XOR_GATE:
                return new GateXor(p1, p2, f, st);
            case DUMP_ID.INVERT:
                return new Inverter(p1, p2, f, st);
            //case DUMP_ID.HALF_ADDER:
            //    return new HalfAdderElm(p1, p2, f, st);
            //case DUMP_ID.FULL_ADDER:
            //    return new FullAdderElm(p1, p2, f, st);
            #endregion

            #region Digital Chips
            //case DUMP_ID.FLIP_FLOP_D:
            //    return new DFlipFlopElm(p1, p2, f, st);
            //case DUMP_ID.FLIP_FLOP_JK:
            //    return new JKFlipFlopElm(p1, p2, f, st);
            //case DUMP_ID.FLIP_FLOP_T:
            //    return new TFlipFlopElm(p1, p2, f, st);
            //case DUMP_ID.SHIFT_REGISTER_PISO:
            //    return new PisoShiftElm(p1, p2, f, st);
            //case DUMP_ID.SHIFT_REGISTER_SIPO:
            //    return new SipoShiftElm(p1, p2, f, st);
            //case DUMP_ID.RING_COUNTER:
            //    return new RingCounterElm(p1, p2, f, st);
            //case DUMP_ID.COUNTER:
            //    return new CounterElm(p1, p2, f, st);
            //case DUMP_ID.LATCH:
            //    return new LatchElm(p1, p2, f, st);
            //case DUMP_ID.MULTIPLEXER:
            //    return new MultiplexerElm(p1, p2, f, st);
            //case DUMP_ID.DEMULTIPLEXER:
            //    return new DeMultiplexerElm(p1, p2, f, st);
            //case DUMP_ID.SRAM:
            //    return new SRAMElm(p1, p2, f, st);
            //case DUMP_ID.CUSTOM_LOGIC:
            //    return new CustomLogicElm(p1, p2, f, st);
            #endregion

            case DUMP_ID.BOX:
                return new GraphicBox(p1, p2, f, st);
            case DUMP_ID.TEXT:
                return new GraphicText(p1, p2, f, st);

            //case 'A': return new AntennaElm(x1, y1, x2, y2, f, st);
            //case 'j': return new JfetElm(x1, y1, x2, y2, f, st);
            //case 'm': return new MemristorElm(x1, y1, x2, y2, f, st);
            //case 'n': return new NoiseElm(x1, y1, x2, y2, f, st);
            //case 157: return new SevenSegElm(x1, y1, x2, y2, f, st);
            //case 158: return new VCOElm(x1, y1, x2, y2, f, st);
            //case 160: return new AnalogSwitch2Elm(x1, y1, x2, y2, f, st);
            //case 161: return new PhaseCompElm(x1, y1, x2, y2, f, st);
            //case 165: return new TimerElm(x1, y1, x2, y2, f, st);
            //case 166: return new DACElm(x1, y1, x2, y2, f, st);
            //case 167: return new ADCElm(x1, y1, x2, y2, f, st);
            //case 169: return new TappedTransformerElm(x1, y1, x2, y2, f, st);
            //case 171: return new TransLineElm(x1, y1, x2, y2, f, st);
            //case 173: return new TriodeElm(x1, y1, x2, y2, f, st);
            //case 175: return new TunnelDiodeElm(x1, y1, x2, y2, f, st);
            //case 177: return new SCRElm(x1, y1, x2, y2, f, st);
            //case 178: return new RelayElm(x1, y1, x2, y2, f, st);
            //case 179: return new CC2Elm(x1, y1, x2, y2, f, st);
            //case 181: return new LampElm(x1, y1, x2, y2, f, st);
            //case 187: return new SparkGapElm(x1, y1, x2, y2, f, st);
            //case 188: return new SeqGenElm(x1, y1, x2, y2, f, st);
            //case 194: return new MonostableElm(x1, y1, x2, y2, f, st);
            //case 197: return new SevenSegDecoderElm(x1, y1, x2, y2, f, st);
            //case 203: return new DiacElm(x1, y1, x2, y2, f, st);
            //case 206: return new TriacElm(x1, y1, x2, y2, f, st);
            //case 212: return new VCVSElm(x1, y1, x2, y2, f, st);
            //case 214: return new CCVSElm(x1, y1, x2, y2, f, st);
            //case 216: return new OhmMeterElm(x1, y1, x2, y2, f, st);
            //case 368: return new TestPointElm(x1, y1, x2, y2, f, st);
            //case 400: return new DarlingtonElm(x1, y1, x2, y2, f, st);
            //case DUMP_ID.COMPARATOR: return new ComparatorElm(x1, y1, x2, y2, f, st);
            //case 402: return new OTAElm(x1, y1, x2, y2, f, st);
            //case 404: return new FuseElm(x1, y1, x2, y2, f, st);
            //case 405: return new LEDArrayElm(x1, y1, x2, y2, f, st);
            //case 406: return new CustomTransformerElm(x1, y1, x2, y2, f, st);
            //case 409: return new OpAmpRealElm(x1, y1, x2, y2, f, st);
            //case DUMP_ID.CUSTOM_COMPOSITE:
            //    return new CustomCompositeElm(p1, p2, f, st);
                //case 411: return new AudioInputElm(x1, y1, x2, y2, f, st);
            }
            return null;
        }
    }
}
