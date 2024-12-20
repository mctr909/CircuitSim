﻿namespace Circuit.Elements.Passive {
	class ElmNamedNode : BaseElement {
		public string Name = "Node";
		public bool IsOutput = true;

		static Dictionary<string, int> mNodeList;
		int mNodeId;

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
					mNodeId = nn;
					return 0;
				}
				// allocate a new one
				return 1;
			}
		}

		public override int VoltageSourceCount { get { return 1; } }

		public static void ResetNodeList() {
			mNodeList = new Dictionary<string, int>();
		}

		public override double GetVoltageDiff() {
			return Volts[0];
		}

		#region [method(Analyze)]
		// get connection node (which is the same as regular nodes for all elements but this one).
		// node 0 is the terminal, node 1 is the internal node shared by all nodes with same name
		public override int GetConnection(int n) {
			if (n == 0) {
				return NodeId[0];
			}
			return mNodeId;
		}

		public override void SetNode(int n, int id) {
			base.SetNode(n, id);
			if (n == 1) {
				// assign new node
				mNodeList.Add(Name, id);
				mNodeId = id;
			}
		}

		public override void Stamp() {
			StampVoltageSource(mNodeId, NodeId[0], mVoltSource, 0);
		}
		#endregion

		#region [method(Circuit)]
		public override double GetCurrent(int n) { return -Current; }

		public override void SetCurrent(int x, double c) { Current = -c; }

		public override void SetVoltage(int n, double c) {
			if (n == 0) {
				Volts[0] = c;
			}
		}
		#endregion
	}
}
