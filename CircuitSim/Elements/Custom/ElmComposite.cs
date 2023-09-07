using System;
using System.Collections.Generic;

using Circuit.Elements.Input;
using Circuit.UI;

namespace Circuit.Elements.Custom {
    class VoltageSourceRecord {
        public int vsNumForElement;
        public int vsNode;
        public BaseElement elm;
    }

    class ElmComposite : BaseElement {
        /* list of elements contained in this subcircuit */
        protected List<BaseElement> CompList = new List<BaseElement>();

        /* list of nodes, mapping each one to a list of elements that reference that node */
        List<CircuitNode> mCompNodeList;
        List<VoltageSourceRecord> mVoltageSources;
        int mNumTerms = 0;
        int mNumNodes = 0;

        public ElmComposite() : base() { }

        public override int TermCount { get { return mNumTerms; } }

        public override int AnaVoltageSourceCount { get { return mVoltageSources.Count; } }

        public override int AnaInternalNodeCount { get { return mNumNodes - mNumTerms; } }

        public override void Reset() {
            for (int i = 0; i < CompList.Count; i++) {
                CompList[i].Reset();
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
            for (int i = 0; i < CompList.Count; i++) {
                var ce = CompList[i];
                /* current sources need special stamp method */
                if (ce is ElmCurrent) {
                    ((ElmCurrent)ce).stampCurrentSource(false);
                } else {
                    ce.AnaStamp();
                }
            }
        }

        /* Find the component with the nth voltage
         * and set the
         * appropriate source in that component */
        public override void AnaSetVoltageSource(int n, int v) {
            var vsr = mVoltageSources[n];
            vsr.elm.AnaSetVoltageSource(vsr.vsNumForElement, v);
            vsr.vsNode = v;
        }

        public override void CirPrepareIteration() {
            for (int i = 0; i < CompList.Count; i++) {
                CompList[i].CirPrepareIteration();
            }
        }

        public override void CirDoIteration() {
            for (int i = 0; i < CompList.Count; i++) {
                CompList[i].CirDoIteration();
            }
        }

        public override void CirIterationFinished() {
            for (int i = 0; i < CompList.Count; i++) {
                CompList[i].CirIterationFinished();
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
                    mVoltageSources[i].elm.CirSetCurrent(vsn, c);
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

        public void SetComposite(Dictionary<int, CircuitNode> nodeHash, List<BaseUI> uiList, int[] externalNodes, string expr) {
            /* Flatten nodeHash in to compNodeList */
            mCompNodeList = new List<CircuitNode>();
            mNumTerms = externalNodes.Length;
            for (int i = 0; i < externalNodes.Length; i++) {
                /* External Nodes First */
                if (nodeHash.ContainsKey(externalNodes[i])) {
                    mCompNodeList.Add(nodeHash[externalNodes[i]]);
                    nodeHash.Remove(externalNodes[i]);
                } else {
                    throw new Exception();
                }
            }
            foreach (var entry in nodeHash) {
                int key = entry.Key;
                mCompNodeList.Add(nodeHash[key]);
            }
            /* allocate more nodes for sub-elements' internal nodes */
            for (int i = 0; i != uiList.Count; i++) {
                var ce = uiList[i].Elm;
                CompList.Add(ce);
                int inodes = ce.AnaInternalNodeCount;
                for (int j = 0; j != inodes; j++) {
                    var cnLink = new CircuitNode.LINK();
                    cnLink.Num = j + ce.TermCount;
                    cnLink.Elm = ce;
                    var cn = new CircuitNode();
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
            mVoltageSources = new List<VoltageSourceRecord>();
            for (int i = 0; i < CompList.Count; i++) {
                int cnt = CompList[i].AnaVoltageSourceCount;
                for (int j = 0; j < cnt; j++) {
                    var vsRecord = new VoltageSourceRecord();
                    vsRecord.elm = CompList[i];
                    vsRecord.vsNumForElement = j;
                    mVoltageSources.Add(vsRecord);
                }
            }

            AllocNodes();
            Init(expr);
        }

        protected virtual void Init(string expr) { }
    }
}
