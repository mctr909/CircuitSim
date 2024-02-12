using Circuit.Elements.Input;
using Circuit.Elements.Output;
using Circuit.Elements.Passive;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Circuit {
	static class CircuitSymbol {
		class NodeMapEntry {
			public int Node;
			public NodeMapEntry() { Node = -1; }
			public NodeMapEntry(int n) { Node = n; }
		}

		class PathInfo {
			public enum TYPE {
				VOLTAGE,
				INDUCTOR,
				CAPACITOR,
				SHORT
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
					case TYPE.CAPACITOR:
						/* checking for capacitor/voltage source loops */
						if (!(cee.IsWire || (cee is ElmCapacitor) || (cee is ElmVoltage))) {
							continue;
						}
						break;
					}

					if (n1 == 0) {
						/* look for posts which have a ground connection;
						/* our path can go through ground */
						for (int j = 0; j != cee.ConnectionNodeCount; j++) {
							if (cee.HasGroundConnection(j) && FindPath(cee.GetConnectionNode(j))) {
								return true;
							}
						}
					}

					int nodeA;
					for (nodeA = 0; nodeA != cee.ConnectionNodeCount; nodeA++) {
						if (cee.GetConnectionNode(nodeA) == n1) {
							break;
						}
					}
					if (nodeA == cee.ConnectionNodeCount) {
						continue;
					}
					if (cee.HasGroundConnection(nodeA) && FindPath(0)) {
						return true;
					}

					if (mType == TYPE.INDUCTOR && (cee is ElmInductor)) {
						/* inductors can use paths with other inductors of matching current */
						var c = cee.Current;
						if (nodeA == 0) {
							c = -c;
						}
						if (Math.Abs(c - mFirstElm.Current) > 1e-10) {
							continue;
						}
					}

					for (int nodeB = 0; nodeB != cee.ConnectionNodeCount; nodeB++) {
						if (nodeA == nodeB) {
							continue;
						}
						if (cee.GetConnection(nodeA, nodeB) && FindPath(cee.GetConnectionNode(nodeB))) {
							/*Console.WriteLine("got findpath " + n1); */
							return true;
						}
					}
				}
				return false;
			}
		}

		public static bool IsRunning { get; set; }
		public static bool NeedAnalyze { get; set; }
		public static int Count { get { return null == List ? 0 : List.Count; } }
		public static List<BaseSymbol> List { get; private set; } = new List<BaseSymbol>();
		public static List<Point> DrawPostList { get; set; } = new List<Point>();
		public static List<Point> BadConnectionList { get; set; } = new List<Point>();

		static Dictionary<Point, int> mPostCountMap = new Dictionary<Point, int>();
		static Dictionary<Point, NodeMapEntry> mNodeMap = new Dictionary<Point, NodeMapEntry>();

		#region public method
		public static void Clear() {
			Circuit.ElmList.Clear();
		}
		public static void Add(BaseElement elm) {
			Circuit.ElmList.Add(elm);
		}
		public static void AnalyzeCircuit() {
			if (0 == Circuit.ElmList.Count) {
				DrawPostList = new List<Point>();
				BadConnectionList = new List<Point>();
				return;
			}

			Circuit.StopMessage = null;
			Circuit.Nodes = new List<CircuitNode>();
			mPostCountMap = new Dictionary<Point, int>();

			calculateWireClosure();

			{
				/* look for voltage or ground element */
				var gotGround = false;
				var gotRail = false;
				BaseElement volt = null;
				for (int i = 0; i != Circuit.ElmList.Count; i++) {
					var ce = Circuit.ElmList[i];
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
					var pt = volt.NodePos[0];
					Circuit.Nodes.Add(cn);
					/* update node map */
					if (mNodeMap.ContainsKey(pt)) {
						mNodeMap[pt].Node = 0;
					} else {
						mNodeMap.Add(pt, new NodeMapEntry(0));
					}
				} else {
					/* otherwise allocate extra node for ground */
					var cn = new CircuitNode();
					Circuit.Nodes.Add(cn);
				}
			}

			/* allocate nodes and voltage sources */
			int vs_count = 0;
			{
				ElmLabeledNode.ResetNodeList();
				for (int i = 0; i < Circuit.ElmList.Count; i++) {
					var ce = Circuit.ElmList[i];
					if (null == ce) {
						continue;
					}
					var inodes = ce.InternalNodeCount;
					var ivs = ce.VoltageSourceCount;
					var posts = ce.TermCount;

					/* allocate a node for each post and match posts to nodes */
					for (int j = 0; j < posts; j++) {
						var pt = ce.NodePos[j];
						if (mPostCountMap.ContainsKey(pt)) {
							var g = mPostCountMap[pt];
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
							ce.SetNode(j, Circuit.Nodes.Count);
							if (ccln) {
								cln.Node = Circuit.Nodes.Count;
							} else {
								mNodeMap.Add(pt, new NodeMapEntry(Circuit.Nodes.Count));
							}
							Circuit.Nodes.Add(cn);
						} else {
							var n = cln.Node;
							var cnl = new CircuitNode.LINK();
							cnl.Num = j;
							cnl.Elm = ce;
							getCircuitNode(n).Links.Add(cnl);
							ce.SetNode(j, n);
							/* if it's the ground node, make sure the node voltage is 0,
                            /* cause it may not get set later */
							if (n == 0) {
								ce.SetVoltage(j, 0);
							}
						}
					}
					for (int j = 0; j < inodes; j++) {
						var cnl = new CircuitNode.LINK();
						cnl.Num = j + posts;
						cnl.Elm = ce;
						var cn = new CircuitNode();
						cn.Internal = true;
						cn.Links.Add(cnl);
						ce.SetNode(cnl.Num, Circuit.Nodes.Count);
						Circuit.Nodes.Add(cn);
					}
					vs_count += ivs;
				}

				makePostDrawList();
				if (calcWireInfo()) {
					mNodeMap = null; /* done with this */
				} else {
					return;
				}

				Circuit.VoltageSources = new BaseElement[vs_count];
				vs_count = 0;
				for (int i = 0; i < Circuit.ElmList.Count; i++) {
					var ce = Circuit.ElmList[i];
					var ivs = ce.VoltageSourceCount;
					for (int j = 0; j < ivs; j++) {
						Circuit.VoltageSources[vs_count] = ce;
						ce.SetVoltageSource(j, vs_count++);
					}
				}
			}

			var matrixSize = Circuit.Nodes.Count - 1 + vs_count;
			Circuit.Matrix = new double[matrixSize, matrixSize];
			Circuit.RightSide = new double[matrixSize];
			Circuit.RowInfo = new Circuit.ROW_INFO[matrixSize];
			for (int i = 0; i < matrixSize; i++) {
				Circuit.RowInfo[i] = new Circuit.ROW_INFO();
			}

			Circuit.MatrixSize = Circuit.MatrixFullSize = matrixSize;
			Circuit.OrigMatrix = new double[matrixSize, matrixSize];
			Circuit.OrigRightSide = new double[matrixSize];
			Circuit.Permute = new int[matrixSize];
			Circuit.CircuitNeedsMap = false;

			/* stamp linear circuit elements */
			for (int i = 0; i < Circuit.ElmList.Count; i++) {
				Circuit.ElmList[i].Stamp();
			}

			/* determine nodes that are not connected indirectly to ground */
			var closure = new bool[Circuit.Nodes.Count];
			var changed = true;
			closure[0] = true;
			while (changed) {
				changed = false;
				for (int i = 0; i < Circuit.ElmList.Count; i++) {
					var ce = Circuit.ElmList[i];
					if (ce is ElmWire) {
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
						for (int k = 0; k != ce.ConnectionNodeCount; k++) {
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
				for (int i = 0; i != Circuit.Nodes.Count; i++) {
					if (!closure[i] && !getCircuitNode(i).Internal) {
						Circuit.StampResistor(0, i, 1e8);
						closure[i] = true;
						changed = true;
						break;
					}
				}
			}

			for (int i = 0; i < Circuit.ElmList.Count; i++) {
				var ce = Circuit.ElmList[i];

				/* look for inductors with no current path */
				if (ce is ElmInductor) {
					var fpi = new PathInfo(PathInfo.TYPE.INDUCTOR, ce, ce.Nodes[1], Circuit.ElmList, Circuit.Nodes.Count);
					if (!fpi.FindPath(ce.Nodes[0])) {
						ce.Reset();
					}
				}

				/* look for current sources with no current path */
				if (ce is ElmCurrent) {
					var cur = (ElmCurrent)ce;
					var fpi = new PathInfo(PathInfo.TYPE.INDUCTOR, ce, ce.Nodes[1], Circuit.ElmList, Circuit.Nodes.Count);
					if (!fpi.FindPath(ce.Nodes[0])) {
						cur.stampCurrentSource(true);
					} else {
						cur.stampCurrentSource(false);
					}
				}

				/* look for voltage source or wire loops.  we do this for voltage sources or wire-like elements (not actual wires
                /* because those are optimized out, so the findPath won't work) */
				if (2 == ce.TermCount) {
					if (ce is ElmVoltage) {
						var fpi = new PathInfo(PathInfo.TYPE.VOLTAGE, ce, ce.Nodes[1], Circuit.ElmList, Circuit.Nodes.Count);
						if (fpi.FindPath(ce.Nodes[0])) {
							Circuit.Stop("Voltage source/wire loop with no resistance!");
							return;
						}
					}
				} else {
					/* look for path from rail to ground */
					if (ce is ElmRail || ce is ElmLogicInput) {
						var fpi = new PathInfo(PathInfo.TYPE.VOLTAGE, ce, ce.Nodes[0], Circuit.ElmList, Circuit.Nodes.Count);
						if (fpi.FindPath(0)) {
							Circuit.Stop("Voltage source/wire loop with no resistance!");
							return;
						}
					}
				}

				/* look for shorted caps, or caps w/ voltage but no R */
				if (ce is ElmCapacitor) {
					var fpi = new PathInfo(PathInfo.TYPE.SHORT, ce, ce.Nodes[1], Circuit.ElmList, Circuit.Nodes.Count);
					if (fpi.FindPath(ce.Nodes[0])) {
						Console.WriteLine(ce + " shorted");
						ce.Shorted();
					} else {
						/* a capacitor loop used to cause a matrix error. but we changed the capacitor model
                        /* so it works fine now. The only issue is if a capacitor is added in parallel with
                        /* another capacitor with a nonzero voltage; in that case we will get oscillation unless
                        /* we reset both capacitors to have the same voltage. Rather than check for that, we just
                        /* give an error. */
						fpi = new PathInfo(PathInfo.TYPE.CAPACITOR, ce, ce.Nodes[1], Circuit.ElmList, Circuit.Nodes.Count);
						if (fpi.FindPath(ce.Nodes[0])) {
							Circuit.Stop("Capacitor loop with no resistance!");
							return;
						}
					}
				}
			}

			if (!simplifyMatrix(matrixSize)) {
				return;
			}

			//if () {
			//	Console.WriteLine("Matrix size:" + matrixSize);
			//	for (int j = 0; j != mMatrixSize; j++) {
			//		Console.WriteLine("RightSide[{0}]:{1}", j, RightSide[j]);
			//		for (int i = 0; i != mMatrixSize; i++) {
			//			Console.WriteLine("  Matrix[{0},{1}]:{2}", j, i, Matrix[j, i]);
			//		}
			//	}
			//}
		}
		#endregion

		#region private method
		static CircuitNode getCircuitNode(int n) {
			if (n >= Circuit.Nodes.Count) {
				return null;
			}
			return Circuit.Nodes[n];
		}

		/* simplify the matrix; this speeds things up quite a bit, especially for digital circuits */
		static bool simplifyMatrix(int matrixSize) {
			int matRow;
			int matCol;
			for (matRow = 0; matRow != matrixSize; matRow++) {
				int qp = -1;
				double qv = 0;
				var re = Circuit.RowInfo[matRow];
				/*Console.WriteLine("row " + i + " " + re.lsChanges + " " + re.rsChanges + " " + re.dropRow);*/
				if (re.LeftChanges || re.DropRow || re.RightChanges) {
					continue;
				}
				double rsadd = 0;

				/* look for rows that can be removed */
				for (matCol = 0; matCol != matrixSize; matCol++) {
					var q = Circuit.Matrix[matRow, matCol];
					if (Circuit.RowInfo[matCol].IsConst) {
						/* keep a running total of const values that have been
                        /* removed already */
						rsadd -= Circuit.RowInfo[matCol].Value * q;
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
					var elt = Circuit.RowInfo[qp];
					/* we found a row with only one nonzero nonconst entry; that value is a constant */
					if (elt.IsConst) {
						Console.WriteLine("type already CONST for " + qp + "!");
						continue;
					}
					elt.IsConst = true;
					elt.Value = (Circuit.RightSide[matRow] + rsadd) / qv;
					Circuit.RowInfo[matRow].DropRow = true;
					matRow = -1; /* start over from scratch */
				}
			}

			/* find size of new matrix */
			int nn = 0;
			for (matRow = 0; matRow != matrixSize; matRow++) {
				var elt = Circuit.RowInfo[matRow];
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
				var rri = Circuit.RowInfo[matRow];
				if (rri.DropRow) {
					rri.MapRow = -1;
					continue;
				}
				newRS[ii] = Circuit.RightSide[matRow];
				rri.MapRow = ii;
				for (matCol = 0; matCol != matrixSize; matCol++) {
					var ri = Circuit.RowInfo[matCol];
					if (ri.IsConst) {
						newRS[ii] -= ri.Value * Circuit.Matrix[matRow, matCol];
					} else {
						newMat[ii, ri.MapCol] += Circuit.Matrix[matRow, matCol];
					}
				}
				ii++;
			}
			/*Console.WriteLine("old size = " + matrixSize + " new size = " + newSize);*/

			Circuit.Matrix = newMat;
			Circuit.RightSide = newRS;
			matrixSize = Circuit.MatrixSize = newSize;
			for (matRow = 0; matRow != matrixSize; matRow++) {
				Circuit.OrigRightSide[matRow] = Circuit.RightSide[matRow];
			}
			for (matRow = 0; matRow != matrixSize; matRow++) {
				for (matCol = 0; matCol != matrixSize; matCol++) {
					Circuit.OrigMatrix[matRow, matCol] = Circuit.Matrix[matRow, matCol];
				}
			}
			Circuit.CircuitNeedsMap = true;
			return true;
		}

		/* find groups of nodes connected by wires and map them to the same node.  this speeds things
        /* up considerably by reducing the size of the matrix */
		static void calculateWireClosure() {
			int mergeCount = 0;
			mNodeMap = new Dictionary<Point, NodeMapEntry>();
			Circuit.WireInfoList = new List<Circuit.WireInfo>();
			for (int i = 0; i < Circuit.ElmList.Count; i++) {
				var ce = Circuit.ElmList[i];
				if (!(ce is ElmWire)) {
					continue;
				}
				var elm = (ElmWire)ce;
				elm.HasWireInfo = false;
				Circuit.WireInfoList.Add(new Circuit.WireInfo(elm));
				var p1 = elm.NodePos[0];
				var p2 = elm.NodePos[1];
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
			for (wireIdx = 0; wireIdx != Circuit.WireInfoList.Count; wireIdx++) {
				var wi = Circuit.WireInfoList[wireIdx];
				var wire = wi.Wire;
				var cn1 = Circuit.Nodes[wire.Nodes[0]];  /* both ends of wire have same node # */
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

					/* which post does this element connect to, if any? */
					var elmPos = ce.NodePos[cnl.Num];
					var wirePosA = wire.NodePos[0];
					var wirePosB = wire.NodePos[1];
					if (elmPos.X == wirePosA.X && elmPos.Y == wirePosA.Y) {
						neighbors0.Add(ce);
						if (notReady) {
							isReady0 = false;
						}
					} else if (elmPos.X == wirePosB.X && elmPos.Y == wirePosB.Y) {
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
					var tmp = Circuit.WireInfoList[wireIdx];
					Circuit.WireInfoList.RemoveAt(wireIdx--);
					Circuit.WireInfoList.Add(tmp);
					moved++;
					if (moved > Circuit.WireInfoList.Count * 2) {
						Circuit.Stop("wire loop detected");
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
			BadConnectionList = new List<Point>();
			foreach (var entry in mPostCountMap) {
				if (2 == entry.Value) {
				} else {
					DrawPostList.Add(entry.Key);
				}
				/* look for bad connections, posts not connected to other elements which intersect
                /* other elements' bounding boxes */
				if (entry.Value == 1) {
					bool bad = false;
					var cn = entry.Key;
					for (int j = 0; j != Circuit.ElmList.Count && !bad; j++) {
						var ce = Circuit.ElmList[j];
						/* does this post belong to the elm? */
						int k;
						int pc = ce.TermCount;
						for (k = 0; k != pc; k++) {
							if (ce.NodePos[k].Equals(cn)) {
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
			Circuit.StopMessage = s;
			Circuit.Matrix = null;  /* causes an exception */
			Circuit.SetSimRunning(false);
		}
		#endregion
	}

}
