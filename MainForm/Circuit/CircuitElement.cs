using Circuit.Elements.Passive;

namespace Circuit {
	static class CircuitElement {
		const int ITER_LIMIT = 200;

		struct LINK {
			public int node;
			public BaseElement elm;
		}

		struct NODE {
			public int link_count;
			public LINK[] links;
		}

		struct WIRE {
			public int post;
			public int connected_count;
			public ElmWire instance;
			public BaseElement[] connected_elms;
		}

		public struct NODE_INFO {
			public int row;
			public int col;
			public bool is_const;
			public double value;
		}

		#region public variable
		public static int ITER_COUNT { get; private set; }
		public static int VOLTAGE_SOURCE_BEGIN { get; private set; }
		public static double[,] MATRIX = new double[0, 0];
		public static double[] RIGHT_SIDE = [];
		public static NODE_INFO[] NODE_INFOS = [];
		#endregion

		#region private variable
		static int m_wire_count;
		static int m_element_count;
		static int m_wave_count;
		static int m_matrix_size;
		static int m_matrix_fullsize;
		static long m_last_iter_tick = 0;
		static long m_last_frame_tick = 0;
		static NODE[] m_nodes = [];
		static WIRE[] m_wires = [];
		static BaseElement[] m_elements = [];
		static BaseElement[] m_voltage_sources = [];
		static SCOPE_WAVE[] m_waves = [];
		static double[,] m_matrix_org = new double[0, 0];
		static double[] m_right_side_org = [];
		static int[] m_permute = [];
		#endregion

		#region public method
		public static void Exec(ref bool isRunning, ref bool didAnalyze, double stepRate) {
			var tick = DateTime.Now.ToFileTimeUtc();

			if (0 == m_last_iter_tick) {
				m_last_iter_tick = tick;
				m_last_frame_tick = tick;
				return;
			}

			/* Check if we don't need to run simulation (for very slow simulation speeds).
			/* If the circuit changed, do at least one iteration to make sure everything is consistent. */
			if (1000 >= stepRate * (tick - m_last_iter_tick) && !didAnalyze) {
				m_last_frame_tick = tick;
				return;
			}

			for (int step = 1; ; step++) {
				if (!DoIteration()) {
					break;
				}
				/* Check whether enough time has elapsed to perform an *additional* iteration after
				/* those we have already completed. */
				tick = DateTime.Now.ToFileTimeUtc();
				if ((step + 1) * 1000 >= stepRate * (tick - m_last_iter_tick) || (tick - m_last_frame_tick > 250000)) {
					break;
				}
				if (!isRunning) {
					break;
				}
			}

			m_last_iter_tick = tick;
			m_last_frame_tick = DateTime.Now.ToFileTimeUtc();
		}

		public static void SetNodes(List<CircuitAnalizer.Node> nodeList, CircuitAnalizer.NodeInfo[] nodeInfo) {
			VOLTAGE_SOURCE_BEGIN = nodeList.Count - 1;
			m_nodes = new NODE[nodeList.Count];
			for (int i = 0; i < nodeList.Count; i++) {
				var ni = nodeList[i];
				m_nodes[i] = new NODE() {
					link_count = ni.Links.Count,
				};
				m_nodes[i].links = new LINK[ni.Links.Count];
				for (int j = 0; j < ni.Links.Count; j++) {
					m_nodes[i].links[j] = new LINK() {
						node = ni.Links[j].Node,
						elm = ni.Links[j].Elm
					};
				}
			}
			NODE_INFOS = new NODE_INFO[nodeInfo.Length];
			for (int i = 0; i < nodeInfo.Length; i++) {
				var ni = nodeInfo[i];
				NODE_INFOS[i] = new NODE_INFO() {
					row = ni.Row,
					col = ni.Col,
					is_const = ni.IsConst,
					value = ni.Value
				};
			}
		}

		public static void SetWires(List<CircuitAnalizer.Wire> wireList) {
			m_wire_count = wireList.Count;
			m_wires = new WIRE[m_wire_count];
			for (int i = 0; i < m_wire_count; i++) {
				var wl = wireList[i];
				var connectedCount = wl.ConnectedElms.Count;
				m_wires[i] = new WIRE() {
					post = wl.Post,
					connected_count = connectedCount,
					instance = wl.Instance
				};
				m_wires[i].connected_elms = wl.ConnectedElms.ToArray();
			}
		}

		public static void SetElements(List<BaseElement> elmList) {
			m_element_count = elmList.Count;
			m_elements = elmList.ToArray();
			m_voltage_sources = new BaseElement[elmList.Count];
			var vsCount = 0;
			for (int i = 0; i < elmList.Count; i++) {
				var elm = elmList[i];
				var ivs = elm.VoltageSourceCount;
				for (int j = 0; j < ivs; j++) {
					m_voltage_sources[vsCount] = elm;
					elm.SetVoltageSource(j, vsCount++);
				}
			}
		}

		public static void SetWaves(SCOPE_WAVE[] waves) {
			m_wave_count = waves.Length;
			m_waves = new SCOPE_WAVE[m_wave_count];
			Array.Copy(waves, m_waves, m_wave_count);
		}

		public static void SetMatrix(double[] rightSide, double[,] matrix, int size) {
			m_matrix_size = m_matrix_fullsize = size;
			m_matrix_org = new double[size, size];
			m_right_side_org = new double[size];
			m_permute = new int[size];
			RIGHT_SIDE = new double[size];
			MATRIX = new double[size, size];
			Array.Copy(rightSide, RIGHT_SIDE, size);
			Array.Copy(rightSide, m_right_side_org, size);
			Array.Copy(matrix, MATRIX, size*size);
			Array.Copy(matrix, m_matrix_org, size*size);
		}
		#endregion

		#region private method
		static void LUFactor() {
			/* use Crout's method; loop through the columns */
			for (int j = 0; j != m_matrix_size; j++) {
				/* calculate upper triangular elements for this column */
				for (int i = 0; i != j; i++) {
					var q = MATRIX[i, j];
					for (int k = 0; k != i; k++) {
						q -= MATRIX[i, k] * MATRIX[k, j];
					}
					MATRIX[i, j] = q;
				}
				/* calculate lower triangular elements for this column */
				double largest = 0;
				int largestRow = -1;
				for (int i = j; i != m_matrix_size; i++) {
					var q = MATRIX[i, j];
					for (int k = 0; k != j; k++) {
						q -= MATRIX[i, k] * MATRIX[k, j];
					}
					MATRIX[i, j] = q;
					var x = Math.Abs(q);
					if (x >= largest) {
						largest = x;
						largestRow = i;
					}
				}
				/* pivoting */
				if (j != largestRow) {
					double x;
					for (int k = 0; k != m_matrix_size; k++) {
						x = MATRIX[largestRow, k];
						MATRIX[largestRow, k] = MATRIX[j, k];
						MATRIX[j, k] = x;
					}
				}
				/* keep track of row interchanges */
				m_permute[j] = largestRow;
				if (0.0 == MATRIX[j, j]) {
					/* avoid zeros */
					MATRIX[j, j] = 1e-18;
				}
				if (j != m_matrix_size - 1) {
					var mult = 1.0 / MATRIX[j, j];
					for (int i = j + 1; i != m_matrix_size; i++) {
						MATRIX[i, j] *= mult;
					}
				}
			}
		}
		static void LUSolve() {
			int i;
			/* find first nonzero b element */
			for (i = 0; i != m_matrix_size; i++) {
				var p = m_permute[i];
				var swap = RIGHT_SIDE[p];
				RIGHT_SIDE[p] = RIGHT_SIDE[i];
				RIGHT_SIDE[i] = swap;
				if (swap != 0) {
					break;
				}
			}
			/* forward substitution using the lower triangular matrix */
			int bi = i++;
			for (; i < m_matrix_size; i++) {
				var p = m_permute[i];
				var sum = RIGHT_SIDE[p];
				RIGHT_SIDE[p] = RIGHT_SIDE[i];
				for (int j = bi; j < i; j++) {
					sum -= MATRIX[i, j] * RIGHT_SIDE[j];
				}
				RIGHT_SIDE[i] = sum;
			}
			/* back-substitution using the upper triangular matrix */
			for (i = m_matrix_size - 1; i >= 0; i--) {
				var sum = RIGHT_SIDE[i];
				for (int j = i + 1; j != m_matrix_size; j++) {
					sum -= MATRIX[i, j] * RIGHT_SIDE[j];
				}
				RIGHT_SIDE[i] = sum / MATRIX[i, i];
			}
		}
		static bool DoIteration() {
			for (int i = 0; i < m_element_count; i++) {
				m_elements[i].PrepareIteration();
			}

			for (ITER_COUNT = 0; ITER_COUNT < ITER_LIMIT; ITER_COUNT++) {
				Array.Copy(m_right_side_org, RIGHT_SIDE, m_matrix_size);
				Array.Copy(m_matrix_org, MATRIX, m_matrix_size*m_matrix_size);

				CircuitState.Converged = true;
				for (int i = 0; i < m_element_count; i++) {
					m_elements[i].DoIteration();
				}

				if (CircuitState.Stopped) {
					return false;
				}

				if (CircuitState.Converged && ITER_COUNT != 0) {
					break;
				}

				LUFactor();
				LUSolve();

				for (int idxN = 0; idxN < m_matrix_fullsize; idxN++) {
					var ni = NODE_INFOS[idxN];
					var val = ni.is_const ? ni.value : RIGHT_SIDE[ni.col];
					if (idxN < VOLTAGE_SOURCE_BEGIN) {
						var node = m_nodes[idxN + 1];
						for (int i = 0; i < node.link_count; i++) {
							var link = node.links[i];
							link.elm.SetVoltage(link.node, val);
						}
					} else {
						var idxV = idxN - VOLTAGE_SOURCE_BEGIN;
						m_voltage_sources[idxV].SetCurrent(idxV, val);
					}
				}
			}

			CircuitState.IterationCount *= 0.999;
			if (ITER_COUNT > CircuitState.IterationCount) {
				CircuitState.IterationCount = ITER_COUNT;
			}

			if (ITER_COUNT == ITER_LIMIT) {
				return false;
			}

			for (int i = 0; i < m_element_count; i++) {
				m_elements[i].FinishIteration();
			}

			for (int i = 0; i < m_wire_count; i++) {
				var wi = m_wires[i];
				var wire = wi.instance;
				var wp = wire.NodePos[wi.post];
				var curr = 0.0;
				for (int j = 0; j < wi.connected_count; j++) {
					var ce = wi.connected_elms[j];
					var n = 0;
					for (int k = 0; k != ce.TermCount; k++) {
						var cp = ce.NodePos[k];
						if (cp.X == wp.X && cp.Y == wp.Y) {
							n = k;
							break;
						}
					}
					curr += ce.GetCurrent(n);
				}
				wire.SetCurrent(-1, wi.post == 0 ? curr : -curr);
			}

			for (int i = 0; i < m_wave_count; i++) {
				var wave = m_waves[i];
				var cursor = wave.Cursor;
				var value = wave.Data[cursor];
				var v = (float)wave.Elm.GetVoltageDiff();
				if (v < value.min) {
					wave.Data[cursor].min = v;
				}
				if (v > value.max) {
					wave.Data[cursor].max = v;
				}
				if (++wave.Interval >= wave.Speed) {
					cursor = (cursor + 1) & (wave.Length - 1);
					wave.Cursor = cursor;
					wave.Interval = 0;
					wave.Data[cursor].min = wave.Data[cursor].max = v;
				}
			}

			CircuitState.Time += CircuitState.DeltaTime;
			return true;
		}
		#endregion
	}
}
