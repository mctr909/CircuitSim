﻿using Circuit.Forms;
using Circuit.Elements.Active;

namespace Circuit.Symbol.Active {
	class OpAmp : BaseSymbol {
		protected const int FLAG_SWAP = 1;
		protected const int FLAG_GAIN = 8;

		const int HEIGHT = 8;
		const int WIDTH = 16;

		ElmOpAmp mElm;

		PointF[] mTextp;
		PointF[] mTriangle;
		Point mPosOut = new();
		PointF[] mPosIn1 = new PointF[2];
		PointF[] mPosIn2 = new PointF[2];

		public override BaseElement Element { get { return mElm; } }

		public OpAmp(Point pos) : base(pos) {
			Post.NoDiagonal = true;
			mFlags = FLAG_GAIN; /* need to do this before setSize() */
			mElm = new ElmOpAmp();
		}

		public OpAmp(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			mElm = new ElmOpAmp {
				MaxOut = st.nextTokenDouble(),
				MinOut = st.nextTokenDouble(),
				Gain = st.nextTokenDouble()
			};
			mElm.NodeVolts[ElmOpAmp.V_N] = st.nextTokenDouble();
			mElm.NodeVolts[ElmOpAmp.V_P] = st.nextTokenDouble();
			Post.NoDiagonal = true;
			SetGain();
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.OPAMP; } }

		protected override void dump(List<object> optionList) {
			mFlags |= FLAG_GAIN;
			optionList.Add(mElm.MaxOut);
			optionList.Add(mElm.MinOut);
			optionList.Add(mElm.Gain);
			optionList.Add(mElm.NodeVolts[ElmOpAmp.V_N].ToString("g3"));
			optionList.Add(mElm.NodeVolts[ElmOpAmp.V_P].ToString("g3"));
		}

		public override void SetPoints() {
			base.SetPoints();
			int ww = WIDTH;
			if (ww > Post.Len / 2) {
				ww = (int)(Post.Len / 2);
			}
			SetLeads(ww * 2);
			int hs = HEIGHT * Post.Dsign;
			if ((mFlags & FLAG_SWAP) != 0) {
				hs = -hs;
			}
			InterpolationPostAB(ref mPosIn1[0], ref mPosIn2[0], 0, hs);
			InterpolationLeadAB(ref mPosIn1[1], ref mPosIn2[1], 0, hs);
			mPosOut = Post.B;

			var signp = new PointF[2];
			InterpolationLeadAB(ref signp[0], ref signp[1], 0.2, hs);

			mTextp = [
				new PointF(signp[0].X - 3, signp[0].Y),
				new PointF(signp[0].X + 3, signp[0].Y),
				new PointF(signp[1].X - 3, signp[1].Y),
				new PointF(signp[1].X + 3, signp[1].Y),
				new PointF(signp[1].X, signp[1].Y - 3),
				new PointF(signp[1].X, signp[1].Y + 3)
			];

			var tris = new PointF[2];
			InterpolationLeadAB(ref tris[0], ref tris[1], 0, hs * 2);
			mTriangle = new PointF[] { tris[0], tris[1], mLead2 };

			mElm.SetNodePos(mPosIn1[0], mPosIn2[0], mPosOut);
		}

		public override void Draw(CustomGraphics g) {
			DrawLine(mPosIn1[0], mPosIn1[1]);
			DrawLine(mPosIn2[0], mPosIn2[1]);
			DrawLine(mLead2, mPosOut);

			DrawPolygon(mTriangle);

			DrawLine(mTextp[0], mTextp[1]);
			DrawLine(mTextp[2], mTextp[3]);
			DrawLine(mTextp[4], mTextp[5]);

			UpdateDotCount(mElm.Current, ref mCurCount);
			DrawCurrent(mLead2, mPosOut, -mCurCount);
		}

		public override void GetInfo(string[] arr) {
			arr[0] = "オペアンプ";
			arr[1] = "+電源：" + TextUtils.Voltage(mElm.MaxOut);
			arr[2] = "-電源：" + TextUtils.Voltage(mElm.MinOut);
			arr[3] = "Vin(+)：" + TextUtils.Voltage(mElm.NodeVolts[ElmOpAmp.V_P]);
			arr[4] = "Vin(-)：" + TextUtils.Voltage(mElm.NodeVolts[ElmOpAmp.V_N]);
			var vo = Math.Max(Math.Min(mElm.NodeVolts[ElmOpAmp.V_O], mElm.MaxOut), mElm.MinOut);
			arr[5] = "Vout：" + TextUtils.Voltage(vo);
			arr[6] = "Iout：" + TextUtils.Current(-mElm.Current);
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c != 0) {
				return null;
			}
			if (r == 0) {
				return new ElementInfo("+電源", mElm.MaxOut);
			}
			if (r == 1) {
				return new ElementInfo("-電源", mElm.MinOut);
			}
			if (r == 2) {
				return new ElementInfo("ゲイン(db)", 20 * Math.Log10(mElm.Gain));
			}
			return null;
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			if (n == 0) {
				mElm.MaxOut = ei.Value;
			}
			if (n == 1) {
				mElm.MinOut = ei.Value;
			}
			if (n == 2 && ei.Value > 0) {
				mElm.Gain = Math.Pow(10.0, ei.Value / 20.0);
			}
		}

		void SetGain() {
			if ((mFlags & FLAG_GAIN) != 0) {
				return;
			}
			mElm.Gain = 1000;
		}
	}
}
