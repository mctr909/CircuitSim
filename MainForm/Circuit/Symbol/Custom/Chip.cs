using Circuit.Forms;
using Circuit.Elements.Custom;
using Circuit.Elements.Input;

namespace Circuit.Symbol.Custom {
	abstract class Chip : BaseSymbol {
		const int FLAG_SMALL = 1;
		protected const int FLAG_FLIP_X = 1024;
		protected const int FLAG_FLIP_Y = 2048;

		public const int SIDE_N = 0;
		public const int SIDE_S = 1;
		public const int SIDE_W = 2;
		public const int SIDE_E = 3;

		int csize;
		int cspc;
		int cspc2;

		PointF[] rectPoints;
		PointF[] clockPoints;
		public int sizeX;
		public int sizeY;

		public class Pin {
			Chip mSymbol;
			public int pos;
			public int side;
			public string text;

			public Point post;
			public Point stub;
			public Point textloc { get; private set; }

			public int voltSource;
			public Point bubblePos;

			public bool lineOver;
			public bool bubble;
			public bool clock;
			public bool output;
			public bool value;
			public bool state = true;
			public bool selected;
			public double curcount;
			public double current;

			public Pin(Chip chip, int p, int s, string t) {
				mSymbol = chip;
				pos = p;
				side = s;
				text = t;
			}

			public void setPoint(int px, int py, int dx, int dy, int dax, int day, int sx, int sy) {
				if ((mSymbol.mFlags & FLAG_FLIP_X) != 0) {
					dx = -dx;
					dax = -dax;
					px += mSymbol.cspc2 * (mSymbol.sizeX - 1);
					sx = -sx;
				}
				if ((mSymbol.mFlags & FLAG_FLIP_Y) != 0) {
					dy = -dy;
					day = -day;
					py += mSymbol.cspc2 * (mSymbol.sizeY - 1);
					sy = -sy;
				}
				int xa = px + mSymbol.cspc2 * dx * pos + sx;
				int ya = py + mSymbol.cspc2 * dy * pos + sy;
				post = new Point(xa + dax * mSymbol.cspc2, ya + day * mSymbol.cspc2);
				stub = new Point(xa + dax * mSymbol.cspc, ya + day * mSymbol.cspc);
				textloc = new Point(xa, ya);
				if (bubble) {
					bubblePos = new Point(xa + dax * 10 * mSymbol.csize, ya + day * 10 * mSymbol.csize);
				}
				if (clock) {
					mSymbol.clockPoints = new PointF[3];
					mSymbol.clockPoints[0] = new Point(
						xa + dax * mSymbol.cspc - dx * mSymbol.cspc / 2,
						ya + day * mSymbol.cspc - dy * mSymbol.cspc / 2
					);
					mSymbol.clockPoints[1] = new Point(xa, ya);
					mSymbol.clockPoints[2] = new Point(
						xa + dax * mSymbol.cspc + dx * mSymbol.cspc / 2,
						ya + day * mSymbol.cspc + dy * mSymbol.cspc / 2
					);
				}
			}

			/* convert position, side to a grid position (0=top left) so we can detect overlaps */
			int toGrid(int p, int s) {
				if (s == SIDE_N) {
					return p;
				}
				if (s == SIDE_S) {
					return p + mSymbol.sizeX * (mSymbol.sizeY - 1);
				}
				if (s == SIDE_W) {
					return p * mSymbol.sizeX;
				}
				if (s == SIDE_E) {
					return p * mSymbol.sizeX + mSymbol.sizeX - 1;
				}
				return -1;
			}

			public bool overlaps(int p, int s) {
				int g = toGrid(p, s);
				if (g == -1) {
					return true;
				}
				return toGrid(pos, side) == g;
			}

			public void fixName() {
				if (text.StartsWith("/")) {
					text = text.Substring(1);
					lineOver = true;
				}
				if (text.CompareTo("clk") == 0) {
					text = "";
					clock = true;
				}
			}
		}

		public Chip(Point pos) : base(pos) {
			Post.NoDiagonal = true;
			setSize(1);
		}

		public Chip(Point p1, Point p2, int f) : base(p1, p2, f) {
			Post.NoDiagonal = true;
			setSize(1);
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.INVALID; } }

		protected override void dump(List<object> optionList) {
			var ce = (ElmChip)Element;
			if (ce.NeedsBits) {
				optionList.Add(ce.Bits);
			}
			for (int i = 0; i != ce.TermCount; i++) {
				if (ce.Pins[i].state) {
					optionList.Add(ce.Volts[i].ToString("0.000000"));
				}
			}
		}

		protected void setSize(int s) {
			csize = s;
			cspc = 8 * s;
			cspc2 = cspc * 2;
			mFlags &= ~FLAG_SMALL;
			mFlags |= (s == 1) ? FLAG_SMALL : 0;
		}

		protected void Setup(ElmChip elm, StringTokenizer st) {
			for (int i = 0; i != elm.TermCount; i++) {
				if (elm.Pins == null) {
					elm.Volts[i] = st.nextTokenDouble();
				} else if (elm.Pins[i].state) {
					elm.Volts[i] = st.nextTokenDouble();
					elm.Pins[i].value = elm.Volts[i] > 2.5;
				}
			}
		}

		public override void Draw(CustomGraphics g) {
			drawChip(g);
		}

		public void drawChip(CustomGraphics g) {
			var ce = (ElmChip)Element;
			for (int i = 0; i != ce.TermCount; i++) {
				var p = ce.Pins[i];
				var a = p.post;
				var b = p.stub;
				DrawLine(a, b);
				UpdateDotCount(p.current, ref p.curcount);
				DrawCurrent(b, a, p.curcount);
				if (p.bubble) {
					DrawCircle(p.bubblePos, 1);
					DrawCircle(p.bubblePos, 3);
				}
				var bkSize = g.FontSize;
				g.FontSize = 12 * csize;
				while (true) {
					int txtW = (int)g.GetTextSize(p.text).Width;
					/* scale font down if it's too big */
					if (12 * csize < txtW) {
						g.FontSize -= 0.5f;
						continue;
					}
					DrawCenteredText(p.text, p.textloc);
					if (p.lineOver) {
						int ya = p.textloc.Y;
						DrawLine(p.textloc.X - txtW / 2, ya, p.textloc.X + txtW / 2, ya);
					}
					break;
				}
				g.FontSize = bkSize;
			}
			DrawPolygon(rectPoints);
			if (clockPoints != null) {
				DrawPolygon(clockPoints);
			}
		}

		public override void Drag(Point pos) {
			pos = SnapGrid(pos);
			if (pos.X < Post.A.X) {
				pos.X = Post.A.X;
				pos.Y = Post.A.Y;
			} else {
				Post.SetPosition(
					Post.A.X, pos.Y,
					SnapGrid(pos.X), pos.Y
				);
			}
			SetPoints();
		}

		public override void SetPoints() {
			var ce = (ElmChip)Element;
			clockPoints = null;
			int hs = cspc;
			int x0 = Post.A.X + cspc2;
			int y0 = Post.A.Y;
			var r = new Point(x0 - cspc, y0 - cspc);
			int xs = sizeX * cspc2;
			int ys = sizeY * cspc2;
			rectPoints = new PointF[] {
				new Point(r.X, r.Y),
				new Point(r.X + xs, r.Y),
				new Point(r.X + xs, r.Y + ys),
				new Point(r.X, r.Y + ys)
			};
			for (int i = 0; i != ce.TermCount; i++) {
				var p = ce.Pins[i];
				switch (p.side) {
				case SIDE_N:
					p.setPoint(x0, y0, 1, 0, 0, -1, 0, 0);
					break;
				case SIDE_S:
					p.setPoint(x0, y0, 1, 0, 0, 1, 0, ys - cspc2);
					break;
				case SIDE_W:
					p.setPoint(x0, y0, 0, 1, -1, 0, 0, 0);
					break;
				case SIDE_E:
					p.setPoint(x0, y0, 0, 1, 1, 0, xs - cspc2, 0);
					break;
				}
			}
		}

		/* see if we can move pin to position xp, yp, and return the new position */
		public bool getPinPos(int xp, int yp, int pin, int[] pos) {
			var ce = (ElmChip)Element;
			int x0 = Post.A.X + cspc2;
			int y0 = Post.A.Y;
			int xr = x0 - cspc;
			int yr = y0 - cspc;
			double xd = (xp - xr) / (double)cspc2 - .5;
			double yd = (yp - yr) / (double)cspc2 - .5;
			if (xd < .25 && yd > 0 && yd < sizeY - 1) {
				pos[0] = (int)Math.Max(Math.Round(yd), 0);
				pos[1] = SIDE_W;
			} else if (xd > sizeX - .75) {
				pos[0] = (int)Math.Min(Math.Round(yd), sizeY - 1);
				pos[1] = SIDE_E;
			} else if (yd < .25) {
				pos[0] = (int)Math.Max(Math.Round(xd), 0);
				pos[1] = SIDE_N;
			} else if (yd > sizeY - .75) {
				pos[0] = (int)Math.Min(Math.Round(xd), sizeX - 1);
				pos[1] = SIDE_S;
			} else {
				return false;
			}

			if (pos[0] < 0) {
				return false;
			}
			if ((pos[1] == SIDE_N || pos[1] == SIDE_S) && pos[0] >= sizeX) {
				return false;
			}
			if ((pos[1] == SIDE_W || pos[1] == SIDE_E) && pos[0] >= sizeY) {
				return false;
			}

			for (int i = 0; i != ce.TermCount; i++) {
				if (pin == i) {
					continue;
				}
				if (ce.Pins[i].overlaps(pos[0], pos[1])) {
					return false;
				}
			}
			return true;
		}

		public override void GetInfo(string[] arr) {
			var ce = (ElmChip)Element;
			arr[0] = getChipName();
			int a = 1;
			for (int i = 0; i != ce.TermCount; i++) {
				var p = ce.Pins[i];
				if (arr[a] != null) {
					arr[a] += "; ";
				} else {
					arr[a] = "";
				}
				string t = p.text;
				if (p.lineOver) {
					t += '\'';
				}
				if (p.clock) {
					t = "Clk";
				}
				arr[a] += t + " = " + TextUtils.Voltage(ce.Volts[i]);
				if (i % 2 == 1) {
					a++;
				}
			}
		}

		string getChipName() { return "chip"; }

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c != 0) {
				return null;
			}
			if (r == 0) {
				return new ElementInfo("Flip X", (mFlags & FLAG_FLIP_X) != 0);
			}
			if (r == 1) {
				return new ElementInfo("Flip Y", (mFlags & FLAG_FLIP_Y) != 0);
			}
			return null;
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			if (n == 0) {
				if (ei.CheckBox.Checked) {
					mFlags |= FLAG_FLIP_X;
				} else {
					mFlags &= ~FLAG_FLIP_X;
				}
				SetPoints();
			}
			if (n == 1) {
				if (ei.CheckBox.Checked) {
					mFlags |= FLAG_FLIP_Y;
				} else {
					mFlags &= ~FLAG_FLIP_Y;
				}
				SetPoints();
			}
		}
	}
}
