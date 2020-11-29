using System;
using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements;

namespace Circuit {
    class RowInfo {
        public const int ROW_NORMAL = 0;  /* ordinary value */
        public const int ROW_CONST = 1;  /* value is constant */
        public int type;
        public int mapCol;
        public int mapRow;
        public double value;
        public bool rsChanges; /* row's right side changes */
        public bool lsChanges; /* row's left side changes */
        public bool dropRow;   /* row is not needed in matrix */
        public RowInfo() { type = ROW_NORMAL; }
    }

    class WireInfo {
        public WireElm wire;
        public List<CircuitElm> neighbors;
        public int post;
        public WireInfo(WireElm w) { wire = w; }
    }

    class NodeMapEntry {
        public int node;
        public NodeMapEntry() { node = -1; }
        public NodeMapEntry(int n) { node = n; }
    }

    class Circuit {
        #region private varidate
        CirSim mSim;

        public List<CircuitNode> NodeList;
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

        #region public variable
        public string StopMessage;
        public CircuitElm StopElm;

        public List<Point> PostDrawList { get; private set; } = new List<Point>();
        public List<Point> BadConnectionList { get; private set; } = new List<Point>();

        public double[,] Matrix;

        public int VoltageSourceCount { get; private set; }
        public CircuitElm[] VoltageSources { get; private set; }

        public bool CircuitNonLinear { get; private set; }

        public bool ShowResistanceInVoltageSources { get; private set; }

        public bool Converged;
        public int SubIterations { get; private set; }
        #endregion

        public Circuit(CirSim sim) {
            mSim = sim;
        }

        /* factors a matrix into upper and lower triangular matrices by
        /* gaussian elimination.  On entry, a[0..n-1][0..n-1] is the
        /* matrix to be factored.  ipvt[] returns an integer vector of pivot
        /* indices, used in the lu_solve() routine. */
        static bool lu_factor(double[,] a, int n, int[] ipvt) {
            int i, j, k;

            /* check for a possible singular matrix by scanning for rows that
            /* are all zeroes */
            for (i = 0; i != n; i++) {
                bool row_all_zeros = true;
                for (j = 0; j != n; j++) {
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
            for (j = 0; j != n; j++) {
                /* calculate upper triangular elements for this column */
                for (i = 0; i != j; i++) {
                    double q = a[i, j];
                    for (k = 0; k != i; k++) {
                        q -= a[i, k] * a[k, j];
                    }
                    a[i, j] = q;
                }
                /* calculate lower triangular elements for this column */
                double largest = 0;
                int largestRow = -1;
                for (i = j; i != n; i++) {
                    double q = a[i, j];
                    for (k = 0; k != j; k++) {
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
                    for (k = 0; k != n; k++) {
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
                    for (i = j + 1; i != n; i++) {
                        a[i, j] *= mult;
                    }
                }
            }
            return true;
        }

        /* Solves the set of n linear equations using a LU factorization
        /* previously performed by lu_factor.  On input, b[0..n-1] is the right
        /* hand side of the equations, and on output, contains the solution. */
        static void lu_solve(double[,] a, int n, int[] ipvt, double[] b) {
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
                int j;
                double tot = b[row];
                b[row] = b[i];
                /* forward substitution using the lower triangular matrix */
                for (j = bi; j < i; j++) {
                    tot -= a[i, j] * b[j];
                }
                b[i] = tot;
            }
            for (i = n - 1; i >= 0; i--) {
                double tot = b[i];
                /* back-substitution using the upper triangular matrix */
                int j;
                for (j = i + 1; j != n; j++) {
                    tot -= a[i, j] * b[j];
                }
                b[i] = tot / a[i, i];
            }
        }

        /* simplify the matrix; this speeds things up quite a bit, especially for
        /* digital circuits */
        bool simplifyMatrix(int matrixSize) {
            int i, j;
            for (i = 0; i != matrixSize; i++) {
                int qp = -1;
                double qv = 0;
                var re = mRowInfo[i];
                /*Console.WriteLine("row " + i + " " + re.lsChanges + " " + re.rsChanges + " " + re.dropRow);*/
                if (re.lsChanges || re.dropRow || re.rsChanges) {
                    continue;
                }
                double rsadd = 0;

                /* look for rows that can be removed */
                for (j = 0; j != matrixSize; j++) {
                    double q = Matrix[i, j];
                    if (mRowInfo[j].type == RowInfo.ROW_CONST) {
                        /* keep a running total of const values that have been
                        /* removed already */
                        rsadd -= mRowInfo[j].value * q;
                        continue;
                    }
                    /* ignore zeroes */
                    if (q == 0) {
                        continue;
                    }
                    /* keep track of first nonzero element that is not ROW_CONST */
                    if (qp == -1) {
                        qp = j;
                        qv = q;
                        continue;
                    }
                    /* more than one nonzero element?  give up */
                    break;
                }
                if (j == matrixSize) {
                    if (qp == -1) {
                        /* probably a singular matrix, try disabling matrix simplification above to check this */
                        stop("Matrix error", null);
                        return false;
                    }
                    var elt = mRowInfo[qp];
                    /* we found a row with only one nonzero nonconst entry; that value
                    /* is a constant */
                    if (elt.type != RowInfo.ROW_NORMAL) {
                        Console.WriteLine("type already " + elt.type + " for " + qp + "!");
                        continue;
                    }
                    elt.type = RowInfo.ROW_CONST;
                    /*Console.WriteLine("ROW_CONST " + i + " " + rsadd);*/
                    elt.value = (mRightSide[i] + rsadd) / qv;
                    mRowInfo[i].dropRow = true;
                    i = -1; /* start over from scratch */
                }
            }
            /*Console.WriteLine("ac7");*/

            /* find size of new matrix */
            int nn = 0;
            for (i = 0; i != matrixSize; i++) {
                RowInfo elt = mRowInfo[i];
                if (elt.type == RowInfo.ROW_NORMAL) {
                    elt.mapCol = nn++;
                    /*Console.WriteLine("col " + i + " maps to " + elt.mapCol);*/
                    continue;
                }
                if (elt.type == RowInfo.ROW_CONST) {
                    elt.mapCol = -1;
                }
            }

            /* make the new, simplified matrix */
            int newsize = nn;
            var newmatx = new double[newsize, newsize];
            var newrs = new double[newsize];
            int ii = 0;
            for (i = 0; i != matrixSize; i++) {
                RowInfo rri = mRowInfo[i];
                if (rri.dropRow) {
                    rri.mapRow = -1;
                    continue;
                }
                newrs[ii] = mRightSide[i];
                rri.mapRow = ii;
                /*Console.WriteLine("Row " + i + " maps to " + ii); */
                for (j = 0; j != matrixSize; j++) {
                    var ri = mRowInfo[j];
                    if (ri.type == RowInfo.ROW_CONST) {
                        newrs[ii] -= ri.value * Matrix[i, j];
                    } else {
                        newmatx[ii, ri.mapCol] += Matrix[i, j];
                    }
                }
                ii++;
            }

            /*Console.WriteLine("old size = " + matrixSize + " new size = " + newsize);*/

            Matrix = newmatx;
            mRightSide = newrs;
            matrixSize = mMatrixSize = newsize;
            for (i = 0; i != matrixSize; i++) {
                mOrigRightSide[i] = mRightSide[i];
            }
            for (i = 0; i != matrixSize; i++) {
                for (j = 0; j != matrixSize; j++) {
                    mOrigMatrix[i, j] = Matrix[i, j];
                }
            }
            mCircuitNeedsMap = true;
            return true;
        }

        /* find groups of nodes connected by wires and map them to the same node.  this speeds things
        /* up considerably by reducing the size of the matrix */
        void calculateWireClosure() {
            int i;
            int mergeCount = 0;
            mNodeMap = new Dictionary<Point, NodeMapEntry>();
            mWireInfoList = new List<WireInfo>();
            for (i = 0; i != mSim.elmList.Count; i++) {
                var ce = mSim.getElm(i);
                if (!(ce is WireElm)) {
                    continue;
                }
                var we = (WireElm)ce;
                we.hasWireInfo = false;
                mWireInfoList.Add(new WireInfo(we));
                var p1 = ce.getPost(0);
                var p2 = ce.getPost(1);
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
                    mNodeMap.Add(ce.getPost(1), cn1);
                    continue;
                }
                if (cp2) {
                    var cn2 = mNodeMap[p2];
                    mNodeMap.Add(ce.getPost(0), cn2);
                    continue;
                }
                /* new entry */
                var cn = new NodeMapEntry();
                mNodeMap.Add(ce.getPost(0), cn);
                mNodeMap.Add(ce.getPost(1), cn);
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
            int i;
            int moved = 0;
            for (i = 0; i != mWireInfoList.Count; i++) {
                var wi = mWireInfoList[i];
                var wire = wi.wire;
                var cn1 = NodeList[wire.getNode(0)];  /* both ends of wire have same node # */
                int j;

                var neighbors0 = new List<CircuitElm>();
                var neighbors1 = new List<CircuitElm>();
                bool isReady0 = true;
                bool isReady1 = true;

                /* go through elements sharing a node with this wire (may be connected indirectly
                /* by other wires, but at least it's faster than going through all elements) */
                for (j = 0; j != cn1.links.Count; j++) {
                    var cnl = cn1.links[j];
                    var ce = cnl.elm;
                    if (ce == wire) {
                        continue;
                    }
                    var pt = cnl.elm.getPost(cnl.num);

                    /* is this a wire that doesn't have wire info yet?  If so we can't use it.
                    /* That would create a circular dependency */
                    bool notReady = (ce is WireElm) && !((WireElm)ce).hasWireInfo;

                    /* which post does this element connect to, if any? */
                    if (pt.X == wire.x1 && pt.Y == wire.y1) {
                        neighbors0.Add(ce);
                        if (notReady) {
                            isReady0 = false;
                        }
                    } else if (pt.X == wire.x2 && pt.Y == wire.y2) {
                        neighbors1.Add(ce);
                        if (notReady) {
                            isReady1 = false;
                        }
                    }
                }

                /* does one of the posts have all information necessary to calculate current */
                if (isReady0) {
                    wi.neighbors = neighbors0;
                    wi.post = 0;
                    wire.hasWireInfo = true;
                    moved = 0;
                } else if (isReady1) {
                    wi.neighbors = neighbors1;
                    wi.post = 1;
                    wire.hasWireInfo = true;
                    moved = 0;
                } else {
                    /* move to the end of the list and try again later */
                    var tmp = mWireInfoList[i];
                    mWireInfoList.RemoveAt(i--);
                    mWireInfoList.Add(tmp);
                    moved++;
                    if (moved > mWireInfoList.Count * 2) {
                        stop("wire loop detected", wire);
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
                    int j;
                    bool bad = false;
                    var cn = entry.Key;
                    for (j = 0; j != mSim.elmList.Count && !bad; j++) {
                        var ce = mSim.getElm(j);
                        if (ce is GraphicElm) {
                            continue;
                        }
                        /* does this post intersect elm's bounding box? */
                        if (!ce.boundingBox.Contains(cn.X, cn.Y)) {
                            continue;
                        }
                        int k;
                        /* does this post belong to the elm? */
                        int pc = ce.getPostCount();
                        for (k = 0; k != pc; k++) {
                            if (ce.getPost(k).Equals(cn)) {
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

        public void stop(string s, CircuitElm ce) {
            StopMessage = s;
            Matrix = null;  /* causes an exception */
            StopElm = ce;
            mSim.setSimRunning(false);
            mSim.analyzeFlag = false;
        }

        /* stamp independent voltage source #vs, from n1 to n2, amount v */
        public void stampVoltageSource(int n1, int n2, int vs, double v) {
            int vn = NodeList.Count + vs;
            stampMatrix(vn, n1, -1);
            stampMatrix(vn, n2, 1);
            stampRightSide(vn, v);
            stampMatrix(n1, vn, 1);
            stampMatrix(n2, vn, -1);
        }

        /* use this if the amount of voltage is going to be updated in doStep(), by updateVoltageSource() */
        public void stampVoltageSource(int n1, int n2, int vs) {
            int vn = NodeList.Count + vs;
            stampMatrix(vn, n1, -1);
            stampMatrix(vn, n2, 1);
            stampRightSide(vn);
            stampMatrix(n1, vn, 1);
            stampMatrix(n2, vn, -1);
        }

        /* update voltage source in doStep() */
        public void updateVoltageSource(int n1, int n2, int vs, double v) {
            int vn = NodeList.Count + vs;
            stampRightSide(vn, v);
        }

        public void stampResistor(int n1, int n2, double r) {
            double r0 = 1 / r;
            if (double.IsNaN(r0) || double.IsInfinity(r0)) {
                Console.WriteLine("bad resistance " + r + " " + r0 + "\n");
                int a = 0;
                a /= a;
            }
            stampMatrix(n1, n1, r0);
            stampMatrix(n2, n2, r0);
            stampMatrix(n1, n2, -r0);
            stampMatrix(n2, n1, -r0);
        }

        public void stampConductance(int n1, int n2, double r0) {
            stampMatrix(n1, n1, r0);
            stampMatrix(n2, n2, r0);
            stampMatrix(n1, n2, -r0);
            stampMatrix(n2, n1, -r0);
        }

        /* current from cn1 to cn2 is equal to voltage from vn1 to 2, divided by g */
        public void stampVCCurrentSource(int cn1, int cn2, int vn1, int vn2, double g) {
            stampMatrix(cn1, vn1, g);
            stampMatrix(cn2, vn2, g);
            stampMatrix(cn1, vn2, -g);
            stampMatrix(cn2, vn1, -g);
        }

        public void stampCurrentSource(int n1, int n2, double i) {
            stampRightSide(n1, -i);
            stampRightSide(n2, i);
        }

        /* stamp a current source from n1 to n2 depending on current through vs */
        public void stampCCCS(int n1, int n2, int vs, double gain) {
            int vn = NodeList.Count + vs;
            stampMatrix(n1, vn, gain);
            stampMatrix(n2, vn, -gain);
        }

        /* stamp value x in row i, column j, meaning that a voltage change
        /* of dv in node j will increase the current into node i by x dv.
        /* (Unless i or j is a voltage source node.) */
        public void stampMatrix(int i, int j, double x) {
            if (i > 0 && j > 0) {
                if (mCircuitNeedsMap) {
                    i = mRowInfo[i - 1].mapRow;
                    var ri = mRowInfo[j - 1];
                    if (ri.type == RowInfo.ROW_CONST) {
                        /*Console.WriteLine("Stamping constant " + i + " " + j + " " + x);*/
                        mRightSide[i] -= x * ri.value;
                        return;
                    }
                    j = ri.mapCol;
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
        public void stampRightSide(int i, double x) {
            if (i > 0) {
                if (mCircuitNeedsMap) {
                    i = mRowInfo[i - 1].mapRow;
                    /*Console.WriteLine("stamping " + i + " " + x);*/
                } else {
                    i--;
                }
                mRightSide[i] += x;
            }
        }

        /* indicate that the value on the right side of row i changes in doStep() */
        public void stampRightSide(int i) {
            /*Console.WriteLine("rschanges true " + (i-1)); */
            if (i > 0) {
                mRowInfo[i - 1].rsChanges = true;
            }
        }

        /* indicate that the values on the left side of row i change in doStep() */
        public void stampNonLinear(int i) {
            if (i > 0) {
                mRowInfo[i - 1].lsChanges = true;
            }
        }

        /* we removed wires from the matrix to speed things up.  in order to display wire currents,
        /* we need to calculate them now. */
        public void calcWireCurrents() {
            /* for debugging */
            /*for (int i = 0; i != mWireInfoList.Count; i++) {
                mWireInfoList[i].wire.setCurrent(-1, 1.23);
            }*/
            for (int i = 0; i != mWireInfoList.Count; i++) {
                var wi = mWireInfoList[i];
                double cur = 0;
                var p = wi.wire.getPost(wi.post);
                for (int j = 0; j != wi.neighbors.Count; j++) {
                    var ce = wi.neighbors[j];
                    int n = ce.getNodeAtPoint(p.X, p.Y);
                    cur += ce.getCurrentIntoNode(n);
                }
                if (wi.post == 0) {
                    wi.wire.setCurrent(-1, cur);
                } else {
                    wi.wire.setCurrent(-1, -cur);
                }
            }
        }

        public void analyzeCircuit() {
            bool debug = false;
            var elmList = mSim.elmList;
            if (elmList.Count == 0) {
                PostDrawList = new List<Point>();
                BadConnectionList = new List<Point>();
                return;
            }

            StopMessage = null;
            StopElm = null;
            int i, j;
            int vscount = 0;
            NodeList = new List<CircuitNode>();
            mPostCountMap = new Dictionary<Point, int>();
            bool gotGround = false;
            bool gotRail = false;
            CircuitElm volt = null;

            calculateWireClosure();

            if (debug) Console.WriteLine("ac1");
            /* look for voltage or ground element */
            for (i = 0; i != elmList.Count; i++) {
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
                var pt = volt.getPost(0);
                NodeList.Add(cn);
                /* update node map */
                if (mNodeMap.ContainsKey(pt)) {
                    mNodeMap[pt].node = 0;
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
            LabeledNodeElm.resetNodeList();
            for (i = 0; i != elmList.Count; i++) {
                var ce = mSim.getElm(i);
                int inodes = ce.getInternalNodeCount();
                int ivs = ce.getVoltageSourceCount();
                int posts = ce.getPostCount();

                /* allocate a node for each post and match posts to nodes */
                for (j = 0; j != posts; j++) {
                    var pt = ce.getPost(j);
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
                    if (!ccln || cln.node == -1) {
                        var cn = new CircuitNode();
                        var cnl = new CircuitNodeLink();
                        cnl.num = j;
                        cnl.elm = ce;
                        cn.links.Add(cnl);
                        ce.setNode(j, NodeList.Count);
                        if (ccln) {
                            cln.node = NodeList.Count;
                        } else {
                            mNodeMap.Add(pt, new NodeMapEntry(NodeList.Count));
                        }
                        NodeList.Add(cn);
                    } else {
                        int n = cln.node;
                        var cnl = new CircuitNodeLink();
                        cnl.num = j;
                        cnl.elm = ce;
                        getCircuitNode(n).links.Add(cnl);
                        ce.setNode(j, n);
                        /* if it's the ground node, make sure the node voltage is 0,
                        /* cause it may not get set later */
                        if (n == 0) {
                            ce.setNodeVoltage(j, 0);
                        }
                    }
                }
                for (j = 0; j != inodes; j++) {
                    var cn = new CircuitNode();
                    cn._internal = true;
                    var cnl = new CircuitNodeLink();
                    cnl.num = j + posts;
                    cnl.elm = ce;
                    cn.links.Add(cnl);
                    ce.setNode(cnl.num, NodeList.Count);
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
            for (i = 0; i != elmList.Count; i++) {
                var ce = mSim.getElm(i);
                if (ce.nonLinear()) {
                    CircuitNonLinear = true;
                }
                int ivs = ce.getVoltageSourceCount();
                for (j = 0; j != ivs; j++) {
                    VoltageSources[vscount] = ce;
                    ce.setVoltageSource(j, vscount++);
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
            for (i = 0; i != matrixSize; i++) {
                mRowInfo[i] = new RowInfo();
            }
            mCircuitNeedsMap = false;

            /* stamp linear circuit elements */
            for (i = 0; i != elmList.Count; i++) {
                var ce = mSim.getElm(i);
                ce.stamp();
            }
            if (debug) Console.WriteLine("ac4");

            /* determine nodes that are not connected indirectly to ground */
            var closure = new bool[NodeList.Count];
            bool changed = true;
            closure[0] = true;
            while (changed) {
                changed = false;
                for (i = 0; i != elmList.Count; i++) {
                    var ce = mSim.getElm(i);
                    if (ce is WireElm) {
                        continue;
                    }
                    /* loop through all ce's nodes to see if they are connected
                    /* to other nodes not in closure */
                    for (j = 0; j < ce.getConnectionNodeCount(); j++) {
                        if (!closure[ce.getConnectionNode(j)]) {
                            if (ce.hasGroundConnection(j)) {
                                closure[ce.getConnectionNode(j)] = changed = true;
                            }
                            continue;
                        }
                        int k;
                        for (k = 0; k != ce.getConnectionNodeCount(); k++) {
                            if (j == k) {
                                continue;
                            }
                            int kn = ce.getConnectionNode(k);
                            if (ce.getConnection(j, k) && !closure[kn]) {
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
                for (i = 0; i != NodeList.Count; i++) {
                    if (!closure[i] && !getCircuitNode(i)._internal) {
                        Console.WriteLine("node " + i + " unconnected");
                        stampResistor(0, i, 1e8);
                        closure[i] = true;
                        changed = true;
                        break;
                    }
                }
            }
            if (debug) Console.WriteLine("ac5");

            for (i = 0; i != elmList.Count; i++) {
                var ce = mSim.getElm(i);

                /* look for inductors with no current path */
                if (ce is InductorElm) {
                    var fpi = new FindPathInfo(FindPathInfo.INDUCT, ce, ce.getNode(1), elmList, NodeList.Count);
                    if (!fpi.findPath(ce.getNode(0))) {
                        if (debug) Console.WriteLine(ce + " no path");
                        ce.reset();
                    }
                }

                /* look for current sources with no current path */
                if (ce is CurrentElm) {
                    var cur = (CurrentElm)ce;
                    var fpi = new FindPathInfo(FindPathInfo.INDUCT, ce, ce.getNode(1), elmList, NodeList.Count);
                    if (!fpi.findPath(ce.getNode(0))) {
                        cur.stampCurrentSource(true);
                    } else {
                        cur.stampCurrentSource(false);
                    }
                }

                if (ce is VCCSElm) {
                    var cur = (VCCSElm)ce;
                    var fpi = new FindPathInfo(FindPathInfo.INDUCT, ce, cur.getOutputNode(0), elmList, NodeList.Count);
                    if (cur.hasCurrentOutput() && !fpi.findPath(cur.getOutputNode(1))) {
                        cur.broken = true;
                    } else {
                        cur.broken = false;
                    }
                }

                /* look for voltage source or wire loops.  we do this for voltage sources or wire-like elements (not actual wires
                /* because those are optimized out, so the findPath won't work) */
                if (ce.getPostCount() == 2) {
                    if ((ce is VoltageElm) || (ce.isWire() && !(ce is WireElm))) {
                        var fpi = new FindPathInfo(FindPathInfo.VOLTAGE, ce, ce.getNode(1), elmList, NodeList.Count);
                        if (fpi.findPath(ce.getNode(0))) {
                            stop("Voltage source/wire loop with no resistance!", ce);
                            return;
                        }
                    }
                } else if (ce is Switch2Elm) {
                    /* for Switch2Elms we need to do extra work to look for wire loops */
                    var fpi = new FindPathInfo(FindPathInfo.VOLTAGE, ce, ce.getNode(0), elmList, NodeList.Count);
                    for (j = 1; j < ce.getPostCount(); j++) {
                        if (ce.getConnection(0, j) && fpi.findPath(ce.getNode(j))) {
                            stop("Voltage source/wire loop with no resistance!", ce);
                            return;
                        }
                    }
                }

                /* look for path from rail to ground */
                if ((ce is RailElm) || (ce is LogicInputElm)) {
                    var fpi = new FindPathInfo(FindPathInfo.VOLTAGE, ce, ce.getNode(0), elmList, NodeList.Count);
                    if (fpi.findPath(0)) {
                        stop("Path to ground with no resistance!", ce);
                        return;
                    }
                }

                /* look for shorted caps, or caps w/ voltage but no R */
                if (ce is CapacitorElm) {
                    var fpi = new FindPathInfo(FindPathInfo.SHORT, ce, ce.getNode(1), elmList, NodeList.Count);
                    if (fpi.findPath(ce.getNode(0))) {
                        Console.WriteLine(ce + " shorted");
                        ((CapacitorElm)ce).shorted();
                    } else {
                        /* a capacitor loop used to cause a matrix error. but we changed the capacitor model
                        /* so it works fine now. The only issue is if a capacitor is added in parallel with
                        /* another capacitor with a nonzero voltage; in that case we will get oscillation unless
                        /* we reset both capacitors to have the same voltage. Rather than check for that, we just
                        /* give an error. */
                        fpi = new FindPathInfo(FindPathInfo.CAP_V, ce, ce.getNode(1), elmList, NodeList.Count);
                        if (fpi.findPath(ce.getNode(0))) {
                            stop("Capacitor loop with no resistance!", ce);
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
                for (j = 0; j != mMatrixSize; j++) {
                    Console.WriteLine("RightSide[{0}]:{1}", j, mRightSide[j]);
                    for (i = 0; i != mMatrixSize; i++) {
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
                if (!lu_factor(Matrix, mMatrixSize, mPermute)) {
                    stop("Singular matrix!", null);
                    return;
                }
            }

            /* show resistance in voltage sources if there's only one */
            bool gotVoltageSource = false;
            ShowResistanceInVoltageSources = true;
            for (i = 0; i != elmList.Count; i++) {
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

        public bool run(bool debugprint) {
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
                for (i = 0; i != mSim.elmList.Count; i++) {
                    var ce = mSim.getElm(i);
                    ce.doStep();
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
                            stop("nan/infinite matrix!", null);
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
                    if (!lu_factor(Matrix, mMatrixSize, mPermute)) {
                        stop("Singular matrix!", null);
                        return false;
                    }
                }
                lu_solve(Matrix, mMatrixSize, mPermute, mRightSide);

                for (j = 0; j != mMatrixFullSize; j++) {
                    var ri = mRowInfo[j];
                    double res = 0;
                    if (ri.type == RowInfo.ROW_CONST) {
                        res = ri.value;
                    } else {
                        res = mRightSide[ri.mapCol];
                    }
                    /*Console.WriteLine(j + " " + res + " " + ri.type + " " + ri.mapCol);*/
                    if (double.IsNaN(res)) {
                        Converged = false;
                        debugprint = true;
                        break;
                    }
                    if (j < NodeList.Count - 1) {
                        var cn = getCircuitNode(j + 1);
                        for (k = 0; k != cn.links.Count; k++) {
                            var cnl = cn.links[k];
                            cnl.elm.setNodeVoltage(cnl.num, res);
                        }
                    } else {
                        int ji = j - (NodeList.Count - 1);
                        /*Console.WriteLine("setting vsrc " + ji + " to " + res); */
                        VoltageSources[ji].setCurrent(ji, res);
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
                stop("Convergence failed!", null);
                return false;
            }

            return true;
        }
    }
}
