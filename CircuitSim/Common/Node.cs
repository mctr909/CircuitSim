using System;
using System.Collections.Generic;

using Circuit.Elements;

namespace Circuit {
    class CircuitNodeLink {
        public int Num;
        public CircuitElm Elm;
    }

    class CircuitNode {
        public List<CircuitNodeLink> Links = new List<CircuitNodeLink>();
        public bool Internal;
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
         * elm = source and destination element.
         * dest = destination node. */
        public FindPathInfo(int type, CircuitElm elm, int dest, List<CircuitElm> elmList, int nodeCount) {
            mDest = dest;
            mType = type;
            mFirstElm = elm;
            mElmList = elmList;
            mVisited = new bool[nodeCount];
        }

        /* look through circuit for loop starting at node n1 of firstElm,
         * for a path back to dest node of firstElm */
        public bool FindPath(int n1) {
            if (n1 == mDest) {
                return true;
            }

            /* depth first search, don't need to revisit already visited nodes! */
            if (mVisited[n1]) {
                return false;
            }

            mVisited[n1] = true;
            for (int i = 0; i != mElmList.Count; i++) {
                var ce = mElmList[i];
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
                    if (!(ce.IsWire || (ce is VoltageElm) || (ce is GroundElm))) {
                        continue;
                    }
                }
                /* when checking for shorts, just check wires */
                if (mType == SHORT && !ce.IsWire) {
                    continue;
                }
                if (mType == CAP_V) {
                    /* checking for capacitor/voltage source loops */
                    if (!(ce.IsWire || (ce is CapacitorElm) || (ce is VoltageElm))) {
                        continue;
                    }
                }
                if (n1 == 0) {
                    /* look for posts which have a ground connection;
                    /* our path can go through ground */
                    for (int j = 0; j != ce.ConnectionNodeCount; j++) {
                        if (ce.HasGroundConnection(j) && FindPath(ce.GetConnectionNode(j))) {
                            return true;
                        }
                    }
                }

                int nodeA;
                for (nodeA = 0; nodeA != ce.ConnectionNodeCount; nodeA++) {
                    if (ce.GetConnectionNode(nodeA) == n1) {
                        break;
                    }
                }
                if (nodeA == ce.ConnectionNodeCount) {
                    continue;
                }
                if (ce.HasGroundConnection(nodeA) && FindPath(0)) {
                    return true;
                }

                if (mType == INDUCT && (ce is InductorElm)) {
                    /* inductors can use paths with other inductors of matching current */
                    double c = ce.Current;
                    if (nodeA == 0) {
                        c = -c;
                    }
                    if (Math.Abs(c - mFirstElm.Current) > 1e-10) {
                        continue;
                    }
                }

                for (int nodeB = 0; nodeB != ce.ConnectionNodeCount; nodeB++) {
                    if (nodeA == nodeB) {
                        continue;
                    }
                    if (ce.GetConnection(nodeA, nodeB) && FindPath(ce.GetConnectionNode(nodeB))) {
                        /*Console.WriteLine("got findpath " + n1); */
                        return true;
                    }
                }
            }
            return false;
        }
	}
}
