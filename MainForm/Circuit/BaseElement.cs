namespace Circuit {
	public abstract class BaseElement {
		protected static bool ComparePair(int x1, int x2, int y1, int y2) {
			return (x1 == y1 && x2 == y2) || (x1 == y2 && x2 == y1);
		}

		public BaseElement() {
			alloc_nodes();
		}

		protected static Random mRandom = new();
		protected int m_volt_source;

		public int[] node_index;
		public Point[] node_pos;
		public double[] volts;
		public double current;

		#region [property(Analyze)]
		public abstract int TermCount { get; }
		public virtual bool IsWire { get { return false; } }
		public virtual int VoltageSourceCount { get { return 0; } }
		public virtual int InternalNodeCount { get { return 0; } }
		public virtual int ConnectionNodeCount { get { return TermCount; } }
		#endregion

		#region [method]
		/// <summary>
		/// allocate NodeIndex/Volts arrays we need
		/// </summary>
		public void alloc_nodes() {
			int n = TermCount + InternalNodeCount;
			if (node_index == null || node_index.Length != n) {
				node_index = new int[n];
				volts = new double[n];
			}
		}
		public void set_node_pos(params Point[] node) {
			node_pos = new Point[node.Length];
			for (int i = 0; i < node.Length; i++) {
				node_pos[i] = node[i];
			}
		}
		public void set_node_pos(PointF pos, params PointF[] node) {
			node_pos = new Point[node.Length + 1];
			node_pos[0].X = (int)pos.X;
			node_pos[0].Y = (int)pos.Y;
			for (int i = 0; i < node.Length; i++) {
				node_pos[i + 1].X = (int)node[i].X;
				node_pos[i + 1].Y = (int)node[i].Y;
			}
		}
		public void set_node_pos(PointF[] node, PointF pos) {
			node_pos = new Point[node.Length + 1];
			for (int i = 0; i < node.Length; i++) {
				node_pos[i].X = (int)node[i].X;
				node_pos[i].Y = (int)node[i].Y;
			}
			node_pos[node.Length].X = (int)pos.X;
			node_pos[node.Length].Y = (int)pos.Y;
		}
		public virtual double voltage_diff() {
			return volts[0] - volts[1];
		}
		#endregion

		#region [method(Analyze)]
		public virtual int get_connection(int n) { return node_index[n]; }
		public virtual bool has_connection(int n1, int n2) { return true; }
		public virtual bool has_ground_connection(int n1) { return false; }
		public virtual void stamp() { }
		public virtual void set_node(int p, int n) {
			if (p < node_index.Length) {
				node_index[p] = n;
			}
		}
		public virtual void set_voltage_source(int n, int v) {
			/* default implementation only makes sense for subclasses with one voltage source.
             * If we have 0 this isn't used, if we have >1 this won't work */
			m_volt_source = v;
		}
		public virtual void reset() {
			for (int i = 0; i != TermCount + InternalNodeCount; i++) {
				volts[i] = 0;
			}
		}
		public virtual void shorted() { }
		#endregion

		#region [method(Circuit)]
		public virtual void prepare_iteration() { }
		public virtual void do_iteration() { }
		public virtual void finish_iteration() { }
		public virtual double get_current_into_node(int n) {
			if (n == 0 && TermCount == 2) {
				return -current;
			} else {
				return current;
			}
		}
		public virtual void set_current(int n, double c) { current = c; }
		public virtual void set_voltage(int n, double c) { volts[n] = c; }
		#endregion
	}
}
