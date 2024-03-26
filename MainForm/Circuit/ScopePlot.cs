namespace Circuit {
	public class ScopePlot {
		const int SCALE_INFO_WIDTH = 45;
		const int SPEED_MAX = 1024;
		const double SCALE_MIN = 1e-9;
		const double FFT_RANGE = 60.0;
		static readonly Color[] COLORS = {
			Color.FromArgb(0xCF, 0x00, 0x00), //RED,
			Color.FromArgb(0x00, 0xCF, 0x00), //GREEN,
			Color.FromArgb(0x1F, 0x1F, 0xEF), //BLUE,
			Color.FromArgb(0xBF, 0x00, 0xBF), //PURPLE,
			Color.FromArgb(0xFF, 0x00, 0x8F), //MAGENTA,
			Color.FromArgb(0x00, 0xBF, 0xBF), //CYAN,
			Color.FromArgb(0xBF, 0xBF, 0x00), //YELLOW,
			Color.FromArgb(0xA0, 0xA0, 0xA0), //GRAY
			Color.FromArgb(0x5F, 0x5F, 0x5F)  //not select
		};

		public enum E_COLOR : int {
			RED,
			GREEN,
			BLUE,
			PURPLE,
			MAGENTA,
			CYAN,
			YELLOW,
			GRAY,
			COUNT
		}

		int mFlags {
			set {
				ShowVoltage = (value & 2) != 0;
				ShowFreq = (value & 8) != 0;
				ManualScale = (value & 16) != 0;
				ShowScale = (value & 512) != 0;
				ShowFFT = (value & 1024) != 0;
				Normarize = (value & 8192) != 0;
				ShowRMS = (value & 16384) != 0;
				LogSpectrum = (value & 65536) != 0;
			}
			get {
				return (ShowVoltage ? 2 : 0)
					| (ShowFreq ? 8 : 0)
					| (ManualScale ? 16 : 0)
					| (ShowScale ? 512 : 0)
					| (ShowFFT ? 1024 : 0)
					| (Normarize ? 8192 : 0)
					| (ShowRMS ? 16384 : 0)
					| (LogSpectrum ? 65536 : 0);
			}
		}

		#region [private variable]
		CustomGraphics mContext;
		List<ScopeWave> mWaves = [];
		FFT mFft = new(16);
		double[] mReal = new double[16];
		double[] mImag = new double[16];
		double mFftMax = 0.0;
		double mFftMainMax;
		Rectangle mFFTBoundingBox;
		bool mShowNegative;
		bool mSomethingSelected;
		double mGridDivX;
		double mGridStepX;
		double mGridStepY;
		double mScopeTimeStep;
		double mMainGridMult;
		double mMainGridMid;
		double mMaxValue;
		double mMinValue;
		int mWaveLength;
		#endregion

		#region [public property]
		public int MouseCursorX { get; set; } = -1;
		public int MouseCursorY { get; set; } = -1;
		public Rectangle BoundingBox { get; private set; } = new Rectangle(0, 0, 1, 1);
		public int Right { get { return BoundingBox.X + BoundingBox.Width; } }
		public int Index { get; set; } = 0;
		public int StackCount { get; set; } = 0;
		public int WaveCount { get { return mWaves.Count; } }
		public int SelectedWave { get; set; } = -1;
		public bool CanMenu { get { return mWaves[0].Symbol != null; } }
		public bool NeedToRemove {
			get {
				bool ret = true;
				for (int i = 0; i != mWaves.Count; i++) {
					var plot = mWaves[i];
					if (CircuitSymbol.List.Contains(plot.Symbol)) {
						ret = false;
					} else {
						mWaves.RemoveAt(i);
						i--;
					}
				}
				return ret;
			}
		}

		public int Speed { get; private set; } = 64;
		public double Scale { get; private set; } = SCALE_MIN;
		public bool Normarize { get; private set; } = true;
		public bool ManualScale { get; set; }
		public bool ShowScale { get; set; }
		public bool ShowRMS { get; set; }
		public bool ShowFreq { get; set; }
		public bool ShowVoltage { get; private set; }
		public bool ShowFFT { get; set; }
		public bool LogSpectrum { get; set; }
		public string Text { get; set; }
		#endregion

		public ScopePlot() {
			AllocImage();
			Initialize();
		}

		#region [get/set method]
		public BaseSymbol GetSelectedSymbol() {
			if (0 <= SelectedWave && SelectedWave < mWaves.Count) {
				return mWaves[SelectedWave].Symbol;
			}
			return 0 < mWaves.Count ? mWaves[0].Symbol : null;
		}
		public void SetRect(Rectangle rect) {
			int w = BoundingBox.Width;
			rect.Width -= SCALE_INFO_WIDTH;
			BoundingBox = rect;
			if (BoundingBox.Width != w) {
				ResetGraph();
			}
			mFFTBoundingBox = new Rectangle(40, 0, rect.Width - 40, rect.Height - 16);
		}
		public void SetSpeed(int speed) {
			if (speed < 1) {
				speed = 1;
			}
			if (1024 < speed) {
				speed = 1024;
			}
			Speed = speed;
			ResetGraph();
		}
		public void SetScale(double scale) {
			if (mWaves.Count == 0) {
				return;
			}
			Scale = Math.Max(SCALE_MIN, scale);
		}
		public void SetShowVoltage(bool show) {
			if (show && !ShowVoltage) {
				SetPlot();
			}
			ShowVoltage = show;
		}
		#endregion

		#region [public method]
		public string Dump() {
			var vPlot = mWaves[0];
			if (vPlot.Symbol == null) {
				return null;
			}
			var dumpList = new List<object>() {
				"o",
				vPlot.Speed,
				mFlags,
				Scale.ToString("g3"),
				Index,
				mWaves.Count
			};
			foreach (var p in mWaves) {
				dumpList.Add(CircuitSymbol.List.IndexOf(p.Symbol) + "_" + (E_COLOR)p.Color);
			}
			if (!string.IsNullOrWhiteSpace(Text)) {
				dumpList.Add(TextUtils.Escape(Text));
			}
			return string.Join(" ", dumpList.ToArray());
		}
		public void Undump(StringTokenizer st) {
			Initialize();
			mWaves = new List<ScopeWave>();

			Speed = st.nextTokenInt(1);
			ResetGraph();

			mFlags = st.nextTokenInt();
			Scale = st.nextTokenDouble();
			Index = st.nextTokenInt();

			try {
				var plotCount = st.nextTokenInt();
				for (int i = 0; i != plotCount; i++) {
					string temp;
					st.nextToken(out temp);
					var subElmCol = temp.Split('_');
					var subElmIdx = int.Parse(subElmCol[0]);
					var subElm = CircuitSymbol.List[subElmIdx];
					var p = new ScopeWave(subElm) {
						Speed = Speed
					};
					SetColor(p, Enum.Parse<E_COLOR>(subElmCol[1]));
					mWaves.Add(p);
				}
			} catch (Exception ex) {
				Console.WriteLine(ex.Message);
				Console.WriteLine(ex.StackTrace);
				throw ex;
			}

			if (st.HasMoreTokens) {
				string temp;
				st.nextToken(out temp);
				Text = TextUtils.UnEscape(temp);
			} else {
				Text = "";
			}
		}
		public void Setup(BaseSymbol ui) {
			SetPlot(ui);
			Initialize();
		}
		public double CalcGridTime() {
			var baseT = 10 * CircuitElement.delta_time * Speed;
			mGridStepX = 1e-9;
			mGridDivX = 10;
			for (int i = 0; mGridStepX < baseT; i++) {
				var m = i % 2;
				var exp = Math.Pow(10, (i - m) / 2);
				switch (m) {
				case 0:
					mGridStepX = 1e-9 * exp;
					mGridDivX = 10;
					break;
				case 1:
					mGridStepX = 2e-9 * exp;
					mGridDivX = 5;
					break;
				}
			}
			return mGridStepX;
		}
		public void Combine(ScopePlot plot) {
			foreach (var wave in plot.mWaves) {
				SetColor(wave, E_COLOR.GREEN);
				mWaves.Add(wave);
			}
			plot.mWaves.Clear();
		}
		public void SpeedUp() {
			if (1 < Speed) {
				Speed >>= 1;
				ResetGraph();
			}
		}
		public void SlowDown() {
			if (Speed < SPEED_MAX) {
				Speed <<= 1;
			}
			ResetGraph();
		}
		public void MaxScale() {
			Normarize = !Normarize;
			mShowNegative = false;
		}
		public void Remove(int waveIndex) {
			if (waveIndex < 0 || mWaves.Count <= waveIndex) {
				return;
			}
			var p = mWaves[waveIndex];
			mWaves.Remove(p);
		}
		public void ResetGraph(bool full = false) {
			mWaveLength = 1;
			while (mWaveLength <= BoundingBox.Width) {
				mWaveLength <<= 1;
			}
			if (mWaves == null) {
				mWaves = new List<ScopeWave>();
			}
			mShowNegative = false;
			for (int i = 0; i != mWaves.Count; i++) {
				var p = mWaves[i];
				p.Reset(mWaveLength, Speed, full);
				if (p.Color >= (int)E_COLOR.COUNT) {
					SetColor(p, E_COLOR.GREEN);
				}
			}
			mScopeTimeStep = CircuitElement.delta_time;
			AllocImage();
		}
		public void SetColor(int waveIndex, int colorIndex) {
			mWaves[waveIndex].Color = colorIndex % (int)E_COLOR.COUNT;
		}
		public void SetColor(ScopeWave wave, E_COLOR color) {
			wave.Color = (int)color;
		}
		public int GetSelectedWaveColor() {
			return mWaves[SelectedWave].Color;
		}
		public void TimeStep() {
			foreach (var wave in mWaves) {
				wave.TimeStep();
			}
		}
		public void Draw(CustomGraphics g, bool isFloat = false) {
			if (mWaves.Count == 0) {
				return;
			}

			/* reset if timestep changed */
			if (mScopeTimeStep != CircuitElement.delta_time) {
				mScopeTimeStep = CircuitElement.delta_time;
				ResetGraph();
			}

			if (Normarize) {
				Scale = SCALE_MIN;
			}

			mSomethingSelected = false;
			foreach (var p in mWaves) {
				CalcScale(p);
				if (p.Symbol != null && p.Symbol.IsMouseElm) {
					mSomethingSelected = true;
				}
			}

			SelectWave();
			if (SelectedWave >= 0) {
				mSomethingSelected = true;
			}

			if (mWaves.Count > 0) {
				CalcMinMax();
			}

			if (isFloat) {
				g.SetPlotPos(BoundingBox.Location);
				g.DrawRectangle(new Rectangle(0, 0, BoundingBox.Width, BoundingBox.Height));
			} else {
				g.SetPlotPos(BoundingBox.Location);
			}

			{
				if (ShowFFT) {
					DrawFFTGridLines(g);
					for (int i = 0; i != mWaves.Count; i++) {
						DrawFFT(g, i);
					}
				}
				if (ShowVoltage) {
					/* Vertical (T) gridlines */
					CalcGridTime();
					/* draw volts on top (last), then current underneath, then everything else */
					for (int i = 0; i != mWaves.Count; i++) {
						DrawWave(g, i);
					}
				}
				if (mWaves.Count > 0) {
					DrawInfoTexts(g);
				}
			}
			g.ClearTransform();

			DrawCrosshairs(g);

			if (5 < mWaves[0].Index && (!ManualScale || Normarize)) {
				if (SCALE_MIN < Scale) {
					Scale /= 2;
				}
			}
		}
		#endregion

		#region [private method]
		void AllocImage() {
			if (mContext == null) {
				mContext = CustomGraphics.FromImage(BoundingBox.Width, BoundingBox.Height);
			}
		}
		void Initialize() {
			ResetGraph();
			Scale = 0.1;
			Speed = 64;
			ShowVoltage = true;
			ShowScale = ShowFreq = ManualScale = ShowFFT = false;
		}
		void SetPlot(BaseSymbol symbol) {
			if (null == symbol) {
				mWaves.Clear();
				return;
			}
			mWaves = new List<ScopeWave>() { new(symbol) };
			ShowVoltage = true;
			ResetGraph();
		}
		void SetPlot() {
			if (mWaves.Count == 0) {
				return;
			}
			var symbolList = new List<BaseSymbol>();
			foreach (var wave in mWaves) {
				if (null == wave.Symbol) {
					continue;
				}
				symbolList.Add(wave.Symbol);
			}
			mWaves.Clear();
			foreach (var symbol in symbolList) {
				mWaves.Add(new ScopeWave(symbol));
			}
			ShowVoltage = true;
			ResetGraph();
		}
		void SelectWave() {
			if (!BoundingBox.Contains(MouseCursorX, MouseCursorY)) {
				SelectedWave = -1;
				return;
			}
			if (ShowFFT) {
				SelectedWave = 0;
				return;
			}
			var height = (BoundingBox.Height - 1) / 2;
			var ofsY = BoundingBox.Y + height;
			var startIndex = GetStartIndex(mWaves[0]);
			var index = (MouseCursorX - BoundingBox.X + startIndex) & (mWaveLength - 1);
			var bestDist = double.MaxValue;
			int bestWave = -1;
			for (int i = 0; i != mWaves.Count; i++) {
				var limitVy = mMainGridMult * (mMainGridMid - mWaves[i].MaxValues[index]);
				limitVy = Math.Max(-height, limitVy);
				limitVy = Math.Min(height, limitVy);
				var dist = Math.Abs(MouseCursorY - (ofsY + limitVy));
				if (dist < bestDist) {
					bestDist = dist;
					bestWave = i;
				}
			}
			SelectedWave = bestWave;
		}
		Color GetColor(ScopeWave wave) {
			return COLORS[wave.Color];
		}
		int GetStartIndex(ScopeWave wave) {
			return wave.Index + wave.Length - BoundingBox.Width;
		}
		#endregion

		#region [calcuration method]
		void CalcMinMax() {
			mMinValue = double.MaxValue;
			mMaxValue = double.MinValue;
			for (int si = 0; si != mWaves.Count; si++) {
				var wave = mWaves[si];
				var startIndex = GetStartIndex(wave);
				var minV = wave.MinValues;
				var maxV = wave.MaxValues;
				for (int i = 0; i != BoundingBox.Width; i++) {
					var index = (i + startIndex) & (mWaveLength - 1);
					if (minV[index] < mMinValue) {
						mMinValue = minV[index];
					}
					if (mMaxValue < maxV[index]) {
						mMaxValue = maxV[index];
					}
				}
			}
		}
		void CalcScale(ScopeWave wave) {
			if (ManualScale && !Normarize) {
				return;
			}
			var startIndex = GetStartIndex(wave);
			var minV = wave.MinValues;
			var maxV = wave.MaxValues;
			var max = 0.0;
			var gridMax = Scale;
			for (int i = 0; i != BoundingBox.Width; i++) {
				var index = (i + startIndex) & (mWaveLength - 1);
				if (minV[index] < -max) {
					max = -minV[index];
				}
				if (max < maxV[index]) {
					max = maxV[index];
				}
			}
			if (Normarize) {
				gridMax = Math.Max(max, gridMax);
			} else {
				int scale = 1;
				gridMax = SCALE_MIN;
				while (gridMax < max) {
					switch (scale) {
					case 1:
						gridMax *= 2;
						scale = 2;
						break;
					case 2:
						gridMax *= 2.5;
						scale = 5;
						break;
					case 5:
						gridMax *= 2;
						scale = 1;
						break;
					}
				}
				if (mShowNegative) {
					gridMax *= 2;
				}
			}
			Scale = gridMax;
		}
		string CalcRMS() {
			var wave = mWaves[0];
			var beginIndex = wave.Index + mWaveLength - BoundingBox.Width;
			var maxV = wave.MaxValues;
			var minV = wave.MinValues;
			var mid = (mMaxValue + mMinValue) / 2;

			var state = -1;
			int skipZeroIndex;
			for (skipZeroIndex = 0; skipZeroIndex != BoundingBox.Width; skipZeroIndex++) {
				var index = (skipZeroIndex + beginIndex) & (mWaveLength - 1);
				if (maxV[index] != 0) {
					if (mid < maxV[index]) {
						state = 1;
					}
					break;
				}
			}

			var firstState = -state;
			var start = skipZeroIndex;
			var end = 0;
			var cycleCount = 0;
			var sum = 0.0;
			var endSum = 0.0;
			for (; skipZeroIndex != BoundingBox.Width; skipZeroIndex++) {
				var index = (skipZeroIndex + beginIndex) & (mWaveLength - 1);
				var sw = false;
				if (state == 1) {
					if (maxV[index] < mid) {
						sw = true;
					}
				} else if (minV[index] > mid) {
					sw = true;
				}
				if (sw) {
					state = -state;
					if (firstState == state) {
						if (cycleCount == 0) {
							start = skipZeroIndex;
							firstState = state;
							sum = 0.0;
						}
						cycleCount++;
						end = skipZeroIndex;
						endSum = sum;
					}
				}
				if (0 < cycleCount) {
					var m = (maxV[index] + minV[index]) * 0.5;
					sum += m * m;
				}
			}
			if (1 < cycleCount) {
				var rms = Math.Sqrt(endSum / (end - start));
				return TextUtils.Voltage(rms) + "rms";
			} else {
				return "";
			}
		}
		string CalcFrequency() {
			/* try to get frequency get average */
			int posX;
			var wave = mWaves[0];
			var startIndex = wave.Index + mWaveLength - BoundingBox.Width;
			var minV = wave.MinValues;
			var maxV = wave.MaxValues;
			var avg = 0.0;
			for (posX = 0; posX != BoundingBox.Width; posX++) {
				int index = (posX + startIndex) & (mWaveLength - 1);
				avg += minV[index] + maxV[index];
			}
			avg /= posX * 2;

			/* count period lengths */
			var state = 0;
			var thresh = avg * 0.05;
			var oldPosX = 0;
			var periodct = -1;
			var avperiod = 0.0;
			var avperiod2 = 0.0;
			for (posX = 0; posX != BoundingBox.Width; posX++) {
				var index = (posX + startIndex) & (mWaveLength - 1);
				var dcCut = maxV[index] - avg;
				var lastState = state;
				if (dcCut < thresh) {
					state = 1;
				} else if (dcCut > -thresh) {
					state = 2;
				}
				if (state == 2 && lastState == 1) {
					var pd = posX - oldPosX;
					oldPosX = posX;
					/* short periods can't be counted properly */
					if (pd < 16) {
						continue;
					}
					/* skip first period, it might be too short */
					if (periodct >= 0) {
						avperiod += pd;
						avperiod2 += pd * pd;
					}
					periodct++;
				}
			}
			avperiod /= periodct;
			avperiod2 /= periodct;
			var periodstd = Math.Sqrt(avperiod2 - avperiod * avperiod);
			/* don't show freq if standard deviation is too great */
			if (periodct < 1 || 2 < periodstd) {
				return "";
			}
			var freq = 1.0 / (avperiod * CircuitElement.delta_time * Speed);
			return TextUtils.Frequency(freq);
		}
		#endregion

		#region [draw method]
		void DrawCrosshairs(CustomGraphics g) {
			if (!BoundingBox.Contains(MouseCursorX, MouseCursorY)) {
				return;
			}
			if (SelectedWave < 0 && !ShowFFT) {
				return;
			}

			var info = new string[4];
			int ct = 0;

			if (ShowVoltage) {
				int maxy = (BoundingBox.Height - 1) / 2;
				int ipa = GetStartIndex(mWaves[0]);
				int pointer = (MouseCursorX - BoundingBox.X + ipa) & (mWaveLength - 1);
				if (SelectedWave >= 0) {
					var wave = mWaves[SelectedWave];
					info[ct++] = TextUtils.Voltage(wave.MaxValues[pointer]);
					var maxvy = (int)(mMainGridMult * (wave.MaxValues[pointer] - mMainGridMid));
					maxvy = Math.Max(-maxy, maxvy);
					maxvy = Math.Min(maxy, maxvy);
					g.FillColor = GetColor(wave);
					g.FillCircle(MouseCursorX, BoundingBox.Y + maxy - maxvy, 5);
					g.DrawColor = CustomGraphics.SelectColor;
					g.DrawCircle(MouseCursorX, BoundingBox.Y + maxy - maxvy, 5);
				}
				if (mWaves.Count > 0) {
					var t = CircuitElement.time - CircuitElement.delta_time * Speed * (BoundingBox.X + BoundingBox.Width - MouseCursorX);
					info[ct++] = TextUtils.Time(t);
				}
			}

			if (ShowFFT) {
				double maxFrequency = 1 / (CircuitElement.delta_time * Speed * 2);
				var posX = MouseCursorX - mFFTBoundingBox.X;
				if (posX < 0) {
					posX = 0;
				}
				info[ct++] = TextUtils.Unit(maxFrequency * posX / mFFTBoundingBox.Width, "Hz");
			}

			int szw = 0, szh = 15 * ct;
			for (int i = 0; i != ct; i++) {
				int w = (int)g.GetTextSize(info[i]).Width;
				if (w > szw) {
					szw = w;
				}
			}

			g.DrawColor = CustomGraphics.WhiteColor;
			g.DrawLine(MouseCursorX, BoundingBox.Y, MouseCursorX, BoundingBox.Y + BoundingBox.Height);
			int bx = MouseCursorX;
			if (bx < szw / 2) {
				bx = szw / 2;
			}

			g.FillColor = ControlPanel.ChkPrintable.Checked ? Color.White : Color.Black;
			g.FillRectangle(bx - szw / 2, BoundingBox.Y - szh, szw, szh);
			for (int i = 0; i != ct; i++) {
				int w = (int)g.GetTextSize(info[i]).Width;
				g.DrawLeftText(info[i], bx - w / 2, BoundingBox.Y - 2 - (ct - 1 - i) * 15);
			}
		}
		void DrawWave(CustomGraphics g, int waveIndex) {
			var wave = mWaves[waveIndex];
			if (wave.Symbol == null) {
				return;
			}

			var centerY = (BoundingBox.Height - 1) / 2.0f;
			double graphMid;
			double graphMult;
			{
				/* if we don't have overlapping scopes of different units, we can move zero around.
                 * Put it at the bottom if the scope is never negative. */
				var mx = Scale;
				var mn = 0.0;
				if (Normarize) {
					/* scale is maxed out, so fix boundaries of scope at maximum and minimum. */
					mx = mMaxValue;
					mn = mMinValue;
				}
				var gridMax = (mx - mn) * 0.55;  /* leave space at top and bottom */
				if (gridMax * gridMax < SCALE_MIN * SCALE_MIN) {
					gridMax = SCALE_MIN;
				} else if (mShowNegative || mMinValue < (mx + mn) * 0.5 - (mx - mn) * 0.54) {
					mn = -Scale;
					mShowNegative = true;
				}
				graphMid = (mx + mn) * 0.5;
				graphMult = centerY / gridMax;
				if (waveIndex == 0) {
					mMainGridMult = graphMult;
					mMainGridMid = graphMid;
				}

				mGridStepY = 1e-12;
				for (int i = 0; mGridStepY < 10 * gridMax / centerY; i++) {
					var m = i % 3;
					var exp = Math.Pow(10, (i - m) / 3);
					switch (m) {
					case 0:
						mGridStepY = 1e-12 * exp;
						break;
					case 1:
						mGridStepY = 2e-12 * exp;
						break;
					case 2:
						mGridStepY = 5e-12 * exp;
						break;
					}
				}
			}

			if (waveIndex == 0) {
				Color minorDiv;
				Color majorDiv;
				if (ControlPanel.ChkPrintable.Checked) {
					minorDiv = Color.FromArgb(0xCF, 0xCF, 0xCF);
					majorDiv = Color.FromArgb(0x7F, 0x7F, 0x7F);
				} else {
					minorDiv = Color.FromArgb(0x30, 0x30, 0x30);
					majorDiv = Color.FromArgb(0x7F, 0x7F, 0x7F);
				}

				/* horizontal gridlines */
				g.DrawColor = minorDiv;
				var gridStepY = mGridStepY * graphMult;
				var gridDivY = (int)(centerY / gridStepY);
				var showGridlines = mGridStepY != 0;
				for (int ll = -gridDivY; ll <= gridDivY && showGridlines; ll++) {
					var ly = (float)(centerY - ll * gridStepY);
					g.DrawLine(0, ly, BoundingBox.Width - 1, ly);
				}

				/* vertical gridlines */
				var baseT = CircuitElement.delta_time * Speed;
				var beginT = CircuitElement.time - BoundingBox.Width * baseT;
				var endT = CircuitElement.time - (CircuitElement.time % mGridStepX);
				g.DrawColor = minorDiv;
				for (int ll = 0; ; ll++) {
					var t = endT - mGridStepX * ll;
					var lx = (float)((t - beginT) / baseT);
					if (lx < 0) {
						break;
					}
					if (t < 0 || BoundingBox.Width <= lx) {
						continue;
					}
					if (((t + mGridStepX / 4) % (mGridStepX * mGridDivX)) < mGridStepX) {
					} else {
						g.DrawLine(lx, 0, lx, BoundingBox.Height - 1);
					}
				}
				g.DrawColor = majorDiv;
				for (int ll = 0; ; ll++) {
					var t = endT - mGridStepX * ll;
					var lx = (float)((t - beginT) / baseT);
					if (lx < 0) {
						break;
					}
					if (t < 0 || BoundingBox.Width <= lx) {
						continue;
					}
					if (((t + mGridStepX / 4) % (mGridStepX * mGridDivX)) < mGridStepX) {
						g.DrawLine(lx, 0, lx, BoundingBox.Height - 1);
					}
				}

				if (Normarize) {
					g.DrawColor = majorDiv;
					g.DrawLine(0, centerY, BoundingBox.Width - 1, centerY);
				} else {
					var ly = (float)(centerY + graphMid * graphMult);
					if (0 <= ly && ly < BoundingBox.Height) {
						g.DrawColor = majorDiv;
						g.DrawLine(0, ly, BoundingBox.Width - 1, ly);
					}
				}
			}

			var idxBegin = GetStartIndex(wave);
			var vMax = wave.MaxValues;
			var vMin = wave.MinValues;
			var yMax = BoundingBox.Height - 1;
			var yMin = 1;
			var rect = new PointF[BoundingBox.Width * 2 + 1];
			for (int x = 0; x != BoundingBox.Width; x++) {
				var idx = (x + idxBegin) & (mWaveLength - 1);
				var v = (float)(graphMult * (vMax[idx] - graphMid));
				var y = centerY - v - 0.5f;
				y = Math.Max(yMin, y);
				y = Math.Min(yMax, y);
				rect[x].X = x;
				rect[x].Y = y;
			}
			for (int x = BoundingBox.Width - 1, i = BoundingBox.Width; 0 <= x; x--, i++) {
				var idx = (x + idxBegin) & (mWaveLength - 1);
				var v = (float)(graphMult * (vMin[idx] - graphMid));
				var y = centerY - v + 0.5f;
				y = Math.Max(yMin, y);
				y = Math.Min(yMax, y);
				rect[i].X = x;
				rect[i].Y = y;
			}
			rect[BoundingBox.Width * 2] = rect[0];
			if (ControlPanel.ChkPrintable.Checked) {
				g.FillColor = GetColor(wave);
			} else {
				if (waveIndex == SelectedWave || wave.Symbol.IsMouseElm) {
					g.FillColor = CustomGraphics.SelectColor;
				} else {
					g.FillColor = mSomethingSelected ? COLORS[COLORS.Length - 1] : GetColor(wave);
				}
			}
			g.FillPolygon(rect);
		}
		void DrawFFTGridLines(CustomGraphics g) {
			const int xDivs = 20;
			const int yDivs = 10;
			int prevEnd = 0;
			double maxFrequency = 1 / (CircuitElement.delta_time * Speed * xDivs * 2);
			var gridBottom = mFFTBoundingBox.Height - 1;
			g.DrawColor = CustomGraphics.LineColor;
			g.DrawLine(0, 0, BoundingBox.Width, 0);
			g.DrawLine(0, gridBottom, BoundingBox.Width, gridBottom);
			g.DrawLine(mFFTBoundingBox.X, 0, mFFTBoundingBox.X, BoundingBox.Height);
			for (int i = 0; i < xDivs; i++) {
				int x = mFFTBoundingBox.X + mFFTBoundingBox.Width * i / xDivs;
				if (x < prevEnd) {
					continue;
				}
				string s = TextUtils.Unit((int)Math.Round(i * maxFrequency), "Hz");
				int sWidth = (int)Math.Ceiling(g.GetTextSize(s).Width);
				prevEnd = x + sWidth + 4;
				if (i > 0) {
					g.DrawLine(x, 0, x, BoundingBox.Height);
				}
				g.DrawLeftText(s, x, BoundingBox.Height - 10);
			}
			if (mWaves.Count == 1) {
				mFftMax = 0;
			} else {
				mFftMax = 20;
			}
			for (int i = 1; i < yDivs; i++) {
				int y = mFFTBoundingBox.Height * i / yDivs;
				string s;
				if (LogSpectrum) {
					s = (mFftMax - FFT_RANGE * i / yDivs).ToString() + "db";
				} else {
					s = (1.0 * (yDivs - i) / yDivs).ToString();
				}
				if (i > 0) {
					g.DrawLine(0, y, BoundingBox.Width, y);
				}
				g.DrawLeftText(s, 0, y + 8);
			}
		}
		void DrawFFT(CustomGraphics g, int waveIndex) {
			if (mFft.Size != mWaveLength) {
				mFft = new FFT(mWaveLength);
				mReal = new double[mWaveLength];
				mImag = new double[mWaveLength];
			}
			var wave = mWaves[waveIndex];
			var maxV = wave.MaxValues;
			var minV = wave.MinValues;
			int ptr = wave.Index;
			for (int i = 0; i < mWaveLength; i++) {
				var ii = (ptr - i + mWaveLength) % mWaveLength;
				mReal[i] = 0.5 * (maxV[ii] + minV[ii]) * (0.5 - 0.5 * Math.Cos(2.0 * Math.PI * i / mWaveLength));
				mImag[i] = 0;
			}
			mFft.Exec(mReal, mImag);
			if (0 == waveIndex) {
				mFftMainMax = SCALE_MIN;
				for (int i = 0; i < mWaveLength / 2; i++) {
					var m = mFft.Magnitude(mReal[i], mImag[i]);
					if (m > mFftMainMax) {
						mFftMainMax = m;
					}
				}
			}
			var bottom = mFFTBoundingBox.Height - 1;
			var scaleX = 2.0f * mFFTBoundingBox.Width / mWaveLength;
			var x0 = 1.0f * mFFTBoundingBox.X;
			var x1 = x0;
			var y0 = 0.0f;
			g.DrawColor = GetColor(wave);
			if (LogSpectrum) {
				var ymult = bottom / FFT_RANGE;
				for (int i = 0; i < mWaveLength / 2; i++) {
					var mag = mFft.Magnitude(mReal[i], mImag[i]);
					if (mag < SCALE_MIN) {
						mag = SCALE_MIN;
					}
					var db = 20 * Math.Log10(mag / mFftMainMax);
					if (db < mFftMax - FFT_RANGE) {
						db = mFftMax - FFT_RANGE;
					}
					var y1 = (float)((mFftMax - db) * ymult);
					x1 += scaleX;
					if (0 == i) {
						g.DrawLine(x0, y1, x1, y1);
					} else {
						g.DrawLine(x0, y0, x1, y1);
					}
					y0 = y1;
					x0 = x1;
				}
			} else {
				for (int i = 0; i < mWaveLength / 2; i++) {
					var mag = mFft.Magnitude(mReal[i], mImag[i]);
					var y1 = bottom - (float)(mag * bottom / mFftMainMax);
					x1 += scaleX;
					if (0 == i) {
						g.DrawLine(x0, y1, x1, y1);
					} else {
						g.DrawLine(x0, y0, x1, y1);
					}
					y0 = y1;
					x0 = x1;
				}
			}
		}
		void DrawInfoTexts(CustomGraphics g) {
			g.FillColor = CustomGraphics.TextColor;

			var textY = 8;
			if (!string.IsNullOrEmpty(Text)) {
				g.DrawLeftText(Text, 0, textY);
				textY += 12;
			}

			var ym = 6;
			if (ShowVoltage) {
				if (ShowScale) {
					string vScaleText = "";
					if (mGridStepY != 0) {
						vScaleText = ", " + TextUtils.VoltageAbs(mGridStepY) + "/div";
					}
					g.DrawLeftText(TextUtils.Time(mGridStepX) + "/div" + vScaleText, 0, textY);
				}
				g.DrawLeftText(TextUtils.Voltage(mMaxValue), BoundingBox.Width, ym);
				if (ShowRMS) {
					ym += 12;
					g.DrawLeftText(CalcRMS(), BoundingBox.Width, ym);

				}
				g.DrawLeftText(TextUtils.Voltage(mMinValue), BoundingBox.Width, BoundingBox.Height - 6);
			}

			if (ShowFreq) {
				ym += 12;
				g.DrawLeftText(CalcFrequency(), BoundingBox.Width, ym);
			}
			if (Normarize) {
				var centerY = (BoundingBox.Height - 1) / 2.0f;
				g.DrawLeftText(TextUtils.Voltage(mMainGridMid), BoundingBox.Width, centerY);
			}
		}
		#endregion
	}
}
