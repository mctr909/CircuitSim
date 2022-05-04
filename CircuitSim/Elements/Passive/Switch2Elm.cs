using System.Drawing;

namespace Circuit.Elements.Passive {
    class Switch2Elm : SwitchElm {
        const int OPEN_HS = 8;
        const int BODY_LEN = 28;

        Point[] mSwPosts;
        Point[] mSwPoles;

        public Switch2Elm(Point pos) : base(pos, 0) {
            CirElm = new Switch2ElmE();
            mNoDiagonal = true;
        }

        public Switch2Elm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            CirElm = new Switch2ElmE(st);
            mNoDiagonal = true;
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.SWITCH2; } }

        protected override string dump() {
            var ce = (Switch2ElmE)CirElm;
            return base.dump() + " " + ce.mLink + " " + ce.mThrowCount;
        }

        public override Point GetPost(int n) {
            return (n == 0) ? mPoint1 : mSwPosts[n - 1];
        }

        public override bool GetConnection(int n1, int n2) {
            var ce = (Switch2ElmE)CirElm;
            return comparePair(n1, n2, 0, 1 + ce.Position);
        }

        public override Rectangle GetSwitchRect() {
            var ce = (Switch2ElmE)CirElm;
            var l1 = new Rectangle(mLead1.X, mLead1.Y, 0, 0);
            var s0 = new Rectangle(mSwPoles[0].X, mSwPoles[0].Y, 0, 0);
            var s1 = new Rectangle(mSwPoles[ce.mThrowCount - 1].X, mSwPoles[ce.mThrowCount - 1].Y, 0, 0);
            return Rectangle.Union(l1, Rectangle.Union(s0, s1));
        }

        public override void Toggle() {
            base.Toggle();
            var ce = (Switch2ElmE)CirElm;
            if (ce.mLink != 0) {
                int i;
                for (i = 0; i != CirSim.Sim.ElmCount; i++) {
                    var o = CirSim.Sim.getElmE(i);
                    if (o is Switch2ElmE) {
                        var s2 = (Switch2ElmE)o;
                        if (s2.mLink == ce.mLink) {
                            s2.Position = ce.Position;
                        }
                    }
                }
            }
        }

        public override void SetPoints() {
            base.SetPoints();
            var ce = (Switch2ElmE)CirElm;
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
            var ce = (Switch2ElmE)CirElm;
            setBbox(mPoint1, mPoint2, OPEN_HS);
            adjustBbox(mSwPosts[0], mSwPosts[ce.mThrowCount - 1]);

            /* draw first lead */
            drawLead(mPoint1, mLead1);
            /* draw other leads */
            for (int i = 0; i < ce.mThrowCount; i++) {
                drawLead(mSwPoles[i], mSwPosts[i]);
            }
            /* draw switch */
            g.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.WhiteColor;
            g.DrawLine(mLead1, mSwPoles[ce.Position]);

            ce.cirUpdateDotCount();
            drawDots(mPoint1, mLead1, ce.mCirCurCount);
            if (ce.Position != 2) {
                drawDots(mSwPoles[ce.Position], mSwPosts[ce.Position], ce.mCirCurCount);
            }
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            var ce = (Switch2ElmE)CirElm;
            arr[0] = "switch (" + (ce.mLink == 0 ? "S" : "D")
                + "P" + ((ce.mThrowCount > 2) ? ce.mThrowCount + "T)" : "DT)");
            arr[1] = "I = " + Utils.CurrentAbsText(ce.mCirCurrent);
        }

        public override ElementInfo GetElementInfo(int n) {
            var ce = (Switch2ElmE)CirElm;
            if (n == 1) {
                return new ElementInfo("グループ", ce.mLink, 0, 100).SetDimensionless();
            }
            if (n == 2) {
                return new ElementInfo("分岐数", ce.mThrowCount, 2, 10).SetDimensionless();
            }
            return base.GetElementInfo(n);
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            var ce = (Switch2ElmE)CirElm;
            if (n == 1) {
                ce.mLink = (int)ei.Value;
            } else if (n == 2) {
                if (ei.Value >= 2) {
                    ce.mThrowCount = (int)ei.Value;
                }
                if (ce.mThrowCount > 2) {
                    ce.Momentary = false;
                }
                ce.cirAllocNodes();
                SetPoints();
            } else {
                base.SetElementValue(n, ei);
            }
        }
    }
}
