using System;
using System.Collections.Generic;

using Circuit.Elements.Input;

namespace Circuit.Elements.Custom {
	class VoltageSourceRecord {
		public int vsNumForElement;
		public int vsNode;
		public BaseElement elm;
	}

	class ElmComposite : BaseElement {
		/* list of elements contained in this subcircuit */
		protected List<BaseElement> CompList = new List<BaseElement>();

		/* list of nodes, mapping each one to a list of elements that reference that node */
		List<CircuitNode> mCompNodeList;
		List<VoltageSourceRecord> mVoltageSources;
		int mNumTerms = 0;
		int mNumNodes = 0;

		public ElmComposite() : base() { }

		public override int TermCount { get { return mNumTerms; } }

		public override int VoltageSourceCount { get { return mVoltageSources.Count; } }

		public override int InternalNodeCount { get { return mNumNodes - mNumTerms; } }

		public override void Reset() {
			for (int i = 0; i < CompList.Count; i++) {
				CompList[i].Reset();
			}
		}

		/* are n1 and n2 connected internally somehow? */
		public override bool GetConnection(int n1, int n2) {
			var cnLinks1 = mCompNodeList[n1].Links;
			var cnLinks2 = mCompNodeList[n2].Links;

			/* see if any elements are connected to both n1 and n2, then call getConnection() on those */
			for (int i = 0; i < cnLinks1.Count; i++) {
				var link1 = cnLinks1[i];
				for (int j = 0; j < cnLinks2.Count; j++) {
					var link2 = cnLinks2[j];
					if (link1.Elm == link2.Elm && link1.Elm.GetConnection(link1.Num, link2.Num)) {
						return true;
					}
				}
			}
			return false;
		}

		/* is n1 connected to ground somehow? */
		public override bool HasGroundConnection(int n1) {
			List<CircuitNode.LINK> cnLinks;
			cnLinks = mCompNodeList[n1].Links;
			for (int i = 0; i < cnLinks.Count; i++) {
				if (cnLinks[i].Elm.HasGroundConnection(cnLinks[i].Num)) {
					return true;
				}
			}
			return false;
		}

		public override void SetNode(int p, int n) {
			base.SetNode(p, n);
			var cnLinks = mCompNodeList[p].Links;
			for (int i = 0; i < cnLinks.Count; i++) {
				cnLinks[i].Elm.SetNode(cnLinks[i].Num, n);
			}
		}

		public override void Stamp() {
			for (int i = 0; i < CompList.Count; i++) {
				var ce = CompList[i];
				/* current sources need special stamp method */
				if (ce is ElmCurrent) {
					((ElmCurrent)ce).stampCurrentSource(false);
				} else {
					ce.Stamp();
				}
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

		public override void IterationFinished() {
			for (int i = 0; i < CompList.Count; i++) {
				CompList[i].IterationFinished();
			}
		}

		public override void SetVoltage(int n, double c) {
			base.SetVoltage(n, c);
			var cnLinks = mCompNodeList[n].Links;
			for (int i = 0; i < cnLinks.Count; i++) {
				cnLinks[i].Elm.SetVoltage(cnLinks[i].Num, c);
			}
			Volts[n] = c;
		}

		public override void SetCurrent(int vsn, double c) {
			for (int i = 0; i < mVoltageSources.Count; i++) {
				if (mVoltageSources[i].vsNode == vsn) {
					mVoltageSources[i].elm.SetCurrent(vsn, c);
				}
			}
		}

		public override double GetCurrentIntoNode(int n) {
			double c = 0;
			var cnLinks = mCompNodeList[n].Links;
			for (int i = 0; i < cnLinks.Count; i++) {
				c += cnLinks[i].Elm.GetCurrentIntoNode(cnLinks[i].Num);
			}
			return c;
		}

		protected virtual void Init() { }

		void SetComposite(Dictionary<int, CircuitNode> nodeHash, List<BaseSymbol> symbolList, int[] externalNodes) {
			/* Flatten nodeHash in to compNodeList */
			mCompNodeList = new List<CircuitNode>();
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
					var cnLink = new CircuitNode.LINK();
					cnLink.Num = j + ce.TermCount;
					cnLink.Elm = ce;
					var cn = new CircuitNode();
					cn.Links.Add(cnLink);
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
					var vsRecord = new VoltageSourceRecord();
					vsRecord.elm = CompList[i];
					vsRecord.vsNumForElement = j;
					mVoltageSources.Add(vsRecord);
				}
			}

			AllocNodes();
			Init();
		}

		protected void LoadComposite(List<BaseSymbol> symbolList, string model, int[] externalNodes) {
			var compNodeHash = new Dictionary<int, CircuitNode>();
			var strModels = new StringTokenizer(model, "\r");
			while (strModels.HasMoreTokens) {
				string modelLine;
				strModels.nextToken(out modelLine);
				var strModel = new StringTokenizer(modelLine, " +\t\n\r\f");
				var ceType = strModel.nextTokenEnum(DUMP_ID.INVALID);
				var newce = MenuItems.ConstructElement(ceType);
				newce.ReferenceName = "";
				symbolList.Add(newce);
				int thisPost = 0;
				while (strModel.HasMoreTokens) {
					var nodeOfThisPost = strModel.nextTokenInt();
					var cnLink = new CircuitNode.LINK();
					cnLink.Num = thisPost;
					cnLink.Elm = newce.Element;
					if (!compNodeHash.ContainsKey(nodeOfThisPost)) {
						var cn = new CircuitNode();
						cn.Links.Add(cnLink);
						compNodeHash.Add(nodeOfThisPost, cn);
					} else {
						var cn = compNodeHash[nodeOfThisPost];
						cn.Links.Add(cnLink);
					}
					thisPost++;
				}
			}
			SetComposite(compNodeHash, symbolList, externalNodes);
		}
	}
}
