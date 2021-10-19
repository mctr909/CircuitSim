using System.Drawing;

namespace Circuit.Elements.Passive {
    class Switch2Elm : SwitchElm {
        const int OPEN_HS = 8;
        const int BODY_LEN = 28;

        int mLink;
        int mThrowCount;
        Point[] mSwPosts;
        Point[] mSwPoles;

        public Switch2Elm(Point pos) : base(pos, false) {
            mNoDiagonal = true;
            mThrowCount = 2;
            allocNodes();
        }

        public Switch2Elm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) {
            mLink = st.nextTokenInt();
            mThrowCount = 2;
            try {
                mThrowCount = st.nextTokenInt();
            } catch { }
            mNoDiagonal = true;
            allocNodes();
        }

        public override bool IsWire { get { return true; } }

        public override int VoltageSourceCount { get { return 1; } }

        public override int PostCount { get { return 1 + mThrowCount; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.SWITCH2; } }

        protected override string dump() {
            return base.dump() + " " + mLink + " " + mThrowCount;
        }

        protected override void calculateCurrent() { }

        public override Point GetPost(int n) {
            return (n == 0) ? mPoint1 : mSwPosts[n - 1];
        }

        public override bool GetConnection(int n1, int n2) {
            return comparePair(n1, n2, 0, 1 + Position);
        }

        public override double GetCurrentIntoNode(int n) {
            if (n == 0) {
                return -mCurrent;
            }
            if (n == Position + 1) {
                return mCurrent;
            }
            return 0;
        }

        public override Rectangle GetSwitchRect() {
            var l1 = new Rectangle(mLead1.X, mLead1.Y, 0, 0);
            var s0 = new Rectangle(mSwPoles[0].X, mSwPoles[0].Y, 0, 0);
            var s1 = new Rectangle(mSwPoles[mThrowCount - 1].X, mSwPoles[mThrowCount - 1].Y, 0, 0);
            return Rectangle.Union(l1, Rectangle.Union(s0, s1));
        }

        public override void Toggle() {
            base.Toggle();
            if (mLink != 0) {
                int i;
                for (i = 0; i != CirSim.Sim.ElmList.Count; i++) {
                    var o = CirSim.Sim.ElmList[i];
                    if (o is Switch2Elm) {
                        var s2 = (Switch2Elm)o;
                        if (s2.mLink == mLink) {
                            s2.Position = Position;
                        }
                    }
                }
            }
        }

        public override void Stamp() {
            mCir.StampVoltageSource(Nodes[0], Nodes[Position + 1], mVoltSource, 0);
        }

        public override void SetPoints() {
            base.SetPoints();
            calcLeads(BODY_LEN);
            mSwPosts = new Point[mThrowCount];
            mSwPoles = new Point[2 + mThrowCount];
            int i;
            for (i = 0; i != mThrowCount; i++) {
                int hs = -OPEN_HS * (i - (mThrowCount - 1) / 2);
                if (mThrowCount == 2 && i == 0) {
                    hs = OPEN_HS;
                }
                interpLead(ref mSwPoles[i], 1, hs);
                interpPoint(ref mSwPosts[i], 1, hs);
            }
            mSwPoles[i] = mLead2; /* for center off */
            PosCount = mThrowCount;
        }

        public override void Draw() {
            setBbox(mPoint1, mPoint2, OPEN_HS);
            adjustBbox(mSwPosts[0], mSwPosts[mThrowCount - 1]);

            /* draw first lead */
            drawVoltage(0, mPoint1, mLead1);
            /* draw other leads */
            for (int i = 0; i < mThrowCount; i++) {
                drawVoltage(i + 1, mSwPoles[i], mSwPosts[i]);
            }
            /* draw switch */
            Context.ThickLineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.WhiteColor;
            Context.DrawThickLine(mLead1, mSwPoles[Position]);

            updateDotCount();
            drawDots(mPoint1, mLead1, mCurCount);
            if (Position != 2) {
                drawDots(mSwPoles[Position], mSwPosts[Position], mCurCount);
            }
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "switch (" + (mLink == 0 ? "S" : "D")
                + "P" + ((mThrowCount > 2) ? mThrowCount + "T)" : "DT)");
            arr[1] = "I = " + Utils.CurrentDText(mCurrent);
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 1) {
                return new ElementInfo("グループ", mLink, 0, 100).SetDimensionless();
            }
            if (n == 2) {
                return new ElementInfo("分岐数", mThrowCount, 2, 10).SetDimensionless();
            }
            return base.GetElementInfo(n);
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 1) {
                mLink = (int)ei.Value;
            } else if (n == 2) {
                if (ei.Value >= 2) {
                    mThrowCount = (int)ei.Value;
                }
                if (mThrowCount > 2) {
                    Momentary = false;
                }
                allocNodes();
                SetPoints();
            } else {
                base.SetElementValue(n, ei);
            }
        }
    }
}
