﻿using System;
using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements;

namespace Circuit {
    class Circuit {
        class RowInfo {
            public const int ROW_NORMAL = 0; /* ordinary value */
            public const int ROW_CONST = 1;  /* value is constant */
            public int Type = ROW_NORMAL;
            public int MapCol;
            public int MapRow;
            public double Value;
            public bool RightChanges; /* row's right side changes */
            public bool LeftChanges;  /* row's left side changes */
            public bool DropRow;      /* row is not needed in matrix */
        }

        class WireInfo {
            public WireElm Wire;
            public List<CircuitElm> Neighbors;
            public int Post;
            public WireInfo(WireElm w) { Wire = w; }
        }

        class NodeMapEntry {
            public int Node;
            public NodeMapEntry() { Node = -1; }
            public NodeMapEntry(int n) { Node = n; }
        }

        #region private varidate
        CirSim mSim;

        Dictionary<Point, NodeMapEntry> mNodeMap;
        Dictionary<Point, int> mPostCountMap;

        /* info about each wire and its neighbors, used to calculate wire currents */
        List<WireInfo> mWireInfoList;

        bool mCircuitNeedsMap;

        int mMatrixSize;
        int mMatrixFullSize;
        double[] mOrigRightSide;
        double[,] mOrigMatrix;
        double[] mRightSide;
        int[] mPermute;
        RowInfo[] mRowInfo;
        #endregion

        #region property
        public CircuitElm StopElm { get; set; }
        public string StopMessage { get; set; }

        public List<Point> PostDrawList { get; private set; } = new List<Point>();
        public List<Point> BadConnectionList { get; private set; } = new List<Point>();

        public List<CircuitNode> NodeList { get; private set; }
        public double[,] Matrix { get; set; }

        public int VoltageSourceCount { get; private set; }
        public CircuitElm[] VoltageSources { get; private set; }

        public bool CircuitNonLinear { get; private set; }

        public bool ShowResistanceInVoltageSources { get; private set; }

        public bool Converged { get; set; }
        public int SubIterations { get; private set; }
        #endregion

        public Circuit(CirSim sim) {
            mSim = sim;
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

        /* simplify the matrix; this speeds things up quite a bit, especially for digital circuits */
        bool simplifyMatrix(int matrixSize) {
            int matRow;
            int matCol;
            for (matRow = 0; matRow != matrixSize; matRow++) {
                int qp = -1;
                double qv = 0;
                var re = mRowInfo[matRow];
                /*Console.WriteLine("row " + i + " " + re.lsChanges + " " + re.rsChanges + " " + re.dropRow);*/
                if (re.LeftChanges || re.DropRow || re.RightChanges) {
                    continue;
                }
                double rsadd = 0;

                /* look for rows that can be removed */
                for (matCol = 0; matCol != matrixSize; matCol++) {
                    double q = Matrix[matRow, matCol];
                    if (mRowInfo[matCol].Type == RowInfo.ROW_CONST) {
                        /* keep a running total of const values that have been
                        /* removed already */
                        rsadd -= mRowInfo[matCol].Value * q;
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
                        Stop("Matrix error", null);
                        return false;
                    }
                    var elt = mRowInfo[qp];
                    /* we found a row with only one nonzero nonconst entry; that value
                    /* is a constant */
                    if (elt.Type != RowInfo.ROW_NORMAL) {
                        Console.WriteLine("type already " + elt.Type + " for " + qp + "!");
                        continue;
                    }
                    elt.Type = RowInfo.ROW_CONST;
                    /*Console.WriteLine("ROW_CONST " + i + " " + rsadd);*/
                    elt.Value = (mRightSide[matRow] + rsadd) / qv;
                    mRowInfo[matRow].DropRow = true;
                    matRow = -1; /* start over from scratch */
                }
            }
            /*Console.WriteLine("ac7");*/

            /* find size of new matrix */
            int nn = 0;
            for (matRow = 0; matRow != matrixSize; matRow++) {
                var elt = mRowInfo[matRow];
                if (elt.Type == RowInfo.ROW_NORMAL) {
                    elt.MapCol = nn++;
                    /*Console.WriteLine("col " + i + " maps to " + elt.mapCol);*/
                    continue;
                }
                if (elt.Type == RowInfo.ROW_CONST) {
                    elt.MapCol = -1;
                }
            }

            /* make the new, simplified matrix */
            int newsize = nn;
            var newmatx = new double[newsize, newsize];
            var newrs = new double[newsize];
            int ii = 0;
            for (matRow = 0; matRow != matrixSize; matRow++) {
                var rri = mRowInfo[matRow];
                if (rri.DropRow) {
                    rri.MapRow = -1;
                    continue;
                }
                newrs[ii] = mRightSide[matRow];
                rri.MapRow = ii;
                /*Console.WriteLine("Row " + i + " maps to " + ii); */
                for (matCol = 0; matCol != matrixSize; matCol++) {
                    var ri = mRowInfo[matCol];
                    if (ri.Type == RowInfo.ROW_CONST) {
                        newrs[ii] -= ri.Value * Matrix[matRow, matCol];
                    } else {
                        newmatx[ii, ri.MapCol] += Matrix[matRow, matCol];
                    }
                }
                ii++;
            }

            /*Console.WriteLine("old size = " + matrixSize + " new size = " + newsize);*/

            Matrix = newmatx;
            mRightSide = newrs;
            matrixSize = mMatrixSize = newsize;
            for (matRow = 0; matRow != matrixSize; matRow++) {
                mOrigRightSide[matRow] = mRightSide[matRow];
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
        void calculateWireClosure() {
            int mergeCount = 0;
            mNodeMap = new Dictionary<Point, NodeMapEntry>();
            mWireInfoList = new List<WireInfo>();
            for (int i = 0; i != mSim.ElmList.Count; i++) {
                var ce = mSim.getElm(i);
                if (!(ce is WireElm)) {
                    continue;
                }
                var we = (WireElm)ce;
                we.hasWireInfo = false;
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
        bool calcWireInfo() {
            int wireIdx;
            int moved = 0;
            for (wireIdx = 0; wireIdx != mWireInfoList.Count; wireIdx++) {
                var wi = mWireInfoList[wireIdx];
                var wire = wi.Wire;
                var cn1 = NodeList[wire.Nodes[0]];  /* both ends of wire have same node # */
                int j;

                var neighbors0 = new List<CircuitElm>();
                var neighbors1 = new List<CircuitElm>();
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
                    var pt = cnl.Elm.GetPost(cnl.Num);

                    /* is this a wire that doesn't have wire info yet?  If so we can't use it.
                    /* That would create a circular dependency */
                    bool notReady = (ce is WireElm) && !((WireElm)ce).hasWireInfo;

                    /* which post does this element connect to, if any? */
                    if (pt.X == wire.X1 && pt.Y == wire.Y1) {
                        neighbors0.Add(ce);
                        if (notReady) {
                            isReady0 = false;
                        }
                    } else if (pt.X == wire.X2 && pt.Y == wire.Y2) {
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
                    wire.hasWireInfo = true;
                    moved = 0;
                } else if (isReady1) {
                    wi.Neighbors = neighbors1;
                    wi.Post = 1;
                    wire.hasWireInfo = true;
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
        void makePostDrawList() {
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
                    for (int j = 0; j != mSim.ElmList.Count && !bad; j++) {
                        var ce = mSim.getElm(j);
                        if (ce is GraphicElm) {
                            continue;
                        }
                        /* does this post intersect elm's bounding box? */
                        if (!ce.BoundingBox.Contains(cn.X, cn.Y)) {
                            continue;
                        }
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

        CircuitNode getCircuitNode(int n) {
            if (n >= NodeList.Count) {
                return null;
            }
            return NodeList[n];
        }

        /* stamp independent voltage source #vs, from n1 to n2, amount v */
        public void StampVoltageSource(int n1, int n2, int vs, double v) {
            int vn = NodeList.Count + vs;
            StampMatrix(vn, n1, -1);
            StampMatrix(vn, n2, 1);
            StampRightSide(vn, v);
            StampMatrix(n1, vn, 1);
            StampMatrix(n2, vn, -1);
        }

        /* use this if the amount of voltage is going to be updated in doStep(), by updateVoltageSource() */
        public void StampVoltageSource(int n1, int n2, int vs) {
            int vn = NodeList.Count + vs;
            StampMatrix(vn, n1, -1);
            StampMatrix(vn, n2, 1);
            StampRightSide(vn);
            StampMatrix(n1, vn, 1);
            StampMatrix(n2, vn, -1);
        }

        /* update voltage source in doStep() */
        public void UpdateVoltageSource(int n1, int n2, int vs, double v) {
            int vn = NodeList.Count + vs;
            StampRightSide(vn, v);
        }

        public void StampResistor(int n1, int n2, double r) {
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

        public void StampConductance(int n1, int n2, double r0) {
            StampMatrix(n1, n1, r0);
            StampMatrix(n2, n2, r0);
            StampMatrix(n1, n2, -r0);
            StampMatrix(n2, n1, -r0);
        }

        /* current from cn1 to cn2 is equal to voltage from vn1 to 2, divided by g */
        public void StampVCCurrentSource(int cn1, int cn2, int vn1, int vn2, double g) {
            StampMatrix(cn1, vn1, g);
            StampMatrix(cn2, vn2, g);
            StampMatrix(cn1, vn2, -g);
            StampMatrix(cn2, vn1, -g);
        }

        public void StampCurrentSource(int n1, int n2, double i) {
            StampRightSide(n1, -i);
            StampRightSide(n2, i);
        }

        /* stamp a current source from n1 to n2 depending on current through vs */
        public void StampCCCS(int n1, int n2, int vs, double gain) {
            int vn = NodeList.Count + vs;
            StampMatrix(n1, vn, gain);
            StampMatrix(n2, vn, -gain);
        }

        /// <summary>
        /// <para>meaning that a voltage change of dv in node j will increase the current into node i by x dv.</para>
        /// <para>(Unless i or j is a voltage source node.)</para>
        /// </summary>
        /// <param name="i">row</param>
        /// <param name="j">column</param>
        /// <param name="x">stamp value in row, column</param>
        public void StampMatrix(int i, int j, double x) {
            if (i > 0 && j > 0) {
                if (mCircuitNeedsMap) {
                    i = mRowInfo[i - 1].MapRow;
                    var ri = mRowInfo[j - 1];
                    if (ri.Type == RowInfo.ROW_CONST) {
                        /*Console.WriteLine("Stamping constant " + i + " " + j + " " + x);*/
                        mRightSide[i] -= x * ri.Value;
                        return;
                    }
                    j = ri.MapCol;
                    /*Console.WriteLine("stamping " + i + " " + j + " " + x);*/
                } else {
                    i--;
                    j--;
                }
                Matrix[i, j] += x;
            }
        }

        /* stamp value x on the right side of row i, representing an
        /* independent current source flowing into node i */
        public void StampRightSide(int i, double x) {
            if (i > 0) {
                if (mCircuitNeedsMap) {
                    i = mRowInfo[i - 1].MapRow;
                    /*Console.WriteLine("stamping " + i + " " + x);*/
                } else {
                    i--;
                }
                mRightSide[i] += x;
            }
        }

        /* indicate that the value on the right side of row i changes in doStep() */
        public void StampRightSide(int i) {
            /*Console.WriteLine("rschanges true " + (i-1)); */
            if (i > 0) {
                mRowInfo[i - 1].RightChanges = true;
            }
        }

        /* indicate that the values on the left side of row i change in doStep() */
        public void StampNonLinear(int i) {
            if (i > 0) {
                mRowInfo[i - 1].LeftChanges = true;
            }
        }

        /* we removed wires from the matrix to speed things up.  in order to display wire currents,
        /* we need to calculate them now. */
        public void CalcWireCurrents() {
            /* for debugging */
            /*for (int i = 0; i != mWireInfoList.Count; i++) {
                mWireInfoList[i].wire.setCurrent(-1, 1.23);
            }*/
            for (int i = 0; i != mWireInfoList.Count; i++) {
                var wi = mWireInfoList[i];
                double cur = 0;
                var p = wi.Wire.GetPost(wi.Post);
                for (int j = 0; j != wi.Neighbors.Count; j++) {
                    var ce = wi.Neighbors[j];
                    int n = ce.GetNodeAtPoint(p.X, p.Y);
                    cur += ce.GetCurrentIntoNode(n);
                }
                if (wi.Post == 0) {
                    wi.Wire.SetCurrent(-1, cur);
                } else {
                    wi.Wire.SetCurrent(-1, -cur);
                }
            }
        }

        public void AnalyzeCircuit() {
            bool debug = false;
            var elmList = mSim.ElmList;
            if (elmList.Count == 0) {
                PostDrawList = new List<Point>();
                BadConnectionList = new List<Point>();
                return;
            }

            StopMessage = null;
            StopElm = null;

            int vscount = 0;
            NodeList = new List<CircuitNode>();
            mPostCountMap = new Dictionary<Point, int>();
            bool gotGround = false;
            bool gotRail = false;
            CircuitElm volt = null;

            calculateWireClosure();

            if (debug) Console.WriteLine("ac1");
            /* look for voltage or ground element */
            for (int i = 0; i != elmList.Count; i++) {
                var ce = mSim.getElm(i);
                if (ce is GroundElm) {
                    gotGround = true;
                    break;
                }

                if (ce is RailElm) {
                    gotRail = true;
                }
                if (volt == null && (ce is VoltageElm)) {
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
            if (debug) Console.WriteLine("ac2");

            /* allocate nodes and voltage sources */
            for (int i = 0; i != elmList.Count; i++) {
                var ce = mSim.getElm(i);
                int inodes = ce.InternalNodeCount;
                int ivs = ce.VoltageSourceCount;
                int posts = ce.PostCount;

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
                        cnl.Elm = ce;
                        cn.Links.Add(cnl);
                        ce.SetNode(j, NodeList.Count);
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
                        cnl.Elm = ce;
                        getCircuitNode(n).Links.Add(cnl);
                        ce.SetNode(j, n);
                        /* if it's the ground node, make sure the node voltage is 0,
                        /* cause it may not get set later */
                        if (n == 0) {
                            ce.SetNodeVoltage(j, 0);
                        }
                    }
                }
                for (int j = 0; j != inodes; j++) {
                    var cn = new CircuitNode();
                    cn.Internal = true;
                    var cnl = new CircuitNodeLink();
                    cnl.Num = j + posts;
                    cnl.Elm = ce;
                    cn.Links.Add(cnl);
                    ce.SetNode(cnl.Num, NodeList.Count);
                    NodeList.Add(cn);
                }
                vscount += ivs;
            }

            makePostDrawList();
            if (!calcWireInfo()) {
                return;
            }
            mNodeMap = null; /* done with this */

            VoltageSources = new CircuitElm[vscount];
            vscount = 0;
            CircuitNonLinear = false;
            if (debug) Console.WriteLine("ac3");

            /* determine if circuit is nonlinear */
            for (int i = 0; i != elmList.Count; i++) {
                var ce = mSim.getElm(i);
                if (ce.NonLinear) {
                    CircuitNonLinear = true;
                }
                int ivs = ce.VoltageSourceCount;
                for (int j = 0; j != ivs; j++) {
                    VoltageSources[vscount] = ce;
                    ce.SetVoltageSource(j, vscount++);
                }
            }
            VoltageSourceCount = vscount;

            int matrixSize = NodeList.Count - 1 + vscount;
            Matrix = new double[matrixSize, matrixSize];
            mRightSide = new double[matrixSize];
            mOrigMatrix = new double[matrixSize, matrixSize];
            mOrigRightSide = new double[matrixSize];
            mMatrixSize = mMatrixFullSize = matrixSize;
            mRowInfo = new RowInfo[matrixSize];
            mPermute = new int[matrixSize];
            for (int i = 0; i != matrixSize; i++) {
                mRowInfo[i] = new RowInfo();
            }
            mCircuitNeedsMap = false;

            /* stamp linear circuit elements */
            for (int i = 0; i != elmList.Count; i++) {
                var ce = mSim.getElm(i);
                ce.Stamp();
            }
            if (debug) Console.WriteLine("ac4");

            /* determine nodes that are not connected indirectly to ground */
            var closure = new bool[NodeList.Count];
            bool changed = true;
            closure[0] = true;
            while (changed) {
                changed = false;
                for (int i = 0; i != elmList.Count; i++) {
                    var ce = mSim.getElm(i);
                    if (ce is WireElm) {
                        continue;
                    }
                    /* loop through all ce's nodes to see if they are connected
                    /* to other nodes not in closure */
                    for (int j = 0; j < ce.ConnectionNodeCount; j++) {
                        if (!closure[ce.GetConnectionNode(j)]) {
                            if (ce.HasGroundConnection(j)) {
                                closure[ce.GetConnectionNode(j)] = changed = true;
                            }
                            continue;
                        }
                        int k;
                        for (k = 0; k != ce.ConnectionNodeCount; k++) {
                            if (j == k) {
                                continue;
                            }
                            int kn = ce.GetConnectionNode(k);
                            if (ce.GetConnection(j, k) && !closure[kn]) {
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
            if (debug) Console.WriteLine("ac5");

            for (int i = 0; i != elmList.Count; i++) {
                var ce = mSim.getElm(i);

                /* look for inductors with no current path */
                if (ce is InductorElm) {
                    var fpi = new FindPathInfo(FindPathInfo.INDUCT, ce, ce.Nodes[1], elmList, NodeList.Count);
                    if (!fpi.FindPath(ce.Nodes[0])) {
                        if (debug) Console.WriteLine(ce + " no path");
                        ce.Reset();
                    }
                }

                /* look for current sources with no current path */
                if (ce is CurrentElm) {
                    var cur = (CurrentElm)ce;
                    var fpi = new FindPathInfo(FindPathInfo.INDUCT, ce, ce.Nodes[1], elmList, NodeList.Count);
                    if (!fpi.FindPath(ce.Nodes[0])) {
                        cur.stampCurrentSource(true);
                    } else {
                        cur.stampCurrentSource(false);
                    }
                }

                if (ce is VCCSElm) {
                    var cur = (VCCSElm)ce;
                    var fpi = new FindPathInfo(FindPathInfo.INDUCT, ce, cur.getOutputNode(0), elmList, NodeList.Count);
                    if (cur.hasCurrentOutput() && !fpi.FindPath(cur.getOutputNode(1))) {
                        cur.broken = true;
                    } else {
                        cur.broken = false;
                    }
                }

                /* look for voltage source or wire loops.  we do this for voltage sources or wire-like elements (not actual wires
                /* because those are optimized out, so the findPath won't work) */
                if (2 == ce.PostCount) {
                    if ((ce is VoltageElm) || (ce.IsWire && !(ce is WireElm))) {
                        var fpi = new FindPathInfo(FindPathInfo.VOLTAGE, ce, ce.Nodes[1], elmList, NodeList.Count);
                        if (fpi.FindPath(ce.Nodes[0])) {
                            Stop("Voltage source/wire loop with no resistance!", ce);
                            return;
                        }
                    }
                } else if (ce is Switch2Elm) {
                    /* for Switch2Elms we need to do extra work to look for wire loops */
                    var fpi = new FindPathInfo(FindPathInfo.VOLTAGE, ce, ce.Nodes[0], elmList, NodeList.Count);
                    for (int j = 1; j < ce.PostCount; j++) {
                        if (ce.GetConnection(0, j) && fpi.FindPath(ce.Nodes[j])) {
                            Stop("Voltage source/wire loop with no resistance!", ce);
                            return;
                        }
                    }
                }

                /* look for path from rail to ground */
                if ((ce is RailElm) || (ce is LogicInputElm)) {
                    var fpi = new FindPathInfo(FindPathInfo.VOLTAGE, ce, ce.Nodes[0], elmList, NodeList.Count);
                    if (fpi.FindPath(0)) {
                        Stop("Path to ground with no resistance!", ce);
                        return;
                    }
                }

                /* look for shorted caps, or caps w/ voltage but no R */
                if (ce is CapacitorElm) {
                    var fpi = new FindPathInfo(FindPathInfo.SHORT, ce, ce.Nodes[1], elmList, NodeList.Count);
                    if (fpi.FindPath(ce.Nodes[0])) {
                        Console.WriteLine(ce + " shorted");
                        ((CapacitorElm)ce).shorted();
                    } else {
                        /* a capacitor loop used to cause a matrix error. but we changed the capacitor model
                        /* so it works fine now. The only issue is if a capacitor is added in parallel with
                        /* another capacitor with a nonzero voltage; in that case we will get oscillation unless
                        /* we reset both capacitors to have the same voltage. Rather than check for that, we just
                        /* give an error. */
                        fpi = new FindPathInfo(FindPathInfo.CAP_V, ce, ce.Nodes[1], elmList, NodeList.Count);
                        if (fpi.FindPath(ce.Nodes[0])) {
                            Stop("Capacitor loop with no resistance!", ce);
                            return;
                        }
                    }
                }
            }
            if (debug) Console.WriteLine("ac6");

            if (!simplifyMatrix(matrixSize)) {
                return;
            }

            if (debug) {
                Console.WriteLine("matrixSize = " + matrixSize + " " + CircuitNonLinear);
                for (int j = 0; j != mMatrixSize; j++) {
                    Console.WriteLine("RightSide[{0}]:{1}", j, mRightSide[j]);
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
                    Stop("Singular matrix!", null);
                    return;
                }
            }

            /* show resistance in voltage sources if there's only one */
            bool gotVoltageSource = false;
            ShowResistanceInVoltageSources = true;
            for (int i = 0; i != elmList.Count; i++) {
                var ce = mSim.getElm(i);
                if (ce is VoltageElm) {
                    if (gotVoltageSource) {
                        ShowResistanceInVoltageSources = false;
                    } else {
                        gotVoltageSource = true;
                    }
                }
            }
        }

        public bool Run(bool debugprint) {
            const int subiterCount = 5000;
            int i, j, k, subiter;

            for (subiter = 0; subiter != subiterCount; subiter++) {
                Converged = true;
                SubIterations = subiter;
                for (i = 0; i != mMatrixSize; i++) {
                    mRightSide[i] = mOrigRightSide[i];
                }
                if (CircuitNonLinear) {
                    for (i = 0; i != mMatrixSize; i++) {
                        for (j = 0; j != mMatrixSize; j++) {
                            Matrix[i, j] = mOrigMatrix[i, j];
                        }
                    }
                }
                for (i = 0; i != mSim.ElmList.Count; i++) {
                    var ce = mSim.getElm(i);
                    ce.DoStep();
                }
                if (StopMessage != null) {
                    return false;
                }

                bool printit = debugprint;
                debugprint = false;
                for (j = 0; j != mMatrixSize; j++) {
                    for (i = 0; i != mMatrixSize; i++) {
                        double x = Matrix[i, j];
                        if (double.IsNaN(x) || double.IsInfinity(x)) {
                            Stop("nan/infinite matrix!", null);
                            return false;
                        }
                    }
                }
                if (printit) {
                    for (j = 0; j != mMatrixSize; j++) {
                        string x = "";
                        for (i = 0; i != mMatrixSize; i++) {
                            x += Matrix[j, i] + ",";
                        }
                        x += "\n";
                        Console.WriteLine(x);
                    }
                    Console.WriteLine("done");
                }
                if (CircuitNonLinear) {
                    if (Converged && subiter > 0) {
                        break;
                    }
                    if (!luFactor(Matrix, mMatrixSize, mPermute)) {
                        Stop("Singular matrix!", null);
                        return false;
                    }
                }
                luSolve(Matrix, mMatrixSize, mPermute, mRightSide);

                for (j = 0; j != mMatrixFullSize; j++) {
                    var ri = mRowInfo[j];
                    double res = 0;
                    if (ri.Type == RowInfo.ROW_CONST) {
                        res = ri.Value;
                    } else {
                        res = mRightSide[ri.MapCol];
                    }
                    /*Console.WriteLine(j + " " + res + " " + ri.type + " " + ri.mapCol);*/
                    if (double.IsNaN(res)) {
                        Converged = false;
                        debugprint = true;
                        break;
                    }
                    if (j < NodeList.Count - 1) {
                        var cn = getCircuitNode(j + 1);
                        for (k = 0; k != cn.Links.Count; k++) {
                            var cnl = cn.Links[k];
                            cnl.Elm.SetNodeVoltage(cnl.Num, res);
                        }
                    } else {
                        int ji = j - (NodeList.Count - 1);
                        /*Console.WriteLine("setting vsrc " + ji + " to " + res); */
                        VoltageSources[ji].SetCurrent(ji, res);
                    }
                }
                if (!CircuitNonLinear) {
                    break;
                }
            }

            if (subiter > 5) {
                Console.WriteLine("converged after " + subiter + " iterations\n");
            }

            if (subiter == subiterCount) {
                Stop("Convergence failed!", null);
                return false;
            }

            return true;
        }

        public void Stop(string s, CircuitElm ce) {
            StopMessage = s;
            Matrix = null;  /* causes an exception */
            StopElm = ce;
            mSim.SetSimRunning(false);
        }
    }
}
