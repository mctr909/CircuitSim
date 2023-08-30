﻿using System;
using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements;
using Circuit.Elements.Passive;
using Circuit.Elements.Input;
using Circuit.Elements.Output;

namespace Circuit {
    class CircuitNode {
        public struct LINK {
            public int Num;
            public BaseElement Elm;
        }
        public List<LINK> Links = new List<LINK>();
        public bool Internal;
    }

    class NodeMapEntry {
        public int Node;
        public NodeMapEntry() { Node = -1; }
        public NodeMapEntry(int n) { Node = n; }
    }

    class WireInfo {
        public ElmWire Wire;
        public List<BaseElement> Neighbors;
        public int Post;
        public WireInfo(ElmWire w) { Wire = w; }
    }

    class PathInfo {
        public enum TYPE {
            INDUCTOR = 1,
            VOLTAGE = 2,
            SHORT = 3,
            CAPACITOR_V = 4
        }

        TYPE mType;
        int mDest;
        BaseElement mFirstElm;
        List<BaseElement> mElmList;
        bool[] mVisited;

        /* State object to help find loops in circuit subject to various conditions (depending on type)
         * elm = source and destination element.
         * dest = destination node. */
        public PathInfo(TYPE type, BaseElement elm, int dest, List<BaseElement> elmList, int nodeCount) {
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
                var cee = mElmList[i];
                if (cee == mFirstElm) {
                    continue;
                }
                switch (mType) {
                case TYPE.INDUCTOR:
                    /* inductors need a path free of current sources */
                    if (cee is ElmCurrent) {
                        continue;
                    }
                    break;
                case TYPE.VOLTAGE:
                    /* when checking for voltage loops, we only care about voltage sources/wires/ground */
                    if (!(cee.IsWire || (cee is ElmVoltage) || (cee is ElmGround))) {
                        continue;
                    }
                    break;
                /* when checking for shorts, just check wires */
                case TYPE.SHORT:
                    if (!cee.IsWire) {
                        continue;
                    }
                    break;
                case TYPE.CAPACITOR_V:
                    /* checking for capacitor/voltage source loops */
                    if (!(cee.IsWire || (cee is ElmCapacitor) || (cee is ElmVoltage))) {
                        continue;
                    }
                    break;
                }

                if (n1 == 0) {
                    /* look for posts which have a ground connection;
                    /* our path can go through ground */
                    for (int j = 0; j != cee.AnaConnectionNodeCount; j++) {
                        if (cee.AnaHasGroundConnection(j) && FindPath(cee.AnaGetConnectionNode(j))) {
                            return true;
                        }
                    }
                }

                int nodeA;
                for (nodeA = 0; nodeA != cee.AnaConnectionNodeCount; nodeA++) {
                    if (cee.AnaGetConnectionNode(nodeA) == n1) {
                        break;
                    }
                }
                if (nodeA == cee.AnaConnectionNodeCount) {
                    continue;
                }
                if (cee.AnaHasGroundConnection(nodeA) && FindPath(0)) {
                    return true;
                }

                if (mType == TYPE.INDUCTOR && (cee is ElmInductor)) {
                    /* inductors can use paths with other inductors of matching current */
                    double c = cee.Current;
                    if (nodeA == 0) {
                        c = -c;
                    }
                    if (Math.Abs(c - mFirstElm.Current) > 1e-10) {
                        continue;
                    }
                }

                for (int nodeB = 0; nodeB != cee.AnaConnectionNodeCount; nodeB++) {
                    if (nodeA == nodeB) {
                        continue;
                    }
                    if (cee.AnaGetConnection(nodeA, nodeB) && FindPath(cee.AnaGetConnectionNode(nodeB))) {
                        /*Console.WriteLine("got findpath " + n1); */
                        return true;
                    }
                }
            }
            return false;
        }
    }

    static class Circuit {
        public class ROW_INFO {
            public bool IsConst;
            public bool RightChanges; /* row's right side changes */
            public bool LeftChanges;  /* row's left side changes */
            public bool DropRow;      /* row is not needed in matrix */
            public int MapCol;
            public int MapRow;
            public double Value;
        }

        const int SubIterMax = 1000;
        const bool DEBUG = false;

        public static double[,] Matrix;
        public static double[] RightSide;
        public static ROW_INFO[] RowInfo;
        public static List<CircuitNode> Nodes;

        #region private varidate
        static Dictionary<Point, NodeMapEntry> mNodeMap;
        static Dictionary<Point, int> mPostCountMap;

        /* info about each wire and its neighbors, used to calculate wire currents */
        static List<WireInfo> mWireInfoList;
        static BaseElement[] mVoltageSources;
        static List<BaseElement> mElmList = new List<BaseElement>();

        static bool mCircuitNeedsMap;

        static int mMatrixSize;
        static int mMatrixFullSize;
        static int[] mPermute;
        static double[] mOrigRightSide;
        static double[,] mOrigMatrix;
        #endregion

        #region property
        public static List<Point> DrawPostList { get; private set; } = new List<Point>();
        public static List<Point> UndrawPostList { get; private set; } = new List<Point>();
        public static List<Point> BadConnectionList { get; private set; } = new List<Point>();
        public static BaseElement StopElm { get; set; }
        public static double Time { get; set; }
        public static string StopMessage { get; set; }
        public static bool Converged { get; set; }
        public static int SubIterations { get; private set; }
        #endregion

        #region private method
        /* simplify the matrix; this speeds things up quite a bit, especially for digital circuits */
        static bool simplifyMatrix(int matrixSize) {
            int matRow;
            int matCol;
            for (matRow = 0; matRow != matrixSize; matRow++) {
                int qp = -1;
                double qv = 0;
                var re = RowInfo[matRow];
                /*Console.WriteLine("row " + i + " " + re.lsChanges + " " + re.rsChanges + " " + re.dropRow);*/
                if (re.LeftChanges || re.DropRow || re.RightChanges) {
                    continue;
                }
                double rsadd = 0;

                /* look for rows that can be removed */
                for (matCol = 0; matCol != matrixSize; matCol++) {
                    double q = Matrix[matRow, matCol];
                    if (RowInfo[matCol].IsConst) {
                        /* keep a running total of const values that have been
                        /* removed already */
                        rsadd -= RowInfo[matCol].Value * q;
                        continue;
                    }
                    /* ignore zeroes */
                    if (q == 0) {
                        continue;
                    }
                    /* keep track of first nonzero element that is not ROW_CONST */
                    if (qp == -1) {
                        qp = matCol;
                        qv = q;
                        continue;
                    }
                    /* more than one nonzero element?  give up */
                    break;
                }
                if (matCol == matrixSize) {
                    if (qp == -1) {
                        /* probably a singular matrix, try disabling matrix simplification above to check this */
                        stop("Matrix error");
                        return false;
                    }
                    var elt = RowInfo[qp];
                    /* we found a row with only one nonzero nonconst entry; that value is a constant */
                    if (elt.IsConst) {
                        Console.WriteLine("type already CONST for " + qp + "!");
                        continue;
                    }
                    elt.IsConst = true;
                    elt.Value = (RightSide[matRow] + rsadd) / qv;
                    RowInfo[matRow].DropRow = true;
                    matRow = -1; /* start over from scratch */
                }
            }

            /* find size of new matrix */
            int nn = 0;
            for (matRow = 0; matRow != matrixSize; matRow++) {
                var elt = RowInfo[matRow];
                if (elt.IsConst) {
                    elt.MapCol = -1;
                } else {
                    elt.MapCol = nn++;
                    continue;
                }
            }

            /* make the new, simplified matrix */
            int newSize = nn;
            var newMat = new double[newSize, newSize];
            var newRS = new double[newSize];
            int ii = 0;
            for (matRow = 0; matRow != matrixSize; matRow++) {
                var rri = RowInfo[matRow];
                if (rri.DropRow) {
                    rri.MapRow = -1;
                    continue;
                }
                newRS[ii] = RightSide[matRow];
                rri.MapRow = ii;
                for (matCol = 0; matCol != matrixSize; matCol++) {
                    var ri = RowInfo[matCol];
                    if (ri.IsConst) {
                        newRS[ii] -= ri.Value * Matrix[matRow, matCol];
                    } else {
                        newMat[ii, ri.MapCol] += Matrix[matRow, matCol];
                    }
                }
                ii++;
            }
            /*Console.WriteLine("old size = " + matrixSize + " new size = " + newSize);*/

            Matrix = newMat;
            RightSide = newRS;
            matrixSize = mMatrixSize = newSize;
            for (matRow = 0; matRow != matrixSize; matRow++) {
                mOrigRightSide[matRow] = RightSide[matRow];
            }
            for (matRow = 0; matRow != matrixSize; matRow++) {
                for (matCol = 0; matCol != matrixSize; matCol++) {
                    mOrigMatrix[matRow, matCol] = Matrix[matRow, matCol];
                }
            }
            mCircuitNeedsMap = true;
            return true;
        }

        /* find groups of nodes connected by wires and map them to the same node.  this speeds things
        /* up considerably by reducing the size of the matrix */
        static void calculateWireClosure() {
            int mergeCount = 0;
            mNodeMap = new Dictionary<Point, NodeMapEntry>();
            mWireInfoList = new List<WireInfo>();
            for (int i = 0; i < mElmList.Count; i++) {
                var ce = mElmList[i];
                if (!(ce is ElmWire)) {
                    continue;
                }
                var elm = (ElmWire)ce;
                elm.HasWireInfo = false;
                mWireInfoList.Add(new WireInfo(elm));
                var p1 = elm.GetPost(0);
                var p2 = elm.GetPost(1);
                var cp1 = mNodeMap.ContainsKey(p1);
                var cp2 = mNodeMap.ContainsKey(p2);
                if (cp1 && cp2) {
                    var cn1 = mNodeMap[p1];
                    var cn2 = mNodeMap[p2];
                    /* merge nodes; go through map and change all keys pointing to cn2 to point to cn */
                    var tmp = new Dictionary<Point, NodeMapEntry>();
                    foreach (var entry in mNodeMap) {
                        if (entry.Value.Equals(cn2)) {
                            tmp.Add(entry.Key, cn1);
                        }
                    }
                    foreach (var entry in tmp) {
                        mNodeMap[entry.Key] = entry.Value;
                    }
                    tmp.Clear();
                    mergeCount++;
                    continue;
                }
                if (cp1) {
                    var cn1 = mNodeMap[p1];
                    mNodeMap.Add(p2, cn1);
                    continue;
                }
                if (cp2) {
                    var cn2 = mNodeMap[p2];
                    mNodeMap.Add(p1, cn2);
                    continue;
                }
                /* new entry */
                var cn = new NodeMapEntry();
                mNodeMap.Add(p1, cn);
                mNodeMap.Add(p2, cn);
            }
            /*Console.WriteLine("groups with " + mNodeMap.Count + " nodes " + mergeCount);*/
        }

        /* generate info we need to calculate wire currents.  Most other elements calculate currents using
        /* the voltage on their terminal nodes.  But wires have the same voltage at both ends, so we need
        /* to use the neighbors' currents instead.  We used to treat wires as zero voltage sources to make
        /* this easier, but this is very inefficient, since it makes the matrix 2 rows bigger for each wire.
        /* So we create a list of WireInfo objects instead to help us calculate the wire currents instead,
        /* so we make the matrix less complex, and we only calculate the wire currents when we need them
        /* (once per frame, not once per subiteration) */
        static bool calcWireInfo() {
            int wireIdx;
            int moved = 0;
            for (wireIdx = 0; wireIdx != mWireInfoList.Count; wireIdx++) {
                var wi = mWireInfoList[wireIdx];
                var wire = wi.Wire;
                var cn1 = Nodes[wire.Nodes[0]];  /* both ends of wire have same node # */
                int j;

                var neighbors0 = new List<BaseElement>();
                var neighbors1 = new List<BaseElement>();
                bool isReady0 = true;
                bool isReady1 = true;

                /* go through elements sharing a node with this wire (may be connected indirectly
                /* by other wires, but at least it's faster than going through all elements) */
                for (j = 0; j != cn1.Links.Count; j++) {
                    var cnl = cn1.Links[j];
                    var ce = cnl.Elm;
                    if (ce == wire) {
                        continue;
                    }

                    /* is this a wire that doesn't have wire info yet?  If so we can't use it.
                    /* That would create a circular dependency */
                    bool notReady = (ce is ElmWire) && !((ElmWire)ce).HasWireInfo;

                    var pt = ce.GetPost(cnl.Num);

                    /* which post does this element connect to, if any? */
                    if (pt.X == wire.Post[0].X && pt.Y == wire.Post[0].Y) {
                        neighbors0.Add(ce);
                        if (notReady) {
                            isReady0 = false;
                        }
                    } else if (pt.X == wire.Post[1].X && pt.Y == wire.Post[1].Y) {
                        neighbors1.Add(ce);
                        if (notReady) {
                            isReady1 = false;
                        }
                    }
                }

                /* does one of the posts have all information necessary to calculate current */
                if (isReady0) {
                    wi.Neighbors = neighbors0;
                    wi.Post = 0;
                    wire.HasWireInfo = true;
                    moved = 0;
                } else if (isReady1) {
                    wi.Neighbors = neighbors1;
                    wi.Post = 1;
                    wire.HasWireInfo = true;
                    moved = 0;
                } else {
                    /* move to the end of the list and try again later */
                    var tmp = mWireInfoList[wireIdx];
                    mWireInfoList.RemoveAt(wireIdx--);
                    mWireInfoList.Add(tmp);
                    moved++;
                    if (moved > mWireInfoList.Count * 2) {
                        Stop("wire loop detected", wire);
                        return false;
                    }
                }
            }

            return true;
        }

        /* make list of posts we need to draw.  posts shared by 2 elements should be hidden, all
        /* others should be drawn.  We can't use the node list anymore because wires have the same
        /* node number at both ends. */
        static void makePostDrawList() {
            DrawPostList = new List<Point>();
            UndrawPostList = new List<Point>();
            BadConnectionList = new List<Point>();
            foreach (var entry in mPostCountMap) {
                if (2 == entry.Value) {
                    UndrawPostList.Add(entry.Key);
                } else {
                    DrawPostList.Add(entry.Key);
                }
                /* look for bad connections, posts not connected to other elements which intersect
                /* other elements' bounding boxes */
                if (entry.Value == 1) {
                    bool bad = false;
                    var cn = entry.Key;
                    for (int j = 0; j != mElmList.Count && !bad; j++) {
                        var ce = mElmList[j];
                        /* does this post belong to the elm? */
                        int k;
                        int pc = ce.PostCount;
                        for (k = 0; k != pc; k++) {
                            if (ce.GetPost(k).Equals(cn)) {
                                break;
                            }
                        }
                        if (k == pc) {
                            bad = true;
                        }
                    }
                    if (bad) {
                        BadConnectionList.Add(cn);
                    }
                }
            }
            mPostCountMap = null;
        }

        static void stop(string s) {
            StopMessage = s;
            Matrix = null;  /* causes an exception */
            StopElm = null;
            CirSimForm.SetSimRunning(false);
        }

        static CircuitNode getCircuitNode(int n) {
            if (n >= Nodes.Count) {
                return null;
            }
            return Nodes[n];
        }

        /* factors a matrix into upper and lower triangular matrices by
        /* gaussian elimination.  On entry, a[0..n-1][0..n-1] is the
        /* matrix to be factored.  ipvt[] returns an integer vector of pivot
        /* indices, used in the lu_solve() routine. */
        static bool luFactor(double[,] a, int n, int[] ipvt) {
            /* check for a possible singular matrix by scanning for rows that
            /* are all zeroes */
            for (int i = 0; i != n; i++) {
                bool row_all_zeros = true;
                for (int j = 0; j != n; j++) {
                    if (a[i, j] != 0) {
                        row_all_zeros = false;
                        break;
                    }
                }
                /* if all zeros, it's a singular matrix */
                if (row_all_zeros) {
                    return false;
                }
            }

            /* use Crout's method; loop through the columns */
            for (int j = 0; j != n; j++) {
                /* calculate upper triangular elements for this column */
                for (int i = 0; i != j; i++) {
                    double q = a[i, j];
                    for (int k = 0; k != i; k++) {
                        q -= a[i, k] * a[k, j];
                    }
                    a[i, j] = q;
                }
                /* calculate lower triangular elements for this column */
                double largest = 0;
                int largestRow = -1;
                for (int i = j; i != n; i++) {
                    double q = a[i, j];
                    for (int k = 0; k != j; k++) {
                        q -= a[i, k] * a[k, j];
                    }
                    a[i, j] = q;
                    double x = Math.Abs(q);
                    if (x >= largest) {
                        largest = x;
                        largestRow = i;
                    }
                }
                /* pivoting */
                if (j != largestRow) {
                    double x;
                    for (int k = 0; k != n; k++) {
                        x = a[largestRow, k];
                        a[largestRow, k] = a[j, k];
                        a[j, k] = x;
                    }
                }
                /* keep track of row interchanges */
                ipvt[j] = largestRow;
                /* avoid zeros */
                if (a[j, j] == 0.0) {
                    Console.WriteLine("avoided zero");
                    a[j, j] = 1e-18;
                }
                if (j != n - 1) {
                    double mult = 1.0 / a[j, j];
                    for (int i = j + 1; i != n; i++) {
                        a[i, j] *= mult;
                    }
                }
            }
            return true;
        }

        /* Solves the set of n linear equations using a LU factorization
        /* previously performed by lu_factor.  On input, b[0..n-1] is the right
        /* hand side of the equations, and on output, contains the solution. */
        static void luSolve(double[,] a, int n, int[] ipvt, double[] b) {
            int i;

            /* find first nonzero b element */
            for (i = 0; i != n; i++) {
                int row = ipvt[i];
                double swap = b[row];
                b[row] = b[i];
                b[i] = swap;
                if (swap != 0) {
                    break;
                }
            }

            int bi = i++;
            for (; i < n; i++) {
                int row = ipvt[i];
                double tot = b[row];
                b[row] = b[i];
                /* forward substitution using the lower triangular matrix */
                for (int j = bi; j < i; j++) {
                    tot -= a[i, j] * b[j];
                }
                b[i] = tot;
            }
            for (i = n - 1; i >= 0; i--) {
                double tot = b[i];
                /* back-substitution using the upper triangular matrix */
                for (int j = i + 1; j != n; j++) {
                    tot -= a[i, j] * b[j];
                }
                b[i] = tot / a[i, i];
            }
        }
        #endregion

        #region public method
        public static void ClearElm() {
            mElmList.Clear();
        }

        public static void AddElm(BaseElement elm) {
            mElmList.Add(elm);
        }

        public static void AnalyzeCircuit() {
            if (0 == mElmList.Count) {
                DrawPostList = new List<Point>();
                UndrawPostList = new List<Point>();
                BadConnectionList = new List<Point>();
                return;
            }

            StopMessage = null;
            StopElm = null;
            Nodes = new List<CircuitNode>();
            mPostCountMap = new Dictionary<Point, int>();

            calculateWireClosure();

            {
                /* look for voltage or ground element */
                bool gotGround = false;
                bool gotRail = false;
                BaseElement volt = null;
                for (int i = 0; i != mElmList.Count; i++) {
                    var ce = mElmList[i];
                    if (ce is ElmGround) {
                        gotGround = true;
                        break;
                    }
                    if (ce is ElmRail) {
                        gotRail = true;
                    }
                    if (volt == null && (ce is ElmVoltage)) {
                        volt = ce;
                    }
                }

                /* if no ground, and no rails, then the voltage elm's first terminal
                /* is ground */
                if (!gotGround && volt != null && !gotRail) {
                    var cn = new CircuitNode();
                    var pt = volt.GetPost(0);
                    Nodes.Add(cn);
                    /* update node map */
                    if (mNodeMap.ContainsKey(pt)) {
                        mNodeMap[pt].Node = 0;
                    } else {
                        mNodeMap.Add(pt, new NodeMapEntry(0));
                    }
                } else {
                    /* otherwise allocate extra node for ground */
                    var cn = new CircuitNode();
                    Nodes.Add(cn);
                }
            }

            /* allocate nodes and voltage sources */
            int vscount = 0;
            {
                ElmLabeledNode.ResetNodeList();
                for (int i = 0; i < mElmList.Count; i++) {
                    var ce = mElmList[i];
                    if (null == ce) {
                        continue;
                    }
                    int inodes = ce.AnaInternalNodeCount;
                    int ivs = ce.AnaVoltageSourceCount;
                    int posts = ce.PostCount;

                    /* allocate a node for each post and match posts to nodes */
                    for (int j = 0; j < posts; j++) {
                        var pt = ce.GetPost(j);
                        if (mPostCountMap.ContainsKey(pt)) {
                            int g = mPostCountMap[pt];
                            mPostCountMap[pt] = g + 1;
                        } else {
                            mPostCountMap.Add(pt, 1);
                        }

                        NodeMapEntry cln = null;
                        var ccln = mNodeMap.ContainsKey(pt);
                        if (ccln) {
                            cln = mNodeMap[pt];
                        }

                        /* is this node not in map yet?  or is the node number unallocated?
                        /* (we don't allocate nodes before this because changing the allocation order
                        /* of nodes changes circuit behavior and breaks backward compatibility;
                        /* the code below to connect unconnected nodes may connect a different node to ground) */
                        if (!ccln || cln.Node == -1) {
                            var cn = new CircuitNode();
                            var cnl = new CircuitNode.LINK();
                            cnl.Num = j;
                            cnl.Elm = ce;
                            cn.Links.Add(cnl);
                            ce.AnaSetNode(j, Nodes.Count);
                            if (ccln) {
                                cln.Node = Nodes.Count;
                            } else {
                                mNodeMap.Add(pt, new NodeMapEntry(Nodes.Count));
                            }
                            Nodes.Add(cn);
                        } else {
                            int n = cln.Node;
                            var cnl = new CircuitNode.LINK();
                            cnl.Num = j;
                            cnl.Elm = ce;
                            getCircuitNode(n).Links.Add(cnl);
                            ce.AnaSetNode(j, n);
                            /* if it's the ground node, make sure the node voltage is 0,
                            /* cause it may not get set later */
                            if (n == 0) {
                                ce.CirSetVoltage(j, 0);
                            }
                        }
                    }
                    for (int j = 0; j < inodes; j++) {
                        var cn = new CircuitNode();
                        cn.Internal = true;
                        var cnl = new CircuitNode.LINK();
                        cnl.Num = j + posts;
                        cnl.Elm = ce;
                        cn.Links.Add(cnl);
                        ce.AnaSetNode(cnl.Num, Nodes.Count);
                        Nodes.Add(cn);
                    }
                    vscount += ivs;
                }

                makePostDrawList();
                if (calcWireInfo()) {
                    mNodeMap = null; /* done with this */
                } else {
                    return;
                }

                mVoltageSources = new BaseElement[vscount];
                vscount = 0;
                for (int i = 0; i < mElmList.Count; i++) {
                    var ce = mElmList[i];
                    int ivs = ce.AnaVoltageSourceCount;
                    for (int j = 0; j < ivs; j++) {
                        mVoltageSources[vscount] = ce;
                        ce.AnaSetVoltageSource(j, vscount++);
                    }
                }
            }

            int matrixSize = Nodes.Count - 1 + vscount;
            Matrix = new double[matrixSize, matrixSize];
            RightSide = new double[matrixSize];
            RowInfo = new ROW_INFO[matrixSize];
            for (int i = 0; i < matrixSize; i++) {
                RowInfo[i] = new ROW_INFO();
            }
            mMatrixSize = mMatrixFullSize = matrixSize;
            mOrigMatrix = new double[matrixSize, matrixSize];
            mOrigRightSide = new double[matrixSize];
            mPermute = new int[matrixSize];
            mCircuitNeedsMap = false;

            /* stamp linear circuit elements */
            for (int i = 0; i < mElmList.Count; i++) {
                mElmList[i].AnaStamp();
            }

            /* determine nodes that are not connected indirectly to ground */
            var closure = new bool[Nodes.Count];
            bool changed = true;
            closure[0] = true;
            while (changed) {
                changed = false;
                for (int i = 0; i < mElmList.Count; i++) {
                    var ce = mElmList[i];
                    if (ce is ElmWire) {
                        continue;
                    }
                    /* loop through all ce's nodes to see if they are connected
                    /* to other nodes not in closure */
                    for (int j = 0; j < ce.AnaConnectionNodeCount; j++) {
                        if (!closure[ce.AnaGetConnectionNode(j)]) {
                            if (ce.AnaHasGroundConnection(j)) {
                                closure[ce.AnaGetConnectionNode(j)] = changed = true;
                            }
                            continue;
                        }
                        int k;
                        for (k = 0; k != ce.AnaConnectionNodeCount; k++) {
                            if (j == k) {
                                continue;
                            }
                            int kn = ce.AnaGetConnectionNode(k);
                            if (ce.AnaGetConnection(j, k) && !closure[kn]) {
                                closure[kn] = true;
                                changed = true;
                            }
                        }
                    }
                }
                if (changed) {
                    continue;
                }

                /* connect one of the unconnected nodes to ground with a big resistor, then try again */
                for (int i = 0; i != Nodes.Count; i++) {
                    if (!closure[i] && !getCircuitNode(i).Internal) {
                        StampResistor(0, i, 1e8);
                        closure[i] = true;
                        changed = true;
                        break;
                    }
                }
            }

            for (int i = 0; i < mElmList.Count; i++) {
                var ce = mElmList[i];

                /* look for inductors with no current path */
                if (ce is ElmInductor) {
                    var fpi = new PathInfo(PathInfo.TYPE.INDUCTOR, ce, ce.Nodes[1], mElmList, Nodes.Count);
                    if (!fpi.FindPath(ce.Nodes[0])) {
                        ce.Reset();
                    }
                }

                /* look for current sources with no current path */
                if (ce is ElmCurrent) {
                    var cur = (ElmCurrent)ce;
                    var fpi = new PathInfo(PathInfo.TYPE.INDUCTOR, ce, ce.Nodes[1], mElmList, Nodes.Count);
                    if (!fpi.FindPath(ce.Nodes[0])) {
                        cur.stampCurrentSource(true);
                    } else {
                        cur.stampCurrentSource(false);
                    }
                }

                /* look for voltage source or wire loops.  we do this for voltage sources or wire-like elements (not actual wires
                /* because those are optimized out, so the findPath won't work) */
                if (2 == ce.PostCount) {
                    if ((ce is ElmVoltage) || (ce.IsWire && !(ce is ElmWire))) {
                        var fpi = new PathInfo(PathInfo.TYPE.VOLTAGE, ce, ce.Nodes[1], mElmList, Nodes.Count);
                        if (fpi.FindPath(ce.Nodes[0])) {
                            Stop("Voltage source/wire loop with no resistance!", ce);
                            return;
                        }
                    }
                } else if (ce is ElmSwitchMulti) {
                    /* for Switch2Elms we need to do extra work to look for wire loops */
                    var fpi = new PathInfo(PathInfo.TYPE.VOLTAGE, ce, ce.Nodes[0], mElmList, Nodes.Count);
                    for (int j = 1; j < ce.PostCount; j++) {
                        if (ce.AnaGetConnection(0, j) && fpi.FindPath(ce.Nodes[j])) {
                            Stop("Voltage source/wire loop with no resistance!", ce);
                            return;
                        }
                    }
                }

                /* look for path from rail to ground */
                if ((ce is ElmRail) || (ce is ElmLogicInput)) {
                    var fpi = new PathInfo(PathInfo.TYPE.VOLTAGE, ce, ce.Nodes[0], mElmList, Nodes.Count);
                    if (fpi.FindPath(0)) {
                        Stop("Path to ground with no resistance!", ce);
                        return;
                    }
                }

                /* look for shorted caps, or caps w/ voltage but no R */
                if (ce is ElmCapacitor) {
                    var fpi = new PathInfo(PathInfo.TYPE.SHORT, ce, ce.Nodes[1], mElmList, Nodes.Count);
                    if (fpi.FindPath(ce.Nodes[0])) {
                        Console.WriteLine(ce + " shorted");
                        ce.AnaShorted();
                    } else {
                        /* a capacitor loop used to cause a matrix error. but we changed the capacitor model
                        /* so it works fine now. The only issue is if a capacitor is added in parallel with
                        /* another capacitor with a nonzero voltage; in that case we will get oscillation unless
                        /* we reset both capacitors to have the same voltage. Rather than check for that, we just
                        /* give an error. */
                        fpi = new PathInfo(PathInfo.TYPE.CAPACITOR_V, ce, ce.Nodes[1], mElmList, Nodes.Count);
                        if (fpi.FindPath(ce.Nodes[0])) {
                            Stop("Capacitor loop with no resistance!", ce);
                            return;
                        }
                    }
                }
            }

            if (!simplifyMatrix(matrixSize)) {
                return;
            }

            if (DEBUG) {
                Console.WriteLine("Matrix size:" + matrixSize);
                for (int j = 0; j != mMatrixSize; j++) {
                    Console.WriteLine("RightSide[{0}]:{1}", j, RightSide[j]);
                    for (int i = 0; i != mMatrixSize; i++) {
                        Console.WriteLine("  Matrix[{0},{1}]:{2}", j, i, Matrix[j, i]);
                    }
                }
            }
        }

        public static bool DoIteration() {
            for (int i = 0; i < mElmList.Count; i++) {
                mElmList[i].CirPrepareIteration();
            }

            int subiter;
            for (subiter = 0; subiter < SubIterMax; subiter++) {
                Converged = true;
                SubIterations = subiter;

                Array.Copy(mOrigRightSide, RightSide, mMatrixSize);
                for (int i = 0; i < mMatrixSize; i++) {
                    for (int j = 0; j < mMatrixSize; j++) {
                        Matrix[i, j] = mOrigMatrix[i, j];
                    }
                }

                for (int i = 0; i < mElmList.Count; i++) {
                    mElmList[i].CirDoIteration();
                }
                if (StopMessage != null) {
                    return false;
                }

                for (int j = 0; j < mMatrixSize; j++) {
                    for (int i = 0; i < mMatrixSize; i++) {
                        var x = Matrix[i, j];
                        if (double.IsNaN(x) || double.IsInfinity(x)) {
                            stop("Matrix[" + i + "," + j + "] is NaN/infinite");
                            return false;
                        }
                    }
                }

                if (Converged && subiter > 0) {
                    break;
                }

                if (!luFactor(Matrix, mMatrixSize, mPermute)) {
                    stop("Singular matrix!");
                    return false;
                }
                luSolve(Matrix, mMatrixSize, mPermute, RightSide);

                for (int j = 0; j < mMatrixFullSize; j++) {
                    var ri = RowInfo[j];
                    double res = 0;
                    if (ri.IsConst) {
                        res = ri.Value;
                    } else {
                        res = RightSide[ri.MapCol];
                    }
                    if (double.IsNaN(res) || double.IsInfinity(res)) {
                        Console.WriteLine((ri.IsConst ? ("RowInfo[" + j + "]") : ("RightSide[" + ri.MapCol + "]")) + " is NaN/infinite");
                        return false;
                    }
                    if (j < Nodes.Count - 1) {
                        var cn = getCircuitNode(j + 1);
                        for (int k = 0; k < cn.Links.Count; k++) {
                            var cnl = cn.Links[k];
                            cnl.Elm.CirSetVoltage(cnl.Num, res);
                        }
                    } else {
                        int ji = j - (Nodes.Count - 1);
                        mVoltageSources[ji].CirSetCurrent(ji, res);
                    }
                }
            }

            if (subiter == SubIterMax) {
                stop("計算が収束しませんでした");
                return false;
            }

            for (int i = 0; i < mElmList.Count; i++) {
                mElmList[i].CirIterationFinished();
            }

            /* calc wire currents */
            /* we removed wires from the matrix to speed things up.  in order to display wire currents,
            /* we need to calculate them now. */
            for (int i = 0; i < mWireInfoList.Count; i++) {
                var wi = mWireInfoList[i];
                var we = wi.Wire;
                double cur = 0;
                var p = we.GetPost(wi.Post);
                for (int j = 0; j < wi.Neighbors.Count; j++) {
                    var ce = wi.Neighbors[j];
                    int n = ce.CirGetNodeAtPoint(p.X, p.Y);
                    cur += ce.CirGetCurrentIntoNode(n);
                }
                if (wi.Post == 0) {
                    we.CirSetCurrent(-1, cur);
                } else {
                    we.CirSetCurrent(-1, -cur);
                }
            }

            return true;
        }

        public static void Stop(string s, BaseElement ce) {
            StopMessage = s;
            Matrix = null;  /* causes an exception */
            StopElm = ce;
            CirSimForm.SetSimRunning(false);
        }
        #endregion

        #region stamp method
        /* stamp independent voltage source #vs, from n1 to n2, amount v */
        public static void StampVoltageSource(int n1, int n2, int vs, double v) {
            int vn = Nodes.Count + vs;
            StampMatrix(vn, n1, -1);
            StampMatrix(vn, n2, 1);
            StampRightSide(vn, v);
            StampMatrix(n1, vn, 1);
            StampMatrix(n2, vn, -1);
        }

        /* use this if the amount of voltage is going to be updated in doStep(), by updateVoltageSource() */
        public static void StampVoltageSource(int n1, int n2, int vs) {
            int vn = Nodes.Count + vs;
            StampMatrix(vn, n1, -1);
            StampMatrix(vn, n2, 1);
            StampRightSide(vn);
            StampMatrix(n1, vn, 1);
            StampMatrix(n2, vn, -1);
        }

        /* update voltage source in doStep() */
        public static void UpdateVoltageSource(int vs, double v) {
            int vn = Nodes.Count + vs;
            StampRightSide(vn, v);
        }

        public static void StampResistor(int n1, int n2, double r) {
            double r0 = 1 / r;
            if (double.IsNaN(r0) || double.IsInfinity(r0)) {
                Console.WriteLine("bad resistance " + r + " " + r0 + "\n");
                throw new Exception("bad resistance " + r + " " + r0);
            }
            StampMatrix(n1, n1, r0);
            StampMatrix(n2, n2, r0);
            StampMatrix(n1, n2, -r0);
            StampMatrix(n2, n1, -r0);
        }

        public static void StampConductance(int n1, int n2, double r0) {
            StampMatrix(n1, n1, r0);
            StampMatrix(n2, n2, r0);
            StampMatrix(n1, n2, -r0);
            StampMatrix(n2, n1, -r0);
        }

        /* current from cn1 to cn2 is equal to voltage from vn1 to 2, divided by g */
        public static void StampVCCurrentSource(int cn1, int cn2, int vn1, int vn2, double g) {
            StampMatrix(cn1, vn1, g);
            StampMatrix(cn2, vn2, g);
            StampMatrix(cn1, vn2, -g);
            StampMatrix(cn2, vn1, -g);
        }

        public static void StampCurrentSource(int n1, int n2, double i) {
            StampRightSide(n1, -i);
            StampRightSide(n2, i);
        }

        /* stamp a current source from n1 to n2 depending on current through vs */
        public static void StampCCCS(int n1, int n2, int vs, double gain) {
            int vn = Nodes.Count + vs;
            StampMatrix(n1, vn, gain);
            StampMatrix(n2, vn, -gain);
        }

        /// <summary>
        /// <para>meaning that a voltage change of dv in node j will increase the current into node i by x dv.</para>
        /// <para>(Unless i or j is a voltage source node.)</para>
        /// </summary>
        /// <param name="r">row</param>
        /// <param name="c">column</param>
        /// <param name="x">stamp value in row, column</param>
        public static void StampMatrix(int r, int c, double x) {
            if (r > 0 && c > 0) {
                if (mCircuitNeedsMap) {
                    r = RowInfo[r - 1].MapRow;
                    var ri = RowInfo[c - 1];
                    if (ri.IsConst) {
                        RightSide[r] -= x * ri.Value;
                        return;
                    }
                    c = ri.MapCol;
                } else {
                    r--;
                    c--;
                }
                Matrix[r, c] += x;
            }
        }

        /* stamp value x on the right side of row i, representing an
        /* independent current source flowing into node i */
        public static void StampRightSide(int i, double x) {
            if (i > 0) {
                if (mCircuitNeedsMap) {
                    i = RowInfo[i - 1].MapRow;
                } else {
                    i--;
                }
                RightSide[i] += x;
            }
        }

        /* indicate that the value on the right side of row i changes in doStep() */
        public static void StampRightSide(int i) {
            if (i > 0) {
                RowInfo[i - 1].RightChanges = true;
            }
        }

        /* indicate that the values on the left side of row i change in doStep() */
        public static void StampNonLinear(int i) {
            if (i > 0) {
                RowInfo[i - 1].LeftChanges = true;
            }
        }
        #endregion
    }
}
