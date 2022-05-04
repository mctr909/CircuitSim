using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements.Output {
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

        public override int CirPostCount { get { return 1; } }

        public override int CirConnectionNodeCount { get { return 2; } }

        // this is basically a wire, since it just connects two nodes together
        public override bool CirIsWire { get { return true; } }

        public override int CirInternalNodeCount {
            get {
                // this can happen at startup
                if (mNodeList == null) {
                    return 0;
                }
                // node assigned already?
                if (null != Text && mNodeList.ContainsKey(Text)) {
                    var nn = mNodeList[Text];
                    mNodeNumber = nn;
                    return 0;
                }
                // allocate a new one
                return 1;
            }
        }

        public override double CirVoltageDiff { get { return CirVolts[0]; } }

        public override int CirVoltageSourceCount { get { return 1; } }

        public static void ResetNodeList() {
            mNodeList = new Dictionary<string, int>();
        }

        protected override string dump() {
            return Text;
        }

        // get connection node (which is the same as regular nodes for all elements but this one).
        // node 0 is the terminal, node 1 is the internal node shared by all nodes with same name
        public override int CirGetConnectionNode(int n) {
            if (n == 0) {
                return CirNodes[0];
            }
            return mNodeNumber;
        }

        public override double CirGetCurrentIntoNode(int n) { return -mCirCurrent; }

        public override void CirSetCurrent(int x, double c) { mCirCurrent = -c; }

        public override void CirStamp() {
            mCir.StampVoltageSource(mNodeNumber, CirNodes[0], mCirVoltSource, 0);
        }

        public override void CirSetNode(int p, int n) {
            base.CirSetNode(p, n);
            if (p == 1) {
                // assign new node
                mNodeList.Add(Text, n);
                mNodeNumber = n;
            }
        }

        public override void SetPoints() {
            base.SetPoints();

            if (mPoint1.X == mPoint2.X) {
                setLead1(1 - 0.5 * Context.GetTextSize(Text).Height / mLen);
            } else {
                setLead1(1 - 0.5 * Context.GetTextSize(Text).Width / mLen);
            }
        }

        public override void Draw(CustomGraphics g) {
            drawLead(mPoint1, mLead1);
            g.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.WhiteColor;
            var str = Text;
            var lineOver = false;
            if (str.StartsWith("/")) {
                lineOver = true;
                str = str.Substring(1);
            }
            drawCenteredText(str, P2, true);
            if (lineOver) {
                int asc = (int)CustomGraphics.FontText.Size;
                if (lineOver) {
                    int ya = P2.Y - asc;
                    int sw = (int)g.GetTextSize(str).Width;
                    g.DrawLine(P2.X - sw / 2, ya, P2.X + sw / 2, ya);
                }
            }
            mCirCurCount = cirUpdateDotCount(mCirCurrent, mCirCurCount);
            drawDots(mPoint1, mLead1, mCirCurCount);
            interpPoint(ref mPos, 1 + 11.0 / mLen);
            setBbox(mPoint1, mPos, CircleSize);
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            arr[0] = Text;
            arr[1] = "I = " + Utils.CurrentText(mCirCurrent);
            arr[2] = "V = " + Utils.VoltageText(CirVolts[0]);
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                var ei = new ElementInfo("名前", 0, -1, -1);
                ei.Text = Text;
                return ei;
            }
            if (n == 1) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() { Text = "内部端子", Checked = IsInternal };
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
