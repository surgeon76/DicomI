using Dicom;
using Dicom.Imaging;
using Dicom.Media;
using Manina.Windows.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DicomI
{
	public partial class FileBrowser : Form
	{
		public IBrowsable fb = null;
		public event EventHandler<string[]> OpenFiles;
		public string lastPath = null;

		public FileBrowser(IBrowsable b, string path = null)
		{
			InitializeComponent();

			fb = b;
			lastPath = path;

			Application.Idle += new EventHandler(Application_Idle);

			// Populate renderer dropdown
			Assembly assembly = Assembly.GetAssembly(typeof(ImageListView));
			int i = 0;
			foreach (Type t in assembly.GetTypes())
			{
				if (t.BaseType == typeof(Manina.Windows.Forms.ImageListView.ImageListViewRenderer))
				{
					renderertoolStripComboBox.Items.Add(new RendererItem(t));
					if (t.Name == "DefaultRenderer")
						renderertoolStripComboBox.SelectedIndex = i;
					i++;
				}
			}
			listView1.RetryOnError = false;

			listView1.RetrieveVirtualItemThumbnail += RetrieveVirtualItemThumbnail;
			listView1.RetrieveVirtualItemImage += RetrieveVirtualItemImage;
			listView1.RetrieveVirtualItemDetails += RetrieveVirtualItemDetails;

			listView1.AllowDrag = true;
		}

		void RetrieveVirtualItemImage(object sender, VirtualItemImageEventArgs e)
		{
			if (e.Key.GetType() != typeof(BrowseItem))
				return;

			BrowseItem it = (BrowseItem)e.Key;
			e.FileName = it.id;
		}

		Bitmap GetImage(object key, Size sz)
		{
			if (key.GetType() != typeof(BrowseItem))
				return null;

			try
			{
				BrowseItem it = (BrowseItem)key;
				int frame = -1;
				string path = "";
				if (it.info.StartsWith("dicom:image"))
				{
					frame = 0;
					path = it.info.Remove(0, "dicom:image:".Length);
				}
				else if (it.info.StartsWith("dicom:frame"))
				{
					string[] arr = it.info.Split(new char[] { ':' }, 4);
					if (arr.Length == 4)
					{
						frame = int.Parse(arr[2]);
						path = arr[3];
					}
				}

				if (frame < 0)
					return null;

				if (DicomFile.HasValidHeader(path))
				{
					DicomImage img = new DicomImage(path, frame);
					Bitmap bmp = img.RenderImage(frame).AsBitmap();
					float ratio = (float)bmp.Width / bmp.Height;
					if (ratio < 1)
						sz.Width = (int)(sz.Height * ratio);
					else
						sz.Height = (int)(sz.Width / ratio);
					return (sz.Width > 0 && sz.Height > 0) ? new Bitmap(img.RenderImage(frame).AsBitmap(), sz) : new Bitmap(img.RenderImage(frame).AsBitmap());
				}
			}
			catch
			{
			}

			return null;
		}

		void RetrieveVirtualItemThumbnail(object sender, VirtualItemThumbnailEventArgs e)
		{
			e.ThumbnailImage = GetImage(e.Key, e.ThumbnailDimensions);
		}

		void RetrieveVirtualItemDetails(object sender, VirtualItemDetailsEventArgs e)
		{
			if (e.Key.GetType() != typeof(BrowseItem))
				return;

			BrowseItem it = (BrowseItem)e.Key;
			e.FileName = it.name;
		}

		void Application_Idle(object sender, EventArgs e)
		{
			thumbnailsToolStripButton.Checked = (listView1.View == Manina.Windows.Forms.View.Thumbnails);
			detailsToolStripButton.Checked = (listView1.View == Manina.Windows.Forms.View.Details);
			galleryToolStripButton.Checked = (listView1.View == Manina.Windows.Forms.View.Gallery);
			paneToolStripButton.Checked = (listView1.View == Manina.Windows.Forms.View.Pane);

			clearCacheToolStripButton.Enabled = (listView1.Items.Count > 0);

			x48ToolStripMenuItem.Checked = (listView1.ThumbnailSize == new Size(48, 48));
			x96ToolStripMenuItem.Checked = (listView1.ThumbnailSize == new Size(96, 96));
			x120ToolStripMenuItem.Checked = (listView1.ThumbnailSize == new Size(120, 120));
			x150ToolStripMenuItem.Checked = (listView1.ThumbnailSize == new Size(150, 150));
			x200ToolStripMenuItem.Checked = (listView1.ThumbnailSize == new Size(200, 200));
		}

		private struct RendererItem
		{
			public Type Type;

			public override string ToString()
			{
				return Type.Name;
			}

			public RendererItem(Type type)
			{
				Type = type;
			}
		}

		private void renderertoolStripComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			// Change the renderer
			Assembly assembly = Assembly.GetAssembly(typeof(ImageListView));
			RendererItem item = (RendererItem)renderertoolStripComboBox.SelectedItem;
			ImageListView.ImageListViewRenderer renderer = assembly.CreateInstance(item.Type.FullName) as ImageListView.ImageListViewRenderer;
			listView1.SetRenderer(renderer);
			listView1.Focus();
		}

		private void FileBrowser_Load(object sender, EventArgs e)
		{
			BrowseItem it = fb.GetRoot();

			TreeNode tn = new TreeNode(it.name);
			tn.Tag = it;
			tn.ImageKey = it.info.Length == 0 ? "folder" : "dicomfolder";
			tn.SelectedImageKey = tn.ImageKey;
			tn.Nodes.Add("");
			treeView1.Nodes.Add(tn);

			tn.Expand();
			OpenPath(lastPath);
		}

		private void treeView1_BeforeExpand(object sender, TreeViewCancelEventArgs e)
		{
			if (e.Node.Nodes[0].Text == "")
			{
				e.Node.Nodes[0].Remove();

				List<BrowseItem> arr = fb.GetChildren((BrowseItem)e.Node.Tag);
				foreach (BrowseItem it in arr)
				{
					if (it.Type == BrowseItemType.Container)
					{
						TreeNode tn = new TreeNode(it.name);
						tn.Tag = it;
						tn.ImageKey = it.info.Length == 0 ? "folder" : "dicomfolder";
						tn.SelectedImageKey = tn.ImageKey;
						tn.Nodes.Add("");
						e.Node.Nodes.Add(tn);
					}
				}
			}

			if (e.Node.Tag.GetType() == typeof(BrowseItem))
			{
				BrowseItem it = (BrowseItem)e.Node.Tag;
				e.Node.ImageKey = it.info.Length == 0 ? "openfolder" : "dicomopenfolder";
				e.Node.SelectedImageKey = e.Node.ImageKey;
			}
		}

		private void treeView1_BeforeCollapse(object sender, TreeViewCancelEventArgs e)
		{
			if (e.Node.Tag.GetType() == typeof(BrowseItem))
			{
				BrowseItem it = (BrowseItem)e.Node.Tag;
				e.Node.ImageKey = it.info.Length == 0 ? "folder" : "dicomfolder";
				e.Node.SelectedImageKey = e.Node.ImageKey;
			}
		}

		private void treeView1_BeforeSelect(object sender, TreeViewCancelEventArgs e)
		{
			listView1.Items.Clear();

			List<BrowseItem> arr = fb.GetChildren((BrowseItem)e.Node.Tag);
			foreach (BrowseItem it in arr)
			{
				if (it.Type == BrowseItemType.Item)
				{
					ImageListViewItem lvi = it.info.StartsWith("dicom:") ? new ImageListViewItem(it, it.name) : new ImageListViewItem(it.id);
					lvi.Tag = it;
					listView1.Items.Add(lvi);
				}
			}
			lastPath = ((BrowseItem)e.Node.Tag).id;
			if (lastPath != null)
				lastPath = lastPath.TrimEnd(Path.DirectorySeparatorChar);

			listView1.Invalidate();
		}

		void Open()
		{
			if (listView1.SelectedItems.Count == 0)
				return;

			string[] files = new string[listView1.SelectedItems.Count];
			for (int i = 0; i < listView1.SelectedItems.Count; i++)
			{
				files[i] = ((BrowseItem)listView1.SelectedItems[i].Tag).id;
			}

			OpenFiles(this, files);
		}

		private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			Open();
		}

		private void FileBrowser_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Return)
				Open();
		}

		private void detailsToolStripButton_Click(object sender, EventArgs e)
		{
			listView1.View = Manina.Windows.Forms.View.Details;
		}

		private void thumbnailsToolStripButton_Click(object sender, EventArgs e)
		{
			listView1.View = Manina.Windows.Forms.View.Thumbnails;
		}

		private void galleryToolStripButton_Click(object sender, EventArgs e)
		{
			listView1.View = Manina.Windows.Forms.View.Gallery;
		}

		private void paneToolStripButton_Click(object sender, EventArgs e)
		{
			listView1.View = Manina.Windows.Forms.View.Pane;
		}

		private void x48ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			listView1.ThumbnailSize = new Size(48, 48);
		}

		private void x96ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			listView1.ThumbnailSize = new Size(96, 96);
		}

		private void x120ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			listView1.ThumbnailSize = new Size(120, 120);
		}

		private void x150ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			listView1.ThumbnailSize = new Size(150, 150);
		}

		private void x200ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			listView1.ThumbnailSize = new Size(200, 200);
		}

		private void clearCacheToolStripButton_Click(object sender, EventArgs e)
		{
			listView1.ClearThumbnailCache();
		}

		public void OpenPath(string path)
		{
			if (path is null)
				return;

			path = path.Trim().TrimEnd(Path.DirectorySeparatorChar);

			if (path.Length == 0)
				return;

			//string fileName = "";
			//if (File.Exists(path) && !File.GetAttributes(path).HasFlag(FileAttributes.Directory))
			//{
			//	fileName = Path.GetFileName(path);
			//	path = Path.GetDirectoryName(path);
			//}
			string[] arr = path.Split(Path.DirectorySeparatorChar);
			if (arr.Length == 0)
				return;

			TreeNode node = treeView1.Nodes[0];
			for (int i = 0; i < arr.Length; i++)
			{
				path = arr[i].ToLower();
				bool found = false;
				for (int j = 0; j < node.Nodes.Count; j++)
				{
					if (path == node.Nodes[j].Text.ToLower())
					{
						node = node.Nodes[j];
						if (i < arr.Length - 1)
							node.Expand();
						found = true;
						break;
					}
				}
				if (!found)
					break;
				if (i == arr.Length - 1)
					treeView1.SelectedNode = node;
			}
		}
	}

	public class FolderBrowser : IBrowsable
	{
		BrowseItem root = null;

		public BrowseItem GetRoot()
		{
			if (root is null)
			{
				root = new BrowseItem();
				root.name = "Computer";
			}
			return root;
		}

		public BrowseItem GetParent(BrowseItem item)
		{
			return item.Parent;
		}

		string ValidateName(string name)
		{
			foreach (char c in Path.GetInvalidFileNameChars())
			{
				name = name.Replace(c, '_');
			}
			return name;
		}

		string GetDicomName(DicomDirectoryRecord r, string infoParent, int count = -1)
		{
			string name = "";
			try
			{
				if (infoParent is null || infoParent.Length == 0)
					name = r.Get<string>(DicomTag.PatientName, -1);
				else if (infoParent.StartsWith("dicom:patient"))
				{
					try
					{
						name = r.Get<string>(DicomTag.StudyDescription, -1);
					}
					catch
					{
					}
					name += " - " + r.Get<string>(DicomTag.StudyDate, -1) + " " + r.Get<string>(DicomTag.StudyTime, -1);
				}
				else if (infoParent.StartsWith("dicom:study"))
				{
					try
					{
						name = r.Get<string>(DicomTag.SeriesNumber, -1);
					}
					catch
					{
					}
					name += " - " + r.Get<string>(DicomTag.SeriesDate, -1) + " " + r.Get<string>(DicomTag.SeriesTime, -1);
				}
				else if (infoParent.StartsWith("dicom:series"))
					name = Path.GetFileName(r.Get<string>(DicomTag.ReferencedFileID, -1));
			}
			catch
			{
			}

			if (count >= 0)
				name += " - " + FormatNumber(count);

			return ValidateName(name.Replace(Path.DirectorySeparatorChar, '-'));
		}

		int GetFrameCount(string path)
		{
			int count = 0;
			try
			{
				if (DicomFile.HasValidHeader(path))
				{
					DicomImage img = new DicomImage(path);
					count = img.NumberOfFrames;
				}
			}
			catch
			{
			}
			return count;
		}

		string GetImagePath(BrowseItem parent, DicomDirectoryRecord r, bool levelFile)
		{
			BrowseItem it = levelFile ? parent.Parent.Parent : parent.Parent.Parent.Parent;
			return Path.Combine(it.info.Remove(0, "dicom:patient:".Length), r.Get<string>(DicomTag.ReferencedFileID, -1));
		}

		void TryToAddDicomPatient(BrowseItem parent, string curPath)
		{
			string dicomdirPath = Path.Combine(curPath, "dicomdir");
			if (File.Exists(dicomdirPath))
			{
				try
				{
					DicomDirectory dir = DicomDirectory.Open(dicomdirPath);
					int i = 0;
					foreach (DicomDirectoryRecord patient in dir.RootDirectoryRecordCollection)
					{
						BrowseItem it = new BrowseItem(parent);
						it.name = GetDicomName(patient, "", ++i);
						it.id = parent.id is null ? it.name : Path.Combine(parent.id, it.name);
						it.info = "dicom:patient:" + curPath;
						it.tag = patient;
						parent.Children.Add(it);
					}
				}
				catch
				{
				}
			}
		}

		string FormatNumber(int n)
		{
			return string.Format("{0:D4}", n);
		}

		public ref List<BrowseItem> GetChildren(BrowseItem item)
		{
			if (item.Type == BrowseItemType.Container)
			{
				if (item.Children == null)
				{
					try
					{
						item.Children = new List<BrowseItem>();
						if (item.Parent != null) //not root
						{
							if (item.tag is null) //not dicom project
							{
								string[] arr = Directory.GetDirectories(item.id);
								Array.Sort(arr);
								foreach (string dir in arr)
								{
									BrowseItem it = new BrowseItem(item);
									it.name = Path.GetFileName(dir);
									it.id = dir;
									item.Children.Add(it);

									TryToAddDicomPatient(item, it.id);
								}

								arr = Directory.GetFiles(item.id);
								Array.Sort(arr);
								foreach (string dir in arr)
								{
									int count = GetFrameCount(dir);
									BrowseItem it = new BrowseItem(item, count <= 1 ? BrowseItemType.Item : BrowseItemType.Container);
									it.name = Path.GetFileName(dir);
									it.id = dir;
									it.info = count > 0 ? "dicom:image:" + dir : dir;
									it.tag = new DicomDirectoryRecord();
									item.Children.Add(it);
								}
							}
							else //dicom project
							{
								if (item.info.StartsWith("dicom:image"))
								{
									string realPath = item.info.Remove(0, "dicom:image:".Length);
									for (int i = 0; i < GetFrameCount(realPath); i++)
									{
										BrowseItem it = new BrowseItem(item, BrowseItemType.Item);
										it.id = "dicom:frame:" + i + ":" + realPath;
										it.info = "dicom:frame:" + i + ":" + realPath;
										it.name = "frame - " + FormatNumber(i + 1);
										item.Children.Add(it);
									}
								}
								else
								{ 
									DicomDirectoryRecord r = ((DicomDirectoryRecord)item.tag);
									int i = 0;
									foreach (DicomDirectoryRecord rec in r.LowerLevelDirectoryRecordCollection)
									{
										BrowseItem it = null;
										string name = GetDicomName(rec, item.info, ++i);
										if (item.info.StartsWith("dicom:patient") || item.info.StartsWith("dicom:study"))
										{
											it = new BrowseItem(item, BrowseItemType.Container);
											it.id = Path.Combine(item.id, name);
											it.info = item.info.StartsWith("dicom:patient") ? "dicom:study" : "dicom:series";
										}
										else if (item.info.StartsWith("dicom:series"))
										{
											string realPath = GetImagePath(item, rec, true);
											int count = GetFrameCount(realPath);
											if (count > 0)
											{
												name = GetDicomName(rec, item.info);
												it = new BrowseItem(item, count > 1 ? BrowseItemType.Container : BrowseItemType.Item);
												it.id = count > 1 ? Path.Combine(item.id, name) : realPath;
												it.info = "dicom:image:" + realPath;
											}
										}

										if (it != null)
										{
											it.name = name;
											it.tag = rec;
											item.Children.Add(it);
										}
									}
								}
							}
						}
						else //root
						{
							foreach (DriveInfo drv in DriveInfo.GetDrives())
							{
								BrowseItem it = new BrowseItem(item);
								it.name = drv.Name.TrimEnd(Path.DirectorySeparatorChar);
								it.id = drv.Name;
								item.Children.Add(it);

								TryToAddDicomPatient(item, it.id);
							}
						}
					}
					catch
					{
					}
				}
			}

			return ref item.Children;
		}
	}
}
