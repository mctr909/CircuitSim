using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Passive;

namespace Circuit.UI.Passive {
    class SwitchMulti : Switch {
        const int OPEN_HS = 8;
        const int BODY_LEN = 24;

        PointF[] mSwPoles;

        public SwitchMulti(Point pos) : base(pos, 0) {
            Elm = new ElmSwitchMulti();
            Elm.AllocNodes();
            mNoDiagonal = true;
        }

        public SwitchMulti(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            var elm = new ElmSwitchMulti();
            Elm = elm;
            elm.Position = st.nextTokenInt();
            st.nextTokenBool(out elm.Momentary, false);
            elm.Link = st.nextTokenInt();
            elm.ThrowCount = st.nextTokenInt();
            elm.AllocNodes();
            mNoDiagonal = true;
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.SWITCH_MULTI; } }

        protected override void dump(List<object> optionList) {
            var ce = (ElmSwitchMulti)Elm;
            base.dump(optionList);
            optionList.Add(ce.ThrowCount);
        }

        public override RectangleF GetSwitchRect() {
            var ce = (ElmSwitchMulti)Elm;
            var l1 = new RectangleF(mLead1.X, mLead1.Y, 0, 0);
            var s0 = new RectangleF(mSwPoles[0].X, mSwPoles[0].Y, 0, 0);
            var s1 = new RectangleF(mSwPoles[ce.ThrowCount - 1].X, mSwPoles[ce.ThrowCount - 1].Y, 0, 0);
            return RectangleF.Union(l1, RectangleF.Union(s0, s1));
        }

        public override void SetPoints() {
            base.SetPoints();
            var ce = (ElmSwitchMulti)Elm;
            calcLeads(BODY_LEN);
            ce.SwPosts = new Point[ce.ThrowCount];
            mSwPoles = new PointF[2 + ce.ThrowCount];
            int i;
            for (i = 0; i != ce.ThrowCount; i++) {
                int hs = -OPEN_HS * (i - (ce.ThrowCount - 1) / 2);
                if (ce.ThrowCount == 2 && i == 0) {
                    hs = OPEN_HS;
                }
                interpLead(ref mSwPoles[i], 1, hs);
                interpPost(ref ce.SwPosts[i], 1, hs);
            }
            mSwPoles[i] = mLead2; /* for center off */
            ce.PosCount = ce.ThrowCount;
        }

        public override void Draw(CustomGraphics g) {
            var ce = (ElmSwitchMulti)Elm;
            setBbox(OPEN_HS);
            DumpInfo.AdjustBbox(ce.SwPosts[0], ce.SwPosts[ce.ThrowCount - 1]);
            /* draw first lead */
            drawLeadA();
            g.DrawPost(mLead1);
            /* draw other leads */
            for (int i = 0; i < ce.ThrowCount; i++) {
                var pole = mSwPoles[i];
                drawLine(pole, ce.SwPosts[i]);
                g.DrawPost(pole);
            }
            /* draw switch */
            drawLine(mLead1, mSwPoles[ce.Position]);

            updateDotCount();
            drawCurrentA(CurCount);
            if (ce.Position != 2) {
                drawCurrent(mSwPoles[ce.Position], ce.SwPosts[ce.Position], CurCount);
            }
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            var ce = (ElmSwitchMulti)Elm;
            arr[0] = "switch (" + (ce.Link == 0 ? "S" : "D")
                + "P" + ((ce.ThrowCount > 2) ? ce.ThrowCount + "T)" : "DT)");
            arr[1] = "I = " + Utils.CurrentAbsText(ce.Current);
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var ce = (ElmSwitchMulti)Elm;
            if (c != 0) {
                return null;
            }
            if (r == 1) {
                return new ElementInfo("分岐数", ce.ThrowCount);
            }
            return base.GetElementInfo(r, c);
        }

        public override void SetElementValue(int n, int c, ElementInfo ei) {
            var ce = (ElmSwitchMulti)Elm;
            if (n == 1) {
                if (ei.Value < 2) {
                    ei.Value = 2;
                }
                ce.ThrowCount = (int)ei.Value;
                ce.AllocNodes();
                SetPoints();
            } else {
                base.SetElementValue(n, c, ei);
            }
        }
    }
}
