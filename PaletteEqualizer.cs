using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DicomI
{
	public partial class PaletteEqualizer : Form
	{
		public PaletteEq paletteEq = null;
		Bitmap bufferBmp = null;

		OpenFileDialog ofd = new OpenFileDialog();
		SaveFileDialog sfd = new SaveFileDialog();

		int mx0 = 0, my0 = 0;

		public PaletteEqualizer()
		{
			InitializeComponent();

			ofd.Title = "Import Palette";
			ofd.Multiselect = false;
			ofd.CheckPathExists = true;
			ofd.CheckFileExists = true;

			sfd.Title = "Export Palette";
			sfd.OverwritePrompt = true;
			sfd.CheckPathExists = true;
			sfd.ValidateNames = true;
			sfd.FileName = "CustomPalette.config";

			this.buttonSave.DialogResult = DialogResult.OK;
		}

		void InitControls()
		{
			comboBoxChannel.SelectedIndex = (int)paletteEq.Channel;
			checkBoxLink.CheckState = paletteEq.LinkChannels ? CheckState.Checked : CheckState.Unchecked;

			userControlEq.Invalidate();
		}

		private void PaletteEqualizer_Load(object sender, EventArgs e)
		{
			InitControls();
		}

		private void userControlEq_Paint(object sender, PaintEventArgs e)
		{
			Graphics gr = e.Graphics;

			if (bufferBmp is null)
				bufferBmp = new Bitmap(ClientRectangle.Width, ClientRectangle.Height);
			Graphics g = Graphics.FromImage(bufferBmp);
			g.Clear(BackColor);

			float w = userControlEq.ClientSize.Width;
			float h = userControlEq.ClientSize.Height;

			float stepX = w / 8.0f;
			float stepY = h / 8.0f;

			Pen pen = new Pen(Color.FromArgb(200, 200, 200), 1.0f);
			for (int i = 1; i < 8; i++)
			{
				g.DrawLine(pen, i * stepX, 0, i * stepX, h);
				g.DrawLine(pen, 0, i * stepY, w, i * stepY);
			}

			stepX = w / 256;
			stepY = h / 256;

			Pen penR = new Pen(Color.FromArgb(200, 0, 0), 2.0f);
			Pen penG = new Pen(Color.FromArgb(0, 200, 0), 2.0f);
			Pen penB = new Pen(Color.FromArgb(0, 0, 200), 2.0f);
			Pen penRGB = new Pen(Color.FromArgb(100, 100, 100), 2.0f);
			for (int i = 0; i < 255; i++)
			{
				if (!paletteEq.LinkChannels)
				{
					if (paletteEq.Channel == ColorChannel.Red)
					{
						g.DrawLine(penG, i * stepX, h - paletteEq.GetValue(ColorChannel.Green, i) * stepY, (i + 1) * stepX, h - paletteEq.GetValue(ColorChannel.Green, i + 1) * stepY);
						g.DrawLine(penB, i * stepX, h - (paletteEq.GetValue(ColorChannel.Blue, i) * stepY), (i + 1) * stepX, h - paletteEq.GetValue(ColorChannel.Blue, i + 1) * stepY);
						g.DrawLine(penR, i * stepX, h - paletteEq.GetValue(ColorChannel.Red, i) * stepY, (i + 1) * stepX, h - paletteEq.GetValue(ColorChannel.Red, i + 1) * stepY);
					}
					else if (paletteEq.Channel == ColorChannel.Green)
					{
						g.DrawLine(penB, i * stepX, h - paletteEq.GetValue(ColorChannel.Blue, i) * stepY, (i + 1) * stepX, h - paletteEq.GetValue(ColorChannel.Blue, i + 1) * stepY);
						g.DrawLine(penR, i * stepX, h - (paletteEq.GetValue(ColorChannel.Red, i) * stepY), (i + 1) * stepX, h - paletteEq.GetValue(ColorChannel.Red, i + 1) * stepY);
						g.DrawLine(penG, i * stepX, h - paletteEq.GetValue(ColorChannel.Green, i) * stepY, (i + 1) * stepX, h - paletteEq.GetValue(ColorChannel.Green, i + 1) * stepY);
					}
					else
					{
						g.DrawLine(penR, i * stepX, h - paletteEq.GetValue(ColorChannel.Red, i) * stepY, (i + 1) * stepX, h - paletteEq.GetValue(ColorChannel.Red, i + 1) * stepY);
						g.DrawLine(penG, i * stepX, h - (paletteEq.GetValue(ColorChannel.Green, i) * stepY), (i + 1) * stepX, h - paletteEq.GetValue(ColorChannel.Green, i + 1) * stepY);
						g.DrawLine(penB, i * stepX, h - paletteEq.GetValue(ColorChannel.Blue, i) * stepY, (i + 1) * stepX, h - paletteEq.GetValue(ColorChannel.Blue, i + 1) * stepY);
					}
				}
				else
				{
					g.DrawLine(penRGB, i * stepX, h - paletteEq.GetValue(ColorChannel.Red, i) * stepY, (i + 1) * stepX, h - paletteEq.GetValue(ColorChannel.Red, i + 1) * stepY);
				}
			}

			gr.DrawImage(bufferBmp, 0, 0);

			g.Dispose();
		}

		void MouseEvent(int mx, int my)
		{
			float w = userControlEq.ClientSize.Width;
			float h = userControlEq.ClientSize.Height;

			if (mx < 0)
				mx = 0;
			else if (mx > w - 1)
				mx = (int)(w - 1);
			if (my < 0)
				my = 0;
			else if (my > h - 1)
				my = (int)(h - 1);

			int x = (int)(mx / w * 256.0f);
			int y = 255 - (int)(my / h * 256.0f);

			paletteEq.SetValue(paletteEq.Channel, x, (byte)y);
		}

		private void userControlEq_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				mx0 = e.X;
				my0 = e.Y;

				userControlEq.Capture = true;

				MouseEvent(e.X, e.Y);

				userControlEq.Invalidate(false);
			}
		}

		private void userControlEq_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				mx0 = 0;
				my0 = 0;

				userControlEq.Capture = false;
			}
		}

		private void userControlEq_MouseMove(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				if (!userControlEq.Capture)
					return;

				int dx = e.X - mx0;
				int dy = e.Y - my0;

				int count = dx > 0 ? dx : -dx;

				for (int i = 0; i < count; i++)
				{
					MouseEvent((int)(mx0 + dx * i / count + 0.5), (int)(my0 + dy * i / count + 0.5));
				}

				mx0 = e.X;
				my0 = e.Y;

				userControlEq.Capture = true;

				userControlEq.Invalidate(false);
			}
		}

		private void comboBoxChannel_SelectedIndexChanged(object sender, EventArgs e)
		{
			paletteEq.Channel = (ColorChannel)comboBoxChannel.SelectedIndex;

			userControlEq.Invalidate();
		}

		private void checkBoxLink_CheckedChanged(object sender, EventArgs e)
		{
			paletteEq.LinkChannels = checkBoxLink.CheckState == CheckState.Checked ? true : false;

			userControlEq.Invalidate();
		}

		private void buttonReset_Click(object sender, EventArgs e)
		{
			paletteEq.SetDefault();

			userControlEq.Invalidate();
		}

		private void buttonImport_Click(object sender, EventArgs e)
		{
			if (ofd.ShowDialog(this) == DialogResult.OK)
			{
				paletteEq.Load(ofd.FileName);
				ofd.FileName = "";

				InitControls();
				userControlEq.Invalidate();
			}
		}

		private void buttonExport_Click(object sender, EventArgs e)
		{
			if (sfd.ShowDialog(this) == DialogResult.OK)
			{
				paletteEq.Save(sfd.FileName);
				sfd.FileName = "CustomPalette.config";
			}
		}

		private void buttonSave_Click(object sender, EventArgs e)
		{
			paletteEq.Save();
		}
	}
}
