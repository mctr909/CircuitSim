using System;
using System.Collections.Generic;
using System.Drawing;

namespace Circuit.Elements.Gate {
    class TriStateUI : BaseUI {
        const int BODY_LEN = 32;

        Point mPost3;
        Point mLead3;
        Point[] mGatePoly;

        public TriStateUI(Point pos) : base(pos) {
            Elm = new TriStateElm();
        }

        public TriStateUI(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Elm = new TriStateElm(st);
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.TRISTATE; } }

        protected override void dump(List<object> optionList) {
            var ce = (TriStateElm)Elm;
            optionList.Add(ce.Ron);
            optionList.Add(ce.Roff);
        }

        public override void SetPoints() {
            base.SetPoints();
            calcLeads(BODY_LEN);
            int hs = BODY_LEN / 2;
            int ww = BODY_LEN / 2;
            if (ww > mLen / 2) {
                ww = (int)(mLen / 2);
            }
            mGatePoly = new Point[3];
            interpLeadAB(ref mGatePoly[0], ref mGatePoly[1], 0, hs);
            interpPoint(ref mGatePoly[2], 0.5 + ww / mLen);
            interpPoint(ref mPost3, 0.5, -hs);
            interpPoint(ref mLead3, 0.5, -hs / 2);
        }

        public override void Draw(CustomGraphics g) {
            var ce = (TriStateElm)Elm;
            int hs = 16;
            setBbox(mPost1, mPost2, hs);

            draw2Leads();

            g.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;
            g.DrawPolygon(mGatePoly);
            drawLead(mPost3, mLead3);
            ce.CurCount = updateDotCount(ce.Current, ce.CurCount);
            drawDots(mLead2, mPost2, ce.CurCount);
            drawPosts();
        }

        public override void Drag(Point pos) {
            pos = CirSimForm.Sim.SnapGrid(pos);
            if (Math.Abs(DumpInfo.P1.X - pos.X) < Math.Abs(DumpInfo.P1.Y - pos.Y)) {
                pos.X = DumpInfo.P1.X;
            } else {
                pos.Y = DumpInfo.P1.Y;
            }
            int q1 = Math.Abs(DumpInfo.P1.X - pos.X) + Math.Abs(DumpInfo.P1.Y - pos.Y);
            int q2 = (q1 / 2) % CirSimForm.GRID_SIZE;
            if (q2 != 0) {
                return;
            }
            DumpInfo.SetP2(pos);
            SetPoints();
        }

        public override Point GetPost(int n) {
            return (n == 0) ? mPost1 : (n == 1) ? mPost2 : mPost3;
        }

        public override void GetInfo(string[] arr) {
            var ce = (TriStateElm)Elm;
            arr[0] = "tri-state buffer";
            arr[1] = ce.Open ? "open" : "closed";
            arr[2] = "Vd = " + Utils.VoltageAbsText(ce.VoltageDiff);
            arr[3] = "I = " + Utils.CurrentAbsText(ce.Current);
            arr[4] = "Vc = " + Utils.VoltageText(ce.Volts[2]);
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var ce = (TriStateElm)Elm;
            if (c != 0) {
                return null;
            }
            if (r == 0) {
                return new ElementInfo("オン抵抗(Ω)", ce.Ron, 0, 0);
            }
            if (r == 1) {
                return new ElementInfo("オフ抵抗(Ω)", ce.Roff, 0, 0);
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            var ce = (TriStateElm)Elm;
            if (n == 0 && ei.Value > 0) {
                ce.Ron = ei.Value;
            }
            if (n == 1 && ei.Value > 0) {
                ce.Roff = ei.Value;
            }
        }
    }
}
