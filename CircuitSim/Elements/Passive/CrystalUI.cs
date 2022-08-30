using System.Drawing;

using Circuit.Elements.Custom;

namespace Circuit.Elements.Passive {
    class CrystalUI : CompositeUI {
        const int HS = 12;

        Point[] mPlate1;
        Point[] mPlate2;
        Point[] mSandwichPoints;

        public override bool CanViewInScope { get { return true; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.CRYSTAL; } }

        public CrystalUI(Point pos) : base(pos) {
            Elm = new CrystalElm();
            mPosts = new Point[((CrystalElm)Elm).NumPosts];
            DumpInfo.ReferenceName = "X";
        }

        public CrystalUI(Point a, Point b, int f, StringTokenizer st) : base(a, b, f) {
            Elm = new CrystalElm(st);
            mPosts = new Point[((CrystalElm)Elm).NumPosts];
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
            setPost(0, mPost1);
            setPost(1, mPost2);

            setTextPos();
        }

        void setTextPos() {
            mNameV = mPost1.X == mPost2.X;
            mNameH = mPost1.Y == mPost2.Y;
            if (mNameH) {
                interpPoint(ref mNamePos, 0.5, 16 * mDsign);
            } else if (mNameV) {
                interpPoint(ref mNamePos, 0.5, -23 * mDsign);
            } else {
                interpPoint(ref mNamePos, 0.5, -12 * mDsign);
            }
        }

        public override void Draw(CustomGraphics g) {
            var ce = (CrystalElm)Elm;
            setBbox(mPost1, mPost2, HS);

            // draw first lead and plate
            drawLead(mPost1, mLead1);
            drawLead(mPlate1[0], mPlate1[1]);

            // draw second lead and plate
            drawLead(mPost2, mLead2);
            drawLead(mPlate2[0], mPlate2[1]);

            for (int i = 0; i != 4; i++) {
                drawLead(mSandwichPoints[i], mSandwichPoints[(i + 1) % 4]);
            }

            updateDotCount();
            if (CirSimForm.Sim.DragElm != this) {
                drawDots(mPost1, mLead1, ce.CurCount);
                drawDots(mPost2, mLead2, -ce.CurCount);
            }
            drawPosts();

            drawName();
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "crystal";
            getBasicInfo(arr);
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var ce = (CrystalElm)Elm;
            if (c != 0) {
                return null;
            }
            if (r == 0) {
                return new ElementInfo("並列静電容量(F)", ce.ParallelCapacitance);
            }
            if (r == 1) {
                return new ElementInfo("直列静電容量(F)", ce.SeriesCapacitance);
            }
            if (r == 2) {
                return new ElementInfo("インダクタンス(H)", ce.Inductance, 0, 0);
            }
            if (r == 3) {
                return new ElementInfo("レジスタンス(Ω)", ce.Resistance, 0, 0);
            }
            if (r == 4) {
                return new ElementInfo("名前", DumpInfo.ReferenceName);
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            var ce = (CrystalElm)Elm;
            if (n == 0 && 0 < ei.Value) {
                ce.ParallelCapacitance = ei.Value;
            }
            if (n == 1 && 0 < ei.Value) {
                ce.SeriesCapacitance = ei.Value;
            }
            if (n == 2 && 0 < ei.Value) {
                ce.Inductance = ei.Value;
            }
            if (n == 3 && 0 < ei.Value) {
                ce.Resistance = ei.Value;
            }
            if (n == 4) {
                DumpInfo.ReferenceName = ei.Textf.Text;
                setTextPos();
            }
            ce.initCrystal();
        }
    }
}
