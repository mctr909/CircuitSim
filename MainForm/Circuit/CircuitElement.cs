using Circuit.Elements.Passive;

namespace Circuit {
	struct CIRCUIT_LINK {
		public int node_index;
		public BaseElement p_elm;
	}

	class CIRCUIT_NODE {
		public bool is_internal;
		public List<CIRCUIT_LINK> links = [];
	}

	class CIRCUIT_WIRE {
		public int post;
		public ElmWire p_elm;
		public List<BaseElement> neighbors = [];
	}

	class CIRCUIT_ROW {
		public bool is_const;
		public bool right_changes;
		public bool left_changes;
		public bool drop;
		public int col;
		public int row;
		public double value;
	}

	static class CircuitElement {
		const int SUB_ITER_MAX = 1000;

		#region variable
		public static double time;
		public static double delta_time;

		public static CIRCUIT_NODE[] nodes = [];
		public static ScopePlot[] plots = [];
		public static double[,] matrix = new double[0, 0];
		public static double[] right_side = [];
		public static CIRCUIT_ROW[] row_info = [];
		public static int sub_iterations;
		public static bool stopped;
		public static bool converged;

		public static BaseElement[] elements = [];
		public static CIRCUIT_WIRE[] wires = [];
		public static BaseElement[] voltage_sources = [];
		public static double[,] orig_matrix = new double[0, 0];
		public static double[] orig_right_side = [];
		public static int[] permute = [];
		public static bool needs_map;
		public static int matrix_size;
		public static int matrix_full_size;

		static long last_iter_time = 0;
		static long last_frame_time = 0;
		#endregion

		public static void exec(ref bool is_running, ref bool did_analyze, double step_rate) {
			var time = DateTime.Now.ToFileTimeUtc();

			if (0 == last_iter_time) {
				last_iter_time = time;
				last_frame_time = time;
				return;
			}

			/* Check if we don't need to run simulation (for very slow simulation speeds).
			/* If the circuit changed, do at least one iteration to make sure everything is consistent. */
			if (1000 >= step_rate * (time - last_iter_time) && !did_analyze) {
				last_frame_time = time;
				return;
			}

			for (int step = 1; ; step++) {
				if (!do_iteration()) {
					break;
				}

				for (int i = 0; i < plots.Length; i++) {
					plots[i].TimeStep();
				}

				/* Check whether enough time has elapsed to perform an *additional* iteration after
				/* those we have already completed. */
				time = DateTime.Now.ToFileTimeUtc();
				if ((step + 1) * 1000 >= step_rate * (time - last_iter_time) || (time - last_frame_time > 250000)) {
					break;
				}
				if (!is_running) {
					break;
				}
			}

			last_iter_time = time;
			last_frame_time = DateTime.Now.ToFileTimeUtc();
		}

		#region private method
		/**
		 * factors a matrix into upper and lower triangular matrices by gaussian elimination.
		 * On entry, Matrix[0..n-1][0..n-1] is the matrix to be factored.
		 * Permute[] returns an integer vector of pivot indices, used in the luSolve() routine.
		 */
		static void lu_factor() {
			/* use Crout's method; loop through the columns */
			for (int j = 0; j != matrix_size; j++) {
				/* calculate upper triangular elements for this column */
				for (int i = 0; i != j; i++) {
					var q = matrix[i, j];
					for (int k = 0; k != i; k++) {
						q -= matrix[i, k] * matrix[k, j];
					}
					matrix[i, j] = q;
				}
				/* calculate lower triangular elements for this column */
				double largest = 0;
				int largestRow = -1;
				for (int i = j; i != matrix_size; i++) {
					var q = matrix[i, j];
					for (int k = 0; k != j; k++) {
						q -= matrix[i, k] * matrix[k, j];
					}
					matrix[i, j] = q;
					var x = Math.Abs(q);
					if (x >= largest) {
						largest = x;
						largestRow = i;
					}
				}
				/* pivoting */
				if (j != largestRow) {
					double x;
					for (int k = 0; k != matrix_size; k++) {
						x = matrix[largestRow, k];
						matrix[largestRow, k] = matrix[j, k];
						matrix[j, k] = x;
					}
				}
				/* keep track of row interchanges */
				permute[j] = largestRow;
				if (0.0 == matrix[j, j]) {
					/* avoid zeros */
					matrix[j, j] = 1e-18;
				}
				if (j != matrix_size - 1) {
					var mult = 1.0 / matrix[j, j];
					for (int i = j + 1; i != matrix_size; i++) {
						matrix[i, j] *= mult;
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
			for (i = 0; i != matrix_size; i++) {
				var row = permute[i];
				var swap = right_side[row];
				right_side[row] = right_side[i];
				right_side[i] = swap;
				if (swap != 0) {
					break;
				}
			}
			int bi = i++;
			for (; i < matrix_size; i++) {
				var row = permute[i];
				var tot = right_side[row];
				right_side[row] = right_side[i];
				/* forward substitution using the lower triangular matrix */
				for (int j = bi; j < i; j++) {
					tot -= matrix[i, j] * right_side[j];
				}
				right_side[i] = tot;
			}
			for (i = matrix_size - 1; i >= 0; i--) {
				var tot = right_side[i];
				/* back-substitution using the upper triangular matrix */
				for (int j = i + 1; j != matrix_size; j++) {
					tot -= matrix[i, j] * right_side[j];
				}
				right_side[i] = tot / matrix[i, i];
			}
		}

		static bool do_iteration() {
			for (int i = 0; i < elements.Length; i++) {
				elements[i].PrepareIteration();
			}

			for (sub_iterations = 0; sub_iterations < SUB_ITER_MAX; sub_iterations++) {
				Array.Copy(orig_right_side, right_side, matrix_size);
				for (int i = 0; i < matrix_size; i++) {
					for (int j = 0; j < matrix_size; j++) {
						matrix[i, j] = orig_matrix[i, j];
					}
				}

				converged = true;
				for (int i = 0; i < elements.Length; i++) {
					elements[i].DoIteration();
				}

				if (stopped) {
					return false;
				}

				if (converged && sub_iterations > 0) {
					break;
				}

				lu_factor();
				lu_solve();

				for (int j = 0; j < matrix_full_size; j++) {
					var ri = row_info[j];
					double res;
					if (ri.is_const) {
						res = ri.value;
					} else {
						res = right_side[ri.col];
					}
					if (double.IsNaN(res) || double.IsInfinity(res)) {
						Console.WriteLine((ri.is_const ? ("RowInfo[" + j + "]") : ("RightSide[" + ri.col + "]")) + " is NaN/infinite");
						return false;
					}
					if (j < nodes.Length - 1) {
						var cn = nodes[j + 1];
						for (int k = 0; k < cn.links.Count; k++) {
							var cl = cn.links[k];
							cl.p_elm.SetVoltage(cl.node_index, res);
						}
					} else {
						var ji = j - (nodes.Length - 1);
						voltage_sources[ji].SetCurrent(ji, res);
					}
				}
			}

			if (sub_iterations == SUB_ITER_MAX) {
				Console.WriteLine("計算が収束しませんでした");
				return false;
			}

			for (int i = 0; i < elements.Length; i++) {
				elements[i].FinishIteration();
			}

			/* calc wire currents */
			/* we removed wires from the matrix to speed things up.  in order to display wire currents,
			/* we need to calculate them now. */
			for (int i = 0; i < wires.Length; i++) {
				var wi = wires[i];
				var we = wi.p_elm;
				var cur = 0.0;
				var p = we.NodePos[wi.post];
				for (int j = 0; j < wi.neighbors.Count; j++) {
					var ce = wi.neighbors[j];
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
				if (wi.post == 0) {
					we.SetCurrent(-1, cur);
				} else {
					we.SetCurrent(-1, -cur);
				}
			}
			time += delta_time;
			return true;
		}
		#endregion

		#region stamp method
		/* stamp independent voltage source #vs, from n1 to n2, amount v */
		public static void StampVoltageSource(int n1, int n2, int vs, double v) {
			int vn = nodes.Length + vs;
			StampMatrix(vn, n1, -1);
			StampMatrix(vn, n2, 1);
			StampRightSide(vn, v);
			StampMatrix(n1, vn, 1);
			StampMatrix(n2, vn, -1);
		}

		/* use this if the amount of voltage is going to be updated in doStep(), by updateVoltageSource() */
		public static void StampVoltageSource(int n1, int n2, int vs) {
			int vn = nodes.Length + vs;
			StampMatrix(vn, n1, -1);
			StampMatrix(vn, n2, 1);
			StampRightSide(vn);
			StampMatrix(n1, vn, 1);
			StampMatrix(n2, vn, -1);
		}

		/* update voltage source in doStep() */
		public static void UpdateVoltageSource(int vs, double v) {
			int vn = nodes.Length + vs;
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
			int vn = nodes.Length + vs;
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
				if (needs_map) {
					r = row_info[r - 1].row;
					var ri = row_info[c - 1];
					if (ri.is_const) {
						right_side[r] -= x * ri.value;
						return;
					}
					c = ri.col;
				} else {
					r--;
					c--;
				}
				matrix[r, c] += x;
			}
		}

		/* stamp value x on the right side of row i, representing an
        /* independent current source flowing into node i */
		public static void StampRightSide(int i, double x) {
			if (i > 0) {
				if (needs_map) {
					i = row_info[i - 1].row;
				} else {
					i--;
				}
				right_side[i] += x;
			}
		}

		/* indicate that the value on the right side of row i changes in doStep() */
		public static void StampRightSide(int i) {
			if (i > 0) {
				row_info[i - 1].right_changes = true;
			}
		}

		/* indicate that the values on the left side of row i change in doStep() */
		public static void StampNonLinear(int i) {
			if (i > 0) {
				row_info[i - 1].left_changes = true;
			}
		}
		#endregion
	}
}
