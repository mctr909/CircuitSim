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
        public List<BaseUI> compElmList = new List<BaseUI>();

        /* list of nodes, mapping each one to a list of elements that reference that node */
        public List<CircuitNode> compNodeList;

        public int numPosts = 0;

        protected bool useEscape;
        protected int numNodes = 0;

        protected List<VoltageSourceRecord> voltageSources;

        public CompositeElm() : base() { }

        public CompositeElm(string s, int[] externalNodes) : base() {
            loadComposite(null, s, externalNodes);
            AllocNodes();
        }

        public CompositeElm(StringTokenizer st, string s, int[] externalNodes) : base() {
            loadComposite(st, s, externalNodes);
            AllocNodes();
        }

        public override int PostCount { get { return numPosts; } }

        public override double Power {
            get {
                double power = 0.0;
                for (int i = 0; i < compElmList.Count; i++) {
                    power += compElmList[i].CirElm.Power;
                }
                return power;
            }
        }

        public override int VoltageSourceCount { get { return voltageSources.Count; } }

        public override int InternalNodeCount { get { return numNodes - numPosts; } }

        public override bool NonLinear {
            get {
                /* Lets assume that any useful composite elements are
                 * non-linear */
                return true;
            }
        }

        public override void Reset() {
            for (int i = 0; i < compElmList.Count; i++) {
                compElmList[i].CirElm.Reset();
            }
        }

        /* is n1 connected to ground somehow? */
        public override bool AnaHasGroundConnection(int n1) {
            List<CircuitNodeLink> cnLinks;
            cnLinks = compNodeList[n1].Links;
            for (int i = 0; i < cnLinks.Count; i++) {
                if (cnLinks[i].Elm.CirElm.AnaHasGroundConnection(cnLinks[i].Num)) {
                    return true;
                }
            }
            return false;
        }

        public override void AnaSetNode(int p, int n) {
            base.AnaSetNode(p, n);
            var cnLinks = compNodeList[p].Links;
            for (int i = 0; i < cnLinks.Count; i++) {
                cnLinks[i].Elm.CirElm.AnaSetNode(cnLinks[i].Num, n);
            }
        }

        public override void AnaStamp() {
            for (int i = 0; i < compElmList.Count; i++) {
                var cee = compElmList[i].CirElm;
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
            var vsr = voltageSources[n];
            vsr.elm.CirElm.AnaSetVoltageSource(vsr.vsNumForElement, v);
            vsr.vsNode = v;
        }

        public override void CirStartIteration() {
            for (int i = 0; i < compElmList.Count; i++) {
                compElmList[i].CirElm.CirStartIteration();
            }
        }

        public override void CirDoStep() {
            for (int i = 0; i < compElmList.Count; i++) {
                compElmList[i].CirElm.CirDoStep();
            }
        }

        public override void CirStepFinished() {
            for (int i = 0; i < compElmList.Count; i++) {
                compElmList[i].CirElm.CirStepFinished();
            }
        }

        public override void CirSetNodeVoltage(int n, double c) {
            base.CirSetNodeVoltage(n, c);
            var cnLinks = compNodeList[n].Links;
            for (int i = 0; i < cnLinks.Count; i++) {
                cnLinks[i].Elm.CirElm.CirSetNodeVoltage(cnLinks[i].Num, c);
            }
            Volts[n] = c;
        }

        public override void CirSetCurrent(int vsn, double c) {
            for (int i = 0; i < voltageSources.Count; i++) {
                if (voltageSources[i].vsNode == vsn) {
                    voltageSources[i].elm.CirElm.CirSetCurrent(vsn, c);
                }
            }
        }

        public override double GetCurrentIntoNode(int n) {
            double c = 0;
            var cnLinks = compNodeList[n].Links;
            for (int i = 0; i < cnLinks.Count; i++) {
                c += cnLinks[i].Elm.CirElm.GetCurrentIntoNode(cnLinks[i].Num);
            }
            return c;
        }

        public void loadComposite(StringTokenizer stIn, string model, int[] externalNodes) {
            var compNodeHash = new Dictionary<int, CircuitNode>();
            var modelLinet = new StringTokenizer(model, "\r");
            CircuitNode cn;
            CircuitNodeLink cnLink;
            VoltageSourceRecord vsRecord;

            compElmList = new List<BaseUI>();
            compNodeList = new List<CircuitNode>();
            voltageSources = new List<VoltageSourceRecord>();

            /* Build compElmList and compNodeHash from input string */

            while (modelLinet.hasMoreTokens()) {
                string line = modelLinet.nextToken();
                var stModel = new StringTokenizer(line, " +\t\n\r\f");
                var ceType = MenuItems.GetItemFromString(stModel.nextToken());
                var newce = MenuItems.ConstructElement(ceType);
                if (stIn != null) {
                    var tint = newce.DumpType;
                    string dumpedCe = stIn.nextToken();
                    if (useEscape) {
                        dumpedCe = CustomLogicModel.unescape(dumpedCe);
                    }
                    var stCe = new StringTokenizer(dumpedCe, useEscape ? " " : "_");
                    // TODO: CompositeElm loadComposite
                    //int flags = stCe.nextTokenInt();
                    int flags = 0;
                    newce = MenuItems.CreateCe(tint, new Point(), new Point(), flags, stCe);
                }
                compElmList.Add(newce);

                int thisPost = 0;
                while (stModel.hasMoreTokens()) {
                    int nodeOfThisPost = stModel.nextTokenInt();
                    cnLink = new CircuitNodeLink();
                    cnLink.Num = thisPost;
                    cnLink.Elm = newce;
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
            numPosts = externalNodes.Length;
            for (int i = 0; i < externalNodes.Length; i++) {
                /* External Nodes First */
                if (compNodeHash.ContainsKey(externalNodes[i])) {
                    compNodeList.Add(compNodeHash[externalNodes[i]]);
                    compNodeHash.Remove(externalNodes[i]);
                } else {
                    throw new Exception();
                }
            }
            foreach (var entry in compNodeHash) {
                int key = entry.Key;
                compNodeList.Add(compNodeHash[key]);
            }

            /* allocate more nodes for sub-elements' internal nodes */
            for (int i = 0; i != compElmList.Count; i++) {
                var ce = compElmList[i];
                int inodes = ce.CirElm.InternalNodeCount;
                for (int j = 0; j != inodes; j++) {
                    cnLink = new CircuitNodeLink();
                    cnLink.Num = j + ce.CirElm.PostCount;
                    cnLink.Elm = ce;
                    cn = new CircuitNode();
                    cn.Links.Add(cnLink);
                    compNodeList.Add(cn);
                }
            }

            numNodes = compNodeList.Count;

            /*Console.WriteLine("Dumping compNodeList");
            for (int i = 0; i < numNodes; i++) {
                Console.WriteLine("New node" + i + " Size of links:" + compNodeList.get(i).links.size());
            }*/

            /* Enumerate voltage sources */
            for (int i = 0; i < compElmList.Count; i++) {
                int cnt = compElmList[i].CirElm.VoltageSourceCount;
                for (int j = 0; j < cnt; j++) {
                    vsRecord = new VoltageSourceRecord();
                    vsRecord.elm = compElmList[i];
                    vsRecord.vsNumForElement = j;
                    voltageSources.Add(vsRecord);
                }
            }

            /* dump new circuits with escape() */
            useEscape = true;
        }
    }
}
