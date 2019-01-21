using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DicomI
{
	public partial class ImagePreview : Form
	{
		public event EventHandler<BitmapTransform> TransformBitmap;
		Bitmap bmpOrig = null;

		public ImagePreview()
		{
			InitializeComponent();
		}

		public void DrawBitmap(bool needOrigImg = false)
		{
			if (imagePreviewControl1.Img is null)
				needOrigImg = true;

			BitmapTransform bt = new BitmapTransform();
			bt.bmp = needOrigImg ? null : imagePreviewControl1.Img;
			bt.bmpOrig = bmpOrig;
			bt.sz = imagePreviewControl1.Size;
			bt.usePalette = false;

			TransformBitmap(this, bt);

			if (needOrigImg)
			{ 
				imagePreviewControl1.Img = bt.bmp;
				bmpOrig = bt.bmpOrig;
			}

			imagePreviewControl1.Draw(null, needOrigImg);
		}

		protected override void OnPaintBackground(PaintEventArgs pevent)
		{
		}
	}

	public class BitmapTransform
	{
		public Bitmap bmp = null;
		public Bitmap bmpOrig = null;
		public Size sz = new Size();
		public bool usePalette = false;
	}
}
