using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;

namespace Circuit.Elements {
    class VoltageSourceRecord {
        public int vsNumForElement;
        public int vsNode;
        public CircuitElm elm;
    }

    abstract class CompositeElm : CircuitElm {
        /* need to use escape() instead of converting spaces to _'s so composite elements can be nested */
        protected const int FLAG_ESCAPE = 1;

        /* list of elements contained in this subcircuit */
        protected List<CircuitElm> compElmList = new List<CircuitElm>();

        /* list of nodes, mapping each one to a list of elements that reference that node */
        protected List<CircuitNode> compNodeList;

        protected int numPosts = 0;
        protected int numNodes = 0;
        protected Point[] posts;
        protected List<VoltageSourceRecord> voltageSources;

        public CompositeElm(Point pos) : base(pos) { }

        public CompositeElm(Point p1, Point p2, int f) : base(p1, p2, f) { }

        public CompositeElm(Point pos, string s, int[] externalNodes) : base(pos) {
            loadComposite(null, s, externalNodes);
            allocNodes();
        }

        public CompositeElm(Point p1, Point p2, int f, StringTokenizer st, string s, int[] externalNodes) : base(p1, p2, f) {
            loadComposite(st, s, externalNodes);
            allocNodes();
        }

        public override bool CanViewInScope { get { return false; } }

        public override double Power {
            get {
                double power = 0.0;
                for (int i = 0; i < compElmList.Count; i++) {
                    power += compElmList[i].Power;
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

        public override int PostCount { get { return numPosts; } }

        public override abstract DUMP_ID DumpType { get; }

        protected override string dump() {
            return dumpElements();
        }

        protected string dumpElements() {
            string dumpStr = "";
            for (int i = 0; i < compElmList.Count; i++) {
                string tstring = compElmList[i].Dump;
                var rg = new Regex("[A-Za-z0-9]+ 0 0 0 0 ");
                tstring = rg.Replace(tstring, "", 1); /* remove unused tint x1 y1 x2 y2 coords for internal components */
                dumpStr = string.Join(" ", dumpStr, CustomLogicModel.escape(tstring));
            }
            return dumpStr;
        }

        /* dump subset of elements
         * (some of them may not have any state, and/or may be very long, so we avoid dumping them for brevity) */
        protected string dumpWithMask(int mask) {
            return dumpElements(mask);
        }

        protected string dumpElements(int mask) {
            string dumpStr = "";
            for (int i = 0; i < compElmList.Count; i++) {
                if ((mask & (1 << i)) == 0) {
                    continue;
                }
                string tstring = compElmList[i].Dump;
                var rg = new Regex("[A-Za-z0-9]+ 0 0 0 0 ");
                tstring = rg.Replace(tstring, "", 1); /* remove unused tint x1 y1 x2 y2 coords for internal components */
                dumpStr += " " + CustomLogicModel.escape(tstring);
            }
            return dumpStr;
        }

        bool useEscape() { return (mFlags & FLAG_ESCAPE) != 0; }

        public void loadComposite(StringTokenizer stIn, string model, int[] externalNodes) {
            var compNodeHash = new Dictionary<int, CircuitNode>();
            var modelLinet = new StringTokenizer(model, "\r");
            CircuitNode cn;
            CircuitNodeLink cnLink;
            VoltageSourceRecord vsRecord;

            compElmList = new List<CircuitElm>();
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
                    if (useEscape()) {
                        dumpedCe = CustomLogicModel.unescape(dumpedCe);
                    }
                    var stCe = new StringTokenizer(dumpedCe, useEscape() ? " " : "_");
                    // TODO: loadComposite
                    int flags = 0; //stCe.nextTokenInt();
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
                int inodes = ce.InternalNodeCount;
                for (int j = 0; j != inodes; j++) {
                    cnLink = new CircuitNodeLink();
                    cnLink.Num = j + ce.PostCount;
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

            posts = new Point[numPosts];

            /* Enumerate voltage sources */
            for (int i = 0; i < compElmList.Count; i++) {
                int cnt = compElmList[i].VoltageSourceCount;
                for (int j = 0; j < cnt; j++) {
                    vsRecord = new VoltageSourceRecord();
                    vsRecord.elm = compElmList[i];
                    vsRecord.vsNumForElement = j;
                    voltageSources.Add(vsRecord);
                }
            }

            /* dump new circuits with escape() */
            mFlags |= FLAG_ESCAPE;
        }

        /* are n1 and n2 connected internally somehow? */
        public override bool GetConnection(int n1, int n2) {
            var cnLinks1 = compNodeList[n1].Links;
            var cnLinks2 = compNodeList[n2].Links;

            /* see if any elements are connected to both n1 and n2, then call getConnection() on those */
            for (int i = 0; i < cnLinks1.Count; i++) {
                CircuitNodeLink link1 = cnLinks1[i];
                for (int j = 0; j < cnLinks2.Count; j++) {
                    CircuitNodeLink link2 = cnLinks2[j];
                    if (link1.Elm == link2.Elm && link1.Elm.GetConnection(link1.Num, link2.Num)) {
                        return true;
                    }
                }
            }
            return false;
        }

        /* is n1 connected to ground somehow? */
        public override bool HasGroundConnection(int n1) {
            List<CircuitNodeLink> cnLinks;
            cnLinks = compNodeList[n1].Links;
            for (int i = 0; i < cnLinks.Count; i++) {
                if (cnLinks[i].Elm.HasGroundConnection(cnLinks[i].Num)) {
                    return true;
                }
            }
            return false;
        }

        public override void Reset() {
            for (int i = 0; i < compElmList.Count; i++) {
                compElmList[i].Reset();
            }
        }

        public override Point GetPost(int n) {
            return posts[n];
        }

        protected void setPost(int n, Point p) {
            posts[n] = p;
        }

        void setPost(int n, int x, int y) {
            posts[n].X = x;
            posts[n].Y = y;
        }

        public override void Stamp() {
            for (int i = 0; i < compElmList.Count; i++) {
                var ce = compElmList[i];
                /* current sources need special stamp method */
                if (ce is CurrentElm) {
                    ((CurrentElm)ce).stampCurrentSource(false);
                } else {
                    ce.Stamp();
                }
            }
        }

        public override void StartIteration() {
            for (int i = 0; i < compElmList.Count; i++) {
                compElmList[i].StartIteration();
            }
        }

        public override void DoStep() {
            for (int i = 0; i < compElmList.Count; i++) {
                compElmList[i].DoStep();
            }
        }

        public override void StepFinished() {
            for (int i = 0; i < compElmList.Count; i++) {
                compElmList[i].StepFinished();
            }
        }

        public override void SetNode(int p, int n) {
            base.SetNode(p, n);
            var cnLinks = compNodeList[p].Links;
            for (int i = 0; i < cnLinks.Count; i++) {
                cnLinks[i].Elm.SetNode(cnLinks[i].Num, n);
            }
        }

        public override void SetNodeVoltage(int n, double c) {
            base.SetNodeVoltage(n, c);
            var cnLinks = compNodeList[n].Links;
            for (int i = 0; i < cnLinks.Count; i++) {
                cnLinks[i].Elm.SetNodeVoltage(cnLinks[i].Num, c);
            }
            Volts[n] = c;
        }

        public override void Delete() {
            for (int i = 0; i < compElmList.Count; i++) {
                compElmList[i].Delete();
            }
            base.Delete();
        }

        /* Find the component with the nth voltage
         * and set the
         * appropriate source in that component */
        public override void SetVoltageSource(int n, int v) {
            var vsr = voltageSources[n];
            vsr.elm.SetVoltageSource(vsr.vsNumForElement, v);
            vsr.vsNode = v;
        }

        public override void SetCurrent(int vsn, double c) {
            for (int i = 0; i < voltageSources.Count; i++) {
                if (voltageSources[i].vsNode == vsn) {
                    voltageSources[i].elm.SetCurrent(vsn, c);
                }
            }
        }

        public override double GetCurrentIntoNode(int n) {
            double c = 0;
            var cnLinks = compNodeList[n].Links;
            for (int i = 0; i < cnLinks.Count; i++) {
                c += cnLinks[i].Elm.GetCurrentIntoNode(cnLinks[i].Num);
            }
            return c;
        }
    }
}
