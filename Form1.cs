using Dicom;
using Dicom.Imaging;
using SharpGL;
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
	public partial class DicomI : Form
	{
		Scene scene = new Scene();

		AboutBox aboutDlg = new AboutBox();
		Usage usageDlg = new Usage();
		FileBrowser fb = null;
		ImagePreview imagePreview = null;

		public DicomI()
		{
			InitializeComponent();
		}

		private void DicomI_DragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
				e.Effect = DragDropEffects.All;
			else
				e.Effect = DragDropEffects.None;
		}

		void OpenFile(string path)
		{
			string[] arr = new string[1];
			arr[0] = path;
			OpenFile(arr);
		}

		void OpenFile(string[] path)
		{
			if (path.Length == 1)
			{
				try
				{
					string dir = path[0];
					if (File.Exists(dir) && dir.ToLower().EndsWith("dicomdir"))
						dir = Path.GetDirectoryName(dir);
					if (File.GetAttributes(dir).HasFlag(FileAttributes.Directory))
					{
						OpenBrowser(dir);
						return;
					}
				}
				catch
				{
				}
			}

			List<Bitmap> bmp = new List<Bitmap>();
			DicomImage img = null;
			string lastDir = "";
			for (int i = 0; i < path.Length; i++)
			{
				try
				{
					string dir = path[i];
					int frame = 0;
					if (dir.StartsWith("dicom:frame:"))
					{
						string[] arr = dir.Split(new char[] { ':' }, 4);
						if (arr.Length == 4)
						{
							frame = int.Parse(arr[2]);
							dir = arr[3];
						}
					}
					if (DicomFile.HasValidHeader(dir))
					{
						if (img is null || dir != lastDir)
						{
							img = new DicomImage(dir);
							lastDir = dir;
						}
						bmp.Add(new Bitmap(img.RenderImage(frame).AsBitmap()));
					}
					else
					{
						bmp.Add(new Bitmap(path[i]));
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}

			try
			{
				scene.Add(bmp.ToArray(), openGLControl1);
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void DicomI_DragDrop(object sender, DragEventArgs e)
		{
			Console.WriteLine("Drop");

			string[] fileList = (string[])e.Data.GetData(DataFormats.FileDrop, false);
			if (fileList.Length == 0)
				return;

			OpenFile(fileList);
		}

		void ShowPaletteDialog()
		{
			PaletteEqualizer pe = new PaletteEqualizer();
			pe.paletteEq = new PaletteEq(scene.paletteEq);
			if (pe.ShowDialog(this) == DialogResult.OK)
			{
				scene.paletteEq = new PaletteEq(pe.paletteEq);
				scene.Create(null, null, false);

				openGLControl1.Invalidate();
			}
		}

		private void openGLControl1_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Multiply)
				scene.ChangeDisplayMode();
			else if (e.KeyCode == Keys.Add)
				scene.ChangeSize(true);
			else if (e.KeyCode == Keys.Subtract)
				scene.ChangeSize(false);
			else if (e.KeyCode == Keys.Divide)
				scene.UseEq = scene.UseEq ? false : true;
			else if (e.KeyCode == Keys.P)
				ShowPaletteDialog();
			else if (e.KeyCode == Keys.Z)
				scene.ChangeInterlacing(true);
			else if (e.KeyCode == Keys.A)
				scene.ChangeInterlacing(false);
			else if (e.KeyCode == Keys.C)
				scene.Clear();
			else if (e.KeyCode == Keys.X)
				scene.Create(null, null, true);
			else if (e.KeyCode == Keys.B)
				OpenBrowser();
			else if (e.KeyCode == Keys.V)
				scene.ResetColorTransform();
		}

		void ShowImagePreview()
		{
			if (!scene.IsInit)
				return;

			bool needOrigImg = false;
			if (imagePreview is null)
			{
				imagePreview = new ImagePreview();
				imagePreview.TransformBitmap += scene.imagePreview_BitmapTransform;
				imagePreview.Show();
				needOrigImg = true;
			}
			else if (!imagePreview.Visible)
			{
				imagePreview.Visible = true;
				needOrigImg = true;
				imagePreview.BringToFront();
			}
			imagePreview.DrawBitmap(needOrigImg);
		}

		private void openGLControl1_MouseDown(object sender, MouseEventArgs e)
		{
			openGLControl1.Capture = true;

			scene.InitXY(e.X, e.Y);
		}

		private void openGLControl1_MouseMove(object sender, MouseEventArgs e)
		{
			if (!openGLControl1.Capture)
				return;

			//openGLControl1.Capture = true;

			if (e.Button == MouseButtons.Left)
			{
				if (ModifierKeys == Keys.Control)
				{
					scene.RecalcXY(e.X, e.Y);
					scene.ChangeBlackTolerance();
					ShowImagePreview();
				}
				else if (ModifierKeys == Keys.Shift)
				{
					scene.RecalcXY(e.X, e.Y);
					scene.ChangeBrightness();
					ShowImagePreview();
				}
				else
					scene.Rotate(e.X, e.Y);
			}
			else if (e.Button == MouseButtons.Right)
			{
				if (ModifierKeys == Keys.Control)
				{
					scene.RecalcXY(e.X, e.Y);
					scene.ChangeWhiteTolerance();
					ShowImagePreview();
				}
				else if (ModifierKeys == Keys.Shift)
				{
					scene.RecalcXY(e.X, e.Y);
					scene.ChangeContrast();
					ShowImagePreview();
				}
				else
					scene.Translate(e.X, e.Y);
			}
		}

		private void openGLControl1_MouseUp(object sender, MouseEventArgs e)
		{
			openGLControl1.Capture = false;

			if (imagePreview != null)
			{
				if (imagePreview.Visible)
				{
					imagePreview.Visible = false;
					scene.Create(null, null, false);
					openGLControl1.Invalidate();
				}
			}
		}

		private void openGLControl1_OpenGLDraw(object sender, RenderEventArgs e)
		{
			scene.Draw();
		}

		private void openGLControl1_Resized(object sender, EventArgs e)
		{
			scene.Resize();
		}

		private void DicomI_Load(object sender, EventArgs e)
		{
			string[] s = Environment.GetCommandLineArgs();
			if (s.Length > 1)
			{
				string[] path = new string[s.Length - 1];
				Array.Copy(s, 1, path, 0, s.Length - 1);
				OpenFile(path);
			}
		}

		private void openGLControl1_MouseWheel(object sender, MouseEventArgs e)
		{
			scene.Scale(e.Delta);
		}

		private void thinerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			scene.ChangeSize(false);
		}

		private void thickerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			scene.ChangeSize(true);
		}

		private void increaseDetalizationToolStripMenuItem_Click(object sender, EventArgs e)
		{
			scene.ChangeInterlacing(false);
		}

		private void decreaseDetalizationToolStripMenuItem_Click(object sender, EventArgs e)
		{
			scene.ChangeInterlacing(true);
		}

		private void fillToolStripMenuItem_Click(object sender, EventArgs e)
		{
			scene.DisplayMode = DisplayMode.Fill;
		}

		private void linesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			scene.DisplayMode = DisplayMode.Lines;
		}

		private void pointsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			scene.DisplayMode = DisplayMode.Points;
		}

		private void usepaletteToolStripMenuItem_Click(object sender, EventArgs e)
		{
			scene.UseEq = ((ToolStripMenuItem)sender).CheckState == CheckState.Checked ? true : false;
		}

		private void paletteequalizerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ShowPaletteDialog();
		}

		private void viewToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
		{
			effectsafterPaletteToolStripMenuItem.CheckState = scene.PaletteFirst ? CheckState.Checked : CheckState.Unchecked;
			usepaletteToolStripMenuItem.CheckState = scene.UseEq ? CheckState.Checked : CheckState.Unchecked;

			fillToolStripMenuItem.CheckState = scene.DisplayMode == DisplayMode.Fill ? CheckState.Checked : CheckState.Unchecked;
			linesToolStripMenuItem.CheckState = scene.DisplayMode == DisplayMode.Lines ? CheckState.Checked : CheckState.Unchecked;
			pointsToolStripMenuItem.CheckState = scene.DisplayMode == DisplayMode.Points ? CheckState.Checked : CheckState.Unchecked;
		}

		private void shortkeysToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (!usageDlg.Visible)
			{
				usageDlg = new Usage();
				usageDlg.Show(this);
			}
		}

		private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
		{
			aboutDlg.ShowDialog(this);
		}

		private void openToolStripMenuItem_Click(object sender, EventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.Title = "Open Image";
			ofd.Multiselect = false;
			ofd.CheckPathExists = true;
			ofd.CheckFileExists = true;
			if (ofd.ShowDialog(this) == DialogResult.OK)
				OpenFile(ofd.FileName);
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}

		private void backgroundColorToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ColorDialog dlg = new ColorDialog();
			dlg.FullOpen = true;
			dlg.Color = scene.BgColor;
			if (dlg.ShowDialog(this) == DialogResult.OK)
				scene.BgColor = dlg.Color;
		}

		private void clearToolStripMenuItem_Click(object sender, EventArgs e)
		{
			scene.Clear();
		}

		private void resetToolStripMenuItem_Click(object sender, EventArgs e)
		{
			scene.Create(null, null, true);
		}

		void OpenBrowser(string path = null)
		{
			if (fb is null || !fb.Visible)
			{
				fb = new FileBrowser(new FolderBrowser(), path != null ? path : (fb != null ? fb.lastPath : null));
				fb.Show(this);
				fb.OpenFiles -= browser_OpenFile;
				fb.OpenFiles += browser_OpenFile;
			}
		}

		private void browseToolStripMenuItem_Click(object sender, EventArgs e)
		{
			OpenBrowser();
		}

		private void browser_OpenFile(object sender, string[] files)
		{
			OpenFile(files);
		}

		private void resetColorEffectsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			scene.ResetColorTransform();
		}

		private void effectsafterPaletteToolStripMenuItem_Click(object sender, EventArgs e)
		{
			scene.PaletteFirst = ((ToolStripMenuItem)sender).CheckState == CheckState.Checked ? true : false;
		}
	}
}
