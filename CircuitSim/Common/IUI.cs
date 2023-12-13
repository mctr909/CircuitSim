using Circuit.UI;
using System;
using System.Drawing;

namespace Circuit {
	public interface IUI {
		DUMP_ID DumpId { get; }
		string ReferenceName { get; set; }
		IElement Elm { get; set; }
		Post Post { get; set; }
		bool IsSelected { get; set; }
		bool IsMouseElm { get; }
		bool NeedsHighlight { get; }
		/// <summary>
		/// called when an element is done being dragged out;
		/// </summary>
		/// <returns>returns true if it's zero size and should be deleted</returns>
		bool IsCreationFailed { get; }
		bool CanViewInScope { get; }

		double DistancePostA(Point p);
		double DistancePostB(Point p);
		/// <summary>
		/// dump component state for export/undo
		/// </summary>
		string Dump();
		/// <summary>
		/// this is used to set the position of an internal element so we can draw it inside the parent
		/// </summary>
		/// <param name="ax"></param>
		/// <param name="ay"></param>
		/// <param name="bx"></param>
		/// <param name="by"></param>
		void SetPosition(int ax, int ay, int bx, int by);
		void Move(int dx, int dy);
		void Move(int dx, int dy, EPOST n);
		/// <summary>
		/// determine if moving this element by (dx,dy) will put it on top of another element
		/// </summary>
		/// <param name="dx"></param>
		/// <param name="dy"></param>
		/// <returns></returns>
		bool AllowMove(int dx, int dy);
		void FlipPosts();
		void SetMouseElm(bool v);

		double Distance(Point p);
		void Delete();
		void Draw(CustomGraphics g);
		/// <summary>
		/// draw second point to xx, yy
		/// </summary>
		/// <param name="pos"></param>
		void Drag(Point pos);
		void SelectRect(RectangleF r);
		/// <summary>
		/// calculate post locations and other convenience values used for drawing.
		/// Called when element is moved
		/// </summary>
		void SetPoints();
		/// <summary>
		/// get component info for display in lower right
		/// </summary>
		/// <param name="arr"></param>
		void GetInfo(string[] arr);
		ElementInfo GetElementInfo(int r, int c);
		void SetElementValue(int r, int c, ElementInfo ei);
		EventHandler CreateSlider(ElementInfo ei, Adjustable adj);
	}
}
