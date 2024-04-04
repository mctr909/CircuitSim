using System;
using Circuit.Elements.Passive;

namespace Circuit {
	struct CIRCUIT_LINK {
		public int node_index;
		public BaseElement elm;
	}

	class CIRCUIT_WIRE {
		public int node_index;
		public ElmWire elm;
		public List<BaseElement> connected_elms = [];
	}

	class CIRCUIT_NODE {
		public bool is_internal;
		public List<CIRCUIT_LINK> links = [];
	}

	class NODE_INFO {
		public int ROW;
		public int COL;
		public bool is_const;
		public bool drop;
		public bool right_changes;
		public bool left_changes;
		public double value;
	}

	static class CircuitElement {
		const int SUB_ITER_MAX = 1000;

		#region public variable
		public static double Time;
		public static double DeltaTime;
		public static bool Stopped;

		public static double[,] Matrix = new double[0, 0];
		public static double[] RightSide = [];
		public static NODE_INFO[] NodeInfo = [];
		public static int SubIterations;
		public static bool Converged;

		public static int NodeCount { get { return mNodes.Count; } }
		#endregion

		#region private variable
		static List<CIRCUIT_NODE> mNodes = [];
		static List<CIRCUIT_WIRE> mWires = [];
		static BaseElement[] mElements = [];
		static BaseElement[] mVoltageSources = [];
		static SCOPE_WAVE[] mWaves = [];
		static double[,] mMatrixOrg = new double[0, 0];
		static double[] mRightSideOrg = [];
		static int[] mPermute = [];
		static bool mNeedsMap;
		static int mMatrixSize;
		static int mMatrixFullSize;
		static long mLastIterTick = 0;
		static long mLastFrameTick = 0;
		#endregion

		#region public method
		public static void exec(ref bool is_running, ref bool did_analyze, double step_rate) {
			var tick = DateTime.Now.ToFileTimeUtc();

			if (0 == mLastIterTick) {
				mLastIterTick = tick;
				mLastFrameTick = tick;
				return;
			}

			/* Check if we don't need to run simulation (for very slow simulation speeds).
			/* If the circuit changed, do at least one iteration to make sure everything is consistent. */
			if (1000 >= step_rate * (tick - mLastIterTick) && !did_analyze) {
				mLastFrameTick = tick;
				return;
			}

			for (int step = 1; ; step++) {
				if (!do_iteration()) {
					break;
				}

				for (int i = 0; i < mWaves.Length; i++) {
					var p_wave = mWaves[i];
					var v = (float)p_wave.p_elm.voltage_diff();
					var index = p_wave.index;
					var p_value = p_wave.p_values[index];
					if (v < p_value.min) {
						p_wave.p_values[index].min = v;
					}
					if (v > p_value.max) {
						p_wave.p_values[index].max = v;
					}
					p_wave.counter++;
					if (p_wave.counter >= p_wave.speed) {
						index = (index + 1) & (p_wave.length - 1);
						p_wave.index = index;
						p_wave.counter = 0;
						p_wave.p_values[index].min = p_wave.p_values[index].max = v;
					}
				}

				/* Check whether enough time has elapsed to perform an *additional* iteration after
				/* those we have already completed. */
				tick = DateTime.Now.ToFileTimeUtc();
				if ((step + 1) * 1000 >= step_rate * (tick - mLastIterTick) || (tick - mLastFrameTick > 250000)) {
					break;
				}
				if (!is_running) {
					break;
				}
			}

			mLastIterTick = tick;
			mLastFrameTick = DateTime.Now.ToFileTimeUtc();
		}

		public static void setNodes(List<CIRCUIT_NODE> nodeList) {
			mNodes = nodeList;
		}

		public static void setWires(List<CIRCUIT_WIRE> wireList) {
			mWires = wireList;
		}

		public static void setElements(List<BaseElement> elmList) {
			mElements = elmList.ToArray();
		}

		public static void setVoltageSource(List<BaseElement> elmList) {
			mVoltageSources = new BaseElement[elmList.Count];
			var vsCount = 0;
			for (int i = 0; i < elmList.Count; i++) {
				var elm = elmList[i];
				var ivs = elm.VoltageSourceCount;
				for (int j = 0; j < ivs; j++) {
					mVoltageSources[vsCount] = elm;
					elm.set_voltage_source(j, vsCount++);
				}
			}
		}

		public static void setWaves(SCOPE_WAVE[] waves) {
			mWaves = waves;
		}

		public static void clearMatrix(int matrixSize) {
			mNeedsMap = false;
			mMatrixSize = mMatrixFullSize = matrixSize;
			mMatrixOrg = new double[matrixSize, matrixSize];
			mRightSideOrg = new double[matrixSize];
			mPermute = new int[matrixSize];
			Matrix = new double[matrixSize, matrixSize];
			RightSide = new double[matrixSize];
			NodeInfo = new NODE_INFO[matrixSize];
			for (int i = 0; i < matrixSize; i++) {
				NodeInfo[i] = new NODE_INFO();
			}
		}

		public static bool simplifyMatrix(int matrixSize) {
			for (int idxRow = 0; idxRow != matrixSize; idxRow++) {
				var nodeR = NodeInfo[idxRow];
				//Console.WriteLine("Row:" + idxRow + " L:" + nodeR.left_changes + " R:" + nodeR.right_changes + " drop:" + nodeR.drop);
				if (nodeR.left_changes || nodeR.drop || nodeR.right_changes) {
					continue;
				}
				var constIdx = -1;
				var constVal = 0.0;
				var constSum = 0.0;
				int idxCol;
				for (idxCol = 0; idxCol != matrixSize; idxCol++) {
					var matVal = Matrix[idxRow, idxCol];
					var nodeC = NodeInfo[idxCol];
					if (nodeC.is_const) {
						/* keep a running total of const values that have been removed already */
						constSum -= nodeC.value * matVal;
						continue;
					}
					if (matVal == 0) {
						/* ignore zeroes */
						continue;
					}
					if (constIdx == -1) {
						/* keep track of first nonzero element that is not ROW_CONST */
						constIdx = idxCol;
						constVal = matVal;
						continue;
					}
					/* more than one nonzero element? give up */
					break;
				}
				if (idxCol == matrixSize) {
					if (constIdx == -1) {
						Console.WriteLine("probably a singular matrix, try disabling matrix simplification above to check this");
						return false;
					}
					var constNode = NodeInfo[constIdx];
					if (constNode.is_const) {
						/* we found a row with only one nonzero nonconst entry; that value is a constant */
						Console.WriteLine("type already CONST for [" + constIdx + "]!");
						continue;
					}
					constNode.is_const = true;
					constNode.value = (RightSide[idxRow] + constSum) / constVal;
					nodeR.drop = true;
					idxRow = -1; /* start over from scratch */
				}
			}

			/* find size of new matrix */
			int rowCount = 0;
			int newSize = 0;
			for (int i = 0; i != matrixSize; i++) {
				var node = NodeInfo[i];
				node.ROW = node.drop ? -1 : rowCount++;
				node.COL = node.is_const ? -1 : newSize++;
			}
			//Console.WriteLine("old size:" + matrixSize + " new size:" + newSize);

			/* make the new, simplified matrix */
			var newMatrix = new double[newSize, newSize];
			var newRightSide = new double[newSize];
			rowCount = 0;
			for (int idxRow = 0; idxRow != matrixSize; idxRow++) {
				var nodeR = NodeInfo[idxRow];
				if (nodeR.drop) {
					continue;
				}
				newRightSide[rowCount] = RightSide[idxRow];
				for (int idxCol = 0; idxCol != matrixSize; idxCol++) {
					var nodeC = NodeInfo[idxCol];
					var m = Matrix[idxRow, idxCol];
					if (nodeC.is_const) {
						newRightSide[rowCount] -= nodeC.value * m;
					} else {
						newMatrix[rowCount, nodeC.COL] += m;
					}
				}
				rowCount++;
			}

			Array.Copy(newRightSide, mRightSideOrg, newSize);
			for (int ir = 0; ir != newSize; ir++) {
				for (int ic = 0; ic != newSize; ic++) {
					mMatrixOrg[ir, ic] = newMatrix[ir, ic];
				}
			}
			Matrix = newMatrix;
			RightSide = newRightSide;
			mMatrixSize = newSize;
			mNeedsMap = true;

			if (false) {
				Console.WriteLine("Matrix size:" + newSize);
				for (int j = 0; j != newSize; j++) {
					Console.WriteLine("RightSide[{0}]:{1}", j, newRightSide[j]);
					for (int i = 0; i != newSize; i++) {
						Console.WriteLine(" Matrix[{0},{1}]:{2}", j, i, newMatrix[j, i]);
					}
				}
			}

			return true;
		}
		#endregion

		#region private method
		/**
		 * factors a matrix into upper and lower triangular matrices by gaussian elimination.
		 * On entry, Matrix[0..n-1][0..n-1] is the matrix to be factored.
		 * Permute[] returns an integer vector of pivot indices, used in the luSolve() routine.
		 */
		static void lu_factor() {
			/* use Crout's method; loop through the columns */
			for (int j = 0; j != mMatrixSize; j++) {
				/* calculate upper triangular elements for this column */
				for (int i = 0; i != j; i++) {
					var q = Matrix[i, j];
					for (int k = 0; k != i; k++) {
						q -= Matrix[i, k] * Matrix[k, j];
					}
					Matrix[i, j] = q;
				}
				/* calculate lower triangular elements for this column */
				double largest = 0;
				int largestRow = -1;
				for (int i = j; i != mMatrixSize; i++) {
					var q = Matrix[i, j];
					for (int k = 0; k != j; k++) {
						q -= Matrix[i, k] * Matrix[k, j];
					}
					Matrix[i, j] = q;
					var x = Math.Abs(q);
					if (x >= largest) {
						largest = x;
						largestRow = i;
					}
				}
				/* pivoting */
				if (j != largestRow) {
					double x;
					for (int k = 0; k != mMatrixSize; k++) {
						x = Matrix[largestRow, k];
						Matrix[largestRow, k] = Matrix[j, k];
						Matrix[j, k] = x;
					}
				}
				/* keep track of row interchanges */
				mPermute[j] = largestRow;
				if (0.0 == Matrix[j, j]) {
					/* avoid zeros */
					Matrix[j, j] = 1e-18;
				}
				if (j != mMatrixSize - 1) {
					var mult = 1.0 / Matrix[j, j];
					for (int i = j + 1; i != mMatrixSize; i++) {
						Matrix[i, j] *= mult;
					}
				}
			}
		}

		/**
		 * Solves the set of n linear equations using a LU factorization previously performed by lu_factor.
		 * On input, RightSide[0..n-1] is the right hand side of the equations, and on output, contains the solution.
		 */
		static void lu_solve() {
			int i;
			/* find first nonzero b element */
			for (i = 0; i != mMatrixSize; i++) {
				var row = mPermute[i];
				var swap = RightSide[row];
				RightSide[row] = RightSide[i];
				RightSide[i] = swap;
				if (swap != 0) {
					break;
				}
			}
			int bi = i++;
			for (; i < mMatrixSize; i++) {
				var row = mPermute[i];
				var tot = RightSide[row];
				RightSide[row] = RightSide[i];
				/* forward substitution using the lower triangular matrix */
				for (int j = bi; j < i; j++) {
					tot -= Matrix[i, j] * RightSide[j];
				}
				RightSide[i] = tot;
			}
			for (i = mMatrixSize - 1; i >= 0; i--) {
				var tot = RightSide[i];
				/* back-substitution using the upper triangular matrix */
				for (int j = i + 1; j != mMatrixSize; j++) {
					tot -= Matrix[i, j] * RightSide[j];
				}
				RightSide[i] = tot / Matrix[i, i];
			}
		}

		static bool do_iteration() {
			for (int i = 0; i < mElements.Length; i++) {
				mElements[i].prepare_iteration();
			}

			for (SubIterations = 0; SubIterations < SUB_ITER_MAX; SubIterations++) {
				Array.Copy(mRightSideOrg, RightSide, mMatrixSize);
				for (int i = 0; i < mMatrixSize; i++) {
					for (int j = 0; j < mMatrixSize; j++) {
						Matrix[i, j] = mMatrixOrg[i, j];
					}
				}

				Converged = true;
				for (int i = 0; i < mElements.Length; i++) {
					mElements[i].do_iteration();
				}

				if (Stopped) {
					return false;
				}

				if (Converged && SubIterations > 0) {
					break;
				}

				lu_factor();
				lu_solve();

				var termNode = mNodes.Count - 1;
				for (int idxN = 0; idxN < mMatrixFullSize; idxN++) {
					var node = NodeInfo[idxN];
					var nodeVal = node.is_const ? node.value : RightSide[node.COL];
					if (idxN < termNode) {
						var nodeC = mNodes[idxN + 1];
						for (int k = 0; k < nodeC.links.Count; k++) {
							var link = nodeC.links[k];
							link.elm.set_voltage(link.node_index, nodeVal);
						}
					} else {
						var idxV = idxN - termNode;
						mVoltageSources[idxV].set_current(idxV, nodeVal);
					}
				}
			}

			if (SubIterations == SUB_ITER_MAX) {
				Console.WriteLine("計算が収束しませんでした");
				return false;
			}

			for (int i = 0; i < mElements.Length; i++) {
				mElements[i].finish_iteration();
			}

			/* calc wire currents */
			/* we removed wires from the matrix to speed things up.  in order to display wire currents,
			/* we need to calculate them now. */
			for (int i = 0; i < mWires.Count; i++) {
				var wi = mWires[i];
				var we = wi.elm;
				var wp = we.node_pos[wi.node_index];
				var curr = 0.0;
				for (int j = 0; j < wi.connected_elms.Count; j++) {
					var ce = wi.connected_elms[j];
					var n = 0;
					for (int k = 0; k != ce.TermCount; k++) {
						var ep = ce.node_pos[k];
						if (ep.X == wp.X && ep.Y == wp.Y) {
							n = k;
							break;
						}
					}
					curr += ce.get_current_into_node(n);
				}
				if (wi.node_index == 0) {
					we.set_current(-1, curr);
				} else {
					we.set_current(-1, -curr);
				}
			}
			Time += DeltaTime;
			return true;
		}
		#endregion

		#region stamp method
		public static void StampVoltageSource(int n1, int n2, int voltage_source, double v) {
			int vn = NodeCount + voltage_source;
			StampMatrix(vn, n1, -1);
			StampMatrix(vn, n2, 1);
			StampRightSide(vn, v);
			StampMatrix(n1, vn, 1);
			StampMatrix(n2, vn, -1);
		}

		/* use this if the amount of voltage is going to be updated in doStep(), by UpdateVoltageSource() */
		public static void StampVoltageSource(int n1, int n2, int voltage_source) {
			int vn = NodeCount + voltage_source;
			StampMatrix(vn, n1, -1);
			StampMatrix(vn, n2, 1);
			StampRightSide(vn);
			StampMatrix(n1, vn, 1);
			StampMatrix(n2, vn, -1);
		}

		public static void UpdateVoltageSource(int voltage_source, double v) {
			int vn = NodeCount + voltage_source;
			StampRightSide(vn, v);
		}

		public static void StampResistor(int n1, int n2, double r) {
			var g = 1.0 / r;
			StampMatrix(n1, n1, g);
			StampMatrix(n2, n2, g);
			StampMatrix(n1, n2, -g);
			StampMatrix(n2, n1, -g);
		}

		public static void StampConductance(int n1, int n2, double g) {
			StampMatrix(n1, n1, g);
			StampMatrix(n2, n2, g);
			StampMatrix(n1, n2, -g);
			StampMatrix(n2, n1, -g);
		}

		/* current from cn1 to cn2 is equal to voltage from vn1 to 2, divided by g */
		public static void StampVCCurrentSource(int cn1, int cn2, int vn1, int vn2, double g) {
			StampMatrix(cn1, vn1, g);
			StampMatrix(cn2, vn2, g);
			StampMatrix(cn1, vn2, -g);
			StampMatrix(cn2, vn1, -g);
		}

		public static void StampCurrentSource(int n1, int n2, double i) {
			StampRightSide(n1, -i);
			StampRightSide(n2, i);
		}

		/* stamp a current source from n1 to n2 depending on current through voltage_source */
		public static void StampCCCS(int n1, int n2, int voltage_source, double gain) {
			int vn = NodeCount + voltage_source;
			StampMatrix(n1, vn, gain);
			StampMatrix(n2, vn, -gain);
		}

		public static void StampMatrix(int r, int c, double x) {
			if (r > 0 && c > 0) {
				if (mNeedsMap) {
					r = NodeInfo[r - 1].ROW;
					var ri = NodeInfo[c - 1];
					if (ri.is_const) {
						RightSide[r] -= x * ri.value;
						return;
					}
					c = ri.COL;
				} else {
					r--;
					c--;
				}
				Matrix[r, c] += x;
			}
		}

		/* stamp value x on the right side of row i, representing an
		/* independent current source flowing into node i */
		public static void StampRightSide(int i, double x) {
			if (i > 0) {
				if (mNeedsMap) {
					i = NodeInfo[i - 1].ROW;
				} else {
					i--;
				}
				RightSide[i] += x;
			}
		}

		/* indicate that the value on the right side of row i changes in doStep() */
		public static void StampRightSide(int i) {
			if (i > 0) {
				NodeInfo[i - 1].right_changes = true;
			}
		}

		/* indicate that the values on the left side of row i change in doStep() */
		public static void StampNonLinear(int i) {
			if (i > 0) {
				NodeInfo[i - 1].left_changes = true;
			}
		}
		#endregion
	}
}
