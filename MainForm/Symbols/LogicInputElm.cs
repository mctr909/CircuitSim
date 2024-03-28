namespace MainForm;

internal class LogicInputElm : SwitchElm {
	const int FLAG_TERNARY = 1;
	const int FLAG_NUMERIC = 2;
	double hiV, loV;

	public LogicInputElm(int xx, int yy) : base(xx, yy, false) {
		hiV = 5;
		loV = 0;
	}

	public LogicInputElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f, st) {
		hiV = st.nextTokenDouble(5);
		loV = st.nextTokenDouble();
		if (isTernary())
			posCount = 3;
	}

	bool isTernary() {
		return (flags & FLAG_TERNARY) != 0;
	}

	bool isNumeric() {
		return (flags & (FLAG_TERNARY | FLAG_NUMERIC)) != 0;
	}

	public override int getDumpType() {
		return 'L';
	}

	public override string dump() {
		return base.dump() + " " + hiV + " " + loV;
	}

	public override int getPostCount() {
		return 1;
	}

	public override void setPoints() {
		base.setPoints();
		lead1 = interpPoint(point1, point2, 1 - 12 / dn);
	}

	public override void draw(CustomGraphics g) {
		var oldf = g.getFont();
		var f = new Font("SansSerif", 20, FontStyle.Bold);
		g.setFont(f);
		g.setColor(needsHighlight() ? selectColor : whiteColor);
		var s = position == 0 ? "L" : "H";
		if (isNumeric())
			s = "" + position;
		setBbox(point1, lead1, 0);
		drawCenteredText(g, s, x2, y2, true);
		setVoltageColor(g, volts[0]);
		drawThickLine(g, point1, lead1);
		updateDotCount();
		drawDots(g, point1, lead1, -curcount);
		drawPosts(g);
		g.setFont(oldf);
	}

	protected override Rectangle getSwitchRect() {
		return new Rectangle(x2 - 10, y2 - 10, 20, 20);
	}

	public override void setCurrent(int vs, double c) {
		current = c;
	}

	public override void stamp() {
		var v = (position == 0) ? loV : hiV;
		if (isTernary())
			v = position * 2.5;
		sim.stampVoltageSource(0, nodes[0], voltSource, v);
	}

	public override int getVoltageSourceCount() {
		return 1;
	}

	public override double getVoltageDiff() {
		return volts[0];
	}

	public override void getInfo(string[] arr) {
		arr[0] = "logic input";
		arr[1] = (position == 0) ? "low" : "high";
		if (isNumeric())
			arr[1] = "" + position;
		arr[1] += " (" + getVoltageText(volts[0]) + ")";
		arr[2] = "I = " + getCurrentText(getCurrent());
	}

	public override bool hasGroundConnection(int n1) {
		return true;
	}

	public override EditInfo? getEditInfo(int n) {
		if (n == 0) {
			return new EditInfo("Momentary Switch", momentary);
		}
		if (n == 1)
			return new EditInfo("High Voltage", hiV, 10, -10);
		if (n == 2)
			return new EditInfo("Low Voltage", loV, 10, -10);
		if (n == 3) {
			return new EditInfo("Numeric", isNumeric());
		}
		if (n == 4) {
			return new EditInfo("Ternary", isTernary());
		}
		return null;
	}

	public override void setEditValue(int n, EditInfo ei) {
		if (n == 0)
			momentary = ei.checkbox.Checked;
		if (n == 1)
			hiV = ei.value;
		if (n == 2)
			loV = ei.value;
		if (n == 3) {
			if (ei.checkbox.Checked)
				flags |= FLAG_NUMERIC;
			else
				flags &= ~FLAG_NUMERIC;
		}
		if (n == 4) {
			if (ei.checkbox.Checked)
				flags |= FLAG_TERNARY;
			else
				flags &= ~FLAG_TERNARY;
			posCount = (isTernary()) ? 3 : 2;
		}
	}

	public override int getShortcut() {
		return 'i';
	}

	public override double getCurrentIntoNode(int n) {
		return current;
	}
}
