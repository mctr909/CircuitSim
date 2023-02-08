using System;
using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements;
using Circuit.Elements.Passive;
using Circuit.Elements.Input;
using Circuit.Elements.Output;

using Circuit.UI;
using Circuit.UI.Passive;
using Circuit.UI.Input;

namespace Circuit {
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
        const bool debug = false;

        public static double[,] Matrix;
        public static double[] RightSide;
        public static ROW_INFO[] RowInfo;
        public static List<CircuitNode> NodeList;

        #region private varidate
        static Dictionary<Point, NodeMapEntry> mNodeMap;
        static Dictionary<Point, int> mPostCountMap;

        /* info about each wire and its neighbors, used to calculate wire currents */
        static List<WireInfo> mWireInfoList;
        static BaseElement[] mVoltageSources;

        static bool mCircuitNeedsMap;

        static int mMatrixSize;
        static int mMatrixFullSize;
        static int[] mPermute;
        static double[] mOrigRightSide;
        static double[,] mOrigMatrix;
        #endregion

        #region property
        public static List<Point> PostDrawList { get; private set; } = new List<Point>();
        public static List<Point> BadConnectionList { get; private set; } = new List<Point>();
        public static BaseElement StopElm { get; set; }
        public static string StopMessage { get; set; }

        public static int VoltageSourceCount { get; private set; }

        public static bool CircuitNonLinear { get; private set; }

        public static bool ShowResistanceInVoltageSources { get; private set; }

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
            /*Console.WriteLine("ac7");*/

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
            for (int i = 0; i != CirSimForm.ElmCount; i++) {
                var ce = CirSimForm.GetElm(i);
                if (!(ce is Wire)) {
                    continue;
                }
                var we = (Wire)ce;
                we.HasWireInfo = false;
                mWireInfoList.Add(new WireInfo(we));
                var p1 = ce.GetPost(0);
                var p2 = ce.GetPost(1);
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
                    mNodeMap.Add(ce.GetPost(1), cn1);
                    continue;
                }
                if (cp2) {
                    var cn2 = mNodeMap[p2];
                    mNodeMap.Add(ce.GetPost(0), cn2);
                    continue;
                }
                /* new entry */
                var cn = new NodeMapEntry();
                mNodeMap.Add(ce.GetPost(0), cn);
                mNodeMap.Add(ce.GetPost(1), cn);
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
                var cn1 = NodeList[wire.Elm.Nodes[0]];  /* both ends of wire have same node # */
                int j;

                var neighbors0 = new List<BaseUI>();
                var neighbors1 = new List<BaseUI>();
                bool isReady0 = true;
                bool isReady1 = true;

                /* go through elements sharing a node with this wire (may be connected indirectly
                /* by other wires, but at least it's faster than going through all elements) */
                for (j = 0; j != cn1.Links.Count; j++) {
                    var cnl = cn1.Links[j];
                    var ce = cnl.UI;
                    if (ce == wire) {
                        continue;
                    }
                    var pt = cnl.UI.GetPost(cnl.Num);

                    /* is this a wire that doesn't have wire info yet?  If so we can't use it.
                    /* That would create a circular dependency */
                    bool notReady = (ce is Wire) && !((Wire)ce).HasWireInfo;

                    /* which post does this element connect to, if any? */
                    if (pt.X == wire.DumpInfo.P1.X && pt.Y == wire.DumpInfo.P1.Y) {
                        neighbors0.Add(ce);
                        if (notReady) {
                            isReady0 = false;
                        }
                    } else if (pt.X == wire.DumpInfo.P2.X && pt.Y == wire.DumpInfo.P2.Y) {
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
                        Stop("wire loop detected", wire.Elm);
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
            PostDrawList = new List<Point>();
            BadConnectionList = new List<Point>();
            foreach (var entry in mPostCountMap) {
                if (entry.Value != 2) {
                    PostDrawList.Add(entry.Key);
                }
                /* look for bad connections, posts not connected to other elements which intersect
                /* other elements' bounding boxes */
                if (entry.Value == 1) {
                    bool bad = false;
                    var cn = entry.Key;
                    for (int j = 0; j != CirSimForm.ElmCount && !bad; j++) {
                        var ce = CirSimForm.GetElm(j);
                        /* does this post intersect elm's bounding box? */
                        if (!ce.DumpInfo.BoundingBox.Contains(cn.X, cn.Y)) {
                            continue;
                        }
                        /* does this post belong to the elm? */
                        int k;
                        int pc = ce.Elm.PostCount;
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
            if (n >= NodeList.Count) {
                return null;
            }
            return NodeList[n];
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
        /* we removed wires from the matrix to speed things up.  in order to display wire currents,
        /* we need to calculate them now. */
        public static void CalcWireCurrents() {
            for (int i = 0; i != mWireInfoList.Count; i++) {
                var wi = mWireInfoList[i];
                double cur = 0;
                var p = wi.Wire.GetPost(wi.Post);
                for (int j = 0; j != wi.Neighbors.Count; j++) {
                    var ce = wi.Neighbors[j];
                    int n = ce.GetNodeAtPoint(p.X, p.Y);
                    cur += ce.Elm.GetCurrentIntoNode(n);
                }
                if (wi.Post == 0) {
                    wi.Wire.Elm.CirSetCurrent(-1, cur);
                }
                else {
                    wi.Wire.Elm.CirSetCurrent(-1, -cur);
                }
            }
        }

        public static void AnalyzeCircuit() {
            var elmList = CirSimForm.ElmList;
            if (0 == CirSimForm.ElmCount) {
                PostDrawList = new List<Point>();
                BadConnectionList = new List<Point>();
                return;
            }

            StopMessage = null;
            StopElm = null;
            NodeList = new List<CircuitNode>();
            mPostCountMap = new Dictionary<Point, int>();

            calculateWireClosure();

            {
                /* look for voltage or ground element */
                bool gotGround = false;
                bool gotRail = false;
                BaseUI volt = null;
                for (int i = 0; i != CirSimForm.ElmCount; i++) {
                    var ce = CirSimForm.GetElm(i);
                    if (ce is Ground) {
                        gotGround = true;
                        break;
                    }
                    if (ce is Rail) {
                        gotRail = true;
                    }
                    if (volt == null && (ce is Voltage)) {
                        volt = ce;
                    }
                }

                /* if no ground, and no rails, then the voltage elm's first terminal
                /* is ground */
                if (!gotGround && volt != null && !gotRail) {
                    var cn = new CircuitNode();
                    var pt = volt.GetPost(0);
                    NodeList.Add(cn);
                    /* update node map */
                    if (mNodeMap.ContainsKey(pt)) {
                        mNodeMap[pt].Node = 0;
                    } else {
                        mNodeMap.Add(pt, new NodeMapEntry(0));
                    }
                } else {
                    /* otherwise allocate extra node for ground */
                    var cn = new CircuitNode();
                    NodeList.Add(cn);
                }
            }

            /* allocate nodes and voltage sources */
            int vscount = 0;
            {
                ElmLabeledNode.ResetNodeList();
                for (int i = 0; i != CirSimForm.ElmCount; i++) {
                    var ce = CirSimForm.GetElm(i);
                    var cee = ce.Elm;
                    if (null == cee) {
                        continue;
                    }
                    int inodes = cee.InternalNodeCount;
                    int ivs = cee.VoltageSourceCount;
                    int posts = cee.PostCount;

                    /* allocate a node for each post and match posts to nodes */
                    for (int j = 0; j != posts; j++) {
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
                            var cnl = new CircuitNodeLink();
                            cnl.Num = j;
                            cnl.UI = ce;
                            cnl.Elm = cee;
                            cn.Links.Add(cnl);
                            cee.AnaSetNode(j, NodeList.Count);
                            if (ccln) {
                                cln.Node = NodeList.Count;
                            } else {
                                mNodeMap.Add(pt, new NodeMapEntry(NodeList.Count));
                            }
                            NodeList.Add(cn);
                        } else {
                            int n = cln.Node;
                            var cnl = new CircuitNodeLink();
                            cnl.Num = j;
                            cnl.UI = ce;
                            cnl.Elm = cee;
                            getCircuitNode(n).Links.Add(cnl);
                            cee.AnaSetNode(j, n);
                            /* if it's the ground node, make sure the node voltage is 0,
                            /* cause it may not get set later */
                            if (n == 0) {
                                cee.CirSetVoltage(j, 0);
                            }
                        }
                    }
                    for (int j = 0; j != inodes; j++) {
                        var cn = new CircuitNode();
                        cn.Internal = true;
                        var cnl = new CircuitNodeLink();
                        cnl.Num = j + posts;
                        cnl.UI = ce;
                        cnl.Elm = cee;
                        cn.Links.Add(cnl);
                        cee.AnaSetNode(cnl.Num, NodeList.Count);
                        NodeList.Add(cn);
                    }
                    vscount += ivs;
                }
                makePostDrawList();
                if (calcWireInfo()) {
                    mNodeMap = null; /* done with this */
                } else {
                    return;
                }
            }

            /* determine if circuit is nonlinear */
            {
                mVoltageSources = new BaseElement[vscount];
                vscount = 0;
                CircuitNonLinear = false;
                for (int i = 0; i != CirSimForm.ElmCount; i++) {
                    var ce = CirSimForm.GetElm(i);
                    var cee = ce.Elm;
                    if (cee.NonLinear) {
                        CircuitNonLinear = true;
                    }
                    int ivs = cee.VoltageSourceCount;
                    for (int j = 0; j != ivs; j++) {
                        mVoltageSources[vscount] = cee;
                        cee.AnaSetVoltageSource(j, vscount++);
                    }
                }
                VoltageSourceCount = vscount;
            }

            int matrixSize = NodeList.Count - 1 + vscount;
            Matrix = new double[matrixSize, matrixSize];
            RightSide = new double[matrixSize];
            RowInfo = new ROW_INFO[matrixSize];
            for (int i = 0; i != matrixSize; i++) {
                RowInfo[i] = new ROW_INFO();
            }
            mMatrixSize = mMatrixFullSize = matrixSize;
            mOrigMatrix = new double[matrixSize, matrixSize];
            mOrigRightSide = new double[matrixSize];
            mPermute = new int[matrixSize];
            mCircuitNeedsMap = false;

            /* stamp linear circuit elements */
            for (int i = 0; i != CirSimForm.ElmCount; i++) {
                var cee = CirSimForm.GetElm(i).Elm;
                cee.AnaStamp();
            }

            /* determine nodes that are not connected indirectly to ground */
            var closure = new bool[NodeList.Count];
            bool changed = true;
            closure[0] = true;
            while (changed) {
                changed = false;
                for (int i = 0; i != CirSimForm.ElmCount; i++) {
                    var cee = CirSimForm.GetElm(i).Elm;
                    if (cee is ElmWire) {
                        continue;
                    }
                    /* loop through all ce's nodes to see if they are connected
                    /* to other nodes not in closure */
                    for (int j = 0; j < cee.ConnectionNodeCount; j++) {
                        if (!closure[cee.AnaGetConnectionNode(j)]) {
                            if (cee.AnaHasGroundConnection(j)) {
                                closure[cee.AnaGetConnectionNode(j)] = changed = true;
                            }
                            continue;
                        }
                        int k;
                        for (k = 0; k != cee.ConnectionNodeCount; k++) {
                            if (j == k) {
                                continue;
                            }
                            int kn = cee.AnaGetConnectionNode(k);
                            if (cee.AnaGetConnection(j, k) && !closure[kn]) {
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
                for (int i = 0; i != NodeList.Count; i++) {
                    if (!closure[i] && !getCircuitNode(i).Internal) {
                        /* Console.WriteLine("node " + i + " unconnected"); */
                        StampResistor(0, i, 1e8);
                        closure[i] = true;
                        changed = true;
                        break;
                    }
                }
            }

            for (int i = 0; i != CirSimForm.ElmCount; i++) {
                var ce = CirSimForm.GetElm(i);
                var cee = ce.Elm;

                /* look for inductors with no current path */
                if (cee is ElmInductor) {
                    var fpi = new PathInfo(PathType.INDUCTOR, cee, cee.Nodes[1], elmList, NodeList.Count);
                    if (!fpi.FindPath(cee.Nodes[0])) {
                        cee.Reset();
                    }
                }

                /* look for current sources with no current path */
                if (cee is ElmCurrent) {
                    var cur = (ElmCurrent)cee;
                    var fpi = new PathInfo(PathType.INDUCTOR, cee, cee.Nodes[1], elmList, NodeList.Count);
                    if (!fpi.FindPath(cee.Nodes[0])) {
                        cur.stampCurrentSource(true);
                    } else {
                        cur.stampCurrentSource(false);
                    }
                }

                /* look for voltage source or wire loops.  we do this for voltage sources or wire-like elements (not actual wires
                /* because those are optimized out, so the findPath won't work) */
                if (2 == cee.PostCount) {
                    if ((cee is ElmVoltage) || (cee.IsWire && !(cee is ElmWire))) {
                        var fpi = new PathInfo(PathType.VOLTAGE, cee, cee.Nodes[1], elmList, NodeList.Count);
                        if (fpi.FindPath(cee.Nodes[0])) {
                            Stop("Voltage source/wire loop with no resistance!", cee);
                            return;
                        }
                    }
                } else if (cee is ElmSwitchMulti) {
                    /* for Switch2Elms we need to do extra work to look for wire loops */
                    var fpi = new PathInfo(PathType.VOLTAGE, cee, cee.Nodes[0], elmList, NodeList.Count);
                    for (int j = 1; j < cee.PostCount; j++) {
                        if (cee.AnaGetConnection(0, j) && fpi.FindPath(cee.Nodes[j])) {
                            Stop("Voltage source/wire loop with no resistance!", cee);
                            return;
                        }
                    }
                }

                /* look for path from rail to ground */
                if ((cee is ElmRail) || (cee is ElmLogicInput)) {
                    var fpi = new PathInfo(PathType.VOLTAGE, cee, cee.Nodes[0], elmList, NodeList.Count);
                    if (fpi.FindPath(0)) {
                        Stop("Path to ground with no resistance!", cee);
                        return;
                    }
                }

                /* look for shorted caps, or caps w/ voltage but no R */
                if (cee is ElmCapacitor) {
                    var fpi = new PathInfo(PathType.SHORT, cee, cee.Nodes[1], elmList, NodeList.Count);
                    if (fpi.FindPath(cee.Nodes[0])) {
                        Console.WriteLine(cee + " shorted");
                        cee.AnaShorted();
                    } else {
                        /* a capacitor loop used to cause a matrix error. but we changed the capacitor model
                        /* so it works fine now. The only issue is if a capacitor is added in parallel with
                        /* another capacitor with a nonzero voltage; in that case we will get oscillation unless
                        /* we reset both capacitors to have the same voltage. Rather than check for that, we just
                        /* give an error. */
                        fpi = new PathInfo(PathType.CAPACITOR_V, cee, cee.Nodes[1], elmList, NodeList.Count);
                        if (fpi.FindPath(cee.Nodes[0])) {
                            Stop("Capacitor loop with no resistance!", cee);
                            return;
                        }
                    }
                }
            }

            if (!simplifyMatrix(matrixSize)) {
                return;
            }

            if (debug) {
                Console.WriteLine("matrixSize = " + matrixSize + " " + CircuitNonLinear);
                for (int j = 0; j != mMatrixSize; j++) {
                    Console.WriteLine("RightSide[{0}]:{1}", j, RightSide[j]);
                    for (int i = 0; i != mMatrixSize; i++) {
                        Console.WriteLine("  Matrix[{0},{1}]:{2}", j, i, Matrix[j, i]);
                    }
                }
            }

            /* check if we called stop() */
            if (Matrix == null) {
                return;
            }

            /* if a matrix is linear, we can do the lu_factor here instead of
            /* needing to do it every frame */
            if (!CircuitNonLinear) {
                if (!luFactor(Matrix, mMatrixSize, mPermute)) {
                    stop("Singular matrix!");
                    return;
                }
            }

            /* show resistance in voltage sources if there's only one */
            bool gotVoltageSource = false;
            ShowResistanceInVoltageSources = true;
            for (int i = 0; i != CirSimForm.ElmCount; i++) {
                var ce = CirSimForm.GetElm(i);
                if (ce is Voltage) {
                    if (gotVoltageSource) {
                        ShowResistanceInVoltageSources = false;
                    } else {
                        gotVoltageSource = true;
                    }
                }
            }
        }

        public static bool DoIteration() {
            int i, j, k, subiter;
            int elmCount = CirSimForm.ElmCount;

            for (i = 0; i != elmCount; i++) {
                var ce = CirSimForm.ElmList[i].Elm;
                ce.CirPrepareIteration();
            }

            for (subiter = 0; subiter != SubIterMax; subiter++) {
                Converged = true;
                SubIterations = subiter;
                for (i = 0; i != mMatrixSize; i++) {
                    RightSide[i] = mOrigRightSide[i];
                }
                if (CircuitNonLinear) {
                    for (i = 0; i != mMatrixSize; i++) {
                        for (j = 0; j != mMatrixSize; j++) {
                            Matrix[i, j] = mOrigMatrix[i, j];
                        }
                    }
                }
                for (i = 0; i != elmCount; i++) {
                    var ce = CirSimForm.ElmList[i].Elm;
                    ce.CirDoIteration();
                }
                if (StopMessage != null) {
                    return false;
                }

                for (j = 0; j != mMatrixSize; j++) {
                    for (i = 0; i != mMatrixSize; i++) {
                        double x = Matrix[i, j];
                        if (double.IsNaN(x) || double.IsInfinity(x)) {
                            stop("nan/infinite matrix!");
                            return false;
                        }
                    }
                }
                if (CircuitNonLinear) {
                    if (Converged && subiter > 0) {
                        break;
                    }
                    if (!luFactor(Matrix, mMatrixSize, mPermute)) {
                        stop("Singular matrix!");
                        return false;
                    }
                }
                luSolve(Matrix, mMatrixSize, mPermute, RightSide);

                for (j = 0; j != mMatrixFullSize; j++) {
                    var ri = RowInfo[j];
                    double res = 0;
                    if (ri.IsConst) {
                        res = ri.Value;
                    } else {
                        res = RightSide[ri.MapCol];
                    }
                    /*Console.WriteLine(j + " " + res + " " + ri.type + " " + ri.mapCol);*/
                    if (double.IsNaN(res)) {
                        Converged = false;
                        break;
                    }
                    if (j < NodeList.Count - 1) {
                        var cn = getCircuitNode(j + 1);
                        for (k = 0; k != cn.Links.Count; k++) {
                            var cnl = cn.Links[k];
                            cnl.Elm.CirSetVoltage(cnl.Num, res);
                        }
                    } else {
                        int ji = j - (NodeList.Count - 1);
                        /*Console.WriteLine("setting vsrc " + ji + " to " + res); */
                        mVoltageSources[ji].CirSetCurrent(ji, res);
                    }
                }
                if (!CircuitNonLinear) {
                    break;
                }
            }

            if (subiter == SubIterMax) {
                stop("計算が収束しませんでした");
                return false;
            }

            for (i = 0; i != elmCount; i++) {
                CirSimForm.ElmList[i].Elm.CirIterationFinished();
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
            int vn = NodeList.Count + vs;
            StampMatrix(vn, n1, -1);
            StampMatrix(vn, n2, 1);
            StampRightSide(vn, v);
            StampMatrix(n1, vn, 1);
            StampMatrix(n2, vn, -1);
        }

        /* use this if the amount of voltage is going to be updated in doStep(), by updateVoltageSource() */
        public static void StampVoltageSource(int n1, int n2, int vs) {
            int vn = NodeList.Count + vs;
            StampMatrix(vn, n1, -1);
            StampMatrix(vn, n2, 1);
            StampRightSide(vn);
            StampMatrix(n1, vn, 1);
            StampMatrix(n2, vn, -1);
        }

        /* update voltage source in doStep() */
        public static void UpdateVoltageSource(int vs, double v) {
            int vn = NodeList.Count + vs;
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
            int vn = NodeList.Count + vs;
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
                        /*Console.WriteLine("Stamping constant " + i + " " + j + " " + x);*/
                        RightSide[r] -= x * ri.Value;
                        return;
                    }
                    c = ri.MapCol;
                    /*Console.WriteLine("stamping " + i + " " + j + " " + x);*/
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
                    /*Console.WriteLine("stamping " + i + " " + x);*/
                } else {
                    i--;
                }
                RightSide[i] += x;
            }
        }

        /* indicate that the value on the right side of row i changes in doStep() */
        public static void StampRightSide(int i) {
            /*Console.WriteLine("rschanges true " + (i-1)); */
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
