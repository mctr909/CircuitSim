using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using Circuit.Elements.Custom;

namespace Circuit.Elements.Logic {
    class SRAMElm : ChipElm {
        int address;
        int addressNodes;
        int dataNodes;
        int internalNodes;
        int addressBits;
        int dataBits;
        List<int> map;

        public SRAMElm(Point pos) : base(pos) {
            addressBits = dataBits = 4;
            map = new List<int>();
            SetupPins();
        }

        public SRAMElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) {
            map = new List<int>();
            addressBits = int.Parse(st.nextToken());
            dataBits = int.Parse(st.nextToken());
            SetupPins();
            try {
                // load contents
                // format: addr val(addr) val(addr+1) val(addr+2) ... -1 addr val val ... -1 ... -2
                while (true) {
                    int a = int.Parse(st.nextToken());
                    if (a < 0) {
                        break;
                    }
                    int v = int.Parse(st.nextToken());
                    map.Add(v);
                    while (true) {
                        v = int.Parse(st.nextToken());
                        if (v < 0) {
                            break;
                        }
                        map.Add(v);
                    }
                }
            } catch { }
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.SRAM; } }

        public override int CirPostCount { get { return 2 + addressBits + dataBits; } }

        public override int CirVoltageSourceCount { get { return dataBits; } }

        public override int CirInternalNodeCount { get { return dataBits; } }

        public override bool CirNonLinear { get { return true; } }

        public override void CirStamp() {
            for (var i = 0; i != dataBits; i++) {
                var p = pins[i + dataNodes];
                mCir.StampVoltageSource(0, CirNodes[internalNodes + i], p.voltSource);
                mCir.StampNonLinear(CirNodes[internalNodes + i]);
                mCir.StampNonLinear(CirNodes[dataNodes + i]);
            }
        }

        public override void CirDoStep() {
            var writeEnabled = CirVolts[0] < 2.5;
            var outputEnabled = (CirVolts[1] < 2.5) && !writeEnabled;

            // get address
            address = 0;
            for (var i = 0; i != addressBits; i++) {
                address |= (CirVolts[addressNodes + i] > 2.5) ? 1 << (addressBits - 1 - i) : 0;
            }

            int data;
            if (address < map.Count) {
                data = map[address];
            } else {
                data = 0;
            }
            for (var i = 0; i != dataBits; i++) {
                var p = pins[i + dataNodes];
                mCir.UpdateVoltageSource(0, CirNodes[internalNodes + i], p.voltSource, (data & (1 << (dataBits - 1 - i))) == 0 ? 0 : 5);

                // stamp resistor from internal voltage source to data pin.
                // if output enabled, make it a small resistor.  otherwise large.
                mCir.StampResistor(CirNodes[internalNodes + i], CirNodes[dataNodes + i], outputEnabled ? 1 : 1e8);
            }
        }

        public override void CirStepFinished() {
            int data = 0;
            var writeEnabled = CirVolts[0] < 2.5;
            if (!writeEnabled) {
                return;
            }

            // store data in RAM
            for (var i = 0; i != dataBits; i++) {
                data |= (CirVolts[dataNodes + i] > 2.5) ? 1 << (dataBits - 1 - i) : 0;
            }
            if (address < map.Count) {
                map[address] = data;
            } else {
                map.Add(data);
            }
        }

        protected override string dump() {
            var s = addressBits + " " + dataBits;

            // dump contents
            int maxI = 1 << addressBits;
            int i;
            for (i = 0; i < maxI; i++) {
                if (map.Count <= i) {
                    continue;
                }
                var val = map[i];
                s += " " + i + " " + val;
                while (true) {
                    if (map.Count <= ++i) {
                        break;
                    }
                    val = map[i];
                    s += " " + val;
                }
                s += " -1";
            }
            s += " -2";
            return s;
        }

        string getChipName() { return "Static RAM"; }

        public override void SetupPins() {
            sizeX = 2;
            sizeY = Math.Max(addressBits, dataBits) + 1;
            pins = new Pin[CirPostCount];
            pins[0] = new Pin(this, 0, SIDE_W, "WE");
            pins[0].lineOver = true;
            pins[1] = new Pin(this, 0, SIDE_E, "OE");
            pins[1].lineOver = true;

            addressNodes = 2;
            dataNodes = 2 + addressBits;
            internalNodes = 2 + addressBits + dataBits;
            for (var i = 0; i != addressBits; i++) {
                var ii = i + addressNodes;
                pins[ii] = new Pin(this, sizeY - addressBits + i, SIDE_W, "A" + (addressBits - i - 1));
            }
            for (var i = 0; i != dataBits; i++) {
                var ii = i + dataNodes;
                pins[ii] = new Pin(this, sizeY - dataBits + i, SIDE_E, "D" + (dataBits - i - 1));
                pins[ii].output = true;
            }
            cirAllocNodes();
        }

        public override void CirReset() {
            base.CirReset();
            CirVolts[2] = 5;
            pins[2].value = true;
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 2) {
                return new ElementInfo("# of Address Bits", addressBits, 1, 1).SetDimensionless();
            }
            if (n == 3) {
                return new ElementInfo("# of Data Bits", dataBits, 1, 1).SetDimensionless();
            }
            if (n == 4) {
                var ei = new ElementInfo("Contents", 0);
                ei.TextArea = new TextBox();
                ei.TextArea.Multiline = true;
                ei.TextArea.Width = 200;
                ei.TextArea.Height = 200;
                ei.TextArea.ScrollBars = ScrollBars.Vertical;
                var s = "";
                int i;
                int maxI = 1 << addressBits;
                for (i = 0; i < maxI; i++) {
                    if (map.Count <= i) {
                        continue;
                    }
                    var val = map[i];
                    s += i + ": " + val;
                    int ct = 1;
                    while (true) {
                        if (map.Count <= ++i) {
                            break;
                        }
                        val = map[i];
                        s += " " + val;
                        if (++ct == 8) {
                            break;
                        }
                    }
                    s += "\r\n";
                    //    	    	    sim.console("got " + i + " " + s);
                }
                ei.TextArea.Text = s;
                return ei;
            }
            return base.GetElementInfo(n);
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n < 2) {
                base.SetElementValue(n, ei);
            }
            if (n == 2 && ei.Value >= 2 && ei.Value <= 16) {
                addressBits = (int)ei.Value;
                SetupPins();
                SetPoints();
            }
            if (n == 3 && ei.Value >= 2 && ei.Value <= 16) {
                dataBits = (int)ei.Value;
                SetupPins();
                SetPoints();
            }
            if (n == 4) {
                var s = ei.TextArea.Text;
                var lines = s.Split('\n');
                int i;
                map.Clear();
                for (i = 0; i != lines.Length; i++) {
                    try {
                        var line = lines[i];
                        var args = line.Split(": *".ToCharArray());
                        var addr = int.Parse(args[0]);
                        var vals = args[1].Split(" +".ToCharArray());
                        for (var j = 0; j != vals.Length; j++) {
                            var val = int.Parse(vals[j]);
                            if (addr < map.Count) {
                                map[addr] = val;
                            } else {
                                map.Add(val);
                                addr++;
                            }
                        }
                    } catch { }
                }
            }
        }
    }
}
