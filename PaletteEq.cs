using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DicomI
{
	public enum ColorChannel
	{
		Red,
		Green,
		Blue
	}

	public class PaletteEq
	{
		ColorChannel channel = ColorChannel.Red;
		byte[] r = new byte[256];
		byte[] g = new byte[256];
		byte[] b = new byte[256];
		bool link = false;

		public PaletteEq(bool load = false)
		{
			SetDefault();

			if (load)
				Load();
		}

		public PaletteEq(PaletteEq eq)
		{
			channel = eq.channel;
			eq.r.CopyTo(r, 0);
			eq.g.CopyTo(g, 0);
			eq.b.CopyTo(b, 0);
			link = eq.link;
		}

		public ColorChannel Channel
		{
			get
			{
				return channel;
			}
			set
			{
				channel = value;
			}
		}

		public bool LinkChannels
		{
			get
			{
				return link;
			}
			set
			{
				link = value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		ref byte[] GetChannelData(ColorChannel ch)
		{
			if (ch == ColorChannel.Red)
				return ref r;
			if (ch == ColorChannel.Green)
				return ref g;
			return ref b;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public byte GetValue(ColorChannel ch, int index)
		{
			return GetChannelData(LinkChannels ? Channel : ch)[index];
		}

		public static byte ValidateValue(int val)
		{
			if (val < 0)
				return 0;
			if (val > 255)
				return 255;
			return (byte)val;
		}

		public static int ValidateIndex(int index)
		{
			if (index < 0)
				return 0;
			if (index > 255)
				return 255;
			return index;

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void SetValue(int delta, ColorChannel ch, int index)
		{
			if (index >= 0 && index <= 255)
			{
				int newVal = ValidateValue(GetChannelData(ch)[index] + delta);
				GetChannelData(ch)[index] = (byte)newVal;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SetValue(ColorChannel ch, int index, byte val)
		{
			index = ValidateIndex(index);
			val = ValidateValue(val);

			byte oldVal = GetChannelData(ch)[index];
			GetChannelData(ch)[index] = val;
		}

		public void SetDefault()
		{
			for (int i = 0; i < 256; i++)
			{
				r[i] = (byte)i;
				g[i] = (byte)i;
				b[i] = (byte)i;
			}
		}

		const string configFile = "Palette.config";

		public void Load(string fname = null)
		{
			try
			{
				TextReader tr = File.OpenText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fname is null ? configFile : fname));

				string[] arr = tr.ReadLine().Split(null);
				channel = (ColorChannel)int.Parse(arr[0]);
				link = int.Parse(arr[1]) != 0 ? true : false;

				arr = tr.ReadLine().Split(null);
				for (int i = 0; i < arr.Length; i++)
				{
					r[i] = Byte.Parse(arr[i]);
				}
				arr = tr.ReadLine().Split(null);
				for (int i = 0; i < arr.Length; i++)
				{
					g[i] = Byte.Parse(arr[i]);
				}
				arr = tr.ReadLine().Split(null);
				for (int i = 0; i < 256; i++)
				{
					b[i] = Byte.Parse(arr[i]);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
		}

		public void Save(string fname = null)
		{
			try
			{
				String s = "" + (int)channel + " " + (link ? 1 : 0) + "\r\n";
				s += String.Join(" ", r) + "\r\n";
				s += String.Join(" ", g) + "\r\n";
				s += String.Join(" ", b) + "\r\n";
				File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fname is null ? configFile : fname), s);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
		}
	}
}
