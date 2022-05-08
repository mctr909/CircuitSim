﻿using System.Drawing;

namespace Circuit.Elements.Active {
    class DiodeUIVaractor : DiodeUI {
        Point[] mPlate1;
        Point[] mPlate2;

        public DiodeUIVaractor(Point pos) : base(pos) {
            Elm = new DiodeElmVaractor();
            ReferenceName = "Vc";
        }

        public DiodeUIVaractor(Point a, Point b, int f, StringTokenizer st) : base(a, b, f) {
            ReferenceName = st.nextToken();
            Elm = new DiodeElmVaractor(st);
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.VARACTOR; } }

        public override DUMP_ID Shortcut { get { return DUMP_ID.VARACTOR; } }

        protected override string dump() {
            var ce = (DiodeElmVaractor)Elm;
            return base.dump() + " " + ce.mCapVoltDiff + " " + ce.mBaseCapacitance;
        }

        public override void SetPoints() {
            base.SetPoints();
            double platef = 0.6;
            var pa = new Point[2];
            interpLeadAB(ref pa[0], ref pa[1], 0, HS);
            interpLeadAB(ref mCathode[0], ref mCathode[1], platef, HS);
            var arrowPoint = new Point();
            interpLead(ref arrowPoint, platef);
            mPoly = new Point[] { pa[0], pa[1], arrowPoint };
            // calc plates
            mPlate1 = new Point[2];
            mPlate2 = new Point[2];
            interpLeadAB(ref mPlate1[0], ref mPlate1[1], platef, HS);
            interpLeadAB(ref mPlate2[0], ref mPlate2[1], 1, HS);
            setTextPos();
        }

        public override void Draw(CustomGraphics g) {
            // draw leads and diode arrow
            drawDiode(g);

            // draw first plate
            drawLead(mPlate1[0], mPlate1[1]);

            // draw second plate
            drawLead(mPlate2[0], mPlate2[1]);

            doDots();
            drawPosts();
            drawName();
        }

        public override void GetInfo(string[] arr) {
            base.GetInfo(arr);
            var ce = (DiodeElmVaractor)Elm;
            arr[0] = "varactor";
            arr[5] = "C = " + Utils.UnitText(ce.mCapacitance, "F");
        }

        public override ElementInfo GetElementInfo(int n) {
            var ce = (DiodeElmVaractor)Elm;
            if (n == 2) {
                return new ElementInfo("静電容量(F) @ 0V", ce.mBaseCapacitance, 10, 1000);
            }
            return base.GetElementInfo(n);
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            var ce = (DiodeElmVaractor)Elm;
            if (n == 2) {
                ce.mBaseCapacitance = ei.Value;
                return;
            }
            base.SetElementValue(n, ei);
        }
    }
}