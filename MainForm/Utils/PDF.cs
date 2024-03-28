class PDF {
	const string FontName = "Arial";
	List<PdfPage> mPageList = [];

	public void AddPage(PdfPage page) {
		mPageList.Add(page);
	}

	public void Save(string path) {
		if (string.IsNullOrEmpty(path) || !Directory.Exists(Path.GetDirectoryName(path))) {
			return;
		}
		var fs = new FileStream(path, FileMode.Create);
		var sw = new StreamWriter(fs)
		{
			NewLine = "\n"
		};
		sw.WriteLine("%PDF-1.7");
		sw.Flush();
		fs.WriteByte(0xE2);
		fs.WriteByte(0xE3);
		fs.WriteByte(0xCF);
		fs.WriteByte(0xD3);
		sw.WriteLine();
		sw.WriteLine("1 0 obj");
		sw.WriteLine("<<");
		sw.WriteLine("  /Type /Catalog");
		sw.WriteLine("  /Pages 2 0 R");
		sw.WriteLine(">>");
		sw.WriteLine("endobj");
		sw.WriteLine();
		sw.WriteLine("2 0 obj");
		sw.WriteLine("<<");
		sw.WriteLine("  /Type /Pages");
		sw.Write("  /Kids [");
		for (int pIdx = 0; pIdx < mPageList.Count; pIdx++) {
			sw.Write("{0} 0 R ", pIdx + 4);
		}
		sw.WriteLine("]");
		sw.WriteLine("  /Count {0}", mPageList.Count);
		sw.WriteLine(">>");
		sw.WriteLine("endobj");
		sw.WriteLine();
		sw.WriteLine("3 0 obj");
		sw.WriteLine("<<");
		sw.WriteLine("  /Font <<");
		sw.WriteLine("    /F0 <<");
		sw.WriteLine("      /Type /Font");
		sw.WriteLine("      /BaseFont /{0}", FontName);
		sw.WriteLine("      /Subtype /Type1");
		sw.WriteLine("    >>");
		sw.WriteLine("  >>");
		sw.WriteLine(">>");
		sw.WriteLine("endobj");
		sw.WriteLine();
		for (int pIdx = 0; pIdx < mPageList.Count; pIdx++) {
			var page = mPageList[pIdx];
			sw.WriteLine("{0} 0 obj", pIdx + 4);
			sw.WriteLine("<<");
			sw.WriteLine("  /Type /Page");
			sw.WriteLine("  /Parent 2 0 R");
			sw.WriteLine("  /Resources 3 0 R");
			sw.WriteLine("  /MediaBox [0 0 {0} {1}]", page.Width, page.Height);
			sw.WriteLine("  /Contents {0} 0 R", mPageList.Count + pIdx + 4);
			sw.WriteLine(">>");
			sw.WriteLine("endobj");
			sw.WriteLine();
		}
		for (int pIdx = 0; pIdx < mPageList.Count; pIdx++) {
			sw.WriteLine("{0} 0 obj", mPageList.Count + pIdx + 4);
			sw.Flush();
			mPageList[pIdx].Flush(fs);
			sw.WriteLine("endobj");
			sw.WriteLine();
		}
		sw.WriteLine("xref");
		sw.WriteLine("trailer");
		sw.WriteLine("<<");
		sw.WriteLine("  /Size {0}", mPageList.Count * 2 + 4);
		sw.WriteLine("  /Root 1 0 R");
		sw.WriteLine(">>");
		sw.WriteLine("startxref");
		sw.WriteLine("0");
		sw.WriteLine("%%EOF");
		sw.Close();
		sw.Dispose();
	}
}
