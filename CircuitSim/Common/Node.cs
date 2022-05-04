using System;
using System.Collections.Generic;

using Circuit.Elements;
using Circuit.Elements.Passive;
using Circuit.Elements.Input;

namespace Circuit {
    class CircuitNodeLink {
        public int Num;
        public CircuitElm Elm;
    }

    class CircuitNode {
        public List<CircuitNodeLink> Links = new List<CircuitNodeLink>();
        public bool Internal;
    }

    class NodeMapEntry {
        public int Node;
        public NodeMapEntry() { Node = -1; }
        public NodeMapEntry(int n) { Node = n; }
    }

    class WireInfo {
        public WireElm Wire;
        public List<CircuitElm> Neighbors;
        public int Post;
        public WireInfo(WireElm w) { Wire = w; }
    }

    enum PathType {
        INDUCTOR = 1,
        VOLTAGE = 2,
        SHORT = 3,
        CAPACITOR_V = 4
    }

    class PathInfo {
        PathType mType;
        int mDest;
        BaseElement mFirstElm;
        List<CircuitElm> mElmList;
        bool[] mVisited;

        /* State object to help find loops in circuit subject to various conditions (depending on type)
         * elm = source and destination element.
         * dest = destination node. */
        public PathInfo(PathType type, BaseElement elm, int dest, List<CircuitElm> elmList, int nodeCount) {
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
                var cee = ce.CirElm;
                if (cee == mFirstElm) {
                    continue;
                }
                switch (mType) {
                case PathType.INDUCTOR:
                    /* inductors need a path free of current sources */
                    if (cee is CurrentElmE) {
                        continue;
                    }
                    break;
                case PathType.VOLTAGE:
                    /* when checking for voltage loops, we only care about voltage sources/wires/ground */
                    if (!(cee.CirIsWire || (cee is VoltageElmE) || (cee is GroundElmE))) {
                        continue;
                    }
                    break;
                /* when checking for shorts, just check wires */
                case PathType.SHORT:
                    if (!cee.CirIsWire) {
                        continue;
                    }
                    break;
                case PathType.CAPACITOR_V:
                    /* checking for capacitor/voltage source loops */
                    if (!(cee.CirIsWire || (cee is CapacitorElmE) || (cee is VoltageElmE))) {
                        continue;
                    }
                    break;
                }

                if (n1 == 0) {
                    /* look for posts which have a ground connection;
                    /* our path can go through ground */
                    for (int j = 0; j != cee.CirConnectionNodeCount; j++) {
                        if (cee.CirHasGroundConnection(j) && FindPath(cee.CirGetConnectionNode(j))) {
                            return true;
                        }
                    }
                }

                int nodeA;
                for (nodeA = 0; nodeA != cee.CirConnectionNodeCount; nodeA++) {
                    if (cee.CirGetConnectionNode(nodeA) == n1) {
                        break;
                    }
                }
                if (nodeA == cee.CirConnectionNodeCount) {
                    continue;
                }
                if (cee.CirHasGroundConnection(nodeA) && FindPath(0)) {
                    return true;
                }

                if (mType == PathType.INDUCTOR && (cee is InductorElmE)) {
                    /* inductors can use paths with other inductors of matching current */
                    double c = cee.CirCurrent;
                    if (nodeA == 0) {
                        c = -c;
                    }
                    if (Math.Abs(c - mFirstElm.CirCurrent) > 1e-10) {
                        continue;
                    }
                }

                for (int nodeB = 0; nodeB != cee.CirConnectionNodeCount; nodeB++) {
                    if (nodeA == nodeB) {
                        continue;
                    }
                    if (ce.GetConnection(nodeA, nodeB) && FindPath(cee.CirGetConnectionNode(nodeB))) {
                        /*Console.WriteLine("got findpath " + n1); */
                        return true;
                    }
                }
            }
            return false;
        }
	}
}
