using System.Drawing.Drawing2D;

public class CustomGraphics {
	public static CustomGraphics Instance;

	static readonly StringFormat mAlignLeft = new()
	{
		Alignment = StringAlignment.Near,
		LineAlignment = StringAlignment.Center
	};
	static readonly StringFormat mAlignRight = new()
	{
		Alignment = StringAlignment.Far,
		LineAlignment = StringAlignment.Center
	};
	static readonly StringFormat mAlignCenter = new()
	{
		Alignment = StringAlignment.Center,
		LineAlignment = StringAlignment.Center
	};

	static Pen mPenPost = new(Color.Green, 5.0f);
	static Pen mPenHandle = new(Color.FromArgb(95, 0, 255, 255), 1.0f);

	protected static Font mFontL = new("Arial", 11.0f);

	protected Bitmap mImage;

	Graphics mG;

	Pen mPenLine = new(Color.White, 1.0f)
	{
		StartCap = LineCap.Triangle,
		EndCap = LineCap.Triangle,
		Width = 0.1f
	};
	Pen mPenFill = new(Color.White, 1.0f);
	Font mFont = new("Arial", 9.0f);
	Brush mFontBrush = Brushes.Black;
	Color mFontColor;

	public const float POST_RADIUS = 2.5f;
	public const float HANDLE_RADIUS = 6.5f;

	public static Color SelectColor { get; private set; }
	public static Color WhiteColor { get; private set; }
	public static Color LineColor { get; private set; }
	public static Color PostColor {
		get { return mPenPost.Color; }
		private set { mPenPost.Color = value; }
	}
	public static Color TextColor { get; private set; }
	public static float LTextSize {
		get { return mFontL.Size; }
		set { mFontL = new Font(mFontL.Name, value); }
	}

	public bool DrawPDF { get; set; } = false;

	public virtual Color DrawColor {
		get { return mPenLine.Color; }
		set { mPenLine.Color = value; }
	}
	public virtual Color FillColor {
		get { return mPenFill.Color; }
		set { mPenFill.Color = value; }
	}
	public Color FontColor {
		get { return mFontColor; }
		set {
			var p = new Pen(value, 1.0f);
			mFontBrush = p.Brush;
			mFontColor = value;
		}
	}
	public float FontSize {
		get { return mFont.Size; }
		set { mFont = new Font(mFont.Name, value); }
	}

	public int Width { get; private set; }
	public int Height { get; private set; }

	CustomGraphics() { }

	public CustomGraphics(int width, int height) {
		Width = width;
		Height = height;
		mImage = new Bitmap(Width, Height);
		mG = Graphics.FromImage(mImage);
	}

	public CustomGraphics(Bitmap image) {
		Width = image.Width;
		Height = image.Height;
		mImage = image;
		mG = Graphics.FromImage(mImage);
	}

	public void CopyTo(Graphics g) {
		g.DrawImage(mImage, 0, 0);
	}

	public void Dispose() {
		mG.Dispose();
		mG = null;
		if (null != mImage) {
			mImage.Dispose();
			mImage = null;
		}
	}

	public static void SetColor(bool isPrintable) {
		if (isPrintable) {
			WhiteColor = Color.Gray;
			LineColor = Color.Black;
			SelectColor = Color.Black;
			PostColor = Color.Black;
			TextColor = Color.Black;
			mPenHandle.Color = Color.Black;
		} else {
			WhiteColor = Color.FromArgb(191, 191, 191);
			LineColor = Color.FromArgb(95, 95, 95);
			SelectColor = Color.FromArgb(0, 255, 255);
			PostColor = Color.FromArgb(0, 127, 0);
			TextColor = Color.FromArgb(147, 147, 147);
			mPenHandle.Color = Color.FromArgb(95, 0, 255, 255);
		}
	}

	public static CustomGraphics FromImage(int width, int height) {
		return new CustomGraphics(width, height);
	}

	public virtual void DrawLeftText(string s, float x, float y) {
		mG.DrawString(s, mFont, mFontBrush, x, y, mAlignLeft);
	}

	public virtual void DrawRightText(string s, float x, float y) {
		mG.DrawString(s, mFont, mFontBrush, x, y, mAlignRight);
	}

	public virtual void DrawCenteredText(string s, PointF p, double rotateAngle) {
		var rot = (float)(rotateAngle * 180 / Math.PI);
		var mat = mG.Transform;
		mG.TranslateTransform(p.X, p.Y);
		mG.RotateTransform(rot);
		mG.DrawString(s, mFont, mFontBrush, 0, 0, mAlignCenter);
		mG.Transform = mat;
	}

	public virtual void DrawCenteredLText(string s, PointF p) {
		mG.DrawString(s, mFontL, mFontBrush, p.X, p.Y + 1, mAlignCenter);
	}

	public virtual void DrawPost(PointF p) {
		mG.FillPie(mPenPost.Brush,
			p.X - POST_RADIUS, p.Y - POST_RADIUS,
			POST_RADIUS * 2, POST_RADIUS * 2,
			0, 360
		);
	}

	public virtual void DrawHandle(PointF p) {
		mG.FillPie(mPenHandle.Brush,
			p.X - HANDLE_RADIUS, p.Y - HANDLE_RADIUS,
			HANDLE_RADIUS * 2, HANDLE_RADIUS * 2,
			0, 360
		);
	}

	public virtual void DrawCurrent(float cx, float cy, float radius) {
		mG.FillPie(Brushes.Snow, cx - radius, cy - radius, radius * 2, radius * 2, 0, 360);
	}

	public virtual void DrawLine(float ax, float ay, float bx, float by) {
		mG.DrawLine(mPenLine, ax, ay, bx, by);
	}

	public virtual void DrawLine(PointF a, PointF b) {
		mG.DrawLine(mPenLine, a, b);
	}

	public virtual void DrawRectangle(Rectangle rect) {
		mG.DrawRectangle(mPenLine, rect);
	}

	public virtual void DrawDashRectangle(float x, float y, float w, float h) {
		mPenLine.DashStyle = DashStyle.Dash;
		mPenLine.DashPattern = new float[] { 2, 3 };
		mG.DrawRectangle(mPenLine, x, y, w, h);
		mPenLine.DashStyle = DashStyle.Solid;
	}

	public virtual void DrawCircle(PointF p, float radius) {
		mG.DrawArc(mPenLine, p.X - radius, p.Y - radius, radius * 2, radius * 2, 0, 360);
	}

	public virtual void DrawArc(PointF p, float diameter, float start, float sweep) {
		var md = diameter * .98f;
		mG.DrawArc(mPenLine, p.X - md / 2, p.Y - md / 2, md, md, start, sweep);
	}

	public virtual void DrawPolyline(PointF[] p) {
		mG.DrawLines(mPenLine, p);
	}

	public virtual void DrawPolygon(PointF[] p) {
		mG.DrawPolygon(mPenLine, p);
	}

	public virtual void FillRectangle(int x, int y, int width, int height) {
		mG.FillRectangle(mPenFill.Brush, x, y, width, height);
	}

	public virtual void FillCircle(float cx, float cy, float radius) {
		mG.FillPie(mPenFill.Brush, cx - radius, cy - radius, radius * 2, radius * 2, 0, 360);
	}

	public virtual void FillPolygon(PointF[] p) {
		mG.FillPolygon(mPenFill.Brush, p);
	}

	public virtual void ScrollCircuit(Point p) {
		mG.Transform = new Matrix(1, 0, 0, 1, p.X, p.Y);
	}

	public virtual void SetPlotPos(Point p) {
		mG.Transform = new Matrix(1, 0, 0, 1, p.X, p.Y);
	}

	public virtual void ClearTransform() {
		mG.Transform = new Matrix(1, 0, 0, 1, 0, 0);
	}

	public SizeF GetTextSize(string s) {
		return mG.MeasureString(s, mFont);
	}

	public SizeF GetTextSizeL(string s) {
		return mG.MeasureString(s, mFontL);
	}

	public void Clear(Color color) {
		mG.Clear(color);
	}
}
