namespace Circuit.Elements.Active {
	class ElmFET : BaseElement {
		protected const int IdxG = 0;
		protected const int IdxS = 1;
		protected const int IdxD = 2;

		public int Nch;
		public bool MOS;
		public double Vth;
		public double Beta;
		public int BodyTerminal;
		public int Mode;
		public double Gm;

		public double DiodeCurrent1 { get; private set; }
		public double DiodeCurrent2 { get; private set; }
		public double Vg { get { return Volts[IdxG]; } }
		public double Vs { get { return Volts[IdxS]; } }
		public double Vd { get { return Volts[IdxD]; } }

		Diode mDiodeB1;
		Diode mDiodeB2;
		double mLastVs = 0.0;
		double mLastVd = 0.0;
		double mLastVg = 0.0;

		public ElmFET() : base() {
			mDiodeB1 = new Diode();
			mDiodeB1.SetupForDefaultModel();
			mDiodeB2 = new Diode();
			mDiodeB2.SetupForDefaultModel();
			AllocNodes();
		}

		public override int TermCount { get { return 3; } }

		public override double VoltageDiff { get { return Volts[IdxG] - Volts[IdxS]; } }

		public override void Reset() {
			Volts[IdxG] = Volts[IdxS] = Volts[IdxD] = 0;
			mLastVs = 0.0;
			mLastVd = 0.0;
			mLastVg = 0.0;
			DiodeCurrent1 = 0.0;
			DiodeCurrent2 = 0.0;
			mDiodeB1.Reset();
			mDiodeB2.Reset();
		}

		public override bool GetConnection(int n1, int n2) { return !(n1 == 0 || n2 == 0); }

		public override void Stamp() {
			CircuitElement.StampNonLinear(Nodes[IdxS]);
			CircuitElement.StampNonLinear(Nodes[IdxD]);
			BodyTerminal = (Nch < 0) ? IdxD : IdxS;
			if (MOS) {
				if (Nch < 0) {
					mDiodeB1.Stamp(Nodes[IdxS], Nodes[BodyTerminal]);
					mDiodeB2.Stamp(Nodes[IdxD], Nodes[BodyTerminal]);
				} else {
					mDiodeB1.Stamp(Nodes[BodyTerminal], Nodes[IdxS]);
					mDiodeB2.Stamp(Nodes[BodyTerminal], Nodes[IdxD]);
				}
			}
		}

		public override double GetCurrentIntoNode(int n) {
			if (n == 0) {
				return 0;
			}
			if (n == 1) {
				return Current + DiodeCurrent1;
			}
			return -Current + DiodeCurrent2;
		}

		public override void DoIteration() {
			Calc(false);
		}

		public override void IterationFinished() {
			Calc(true);
			if (BodyTerminal == IdxS) {
				DiodeCurrent1 = -DiodeCurrent2;
			}
			if (BodyTerminal == IdxD) {
				DiodeCurrent2 = -DiodeCurrent1;
			}
		}

		void Calc(bool finished) {
			var v = new double[] { Volts[0], Volts[1], Volts[2] };
			if (!finished) {
				if (v[IdxG] > mLastVg + 0.5) {
					v[IdxG] = mLastVg + 0.5;
				}
				if (v[IdxG] < mLastVg - 0.5) {
					v[IdxG] = mLastVg - 0.5;
				}
				if (v[IdxS] > mLastVs + 0.5) {
					v[IdxS] = mLastVs + 0.5;
				}
				if (v[IdxS] < mLastVs - 0.5) {
					v[IdxS] = mLastVs - 0.5;
				}
				if (v[IdxD] > mLastVd + 0.5) {
					v[IdxD] = mLastVd + 0.5;
				}
				if (v[IdxD] < mLastVd - 0.5) {
					v[IdxD] = mLastVd - 0.5;
				}
				if (CircuitElement.Converged && (nonConvergence(mLastVs, v[IdxS]) || nonConvergence(mLastVd, v[IdxD]) || nonConvergence(mLastVg, v[IdxG]))) {
					CircuitElement.Converged = false;
				}
			}
			mLastVg = v[IdxG];
			mLastVs = v[IdxS];
			mLastVd = v[IdxD];

			/* ドレインソース間電圧が負の場合
			 * ドレインとソースを入れ替える
			 * (電流の計算を単純化するため) */
			int idxS, idxD;
			if (Nch * v[IdxD] < Nch * v[IdxS]) {
				idxS = IdxD;
				idxD = IdxS;
			} else {
				idxS = IdxS;
				idxD = IdxD;
			}
			var vgs = v[IdxG] - v[idxS];
			var vds = v[idxD] - v[idxS];

			double gds;
			{
				var vgs_n = vgs * Nch;
				var vds_n = vds * Nch;
				if (vgs_n < Vth) {
					/* 遮断領域 */
					Mode = 0;
					Gm = 0;
					/* 電流を0にするべきだが特異な行列となるため
					 * 100MΩとした時の電流にする */
					gds = 1e-8;
					Current = vds_n * gds;
				} else if (vds_n < vgs_n - Vth) {
					/* 線形領域 */
					Mode = 1;
					Gm = Beta * vds_n;
					gds = Beta * (vgs_n - vds_n - Vth);
					Current = Beta * ((vgs_n - Vth) * vds_n - vds_n * vds_n * 0.5);
				} else {
					/* 飽和領域 */
					Mode = 2;
					Gm = Beta * (vgs_n - Vth);
					gds = 1e-8;
					Current = 0.5 * Beta * (vgs_n - Vth) * (vgs_n - Vth) + (vds_n - (vgs_n - Vth)) * gds;
				}
			}

			var rs = gds * vds + Gm * vgs - Current * Nch;

			/* ドレインとソースを入れ替えている場合、電流を反転 */
			if (idxS == IdxD && Nch == 1 || idxS == IdxS && Nch == -1) {
				Current = -Current;
			}

			if (MOS) {
				mDiodeB1.DoIteration(Nch * (Volts[BodyTerminal] - Volts[IdxS]));
				DiodeCurrent1 = mDiodeB1.CalculateCurrent(Nch * (Volts[BodyTerminal] - Volts[IdxS])) * Nch;
				mDiodeB2.DoIteration(Nch * (Volts[BodyTerminal] - Volts[IdxD]));
				DiodeCurrent2 = mDiodeB2.CalculateCurrent(Nch * (Volts[BodyTerminal] - Volts[IdxD])) * Nch;
			} else {
				DiodeCurrent1 = DiodeCurrent2 = 0;
			}

			CircuitElement.StampMatrix(Nodes[IdxD], Nodes[IdxD], gds);
			CircuitElement.StampMatrix(Nodes[IdxD], Nodes[IdxS], -gds - Gm);
			CircuitElement.StampMatrix(Nodes[IdxD], Nodes[IdxG], Gm);

			CircuitElement.StampMatrix(Nodes[IdxS], Nodes[IdxD], -gds);
			CircuitElement.StampMatrix(Nodes[IdxS], Nodes[IdxS], gds + Gm);
			CircuitElement.StampMatrix(Nodes[IdxS], Nodes[IdxG], -Gm);

			CircuitElement.StampRightSide(Nodes[IdxD], rs);
			CircuitElement.StampRightSide(Nodes[IdxS], -rs);
		}

		bool nonConvergence(double last, double now) {
			var diff = Math.Abs(last - now);
			if (Beta > 1) {
				// high beta MOSFETs are more sensitive to small differences, so we are more strict about convergence testing
				diff *= 100;
			}
			if (diff < 0.01) {
				// difference of less than 10mV is fine
				return false;
			}
			if (CircuitElement.SubIterations > 10 && diff < Math.Abs(now)*0.001) {
				// larger differences are fine if value is large
				return false;
			}
			if (CircuitElement.SubIterations > 100 && diff < 0.01+(CircuitElement.SubIterations-100)*0.0001) {
				// if we're having trouble converging, get more lenient
				return false;
			}
			return true;
		}
	}
}
