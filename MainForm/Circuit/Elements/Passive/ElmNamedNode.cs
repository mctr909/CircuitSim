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

		public static void ResetNodeList() {
			mNodeList = new Dictionary<string, int>();
		}

		public override double voltage_diff() {
			return volts[0];
		}

		#region [method(Analyze)]
		// get connection node (which is the same as regular nodes for all elements but this one).
		// node 0 is the terminal, node 1 is the internal node shared by all nodes with same name
		public override int get_connection(int n) {
			if (n == 0) {
				return node_index[0];
			}
			return mNodeNumber;
		}

		public override void set_node(int p, int n) {
			base.set_node(p, n);
			if (p == 1) {
				// assign new node
				mNodeList.Add(Name, n);
				mNodeNumber = n;
			}
		}

		public override void stamp() {
			CircuitElement.StampVoltageSource(mNodeNumber, node_index[0], m_volt_source, 0);
		}
		#endregion

		#region [method(Circuit)]
		public override double get_current_into_node(int n) { return -current; }

		public override void set_current(int x, double c) { current = -c; }

		public override void set_voltage(int n, double c) {
			if (n == 0) {
				volts[0] = c;
			}
		}
		#endregion
	}
}
