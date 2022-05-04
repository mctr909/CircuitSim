using System.Drawing;

namespace Circuit.Elements.Passive {
    class CapacitorElm : CircuitElm {
        public static readonly int FLAG_BACK_EULER = 2;

        const int BODY_LEN = 6;
        const int HS = 6;

        double mCompResistance;
        double mVoltDiff;
        double mCurSourceValue;

        Point[] mPlate1;
        Point[] mPlate2;

        public CapacitorElm(Point pos) : base(pos) {
            Capacitance = 1e-5;
            ReferenceName = "C";
        }

        public CapacitorElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            try {
                Capacitance = st.nextTokenDouble();
                mVoltDiff = st.nextTokenDouble();
                ReferenceName = st.nextToken();
            } catch { }
        }

        public double Capacitance { get; set; }

        public override DUMP_ID Shortcut { get { return DUMP_ID.CAPACITOR; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.CAPACITOR; } }

        protected override string dump() {
            return Capacitance + " " + mVoltDiff + " " + ReferenceName;
        }

        protected override void cirCalculateCurrent() {
            double voltdiff = CirVolts[0] - CirVolts[1];
            if (CirSim.Sim.DcAnalysisFlag) {
                mCirCurrent = voltdiff / 1e8;
                return;
            }
            /* we check compResistance because this might get called
             * before stamp(), which sets compResistance, causing
             * infinite current */
            if (0 < mCompResistance) {
                mCirCurrent = voltdiff / mCompResistance + mCurSourceValue;
            }
        }

        public override void CirShorted() {
            base.CirReset();
            mVoltDiff = mCirCurrent = mCirCurCount = mCurSourceValue = 0;
        }

        public override void CirSetNodeVoltage(int n, double c) {
            base.CirSetNodeVoltage(n, c);
            mVoltDiff = CirVolts[0] - CirVolts[1];
        }

        public override void CirStamp() {
            if (CirSim.Sim.DcAnalysisFlag) {
                /* when finding DC operating point, replace cap with a 100M resistor */
                mCir.StampResistor(CirNodes[0], CirNodes[1], 1e8);
                mCurSourceValue = 0;
                return;
            }

            mCompResistance = ControlPanel.TimeStep / (2 * Capacitance);

            mCir.StampResistor(CirNodes[0], CirNodes[1], mCompResistance);
            mCir.StampRightSide(CirNodes[0]);
            mCir.StampRightSide(CirNodes[1]);
        }

        public override void CirStartIteration() {
            mCurSourceValue = -mVoltDiff / mCompResistance - mCirCurrent;
        }

        public override void CirDoStep() {
            if (CirSim.Sim.DcAnalysisFlag) {
                return;
            }
            mCir.StampCurrentSource(CirNodes[0], CirNodes[1], mCurSourceValue);
        }

        public override void CirReset() {
            base.CirReset();
            mCirCurrent = mCirCurCount = mCurSourceValue = 0;
            /* put small charge on caps when reset to start oscillators */
            mVoltDiff = 1e-3;
        }

        public override void SetPoints() {
            base.SetPoints();
            double f = (mLen - BODY_LEN) * 0.5 / mLen;
            /* calc leads */
            setLead1(f);
            setLead2(1 - f);
            /* calc plates */
            mPlate1 = new Point[2];
            mPlate2 = new Point[2];
            interpPointAB(ref mPlate1[0], ref mPlate1[1], f, HS);
            interpPointAB(ref mPlate2[0], ref mPlate2[1], 1 - f, HS);
            setTextPos();
        }

        void setTextPos() {
            mNameV = mPoint1.X == mPoint2.X;
            if (mPoint1.Y == mPoint2.Y) {
                var wv = Context.GetTextSize(Utils.UnitText(Capacitance, "")).Width * 0.5;
                var wn = Context.GetTextSize(ReferenceName).Width * 0.5;
                interpPoint(ref mValuePos, 0.5 - wv / mLen * mDsign, -12 * mDsign);
                interpPoint(ref mNamePos, 0.5 + wn / mLen * mDsign, 11 * mDsign);
            } else if (mNameV) {
                interpPoint(ref mValuePos, 0.5, 3 * mDsign);
                interpPoint(ref mNamePos, 0.5, -20 * mDsign);
            } else {
                interpPoint(ref mValuePos, 0.5, 8 * mDsign);
                interpPoint(ref mNamePos, 0.5, -8 * mDsign);
            }
        }

        public override void Draw(CustomGraphics g) {
            setBbox(mPoint1, mPoint2, HS);

            /* draw first lead and plate */
            drawLead(mPoint1, mLead1);
            drawLead(mPlate1[0], mPlate1[1]);
            /* draw second lead and plate */
            drawLead(mPoint2, mLead2);
            drawLead(mPlate2[0], mPlate2[1]);

            cirUpdateDotCount();
            if (CirSim.Sim.DragElm != this) {
                drawDots(mPoint1, mLead1, mCirCurCount);
                drawDots(mPoint2, mLead2, -mCirCurCount);
            }
            drawPosts();

            drawValue(Capacitance);
            drawName();
        }

        public override void GetInfo(string[] arr) {
            arr[0] = string.IsNullOrEmpty(ReferenceName) ? "コンデンサ" : ReferenceName;
            getBasicInfo(arr);
            arr[3] = "C = " + Utils.UnitText(Capacitance, "F");
            arr[4] = "P = " + Utils.UnitText(CirPower, "W");
        }

        public override string GetScopeText(Scope.VAL v) {
            base.GetScopeText(v);
            return "capacitor, " + Utils.UnitText(Capacitance, "F");
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                return new ElementInfo("静電容量(F)", Capacitance, 0, 0);
            }
            if (n == 1) {
                var ei = new ElementInfo("名前", 0, 0, 0);
                ei.Text = ReferenceName;
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 0 && ei.Value > 0) {
                Capacitance = ei.Value;
                setTextPos();
            }
            if (n == 1) {
                ReferenceName = ei.Textf.Text;
                setTextPos();
            }
        }
    }
}
