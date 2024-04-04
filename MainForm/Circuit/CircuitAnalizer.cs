using Circuit.Elements.Input;
using Circuit.Elements.Passive;
using Circuit.Forms;
using Circuit.Symbol.Measure;

namespace Circuit {
	static class CircuitAnalizer {
		public class Link {
			public int Node;
			public BaseElement Elm;
		}

		public class Node {
			public bool IsInternal;
			public List<Link> Links = [];
		}

		public class Wire {
			public int Post;
			public ElmWire Instance;
			public List<BaseElement> ConnectedElms = [];
		}

		public class NodeInfo {
			public int Row;
			public int Col;
			public bool IsConst;
			public bool NonLinear;
			public bool RightChanges;
			public bool Drop;
			public double Value;
		}

		class NodeMapEntry {
			public const int Unallocated = -1;
			public const int GroundNode = 0;
			public int Id = Unallocated;
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
							if (cee.HasGroundConnection(j) && FindPath(cee.GetConnection(j))) {
								return true;
							}
						}
					}

					int nodeA;
					for (nodeA = 0; nodeA != cee.ConnectionNodeCount; nodeA++) {
						if (cee.GetConnection(nodeA) == n1) {
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
						if (cee.HasConnection(nodeA, nodeB) && FindPath(cee.GetConnection(nodeB))) {
							/*Console.WriteLine("got findpath " + n1); */
							return true;
						}
					}
				}
				return false;
			}
		}

		public static List<Point> DrawPostList { get; set; } = new List<Point>();
		public static List<Point> BadConnectionList { get; set; } = new List<Point>();

		public static int NodeCount { get; private set; }
		public static double[,] Matrix = new double[0, 0];
		public static double[] RightSide = [];
		public static NodeInfo[] NodeInfos = [];

		public static void Analyze(List<BaseSymbol> symbolList) {
			if (0 == symbolList.Count) {
				DrawPostList = new List<Point>();
				BadConnectionList = new List<Point>();
				CircuitElement.SetElements([]);
				CircuitElement.SetWires([]);
				CircuitElement.SetNodes([], []);
				CircuitElement.SetMatrix([], Matrix, 0);
				return;
			}

			CircuitState.Stopped = false;
			var elmList = new List<BaseElement>();
			foreach (var symbol in symbolList) {
				elmList.Add(symbol.Element);
			}
			ElmNamedNode.ResetNodeList();

			/* allocate nodes */
			var nodeList = new List<Node>();
			int vsCount = 0;
			{
				/* look for voltage or ground element */
				var gotGround = false;
				var gotRail = false;
				BaseElement volt = null;
				for (int i = 0; i != elmList.Count; i++) {
					var ce = elmList[i];
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

				var nodeMap = CalcWireClosure(elmList);

				/* if no ground, and no rails, then the voltage elm's first terminal is ground */
				if (!gotGround && volt != null && !gotRail) {
					nodeList.Add(new Node());
					var nodePos = volt.NodePos[0];
					/* update node map */
					if (nodeMap.ContainsKey(nodePos)) {
						nodeMap[nodePos].Id = NodeMapEntry.GroundNode;
					} else {
						nodeMap.Add(nodePos, new NodeMapEntry() { Id = NodeMapEntry.GroundNode });
					}
				} else {
					/* otherwise allocate extra node for ground */
					nodeList.Add(new Node());
				}

				var postCountMap = new Dictionary<Point, int>();
				for (int idxE = 0; idxE < elmList.Count; idxE++) {
					var elm = elmList[idxE];
					if (null == elm) {
						continue;
					}
					var inodes = elm.InternalNodeCount;
					var ivs = elm.VoltageSourceCount;
					var posts = elm.TermCount;

					/* allocate a node for each post and match posts to nodes */
					for (int idxN = 0; idxN < posts; idxN++) {
						var nodePos = elm.NodePos[idxN];
						if (postCountMap.ContainsKey(nodePos)) {
							var g = postCountMap[nodePos];
							postCountMap[nodePos] = g + 1;
						} else {
							postCountMap.Add(nodePos, 1);
						}

						NodeMapEntry entryNode = null;
						var isKnownNode = nodeMap.ContainsKey(nodePos);
						if (isKnownNode) {
							entryNode = nodeMap[nodePos];
						}

						/* is this node not in map yet?  or is the node number unallocated?
                        /* (we don't allocate nodes before this because changing the allocation order
                        /* of nodes changes circuit behavior and breaks backward compatibility;
                        /* the code below to connect unconnected nodes may connect a different node to ground) */
						if (!isKnownNode || entryNode.Id == NodeMapEntry.Unallocated) {
							var cn = new Node();
							cn.Links.Add(new Link {
								Node = idxN,
								Elm = elm
							});
							elm.SetNode(idxN, nodeList.Count);
							if (isKnownNode) {
								entryNode.Id = nodeList.Count;
							} else {
								nodeMap.Add(nodePos, new NodeMapEntry() { Id = nodeList.Count });
							}
							nodeList.Add(cn);
						} else {
							nodeList[entryNode.Id].Links.Add(new Link {
								Node = idxN,
								Elm = elm
							});
							elm.SetNode(idxN, entryNode.Id);
							/* if it's the ground node, make sure the node voltage is 0,
                            /* cause it may not get set later */
							if (entryNode.Id == NodeMapEntry.GroundNode) {
								elm.SetVoltage(idxN, 0);
							}
						}
					}
					for (int j = 0; j < inodes; j++) {
						var cl = new Link {
							Node = j + posts,
							Elm = elm
						};
						var cn = new Node {
							IsInternal = true
						};
						cn.Links.Add(cl);
						elm.SetNode(cl.Node, nodeList.Count);
						nodeList.Add(cn);
					}
					vsCount += ivs;
				}

				MakeDrawingPostList(elmList, postCountMap);
			}

			var wireList = CalcWireInfo(elmList, nodeList);
			if (null == wireList) {
				return;
			}
			CircuitElement.SetElements(elmList);
			CircuitElement.SetWires(wireList);

			NodeCount = nodeList.Count;
			var matrixSize = NodeCount - 1 + vsCount;
			Matrix = new double[matrixSize, matrixSize];
			RightSide = new double[matrixSize];
			NodeInfos = new NodeInfo[matrixSize];
			for (int i = 0; i < matrixSize; i++) {
				NodeInfos[i] = new NodeInfo();
			}

			/* stamp linear circuit elements */
			for (int i = 0; i < elmList.Count; i++) {
				elmList[i].Stamp();
			}

			/* determine nodes that are not connected indirectly to ground */
			var closure = new bool[NodeCount];
			var changed = true;
			closure[0] = true;
			while (changed) {
				changed = false;
				for (int idxE = 0; idxE < elmList.Count; idxE++) {
					var ce = elmList[idxE];
					if (ce is ElmWire) {
						continue;
					}
					/* loop through all ce's nodes to see if they are connected
                    /* to other nodes not in closure */
					for (int i = 0; i < ce.ConnectionNodeCount; i++) {
						if (!closure[ce.GetConnection(i)]) {
							if (ce.HasGroundConnection(i)) {
								closure[ce.GetConnection(i)] = changed = true;
							}
							continue;
						}
						for (int j = 0; j != ce.ConnectionNodeCount; j++) {
							if (i == j) {
								continue;
							}
							int kn = ce.GetConnection(j);
							if (ce.HasConnection(i, j) && !closure[kn]) {
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
				for (int i = 0; i != NodeCount; i++) {
					if (!closure[i] && !nodeList[i].IsInternal) {
						Matrix[i, i] += 1e-8;
						closure[i] = true;
						changed = true;
						break;
					}
				}
			}

			for (int idxE = 0; idxE < elmList.Count; idxE++) {
				var ce = elmList[idxE];

				/* look for inductors with no current path */
				if (ce is ElmInductor) {
					var fpi = new PathInfo(PathInfo.TYPE.INDUCTOR, ce, ce.NodeId[1], elmList, NodeCount);
					if (!fpi.FindPath(ce.NodeId[0])) {
						ce.Reset();
					}
				}

				/* look for current sources with no current path */
				if (ce is ElmCurrent) {
					var cur = (ElmCurrent)ce;
					var fpi = new PathInfo(PathInfo.TYPE.INDUCTOR, ce, ce.NodeId[1], elmList, NodeCount);
					if (!fpi.FindPath(ce.NodeId[0])) {
						cur.StampCurrentSource(true);
					} else {
						cur.StampCurrentSource(false);
					}
				}

				/* look for voltage source or wire loops.  we do this for voltage sources or wire-like elements (not actual wires
                /* because those are optimized out, so the findPath won't work) */
				if (2 == ce.TermCount) {
					if (ce is ElmVoltage) {
						var fpi = new PathInfo(PathInfo.TYPE.VOLTAGE, ce, ce.NodeId[1], elmList, NodeCount);
						if (fpi.FindPath(ce.NodeId[0])) {
							Stop("Voltage source/wire loop with no resistance!");
							return;
						}
					}
				} else {
					/* look for path from rail to ground */
					if (ce is ElmRail || ce is ElmLogicInput) {
						var fpi = new PathInfo(PathInfo.TYPE.VOLTAGE, ce, ce.NodeId[0], elmList, NodeCount);
						if (fpi.FindPath(0)) {
							Stop("Voltage source/wire loop with no resistance!");
							return;
						}
					}
				}

				/* look for shorted caps, or caps w/ voltage but no R */
				if (ce is ElmCapacitor) {
					var fpi = new PathInfo(PathInfo.TYPE.SHORT, ce, ce.NodeId[1], elmList, NodeCount);
					if (fpi.FindPath(ce.NodeId[0])) {
						Console.WriteLine(ce + " shorted");
						ce.Shorted();
					} else {
						/* a capacitor loop used to cause a matrix error. but we changed the capacitor model
                        /* so it works fine now. The only issue is if a capacitor is added in parallel with
                        /* another capacitor with a nonzero voltage; in that case we will get oscillation unless
                        /* we reset both capacitors to have the same voltage. Rather than check for that, we just
                        /* give an error. */
						fpi = new PathInfo(PathInfo.TYPE.CAPACITOR, ce, ce.NodeId[1], elmList, NodeCount);
						if (fpi.FindPath(ce.NodeId[0])) {
							Stop("Capacitor loop with no resistance!");
							return;
						}
					}
				}
			}

			if (!SimplifyMatrix(matrixSize)) {
				return;
			}
			CircuitElement.SetNodes(nodeList, NodeInfos);

			var waveCount = 0;
			for(var i = 0; i < ScopeForm.PlotCount; i++) {
				waveCount += ScopeForm.Plots[i].WaveCount;
			}
			foreach(var item in symbolList) {
				if (item is Scope scope) {
					waveCount += scope.Plot.WaveCount;
				}
			}
			var waves = new SCOPE_WAVE[waveCount];
			waveCount = 0;
			for(var i = 0; i < ScopeForm.PlotCount; i++) {
				var plot = ScopeForm.Plots[i];
				for(var j = 0; j < plot.WaveCount; j++) {
					waves[waveCount++] = plot.Waves[j];
				}
			}
			foreach(var item in symbolList) {
				if (item is Scope scope) {
					var plot = scope.Plot;
					for (var i = 0; i < plot.WaveCount; i++) {
						waves[waveCount++] = plot.Waves[i];
					}
				}
			}
			CircuitElement.SetWaves(waves);
		}

		#region private method
		static bool SimplifyMatrix(int matrixSize) {
			for (int idxRow = 0; idxRow != matrixSize; idxRow++) {
				var nodeR = NodeInfos[idxRow];
				//Console.WriteLine("Row:" + idxRow + " NonLinear:" + nodeR.non_linear + " R:" + nodeR.right_changes + " drop:" + nodeR.drop);
				if (nodeR.Drop || nodeR.NonLinear || nodeR.RightChanges) {
					continue;
				}
				var constIdx = -1;
				var constVal = 0.0;
				var constSum = 0.0;
				int idxCol;
				for (idxCol = 0; idxCol != matrixSize; idxCol++) {
					var matVal = Matrix[idxRow, idxCol];
					var nodeC = NodeInfos[idxCol];
					if (nodeC.IsConst) {
						/* keep a running total of const values that have been removed already */
						constSum -= nodeC.Value * matVal;
						continue;
					}
					if (matVal == 0) {
						/* ignore zeroes */
						continue;
					}
					if (constIdx == -1) {
						/* keep track of first nonzero element that is not ROW_CONST */
						constIdx = idxCol;
						constVal = matVal;
						continue;
					}
					/* more than one nonzero element? give up */
					break;
				}
				if (idxCol == matrixSize) {
					if (constIdx == -1) {
						Console.WriteLine("probably a singular matrix, try disabling matrix simplification above to check this");
						return false;
					}
					var constNode = NodeInfos[constIdx];
					if (constNode.IsConst) {
						/* we found a row with only one nonzero nonconst entry; that value is a constant */
						Console.WriteLine("type already CONST for [" + constIdx + "]!");
						continue;
					}
					constNode.IsConst = true;
					constNode.Value = (RightSide[idxRow] + constSum) / constVal;
					nodeR.Drop = true;
					idxRow = -1; /* start over from scratch */
				}
			}

			/* find size of new matrix */
			int rowCount = 0;
			int newSize = 0;
			for (int i = 0; i != matrixSize; i++) {
				var node = NodeInfos[i];
				node.Row = node.Drop ? -1 : rowCount++;
				node.Col = node.IsConst ? -1 : newSize++;
			}
			//Console.WriteLine("old size:" + matrixSize + " new size:" + newSize);

			/* make the new, simplified matrix */
			var newMatrix = new double[newSize, newSize];
			var newRightSide = new double[newSize];
			rowCount = 0;
			for (int idxRow = 0; idxRow != matrixSize; idxRow++) {
				var nodeR = NodeInfos[idxRow];
				if (nodeR.Drop) {
					continue;
				}
				newRightSide[rowCount] = RightSide[idxRow];
				for (int idxCol = 0; idxCol != matrixSize; idxCol++) {
					var nodeC = NodeInfos[idxCol];
					var m = Matrix[idxRow, idxCol];
					if (nodeC.IsConst) {
						newRightSide[rowCount] -= nodeC.Value * m;
					} else {
						newMatrix[rowCount, nodeC.Col] += m;
					}
				}
				rowCount++;
			}
			if (false) {
				Console.WriteLine("Matrix size:" + newSize);
				for (int j = 0; j != newSize; j++) {
					Console.WriteLine("RightSide[{0}]:{1}", j, newRightSide[j]);
					for (int i = 0; i != newSize; i++) {
						Console.WriteLine(" Matrix[{0},{1}]:{2}", j, i, newMatrix[j, i]);
					}
				}
			}
			CircuitElement.SetMatrix(newRightSide, newMatrix, newSize);
			return true;
		}

		/* find groups of nodes connected by wires and map them to the same node.  this speeds things
        /* up considerably by reducing the size of the matrix */
		static Dictionary<Point, NodeMapEntry> CalcWireClosure(List<BaseElement> elements) {
			int mergeCount = 0;
			var nodeMap = new Dictionary<Point, NodeMapEntry>();
			for (int i = 0; i < elements.Count; i++) {
				var ce = elements[i];
				if (ce is not ElmWire) {
					continue;
				}
				var elm = (ElmWire)ce;
				elm.HasWireInfo = false;
				var p1 = elm.NodePos[0];
				var p2 = elm.NodePos[1];
				var cp1 = nodeMap.ContainsKey(p1);
				var cp2 = nodeMap.ContainsKey(p2);
				if (cp1 && cp2) {
					var cn1 = nodeMap[p1];
					var cn2 = nodeMap[p2];
					/* merge nodes; go through map and change all keys pointing to cn2 to point to cn */
					var tmp = new Dictionary<Point, NodeMapEntry>();
					foreach (var entry in nodeMap) {
						if (entry.Value.Equals(cn2)) {
							tmp.Add(entry.Key, cn1);
						}
					}
					foreach (var entry in tmp) {
						nodeMap[entry.Key] = entry.Value;
					}
					tmp.Clear();
					mergeCount++;
					continue;
				}
				if (cp1) {
					var cn1 = nodeMap[p1];
					nodeMap.Add(p2, cn1);
					continue;
				}
				if (cp2) {
					var cn2 = nodeMap[p2];
					nodeMap.Add(p1, cn2);
					continue;
				}
				/* new entry */
				var cn = new NodeMapEntry();
				nodeMap.Add(p1, cn);
				nodeMap.Add(p2, cn);
			}
			/*Console.WriteLine("groups with " + mNodeMap.Count + " nodes " + mergeCount);*/
			return nodeMap;
		}

		/* generate info we need to calculate wire currents.  Most other elements calculate currents using
		/* the voltage on their terminal nodes.  But wires have the same voltage at both ends, so we need
		/* to use the neighbors' currents instead.  We used to treat wires as zero voltage sources to make
		/* this easier, but this is very inefficient, since it makes the matrix 2 rows bigger for each wire.
		/* So we create a list of WireInfo objects instead to help us calculate the wire currents instead,
		/* so we make the matrix less complex, and we only calculate the wire currents when we need them
		/* (once per frame, not once per subiteration) */
		static List<Wire> CalcWireInfo(List<BaseElement> elmList, List<Node> nodeList) {
			var wireList = new List<Wire>();
			for (int i = 0; i < elmList.Count; i++) {
				var elm = elmList[i];
				if (elm is ElmWire wire) {
					wireList.Add(new Wire() {
						Instance = wire
					});
				}
			}
			int wireIdx;
			int moved = 0;
			for (wireIdx = 0; wireIdx != wireList.Count; wireIdx++) {
				var wi = wireList[wireIdx];
				var wire = wi.Instance;
				var cn1 = nodeList[wire.NodeId[0]];  /* both ends of wire have same node # */
				int j;

				var connectedElmsA = new List<BaseElement>();
				var connectedElmsB = new List<BaseElement>();
				bool isReady0 = true;
				bool isReady1 = true;

				/* go through elements sharing a node with this wire (may be connected indirectly
                /* by other wires, but at least it's faster than going through all elements) */
				for (j = 0; j != cn1.Links.Count; j++) {
					var cl = cn1.Links[j];
					var elm = cl.Elm;
					if (elm == wire) {
						continue;
					}

					/* is this a wire that doesn't have wire info yet?  If so we can't use it.
                    /* That would create a circular dependency */
					bool isReady = (elm is ElmWire ew) && !ew.HasWireInfo;

					/* which post does this element connect to, if any? */
					var elmPos = elm.NodePos[cl.Node];
					var wirePosA = wire.NodePos[0];
					var wirePosB = wire.NodePos[1];
					if (elmPos.X == wirePosA.X && elmPos.Y == wirePosA.Y) {
						connectedElmsA.Add(elm);
						if (isReady) {
							isReady0 = false;
						}
					} else if (elmPos.X == wirePosB.X && elmPos.Y == wirePosB.Y) {
						connectedElmsB.Add(elm);
						if (isReady) {
							isReady1 = false;
						}
					}
				}

				/* does one of the posts have all information necessary to calculate current */
				if (isReady0) {
					wi.Post = 0;
					wi.ConnectedElms = connectedElmsA;
					wire.HasWireInfo = true;
					moved = 0;
				} else if (isReady1) {
					wi.Post = 1;
					wi.ConnectedElms = connectedElmsB;
					wire.HasWireInfo = true;
					moved = 0;
				} else {
					/* move to the end of the list and try again later */
					var tmp = wireList[wireIdx];
					wireList.RemoveAt(wireIdx--);
					wireList.Add(tmp);
					moved++;
					if (moved > wireList.Count * 2) {
						Stop("wire loop detected");
						return null;
					}
				}
			}
			return wireList;
		}

		/* make list of posts we need to draw.  posts shared by 2 elements should be hidden, all
        /* others should be drawn.  We can't use the node list anymore because wires have the same
        /* node number at both ends. */
		static void MakeDrawingPostList(List<BaseElement> elements, Dictionary<Point, int> postCountMap) {
			DrawPostList = new List<Point>();
			BadConnectionList = new List<Point>();
			foreach (var entry in postCountMap) {
				if (2 == entry.Value) {
				} else {
					DrawPostList.Add(entry.Key);
				}
				/* look for bad connections, posts not connected to other elements which intersect
                /* other elements' bounding boxes */
				if (entry.Value == 1) {
					bool bad = false;
					var cn = entry.Key;
					for (int j = 0; j != elements.Count && !bad; j++) {
						var ce = elements[j];
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
		}

		static void Stop(string s) {
			MainForm.MainForm.SetSimRunning(false);
		}
		#endregion
	}
}
