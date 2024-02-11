namespace Circuit.Elements.Active {
	class ElmFET : BaseElement {
		protected const int IdxG = 0;
		protected const int IdxS = 1;
		protected const int IdxD = 2;

		public const double DefaultBeta = 1;

		public double Vth;
		public double Beta;

		public int Nch { get; private set; }
		public bool MOS { get; private set; }
		public int BodyTerminal { get; private set; }
		public int Mode { get; private set; } = 0;
		public double Gm { get; private set; } = 0;

		public double DiodeCurrent1 { get; private set; }
		public double DiodeCurrent2 { get; private set; }
		public double Vg { get { return Volts[IdxG]; } }
		public double Vs { get { return Volts[IdxS]; } }
		public double Vd { get { return Volts[IdxD]; } }

		Diode mDiodeB1;
		Diode mDiodeB2;
		double mLastVs = 0.0;
		double mLastVd = 0.0;

		public ElmFET(bool isNch, bool mos, double vth, double beta) : base() {
			Nch = isNch ? 1 : -1;
			MOS = mos;
			Vth = vth;
			Beta = beta;
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
			DiodeCurrent1 = 0.0;
			DiodeCurrent2 = 0.0;
			mDiodeB1.Reset();
			mDiodeB2.Reset();
		}

		public override bool GetConnection(int n1, int n2) { return !(n1 == 0 || n2 == 0); }

		public override void Stamp() {
			Circuit.StampNonLinear(Nodes[IdxS]);
			Circuit.StampNonLinear(Nodes[IdxD]);
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
			var vs = Volts[IdxS];
			var vd = Volts[IdxD];
			if (!finished) {
				if (vs > mLastVs + 0.5) {
					vs = mLastVs + 0.5;
				}
				if (vs < mLastVs - 0.5) {
					vs = mLastVs - 0.5;
				}
				if (vd > mLastVd + 0.5) {
					vd = mLastVd + 0.5;
				}
				if (vd < mLastVd - 0.5) {
					vd = mLastVd - 0.5;
				}
				Volts[IdxS] = vs;
				Volts[IdxD] = vd;
			}

			/* ドレインソース間電圧が負の場合
			 * ドレインとソースを入れ替える
			 * (電流の計算を単純化するため) */
			int idxS, idxD;
			if (Nch * Volts[IdxD] < Nch * Volts[IdxS]) {
				idxS = IdxD;
				idxD = IdxS;
			} else {
				idxS = IdxS;
				idxD = IdxD;
			}

			var real_vgs = Volts[IdxG] - Volts[idxS];
			var real_vds = Volts[idxD] - Volts[idxS];
			mLastVs = vs;
			mLastVd = vd;

			double gds;
			{
				var vgs = real_vgs * Nch;
				var vds = real_vds * Nch;
				if (vgs < Vth) {
					/* 遮断領域 */
					/* 電流を0にするべきだが特異な行列となるため
					 * 100MΩとした時の電流にする */
					gds = 1e-8;
					Current = vds * gds;
					Gm = 0;
					Mode = 0;
				} else if (vds < vgs - Vth) {
					/* 線形領域 */
					gds = Beta * (vgs - vds - Vth);
					Current = Beta * ((vgs - Vth) * vds - vds * vds * 0.5);
					Gm = Beta * vds;
					Mode = 1;
				} else {
					/* 飽和領域 */
					gds = 1e-8;
					Current = 0.5 * Beta * (vgs - Vth) * (vgs - Vth) + (vds - (vgs - Vth)) * gds;
					Gm = Beta * (vgs - Vth);
					Mode = 2;
				}
			}

			var rs = gds * real_vds + Gm * real_vgs - Nch * Current;

			/* ドレインソース間電圧が負の場合
			 * ドレインとソースを入れ替えているため電流を反転 */
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

			Circuit.StampMatrix(Nodes[IdxD], Nodes[IdxD], gds);
			Circuit.StampMatrix(Nodes[IdxD], Nodes[IdxS], -gds - Gm);
			Circuit.StampMatrix(Nodes[IdxD], Nodes[IdxG], Gm);

			Circuit.StampMatrix(Nodes[IdxS], Nodes[IdxD], -gds);
			Circuit.StampMatrix(Nodes[IdxS], Nodes[IdxS], gds + Gm);
			Circuit.StampMatrix(Nodes[IdxS], Nodes[IdxG], -Gm);

			Circuit.StampRightSide(Nodes[IdxD], rs);
			Circuit.StampRightSide(Nodes[IdxS], -rs);
		}
	}
}
