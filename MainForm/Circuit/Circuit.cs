using Circuit.Elements.Passive;

namespace Circuit {
	class CircuitNode {
		public struct LINK {
			public int Num;
			public BaseElement Elm;
		}
		public List<LINK> Links = [];
		public bool Internal;
	}

	static class Circuit {
		public class ROW_INFO {
			public bool IsConst;
			public bool RightChanges; /* row's right side changes */
			public bool LeftChanges;  /* row's left side changes */
			public bool DropRow;      /* row is not needed in matrix */
			public int MapCol;
			public int MapRow;
			public double Value;
		}

		public class WireInfo {
			public ElmWire Wire;
			public List<BaseElement> Neighbors;
			public int Post;
			public WireInfo(ElmWire w) { Wire = w; }
		}

		const int SubIterMax = 1000;

		#region variable
		public static Random Random = new();
		public static List<BaseElement> ElmList = [];
		public static double Time;
		public static string StopMessage;
		public static bool Converged;
		public static int SubIterations;

		public static double[,] Matrix;
		public static double[] RightSide;
		public static ROW_INFO[] RowInfo;
		public static List<CircuitNode> Nodes;
		public static double TimeStep;

		public static List<WireInfo> WireInfoList;
		public static BaseElement[] VoltageSources;
		public static bool CircuitNeedsMap;

		public static int MatrixSize;
		public static int MatrixFullSize;
		public static int[] Permute;
		public static double[] OrigRightSide;
		public static double[,] OrigMatrix;
		#endregion

		public static void Stop(string s) {
			StopMessage = s;
			Matrix = null;  /* causes an exception */
			SetSimRunning(false);
		}
		public static void SetSimRunning(bool s) {
			Console.WriteLine(StopMessage);
			if (s) {
				if (StopMessage != null) {
					return;
				}
				CircuitSymbol.IsRunning = true;
				ControlPanel.BtnRunStop.Text = "停止";
			} else {
				CircuitSymbol.IsRunning = false;
				CircuitSymbol.NeedAnalyze = false;
				ControlPanel.BtnRunStop.Text = "実行";
			}
		}
		public static bool DoIteration() {
			for (int i = 0; i < ElmList.Count; i++) {
				ElmList[i].PrepareIteration();
			}

			for (SubIterations = 0; SubIterations < SubIterMax; SubIterations++) {
				Converged = true;

				Array.Copy(OrigRightSide, RightSide, MatrixSize);
				for (int i = 0; i < MatrixSize; i++) {
					for (int j = 0; j < MatrixSize; j++) {
						Matrix[i, j] = OrigMatrix[i, j];
					}
				}

				for (int i = 0; i < ElmList.Count; i++) {
					ElmList[i].DoIteration();
				}
				if (StopMessage != null) {
					return false;
				}

				for (int j = 0; j < MatrixSize; j++) {
					for (int i = 0; i < MatrixSize; i++) {
						var x = Matrix[i, j];
						if (double.IsNaN(x) || double.IsInfinity(x)) {
							//stop("Matrix[" + i + "," + j + "] is NaN/infinite");
							return false;
						}
					}
				}

				if (Converged && SubIterations > 0) {
					break;
				}

				if (!luFactor()) {
					//stop("Singular matrix!");
					return false;
				}
				luSolve();

				for (int j = 0; j < MatrixFullSize; j++) {
					var ri = RowInfo[j];
					double res;
					if (ri.IsConst) {
						res = ri.Value;
					} else {
						res = RightSide[ri.MapCol];
					}
					if (double.IsNaN(res) || double.IsInfinity(res)) {
						//Console.WriteLine((ri.IsConst ? ("RowInfo[" + j + "]") : ("RightSide[" + ri.MapCol + "]")) + " is NaN/infinite");
						return false;
					}
					if (j < Nodes.Count - 1) {
						var cn = Nodes[j + 1];
						for (int k = 0; k < cn.Links.Count; k++) {
							var cnl = cn.Links[k];
							cnl.Elm.SetVoltage(cnl.Num, res);
						}
					} else {
						var ji = j - (Nodes.Count - 1);
						VoltageSources[ji].SetCurrent(ji, res);
					}
				}
			}

			if (SubIterations == SubIterMax) {
				//stop("計算が収束しませんでした");
				return false;
			}

			for (int i = 0; i < ElmList.Count; i++) {
				ElmList[i].IterationFinished();
			}

			/* calc wire currents */
			/* we removed wires from the matrix to speed things up.  in order to display wire currents,
            /* we need to calculate them now. */
			for (int i = 0; i < WireInfoList.Count; i++) {
				var wi = WireInfoList[i];
				var we = wi.Wire;
				var cur = 0.0;
				var p = we.NodePos[wi.Post];
				for (int j = 0; j < wi.Neighbors.Count; j++) {
					var ce = wi.Neighbors[j];
					var n = 0;
					for (int k = 0; k != ce.TermCount; k++) {
						var nodePos = ce.NodePos[k];
						if (nodePos.X == p.X && nodePos.Y == p.Y) {
							n = k;
							break;
						}
					}
					cur += ce.GetCurrentIntoNode(n);
				}
				if (wi.Post == 0) {
					we.SetCurrent(-1, cur);
				} else {
					we.SetCurrent(-1, -cur);
				}
			}

			return true;
		}

		#region private method
		/* factors a matrix into upper and lower triangular matrices by
        /* gaussian elimination. On entry, Matrix[0..n-1][0..n-1] is the
        /* matrix to be factored. mPermute[] returns an integer vector of pivot
        /* indices, used in the lu_solve() routine. */
		static bool luFactor() {
			/* check for a possible singular matrix by scanning for rows that
            /* are all zeroes */
			for (int i = 0; i != MatrixSize; i++) {
				bool row_all_zeros = true;
				for (int j = 0; j != MatrixSize; j++) {
					if (Matrix[i, j] != 0) {
						row_all_zeros = false;
						break;
					}
				}
				/* if all zeros, it's a singular matrix */
				if (row_all_zeros) {
					return false;
				}
			}
			/* use Crout's method; loop through the columns */
			for (int j = 0; j != MatrixSize; j++) {
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
				for (int i = j; i != MatrixSize; i++) {
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
					for (int k = 0; k != MatrixSize; k++) {
						x = Matrix[largestRow, k];
						Matrix[largestRow, k] = Matrix[j, k];
						Matrix[j, k] = x;
					}
				}
				/* keep track of row interchanges */
				Permute[j] = largestRow;
				/* avoid zeros */
				if (Matrix[j, j] == 0.0) {
					Console.WriteLine("avoided zero");
					Matrix[j, j] = 1e-18;
				}
				if (j != MatrixSize - 1) {
					var mult = 1.0 / Matrix[j, j];
					for (int i = j + 1; i != MatrixSize; i++) {
						Matrix[i, j] *= mult;
					}
				}
			}
			return true;
		}

		/* Solves the set of n linear equations using a LU factorization
        /* previously performed by lu_factor.  On input, RightSide[0..n-1] is the right
        /* hand side of the equations, and on output, contains the solution. */
		static void luSolve() {
			int i;
			/* find first nonzero b element */
			for (i = 0; i != MatrixSize; i++) {
				var row = Permute[i];
				var swap = RightSide[row];
				RightSide[row] = RightSide[i];
				RightSide[i] = swap;
				if (swap != 0) {
					break;
				}
			}
			int bi = i++;
			for (; i < MatrixSize; i++) {
				var row = Permute[i];
				var tot = RightSide[row];
				RightSide[row] = RightSide[i];
				/* forward substitution using the lower triangular matrix */
				for (int j = bi; j < i; j++) {
					tot -= Matrix[i, j] * RightSide[j];
				}
				RightSide[i] = tot;
			}
			for (i = MatrixSize - 1; i >= 0; i--) {
				var tot = RightSide[i];
				/* back-substitution using the upper triangular matrix */
				for (int j = i + 1; j != MatrixSize; j++) {
					tot -= Matrix[i, j] * RightSide[j];
				}
				RightSide[i] = tot / Matrix[i, i];
			}
		}
		#endregion

		#region stamp method
		/* stamp independent voltage source #vs, from n1 to n2, amount v */
		public static void StampVoltageSource(int n1, int n2, int vs, double v) {
			int vn = Nodes.Count + vs;
			StampMatrix(vn, n1, -1);
			StampMatrix(vn, n2, 1);
			StampRightSide(vn, v);
			StampMatrix(n1, vn, 1);
			StampMatrix(n2, vn, -1);
		}

		/* use this if the amount of voltage is going to be updated in doStep(), by updateVoltageSource() */
		public static void StampVoltageSource(int n1, int n2, int vs) {
			int vn = Nodes.Count + vs;
			StampMatrix(vn, n1, -1);
			StampMatrix(vn, n2, 1);
			StampRightSide(vn);
			StampMatrix(n1, vn, 1);
			StampMatrix(n2, vn, -1);
		}

		/* update voltage source in doStep() */
		public static void UpdateVoltageSource(int vs, double v) {
			int vn = Nodes.Count + vs;
			StampRightSide(vn, v);
		}

		public static void StampResistor(int n1, int n2, double r) {
			double r0 = 1 / r;
			if (double.IsNaN(r0) || double.IsInfinity(r0)) {
				Console.WriteLine("bad resistance " + r + " " + r0 + "\n");
				throw new Exception("bad resistance " + r + " " + r0);
			}
			StampMatrix(n1, n1, r0);
			StampMatrix(n2, n2, r0);
			StampMatrix(n1, n2, -r0);
			StampMatrix(n2, n1, -r0);
		}

		public static void StampConductance(int n1, int n2, double r0) {
			StampMatrix(n1, n1, r0);
			StampMatrix(n2, n2, r0);
			StampMatrix(n1, n2, -r0);
			StampMatrix(n2, n1, -r0);
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

		/* stamp a current source from n1 to n2 depending on current through vs */
		public static void StampCCCS(int n1, int n2, int vs, double gain) {
			int vn = Nodes.Count + vs;
			StampMatrix(n1, vn, gain);
			StampMatrix(n2, vn, -gain);
		}

		/// <summary>
		/// <para>meaning that a voltage change of dv in node j will increase the current into node i by x dv.</para>
		/// <para>(Unless i or j is a voltage source node.)</para>
		/// </summary>
		/// <param name="r">row</param>
		/// <param name="c">column</param>
		/// <param name="x">stamp value in row, column</param>
		public static void StampMatrix(int r, int c, double x) {
			if (r > 0 && c > 0) {
				if (CircuitNeedsMap) {
					r = RowInfo[r - 1].MapRow;
					var ri = RowInfo[c - 1];
					if (ri.IsConst) {
						RightSide[r] -= x * ri.Value;
						return;
					}
					c = ri.MapCol;
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
				if (CircuitNeedsMap) {
					i = RowInfo[i - 1].MapRow;
				} else {
					i--;
				}
				RightSide[i] += x;
			}
		}

		/* indicate that the value on the right side of row i changes in doStep() */
		public static void StampRightSide(int i) {
			if (i > 0) {
				RowInfo[i - 1].RightChanges = true;
			}
		}

		/* indicate that the values on the left side of row i change in doStep() */
		public static void StampNonLinear(int i) {
			if (i > 0) {
				RowInfo[i - 1].LeftChanges = true;
			}
		}
		#endregion
	}
}
