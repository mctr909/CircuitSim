using System.Drawing;

using Circuit.Elements.Custom;

namespace Circuit.Elements.Passive {
    class CrystalElm : CompositeElm {
        static readonly int[] EXTERNAL_NODES = { 1, 2 };
        static readonly string MODEL_STRING
            = ELEMENTS.CAPACITOR + " 1 2\r"
            + ELEMENTS.CAPACITOR + " 1 3\r"
            + ELEMENTS.INDUCTOR + " 3 4\r"
            + ELEMENTS.RESISTOR + " 4 2";

        const int HS = 12;

        double mSeriesCapacitance;
        double mParallelCapacitance;
        double mInductance;
        double mResistance;
        Point[] mPlate1;
        Point[] mPlate2;
        Point[] mSandwichPoints;
        Point mNamePos;
        string mReferenceName = "X";

        public override bool CanViewInScope { get { return true; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.CRYSTAL; } }

        public CrystalElm(Point pos) : base(pos, MODEL_STRING, EXTERNAL_NODES) {
            mParallelCapacitance = 28.7e-12;
            mSeriesCapacitance = 0.1e-12;
            mInductance = 2.5e-3;
            mResistance = 6.4;
            initCrystal();
        }

        public CrystalElm(Point a, Point b, int f, StringTokenizer st) : base(a, b, f, st, MODEL_STRING, EXTERNAL_NODES) {
            var c1 = (CapacitorElm)compElmList[0];
            mParallelCapacitance = c1.Capacitance;
            var c2 = (CapacitorElm)compElmList[1];
            mSeriesCapacitance = c2.Capacitance;
            var i1 = (InductorElm)compElmList[2];
            mInductance = i1.Inductance;
            var r1 = (ResistorElm)compElmList[3];
            mResistance = r1.Resistance;
            initCrystal();
        }

        void initCrystal() {
            var c1 = (CapacitorElm)compElmList[0];
            c1.Capacitance = mParallelCapacitance;
            var c2 = (CapacitorElm)compElmList[1];
            c2.Capacitance = mSeriesCapacitance;
            var i1 = (InductorElm)compElmList[2];
            i1.Inductance = mInductance;
            var r1 = (ResistorElm)compElmList[3];
            r1.Resistance = mResistance;
        }

        protected override void calculateCurrent() {
            mCurrent = GetCurrentIntoNode(1);
        }

        public override void SetPoints() {
            base.SetPoints();
            double f = (mLen / 2 - 6) / mLen;

            // calc leads
            setLead1(f);
            setLead2(1 - f);

            // calc plates
            mPlate1 = new Point[2];
            mPlate2 = new Point[2];
            interpPointAB(ref mPlate1[0], ref mPlate1[1], f, 6);
            interpPointAB(ref mPlate2[0], ref mPlate2[1], 1 - f, 6);

            double f2 = (mLen / 2 - 3) / mLen;
            mSandwichPoints = new Point[4];
            interpPointAB(ref mSandwichPoints[0], ref mSandwichPoints[1], f2, 8);
            interpPointAB(ref mSandwichPoints[3], ref mSandwichPoints[2], 1 - f2, 8);

            // need to do this explicitly for CompositeElms
            setPost(0, mPoint1);
            setPost(1, mPoint2);

            setTextPos();
        }

        void setTextPos() {
            if (mPoint1.Y == mPoint2.Y) {
                var wn = Context.GetTextSize(mReferenceName).Width * 0.5;
                interpPoint(ref mNamePos, 0.5 - wn / mLen * mDsign, -16 * mDsign);
            } else if (mPoint1.X == mPoint2.X) {
                interpPoint(ref mNamePos, 0.5, 8 * mDsign);
            } else {
                interpPoint(ref mNamePos, 0.5, 12 * mDsign);
            }
        }

        public override void Draw(CustomGraphics g) {
            setBbox(mPoint1, mPoint2, HS);

            // draw first lead and plate
            drawVoltage(0, mPoint1, mLead1);
            g.DrawLine(mPlate1[0], mPlate1[1]);

            // draw second lead and plate
            drawVoltage(1, mPoint2, mLead2);
            g.DrawLine(mPlate2[0], mPlate2[1]);

            g.LineColor = getVoltageColor(0.5 * (Volts[0] + Volts[1]));
            for (int i = 0; i != 4; i++) {
                g.DrawLine(mSandwichPoints[i], mSandwichPoints[(i + 1) % 4]);
            }

            updateDotCount();
            if (CirSim.Sim.DragElm != this) {
                drawDots(mPoint1, mLead1, mCurCount);
                drawDots(mPoint2, mLead2, -mCurCount);
            }
            drawPosts();

            if (ControlPanel.ChkShowName.Checked) {
                g.DrawLeftText(mReferenceName, mNamePos.X, mNamePos.Y);
            }
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "crystal";
            getBasicInfo(arr);
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                return new ElementInfo(ElementInfo.MakeLink("crystal.html", "並列静電容量(F)"), mParallelCapacitance);
            }
            if (n == 1) {
                return new ElementInfo("直列静電容量(F)", mSeriesCapacitance);
            }
            if (n == 2) {
                return new ElementInfo("インダクタンス(H)", mInductance, 0, 0);
            }
            if (n == 3) {
                return new ElementInfo("レジスタンス(Ω)", mResistance, 0, 0);
            }
            if (n == 4) {
                var ei = new ElementInfo("名前", 0, 0, 0);
                ei.Text = mReferenceName;
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 0 && 0 < ei.Value) {
                mParallelCapacitance = ei.Value;
            }
            if (n == 1 && 0 < ei.Value) {
                mSeriesCapacitance = ei.Value;
            }
            if (n == 2 && 0 < ei.Value) {
                mInductance = ei.Value;
            }
            if (n == 3 && 0 < ei.Value) {
                mResistance = ei.Value;
            }
            if (n == 4) {
                mReferenceName = ei.Textf.Text;
                setTextPos();
            }
            initCrystal();
        }
    }
}
