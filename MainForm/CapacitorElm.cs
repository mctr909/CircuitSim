namespace MainForm;

public class CapacitorElm : ElmBase {
	public const int FLAG_BACK_EULER = 2;
	double capacitance;
	double compResistance, voltdiff;
	double initialVoltage;
	double curSourceValue;

	Point[] plate1, plate2;
	// used for PolarCapacitorElm
	Point[] platePoints;

	public CapacitorElm(int xx, int yy) : base(xx, yy) {
		capacitance = 1e-5;
		initialVoltage = 1e-3;
	}

	public CapacitorElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
		capacitance = st.nextTokenDouble();
		voltdiff = st.nextTokenDouble();
		initialVoltage = st.nextTokenDouble(1e-3);
	}

	private bool isTrapezoidal() {
		return (flags & FLAG_BACK_EULER) == 0;
	}

	public double getCapacitance() {
		return capacitance;
	}

	public void setCapacitance(double c) {
		capacitance = c;
	}

	public void shorted() {
		base.reset();
		voltdiff = current = curcount = curSourceValue = 0;
	}

	public override void setNodeVoltage(int n, double c) {
		base.setNodeVoltage(n, c);
		voltdiff = volts[0] - volts[1];
	}

	public override void reset() {
		base.reset();
		current = curcount = curSourceValue = 0;
		// put small charge on caps when reset to start oscillators
		voltdiff = initialVoltage;
	}

	protected override int getDumpType() {
		return 'c';
	}

	public override int getShortcut() {
		return 'c';
	}

	public override string dump() {
		return base.dump() + " " + capacitance + " " + voltdiff + " " + initialVoltage;
	}

	public override void setPoints() {
		base.setPoints();
		var f = (dn / 2 - 4) / dn;
		// calc leads
		lead1 = interpPoint(point1, point2, f);
		lead2 = interpPoint(point1, point2, 1 - f);
		// calc plates
		plate1 = newPointArray(2);
		plate2 = newPointArray(2);
		interpPoint2(point1, point2, ref plate1[0], ref plate1[1], f, 12);
		interpPoint2(point1, point2, ref plate2[0], ref plate2[1], 1 - f, 12);
	}

	public override void stamp() {
		if (sim.dcAnalysisFlag) {
			// when finding DC operating point, replace cap with a 100M resistor
			sim.stampResistor(nodes[0], nodes[1], 1e8);
			curSourceValue = 0;
			return;
		}

		// capacitor companion model using trapezoidal approximation
		// (Norton equivalent) consists of a current source in
		// parallel with a resistor. Trapezoidal is more accurate
		// than backward euler but can cause oscillatory behavior
		// if RC is small relative to the timestep.
		if (isTrapezoidal())
			compResistance = sim.timeStep / (2 * capacitance);
		else
			compResistance = sim.timeStep / capacitance;
		sim.stampResistor(nodes[0], nodes[1], compResistance);
		sim.stampRightSide(nodes[0]);
		sim.stampRightSide(nodes[1]);
	}

	public override void startIteration() {
		if (isTrapezoidal())
			curSourceValue = -voltdiff / compResistance - current;
		else
			curSourceValue = -voltdiff / compResistance;
	}

	public override void calculateCurrent() {
		double voltdiff = volts[0] - volts[1];
		if (sim.dcAnalysisFlag) {
			current = voltdiff / 1e8;
			return;
		}
		// we check compResistance because this might get called
		// before stamp(), which sets compResistance, causing
		// infinite current
		if (compResistance > 0)
			current = voltdiff / compResistance + curSourceValue;
	}

	public override void doStep() {
		if (sim.dcAnalysisFlag)
			return;
		sim.stampCurrentSource(nodes[0], nodes[1], curSourceValue);
	}

	public override void getInfo(string[] arr) {
		arr[0] = "capacitor";
		getBasicInfo(arr);
		arr[3] = "C = " + getUnitText(capacitance, "F");
		arr[4] = "P = " + getUnitText(getPower(), "W");
		// double v = getVoltageDiff();
		// arr[4] = "U = " + getUnitText(.5*capacitance*v*v, "J");
	}

	public override string getScopeText(int v) {
		return "capacitor, " + getUnitText(capacitance, "F");
	}

	public override EditInfo? getEditInfo(int n) {
		if (n == 0)
			return new EditInfo("Capacitance (F)", capacitance, 0, 0);
		if (n == 1) {
			var ei = new EditInfo("", 0, -1, -1);
			ei.checkbox = new CheckBox() { Text = "Trapezoidal Approximation", Checked = isTrapezoidal() };
			return ei;
		}
		if (n == 2)
			return new EditInfo("Initial Voltage (on Reset)", initialVoltage);
		return null;
	}

	public override void setEditValue(int n, EditInfo ei) {
		if (n == 0 && ei.value > 0)
			capacitance = ei.value;
		if (n == 1) {
			if (ei.checkbox != null && ei.checkbox.Checked)
				flags &= ~FLAG_BACK_EULER;
			else
				flags |= FLAG_BACK_EULER;
		}
		if (n == 2)
			initialVoltage = ei.value;
	}

	public override void draw(CustomGraphics g) {
		int hs = 12;
		setBbox(point1, point2, hs);

		// draw first lead and plate
		setVoltageColor(g, volts[0]);
		drawThickLine(g, point1, lead1);
		setPowerColor(g, false);
		drawThickLine(g, plate1[0], plate1[1]);
		if (sim.powerCheckItem.Checked) {
			g.setStrokeStyle(Color.Gray);
			g.setFillStyle(Color.Gray);
		}

		// draw second lead and plate
		setVoltageColor(g, volts[1]);
		drawThickLine(g, point2, lead2);
		setPowerColor(g, false);
		if (platePoints == null) {
			drawThickLine(g, plate2[0], plate2[1]);
		} else {
			for (int i = 0; i != 7; i++)
				drawThickLine(g, platePoints[i], platePoints[i + 1]);
		}

		updateDotCount();
		if (sim.dragElm != this) {
			drawDots(g, point1, lead1, curcount);
			drawDots(g, point2, lead2, -curcount);
		}
		drawPosts(g);
		if (sim.showValuesCheckItem.Checked) {
			var s = getShortUnitText(capacitance, "F");
			drawValues(g, s, hs);
		}
	}
}
