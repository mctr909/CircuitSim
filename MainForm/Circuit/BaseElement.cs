namespace Circuit {
	public abstract class BaseElement {
		protected static bool ComparePair(int x1, int x2, int y1, int y2) {
			return (x1 == y1 && x2 == y2) || (x1 == y2 && x2 == y1);
		}

		public BaseElement() {
			AllocateNodes();
		}

		protected static Random mRandom = new();
		protected int mVoltSource;

		public int[] NodeId;
		public Point[] NodePos;
		public double[] NodeVolts;
		public double Current;

		#region [property(Analyze)]
		public abstract int TermCount { get; }
		public virtual bool IsWire { get { return false; } }
		public virtual int VoltageSourceCount { get { return 0; } }
		public virtual int InternalNodeCount { get { return 0; } }
		public virtual int ConnectionNodeCount { get { return TermCount; } }
		#endregion

		#region [method]
		public void AllocateNodes() {
			int n = TermCount + InternalNodeCount;
			if (NodeId == null || NodeId.Length != n) {
				NodeId = new int[n];
				NodeVolts = new double[n];
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
		public virtual double GetVoltageDiff() {
			return NodeVolts[0] - NodeVolts[1];
		}
		#endregion

		#region [method(Stamp)]
		protected static void StampMatrix(int r, int c, double val) {
			if (r > 0 && c > 0) {
				CircuitAnalizer.Matrix[r - 1, c - 1] += val;
			}
		}
		/* indicate that the values on the left side of row i */
		protected static void StampNonLinear(int i) {
			CircuitAnalizer.NodeInfos[i - 1].NonLinear = true;
		}
		/* indicate that the value on the right side of row i */
		protected static void StampRightSide(int i) {
			CircuitAnalizer.NodeInfos[i - 1].RightChanges = true;
		}
		/* stamp value val on the right side of row i, representing an
		/* independent current source flowing into node i */
		protected static void StampRightSide(int i, double val) {
			CircuitAnalizer.RightSide[i - 1] += val;
		}
		protected static void StampConductance(int n1, int n2, double g) {
			StampMatrix(n1, n1, g);
			StampMatrix(n1, n2, -g);
			StampMatrix(n2, n1, -g);
			StampMatrix(n2, n2, g);
		}
		protected static void StampResistor(int n1, int n2, double r) {
			var g = 1.0 / r;
			StampMatrix(n1, n1, g);
			StampMatrix(n1, n2, -g);
			StampMatrix(n2, n1, -g);
			StampMatrix(n2, n2, g);
		}
		protected static void StampVoltageSource(int n, int vsIndex, double v) {
			var vn = CircuitAnalizer.NodeCount + vsIndex;
			StampMatrix(vn, n, 1);
			StampMatrix(n, vn, -1);
			StampRightSide(vn, v);
		}
		protected static void StampVoltageSource(int n1, int n2, int vsIndex, double v) {
			var vn = CircuitAnalizer.NodeCount + vsIndex;
			StampMatrix(vn, n1, -1);
			StampMatrix(vn, n2, 1);
			StampMatrix(n1, vn, 1);
			StampMatrix(n2, vn, -1);
			StampRightSide(vn, v);
		}
		/* use this if the amount of voltage is going to be updated in DoIteration(), by UpdateVoltage() */
		protected static void StampVoltageSource(int n, int vsIndex) {
			var vn = CircuitAnalizer.NodeCount + vsIndex;
			StampMatrix(vn, n, 1);
			StampMatrix(n, vn, -1);
			StampRightSide(vn);
		}
		/* use this if the amount of voltage is going to be updated in DoIteration(), by UpdateVoltage() */
		protected static void StampVoltageSource(int n1, int n2, int vsIndex) {
			var vn = CircuitAnalizer.NodeCount + vsIndex;
			StampMatrix(vn, n1, -1);
			StampMatrix(vn, n2, 1);
			StampMatrix(n1, vn, 1);
			StampMatrix(n2, vn, -1);
			StampRightSide(vn);
		}
		/* current from cn1 to cn2 is equal to voltage from vn1 to 2, divided by g */
		protected static void StampVCCurrentSource(int cn1, int cn2, int vn1, int vn2, double g) {
			StampMatrix(cn1, vn1, g);
			StampMatrix(cn1, vn2, -g);
			StampMatrix(cn2, vn1, -g);
			StampMatrix(cn2, vn2, g);
		}
		#endregion

		#region [method(Analyze)]
		public virtual int GetConnection(int nodeIndex) { return NodeId[nodeIndex]; }
		public virtual bool HasConnection(int n1, int n2) { return true; }
		public virtual bool HasGroundConnection(int nodeIndex) { return false; }
		public virtual void Stamp() { }
		public virtual void SetNode(int index, int id) {
			if (index < NodeId.Length) {
				NodeId[index] = id;
			}
		}
		public virtual void SetVoltageSource(int n, int v) {
			/* default implementation only makes sense for subclasses with one voltage source.
             * If we have 0 this isn't used, if we have >1 this won't work */
			mVoltSource = v;
		}
		public virtual void Reset() {
			for (int i = 0; i != TermCount + InternalNodeCount; i++) {
				NodeVolts[i] = 0;
			}
		}
		public virtual void Shorted() { }
		#endregion

		#region [method(Circuit)]
		public virtual void PrepareIteration() { }
		public virtual void DoIteration() { }
		public virtual void FinishIteration() { }
		public virtual double GetCurrent(int n) {
			if (n == 0 && TermCount == 2) {
				return -Current;
			} else {
				return Current;
			}
		}
		public virtual void SetCurrent(int n, double c) { Current = c; }
		public virtual void SetVoltage(int nodeIndex, double v) { NodeVolts[nodeIndex] = v; }
		protected static void UpdateCurrent(int n1, int n2, double i) {
			n1 = CircuitElement.NODE_INFOS[n1 - 1].row;
			n2 = CircuitElement.NODE_INFOS[n2 - 1].row;
			CircuitElement.RIGHT_SIDE[n1] -= i;
			CircuitElement.RIGHT_SIDE[n2] += i;
		}
		protected static void UpdateVoltage(int vsIndex, double v) {
			vsIndex += CircuitElement.VOLTAGE_SOURCE_BEGIN;
			vsIndex = CircuitElement.NODE_INFOS[vsIndex].row;
			CircuitElement.RIGHT_SIDE[vsIndex] += v;
		}
		protected static void UpdateConductance(int n1, int n2, double g) {
			var ni1 = CircuitElement.NODE_INFOS[n1 - 1];
			var ni2 = CircuitElement.NODE_INFOS[n2 - 1];
			n1 = ni1.row;
			n2 = ni2.row;
			if (ni1.is_const) {
				CircuitElement.RIGHT_SIDE[n1] -= g * ni1.value;
				CircuitElement.RIGHT_SIDE[n2] += g * ni1.value;
			} else {
				CircuitElement.MATRIX[n1, ni1.col] += g;
				CircuitElement.MATRIX[n2, ni1.col] -= g;
			}
			if (ni2.is_const) {
				CircuitElement.RIGHT_SIDE[n1] += g * ni2.value;
				CircuitElement.RIGHT_SIDE[n2] -= g * ni2.value;
			} else {
				CircuitElement.MATRIX[n1, ni2.col] -= g;
				CircuitElement.MATRIX[n2, ni2.col] += g;
			}
		}
		protected static void UpdateMatrix(int r, int c, double val) {
			r = CircuitElement.NODE_INFOS[r - 1].row;
			var nc = CircuitElement.NODE_INFOS[c - 1];
			if (nc.is_const) {
				CircuitElement.RIGHT_SIDE[r] -= val * nc.value;
			} else {
				CircuitElement.MATRIX[r, nc.col] += val;
			}
		}
		#endregion
	}
}
