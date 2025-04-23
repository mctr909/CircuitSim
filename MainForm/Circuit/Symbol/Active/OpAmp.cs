using Circuit.Elements.Active;
using Circuit.Elements;
using MainForm.Forms;

namespace Circuit.Symbol.Active {
	class OpAmp : BaseSymbol {
		protected const int FLAG_SWAP = 1;
		protected const int FLAG_GAIN = 8;

		const int HEIGHT = 8;
		const int WIDTH = 16;

		PointF[] mTextp;
		PointF[] mTriangle;
		Point mPosOut = new();
		PointF[] mPosIn1 = new PointF[2];
		PointF[] mPosIn2 = new PointF[2];

		public override int VoltageSourceCount { get { return 1; } }
		/* there is no current path through the op-amp inputs,
         * but there is an indirect path through the output to ground. */
		public override bool HasConnection(int n1, int n2) { return false; }
		public override bool HasGroundConnection(int nodeIndex) { return nodeIndex == 2; }

		public OpAmp(Point pos) : base(pos) {
			mFlags = FLAG_GAIN; /* need to do this before setSize() */
			Element.Para[ElmOpAmp.OUT_MAX] = 15;
			Element.Para[ElmOpAmp.OUT_MIN] = -15;
			Element.Para[ElmOpAmp.GAIN] = 1e3;
			Post.NoDiagonal = true;
		}

		public OpAmp(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			Element.Para[ElmOpAmp.OUT_MAX] = st.nextTokenDouble();
			Element.Para[ElmOpAmp.OUT_MIN] = st.nextTokenDouble();
			Element.Para[ElmOpAmp.GAIN] = st.nextTokenDouble();
			Element.V[ElmOpAmp.N] = st.nextTokenDouble();
			Element.V[ElmOpAmp.P] = st.nextTokenDouble();
			Post.NoDiagonal = true;
			SetGain();
		}

		protected override BaseElement Create() {
			return new ElmOpAmp();
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.OPAMP; } }

		protected override void dump(List<object> optionList) {
			mFlags |= FLAG_GAIN;
			optionList.Add(Element.Para[ElmOpAmp.OUT_MAX]);
			optionList.Add(Element.Para[ElmOpAmp.OUT_MIN]);
			optionList.Add(Element.Para[ElmOpAmp.GAIN]);
			optionList.Add(Element.V[ElmOpAmp.N].ToString("g3"));
			optionList.Add(Element.V[ElmOpAmp.P].ToString("g3"));
		}

		public override void Stamp() {
			var vn = CircuitAnalizer.NodeCount + Element.VoltSource;
			StampMatrix(Element.Nodes[2], vn, 1);
			StampNonLinear(vn);
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
			mTriangle = [tris[0], tris[1], mLead2];

			SetNodePos(mPosIn1[0], mPosIn2[0], mPosOut);
		}

		public override void Draw(CustomGraphics g) {
			DrawLine(mPosIn1[0], mPosIn1[1]);
			DrawLine(mPosIn2[0], mPosIn2[1]);
			DrawLine(mLead2, mPosOut);

			DrawPolygon(mTriangle);

			DrawLine(mTextp[0], mTextp[1]);
			DrawLine(mTextp[2], mTextp[3]);
			DrawLine(mTextp[4], mTextp[5]);

			UpdateDotCount(Element.I[0], ref mCurCount);
			DrawCurrent(mLead2, mPosOut, -mCurCount);
		}

		public override void GetInfo(string[] arr) {
			arr[0] = "オペアンプ";
			arr[1] = "+電源：" + TextUtils.Voltage(Element.Para[ElmOpAmp.OUT_MAX]);
			arr[2] = "-電源：" + TextUtils.Voltage(Element.Para[ElmOpAmp.OUT_MIN]);
			arr[3] = "Vin(+)：" + TextUtils.Voltage(Element.V[ElmOpAmp.P]);
			arr[4] = "Vin(-)：" + TextUtils.Voltage(Element.V[ElmOpAmp.N]);
			var vo = Math.Max(
				Math.Min(Element.V[ElmOpAmp.O], Element.Para[ElmOpAmp.OUT_MAX]),
				Element.Para[ElmOpAmp.OUT_MIN]
			);
			arr[5] = "Vout：" + TextUtils.Voltage(vo);
			arr[6] = "Iout：" + TextUtils.Current(-Element.I[0]);
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c != 0) {
				return null;
			}
			if (r == 0) {
				return new ElementInfo("+電源", Element.Para[ElmOpAmp.OUT_MAX]);
			}
			if (r == 1) {
				return new ElementInfo("-電源", Element.Para[ElmOpAmp.OUT_MIN]);
			}
			if (r == 2) {
				return new ElementInfo("ゲイン(db)", 20 * Math.Log10(Element.Para[ElmOpAmp.GAIN]));
			}
			return null;
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			if (n == 0) {
				Element.Para[ElmOpAmp.OUT_MAX] = ei.Value;
			}
			if (n == 1) {
				Element.Para[ElmOpAmp.OUT_MIN] = ei.Value;
			}
			if (n == 2 && ei.Value > 0) {
				Element.Para[ElmOpAmp.GAIN] = Math.Pow(10.0, ei.Value / 20.0);
			}
		}

		void SetGain() {
			if ((mFlags & FLAG_GAIN) != 0) {
				return;
			}
			Element.Para[ElmOpAmp.GAIN] = 1e3;
		}
	}
}
