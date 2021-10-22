using System;
using System.Drawing;

namespace Circuit.Elements.Active {
    class ZenerElm : DiodeElm {
        const double DEFAULT_Z_VOLT = 5.6;

        static string mLastZenerModelName = "default-zener";

        Point[] mWing;
        Point mNamePos;
        string mReferenceName = "Z";

        public ZenerElm(Point pos) : base(pos) {
            mModelName = mLastZenerModelName;
            setup();
        }

        public ZenerElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) {
            if ((f & FLAG_MODEL) == 0) {
                double zvoltage = st.nextTokenDouble();
                mModel = DiodeModel.getModelWithParameters(mModel.fwdrop, zvoltage);
                mModelName = mModel.name;
                Console.WriteLine("model name wparams = " + mModelName);
            }
            setup();
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.ZENER; } }

        public override DUMP_ID Shortcut { get { return DUMP_ID.INVALID; } }

        public override void SetPoints() {
            base.SetPoints();
            mCathode = new Point[2];
            mWing = new Point[2];
            var pa = new Point[2];
            interpLeadAB(ref pa[0], ref pa[1], 0, HS);
            interpLeadAB(ref mCathode[0], ref mCathode[1], 1, HS);
            Utils.InterpPoint(mCathode[0], mCathode[1], ref mWing[0], -0.2, -HS);
            Utils.InterpPoint(mCathode[1], mCathode[0], ref mWing[1], -0.2, -HS);
            mPoly = new Point[] { pa[0], pa[1], mLead2 };
            setTextPos();
        }

        void setTextPos() {
            if (mPoint1.Y == mPoint2.Y) {
                var wn = Context.GetTextSize(mReferenceName).Width * 0.5;
                interpPoint(ref mNamePos, 0.5 - wn / mLen * mDsign, -14 * mDsign);
            } else if (mPoint1.X == mPoint2.X) {
                interpPoint(ref mNamePos, 0.5, 6 * mDsign);
            } else {
                interpPoint(ref mNamePos, 0.5, 10 * mDsign);
            }
        }

        public override void Draw(CustomGraphics g) {
            setBbox(mPoint1, mPoint2, HS);

            double v2 = Volts[1];

            draw2Leads();

            /* draw arrow thingy */
            fillVoltage(0, mPoly);
            /* draw thing arrow is pointing to */
            g.LineColor = getVoltageColor(v2);
            g.DrawLine(mCathode[0], mCathode[1]);
            /* draw wings on cathode */
            g.DrawLine(mWing[0], mCathode[0]);
            g.DrawLine(mWing[1], mCathode[1]);

            doDots();
            drawPosts();
            if (ControlPanel.ChkShowValues.Checked) {
                g.DrawLeftText(mReferenceName, mNamePos.X, mNamePos.Y);
            }
        }

        public override void GetInfo(string[] arr) {
            base.GetInfo(arr);
            arr[0] = "Zener diode";
            arr[5] = "Vz = " + Utils.VoltageText(mModel.breakdownVoltage);
        }

        void setLastModelName(string n) {
            mLastZenerModelName = n;
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                var ei = new ElementInfo("名前", 0, 0, 0);
                ei.Text = mReferenceName;
                return ei;
            }
            if (n == 2) {
                return new ElementInfo("ブレークダウン電圧(V)", mModel.breakdownVoltage, 0, 0);
            }
            return base.GetElementInfo(n);
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 0) {
                mReferenceName = ei.Textf.Text;
                setTextPos();
            }
            base.SetElementValue(n, ei);
        }
    }
}
