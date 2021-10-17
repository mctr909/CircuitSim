using System;
using System.Drawing;

namespace Circuit.Elements.Active {
    class ZenerElm : DiodeElm {
        const double DEFAULT_Z_VOLT = 5.6;

        static string mLastZenerModelName = "default-zener";

        Point[] mWing;

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
            calcLeads(12);
            mCathode = new Point[2];
            mWing = new Point[2];
            var pa = new Point[2];
            interpLeadAB(ref pa[0], ref pa[1], 0, HS);
            interpLeadAB(ref mCathode[0], ref mCathode[1], 1, HS);
            Utils.InterpPoint(mCathode[0], mCathode[1], ref mWing[0], -0.2, -HS);
            Utils.InterpPoint(mCathode[1], mCathode[0], ref mWing[1], -0.2, -HS);
            mPoly = new Point[] { pa[0], pa[1], mLead2 };
        }

        public override void Draw(CustomGraphics g) {
            setBbox(mPoint1, mPoint2, HS);

            double v2 = Volts[1];

            draw2Leads(g);

            /* draw arrow thingy */
            drawVoltage(g, 0, mPoly);
            /* draw thing arrow is pointing to */
            g.ThickLineColor = getVoltageColor(v2);
            g.DrawThickLine(mCathode[0], mCathode[1]);
            /* draw wings on cathode */
            g.DrawThickLine(mWing[0], mCathode[0]);
            g.DrawThickLine(mWing[1], mCathode[1]);

            doDots(g);
            drawPosts(g);
        }

        public override void GetInfo(string[] arr) {
            base.GetInfo(arr);
            arr[0] = "Zener diode";
            arr[5] = "Vz = " + Utils.VoltageText(mModel.breakdownVoltage);
        }

        void setLastModelName(string n) {
            mLastZenerModelName = n;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 2) {
                var val = new InputDialog("Breakdown Voltage", "5.6");
                try {
                    double zvoltage = double.Parse(val.Value);
                    zvoltage = Math.Abs(zvoltage);
                    if (zvoltage > 0) {
                        mModel = DiodeModel.getZenerModel(zvoltage);
                        mModelName = mModel.name;
                        ei.NewDialog = true;
                        return;
                    }
                } catch { }
            }
            base.SetElementValue(n, ei);
        }
    }
}
