using System.Reflection.Metadata;
using Circuit.Elements.Input;
using Circuit.Elements.Passive;
using Circuit.Forms;
using Circuit.Symbol.Measure;

namespace Circuit {
	static class CircuitSymbol {
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
							if (cee.has_ground_connection(j) && FindPath(cee.get_connection(j))) {
								return true;
							}
						}
					}

					int nodeA;
					for (nodeA = 0; nodeA != cee.ConnectionNodeCount; nodeA++) {
						if (cee.get_connection(nodeA) == n1) {
							break;
						}
					}
					if (nodeA == cee.ConnectionNodeCount) {
						continue;
					}
					if (cee.has_ground_connection(nodeA) && FindPath(0)) {
						return true;
					}

					if (mType == TYPE.INDUCTOR && (cee is ElmInductor)) {
						/* inductors can use paths with other inductors of matching current */
						var c = cee.current;
						if (nodeA == 0) {
							c = -c;
						}
						if (Math.Abs(c - mFirstElm.current) > 1e-10) {
							continue;
						}
					}

					for (int nodeB = 0; nodeB != cee.ConnectionNodeCount; nodeB++) {
						if (nodeA == nodeB) {
							continue;
						}
						if (cee.has_connection(nodeA, nodeB) && FindPath(cee.get_connection(nodeB))) {
							/*Console.WriteLine("got findpath " + n1); */
							return true;
						}
					}
				}
				return false;
			}
		}

		public static bool IsRunning;
		public static bool NeedAnalyze;
		public static int Count { get { return null == List ? 0 : List.Count; } }
		public static List<BaseSymbol> List { get; private set; } = new List<BaseSymbol>();
		public static List<Point> DrawPostList { get; set; } = new List<Point>();
		public static List<Point> BadConnectionList { get; set; } = new List<Point>();

		static Dictionary<Point, int> mPostCountMap = [];
		static Dictionary<Point, NodeMapEntry> mNodeMap = [];

		#region public method
		public static void Reset() {
			for (int i = 0; i != List.Count; i++) {
				List[i].Element.reset();
			}
			CircuitElement.Time = 0;
			NeedAnalyze = true;
		}
		public static void SetSimRunning(bool s) {
			if (s) {
				IsRunning = true;
				CircuitElement.Stopped = false;
				ControlPanel.BtnRunStop.Text = "停止";
			} else {
				NeedAnalyze = false;
				IsRunning = false;
				ControlPanel.BtnRunStop.Text = "実行";
			}
		}
		public static void AnalyzeCircuit() {
			CircuitElement.setElements([]);
			var elements = new List<BaseElement>();
			foreach (var symbol in List) {
				elements.Add(symbol.Element);
			}

			if (0 == elements.Count) {
				DrawPostList = new List<Point>();
				BadConnectionList = new List<Point>();
				return;
			}
			CircuitElement.Stopped = false;
			CircuitElement.setElements(elements);

			var nodes = new List<CIRCUIT_NODE>();
			var wires = new List<CIRCUIT_WIRE>();
			CircuitElement.setNodes(nodes);
			CircuitElement.setWires(wires);

			mPostCountMap = new Dictionary<Point, int>();

			calculateWireClosure(wires, elements);

			{
				/* look for voltage or ground element */
				var gotGround = false;
				var gotRail = false;
				BaseElement volt = null;
				for (int i = 0; i != elements.Count; i++) {
					var ce = elements[i];
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
					var cn = new CIRCUIT_NODE();
					var np = volt.node_pos[0];
					nodes.Add(cn);
					/* update node map */
					if (mNodeMap.ContainsKey(np)) {
						mNodeMap[np].Id = NodeMapEntry.GroundNode;
					} else {
						mNodeMap.Add(np, new NodeMapEntry() { Id = NodeMapEntry.GroundNode });
					}
				} else {
					/* otherwise allocate extra node for ground */
					nodes.Add(new CIRCUIT_NODE());
				}
			}

			/* allocate nodes */
			int vsCount = 0;
			{
				ElmNamedNode.ResetNodeList();
				for (int i = 0; i < elements.Count; i++) {
					var elm = elements[i];
					if (null == elm) {
						continue;
					}
					var inodes = elm.InternalNodeCount;
					var ivs = elm.VoltageSourceCount;
					var posts = elm.TermCount;

					/* allocate a node for each post and match posts to nodes */
					for (int idxN = 0; idxN < posts; idxN++) {
						var np = elm.node_pos[idxN];
						if (mPostCountMap.ContainsKey(np)) {
							var g = mPostCountMap[np];
							mPostCountMap[np] = g + 1;
						} else {
							mPostCountMap.Add(np, 1);
						}

						NodeMapEntry entryNode = null;
						var isKnownNode = mNodeMap.ContainsKey(np);
						if (isKnownNode) {
							entryNode = mNodeMap[np];
						}

						/* is this node not in map yet?  or is the node number unallocated?
                        /* (we don't allocate nodes before this because changing the allocation order
                        /* of nodes changes circuit behavior and breaks backward compatibility;
                        /* the code below to connect unconnected nodes may connect a different node to ground) */
						if (!isKnownNode || entryNode.Id == NodeMapEntry.Unallocated) {
							var cn = new CIRCUIT_NODE();
							var cl = new CIRCUIT_LINK {
								node_index = idxN,
								elm = elm
							};
							cn.links.Add(cl);
							elm.set_node(idxN, nodes.Count);
							if (isKnownNode) {
								entryNode.Id = nodes.Count;
							} else {
								mNodeMap.Add(np, new NodeMapEntry() { Id = nodes.Count });
							}
							nodes.Add(cn);
						} else {
							var cl = new CIRCUIT_LINK {
								node_index = idxN,
								elm = elm
							};
							nodes[entryNode.Id].links.Add(cl);
							elm.set_node(idxN, entryNode.Id);
							/* if it's the ground node, make sure the node voltage is 0,
                            /* cause it may not get set later */
							if (entryNode.Id == NodeMapEntry.GroundNode) {
								elm.set_voltage(idxN, 0);
							}
						}
					}
					for (int j = 0; j < inodes; j++) {
						var cl = new CIRCUIT_LINK {
							node_index = j + posts,
							elm = elm
						};
						var cn = new CIRCUIT_NODE {
							is_internal = true
						};
						cn.links.Add(cl);
						elm.set_node(cl.node_index, nodes.Count);
						nodes.Add(cn);
					}
					vsCount += ivs;
				}

				makePostDrawList(elements);
				if (calcWireInfo(nodes, wires)) {
					mNodeMap = null; /* done with this */
				} else {
					return;
				}
			}

			CircuitElement.setVoltageSource(elements);

			var matrixSize = nodes.Count - 1 + vsCount;
			CircuitElement.clearMatrix(matrixSize);

			/* stamp linear circuit elements */
			for (int i = 0; i < elements.Count; i++) {
				elements[i].stamp();
			}

			/* determine nodes that are not connected indirectly to ground */
			var closure = new bool[nodes.Count];
			var changed = true;
			closure[0] = true;
			while (changed) {
				changed = false;
				for (int i = 0; i < elements.Count; i++) {
					var ce = elements[i];
					if (ce is ElmWire) {
						continue;
					}
					/* loop through all ce's nodes to see if they are connected
                    /* to other nodes not in closure */
					for (int j = 0; j < ce.ConnectionNodeCount; j++) {
						if (!closure[ce.get_connection(j)]) {
							if (ce.has_ground_connection(j)) {
								closure[ce.get_connection(j)] = changed = true;
							}
							continue;
						}
						for (int k = 0; k != ce.ConnectionNodeCount; k++) {
							if (j == k) {
								continue;
							}
							int kn = ce.get_connection(k);
							if (ce.has_connection(j, k) && !closure[kn]) {
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
				for (int i = 0; i != nodes.Count; i++) {
					if (!closure[i] && !nodes[i].is_internal) {
						CircuitElement.StampResistor(0, i, 1e8);
						closure[i] = true;
						changed = true;
						break;
					}
				}
			}

			for (int i = 0; i < elements.Count; i++) {
				var ce = elements[i];

				/* look for inductors with no current path */
				if (ce is ElmInductor) {
					var fpi = new PathInfo(PathInfo.TYPE.INDUCTOR, ce, ce.node_index[1], elements, nodes.Count);
					if (!fpi.FindPath(ce.node_index[0])) {
						ce.reset();
					}
				}

				/* look for current sources with no current path */
				if (ce is ElmCurrent) {
					var cur = (ElmCurrent)ce;
					var fpi = new PathInfo(PathInfo.TYPE.INDUCTOR, ce, ce.node_index[1], elements, nodes.Count);
					if (!fpi.FindPath(ce.node_index[0])) {
						cur.StampCurrentSource(true);
					} else {
						cur.StampCurrentSource(false);
					}
				}

				/* look for voltage source or wire loops.  we do this for voltage sources or wire-like elements (not actual wires
                /* because those are optimized out, so the findPath won't work) */
				if (2 == ce.TermCount) {
					if (ce is ElmVoltage) {
						var fpi = new PathInfo(PathInfo.TYPE.VOLTAGE, ce, ce.node_index[1], elements, nodes.Count);
						if (fpi.FindPath(ce.node_index[0])) {
							stop("Voltage source/wire loop with no resistance!");
							return;
						}
					}
				} else {
					/* look for path from rail to ground */
					if (ce is ElmRail || ce is ElmLogicInput) {
						var fpi = new PathInfo(PathInfo.TYPE.VOLTAGE, ce, ce.node_index[0], elements, nodes.Count);
						if (fpi.FindPath(0)) {
							stop("Voltage source/wire loop with no resistance!");
							return;
						}
					}
				}

				/* look for shorted caps, or caps w/ voltage but no R */
				if (ce is ElmCapacitor) {
					var fpi = new PathInfo(PathInfo.TYPE.SHORT, ce, ce.node_index[1], elements, nodes.Count);
					if (fpi.FindPath(ce.node_index[0])) {
						Console.WriteLine(ce + " shorted");
						ce.shorted();
					} else {
						/* a capacitor loop used to cause a matrix error. but we changed the capacitor model
                        /* so it works fine now. The only issue is if a capacitor is added in parallel with
                        /* another capacitor with a nonzero voltage; in that case we will get oscillation unless
                        /* we reset both capacitors to have the same voltage. Rather than check for that, we just
                        /* give an error. */
						fpi = new PathInfo(PathInfo.TYPE.CAPACITOR, ce, ce.node_index[1], elements, nodes.Count);
						if (fpi.FindPath(ce.node_index[0])) {
							stop("Capacitor loop with no resistance!");
							return;
						}
					}
				}
			}

			var waveCount = 0;
			for(var i = 0; i < ScopeForm.PlotCount; i++) {
				waveCount += ScopeForm.Plots[i].WaveCount;
			}
			foreach(var item in List) {
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
			foreach(var item in List) {
				if (item is Scope scope) {
					var plot = scope.Plot;
					for (var i = 0; i < plot.WaveCount; i++) {
						waves[waveCount++] = plot.Waves[i];
					}
				}
			}
			CircuitElement.setWaves(waves);

			if (!CircuitElement.simplifyMatrix(matrixSize)) {
				return;
			}
		}
		#endregion

		#region private method
		/* find groups of nodes connected by wires and map them to the same node.  this speeds things
        /* up considerably by reducing the size of the matrix */
		static void calculateWireClosure(List<CIRCUIT_WIRE> wires, List<BaseElement> elements) {
			int mergeCount = 0;
			mNodeMap = new Dictionary<Point, NodeMapEntry>();
			for (int i = 0; i < elements.Count; i++) {
				var ce = elements[i];
				if (ce is not ElmWire) {
					continue;
				}
				var elm = (ElmWire)ce;
				elm.HasWireInfo = false;
				wires.Add(new CIRCUIT_WIRE() {
					elm = elm
				});
				var p1 = elm.node_pos[0];
				var p2 = elm.node_pos[1];
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
		static bool calcWireInfo(List<CIRCUIT_NODE> nodes, List<CIRCUIT_WIRE> wires) {
			int wireIdx;
			int moved = 0;
			for (wireIdx = 0; wireIdx != wires.Count; wireIdx++) {
				var wi = wires[wireIdx];
				var wire = wi.elm;
				var cn1 = nodes[wire.node_index[0]];  /* both ends of wire have same node # */
				int j;

				var connectedElmsA = new List<BaseElement>();
				var connectedElmsB = new List<BaseElement>();
				bool isReady0 = true;
				bool isReady1 = true;

				/* go through elements sharing a node with this wire (may be connected indirectly
                /* by other wires, but at least it's faster than going through all elements) */
				for (j = 0; j != cn1.links.Count; j++) {
					var cl = cn1.links[j];
					var elm = cl.elm;
					if (elm == wire) {
						continue;
					}

					/* is this a wire that doesn't have wire info yet?  If so we can't use it.
                    /* That would create a circular dependency */
					bool isReady = (elm is ElmWire ew) && !ew.HasWireInfo;

					/* which post does this element connect to, if any? */
					var elmPos = elm.node_pos[cl.node_index];
					var wirePosA = wire.node_pos[0];
					var wirePosB = wire.node_pos[1];
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
					wi.connected_elms = connectedElmsA;
					wi.node_index = 0;
					wire.HasWireInfo = true;
					moved = 0;
				} else if (isReady1) {
					wi.connected_elms = connectedElmsB;
					wi.node_index = 1;
					wire.HasWireInfo = true;
					moved = 0;
				} else {
					/* move to the end of the list and try again later */
					var tmp = wires[wireIdx];
					wires.RemoveAt(wireIdx--);
					wires.Add(tmp);
					moved++;
					if (moved > wires.Count * 2) {
						stop("wire loop detected");
						return false;
					}
				}
			}

			return true;
		}

		/* make list of posts we need to draw.  posts shared by 2 elements should be hidden, all
        /* others should be drawn.  We can't use the node list anymore because wires have the same
        /* node number at both ends. */
		static void makePostDrawList(List<BaseElement> elements) {
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
					for (int j = 0; j != elements.Count && !bad; j++) {
						var ce = elements[j];
						/* does this post belong to the elm? */
						int k;
						int pc = ce.TermCount;
						for (k = 0; k != pc; k++) {
							if (ce.node_pos[k].Equals(cn)) {
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
			SetSimRunning(false);
		}
		#endregion
	}

}
