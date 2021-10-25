using System.Drawing;

namespace Circuit.Elements.Passive {
    class ResistorElm : CircuitElm {
        const int BODY_LEN = 24;
        const int SEGMENTS = 12;
        const int ANSI_HEIGHT = 5;
        const int EU_HEIGHT = 4;
        const double SEG_F = 1.0 / SEGMENTS;

        Point[] mP1;
        Point[] mP2;
        Point[] mRect1;
        Point[] mRect2;
        Point[] mRect3;
        Point[] mRect4;

        public ResistorElm(Point pos) : base(pos) {
            Resistance = 1000;
            ReferenceName = "R";
        }

        public ResistorElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            try {
                Resistance = st.nextTokenDouble();
                ReferenceName = st.nextToken();
            } catch { }
        }

        public double Resistance { get; set; }

        public override DUMP_ID Shortcut { get { return DUMP_ID.RESISTOR; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.RESISTOR; } }

        protected override string dump() {
            return Resistance + " " + ReferenceName;
        }

        protected override void calculateCurrent() {
            mCurrent = (Volts[0] - Volts[1]) / Resistance;
        }

        public override void Stamp() {
            mCir.StampResistor(Nodes[0], Nodes[1], Resistance);
        }

        public override void SetPoints() {
            base.SetPoints();
            calcLeads(BODY_LEN);
            setTextPos();
            setPoly();
        }

        void setTextPos() {
            mNameV = mPoint1.X == mPoint2.X;
            if (mPoint1.Y == mPoint2.Y) {
                var wv = Context.GetTextSize(Utils.UnitText(Resistance, "")).Width * 0.5;
                var wn = Context.GetTextSize(ReferenceName).Width * 0.5;
                interpPoint(ref mValuePos, 0.5 - wv / mLen * mDsign, -13 * mDsign);
                interpPoint(ref mNamePos, 0.5 + wn / mLen * mDsign, 10 * mDsign);
            } else if (mNameV) {
                interpPoint(ref mValuePos, 0.5, 2 * mDsign);
                interpPoint(ref mNamePos, 0.5, -20 * mDsign);
            } else {
                interpPoint(ref mValuePos, 0.5, 10 * mDsign);
                interpPoint(ref mNamePos, 0.5, -10 * mDsign);
            }
        }

        void setPoly() {
            /* zigzag */
            mP1 = new Point[SEGMENTS];
            mP2 = new Point[SEGMENTS];
            int oy = 0;
            int ny;
            for (int i = 0; i != SEGMENTS; i++) {
                switch (i & 3) {
                case 0:
                    ny = ANSI_HEIGHT;
                    break;
                case 2:
                    ny = -ANSI_HEIGHT;
                    break;
                default:
                    ny = 0;
                    break;
                }
                interpLead(ref mP1[i], i * SEG_F, oy);
                interpLead(ref mP2[i], (i + 1) * SEG_F, ny);
                oy = ny;
            }

            /* rectangle */
            mRect1 = new Point[SEGMENTS + 2];
            mRect2 = new Point[SEGMENTS + 2];
            mRect3 = new Point[SEGMENTS + 2];
            mRect4 = new Point[SEGMENTS + 2];
            interpLeadAB(ref mRect1[0], ref mRect2[0], 0, EU_HEIGHT);
            for (int i = 0, j = 1; i != SEGMENTS; i++, j++) {
                interpLeadAB(ref mRect1[j], ref mRect2[j], i * SEG_F, EU_HEIGHT);
                interpLeadAB(ref mRect3[j], ref mRect4[j], (i + 1) * SEG_F, EU_HEIGHT);
            }
            interpLeadAB(ref mRect1[SEGMENTS + 1], ref mRect2[SEGMENTS + 1], 1, EU_HEIGHT);
        }

        public override void Draw(CustomGraphics g) {
            var len = (float)Utils.Distance(mLead1, mLead2);
            if (0 == len) {
                return;
            }

            int hs = ControlPanel.ChkUseAnsiSymbols.Checked ? ANSI_HEIGHT : EU_HEIGHT;
            setBbox(mPoint1, mPoint2, hs);

            draw2Leads();

            double v1 = Volts[0];
            double v2 = Volts[1];

            if (ControlPanel.ChkUseAnsiSymbols.Checked) {
                /* draw zigzag */
                for (int i = 0; i < SEGMENTS; i++) {
                    double v = v1 + (v2 - v1) * i / SEGMENTS;
                    g.LineColor = getVoltageColor(v);
                    g.DrawLine(mP1[i], mP2[i]);
                }
            } else {
                /* draw rectangle */
                g.LineColor = getVoltageColor(v1);
                g.DrawLine(mRect1[0], mRect2[0]);
                for (int i = 0, j = 1; i < SEGMENTS; i++, j++) {
                    double v = v1 + (v2 - v1) * i / SEGMENTS;
                    g.LineColor = getVoltageColor(v);
                    g.DrawLine(mRect1[j], mRect3[j]);
                    g.DrawLine(mRect2[j], mRect4[j]);
                }
                g.DrawLine(mRect1[SEGMENTS + 1], mRect2[SEGMENTS + 1]);
            }

            drawValue(Resistance);
            drawName();

            doDots();
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            arr[0] = string.IsNullOrEmpty(ReferenceName) ? "抵抗" : ReferenceName;
            getBasicInfo(arr);
            arr[3] = "R = " + Utils.UnitText(Resistance, CirSim.OHM_TEXT);
            arr[4] = "P = " + Utils.UnitText(Power, "W");
        }

        public override string GetScopeText(Scope.VAL v) {
            return "resistor, " + Utils.UnitText(Resistance, CirSim.OHM_TEXT);
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                return new ElementInfo("レジスタンス(Ω)", Resistance, 0, 0);
            }
            if (n == 1) {
                var ei = new ElementInfo("名前", 0, 0, 0);
                ei.Text = ReferenceName;
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 0 && 0 < ei.Value) {
                Resistance = ei.Value;
                setTextPos();
            }
            if (n == 1) {
                ReferenceName = ei.Textf.Text;
                setTextPos();
            }
        }
    }
}
