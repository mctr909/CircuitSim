namespace MainForm;

internal class GroundElm : ElmBase {
	static int lastSymbolType = 0;
	int symbolType;

	public GroundElm(int xx, int yy) : base(xx, yy) {
		symbolType = lastSymbolType;
	}

	public GroundElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
		if (st.HasMoreTokens) {
			symbolType = st.nextTokenInt();
		}
	}

	public override string dump() {
		return base.dump() + " " + symbolType;
	}

	public override int getDumpType() {
		return 'g';
	}

	public override int getPostCount() {
		return 1;
	}

	public override void draw(CustomGraphics g) {
		setVoltageColor(g, 0);
		drawThickLine(g, point1, point2);
		if (symbolType == 0) {
			for (int i = 0; i != 3; i++) {
				var a = 10 - i * 4;
				var b = i * 5; // -10;
				interpPoint2(point1, point2, ref ps1, ref ps2, 1 + b / dn, a);
				drawThickLine(g, ps1, ps2);
			}
		} else if (symbolType == 1) {
			interpPoint2(point1, point2, ref ps1, ref ps2, 1, 10);
			drawThickLine(g, ps1, ps2);
			for (int i = 0; i <= 2; i++) {
				var p = interpPoint(ps1, ps2, i / 2.0);
				drawThickLine(g, p.X, p.Y, (int)(p.X - 5 * dpx1 + 8 * dx / dn), (int)(p.Y + 8 * dy / dn - 5 * dpy1));
			}
		} else if (symbolType == 2) {
			interpPoint2(point1, point2, ref ps1, ref ps2, 1, 10);
			drawThickLine(g, ps1, ps2);
			var ps3x = (int)(point2.X + 10 * dx / dn);
			var ps3y = (int)(point2.Y + 10 * dy / dn);
			drawThickLine(g, ps1.X, ps1.Y, ps3x, ps3y);
			drawThickLine(g, ps2.X, ps2.Y, ps3x, ps3y);
		} else {
			interpPoint2(point1, point2, ref ps1, ref ps2, 1, 10);
			drawThickLine(g, ps1, ps2);
		}
		interpPoint(point1, point2, ref ps2, 1 + 11.0 / dn);
		doDots(g);
		setBbox(point1, ps2, 11);
		drawPosts(g);
	}

	public override void setCurrent(int x, double c) {
		current = -c;
	}

	public override void stamp() {
		sim.stampVoltageSource(0, nodes[0], voltSource, 0);
	}

	public override double getVoltageDiff() {
		return 0;
	}

	public override int getVoltageSourceCount() {
		return 1;
	}

	public override void getInfo(string[] arr) {
		arr[0] = "ground";
		arr[1] = "I = " + getCurrentText(getCurrent());
	}

	public override bool hasGroundConnection(int n1) {
		return true;
	}

	public override int getShortcut() {
		return 'g';
	}

	public override EditInfo? getEditInfo(int n) {
		if (n == 0) {
			var ei = new EditInfo("Symbol", 0);
			ei.choice = new ComboBox();
			ei.choice.Items.Add("Earth");
			ei.choice.Items.Add("Chassis");
			ei.choice.Items.Add("Signal");
			ei.choice.Items.Add("Common");
			ei.choice.SelectedIndex = symbolType;
			return ei;
		}
		return null;
	}

	public override void setEditValue(int n, EditInfo ei) {
		if (n == 0)
			lastSymbolType = symbolType = ei.choice.SelectedIndex;
	}

	public override double getCurrentIntoNode(int n) {
		return -current;
	}
}
