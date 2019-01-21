using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DicomI
{
	public enum BrowseItemType
	{
		Container,
		Item
	}

	public class BrowseItem
	{
		public string name = "";
		public string id = "";
		public string info = "";
		public object tag = null;

		BrowseItem parent = null;
		List<BrowseItem> children = null;
		BrowseItemType type = BrowseItemType.Container;

		public BrowseItem(BrowseItem p = null, BrowseItemType t = BrowseItemType.Container)
		{
			parent = p;
			type = t;
		}

		public BrowseItem Parent
		{
			get
			{
				return parent;
			}
		}

		public ref List<BrowseItem> Children
		{
			get
			{
				return ref children;
			}
		}

		public BrowseItemType Type
		{
			get
			{
				return type;
			}
		}
	}

	public interface IBrowsable
	{
		BrowseItem GetRoot();
		BrowseItem GetParent(BrowseItem item);
		ref List<BrowseItem> GetChildren(BrowseItem item);
	}
}
