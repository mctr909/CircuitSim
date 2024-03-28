using Circuit.Elements.Input;

namespace Circuit.Elements.Custom {
	class VoltageSourceRecord {
		public int vsNumForElement;
		public int vsNode;
		public BaseElement elm;
	}

	abstract class ElmComposite : BaseElement {
		/* list of elements contained in this subcircuit */
		protected List<BaseElement> CompList = [];

		/* list of nodes, mapping each one to a list of elements that reference that node */
		List<CIRCUIT_NODE> mCompNodeList;
		List<VoltageSourceRecord> mVoltageSources;
		int mNumTerms = 0;
		int mNumNodes = 0;

		public override int TermCount { get { return mNumTerms; } }

		public override int VoltageSourceCount { get { return mVoltageSources.Count; } }

		public override int InternalNodeCount { get { return mNumNodes - mNumTerms; } }

		public override void reset() {
			for (int i = 0; i < CompList.Count; i++) {
				CompList[i].reset();
			}
		}

		/* are n1 and n2 connected internally somehow? */
		public override bool has_connection(int n1, int n2) {
			var cl1 = mCompNodeList[n1].links;
			var cl2 = mCompNodeList[n2].links;
			/* see if any elements are connected to both n1 and n2, then call getConnection() on those */
			for (int i = 0; i < cl1.Count; i++) {
				var link1 = cl1[i];
				for (int j = 0; j < cl2.Count; j++) {
					var link2 = cl2[j];
					if (link1.p_elm == link2.p_elm && link1.p_elm.has_connection(link1.node_index, link2.node_index)) {
						return true;
					}
				}
			}
			return false;
		}

		/* is n1 connected to ground somehow? */
		public override bool has_ground_connection(int n1) {
			var links = mCompNodeList[n1].links;
			for (int i = 0; i < links.Count; i++) {
				if (links[i].p_elm.has_ground_connection(links[i].node_index)) {
					return true;
				}
			}
			return false;
		}

		public override void set_node(int p, int n) {
			base.set_node(p, n);
			var links = mCompNodeList[p].links;
			for (int i = 0; i < links.Count; i++) {
				links[i].p_elm.set_node(links[i].node_index, n);
			}
		}

		/* Find the component with the nth voltage
         * and set the
         * appropriate source in that component */
		public override void set_voltage_source(int n, int v) {
			var vsr = mVoltageSources[n];
			vsr.elm.set_voltage_source(vsr.vsNumForElement, v);
			vsr.vsNode = v;
		}

		public override void stamp() {
			for (int i = 0; i < CompList.Count; i++) {
				var ce = CompList[i];
				/* current sources need special stamp method */
				if (ce is ElmCurrent elm) {
					elm.StampCurrentSource(false);
				} else {
					ce.stamp();
				}
			}
		}

		#region [method(Circuit)]
		public override void prepare_iteration() {
			for (int i = 0; i < CompList.Count; i++) {
				CompList[i].prepare_iteration();
			}
		}

		public override void do_iteration() {
			for (int i = 0; i < CompList.Count; i++) {
				CompList[i].do_iteration();
			}
		}

		public override void finish_iteration() {
			for (int i = 0; i < CompList.Count; i++) {
				CompList[i].finish_iteration();
			}
		}

		public override double get_current_into_node(int n) {
			double c = 0;
			var links = mCompNodeList[n].links;
			for (int i = 0; i < links.Count; i++) {
				c += links[i].p_elm.get_current_into_node(links[i].node_index);
			}
			return c;
		}

		public override void set_voltage(int n, double c) {
			base.set_voltage(n, c);
			var links = mCompNodeList[n].links;
			for (int i = 0; i < links.Count; i++) {
				links[i].p_elm.set_voltage(links[i].node_index, c);
			}
			volts[n] = c;
		}

		public override void set_current(int vsn, double c) {
			for (int i = 0; i < mVoltageSources.Count; i++) {
				if (mVoltageSources[i].vsNode == vsn) {
					mVoltageSources[i].elm.set_current(vsn, c);
				}
			}
		}
		#endregion

		public void LoadComposite(Dictionary<int, CIRCUIT_NODE> nodeHash, List<BaseSymbol> symbolList, int[] externalNodes) {
			/* Flatten nodeHash in to compNodeList */
			mCompNodeList = new List<CIRCUIT_NODE>();
			mNumTerms = externalNodes.Length;
			for (int i = 0; i < externalNodes.Length; i++) {
				/* External Nodes First */
				if (nodeHash.ContainsKey(externalNodes[i])) {
					mCompNodeList.Add(nodeHash[externalNodes[i]]);
					nodeHash.Remove(externalNodes[i]);
				} else {
					throw new Exception();
				}
			}
			foreach (var entry in nodeHash) {
				int key = entry.Key;
				mCompNodeList.Add(nodeHash[key]);
			}
			/* allocate more nodes for sub-elements' internal nodes */
			for (int i = 0; i != symbolList.Count; i++) {
				var ce = symbolList[i].Element;
				CompList.Add(ce);
				int inodes = ce.InternalNodeCount;
				for (int j = 0; j != inodes; j++) {
					var cl = new CIRCUIT_LINK() {
						node_index = j + ce.TermCount,
						p_elm = ce
					};
					var cn = new CIRCUIT_NODE();
					cn.links.Add(cl);
					mCompNodeList.Add(cn);
				}
			}
			mNumNodes = mCompNodeList.Count;

			/*Console.WriteLine("Dumping compNodeList");
            for (int i = 0; i < numNodes; i++) {
                Console.WriteLine("New node" + i + " Size of links:" + compNodeList.get(i).links.size());
            }*/

			/* Enumerate voltage sources */
			mVoltageSources = new List<VoltageSourceRecord>();
			for (int i = 0; i < CompList.Count; i++) {
				int cnt = CompList[i].VoltageSourceCount;
				for (int j = 0; j < cnt; j++) {
					var vsRecord = new VoltageSourceRecord() {
						elm = CompList[i],
						vsNumForElement = j
					};
					mVoltageSources.Add(vsRecord);
				}
			}

			alloc_nodes();
			Init();
		}

		protected virtual void Init() { }
	}
}
