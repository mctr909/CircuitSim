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

    class CompositeElm : CircuitElm {
        /* need to use escape() instead of converting spaces to _'s so composite elements can be nested */
        protected const int FLAG_ESCAPE = 1;

        /* list of elements contained in this subcircuit */
        protected List<CircuitElm> compElmList;

        /* list of nodes, mapping each one to a list of elements that reference that node */
        protected List<CircuitNode> compNodeList;

        protected int numPosts = 0;
        protected int numNodes = 0;
        protected Point[] posts;
        protected List<VoltageSourceRecord> voltageSources;

        public CompositeElm(int xx, int yy) : base(xx, yy) { }

        public CompositeElm(int xa, int ya, int xb, int yb, int f) : base(xa, ya, xb, yb, f) { }

        public CompositeElm(int xx, int yy, string s, int[] externalNodes) : base(xx, yy) {
            loadComposite(null, s, externalNodes);
            allocNodes();
        }

        public CompositeElm(int xa, int ya, int xb, int yb, int f, 
            StringTokenizer st, string s, int[] externalNodes) : base(xa, ya, xb, yb, f) {
            loadComposite(st, s, externalNodes);
            allocNodes();
        }

        protected override string dump() {
            return dumpElements();
        }

        protected override DUMP_ID getDumpType() { return DUMP_ID.INVALID; }

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
                var ceType = MenuItems.getItemFromString(stModel.nextToken());
                var newce = MenuItems.constructElement(ceType, 0, 0);
                if (stIn != null) {
                    var tint = newce.DumpType;
                    string dumpedCe = stIn.nextToken();
                    if (useEscape()) {
                        dumpedCe = CustomLogicModel.unescape(dumpedCe);
                    }
                    var stCe = new StringTokenizer(dumpedCe, useEscape() ? " " : "_");
                    int flags = stCe.nextTokenInt();
                    newce = MenuItems.createCe(tint, 0, 0, 0, 0, flags, stCe);
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
                int inodes = ce.getInternalNodeCount();
                for (int j = 0; j != inodes; j++) {
                    cnLink = new CircuitNodeLink();
                    cnLink.Num = j + ce.getPostCount();
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
                int cnt = compElmList[i].getVoltageSourceCount();
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

        public override bool nonLinear() {
            return true; /* Lets assume that any useful composite elements are
                         /* non-linear */
        }

        /* are n1 and n2 connected internally somehow? */
        public override bool getConnection(int n1, int n2) {
            var cnLinks1 = compNodeList[n1].Links;
            var cnLinks2 = compNodeList[n2].Links;

            /* see if any elements are connected to both n1 and n2, then call getConnection() on those */
            for (int i = 0; i < cnLinks1.Count; i++) {
                CircuitNodeLink link1 = cnLinks1[i];
                for (int j = 0; j < cnLinks2.Count; j++) {
                    CircuitNodeLink link2 = cnLinks2[j];
                    if (link1.Elm == link2.Elm && link1.Elm.getConnection(link1.Num, link2.Num)) {
                        return true;
                    }
                }
            }
            return false;
        }

        /* is n1 connected to ground somehow? */
        public override bool hasGroundConnection(int n1) {
            List<CircuitNodeLink> cnLinks;
            cnLinks = compNodeList[n1].Links;
            for (int i = 0; i < cnLinks.Count; i++) {
                if (cnLinks[i].Elm.hasGroundConnection(cnLinks[i].Num)) {
                    return true;
                }
            }
            return false;
        }

        public override void reset() {
            for (int i = 0; i < compElmList.Count; i++) {
                compElmList[i].reset();
            }
        }

        public override int getPostCount() {
            return numPosts;
        }

        public override int getInternalNodeCount() {
            return numNodes - numPosts;
        }

        public override Point getPost(int n) {
            return posts[n];
        }

        protected void setPost(int n, Point p) {
            posts[n] = p;
        }

        void setPost(int n, int x, int y) {
            posts[n].X = x;
            posts[n].Y = y;
        }

        public override double getPower() {
            double power;
            power = 0;
            for (int i = 0; i < compElmList.Count; i++) {
                power += compElmList[i].getPower();
            }
            return power;
        }

        public override void stamp() {
            for (int i = 0; i < compElmList.Count; i++) {
                var ce = compElmList[i];
                /* current sources need special stamp method */
                if (ce is CurrentElm) {
                    ((CurrentElm)ce).stampCurrentSource(false);
                } else {
                    ce.stamp();
                }
            }
        }

        public override void startIteration() {
            for (int i = 0; i < compElmList.Count; i++) {
                compElmList[i].startIteration();
            }
        }

        public override void doStep() {
            for (int i = 0; i < compElmList.Count; i++) {
                compElmList[i].doStep();
            }
        }

        public override void stepFinished() {
            for (int i = 0; i < compElmList.Count; i++) {
                compElmList[i].stepFinished();
            }
        }

        public override void setNode(int p, int n) {
            base.setNode(p, n);
            var cnLinks = compNodeList[p].Links;
            for (int i = 0; i < cnLinks.Count; i++) {
                cnLinks[i].Elm.setNode(cnLinks[i].Num, n);
            }
        }

        public override void setNodeVoltage(int n, double c) {
            base.setNodeVoltage(n, c);
            var cnLinks = compNodeList[n].Links;
            for (int i = 0; i < cnLinks.Count; i++) {
                cnLinks[i].Elm.setNodeVoltage(cnLinks[i].Num, c);
            }
            Volts[n] = c;
        }

        public override bool canViewInScope() {
            return false;
        }

        public override void delete() {
            for (int i = 0; i < compElmList.Count; i++) {
                compElmList[i].delete();
            }
            base.delete();
        }

        public override int getVoltageSourceCount() {
            return voltageSources.Count;
        }

        /* Find the component with the nth voltage
         * and set the
         * appropriate source in that component */
        public override void setVoltageSource(int n, int v) {
            var vsr = voltageSources[n];
            vsr.elm.setVoltageSource(vsr.vsNumForElement, v);
            vsr.vsNode = v;
        }

        public override void setCurrent(int vsn, double c) {
            for (int i = 0; i < voltageSources.Count; i++) {
                if (voltageSources[i].vsNode == vsn) {
                    voltageSources[i].elm.setCurrent(vsn, c);
                }
            }
        }

        public override double getCurrentIntoNode(int n) {
            double c = 0;
            var cnLinks = compNodeList[n].Links;
            for (int i = 0; i < cnLinks.Count; i++) {
                c += cnLinks[i].Elm.getCurrentIntoNode(cnLinks[i].Num);
            }
            return c;
        }
    }
}
