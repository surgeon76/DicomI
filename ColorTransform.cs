using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DicomIClasses
{
	public class ColorTransform
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte GetBrightness(byte r, byte g, byte b)
		{
			byte max, min;

			max = r; min = r;

			if (g > max) max = g;
			if (b > max) max = b;

			if (g < min) min = g;
			if (b < min) min = b;

			return (byte)((max + min + 1) / 2);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte UnpackRgb(int ch, int rgb)
		{
			if (ch == 0)
				return (byte)(rgb & 0x0000ff);
			if (ch == 1)
				return (byte)((rgb >> 8) & 0x00ff);
			return (byte)(rgb >> 16);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int PackRgb(byte r, byte g, byte b)
		{
			return r | (g << 8) | (b << 16);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Brightness(byte r, byte g, byte b, float br)
		{
			float fr = (r + 1) * br - 0.5f;
			float fg = (g + 1) * br - 0.5f;
			float fb = (b + 1) * br - 0.5f;
			return PackRgb(fr <= 255 ? (byte)fr : (byte)255, fg <= 255 ? (byte)fg : (byte)255, fb <= 255 ? (byte)fb : (byte)255);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Contrast(byte r, byte g, byte b, float c, byte middle = 127)
		{
			byte br = GetBrightness(r, g, b);
			float fr = (br > middle ? (r + 1) * c : (r + 1) / c) - 0.5f;
			float fg = (br > middle ? (g + 1) * c : (g + 1) / c) - 0.5f;
			float fb = (br > middle ? (b + 1) * c : (b + 1) / c) - 0.5f;
			return PackRgb(fr <= 255 ? (byte)fr : (byte)255, fg <= 255 ? (byte)fg : (byte)255, fb <= 255 ? (byte)fb : (byte)255);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool BlackTolerance(byte r, byte g, byte b, int bt)
		{
			return GetBrightness(r, g, b) >= bt ? true : false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool WhiteTolerance(byte r, byte g, byte b, int wt)
		{
			return GetBrightness(r, g, b) <= wt ? true : false;
		}

		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		//public static Color Brightness(Color cr, float br)
		//{
		//	float r = (cr.R + 1) * br - 0.5f;
		//	float g = (cr.G + 1) * br - 0.5f;
		//	float b = (cr.B + 1) * br - 0.5f;
		//	return Color.FromArgb(r <= 255 ? (int)r : 255, g <= 255 ? (int)g : 255, b <= 255 ? (int)b : 255);
		//}

		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		//public static Color Contrast(Color cr, float c, int middle = 127)
		//{
		//	float br = cr.GetBrightness() * 255 + 0.5f;
		//	float r = (br > middle ? (cr.R + 1) * c : (cr.R + 1) / c) - 0.5f;
		//	float g = (br > middle ? (cr.G + 1) * c : (cr.G + 1) / c) - 0.5f;
		//	float b = (br > middle ? (cr.B + 1) * c : (cr.B + 1) / c) - 0.5f;
		//	return Color.FromArgb(r <= 255 ? (int)r : 255, g <= 255 ? (int)g : 255, b <= 255 ? (int)b : 255);
		//}

		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		//public static bool BlackTolerance(Color cr, int bt)
		//{
		//	return cr.GetBrightness() * 255 >= bt ? true : false;
		//}

		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		//public static bool WhiteTolerance(Color cr, int wt)
		//{
		//	return cr.GetBrightness() * 255 <= wt ? true : false;
		//}
	}
}
