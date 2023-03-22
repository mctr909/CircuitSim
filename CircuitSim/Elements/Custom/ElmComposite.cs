using System;
using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Input;
using Circuit.UI;

namespace Circuit.Elements.Custom {
    class VoltageSourceRecord {
        public int vsNumForElement;
        public int vsNode;
        public BaseUI elm;
    }

    class ElmComposite : BaseElement {
        /* list of elements contained in this subcircuit */
        public List<BaseUI> CompElmList = new List<BaseUI>();
        public Point[] Posts;

        public int NumPosts { get; protected set; } = 0;

        public override Point GetPost(int n) {
            return Posts[n];
        }

        /* list of nodes, mapping each one to a list of elements that reference that node */
        List<CircuitNode> mCompNodeList;
        List<VoltageSourceRecord> mVoltageSources;
        int mNumNodes = 0;

        public ElmComposite() : base() { }

        public ElmComposite(string s, int[] externalNodes) : base() {
            loadComposite(null, s, externalNodes);
            AllocNodes();
        }

        public ElmComposite(StringTokenizer st, string s, int[] externalNodes) : base() {
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

        public override int AnaVoltageSourceCount { get { return mVoltageSources.Count; } }

        public override int AnaInternalNodeCount { get { return mNumNodes - NumPosts; } }

        public override void Reset() {
            for (int i = 0; i < CompElmList.Count; i++) {
                CompElmList[i].Elm.Reset();
            }
        }

        /* are n1 and n2 connected internally somehow? */
        public override bool AnaGetConnection(int n1, int n2) {
            var cnLinks1 = mCompNodeList[n1].Links;
            var cnLinks2 = mCompNodeList[n2].Links;

            /* see if any elements are connected to both n1 and n2, then call getConnection() on those */
            for (int i = 0; i < cnLinks1.Count; i++) {
                var link1 = cnLinks1[i];
                for (int j = 0; j < cnLinks2.Count; j++) {
                    var link2 = cnLinks2[j];
                    if (link1.Elm == link2.Elm && link1.Elm.AnaGetConnection(link1.Num, link2.Num)) {
                        return true;
                    }
                }
            }
            return false;
        }

        /* is n1 connected to ground somehow? */
        public override bool AnaHasGroundConnection(int n1) {
            List<CircuitNode.LINK> cnLinks;
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
                if (cee is ElmCurrent) {
                    ((ElmCurrent)cee).stampCurrentSource(false);
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

        public override void CirPrepareIteration() {
            for (int i = 0; i < CompElmList.Count; i++) {
                CompElmList[i].Elm.CirPrepareIteration();
            }
        }

        public override void CirDoIteration() {
            for (int i = 0; i < CompElmList.Count; i++) {
                CompElmList[i].Elm.CirDoIteration();
            }
        }

        public override void CirIterationFinished() {
            for (int i = 0; i < CompElmList.Count; i++) {
                CompElmList[i].Elm.CirIterationFinished();
            }
        }

        public override void CirSetVoltage(int n, double c) {
            base.CirSetVoltage(n, c);
            var cnLinks = mCompNodeList[n].Links;
            for (int i = 0; i < cnLinks.Count; i++) {
                cnLinks[i].Elm.CirSetVoltage(cnLinks[i].Num, c);
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

        public override double CirGetCurrentIntoNode(int n) {
            double c = 0;
            var cnLinks = mCompNodeList[n].Links;
            for (int i = 0; i < cnLinks.Count; i++) {
                c += cnLinks[i].Elm.CirGetCurrentIntoNode(cnLinks[i].Num);
            }
            return c;
        }

        public void loadComposite(StringTokenizer stIn, string model, int[] externalNodes) {
            var compNodeHash = new Dictionary<int, CircuitNode>();
            var modelLinet = new StringTokenizer(model, "\r");
            CircuitNode cn;
            CircuitNode.LINK cnLink;
            VoltageSourceRecord vsRecord;

            CompElmList = new List<BaseUI>();
            mCompNodeList = new List<CircuitNode>();
            mVoltageSources = new List<VoltageSourceRecord>();

            /* Build compElmList and compNodeHash from input string */

            while (modelLinet.HasMoreTokens) {
                string line = modelLinet.nextToken();
                var stModel = new StringTokenizer(line, " +\t\n\r\f");
                var ceType = MenuItems.GetItemFromString(stModel.nextToken());
                var newce = MenuItems.ConstructElement(ceType);
                if (stIn != null) {
                    var tint = newce.DumpType;
                    string dumpedCe = stIn.nextToken();
                    dumpedCe = Utils.Unescape(dumpedCe);
                    var stCe = new StringTokenizer(dumpedCe, "_");
                    // TODO: CompositeElm loadComposite
                    //int flags = stCe.nextTokenInt();
                    int flags = 0;
                    newce = MenuItems.CreateCe(tint, new Point(), new Point(), flags, stCe);
                }
                CompElmList.Add(newce);

                int thisPost = 0;
                while (stModel.HasMoreTokens) {
                    int nodeOfThisPost = stModel.nextTokenInt();
                    cnLink = new CircuitNode.LINK();
                    cnLink.Num = thisPost;
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
                int inodes = cee.AnaInternalNodeCount;
                for (int j = 0; j != inodes; j++) {
                    cnLink = new CircuitNode.LINK();
                    cnLink.Num = j + cee.PostCount;
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
                int cnt = CompElmList[i].Elm.AnaVoltageSourceCount;
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
