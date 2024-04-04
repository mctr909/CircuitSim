using Circuit;
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
		List<CircuitAnalizer.Node> mCompNodeList;
		List<VoltageSourceRecord> mVoltageSources;
		int mNumTerms = 0;
		int mNumNodes = 0;

		public override int TermCount { get { return mNumTerms; } }

		public override int VoltageSourceCount { get { return mVoltageSources.Count; } }

		public override int InternalNodeCount { get { return mNumNodes - mNumTerms; } }

		public override void Reset() {
			for (int i = 0; i < CompList.Count; i++) {
				CompList[i].Reset();
			}
		}

		/* are n1 and n2 connected internally somehow? */
		public override bool HasConnection(int n1, int n2) {
			var cl1 = mCompNodeList[n1].Links;
			var cl2 = mCompNodeList[n2].Links;
			/* see if any elements are connected to both n1 and n2, then call getConnection() on those */
			for (int i = 0; i < cl1.Count; i++) {
				var link1 = cl1[i];
				for (int j = 0; j < cl2.Count; j++) {
					var link2 = cl2[j];
					if (link1.Elm == link2.Elm && link1.Elm.HasConnection(link1.Node, link2.Node)) {
						return true;
					}
				}
			}
			return false;
		}

		/* is nodeIndex connected to ground somehow? */
		public override bool HasGroundConnection(int nodeIndex) {
			var links = mCompNodeList[nodeIndex].Links;
			for (int i = 0; i < links.Count; i++) {
				if (links[i].Elm.HasGroundConnection(links[i].Node)) {
					return true;
				}
			}
			return false;
		}

		public override void SetNode(int index, int id) {
			base.SetNode(index, id);
			var links = mCompNodeList[index].Links;
			for (int i = 0; i < links.Count; i++) {
				links[i].Elm.SetNode(links[i].Node, id);
			}
		}

		/* Find the component with the nth voltage
         * and set the
         * appropriate source in that component */
		public override void SetVoltageSource(int n, int v) {
			var vsr = mVoltageSources[n];
			vsr.elm.SetVoltageSource(vsr.vsNumForElement, v);
			vsr.vsNode = v;
		}

		public override void Stamp() {
			for (int i = 0; i < CompList.Count; i++) {
				var ce = CompList[i];
				/* current sources need special stamp method */
				if (ce is ElmCurrent elm) {
					elm.StampCurrentSource(false);
				} else {
					ce.Stamp();
				}
			}
		}

		#region [method(Circuit)]
		public override void PrepareIteration() {
			for (int i = 0; i < CompList.Count; i++) {
				CompList[i].PrepareIteration();
			}
		}

		public override void DoIteration() {
			for (int i = 0; i < CompList.Count; i++) {
				CompList[i].DoIteration();
			}
		}

		public override void FinishIteration() {
			for (int i = 0; i < CompList.Count; i++) {
				CompList[i].FinishIteration();
			}
		}

		public override double GetCurrent(int n) {
			double c = 0;
			var links = mCompNodeList[n].Links;
			for (int i = 0; i < links.Count; i++) {
				c += links[i].Elm.GetCurrent(links[i].Node);
			}
			return c;
		}

		public override void SetVoltage(int nodeIndex, double v) {
			base.SetVoltage(nodeIndex, v);
			var links = mCompNodeList[nodeIndex].Links;
			for (int i = 0; i < links.Count; i++) {
				links[i].Elm.SetVoltage(links[i].Node, v);
			}
			NodeVolts[nodeIndex] = v;
		}

		public override void SetCurrent(int vsn, double c) {
			for (int i = 0; i < mVoltageSources.Count; i++) {
				if (mVoltageSources[i].vsNode == vsn) {
					mVoltageSources[i].elm.SetCurrent(vsn, c);
				}
			}
		}
		#endregion

		public void LoadComposite(Dictionary<int, CircuitAnalizer.Node> nodeHash, List<BaseSymbol> symbolList, int[] externalNodes) {
			/* Flatten nodeHash in to compNodeList */
			mCompNodeList = new List<CircuitAnalizer.Node>();
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
					var cl = new CircuitAnalizer.Link() {
						Node = j + ce.TermCount,
						Elm = ce
					};
					var cn = new CircuitAnalizer.Node();
					cn.Links.Add(cl);
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

			AllocateNodes();
			Init();
		}

		protected virtual void Init() { }
	}
}
