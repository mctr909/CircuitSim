using System.Drawing;

namespace Circuit.Elements.Gate {
    class InverterUI : BaseUI {
        Point[] mGatePolyEuro;
        Point[] mGatePolyAnsi;
        Point mCenter;
        Point mPcircle;

        public InverterUI(Point pos) : base(pos) {
            Elm = new InverterElm();
            mNoDiagonal = true;
        }

        public InverterUI(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Elm = new InverterElm(st);
            mNoDiagonal = true;
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.INVERT; } }

        public override void SetPoints() {
            base.SetPoints();
            int hs = 16;
            int ww = 16;
            if (ww > mLen / 2) {
                ww = (int)(mLen / 2);
            }
            setLead1(0.5 - ww / mLen);
            setLead2(0.5 + (ww + 2) / mLen);
            interpPoint(ref mPcircle, 0.5 + (ww - 2) / mLen);

            mGatePolyAnsi = new Point[3];
            interpLeadAB(ref mGatePolyAnsi[0], ref mGatePolyAnsi[1], 0, hs);
            interpPoint(ref mGatePolyAnsi[2], 0.5 + (ww - 5) / mLen);

            mGatePolyEuro = new Point[4];
            var l2 = new Point();
            interpPoint(ref l2, 0.5 + (ww - 5) / mLen); /* make room for circle */
            Utils.InterpPoint(mLead1, l2, ref mGatePolyEuro[0], ref mGatePolyEuro[1], 0, hs);
            Utils.InterpPoint(mLead1, l2, ref mGatePolyEuro[3], ref mGatePolyEuro[2], 1, hs);
            Utils.InterpPoint(mLead1, l2, ref mCenter, .5);

            setBbox(mPost1, mPost2, hs);
        }

        public override void Draw(CustomGraphics g) {
            var ce = (InverterElm)Elm;
            drawPosts();
            draw2Leads();
            g.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;
            if (GateUI.UseAnsiGates()) {
                g.DrawPolygon(mGatePolyAnsi);
            } else {
                g.DrawPolygon(mGatePolyEuro);
                drawCenteredLText("1", mCenter, true);
            }
            g.DrawCircle(mPcircle, 3);
            ce.CurCount = updateDotCount(ce.Current, ce.CurCount);
            drawDots(mLead2, mPost2, ce.CurCount);
        }

        public override void GetInfo(string[] arr) {
            var ce = (InverterElm)Elm;
            arr[0] = "inverter";
            arr[1] = "Vi = " + Utils.VoltageText(ce.Volts[0]);
            arr[2] = "Vo = " + Utils.VoltageText(ce.Volts[1]);
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var ce = (InverterElm)Elm;
            if (c != 0) {
                return null;
            }
            if (r == 0) {
                return new ElementInfo("Slew Rate (V/ns)", ce.SlewRate);
            }
            if (r == 1) {
                return new ElementInfo("High Voltage (V)", ce.HighVoltage, 1, 10);
            }
            return null;
        }

        public override void SetElementValue(int n, int c, ElementInfo ei) {
            var ce = (InverterElm)Elm;
            if (n == 0) {
                ce.SlewRate = ei.Value;
            }
            if (n == 1) {
                ce.HighVoltage = GateElm.LastHighVoltage = ei.Value;
            }
        }
    }
}
