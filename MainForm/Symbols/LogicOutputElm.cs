namespace MainForm;

internal class LogicOutputElm : ElmBase {
	const int FLAG_TERNARY = 1;
	const int FLAG_NUMERIC = 2;
	const int FLAG_PULLDOWN = 4;
	double threshold;
	string value;

	public LogicOutputElm(int xx, int yy) : base(xx, yy) {
		threshold = 2.5;
	}

	public LogicOutputElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
		threshold = st.nextTokenDouble(2.5);
	}

	public override string dump() {
		return base.dump() + " " + threshold;
	}

	public override int getDumpType() { return 'M'; }

	public override int getPostCount() { return 1; }

	bool isTernary() { return (flags & FLAG_TERNARY) != 0; }

	bool isNumeric() { return (flags & (FLAG_TERNARY | FLAG_NUMERIC)) != 0; }

	bool needsPullDown() { return (flags & FLAG_PULLDOWN) != 0; }

	public override void setPoints() {
		base.setPoints();
		lead1 = interpPoint(point1, point2, 1 - 12 / dn);
	}

	public override void draw(CustomGraphics g) {
		var oldf = g.getFont();
		var f = new Font("SansSerif", 20, FontStyle.Bold);
		g.setFont(f);
		//g.setColor(needsHighlight() ? selectColor : lightGrayColor);
		g.setColor(lightGrayColor);
		var s = (volts[0] < threshold) ? "L" : "H";
		if (isTernary()) {
			if (volts[0] > 3.75)
				s = "2";
			else if (volts[0] > 1.25)
				s = "1";
			else
				s = "0";
		} else if (isNumeric())
			s = (volts[0] < threshold) ? "0" : "1";
		value = s;
		setBbox(point1, lead1, 0);
		drawCenteredText(g, s, x2, y2, true);
		setVoltageColor(g, volts[0]);
		drawThickLine(g, point1, lead1);
		drawPosts(g);
		g.setFont(oldf);
	}

	public override void stamp() {
		if (needsPullDown())
			sim.stampResistor(nodes[0], 0, 1e6);
	}

	public override double getVoltageDiff() { return volts[0]; }

	public override void getInfo(string[] arr) {
		arr[0] = "logic output";
		arr[1] = (volts[0] < threshold) ? "low" : "high";
		if (isNumeric())
			arr[1] = value;
		arr[2] = "V = " + getVoltageText(volts[0]);
	}

	public override EditInfo? getEditInfo(int n) {
		if (n == 0)
			return new EditInfo("Threshold", threshold, 10, -10);
		if (n == 1) {
			return new EditInfo("Current Required", needsPullDown());
		}
		if (n == 2) {
			return new EditInfo("Numeric", isNumeric());
		}
		if (n == 3) {
			return new EditInfo("Ternary", isTernary());
		}
		return null;
	}

	public override void setEditValue(int n, EditInfo ei) {
		if (n == 0)
			threshold = ei.value;
		if (n == 1) {
			if (ei.checkbox.Checked)
				flags = FLAG_PULLDOWN;
			else
				flags &= ~FLAG_PULLDOWN;
		}
		if (n == 2) {
			if (ei.checkbox.Checked)
				flags |= FLAG_NUMERIC;
			else
				flags &= ~FLAG_NUMERIC;
		}
		if (n == 3) {
			if (ei.checkbox.Checked)
				flags |= FLAG_TERNARY;
			else
				flags &= ~FLAG_TERNARY;
		}
	}

	public override int getShortcut() { return 'o'; }
}
