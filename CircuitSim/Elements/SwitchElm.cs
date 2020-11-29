using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements {
    class SwitchElm : CircuitElm {
        const int openhs = 16;

        public bool momentary { get; protected set; }
        /* position 0 == closed
         * position 1 == open */
        public int position { get; protected set; }
        public int posCount { get; protected set; }
        Point ps;

        public SwitchElm(int xx, int yy) : base(xx, yy) {
            momentary = false;
            position = 0;
            posCount = 2;
        }

        public SwitchElm(int xx, int yy, bool mm) : base(xx, yy) {
            position = mm ? 1 : 0;
            momentary = mm;
            posCount = 2;
        }

        public SwitchElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
            string str = st.nextToken();
            if (str.CompareTo("true") == 0) {
                position = (typeof(LogicInputElm) == GetType()) ? 0 : 1;
            } else if (str.CompareTo("false") == 0) {
                position = (typeof(LogicInputElm) == GetType()) ? 1 : 0;
            } else {
                position = int.Parse(str);
            }
            momentary = st.nextTokenBool();
            posCount = 2;
        }

        public override DUMP_ID getDumpType() { return DUMP_ID.SWITCH; }

        public override string dump() {
            return base.dump() + " " + position + " " + momentary;
        }

        public override void setPoints() {
            base.setPoints();
            calcLeads(32);
            ps = new Point();
            ps2 = new Point();
        }

        public override void draw(Graphics g) {
            int hs1 = (position == 1) ? 0 : 2;
            int hs2 = (position == 1) ? openhs : 2;
            setBbox(point1, point2, openhs);
            draw2Leads(g);
            if (position == 0) {
                doDots(g);
            }
            if (!needsHighlight()) {
                PEN_THICK_LINE.Color = whiteColor;
            }
            interpPoint(lead1, lead2, ref ps, 0, hs1);
            interpPoint(lead1, lead2, ref ps2, 1, hs2);
            drawThickLine(g, ps, ps2);
            drawPosts(g);
        }

        public virtual Rectangle getSwitchRect() {
            interpPoint(lead1, lead2, ref ps, 0, openhs);
            var l1 = new Rectangle(lead1.X, lead1.Y, 0, 0);
            var l2 = new Rectangle(lead2.X, lead2.Y, 0, 0);
            var p = new Rectangle(ps.X, ps.Y, 0, 0);
            return Rectangle.Union(l1, Rectangle.Union(l2, p));
        }

        public override void calculateCurrent() {
            if (position == 1) {
                current = 0;
            }
        }

        public override void stamp() {
            if (position == 0) {
                cir.stampVoltageSource(nodes[0], nodes[1], voltSource, 0);
            }
        }

        public override int getVoltageSourceCount() {
            return (position == 1) ? 0 : 1;
        }

        public void mouseUp() {
            if (momentary) {
                toggle();
            }
        }

        public virtual void toggle() {
            position++;
            if (position >= posCount) {
                position = 0;
            }
        }

        public override void getInfo(string[] arr) {
            arr[0] = (momentary) ? "push switch (SPST)" : "switch (SPST)";
            if (position == 1) {
                arr[1] = "open";
                arr[2] = "Vd = " + getVoltageDText(getVoltageDiff());
            } else {
                arr[1] = "closed";
                arr[2] = "V = " + getVoltageText(volts[0]);
                arr[3] = "I = " + getCurrentDText(getCurrent());
            }
        }

        public override bool getConnection(int n1, int n2) { return position == 0; }

        public override bool isWire() { return position == 0; }

        public override EditInfo getEditInfo(int n) {
            if (n == 0) {
                var ei = new EditInfo("", 0, -1, -1);
                ei.checkbox = new CheckBox();
                ei.checkbox.Text = "Momentary Switch";
                ei.checkbox.Checked = momentary;
                return ei;
            }
            return null;
        }

        public override void setEditValue(int n, EditInfo ei) {
            if (n == 0) {
                momentary = ei.checkbox.Checked;
            }
        }

        public override DUMP_ID getShortcut() { return DUMP_ID.SWITCH; }
    }
}
