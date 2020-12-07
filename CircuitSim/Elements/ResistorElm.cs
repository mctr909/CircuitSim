using System.Drawing;

namespace Circuit.Elements {
    class ResistorElm : CircuitElm {
        public double Resistance { get; set; }

        Point ps1;
        Point ps2;
        Point ps3;
        Point ps4;
        Point textPos;

        public ResistorElm(int xx, int yy) : base(xx, yy) {
            Resistance = 1000;
        }

        public ResistorElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
            Resistance = st.nextTokenDouble();
        }

        public override DUMP_ID Shortcut { get { return DUMP_ID.RESISTOR; } }

        protected override string dump() {
            return Resistance.ToString();
        }

        protected override DUMP_ID getDumpType() { return DUMP_ID.RESISTOR; }

        public override void SetPoints() {
            base.SetPoints();
            calcLeads(32);
            if (mPoint1.Y == mPoint2.Y) {
                textPos = Utils.InterpPoint(mPoint1, mPoint2, 0.5 + 10 * mDsign / mLen, 12 * mDsign);
            } else {
                textPos = Utils.InterpPoint(mPoint1, mPoint2, 0.5, -10 * mDsign);
            }
        }

        public override void Draw(CustomGraphics g) {
            var len = (float)Utils.Distance(mLead1, mLead2);
            if (0 == len) {
                return;
            }

            const int hs = 5;
            setBbox(mPoint1, mPoint2, hs);

            draw2Leads(g);

            int segments = 12;
            double segf = 1.0 / segments;
            double v1 = Volts[0];
            double v2 = Volts[1];

            if (Sim.chkAnsiResistorCheckItem.Checked) {
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

            if (Sim.chkShowValuesCheckItem.Checked) {
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
            arr[3] = "R = " + Utils.UnitText(Resistance, CirSim.ohmString);
            arr[4] = "P = " + Utils.UnitText(Power, "W");
        }

        public override string GetScopeText(int v) {
            return "resistor, " + Utils.UnitText(Resistance, CirSim.ohmString);
        }

        public override EditInfo GetEditInfo(int n) {
            /* ohmString doesn't work here on linux */
            if (n == 0) {
                return new EditInfo("Resistance (ohms)", Resistance, 0, 0);
            }
            return null;
        }

        public override void SetEditValue(int n, EditInfo ei) {
            if (ei.Value > 0) {
                Resistance = ei.Value;
            }
        }
    }
}
