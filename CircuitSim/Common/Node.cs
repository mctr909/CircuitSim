using System;
using System.Collections.Generic;

using Circuit.Elements;

namespace Circuit {
    class CircuitNodeLink {
        public int num;
        public CircuitElm elm;
    }

    class CircuitNode {
        public List<CircuitNodeLink> links;
        public bool _internal;
        public CircuitNode() {
            links = new List<CircuitNodeLink>();
        }
    }

    class FindPathInfo {
        public const int INDUCT = 1;
        public const int VOLTAGE = 2;
        public const int SHORT = 3;
        public const int CAP_V = 4;

        int mType;
        int mDest;
        CircuitElm mFirstElm;
        List<CircuitElm> mElmList;
        bool[] mVisited;

        /* State object to help find loops in circuit subject to various conditions (depending on type)
        /* elm = source and destination element.  dest = destination node. */
        public FindPathInfo(int type, CircuitElm elm, int dest, List<CircuitElm> elmList, int nodeCount) {
            mDest = dest;
            mType = type;
            mFirstElm = elm;
            mElmList = elmList;
            mVisited = new bool[nodeCount];
        }

        CircuitElm getElm(int n) {
            if (n >= mElmList.Count) {
                return null;
            }
            return mElmList[n];
        }

        /* look through circuit for loop starting at node n1 of firstElm, for a path back to
        /* dest node of firstElm */
        public bool findPath(int n1) {
            if (n1 == mDest) {
                return true;
            }

            /* depth first search, don't need to revisit already visited nodes! */
            if (mVisited[n1]) {
                return false;
            }

            mVisited[n1] = true;
            int i;
            for (i = 0; i != mElmList.Count; i++) {
                CircuitElm ce = getElm(i);
                if (ce == mFirstElm) {
                    continue;
                }
                if (mType == INDUCT) {
                    /* inductors need a path free of current sources */
                    if (ce is CurrentElm) {
                        continue;
                    }
                }
                if (mType == VOLTAGE) {
                    /* when checking for voltage loops, we only care about voltage sources/wires/ground */
                    if (!(ce.isWire() || (ce is VoltageElm) || (ce is GroundElm))) {
                        continue;
                    }
                }
                /* when checking for shorts, just check wires */
                if (mType == SHORT && !ce.isWire()) {
                    continue;
                }
                if (mType == CAP_V) {
                    /* checking for capacitor/voltage source loops */
                    if (!(ce.isWire() || (ce is CapacitorElm) || (ce is VoltageElm))) {
                        continue;
                    }
                }
                if (n1 == 0) {
                    /* look for posts which have a ground connection;
                    /* our path can go through ground */
                    for (int j = 0; j != ce.getConnectionNodeCount(); j++) {
                        if (ce.hasGroundConnection(j) && findPath(ce.getConnectionNode(j))) {
                            return true;
                        }
                    }
                }

                int nodeA;
                for (nodeA = 0; nodeA != ce.getConnectionNodeCount(); nodeA++) {
                    if (ce.getConnectionNode(nodeA) == n1) {
                        break;
                    }
                }
                if (nodeA == ce.getConnectionNodeCount()) {
                    continue;
                }
                if (ce.hasGroundConnection(nodeA) && findPath(0)) {
                    return true;
                }

                if (mType == INDUCT && (ce is InductorElm)) {
                    /* inductors can use paths with other inductors of matching current */
                    double c = ce.getCurrent();
                    if (nodeA == 0) {
                        c = -c;
                    }
                    if (Math.Abs(c - mFirstElm.getCurrent()) > 1e-10) {
                        continue;
                    }
                }

                int nodeB;
                for (nodeB = 0; nodeB != ce.getConnectionNodeCount(); nodeB++) {
                    if (nodeA == nodeB) {
                        continue;
                    }
                    if (ce.getConnection(nodeA, nodeB) && findPath(ce.getConnectionNode(nodeB))) {
                        /*Console.WriteLine("got findpath " + n1); */
                        return true;
                    }
                }
            }
            return false;
        }
	}
}
