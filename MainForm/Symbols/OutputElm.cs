namespace MainForm;

internal class OutputElm : ElmBase {
	const int FLAG_VALUE = 1;
	int scale;

	public OutputElm(int xx, int yy) : base(xx, yy) {
		scale = SCALE_AUTO;
	}

	public OutputElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
		scale = st.nextTokenInt(SCALE_AUTO);
	}

	public override string dump() {
		return base.dump() + " " + scale;
	}

	public override int getDumpType() {
		return 'O';
	}

	public override int getPostCount() {
		return 1;
	}

	public override void setPoints() {
		base.setPoints();
		lead1 = new Point();
	}

	public override void draw(CustomGraphics g) {
		var selected = needsHighlight();
		var f = new Font("SansSerif", 14, selected ? FontStyle.Bold : FontStyle.Regular);
		g.setFont(f);
		g.setColor(selected ? selectColor : whiteColor);
		var s = (flags & FLAG_VALUE) != 0 ? getUnitTextWithScale(volts[0], "V", scale) : "out";
		// FontMetrics fm = g.getFontMetrics();
		if (this == sim.plotXElm)
			s = "X";
		if (this == sim.plotYElm)
			s = "Y";
		interpPoint(point1, point2, ref lead1, 1 - ((int)g.measureText(s).Width / 2 + 8) / dn);
		setBbox(point1, lead1, 0);
		drawCenteredText(g, s, x2, y2, true);
		setVoltageColor(g, volts[0]);
		if (selected)
			g.setColor(selectColor);
		drawThickLine(g, point1, lead1);
		drawPosts(g);
	}

	public override double getVoltageDiff() {
		return volts[0];
	}

	public override void getInfo(string[] arr) {
		arr[0] = "output";
		arr[1] = "V = " + getVoltageText(volts[0]);
	}

	public override EditInfo? getEditInfo(int n) {
		if (n == 0) {
			return new EditInfo("Show Voltage", (flags & FLAG_VALUE) != 0);
		}
		if (n == 1) {
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
		if (n == 0)
			flags = ei.checkbox.Checked ? (flags | FLAG_VALUE) : (flags & ~FLAG_VALUE);
		if (n == 1)
			scale = ei.choice.SelectedIndex;
	}
}
