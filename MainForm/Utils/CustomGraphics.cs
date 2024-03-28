using System.Drawing.Drawing2D;

public class CustomGraphics {
	protected readonly GraphicsState mState = new();
	protected readonly SizeF SInvalid = new(float.MinValue, float.MinValue);
	private readonly Stack<GraphicsState> mStateStack = new();
	private readonly Graphics? mG = null;
	private Pen mDrawP = Pens.Black;
	private Brush mFillB = Brushes.Black;

	protected CustomGraphics() { }

	public float currentFontSize => mState.font.Size;

	public CustomGraphics(Bitmap bmp) {
		mG = Graphics.FromImage(bmp);
	}

	#region public virtual
	public virtual void save() {
		mStateStack.Push(mState);
	}

	public virtual void restore() {
		if (mStateStack.Count > 0) {
			var state = mStateStack.Pop();
			state.CopyTo(mState);
			mDrawP.Color = mState.strokeColor;
			mDrawP.Width = mState.lineWidth;
			mDrawP.StartCap = mState.lineCap;
			mDrawP.EndCap = mState.lineCap;
			mFillB = new SolidBrush(mState.fillColor);
		}
	}

	public virtual void scale(float scaleX, float scaleY) {
		mState.scaleX = scaleX;
		mState.scaleY = scaleY;
		mState.SetMatrix(mG);
	}

	public virtual void rotate(float angle) {
		var c = (float)Math.Cos(angle) * mState.scaleX;
		var s = (float)Math.Sin(angle) * mState.scaleY;
		mState.matrix[0] = c;
		mState.matrix[1] = -s;
		mState.matrix[2] = s;
		mState.matrix[3] = c;
		mState.SetMatrix(mG);
	}

	public virtual void translate(float x, float y) {
		mState.matrix[4] = x;
		mState.matrix[5] = y;
		mState.SetMatrix(mG);
	}

	public virtual void transform(params float[] m) {
		mState.matrix[0] = m[0];
		mState.matrix[1] = m[1];
		mState.matrix[2] = m[2];
		mState.matrix[3] = m[3];
		mState.matrix[4] = m[4];
		mState.matrix[5] = m[5];
		mState.SetMatrix(mG);
	}

	public virtual void setLineWidth(float width) {
		mState.lineWidth = width;
		mDrawP.Width = width;
	}

	public virtual void setLineCap(LineCap cap) {
		mState.lineCap = cap;
		mDrawP.StartCap = cap;
		mDrawP.EndCap = cap;
	}

	public virtual void setStrokeStyle(Color color) {
		mState.strokeColor = color;
		mDrawP.Color = color;
	}

	public virtual void setStrokeStyle(LinearGradientBrush brush) {
		mState.strokeColor = brush.LinearColors[0];
		mDrawP.Brush = brush;
	}

	public virtual void setFillStyle(Color color) {
		mState.fillColor = color;
		mFillB = new SolidBrush(color);
	}

	public virtual void setFillStyle(LinearGradientBrush brush) {
		mState.fillColor = brush.LinearColors[0];
		mFillB = brush;
	}

	public virtual void setFont(Font font) {
		mState.font = font;
	}

	public virtual SizeF measureText(string text) {
		return mG?.MeasureString(text, mState.font) ?? SInvalid;
	}

	public virtual void drawLine(float x1, float y1, float x2, float y2) {
		mG?.DrawLine(mDrawP, x1, y1, x2, y2);
	}

	public virtual void drawRect(float x, float y, float width, float height) {
		mG?.DrawRectangle(mDrawP, x, y, width, height);
	}

	public virtual void drawArc(float cx, float cy, float width, float height, float startAngle, float sweepAngle) {
		mG?.DrawArc(mDrawP,
			cx - width * 0.5f, cy - height * 0.5f,
			width, height,
			startAngle * 180 / (float)Math.PI,
			sweepAngle * 180 / (float)Math.PI
		);
	}

	public virtual void drawPolygon(params PointF[] points) {
		mG?.DrawPolygon(mDrawP, points);
	}

	public virtual void fillRect(float x, float y, float width, float height) {
		mG?.FillRectangle(mFillB, x, y, width, height);
	}

	public virtual void fillCircle(float cx, float cy, float width, float height) {
		mG?.FillPie(mFillB,
			cx - width * 0.5f, cy - height * 0.5f,
			width, height,
			0,
			360
		);
	}

	public virtual void fillPolygon(params PointF[] points) {
		mG?.FillPolygon(mFillB, points);
	}

	public virtual void fillText(string text, float x, float y) {
		mG?.DrawString(text, mState.font, mFillB, x, y);
	}
	#endregion

	public static PointF[] CreateArc(float cx, float cy, float width, float height, float start = 0, float sweep = 360) {
		width *= 0.5f;
		height *= 0.5f;
		var poly = new PointF[Math.Max(width, height) < 4 ? 8 : 16];
		var sRad = Math.PI * start / 180;
		var ssweep = sweep / 360.0;
		for (int i = 0; i < poly.Length; i++) {
			var th = 2 * Math.PI * (i + 0.5) * ssweep / poly.Length + sRad;
			poly[i] = new PointF(
				(float)(cx + width * Math.Cos(th)),
				(float)(cy + height * Math.Sin(th))
			);
		}
		return poly;
	}

	public static int distanceSq(int x1, int y1, int x2, int y2) {
		x2 -= x1;
		y2 -= y1;
		return x2 * x2 + y2 * y2;
	}
}
