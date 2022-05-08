using System;
using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Input;

namespace Circuit.Elements.Custom {
    class VoltageSourceRecord {
        public int vsNumForElement;
        public int vsNode;
        public BaseUI elm;
    }

    class CompositeElm : BaseElement {
        /* list of elements contained in this subcircuit */
        public List<BaseUI> CompElmList = new List<BaseUI>();

        public int NumPosts { get; protected set; } = 0;

        /* list of nodes, mapping each one to a list of elements that reference that node */
        List<CircuitNode> mCompNodeList;
        List<VoltageSourceRecord> mVoltageSources;
        int mNumNodes = 0;

        public CompositeElm() : base() { }

        public CompositeElm(string s, int[] externalNodes) : base() {
            loadComposite(null, s, externalNodes);
            AllocNodes();
        }

        public CompositeElm(StringTokenizer st, string s, int[] externalNodes) : base() {
            loadComposite(st, s, externalNodes);
            AllocNodes();
        }

        public override int PostCount { get { return NumPosts; } }

        public override double Power {
            get {
                double power = 0.0;
                for (int i = 0; i < CompElmList.Count; i++) {
                    power += CompElmList[i].Elm.Power;
                }
                return power;
            }
        }

        public override int VoltageSourceCount { get { return mVoltageSources.Count; } }

        public override int InternalNodeCount { get { return mNumNodes - NumPosts; } }

        public override bool NonLinear {
            get {
                /* Lets assume that any useful composite elements are
                 * non-linear */
                return true;
            }
        }

        /* are n1 and n2 connected internally somehow? */
        public override bool GetConnection(int n1, int n2) {
            var cnLinks1 = mCompNodeList[n1].Links;
            var cnLinks2 = mCompNodeList[n2].Links;

            /* see if any elements are connected to both n1 and n2, then call getConnection() on those */
            for (int i = 0; i < cnLinks1.Count; i++) {
                var link1 = cnLinks1[i];
                for (int j = 0; j < cnLinks2.Count; j++) {
                    var link2 = cnLinks2[j];
                    if (link1.Elm == link2.Elm && link1.Elm.GetConnection(link1.Num, link2.Num)) {
                        return true;
                    }
                }
            }
            return false;
        }

        public override void Reset() {
            for (int i = 0; i < CompElmList.Count; i++) {
                CompElmList[i].Elm.Reset();
            }
        }

        /* is n1 connected to ground somehow? */
        public override bool AnaHasGroundConnection(int n1) {
            List<CircuitNodeLink> cnLinks;
            cnLinks = mCompNodeList[n1].Links;
            for (int i = 0; i < cnLinks.Count; i++) {
                if (cnLinks[i].Elm.AnaHasGroundConnection(cnLinks[i].Num)) {
                    return true;
                }
            }
            return false;
        }

        public override void AnaSetNode(int p, int n) {
            base.AnaSetNode(p, n);
            var cnLinks = mCompNodeList[p].Links;
            for (int i = 0; i < cnLinks.Count; i++) {
                cnLinks[i].Elm.AnaSetNode(cnLinks[i].Num, n);
            }
        }

        public override void AnaStamp() {
            for (int i = 0; i < CompElmList.Count; i++) {
                var cee = CompElmList[i].Elm;
                /* current sources need special stamp method */
                if (cee is CurrentElm) {
                    ((CurrentElm)cee).stampCurrentSource(false);
                } else {
                    cee.AnaStamp();
                }
            }
        }

        /* Find the component with the nth voltage
         * and set the
         * appropriate source in that component */
        public override void AnaSetVoltageSource(int n, int v) {
            var vsr = mVoltageSources[n];
            vsr.elm.Elm.AnaSetVoltageSource(vsr.vsNumForElement, v);
            vsr.vsNode = v;
        }

        public override void CirStartIteration() {
            for (int i = 0; i < CompElmList.Count; i++) {
                CompElmList[i].Elm.CirStartIteration();
            }
        }

        public override void CirDoStep() {
            for (int i = 0; i < CompElmList.Count; i++) {
                CompElmList[i].Elm.CirDoStep();
            }
        }

        public override void CirStepFinished() {
            for (int i = 0; i < CompElmList.Count; i++) {
                CompElmList[i].Elm.CirStepFinished();
            }
        }

        public override void CirSetNodeVoltage(int n, double c) {
            base.CirSetNodeVoltage(n, c);
            var cnLinks = mCompNodeList[n].Links;
            for (int i = 0; i < cnLinks.Count; i++) {
                cnLinks[i].Elm.CirSetNodeVoltage(cnLinks[i].Num, c);
            }
            Volts[n] = c;
        }

        public override void CirSetCurrent(int vsn, double c) {
            for (int i = 0; i < mVoltageSources.Count; i++) {
                if (mVoltageSources[i].vsNode == vsn) {
                    mVoltageSources[i].elm.Elm.CirSetCurrent(vsn, c);
                }
            }
        }

        public override double GetCurrentIntoNode(int n) {
            double c = 0;
            var cnLinks = mCompNodeList[n].Links;
            for (int i = 0; i < cnLinks.Count; i++) {
                c += cnLinks[i].Elm.GetCurrentIntoNode(cnLinks[i].Num);
            }
            return c;
        }

        public void loadComposite(StringTokenizer stIn, string model, int[] externalNodes) {
            var compNodeHash = new Dictionary<int, CircuitNode>();
            var modelLinet = new StringTokenizer(model, "\r");
            CircuitNode cn;
            CircuitNodeLink cnLink;
            VoltageSourceRecord vsRecord;

            CompElmList = new List<BaseUI>();
            mCompNodeList = new List<CircuitNode>();
            mVoltageSources = new List<VoltageSourceRecord>();

            /* Build compElmList and compNodeHash from input string */

            while (modelLinet.hasMoreTokens()) {
                string line = modelLinet.nextToken();
                var stModel = new StringTokenizer(line, " +\t\n\r\f");
                var ceType = MenuItems.GetItemFromString(stModel.nextToken());
                var newce = MenuItems.ConstructElement(ceType);
                if (stIn != null) {
                    var tint = newce.DumpType;
                    string dumpedCe = stIn.nextToken();
                    dumpedCe = CustomLogicModel.unescape(dumpedCe);
                    var stCe = new StringTokenizer(dumpedCe, "_");
                    // TODO: CompositeElm loadComposite
                    //int flags = stCe.nextTokenInt();
                    int flags = 0;
                    newce = MenuItems.CreateCe(tint, new Point(), new Point(), flags, stCe);
                }
                CompElmList.Add(newce);

                int thisPost = 0;
                while (stModel.hasMoreTokens()) {
                    int nodeOfThisPost = stModel.nextTokenInt();
                    cnLink = new CircuitNodeLink();
                    cnLink.Num = thisPost;
                    cnLink.UI = newce;
                    cnLink.Elm = newce.Elm;
                    if (!compNodeHash.ContainsKey(nodeOfThisPost)) {
                        cn = new CircuitNode();
                        cn.Links.Add(cnLink);
                        compNodeHash.Add(nodeOfThisPost, cn);
                    } else {
                        cn = compNodeHash[nodeOfThisPost];
                        cn.Links.Add(cnLink);
                    }
                    thisPost++;
                }
            }

            /* Flatten compNodeHash in to compNodeList */
            NumPosts = externalNodes.Length;
            for (int i = 0; i < externalNodes.Length; i++) {
                /* External Nodes First */
                if (compNodeHash.ContainsKey(externalNodes[i])) {
                    mCompNodeList.Add(compNodeHash[externalNodes[i]]);
                    compNodeHash.Remove(externalNodes[i]);
                } else {
                    throw new Exception();
                }
            }
            foreach (var entry in compNodeHash) {
                int key = entry.Key;
                mCompNodeList.Add(compNodeHash[key]);
            }

            /* allocate more nodes for sub-elements' internal nodes */
            for (int i = 0; i != CompElmList.Count; i++) {
                var ce = CompElmList[i];
                var cee = ce.Elm;
                int inodes = cee.InternalNodeCount;
                for (int j = 0; j != inodes; j++) {
                    cnLink = new CircuitNodeLink();
                    cnLink.Num = j + cee.PostCount;
                    cnLink.UI = ce;
                    cnLink.Elm = cee;
                    cn = new CircuitNode();
                    cn.Links.Add(cnLink);
                    mCompNodeList.Add(cn);
                }
            }

            mNumNodes = mCompNodeList.Count;

            /*Console.WriteLine("Dumping compNodeList");
            for (int i = 0; i < numNodes; i++) {
                Console.WriteLine("New node" + i + " Size of links:" + compNodeList.get(i).links.size());
            }*/

            /* Enumerate voltage sources */
            for (int i = 0; i < CompElmList.Count; i++) {
                int cnt = CompElmList[i].Elm.VoltageSourceCount;
                for (int j = 0; j < cnt; j++) {
                    vsRecord = new VoltageSourceRecord();
                    vsRecord.elm = CompElmList[i];
                    vsRecord.vsNumForElement = j;
                    mVoltageSources.Add(vsRecord);
                }
            }
        }
    }
}
