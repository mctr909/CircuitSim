namespace Circuit.Elements.Passive {
	class ElmNamedNode : BaseElement {
		public string Name = "Node";
		public bool IsOutput = true;

		static Dictionary<string, int> mNodeList;
		int mNodeNumber;

		public override int TermCount { get { return 1; } }

		public override int ConnectionNodeCount { get { return 2; } }

		// this is basically a wire, since it just connects two nodes together
		public override bool IsWire { get { return true; } }

		public override int InternalNodeCount {
			get {
				// this can happen at startup
				if (mNodeList == null) {
					return 0;
				}
				// node assigned already?
				if (null != Name && mNodeList.ContainsKey(Name)) {
					var nn = mNodeList[Name];
					mNodeNumber = nn;
					return 0;
				}
				// allocate a new one
				return 1;
			}
		}

		public override int VoltageSourceCount { get { return 1; } }

		public override double VoltageDiff { get { return Volts[0]; } }

		public override double GetCurrentIntoNode(int n) { return -Current; }

		public static void ResetNodeList() {
			mNodeList = new Dictionary<string, int>();
		}

		// get connection node (which is the same as regular nodes for all elements but this one).
		// node 0 is the terminal, node 1 is the internal node shared by all nodes with same name
		public override int GetConnectionNode(int n) {
			if (n == 0) {
				return Nodes[0];
			}
			return mNodeNumber;
		}

		public override void SetNode(int p, int n) {
			base.SetNode(p, n);
			if (p == 1) {
				// assign new node
				mNodeList.Add(Name, n);
				mNodeNumber = n;
			}
		}

		public override void Stamp() {
			CircuitElement.StampVoltageSource(mNodeNumber, Nodes[0], mVoltSource, 0);
		}

		public override void SetCurrent(int x, double c) { Current = -c; }

		public override void SetVoltage(int n, double c) {
			if (n == 0) {
				Volts[0] = c;
			}
		}
	}
}
