using System.Drawing;

namespace Circuit.Elements {
    class Switch2Elm : SwitchElm {
        const int FLAG_CENTER_OFF = 1;
        const int openhs = 16;

        int link;
        int throwCount;
        Point[] swposts;
        Point[] swpoles;

        public Switch2Elm(int xx, int yy) : base(xx, yy, false) {
            mNoDiagonal = true;
            throwCount = 2;
            allocNodes();
        }

        Switch2Elm(int xx, int yy, bool mm) : base(xx, yy, mm) {
            mNoDiagonal = true;
            throwCount = 2;
            allocNodes();
        }

        public Switch2Elm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f, st) {
            link = st.nextTokenInt();
            throwCount = 2;
            try {
                throwCount = st.nextTokenInt();
            } catch { }
            mNoDiagonal = true;
            allocNodes();
        }

        public override bool IsWire { get { return true; } }

        public override int VoltageSourceCount { get { return (2 == position && hasCenterOff()) ? 0 : 1; } }

        public override int PostCount { get { return 1 + throwCount; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.SWITCH2; } }

        protected override string dump() {
            return base.dump() + " " + link + " " + throwCount;
        }

        public override void SetPoints() {
            base.SetPoints();
            calcLeads(32);
            swposts = new Point[throwCount];
            swpoles = new Point[2 + throwCount];
            int i;
            for (i = 0; i != throwCount; i++) {
                int hs = -openhs * (i - (throwCount - 1) / 2);
                if (throwCount == 2 && i == 0) {
                    hs = openhs;
                }
                Utils.InterpPoint(mLead1, mLead2, ref swpoles[i], 1, hs);
                Utils.InterpPoint(mPoint1, mPoint2, ref swposts[i], 1, hs);
            }
            swpoles[i] = mLead2; /* for center off */
            posCount = hasCenterOff() ? 3 : throwCount;
        }

        public override void Draw(CustomGraphics g) {
            setBbox(mPoint1, mPoint2, openhs);
            adjustBbox(swposts[0], swposts[throwCount - 1]);

            /* draw first lead */
            g.DrawThickLine(getVoltageColor(Volts[0]), mPoint1, mLead1);
            /* draw other leads */
            for (int i = 0; i < throwCount; i++) {
                g.DrawThickLine(getVoltageColor(Volts[i + 1]), swpoles[i], swposts[i]);
            }
            /* draw switch */
            g.ThickLineColor = NeedsHighlight ? SelectColor : WhiteColor;
            g.DrawThickLine(mLead1, swpoles[position]);

            updateDotCount();
            drawDots(g, mPoint1, mLead1, mCurCount);
            if (position != 2) {
                drawDots(g, swpoles[position], swposts[position], mCurCount);
            }
            drawPosts(g);
        }

        public override double GetCurrentIntoNode(int n) {
            if (n == 0) {
                return -mCurrent;
            }
            if (n == position + 1) {
                return mCurrent;
            }
            return 0;
        }

        public override Rectangle getSwitchRect() {
            var l1 = new Rectangle(mLead1.X, mLead1.Y, 0, 0);
            var s0 = new Rectangle(swpoles[0].X, swpoles[0].Y, 0, 0);
            var s1 = new Rectangle(swpoles[throwCount - 1].X, swpoles[throwCount - 1].Y, 0, 0);
            return Rectangle.Union(l1, Rectangle.Union(s0, s1));
        }

        public override Point GetPost(int n) {
            return (n == 0) ? mPoint1 : swposts[n - 1];
        }

        protected override void calculateCurrent() {
            if (position == 2 && hasCenterOff()) {
                mCurrent = 0;
            }
        }

        public override void Stamp() {
            if (position == 2 && hasCenterOff()) { /* in center? */
                return;
            }
            mCir.StampVoltageSource(Nodes[0], Nodes[position + 1], mVoltSource, 0);
        }

        public override void toggle() {
            base.toggle();
            if (link != 0) {
                int i;
                for (i = 0; i != Sim.elmList.Count; i++) {
                    var o = Sim.elmList[i];
                    if (o is Switch2Elm) {
                        var s2 = (Switch2Elm)o;
                        if (s2.link == link) {
                            s2.position = position;
                        }
                    }
                }
            }
        }

        public override bool GetConnection(int n1, int n2) {
            if (position == 2 && hasCenterOff()) {
                return false;
            }
            return comparePair(n1, n2, 0, 1 + position);
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "switch (" + (link == 0 ? "S" : "D")
                + "P" + ((throwCount > 2) ? throwCount + "T)" : "DT)");
            arr[1] = "I = " + Utils.CurrentDText(mCurrent);
        }

        public override EditInfo GetEditInfo(int n) {
            /*if (n == 1) {
                EditInfo ei = new EditInfo("", 0, -1, -1);
                ei.checkbox = new Checkbox("Center Off", hasCenterOff());
                return ei;
            }*/
            if (n == 1) {
                return new EditInfo("Switch Group", link, 0, 100).SetDimensionless();
            }
            if (n == 2) {
                return new EditInfo("# of Throws", throwCount, 2, 10).SetDimensionless();
            }
            return base.GetEditInfo(n);
        }

        public override void SetEditValue(int n, EditInfo ei) {
            /*if (n == 1) {
                flags &= ~FLAG_CENTER_OFF;
                if (ei.checkbox.getState())
                    flags |= FLAG_CENTER_OFF;
                if (hasCenterOff())
                    momentary = false;
                setPoints();
            } else*/
            if (n == 1) {
                link = (int)ei.Value;
            } else if (n == 2) {
                if (ei.Value >= 2) {
                    throwCount = (int)ei.Value;
                }
                if (throwCount > 2) {
                    momentary = false;
                }
                allocNodes();
                SetPoints();
            } else {
                base.SetEditValue(n, ei);
            }
        }

        /* this is for backwards compatibility only.
         * we only support it if throwCount = 2 */
        bool hasCenterOff() { return (mFlags & FLAG_CENTER_OFF) != 0 && throwCount == 2; }
    }
}
