namespace Circuit.Elements.Active {
	internal class Diode {
		int[] mNodes = new int[2];
		double mVScale;
		double mVdCoef;
		double mLeakage;
		double mVCrit;
		double mLastVdiff;

		public void SetupForDefaultModel() {
			Setup(DiodeModel.GetDefaultModel());
		}

		public void Setup(DiodeModel model) {
			mLeakage = model.SaturationCurrent;
			mVScale = model.VScale;
			mVdCoef = model.VdCoef;
			mVCrit = mVScale * Math.Log(mVScale / (Math.Sqrt(2) * mLeakage));
		}

		public void Reset() {
			mLastVdiff = 0;
		}

		public void Stamp(int n0, int n1) {
			mNodes[0] = n0;
			mNodes[1] = n1;
			Circuit.StampNonLinear(mNodes[0]);
			Circuit.StampNonLinear(mNodes[1]);
		}

		public void DoIteration(double voltDiff) {
			if (Math.Abs(voltDiff - mLastVdiff) > 0.01) {
				Circuit.Converged = false;
			}
			LimitStep(ref voltDiff);
			var gmin = mLeakage * 0.01;
			if (Circuit.SubIterations > 100) {
				gmin = Math.Exp(-9 * Math.Log(10) * (1 - Circuit.SubIterations / 3000.0));
				if (gmin > 0.1) {
					gmin = 0.1;
				}
			}
			var eval = Math.Exp(voltDiff * mVdCoef);
			var geq = mVdCoef * mLeakage * eval + gmin;
			var nc = (eval - 1) * mLeakage - geq * voltDiff;
			Circuit.StampConductance(mNodes[0], mNodes[1], geq);
			Circuit.StampCurrentSource(mNodes[0], mNodes[1], nc);
		}

		public double CalculateCurrent(double voltdiff) {
			return mLeakage * (Math.Exp(voltdiff * mVdCoef) - 1);
		}

		void LimitStep(ref double vnew) {
			if (vnew > mVCrit && Math.Abs(vnew - mLastVdiff) > (mVScale + mVScale)) {
				if (mLastVdiff > 0) {
					var arg = 1 + (vnew - mLastVdiff) / mVScale;
					if (arg > 0) {
						vnew = mLastVdiff + mVScale * Math.Log(arg);
					} else {
						vnew = mVCrit;
					}
				} else {
					vnew = mVScale * Math.Log(vnew / mVScale);
				}
				Circuit.Converged = false;
			}
			mLastVdiff = vnew;
		}
	}
}
