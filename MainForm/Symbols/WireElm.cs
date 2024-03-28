namespace MainForm;

internal class WireElm : ElmBase {
	public bool hasWireInfo; // used in CirSim to calculate wire currents

	public WireElm(int xx, int yy) : base(xx, yy) {
	}

	public WireElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
	}

	const int FLAG_SHOWCURRENT = 1;
	const int FLAG_SHOWVOLTAGE = 2;

	public override void draw(CustomGraphics g) {
		setVoltageColor(g, volts[0]);
		drawThickLine(g, point1, point2);
		doDots(g);
		setBbox(point1, point2, 3);
		var s = "";
		if (mustShowCurrent()) {
			s = getShortUnitText(Math.Abs(getCurrent()), "A");
		}
		if (mustShowVoltage()) {
			s = (s.Length > 0 ? s + " " : "") + getShortUnitText(volts[0], "V");
		}
		drawValues(g, s, 4);
		drawPosts(g);
	}

	public override void stamp() {
		// sim.stampVoltageSource(nodes[0], nodes[1], voltSource, 0);
	}

	bool mustShowCurrent() {
		return (flags & FLAG_SHOWCURRENT) != 0;
	}

	bool mustShowVoltage() {
		return (flags & FLAG_SHOWVOLTAGE) != 0;
	}

	// int getVoltageSourceCount() { return 1; }
	public override void getInfo(string[] arr) {
		arr[0] = "wire";
		arr[1] = "I = " + getCurrentDText(getCurrent());
		arr[2] = "V = " + getVoltageText(volts[0]);
	}

	public override int getDumpType() {
		return 'w';
	}

	public override double getPower() {
		return 0;
	}

	public override double getVoltageDiff() {
		return volts[0];
	}

	public override bool isWire() {
		return true;
	}

	public override EditInfo? getEditInfo(int n) {
		if (n == 0) {
			return new EditInfo("Show Current", mustShowCurrent());
		}
		if (n == 1) {
			return new EditInfo("Show Voltage", mustShowVoltage());
		}
		return null;
	}

	public override void setEditValue(int n, EditInfo ei) {
		if (n == 0) {
			if (ei.checkbox.Checked)
				flags |= FLAG_SHOWCURRENT;
			else
				flags &= ~FLAG_SHOWCURRENT;
		}
		if (n == 1) {
			if (ei.checkbox.Checked)
				flags |= FLAG_SHOWVOLTAGE;
			else
				flags &= ~FLAG_SHOWVOLTAGE;
		}
	}

	public override int getShortcut() {
		return 'w';
	}
}
