namespace MainForm;

public class PotElm : ElmBase {
	const int FLAG_SHOW_VALUES = 1;
	double position, maxResistance, resistance1, resistance2;
	double current1, current2, current3;
	double curcount1, curcount2, curcount3;
	Forms.Slider slider;
	Label label;
	string sliderText;

	Point post3, corner2, arrowPoint, midpoint, arrow1, arrow2;
	Point ps3, ps4;
	int bodyLen;

	public PotElm(int xx, int yy) : base(xx, yy) {
		setup();
		maxResistance = 1000;
		position = .5;
		sliderText = "Resistance";
		flags = FLAG_SHOW_VALUES;
		createSlider();
	}

	public PotElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
		maxResistance = st.nextTokenDouble();
		position = st.nextTokenDouble();
		st.nextToken(out sliderText);
		while (st.HasMoreTokens)
			sliderText += ' ' + st.nextToken();
		createSlider();
	}

	private void createSlider() {
		sim.addWidgetToVerticalPanel(label = new Label() { Text = sliderText });
		label.Anchor = AnchorStyles.Top | AnchorStyles.Left;
		int value = (int) (position * 100);
		sim.addWidgetToVerticalPanel(slider = new Forms.Slider() { Value = value, Minimum = 0, Maximum = 101 });
		// sim.verticalPanel.validate();
		// slider.addAdjustmentListener(this);
	}

	public virtual void setup() {
	}

	protected override int getPostCount() {
		return 3;
	}

	protected override int getDumpType() {
		return 174;
	}

	public void execute() {
		sim.analyzeFlag = true;
		setPoints();
	}

	// draw component values (number of resistor ohms, etc). hs = offset
	public void drawValues(CustomGraphics g, string s, Point pt, int hs) {
		if (s == null)
			return;
		g.setFont(unitsFont);
		// FontMetrics fm = g.getFontMetrics();
		var w = (int) g.measureText(s).Width;
		g.setStrokeStyle(whiteColor);
		g.setFillStyle(whiteColor);
		var ya = (int) g.currentFontSize / 2;
		var xc = pt.X;
		var yc = pt.Y;
		var dpx = hs;
		int dpy = 0;
		if (lead1.X != lead2.X) {
			dpx = 0;
			dpy = -hs;
		}
		if (dpx == 0)
			g.fillText(s, xc - w / 2, yc - Math.Abs(dpy) - 2);
		else {
			var xx = xc + Math.Abs(dpx) + 2;
			g.fillText(s, xx, yc + dpy + ya);
		}
	}

	public void onMouseWheel(MouseEventArgs e) {
		if (slider != null)
			slider.onMouseWheel(e);
	}

	public override Point getPost(int n) {
		return (n == 0) ? point1 : (n == 1) ? point2 : post3;
	}

	public override string dump() {
		return base.dump() + " " + maxResistance + " " +
				position + " " + sliderText;
	}

	public override void stamp() {
		resistance1 = maxResistance * position;
		resistance2 = maxResistance * (1 - position);
		sim.stampResistor(nodes[0], nodes[2], resistance1);
		sim.stampResistor(nodes[2], nodes[1], resistance2);
	}

	public override void delete() {
		sim.removeWidgetFromVerticalPanel(label);
		sim.removeWidgetFromVerticalPanel(slider);
		base.delete();
	}

	public override void setPoints() {
		base.setPoints();
		int offset = 0;
		int myLen = 0;
		if (Math.Abs(dx) > Math.Abs(dy)) {
			myLen = 2 * sim.gridSize * Math.Sign(dx)
					* ((Math.Abs(dx) + 2 * sim.gridSize - 1) / (2 * sim.gridSize));
			point2.X = point1.X + myLen;
			offset = (dx < 0) ? dy : -dy;
			point2.Y = point1.Y;
		} else {
			myLen = 2 * sim.gridSize * Math.Sign(dy)
					* ((Math.Abs(dy) + 2 * sim.gridSize - 1) / (2 * sim.gridSize));
			if (dy != 0) {
				point2.Y = point1.Y + myLen;
				offset = (dy > 0) ? dx : -dx;
				point2.X = point1.X;
			}
		}
		// if (abs(dx) > abs(dy)) {
		// dx = Integer.signum(dx) * sim.snapGrid(Math.abs(dx) / 2) * 2;
		// point2.x = x2 = point1.x + dx;
		// offset = (dx < 0) ? dy : -dy;
		// point2.y = point1.y;
		// } else {
		// dy = Integer.signum(dy) * sim.snapGrid(Math.abs(dy) / 2) * 2;
		// if (dy != 0) {
		// point2.y = y2 = point1.y + dy;
		// offset = (dy > 0) ? dx : -dx;
		// point2.x = point1.x;
		// }
		// }
		if (offset == 0)
			offset = sim.gridSize;
		dn = distance(point1, point2);
		int bodyLen = 32;
		calcLeads(bodyLen);
		position = slider.Value * .0099 + .005;
		var soff = (int) ((position - .5) * bodyLen);
		// int offset2 = offset - sign(offset)*4;
		post3 = interpPoint(point1, point2, .5, offset);
		corner2 = interpPoint(point1, point2, soff / dn + .5, offset);
		arrowPoint = interpPoint(point1, point2, soff / dn + .5, 8 * Math.Sign(offset));
		midpoint = interpPoint(point1, point2, soff / dn + .5);
		arrow1 = new Point();
		arrow2 = new Point();
		var clen = Math.Abs(offset) - 8;
		interpPoint2(corner2, arrowPoint, ref arrow1, ref arrow2, (clen - 8) / clen, 8);
		ps3 = new Point();
		ps4 = new Point();
	}

	public override void reset() {
		curcount1 = curcount2 = curcount3 = 0;
		base.reset();
	}

	public override void calculateCurrent() {
		if (resistance1 == 0)
			return; // avoid NaN
		current1 = (volts[0] - volts[2]) / resistance1;
		current2 = (volts[1] - volts[2]) / resistance2;
		current3 = -current1 - current2;
	}

	public override double getCurrentIntoNode(int n) {
		if (n == 0)
			return -current1;
		if (n == 1)
			return -current2;
		return -current3;
	}

	public override void getInfo(params string[] arr) {
		arr[0] = "potentiometer";
		arr[1] = "Vd = " + getVoltageDText(getVoltageDiff());
		arr[2] = "R1 = " + getUnitText(resistance1, "Ω");
		arr[3] = "R2 = " + getUnitText(resistance2, "Ω");
		arr[4] = "I1 = " + getCurrentDText(current1);
		arr[5] = "I2 = " + getCurrentDText(current2);
	}

	public override EditInfo? getEditInfo(int n) {
		// ohmString doesn't work here on linux
		if (n == 0)
			return new EditInfo("Resistance (ohms)", maxResistance, 0, 0);
		if (n == 1) {
			var ei = new EditInfo("Slider Text", 0, -1, -1);
			ei.text = sliderText;
			return ei;
		}
		if (n == 2) {
			var ei = new EditInfo("", 0, -1, -1);
			ei.checkbox = new CheckBox() { Text = "Show Values", Checked = (flags & FLAG_SHOW_VALUES) != 0 };
			return ei;
		}
		return null;
	}

	public override void setEditValue(int n, EditInfo ei) {
		if (n == 0)
			maxResistance = ei.value;
		if (n == 1) {
			sliderText = ei.textf.Text;
			label.Text = sliderText;
			sim.setiFrameHeight();
		}
		if (n == 2)
			flags = ei.changeFlag(flags, FLAG_SHOW_VALUES);
	}

	public override void setMouseElm(bool v) {
		base.setMouseElm(v);
		if (slider != null)
			slider.draw();
	}

	public override void draw(CustomGraphics g) {
		int segments = 16;
		int i;
		int ox = 0;
		var hs = sim.euroResistorCheckItem.Checked ? 6 : 8;
		var v1 = volts[0];
		var v2 = volts[1];
		var v3 = volts[2];
		setBbox(point1, point2, hs);
		draw2Leads(g);
		setPowerColor(g, true);
		var segf = 1.0 / segments;
		var divide = (int) (segments * position);
		if (!sim.euroResistorCheckItem.Checked) {
			// draw zigzag
			for (i = 0; i != segments; i++) {
				int nx = 0;
				switch (i & 3) {
				case 0:
					nx = 1;
					break;
				case 2:
					nx = -1;
					break;
				default:
					nx = 0;
					break;
				}
				var v = v1 + (v3 - v1) * i / divide;
				if (i >= divide)
					v = v3 + (v2 - v3) * (i - divide) / (segments - divide);
				setVoltageColor(g, v);
				interpPoint(lead1, lead2, ref ps1, i * segf, hs * ox);
				interpPoint(lead1, lead2, ref ps2, (i + 1) * segf, hs * nx);
				drawThickLine(g, ps1, ps2);
				ox = nx;
			}
		} else {
			// draw rectangle
			setVoltageColor(g, v1);
			interpPoint2(lead1, lead2, ref ps1, ref ps2, 0, hs);
			drawThickLine(g, ps1, ps2);
			for (i = 0; i != segments; i++) {
				var v = v1 + (v3 - v1) * i / divide;
				if (i >= divide)
					v = v3 + (v2 - v3) * (i - divide) / (segments - divide);
				setVoltageColor(g, v);
				interpPoint2(lead1, lead2, ref ps1, ref ps2, i * segf, hs);
				interpPoint2(lead1, lead2, ref ps3, ref ps4, (i + 1) * segf, hs);
				drawThickLine(g, ps1, ps3);
				drawThickLine(g, ps2, ps4);
			}
			interpPoint2(lead1, lead2, ref ps1, ref ps2, 1, hs);
			drawThickLine(g, ps1, ps2);
		}
		setVoltageColor(g, v3);
		drawThickLine(g, post3, corner2);
		drawThickLine(g, corner2, arrowPoint);
		drawThickLine(g, arrow1, arrowPoint);
		drawThickLine(g, arrow2, arrowPoint);
		curcount1 = updateDotCount(current1, curcount1);
		curcount2 = updateDotCount(current2, curcount2);
		curcount3 = updateDotCount(current3, curcount3);
		if (sim.dragElm != this) {
			drawDots(g, point1, midpoint, curcount1);
			drawDots(g, point2, midpoint, curcount2);
			drawDots(g, post3, corner2, curcount3);
			drawDots(g, corner2, midpoint,
					curcount3 + distance(post3, corner2));
		}
		drawPosts(g);

		if (sim.showValuesCheckItem.Checked && resistance1 > 0 && (flags & FLAG_SHOW_VALUES) != 0) {
			// check for vertical pot with 3rd terminal on left
			var reverseY = (post3.X < lead1.X && lead1.X == lead2.X);
			// check for horizontal pot with 3rd terminal on top
			var reverseX = (post3.Y < lead1.Y && lead1.X != lead2.X);
			// check if we need to swap texts (if leads are reversed, e.g. drawn right to
			// left)
			var rev = (lead1.X == lead2.X && lead1.Y < lead2.Y) || (lead1.Y == lead2.Y && lead1.X > lead2.X);

			// draw units
			var s1 = getShortUnitText(rev ? resistance2 : resistance1, "");
			var s2 = getShortUnitText(rev ? resistance1 : resistance2, "");
			g.setFont(unitsFont);
			g.setStrokeStyle(whiteColor);
			g.setFillStyle(whiteColor);
			var ya = (int) g.currentFontSize / 2;
			int w;
			w = (int)g.measureText(s1).Width;

			// vertical?
			if (lead1.X == lead2.X)
				g.fillText(s1,
					!reverseY ? arrowPoint.X + 2 : arrowPoint.X - 2 - w,
					Math.Max(arrow1.Y, arrow2.Y) + 5 + ya
				);
			else
				g.fillText(s1,
					Math.Min(arrow1.X, arrow2.X) - 2 - w,
					!reverseX ? arrowPoint.Y + 4 + ya : arrowPoint.Y - 4
				);

			w = (int)g.measureText(s2).Width;
			if (lead1.X == lead2.X)
				g.fillText(s2,
					!reverseY ? arrowPoint.X + 2 : arrowPoint.X - 2 - w,
					Math.Min(arrow1.Y, arrow2.Y) - 3
				);
			else
				g.fillText(s2,
					Math.Max(arrow1.X, arrow2.X) + 2,
					!reverseX ? arrowPoint.Y + 4 + ya : arrowPoint.Y - 4
				);
		}
	}
}
