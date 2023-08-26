﻿using System;
using System.Drawing;

using Circuit.Elements.Active;

namespace Circuit.UI.Active {
    class DiodeZener : Diode {
        const double DEFAULT_Z_VOLT = 5.6;

        static string mLastZenerModelName = "default-zener";

        PointF[] mWing;

        public DiodeZener(Point pos) : base(pos, "Z") {
            var ce = (ElmDiode)Elm;
            ce.mModelName = mLastZenerModelName;
            setup();
        }

        public DiodeZener(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) {
            if ((f & FLAG_MODEL) == 0) {
                var ce = (ElmDiode)Elm;
                double zvoltage = st.nextTokenDouble();
                ce.mModel = DiodeModel.GetModelWithParameters(ce.mModel.FwDrop, zvoltage);
                ce.mModelName = ce.mModel.Name;
                Console.WriteLine("model name wparams = " + ce.mModelName);
            }
            setup();
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.ZENER; } }

        public override DUMP_ID Shortcut { get { return DUMP_ID.INVALID; } }

        public override void SetPoints() {
            base.SetPoints();
            setBbox(HS);
            mCathode = new PointF[2];
            mWing = new PointF[2];
            var pa = new PointF[2];
            interpLeadAB(ref pa[0], ref pa[1], 0, HS);
            interpLeadAB(ref mCathode[0], ref mCathode[1], 1, HS);
            Utils.InterpPoint(mCathode[0], mCathode[1], ref mWing[0], -0.2, -HS);
            Utils.InterpPoint(mCathode[1], mCathode[0], ref mWing[1], -0.2, -HS);
            mPoly = new PointF[] { pa[0], pa[1], mLead2 };
            setTextPos();
        }

        public override void Draw(CustomGraphics g) {
            draw2Leads();

            /* draw arrow thingy */
            fillPolygon(mPoly);
            /* draw thing arrow is pointing to */
            drawLine(mCathode[0], mCathode[1]);
            /* draw wings on cathode */
            drawLine(mWing[0], mCathode[0]);
            drawLine(mWing[1], mCathode[1]);

            doDots();
            drawPosts();
            drawName();
        }

        public override void GetInfo(string[] arr) {
            var ce = (ElmDiode)Elm;
            base.GetInfo(arr);
            arr[0] = "ツェナーダイオード";
            arr[5] = "降伏電圧：" + Utils.VoltageText(ce.mModel.BreakdownVoltage);
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var ce = (ElmDiode)Elm;
            if (c != 0) {
                return null;
            }
            if (r == 2) {
                return new ElementInfo("降伏電圧", ce.mModel.BreakdownVoltage);
            }
            return base.GetElementInfo(r, c);
        }
    }
}
