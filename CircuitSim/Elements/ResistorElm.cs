using System.Drawing;

namespace Circuit.Elements {
    class ResistorElm : CircuitElm {
        public double Resistance { get; set; }

        Point ps1;
        Point ps2;
        Point ps3;
        Point ps4;
        Point textPos;

        public ResistorElm(Point pos) : base(pos) {
            Resistance = 1000;
        }

        public ResistorElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Resistance = st.nextTokenDouble();
        }

        public override DUMP_ID Shortcut { get { return DUMP_ID.RESISTOR; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.RESISTOR; } }

        protected override string dump() {
            return Resistance.ToString();
        }

        public override void SetPoints() {
            base.SetPoints();
            calcLeads(24);
            if (mPoint1.Y == mPoint2.Y) {
                Utils.InterpPoint(mPoint1, mPoint2, ref textPos, 0.5 + 10 * mDsign / mLen, 12 * mDsign);
            } else if (mPoint1.X == mPoint2.X) {
                Utils.InterpPoint(mPoint1, mPoint2, ref textPos, 0.5, -4 * mDsign);
            } else {
                Utils.InterpPoint(mPoint1, mPoint2, ref textPos, 0.5, -8 * mDsign);
            }
        }

        public override void Draw(CustomGraphics g) {
            var len = (float)Utils.Distance(mLead1, mLead2);
            if (0 == len) {
                return;
            }

            int hs = ControlPanel.ChkUseAnsiSymbols.Checked ? 5 : 4;
            setBbox(mPoint1, mPoint2, hs);

            draw2Leads(g);

            int segments = 12;
            double segf = 1.0 / segments;
            double v1 = Volts[0];
            double v2 = Volts[1];

            if (ControlPanel.ChkUseAnsiSymbols.Checked) {
                /* draw zigzag */
                int oy = 0;
                int ny;
                for (int i = 0; i != segments; i++) {
                    switch (i & 3) {
                    case 0: ny = hs; break;
                    case 2: ny = -hs; break;
                    default: ny = 0; break;
                    }
                    Utils.InterpPoint(mLead1, mLead2, ref ps1, i * segf, oy);
                    Utils.InterpPoint(mLead1, mLead2, ref ps2, (i + 1) * segf, ny);
                    double v = v1 + (v2 - v1) * i / segments;
                    g.DrawThickLine(getVoltageColor(v), ps1, ps2);
                    oy = ny;
                }
            } else {
                /* draw rectangle */
                Utils.InterpPoint(mLead1, mLead2, ref ps1, ref ps2, 0, hs);
                g.ThickLineColor = getVoltageColor(v1);
                g.DrawThickLine(ps1, ps2);
                for (int i = 0; i != segments; i++) {
                    double v = v1 + (v2 - v1) * i / segments;
                    Utils.InterpPoint(mLead1, mLead2, ref ps1, ref ps2, i * segf, hs);
                    Utils.InterpPoint(mLead1, mLead2, ref ps3, ref ps4, (i + 1) * segf, hs);
                    g.ThickLineColor = getVoltageColor(v);
                    g.DrawThickLine(ps1, ps3);
                    g.DrawThickLine(ps2, ps4);
                }
                Utils.InterpPoint(mLead1, mLead2, ref ps1, ref ps2, 1, hs);
                g.DrawThickLine(ps1, ps2);
            }

            if (ControlPanel.ChkShowValues.Checked) {
                var s = Utils.ShortUnitText(Resistance, "");
                g.DrawRightText(s, textPos.X, textPos.Y);
            }

            doDots(g);
            drawPosts(g);
        }

        protected override void calculateCurrent() {
            mCurrent = (Volts[0] - Volts[1]) / Resistance;
            /*Console.WriteLine(this + " res current set to " + current + "\n");*/
        }

        public override void Stamp() {
            mCir.StampResistor(Nodes[0], Nodes[1], Resistance);
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "resistor";
            getBasicInfo(arr);
            arr[3] = "R = " + Utils.UnitText(Resistance, CirSim.OHM_TEXT);
            arr[4] = "P = " + Utils.UnitText(Power, "W");
        }

        public override string GetScopeText(Scope.VAL v) {
            return "resistor, " + Utils.UnitText(Resistance, CirSim.OHM_TEXT);
        }

        public override ElementInfo GetElementInfo(int n) {
            /* ohmString doesn't work here on linux */
            if (n == 0) {
                return new ElementInfo("Resistance (ohms)", Resistance, 0, 0);
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (ei.Value > 0) {
                Resistance = ei.Value;
            }
        }
    }
}
