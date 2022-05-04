using System.Collections.Generic;

namespace Circuit.Elements.Output {
    class LabeledNodeElmE : BaseElement {
        public string Text;

        static Dictionary<string, int> mNodeList;
        int mNodeNumber;

        public LabeledNodeElmE() : base() {
            Text = "label";
        }

        public LabeledNodeElmE(StringTokenizer st) : base() {
            Text = st.nextToken();
            Text = CustomLogicModel.unescape(Text);
        }

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

        public static void ResetNodeList() {
            mNodeList = new Dictionary<string, int>();
        }
    }
}
