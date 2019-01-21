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
	public partial class ImagePreviewControl : Control
	{
		Bitmap bmp = null;

		public ImagePreviewControl()
		{
			InitializeComponent();
		}

		public void Draw(Graphics g = null, bool clear = false)
		{
			if (bmp is null)
				return;

			if (g is null)
				g = CreateGraphics();

			if (clear)
				g.Clear(Color.FromArgb(0, 0, 0));
			g.DrawImage(bmp, (ClientSize.Width - bmp.Width) / 2, (ClientSize.Height - bmp.Height) / 2);

			g.Dispose();
		}

		protected override void OnPaint(PaintEventArgs pe)
		{
			//base.OnPaint(pe);

			Draw(pe.Graphics);
		}

		protected override void OnPaintBackground(PaintEventArgs pevent)
		{
		}

		public Bitmap Img
		{
			set
			{
				if (value != null)
					bmp = new Bitmap(value);
			}
			get
			{
				return bmp;
			}
		}
	}
}
