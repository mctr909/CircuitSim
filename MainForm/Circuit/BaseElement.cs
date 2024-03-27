namespace Circuit {
	public abstract class BaseElement {
		protected static bool ComparePair(int x1, int x2, int y1, int y2) {
			return (x1 == y1 && x2 == y2) || (x1 == y2 && x2 == y1);
		}

		public BaseElement() {
			AllocNodes();
		}

		protected static Random mRandom = new();
		protected int mVoltSource;

		public int[] NodeIndex;
		public Point[] NodePos;
		public double[] Volts;
		public double Current;

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
		public void AllocNodes() {
			int n = TermCount + InternalNodeCount;
			if (NodeIndex == null || NodeIndex.Length != n) {
				NodeIndex = new int[n];
				Volts = new double[n];
			}
		}
		public void SetNodePos(params Point[] node) {
			NodePos = new Point[node.Length];
			for (int i = 0; i < node.Length; i++) {
				NodePos[i] = node[i];
			}
		}
		public void SetNodePos(PointF pos, params PointF[] node) {
			NodePos = new Point[node.Length + 1];
			NodePos[0].X = (int)pos.X;
			NodePos[0].Y = (int)pos.Y;
			for (int i = 0; i < node.Length; i++) {
				NodePos[i + 1].X = (int)node[i].X;
				NodePos[i + 1].Y = (int)node[i].Y;
			}
		}
		public void SetNodePos(PointF[] node, PointF pos) {
			NodePos = new Point[node.Length + 1];
			for (int i = 0; i < node.Length; i++) {
				NodePos[i].X = (int)node[i].X;
				NodePos[i].Y = (int)node[i].Y;
			}
			NodePos[node.Length].X = (int)pos.X;
			NodePos[node.Length].Y = (int)pos.Y;
		}
		public virtual double VoltageDiff() {
			return Volts[0] - Volts[1];
		}
		#endregion

		#region [method(Analyze)]
		public virtual int GetConnectionNode(int n) { return NodeIndex[n]; }
		public virtual bool HasConnection(int n1, int n2) { return true; }
		public virtual bool HasGroundConnection(int n1) { return false; }
		public virtual void Stamp() { }
		public virtual void SetNode(int p, int n) {
			if (p < NodeIndex.Length) {
				NodeIndex[p] = n;
			}
		}
		public virtual void SetVoltageSource(int n, int v) {
			/* default implementation only makes sense for subclasses with one voltage source.
             * If we have 0 this isn't used, if we have >1 this won't work */
			mVoltSource = v;
		}
		public virtual void Reset() {
			for (int i = 0; i != TermCount + InternalNodeCount; i++) {
				Volts[i] = 0;
			}
		}
		public virtual void Shorted() { }
		#endregion

		#region [method(Circuit)]
		public virtual void PrepareIteration() { }
		public virtual void DoIteration() { }
		public virtual void FinishIteration() { }
		public virtual double GetCurrentIntoNode(int n) {
			if (n == 0 && TermCount == 2) {
				return -Current;
			} else {
				return Current;
			}
		}
		public virtual void SetCurrent(int n, double c) { Current = c; }
		public virtual void SetVoltage(int n, double c) { Volts[n] = c; }
		#endregion
	}
}
