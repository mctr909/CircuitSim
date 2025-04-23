using Circuit.Elements.Passive;
using Circuit.Symbol;

namespace Circuit.Elements {
	public abstract partial class BaseElement {
		const int IterLimit = 200;

		#region struct
		protected struct NODE_INFO {
			public int Row;
			public int Col;
			public int IsVariable;
			public bool IsConst;
			public double Value;
		}
		private struct LINK {
			public int Node;
			public BaseElement Elm;
		}
		private struct NODE {
			public int LinkCount;
			public LINK[] Links;
		}
		private struct WIRE {
			public int Direction;
			public int LinkCount;
			public ElmWire Instance;
			public LINK[] Links;
		}
		#endregion

		#region protected variable
		protected static bool CONVERGED;
		protected static int ITER_COUNT;
		protected static int VOLTAGE_SOURCE_BEGIN;
		protected static double[,] MATRIX = new double[0, 0];
		protected static double[] RIGHTSIDE = [];
		protected static NODE_INFO[] NODE_INFOS = [];
		#endregion

		#region private variable
		private static int WIRE_COUNT;
		private static int ELEM_COUNT;
		private static int WAVE_COUNT;
		private static int MATRIX_SIZE;
		private static int MATRIX_FULLSIZE;
		private static long LASTTICK_STEP = 0;
		private static long LASTTICK_FRAME = 0;
		private static NODE[] NODE_LIST = [];
		private static WIRE[] WIRE_LIST = [];
		private static BaseElement[] ELEM_LIST = [];
		private static BaseElement[] VOLTAGE_SOURCES = [];
		private static SCOPE_WAVE[] WAVE_LIST = [];
		private static double[,] MATRIX_DEFAULT = new double[0, 0];
		private static double[] RIGHTSIDE_DEFAULT = [];
		private static int[] PERMUTE = [];
		#endregion

		#region update method
		protected static void UpdateCurrentSource(int n1, int n2, double i) {
			n1 = NODE_INFOS[n1 - 1].Row;
			n2 = NODE_INFOS[n2 - 1].Row;
			RIGHTSIDE[n1] -= i;
			RIGHTSIDE[n2] += i;
		}
		protected static void UpdateVoltageSource(int vsIndex, double v) {
			vsIndex += VOLTAGE_SOURCE_BEGIN;
			vsIndex = NODE_INFOS[vsIndex].Row;
			RIGHTSIDE[vsIndex] += v;
		}
		protected static void UpdateConductance(int n1, int n2, double g) {
			var ni1 = NODE_INFOS[n1 - 1];
			var ni2 = NODE_INFOS[n2 - 1];
			n1 = ni1.Row;
			n2 = ni2.Row;
			if (ni1.IsConst) {
				RIGHTSIDE[n1] -= g * ni1.Value;
				RIGHTSIDE[n2] += g * ni1.Value;
			} else {
				MATRIX[n1, ni1.Col] += g;
				MATRIX[n2, ni1.Col] -= g;
			}
			if (ni2.IsConst) {
				RIGHTSIDE[n1] += g * ni2.Value;
				RIGHTSIDE[n2] -= g * ni2.Value;
			} else {
				MATRIX[n1, ni2.Col] -= g;
				MATRIX[n2, ni2.Col] += g;
			}
		}
		protected static void UpdateMatrix(int r, int c, double val) {
			r = NODE_INFOS[r - 1].Row;
			var nc = NODE_INFOS[c - 1];
			if (nc.IsConst) {
				RIGHTSIDE[r] -= val * nc.Value;
			} else {
				MATRIX[r, nc.Col] += val;
			}
		}
		#endregion

		#region public method
		public static void DoFrame(ref bool isRunning, ref bool didAnalyze, double stepRate) {
			var tick = DateTime.Now.ToFileTimeUtc();

			if (0 == LASTTICK_STEP) {
				LASTTICK_STEP = tick;
				LASTTICK_FRAME = tick;
				return;
			}

			/* Check if we don't need to run simulation (for very slow simulation speeds).
			/* If the circuit changed, do at least one iteration to make sure everything is consistent. */
			if (1000 >= stepRate * (tick - LASTTICK_STEP) && !didAnalyze) {
				LASTTICK_FRAME = tick;
				return;
			}

			for (int step = 1; ; step++) {
				if (!DoStep()) {
					break;
				}
				/* Check whether enough time has elapsed to perform an *additional* iteration after
				/* those we have already completed. */
				tick = DateTime.Now.ToFileTimeUtc();
				if ((step + 1) * 1000 >= stepRate * (tick - LASTTICK_STEP) || (tick - LASTTICK_FRAME > 250000)) {
					break;
				}
				if (!isRunning) {
					break;
				}
			}

			LASTTICK_STEP = tick;
			LASTTICK_FRAME = DateTime.Now.ToFileTimeUtc();
		}

		public static void SetNodeList(List<CircuitAnalizer.Node> nodeList, CircuitAnalizer.NodeInfo[] nodeInfo) {
			VOLTAGE_SOURCE_BEGIN = nodeList.Count - 1;
			NODE_LIST = new NODE[nodeList.Count];
			for (int idxN = 0; idxN != nodeList.Count; idxN++) {
				var n = nodeList[idxN];
				var links = new LINK[n.Links.Count];
				for (int idxL = 0; idxL != n.Links.Count; idxL++) {
					var nl = n.Links[idxL];
					links[idxL] = new LINK {
						Node = nl.Node,
						Elm = nl.Sym.Element
					};
				}
				NODE_LIST[idxN] = new NODE {
					LinkCount = n.Links.Count,
					Links = links
				};
			}
			NODE_INFOS = new NODE_INFO[nodeInfo.Length];
			for (int idxN = 0; idxN != nodeInfo.Length; idxN++) {
				var n = nodeInfo[idxN];
				NODE_INFOS[idxN] = new NODE_INFO {
					Row = n.Row,
					Col = n.IsConst ? 0 : n.Col,
					IsConst = n.IsConst,
					IsVariable = n.IsConst ? 0 : 1,
					Value = n.IsConst ? n.Value : 0
				};
			}
		}

		public static void SetWireList(List<CircuitAnalizer.Wire> wireList) {
			WIRE_COUNT = wireList.Count;
			WIRE_LIST = new WIRE[WIRE_COUNT];
			for (int idxW = 0; idxW != WIRE_COUNT; idxW++) {
				var w = wireList[idxW];
				var wp = w.Instance.NodePos[w.Post];
				var linkCount = w.Links.Count;
				var links = new LINK[linkCount];
				for (int idxL = 0; idxL != linkCount; idxL++) {
					var l = w.Links[idxL];
					int node = 0;
					for (int idxN = 0; idxN != l.Element.TermCount; idxN++) {
						var lp = l.NodePos[idxN];
						if (lp.X == wp.X && lp.Y == wp.Y) {
							node = idxN;
							break;
						}
					}
					links[idxL] = new LINK {
						Node = node,
						Elm = l.Element
					};
				}
				WIRE_LIST[idxW] = new WIRE {
					Direction = w.Post == 0 ? 1 : -1,
					LinkCount = linkCount,
					Instance = (ElmWire)w.Instance.Element,
					Links = links
				};
			}
		}

		public static void SetElementList(List<BaseSymbol> symList) {
			ELEM_COUNT = symList.Count;
			ELEM_LIST = new BaseElement[symList.Count];
			for (int i = 0; i != symList.Count; i++) {
				ELEM_LIST[i] = symList[i].Element;
			}
			VOLTAGE_SOURCES = new BaseElement[symList.Count];
			var vsCount = 0;
			for (int i = 0; i != symList.Count; i++) {
				var sym = symList[i];
				var ivs = sym.VoltageSourceCount;
				for (int j = 0; j != ivs; j++) {
					VOLTAGE_SOURCES[vsCount] = sym.Element;
					sym.Element.SetVoltageSource(j, vsCount++);
				}
			}
		}

		public static void SetWaveList(SCOPE_WAVE[] waves) {
			WAVE_COUNT = waves.Length;
			WAVE_LIST = new SCOPE_WAVE[WAVE_COUNT];
			Array.Copy(waves, WAVE_LIST, WAVE_COUNT);
		}

		public static void SetMatrix(double[] rightSide, double[,] matrix, int size) {
			PERMUTE = new int[size];
			MATRIX_SIZE = MATRIX_FULLSIZE = size;
			MATRIX = new double[size, size];
			MATRIX_DEFAULT = new double[size, size];
			RIGHTSIDE = new double[size];
			RIGHTSIDE_DEFAULT = new double[size];
			Array.Copy(rightSide, RIGHTSIDE, size);
			Array.Copy(rightSide, RIGHTSIDE_DEFAULT, size);
			Array.Copy(matrix, MATRIX, size * size);
			Array.Copy(matrix, MATRIX_DEFAULT, size * size);
		}
		#endregion

		#region private method
		static void LUFactor() {
			/* use Crout's method; loop through the columns */
			for (int j = 0; j != MATRIX_SIZE; j++) {
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
				for (int i = j; i != MATRIX_SIZE; i++) {
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
					for (int k = 0; k != MATRIX_SIZE; k++) {
						x = MATRIX[largestRow, k];
						MATRIX[largestRow, k] = MATRIX[j, k];
						MATRIX[j, k] = x;
					}
				}
				/* keep track of row interchanges */
				PERMUTE[j] = largestRow;
				if (0.0 == MATRIX[j, j]) {
					/* avoid zeros */
					MATRIX[j, j] = 1e-18;
				}
				if (j != MATRIX_SIZE - 1) {
					var mult = 1.0 / MATRIX[j, j];
					for (int i = j + 1; i != MATRIX_SIZE; i++) {
						MATRIX[i, j] *= mult;
					}
				}
			}
		}
		static void LUSolve() {
			int i;
			/* find first nonzero b element */
			for (i = 0; i != MATRIX_SIZE; i++) {
				var p = PERMUTE[i];
				var swap = RIGHTSIDE[p];
				RIGHTSIDE[p] = RIGHTSIDE[i];
				RIGHTSIDE[i] = swap;
				if (swap != 0) {
					break;
				}
			}
			/* forward substitution using the lower triangular matrix */
			int bi = i++;
			for (; i < MATRIX_SIZE; i++) {
				var p = PERMUTE[i];
				var sum = RIGHTSIDE[p];
				RIGHTSIDE[p] = RIGHTSIDE[i];
				for (int j = bi; j < i; j++) {
					sum -= MATRIX[i, j] * RIGHTSIDE[j];
				}
				RIGHTSIDE[i] = sum;
			}
			/* back-substitution using the upper triangular matrix */
			for (i = MATRIX_SIZE - 1; i >= 0; i--) {
				var sum = RIGHTSIDE[i];
				for (int j = i + 1; j != MATRIX_SIZE; j++) {
					sum -= MATRIX[i, j] * RIGHTSIDE[j];
				}
				RIGHTSIDE[i] = sum / MATRIX[i, i];
			}
		}
		static bool DoStep() {
			for (int idxE = 0; idxE < ELEM_COUNT; idxE++) {
				ELEM_LIST[idxE].StartIteration();
			}

			for (int i = 0; i < IterLimit; i++) {
				Array.Copy(RIGHTSIDE_DEFAULT, RIGHTSIDE, MATRIX_SIZE);
				Array.Copy(MATRIX_DEFAULT, MATRIX, MATRIX_SIZE * MATRIX_SIZE);

				ITER_COUNT = i;
				CONVERGED = true;
				for (int idxE = 0; idxE < ELEM_COUNT; idxE++) {
					ELEM_LIST[idxE].DoIteration();
				}

				if (CircuitState.Stopped) {
					return false;
				}

				if (CONVERGED && i != 0) {
					break;
				}

				LUFactor();
				LUSolve();

				for (int idxN = 0; idxN < VOLTAGE_SOURCE_BEGIN; idxN++) {
					var ni = NODE_INFOS[idxN];
					var volt = ni.IsConst ? ni.Value : RIGHTSIDE[ni.Col];
					var node = NODE_LIST[idxN + 1];
					var linkCount = node.LinkCount;
					for (int idxL = 0; idxL < linkCount; idxL++) {
						var link = node.Links[idxL];
						link.Elm.SetVoltage(link.Node, volt);
					}
				}

				for (int idxN = VOLTAGE_SOURCE_BEGIN, idxV = 0; idxN < MATRIX_FULLSIZE; idxN++, idxV++) {
					var ni = NODE_INFOS[idxN];
					var current = ni.IsConst ? ni.Value : RIGHTSIDE[ni.Col];
					VOLTAGE_SOURCES[idxV].SetCurrent(idxV, current);
				}
			}

			if (ITER_COUNT == IterLimit) {
				return false;
			}

			for (int idxE = 0; idxE < ELEM_COUNT; idxE++) {
				ELEM_LIST[idxE].FinishIteration();
			}

			for (int idxW = 0; idxW < WIRE_COUNT; idxW++) {
				var wire = WIRE_LIST[idxW];
				var current = 0.0;
				for (int idxL = 0; idxL < wire.LinkCount; idxL++) {
					var link = wire.Links[idxL];
					current += link.Elm.GetCurrent(link.Node);
				}
				wire.Instance.SetCurrent(-1, wire.Direction * current);
			}

			for (int idxW = 0; idxW < WAVE_COUNT; idxW++) {
				var wave = WAVE_LIST[idxW];
				var cursor = wave.Cursor;
				var value = wave.Values[cursor];
				var volt = (float)wave.Elm.VoltageDiff;
				if (volt < value.Min) {
					wave.Values[cursor].Min = volt;
				}
				if (volt > value.Max) {
					wave.Values[cursor].Max = volt;
				}
				if (++wave.Interval >= wave.Speed) {
					cursor = (cursor + 1) & (wave.Length - 1);
					wave.Cursor = cursor;
					wave.Interval = 0;
					wave.Values[cursor].Min = wave.Values[cursor].Max = volt;
				}
			}

			CircuitState.Time += CircuitState.DeltaTime;
			return true;
		}
		#endregion
	}
}
