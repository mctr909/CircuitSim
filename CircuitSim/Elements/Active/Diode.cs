using System;

namespace Circuit.Elements.Active {
	internal class Diode {
		const double VT = 0.025865;
		const double VZ_COEF = 1 / VT;

		int[] mNodes = new int[2];
		double mVScale;
		double mVdCoef;
		double mVz;
		double mLeakage;
		double mZOffset;
		double mVCrit;
		double mVzCrit;
		double mLastVdiff;

		public void SetupForDefaultModel() {
			Setup(DiodeModel.GetDefaultModel());
		}

		public void Setup(DiodeModel model) {
			mLeakage = model.SaturationCurrent;
			mVz = model.BreakdownVoltage;
			mVScale = model.VScale;
			mVdCoef = model.VdCoef;
			mVCrit = mVScale * Math.Log(mVScale / (Math.Sqrt(2) * mLeakage));
			mVzCrit = VT * Math.Log(VT / (Math.Sqrt(2) * mLeakage));
			if (mVz == 0) {
				mZOffset = 0;
			} else {
				var i = -0.005;
				mZOffset = mVz - Math.Log(-(1 + i / mLeakage)) / VZ_COEF;
			}
		}

		public void Reset() {
			mLastVdiff = 0;
		}

		double LimitStep(double vnew, double vold) {
			if (vnew > mVCrit && Math.Abs(vnew - vold) > (mVScale + mVScale)) {
				if (vold > 0) {
					var arg = 1 + (vnew - vold) / mVScale;
					if (arg > 0) {
						vnew = vold + mVScale * Math.Log(arg);
					} else {
						vnew = mVCrit;
					}
				} else {
					vnew = mVScale * Math.Log(vnew / mVScale);
				}
				Circuit.Converged = false;
			} else if (vnew < 0 && mZOffset != 0) {
				vnew = -vnew - mZOffset;
				vold = -vold - mZOffset;
				if (vnew > mVzCrit && Math.Abs(vnew - vold) > (VT + VT)) {
					if (vold > 0) {
						var arg = 1 + (vnew - vold) / VT;
						if (arg > 0) {
							vnew = vold + VT * Math.Log(arg);
						} else {
							vnew = mVzCrit;
						}
					} else {
						vnew = VT * Math.Log(vnew / VT);
					}
					Circuit.Converged = false;
				}
				vnew = -(vnew + mZOffset);
			}
			return vnew;
		}

		public void Stamp(int n0, int n1) {
			mNodes[0] = n0;
			mNodes[1] = n1;
			Circuit.StampNonLinear(mNodes[0]);
			Circuit.StampNonLinear(mNodes[1]);
		}

		public void DoIteration(double voltdiff) {
			if (Math.Abs(voltdiff - mLastVdiff) > 0.01) {
				Circuit.Converged = false;
			}
			voltdiff = LimitStep(voltdiff, mLastVdiff);
			mLastVdiff = voltdiff;

			var gmin = mLeakage * 0.01;
			if (Circuit.SubIterations > 100) {
				gmin = Math.Exp(-9 * Math.Log(10) * (1 - Circuit.SubIterations / 3000.0));
				if (gmin > 0.1) {
					gmin = 0.1;
				}
			}

			if (voltdiff >= 0 || mVz == 0) {
				var eval = Math.Exp(voltdiff * mVdCoef);
				var geq = mVdCoef * mLeakage * eval + gmin;
				var nc = (eval - 1) * mLeakage - geq * voltdiff;
				Circuit.StampConductance(mNodes[0], mNodes[1], geq);
				Circuit.StampCurrentSource(mNodes[0], mNodes[1], nc);
			} else {
				/*
				 * I(Vd) = Is * (exp[Vd*C] - exp[(-Vd-Vz)*Cz] - 1 )
				 *
				 * geq is I'(Vd)
				 * nc is I(Vd) + I'(Vd)*(-Vd)
				 */
				var geq = mLeakage * (mVdCoef * Math.Exp(voltdiff * mVdCoef) + VZ_COEF * Math.Exp((-voltdiff - mZOffset) * VZ_COEF)) + gmin;
				var nc = mLeakage * (Math.Exp(voltdiff * mVdCoef) - Math.Exp((-voltdiff - mZOffset) * VZ_COEF) - 1) + geq * (-voltdiff);
				Circuit.StampConductance(mNodes[0], mNodes[1], geq);
				Circuit.StampCurrentSource(mNodes[0], mNodes[1], nc);
			}
		}

		public double CalculateCurrent(double voltdiff) {
			if (voltdiff >= 0 || mVz == 0) {
				return mLeakage * (Math.Exp(voltdiff * mVdCoef) - 1);
			}
			return mLeakage * (Math.Exp(voltdiff * mVdCoef) - Math.Exp((-voltdiff - mZOffset) * VZ_COEF) - 1);
		}
	}
}
