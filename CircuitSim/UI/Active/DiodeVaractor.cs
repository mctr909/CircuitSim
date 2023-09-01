using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Active;

namespace Circuit.UI.Active {
    class DiodeVaractor : Diode {
        PointF[] mPlate1;
        PointF[] mPlate2;

        public DiodeVaractor(Point pos) : base(pos, "Vc") {
            Elm = new ElmDiodeVaractor();
            setup();
        }

        public DiodeVaractor(Point a, Point b, int f, StringTokenizer st) : base(a, b, f) {
            Elm = new ElmDiodeVaractor(st);
            setup();
        }

        public override DUMP_ID DumpId { get { return DUMP_ID.VARACTOR; } }

        protected override void dump(List<object> optionList) {
            var ce = (ElmDiodeVaractor)Elm;
            base.dump(optionList);
            optionList.Add(ce.mCapVoltDiff.ToString("g3"));
            optionList.Add(ce.mBaseCapacitance.ToString("g3"));
        }

        public override void SetPoints() {
            BODY_LEN = 12;
            base.SetPoints();
            var plate11 = (BODY_LEN - 4.0) / BODY_LEN;
            var plate12 = (BODY_LEN - 5.0) / BODY_LEN;
            var plate21 = (BODY_LEN - 1.0) / BODY_LEN;
            var pa = new PointF[2];
            interpLeadAB(ref pa[0], ref pa[1], 0, HS);
            var arrowPoint = new PointF();
            interpLead(ref arrowPoint, plate11);
            mPoly = new PointF[] { pa[0], pa[1], arrowPoint };
            // calc plates
            mPlate1 = new PointF[4];
            mPlate2 = new PointF[4];
            interpLeadAB(ref mPlate1[0], ref mPlate1[1], plate11, HS);
            interpLeadAB(ref mPlate1[3], ref mPlate1[2], plate12, HS);
            interpLeadAB(ref mPlate2[0], ref mPlate2[1], plate21, HS);
            interpLeadAB(ref mPlate2[3], ref mPlate2[2], 1, HS);
            setTextPos();
        }

        public override void Draw(CustomGraphics g) {
            // draw leads and diode arrow
            drawDiode();
            // draw first plate
            fillPolygon(mPlate1);
            // draw second plate
            fillPolygon(mPlate2);
            doDots();
            drawName();
        }

        public override void GetInfo(string[] arr) {
            base.GetInfo(arr);
            var ce = (ElmDiodeVaractor)Elm;
            arr[0] = "可変容量ダイオード";
            arr[5] = "静電容量：" + Utils.UnitText(ce.mCapacitance, "F");
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var ce = (ElmDiodeVaractor)Elm;
            if (c != 0) {
                return null;
            }
            if (r == 2) {
                return new ElementInfo("静電容量 @ 0V", ce.mBaseCapacitance);
            }
            return base.GetElementInfo(r, c);
        }

        public override void SetElementValue(int n, int c, ElementInfo ei) {
            var ce = (ElmDiodeVaractor)Elm;
            if (n == 2) {
                ce.mBaseCapacitance = ei.Value;
                return;
            }
            base.SetElementValue(n, c, ei);
        }
    }
}
