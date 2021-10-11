using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements.Passive {
    class LabeledNodeElm : CircuitElm {
        const int FLAG_ESCAPE = 4;
        const int FLAG_INTERNAL = 1;
        const int CircleSize = 17;

        public string Text;

        static Dictionary<string, int> mNodeList;
        int mNodeNumber;
        Point mPos;

        public LabeledNodeElm(Point pos) : base(pos) {
            Text = "label";
        }

        public LabeledNodeElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Text = st.nextToken();
            if ((mFlags & FLAG_ESCAPE) == 0) {
                // old-style dump before escape/unescape
                while (st.hasMoreTokens()) {
                    Text += ' ' + st.nextToken();
                }
            } else {
                // new-style dump
                Text = CustomLogicModel.unescape(Text);
            }
        }

        public bool IsInternal { get { return (mFlags & FLAG_INTERNAL) != 0; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.LABELED_NODE; } }

        public override int PostCount { get { return 1; } }

        public override int ConnectionNodeCount { get { return 2; } }

        // this is basically a wire, since it just connects two nodes together
        public override bool IsWire { get { return true; } }

        public override int InternalNodeCount {
            get {
                // this can happen at startup
                if (mNodeList == null) {
                    return 0;
                }
                // node assigned already?
                if (mNodeList.ContainsKey(Text)) {
                    var nn = mNodeList[Text];
                    mNodeNumber = nn;
                    return 0;
                }
                // allocate a new one
                return 1;
            }
        }

        public override double VoltageDiff { get { return Volts[0]; } }

        public override int VoltageSourceCount { get { return 1; } }

        protected override string dump() {
            return Text;
        }

        public override void SetPoints() {
            base.SetPoints();
            interpPoint(ref mLead1, 1 - CircleSize / mLen);
        }

        public override void SetNode(int p, int n) {
            base.SetNode(p, n);
            if (p == 1) {
                // assign new node
                mNodeList.Add(Text, n);
                mNodeNumber = n;
            }
        }

        static void resetNodeList() {
            mNodeList = new Dictionary<string, int>();
        }

        // get connection node (which is the same as regular nodes for all elements but this one).
        // node 0 is the terminal, node 1 is the internal node shared by all nodes with same name
        public override int GetConnectionNode(int n) {
            if (n == 0) {
                return Nodes[0];
            }
            return mNodeNumber;
        }

        public override double GetCurrentIntoNode(int n) { return -mCurrent; }

        public override void SetCurrent(int x, double c) { mCurrent = -c; }

        public override void Stamp() {
            mCir.StampVoltageSource(mNodeNumber, Nodes[0], mVoltSource, 0);
        }

        public override void Draw(CustomGraphics g) {
            drawVoltage(g, 0, mPoint1, mLead1);
            g.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.WhiteColor;
            var str = Text;
            var lineOver = false;
            if (str.StartsWith("/")) {
                lineOver = true;
                str = str.Substring(1);
            }
            drawCenteredText(g, str, P2.X, P2.Y, true);
            if (lineOver) {
                int asc = (int)CustomGraphics.FontText.Size;
                if (lineOver) {
                    int ya = P2.Y - asc;
                    int sw = (int)g.GetTextSize(str).Width;
                    g.DrawLine(P2.X - sw / 2, ya, P2.X + sw / 2, ya);
                }
            }
            mCurCount = updateDotCount(mCurrent, mCurCount);
            drawDots(g, mPoint1, mLead1, mCurCount);
            interpPoint(ref mPos, 1 + 11.0 / mLen);
            setBbox(mPoint1, mPos, CircleSize);
            drawPosts(g);
        }

        public override void GetInfo(string[] arr) {
            arr[0] = Text;
            arr[1] = "I = " + Utils.CurrentText(mCurrent);
            arr[2] = "V = " + Utils.VoltageText(Volts[0]);
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                var ei = new ElementInfo("Text", 0, -1, -1);
                ei.Text = Text;
                return ei;
            }
            if (n == 1) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() { Text = "Internal Node", Checked = IsInternal };
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 0) {
                Text = ei.Textf.Text;
            }
            if (n == 1) {
                mFlags = ei.ChangeFlag(mFlags, FLAG_INTERNAL);
            }
        }
    }
}
