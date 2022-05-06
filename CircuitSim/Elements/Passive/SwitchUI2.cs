using System.Drawing;

namespace Circuit.Elements.Passive {
    class SwitchUI2 : SwitchUI {
        const int OPEN_HS = 8;
        const int BODY_LEN = 28;

        Point[] mSwPosts;
        Point[] mSwPoles;

        public SwitchUI2(Point pos) : base(pos, 0) {
            CirElm = new SwitchElm2();
            mNoDiagonal = true;
        }

        public SwitchUI2(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            CirElm = new SwitchElm2(st);
            mNoDiagonal = true;
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.SWITCH2; } }

        protected override string dump() {
            var ce = (SwitchElm2)CirElm;
            return base.dump() + " " + ce.mLink + " " + ce.mThrowCount;
        }

        public override Point GetPost(int n) {
            return (n == 0) ? mPost1 : mSwPosts[n - 1];
        }

        public override Rectangle GetSwitchRect() {
            var ce = (SwitchElm2)CirElm;
            var l1 = new Rectangle(mLead1.X, mLead1.Y, 0, 0);
            var s0 = new Rectangle(mSwPoles[0].X, mSwPoles[0].Y, 0, 0);
            var s1 = new Rectangle(mSwPoles[ce.mThrowCount - 1].X, mSwPoles[ce.mThrowCount - 1].Y, 0, 0);
            return Rectangle.Union(l1, Rectangle.Union(s0, s1));
        }

        public override void Toggle() {
            base.Toggle();
            var ce = (SwitchElm2)CirElm;
            if (ce.mLink != 0) {
                int i;
                for (i = 0; i != CirSim.Sim.ElmCount; i++) {
                    var o = CirSim.Sim.getElm(i).CirElm;
                    if (o is SwitchElm2) {
                        var s2 = (SwitchElm2)o;
                        if (s2.mLink == ce.mLink) {
                            s2.Position = ce.Position;
                        }
                    }
                }
            }
        }

        public override void SetPoints() {
            base.SetPoints();
            var ce = (SwitchElm2)CirElm;
            calcLeads(BODY_LEN);
            mSwPosts = new Point[ce.mThrowCount];
            mSwPoles = new Point[2 + ce.mThrowCount];
            int i;
            for (i = 0; i != ce.mThrowCount; i++) {
                int hs = -OPEN_HS * (i - (ce.mThrowCount - 1) / 2);
                if (ce.mThrowCount == 2 && i == 0) {
                    hs = OPEN_HS;
                }
                interpLead(ref mSwPoles[i], 1, hs);
                interpPoint(ref mSwPosts[i], 1, hs);
            }
            mSwPoles[i] = mLead2; /* for center off */
            ce.PosCount = ce.mThrowCount;
        }

        public override void Draw(CustomGraphics g) {
            var ce = (SwitchElm2)CirElm;
            setBbox(mPost1, mPost2, OPEN_HS);
            adjustBbox(mSwPosts[0], mSwPosts[ce.mThrowCount - 1]);

            /* draw first lead */
            drawLead(mPost1, mLead1);
            /* draw other leads */
            for (int i = 0; i < ce.mThrowCount; i++) {
                drawLead(mSwPoles[i], mSwPosts[i]);
            }
            /* draw switch */
            g.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.WhiteColor;
            g.DrawLine(mLead1, mSwPoles[ce.Position]);

            updateDotCount();
            drawDots(mPost1, mLead1, ce.CurCount);
            if (ce.Position != 2) {
                drawDots(mSwPoles[ce.Position], mSwPosts[ce.Position], ce.CurCount);
            }
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            var ce = (SwitchElm2)CirElm;
            arr[0] = "switch (" + (ce.mLink == 0 ? "S" : "D")
                + "P" + ((ce.mThrowCount > 2) ? ce.mThrowCount + "T)" : "DT)");
            arr[1] = "I = " + Utils.CurrentAbsText(ce.Current);
        }

        public override ElementInfo GetElementInfo(int n) {
            var ce = (SwitchElm2)CirElm;
            if (n == 1) {
                return new ElementInfo("グループ", ce.mLink, 0, 100).SetDimensionless();
            }
            if (n == 2) {
                return new ElementInfo("分岐数", ce.mThrowCount, 2, 10).SetDimensionless();
            }
            return base.GetElementInfo(n);
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            var ce = (SwitchElm2)CirElm;
            if (n == 1) {
                ce.mLink = (int)ei.Value;
            } else if (n == 2) {
                if (ei.Value >= 2) {
                    ce.mThrowCount = (int)ei.Value;
                }
                if (ce.mThrowCount > 2) {
                    ce.Momentary = false;
                }
                ce.AllocNodes();
                SetPoints();
            } else {
                base.SetElementValue(n, ei);
            }
        }
    }
}
