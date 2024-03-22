using Circuit.Symbol.Active;
using Circuit.Symbol.Custom;
using Circuit.Symbol.Logic;
using Circuit.Symbol.Input;
using Circuit.Symbol.Measure;
using Circuit.Symbol.Passive;

namespace Circuit {
	public enum DUMP_ID {
		INVALID = 0,
		BOX,
		TEXT,

		#region Passive Components
		WIRE,
		GROUND,
		SWITCH,
		SWITCH_MULTI,
		SWITCH_PUSH_C,
		SWITCH_PUSH_NC,
		SWITCH_PUSH_NO,
		RESISTOR,
		POT,
		CAPACITOR,
		CAPACITOR_POLAR,
		INDUCTOR,
		TRANSFORMER,
		INPUT_TERMINAL,
		OUTPUT_TERMINAL,
		#endregion

		#region Active Components
		DIODE,
		ZENER,
		VARACTOR,
		LED,
		TRANSISTOR,
		TRANSISTOR_N,
		TRANSISTOR_P,
		MOSFET,
		MOSFET_N,
		MOSFET_P,
		JFET,
		JFET_N,
		JFET_P,
		OPAMP,
		OPAMP_SWAP,
		ANALOG_SW,
		OPTO_COUPLER,
		#endregion

		#region Inputs and Sources
		CURRENT,
		VOLTAGE,
		DC,
		AC,

		RAIL,
		RAIL_DC,
		RAIL_AC,
		CLOCK,
		SWEEP,
		NOISE,
		AM,
		FM,

		CCCS,
		VCCS,
		#endregion

		#region Outputs and Labels
		AMMETER,
		VOLTMETER,
		VOLTMETER1,
		SCOPE,
		LABELED_NODE,
		DATA_RECORDER,
		STOP_TRIGGER,
		#endregion

		#region Logic Gates
		LOGIC_I,
		LOGIC_O,
		INVERT,
		AND_GATE,
		NAND_GATE,
		OR_GATE,
		NOR_GATE,
		XOR_GATE,
		#endregion

		#region Digital Chips
		FLIP_FLOP_D,
		FLIP_FLOP_JK,
		FLIP_FLOP_T,
		RING_COUNTER,
		COUNTER,
		LATCH,
		TRISTATE,
		SCHMITT,
		INVERT_SCHMITT,
		MULTIPLEXER,
		DEMULTIPLEXER,
		SHIFT_REGISTER_PISO,
		SHIFT_REGISTER_SIPO,
		HALF_ADDER,
		FULL_ADDER,
		SRAM,
		CUSTOM_LOGIC,
		#endregion
	}

	public class SymbolMenu {
		public static BaseSymbol Construct(DUMP_ID id, Point pos = new Point()) {
			switch (id) {
			#region Passive Components
			case DUMP_ID.WIRE:
				return new Wire(pos);
			case DUMP_ID.GROUND:
				return new Ground(pos);
			case DUMP_ID.SWITCH:
				return new Switch(pos);
			case DUMP_ID.SWITCH_PUSH_C:
				return new SwitchPushC(pos);
			case DUMP_ID.SWITCH_PUSH_NC:
				return new SwitchPushNC(pos);
			case DUMP_ID.SWITCH_PUSH_NO:
				return new SwitchPushNO(pos);
			case DUMP_ID.SWITCH_MULTI:
				return new SwitchMulti(pos);
			case DUMP_ID.RESISTOR:
				return new Resistor(pos);
			case DUMP_ID.POT:
				return new Pot(pos);
			case DUMP_ID.CAPACITOR:
				return new Capacitor(pos);
			case DUMP_ID.CAPACITOR_POLAR:
				return new CapacitorPolar(pos);
			case DUMP_ID.INDUCTOR:
				return new Inductor(pos);
			case DUMP_ID.TRANSFORMER:
				return new Transformer(pos);
			case DUMP_ID.INPUT_TERMINAL:
				return new InputTerminal(pos);
			case DUMP_ID.OUTPUT_TERMINAL:
				return new OutputTerminal(pos);
			#endregion

			#region Active Components
			case DUMP_ID.DIODE:
				return new Diode(pos);
			case DUMP_ID.ZENER:
				return new DiodeZener(pos);
			case DUMP_ID.VARACTOR:
				return new DiodeVaractor(pos);
			case DUMP_ID.LED:
				return new DiodeLED(pos);
			case DUMP_ID.TRANSISTOR_N:
				return new TransistorN(pos);
			case DUMP_ID.TRANSISTOR_P:
				return new TransistorP(pos);
			case DUMP_ID.MOSFET_N:
				return new MosfetN(pos);
			case DUMP_ID.MOSFET_P:
				return new MosfetP(pos);
			case DUMP_ID.JFET_N:
				return new JfetN(pos);
			case DUMP_ID.JFET_P:
				return new JfetP(pos);
			case DUMP_ID.OPAMP:
				return new OpAmp(pos);
			case DUMP_ID.OPAMP_SWAP:
				return new OpAmpSwap(pos);
			case DUMP_ID.ANALOG_SW:
				return new AnalogSwitch(pos);
			case DUMP_ID.OPTO_COUPLER:
				return new Optocoupler(pos);
			#endregion

			#region Inputs and Sources
			case DUMP_ID.VOLTAGE:
				return new Voltage(pos, Elements.Input.ElmVoltage.WAVEFORM.TRIANGLE);
			case DUMP_ID.DC:
				return new VoltageDC(pos);
			case DUMP_ID.AC:
				return new VoltageAC(pos);
			case DUMP_ID.RAIL:
				return new Rail(pos, Elements.Input.ElmVoltage.WAVEFORM.TRIANGLE);
			case DUMP_ID.RAIL_DC:
				return new RailDC(pos);
			case DUMP_ID.RAIL_AC:
				return new RailAC(pos);
			case DUMP_ID.CLOCK:
				return new RailClock(pos);
			case DUMP_ID.NOISE:
				return new RailNoise(pos);
			case DUMP_ID.SWEEP:
				return new Sweep(pos);
			case DUMP_ID.AM:
				return new AM(pos);
			case DUMP_ID.FM:
				return new FM(pos);
			case DUMP_ID.CURRENT:
				return new Current(pos);
			#endregion

			#region Outputs and Labels
			case DUMP_ID.VOLTMETER:
				return new VoltMeter(pos);
			case DUMP_ID.VOLTMETER1:
				return new VoltMeter1Term(pos);
			case DUMP_ID.AMMETER:
				return new Ammeter(pos);
			case DUMP_ID.DATA_RECORDER:
				return new DataRecorder(pos);
			case DUMP_ID.STOP_TRIGGER:
				return new StopTrigger(pos);
			case DUMP_ID.SCOPE:
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
			case DUMP_ID.VCCS:
				return new VCCS(pos);
			case DUMP_ID.CCCS:
				return new CCCS(pos);
			//case ELEMENTS.CCVSElm:
			//    return null; //(CircuitElm)new CCVSElm(x1, y1);
			#endregion

			#region Logic Gates
			case DUMP_ID.LOGIC_I:
				return new LogicInput(pos);
			case DUMP_ID.LOGIC_O:
				return new LogicOutput(pos);
			case DUMP_ID.TRISTATE:
				return new TriState(pos);
			case DUMP_ID.INVERT_SCHMITT:
				return new InvertingSchmitt(pos);
			case DUMP_ID.SCHMITT:
				return new Schmitt(pos);
			case DUMP_ID.INVERT:
				return new Inverter(pos);
			case DUMP_ID.AND_GATE:
				return new GateAnd(pos);
			case DUMP_ID.NAND_GATE:
				return new GateNand(pos);
			case DUMP_ID.OR_GATE:
				return new GateOr(pos);
			case DUMP_ID.NOR_GATE:
				return new GateNor(pos);
			case DUMP_ID.XOR_GATE:
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

			case DUMP_ID.BOX:
				return new GraphicBox(pos);
			case DUMP_ID.TEXT:
				return new GraphicText(pos);

			default:
				return null;
			}
		}

		public static BaseSymbol Construct(DUMP_ID id, Point p1, Point p2, int f, StringTokenizer st) {
			switch (id) {
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
			case DUMP_ID.INPUT_TERMINAL:
				return new InputTerminal(p1, p2, f, st);
			case DUMP_ID.OUTPUT_TERMINAL:
				return new OutputTerminal(p1, p2, f, st);
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
			case DUMP_ID.JFET:
				return new FET(p1, p2, false, f, st);
			case DUMP_ID.MOSFET:
				return new FET(p1, p2, true, f, st);
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
				return new OutputTerminal(p1, p2, f, st);
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
				//case 411: return new AudioInputElm(x1, y1, x2, y2, f, st);
			}
			return null;
		}
	}
}
