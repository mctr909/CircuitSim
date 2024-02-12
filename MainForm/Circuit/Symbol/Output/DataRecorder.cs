using Circuit.Elements.Output;

namespace Circuit.Symbol.Output {
	class DataRecorder : BaseSymbol {
		SaveFileDialog saveFileDialog = new SaveFileDialog();
		string mColName = "";
		PointF[] mTextPoly;
		ElmDataRecorder mElm;

		public override BaseElement Element { get { return mElm; } }

		public DataRecorder(Point pos) : base(pos) {
			mElm = new ElmDataRecorder();
		}

		public DataRecorder(Point a, Point b, int f, StringTokenizer st) : base(a, b, f) {
			mElm = new ElmDataRecorder(st);
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.DATA_RECORDER; } }

		protected override void dump(List<object> optionList) {
			optionList.Add(mElm.DataCount);
		}

		public override void SetPoints() {
			base.SetPoints();
			mLead1 = new Point();
			SetTextPos();
		}

		void SetTextPos() {
			if (string.IsNullOrWhiteSpace(mColName)) {
				ReferenceName = "CSV";
			} else {
				ReferenceName = "CSV " + mColName;
			}
			var txtSize = CustomGraphics.Instance.GetTextSize(ReferenceName);
			var txtW = txtSize.Width;
			var txtH = txtSize.Height;
			var pw = txtW / Post.Len;
			var ph = 0.5 * (txtH - 1);
			SetLead1(1);
			var p1 = new PointF();
			var p2 = new PointF();
			var p3 = new PointF();
			var p4 = new PointF();
			var p5 = new PointF();
			InterpolationPost(ref p1, 1, -ph);
			InterpolationPost(ref p2, 1, ph);
			InterpolationPost(ref p3, 1 + pw, ph);
			InterpolationPost(ref p4, 1 + pw + ph / Post.Len, 0);
			InterpolationPost(ref p5, 1 + pw, -ph);
			mTextPoly = new PointF[] {
				p1, p2, p3, p4, p5, p1
			};
			var abX = Post.B.X - Post.A.X;
			var abY = Post.B.Y - Post.A.Y;
			mTextRot = Math.Atan2(abY, abX);
			var deg = -mTextRot * 180 / Math.PI;
			if (deg < 0.0) {
				deg += 360;
			}
			if (45 * 3 <= deg && deg < 45 * 7) {
				mTextRot += Math.PI;
				InterpolationPost(ref mNamePos, 1 + 0.5 * pw, txtH / Post.Len);
			} else {
				InterpolationPost(ref mNamePos, 1 + 0.5 * pw, -txtH / Post.Len);
			}
		}

		public override void Draw(CustomGraphics g) {
			DrawLeadA();
			DrawCenteredText(ReferenceName, mNamePos, mTextRot);
			DrawPolyline(mTextPoly);
		}

		public override void GetInfo(string[] arr) {
			arr[0] = ReferenceName;
			arr[1] = "電位：" + Utils.VoltageText(mElm.Volts[0]);
			arr[2] = (mElm.DataFull ? mElm.DataCount : mElm.DataPtr) + "/" + mElm.DataCount;
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c != 0) {
				return null;
			}
			if (r == 0) {
				return new ElementInfo("サンプル数", mElm.DataCount);
			}
			if (r == 1) {
				return new ElementInfo("列名", mColName);
			}
			if (r == 2) {
				return new ElementInfo("ファイルに保存", new EventHandler((s, e) => {
					saveFileDialog.Filter = "CSVファイル(*.csv)|*.csv";
					saveFileDialog.ShowDialog();
					var filePath = saveFileDialog.FileName;
					var fs = new StreamWriter(filePath);
					fs.WriteLine(mColName + "," + ControlPanel.TimeStep);
					if (mElm.DataFull) {
						for (int i = 0; i != mElm.DataCount; i++) {
							fs.WriteLine(mElm.Data[(i + mElm.DataPtr) % mElm.DataCount]);
						}
					} else {
						for (int i = 0; i != mElm.DataPtr; i++) {
							fs.WriteLine(mElm.Data[i]);
						}
					}
					fs.Close();
					fs.Dispose();
				}));
			}
			return null;
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			if (n == 0 && 0 < ei.Value) {
				mElm.SetDataCount((int)ei.Value);
			}
			if (n == 1) {
				mColName = ei.Text;
				SetTextPos();
			}
			if (n == 2) {
				return;
			}
		}
	}
}
