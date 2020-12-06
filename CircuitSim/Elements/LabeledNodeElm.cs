using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements {
    class LabeledNodeElm : CircuitElm {
        const int FLAG_ESCAPE = 4;
        const int FLAG_INTERNAL = 1;
        const int circleSize = 17;

        static Dictionary<string, int> nodeList;
        public string text { get; private set; }
        int nodeNumber;

        Point ps2;

        public LabeledNodeElm(int xx, int yy) : base(xx, yy) {
            text = "label";
        }

        public LabeledNodeElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
            text = st.nextToken();
            if ((mFlags & FLAG_ESCAPE) == 0) {
                /* old-style dump before escape/unescape */
                while (st.hasMoreTokens()) {
                    text += ' ' + st.nextToken();
                }
            } else {
                /* new-style dump */
                text = CustomLogicModel.unescape(text);
            }
        }

        /* this is basically a wire, since it just connects two nodes together */
        public override bool IsWire { get { return true; } }

        public override double VoltageDiff { get { return Volts[0]; } }

        public override int VoltageSourceCount { get { return 1; } }

        public override int InternalNodeCount {
            get {
                /* this can happen at startup */
                if (nodeList == null || string.IsNullOrEmpty(text)) {
                    return 0;
                }
                /* node assigned already? */
                if (nodeList.ContainsKey(text)) {
                    nodeNumber = nodeList[text];
                    return 0;
                }
                /* allocate a new one */
                return 1;
            }
        }

        public override int PostCount { get { return 1; } }

        public override int ConnectionNodeCount { get { return 2; } }

        protected override string dump() {
            mFlags |= FLAG_ESCAPE;
            return CustomLogicModel.escape(text);
        }

        protected override DUMP_ID getDumpType() { return DUMP_ID.LABELED_NODE; }

        public bool isInternal() { return (mFlags & FLAG_INTERNAL) != 0; }

        public static void resetNodeList() {
            nodeList = new Dictionary<string, int>();
        }

        public override void SetPoints() {
            base.SetPoints();
            mLead1 = interpPoint(mPoint1, mPoint2, 1 - circleSize / mLen);
        }

        public override void SetNode(int p, int n) {
            base.SetNode(p, n);
            if (p == 1) {
                /* assign new node */
                nodeList.Add(text, n);
                nodeNumber = n;
            }
        }

        /* get connection node (which is the same as regular nodes for all elements but this one).
         * node 0 is the terminal, node 1 is the internal node shared by all nodes with same name */
        public override int GetConnectionNode(int n) {
            if (n == 0) {
                return Nodes[0];
            }
            return nodeNumber;
        }

        public override void Draw(CustomGraphics g) {
            g.DrawThickLine(getVoltageColor(Volts[0]), mPoint1, mLead1);

            g.ThickLineColor = NeedsHighlight ? SelectColor : WhiteColor;

            string str = text;
            bool lineOver = false;
            if (str.StartsWith("/")) {
                lineOver = true;
                str = str.Substring(1);
            }
            drawCenteredLText(g, str, X2, Y2, true);

            if (lineOver) {
                int ya = Y2 - CustomGraphics.FontText.Height;
                int sw = (int)g.GetTextSize(str).Width;
                g.DrawThickLine(X2 - sw / 2, ya, X2 + sw / 2, ya);
            }
            mCurCount = updateDotCount(mCurrent, mCurCount);
            drawDots(g, mPoint1, mLead1, mCurCount);
            interpPoint(mPoint1, mPoint2, ref ps2, 1 + 11.0 / mLen);
            setBbox(mPoint1, ps2, circleSize);
            drawPosts(g);
        }

        public override double GetCurrentIntoNode(int n) { return -mCurrent; }

        public override void SetCurrent(int x, double c) { mCurrent = -c; }

        public override void Stamp() {
            Cir.StampVoltageSource(nodeNumber, Nodes[0], mVoltSource, 0);
        }

        public override void GetInfo(string[] arr) {
            arr[0] = text;
            arr[1] = "I = " + getCurrentText(mCurrent);
            arr[2] = "V = " + getVoltageText(Volts[0]);
        }

        public override EditInfo GetEditInfo(int n) {
            if (n == 0) {
                var ei = new EditInfo("Text", 0, -1, -1);
                ei.Text = text;
                return ei;
            }
            if (n == 1) {
                var ei = new EditInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox();
                ei.CheckBox.Text = "Internal Node";
                ei.CheckBox.Checked = isInternal();
                return ei;
            }
            return null;
        }

        public override void SetEditValue(int n, EditInfo ei) {
            if (n == 0) {
                text = ei.Textf.Text;
            }
            if (n == 1) {
                mFlags = ei.ChangeFlag(mFlags, FLAG_INTERNAL);
            }
        }
    }
}
