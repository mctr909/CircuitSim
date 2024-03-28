namespace MainForm;

internal class ProbeElm : ElmBase {
	const int FLAG_SHOWVOLTAGE = 1;
	int meter;
	int units;
	int scale;
	const int TP_VOL = 0;
	const int TP_RMS = 1;
	const int TP_MAX = 2;
	const int TP_MIN = 3;
	const int TP_P2P = 4;
	const int TP_BIN = 5;
	const int TP_FRQ = 6;
	const int TP_PER = 7;
	const int TP_PWI = 8;
	const int TP_DUT = 9; // mark to space ratio

	public ProbeElm(int xx, int yy) : base(xx, yy) {
		meter = TP_VOL;

		// default for new elements
		flags = FLAG_SHOWVOLTAGE;
		scale = SCALE_AUTO;
	}

	public ProbeElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
		meter = st.nextTokenInt(TP_VOL); // get meter type from saved dump
		scale = st.nextTokenInt(SCALE_AUTO);
	}

	public override int getDumpType() {
		return 'p';
	}

	public override string dump() {
		return base.dump() + " " + meter + " " + scale;
	}

	string getMeter() {
		return meter switch {
			TP_VOL => "V",
			TP_RMS => "V(rms)",
			TP_MAX => "Vmax",
			TP_MIN => "Vmin",
			TP_P2P => "Peak to peak",
			TP_BIN => "Binary",
			TP_FRQ => "Frequency",
			TP_PER => "Period",
			TP_PWI => "Pulse width",
			TP_DUT => "Duty cycle",
			_ => "",
		};
	}

	double rmsV = 0, total, count;
	double binaryLevel = 0;// 0 or 1 - double because we only pass doubles back to the web page
	int zerocount = 0;
	double maxV = 0, lastMaxV;
	double minV = 0, lastMinV;
	double frequency = 0;
	double period = 0;
	double pulseWidth = 0;
	double dutyCycle = 0;
	double selectedValue = 0;

	bool increasingV = true, decreasingV = true;
	long periodStart, periodLength, pulseStart;// time between consecutive max values

	Point center;

	public override void setPoints() {
		base.setPoints();
		center = interpPoint(point1, point2, .5);
	}

	public override void draw(CustomGraphics g) {
		int hs = 8;
		setBbox(point1, point2, hs);
		var selected = needsHighlight();
		double len = (selected || sim.dragElm == this || mustShowVoltage()) ? 16 : dn - 32;
		calcLeads((int)len);
		setVoltageColor(g, volts[0]);
		if (selected)
			g.setColor(selectColor);
		drawThickLine(g, point1, lead1);
		setVoltageColor(g, volts[1]);
		if (selected)
			g.setColor(selectColor);
		drawThickLine(g, lead2, point2);
		var f = new Font("SansSerif", 14f, FontStyle.Bold);
		g.setFont(f);
		if (this == sim.plotXElm)
			drawCenteredText(g, "X", center.X, center.Y, true);
		if (this == sim.plotYElm)
			drawCenteredText(g, "Y", center.X, center.Y, true);
		if (mustShowVoltage()) {
			var s = "";
			switch (meter) {
			case TP_VOL:
				s = getUnitTextWithScale(getVoltageDiff(), "V", scale);
				break;
			case TP_RMS:
				s = getUnitTextWithScale(rmsV, "V(rms)", scale);
				break;
			case TP_MAX:
				s = getUnitTextWithScale(lastMaxV, "Vpk", scale);
				break;
			case TP_MIN:
				s = getUnitTextWithScale(lastMinV, "Vmin", scale);
				break;
			case TP_P2P:
				s = getUnitTextWithScale(lastMaxV - lastMinV, "Vp2p", scale);
				break;
			case TP_BIN:
				s = binaryLevel + "";
				break;
			case TP_FRQ:
				s = getUnitText(frequency, "Hz");
				break;
			case TP_PER:
				// s = "percent:"+period + " " + sim.timeStep + " " + sim.simTime + " " +
				// sim.getIterCount();
				break;
			case TP_PWI:
				s = getUnitText(pulseWidth, "S");
				break;
			case TP_DUT:
				s = dutyCycle.ToString(showFormat);
				break;
			}
			drawValues(g, s, 4);
		}
		g.setColor(Color.White);
		g.setFont(unitsFont);
		var plusPoint = interpPoint(point1, point2, (dn / 2 - len / 2 - 4) / dn, -10 * dsign);
		if (y2 > y)
			plusPoint.Y += 4;
		if (y > y2)
			plusPoint.Y += 3;
		var w = (int)g.measureText("+").Width;
		g.drawString("+", plusPoint.X - w / 2, plusPoint.Y);
		drawPosts(g);
	}

	bool mustShowVoltage() {
		return (flags & FLAG_SHOWVOLTAGE) != 0;
	}

	public override void stepFinished() {
		count++;// how many counts are in a cycle
		var v = getVoltageDiff();
		total += v * v; // sum of squares

		if (v < 2.5)
			binaryLevel = 0;
		else
			binaryLevel = 1;

		// V going up, track maximum value with
		if (v > maxV && increasingV) {
			maxV = v;
			increasingV = true;
			decreasingV = false;
		}

		var milliseconds = sw.ElapsedMilliseconds;

		if (v < maxV && increasingV) {// change of direction V now going down - at start of waveform
			lastMaxV = maxV; // capture last maximum
							 // capture time between

			periodLength = milliseconds - periodStart;
			periodStart = milliseconds;
			period = periodLength;
			pulseWidth = milliseconds - pulseStart;
			dutyCycle = pulseWidth / periodLength;
			minV = v; // track minimum value with V
			increasingV = false;
			decreasingV = true;

			// rms data
			total = total / count;
			rmsV = Math.Sqrt(total);
			if (double.IsNaN(rmsV))
				rmsV = 0;
			count = 0;
			total = 0;

		}
		if (v < minV && decreasingV) { // V going down, track minimum value with V
			minV = v;
			increasingV = false;
			decreasingV = true;
		}

		if (v > minV && decreasingV) { // change of direction V now going up
			lastMinV = minV; // capture last minimum
			pulseStart = milliseconds;
			maxV = v;
			increasingV = true;
			decreasingV = false;

			// rms data
			total = total / count;
			rmsV = Math.Sqrt(total);
			if (double.IsNaN(rmsV))
				rmsV = 0;
			count = 0;
			total = 0;

		}
		// need to zero the rms value if it stays at 0 for a while
		if (v == 0) {
			zerocount++;
			if (zerocount > 5) {
				total = 0;
				rmsV = 0;
				maxV = 0;
				minV = 0;
			}
		} else {
			zerocount = 0;
		}
	}

	public override void getInfo(string[] arr) {
		arr[0] = "voltmeter";
		arr[1] = "Vd = " + getVoltageText(getVoltageDiff());
	}

	public override bool getConnection(int n1, int n2) {
		return false;
	}

	public override EditInfo? getEditInfo(int n) {
		if (n == 0) {
			return new EditInfo("Show Value", mustShowVoltage());
		}
		if (n == 1) {
			var ei = new EditInfo("Value", selectedValue, -1, -1);
			ei.choice = new ComboBox();
			ei.choice.Items.Add("Voltage");
			ei.choice.Items.Add("RMS Voltage");
			ei.choice.Items.Add("Max Voltage");
			ei.choice.Items.Add("Min Voltage");
			ei.choice.Items.Add("P2P Voltage");
			ei.choice.Items.Add("Binary Value");
			// ei.choice.Items.Add("Frequency");
			// ei.choice.Items.Add("Period");
			// ei.choice.Items.Add("Pulse Width");
			// ei.choice.Items.Add("Duty Cycle");
			ei.choice.SelectedIndex = meter;
			return ei;
		}
		if (n == 2) {
			var ei = new EditInfo("Scale", 0);
			ei.choice = new ComboBox();
			ei.choice.Items.Add("Auto");
			ei.choice.Items.Add("V");
			ei.choice.Items.Add("mV");
			ei.choice.Items.Add("uV");
			ei.choice.SelectedIndex = scale;
			return ei;
		}
		return null;
	}

	public override void setEditValue(int n, EditInfo ei) {
		if (n == 0) {
			if (ei.checkbox.Checked)
				flags = FLAG_SHOWVOLTAGE;
			else
				flags &= ~FLAG_SHOWVOLTAGE;
		}
		if (n == 1) {
			meter = ei.choice.SelectedIndex;
		}
		if (n == 2) {
			scale = ei.choice.SelectedIndex;
		}
	}
}
