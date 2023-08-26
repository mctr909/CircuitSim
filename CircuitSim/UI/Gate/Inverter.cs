using System.Drawing;
using System.Collections.Generic;

using Circuit.Elements.Gate;

namespace Circuit.UI.Gate {
    class Inverter : BaseUI {
        PointF[] mGatePolyEuro;
        PointF[] mGatePolyAnsi;
        PointF mCenter;
        PointF mPcircle;

        public Inverter(Point pos) : base(pos) {
            Elm = new ElmInverter();
            mNoDiagonal = true;
        }

        public Inverter(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Elm = new ElmInverter(st);
            mNoDiagonal = true;
        }

        protected override void dump(List<object> optionList) {
            var ce = (ElmInverter)Elm;
            optionList.Add(ce.SlewRate);
            optionList.Add(ce.HighVoltage);
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.INVERT; } }

        public override void SetPoints() {
            base.SetPoints();
            int hs = 10;
            int ww = 12;
            if (ww > mLen / 2) {
                ww = (int)(mLen / 2);
            }
            setLead1(0.5 - ww / mLen);
            setLead2(0.5 + (ww + 2) / mLen);
            interpPost(ref mPcircle, 0.5 + (ww - 2) / mLen);

            mGatePolyAnsi = new PointF[3];
            interpLeadAB(ref mGatePolyAnsi[0], ref mGatePolyAnsi[1], 0, hs);
            interpPost(ref mGatePolyAnsi[2], 0.5 + (ww - 5) / mLen);

            mGatePolyEuro = new PointF[4];
            var l2 = new PointF();
            interpPost(ref l2, 0.5 + (ww - 5) / mLen); /* make room for circle */
            Utils.InterpPoint(mLead1, l2, ref mGatePolyEuro[0], ref mGatePolyEuro[1], 0, hs);
            Utils.InterpPoint(mLead1, l2, ref mGatePolyEuro[3], ref mGatePolyEuro[2], 1, hs);
            Utils.InterpPoint(mLead1, l2, ref mCenter, .5);

            setBbox(hs);
        }

        public override void Draw(CustomGraphics g) {
            var ce = (ElmInverter)Elm;
            drawPosts();
            draw2Leads();
            if (Gate.UseAnsiGates()) {
                drawPolygon(mGatePolyAnsi);
            } else {
                drawPolygon(mGatePolyEuro);
                drawCenteredLText("1", mCenter, true);
            }
            drawCircle(mPcircle, 3);
            updateDotCount(ce.Current, ref mCurCount);
            drawCurrentB(mCurCount);
        }

        public override void GetInfo(string[] arr) {
            var ce = (ElmInverter)Elm;
            arr[0] = "inverter";
            arr[1] = "Vi = " + Utils.VoltageText(ce.Volts[0]);
            arr[2] = "Vo = " + Utils.VoltageText(ce.Volts[1]);
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var ce = (ElmInverter)Elm;
            if (c != 0) {
                return null;
            }
            if (r == 0) {
                return new ElementInfo("Slew Rate (V/ns)", ce.SlewRate);
            }
            if (r == 1) {
                return new ElementInfo("High電圧", ce.HighVoltage);
            }
            return null;
        }

        public override void SetElementValue(int n, int c, ElementInfo ei) {
            var ce = (ElmInverter)Elm;
            if (n == 0) {
                ce.SlewRate = ei.Value;
            }
            if (n == 1) {
                ce.HighVoltage = ElmGate.LastHighVoltage = ei.Value;
            }
        }
    }
}
