using DicomIClasses;
using SharpGL;
using SharpGL.Enumerations;
using SharpGL.SceneGraph;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DicomI
{
	enum DisplayMode
	{
		Fill,
		Lines,
		Points
	}

	class ColorBuffer
	{
		IntPtr buf = IntPtr.Zero;
		int bufSz = 0;

		public ColorBuffer(int size)
		{
			buf = Marshal.AllocHGlobal(size * 3);
			bufSz = size;
		}

		~ColorBuffer()
		{
			if (buf != IntPtr.Zero)
				Marshal.FreeHGlobal(buf);
		}

		public void SetColor(int index, Color cr)
		{
			Marshal.WriteByte(buf, index * 3, cr.R);
			Marshal.WriteByte(buf, index * 3 + 1, cr.G);
			Marshal.WriteByte(buf, index * 3 + 2, cr.B);
		}

		public Color GetColor(int index)
		{
			return Color.FromArgb(Marshal.ReadByte(buf, index * 3), Marshal.ReadByte(buf, index * 3 + 1), Marshal.ReadByte(buf, index * 3 + 2));
		}

		public IntPtr Data
		{
			get
			{
				return buf;
			}
		}
	}

	class Scene
	{
		OpenGLControl output = null;
		Bitmap bmp = null;
		List<Size> bmpSizes = new List<Size>();
		public const PixelFormat pixelFormat = PixelFormat.Format24bppRgb;
		const int bytesPerPixel = 3;
		public PaletteEq paletteEq = new PaletteEq(true);

		int dx = 0, dy = 0;
		int dx0 = 0, dy0 = 0;
		Size orthoSz = new Size(1, 1);
		double scale = 1;
		int size = 1;
		const int maxSize = 8;
		DisplayMode displayMode = DisplayMode.Fill;
		bool useEq = true;
		int interlacing = 1;
		const int maxInterlacing = 8;
		Color bgColor = Color.FromArgb(0, 0, 0);

		float brightness = 1.0f;
		float contrast = 1.0f;
		int blackTolerance = 0;
		int whiteTolerance = 255;
		bool paletteFirst = false;

		public bool IsInit
		{
			get
			{
				return output != null;
			}
		}

		public bool PaletteFirst
		{
			get
			{
				return paletteFirst;
			}
			set
			{
				paletteFirst = value;
				Create(null, null, false);
				Draw();
			}
		}

		public void ResetColorTransform()
		{
			brightness = 1.0f;
			contrast = 1.0f;
			blackTolerance = 0;
			whiteTolerance = 255;
			paletteFirst = false;

			if (output is null)
				return;
			Create(null, null, false);
			output.Invalidate();
		}

		public void ChangeBrightness(float coeff = 1.0f)
		{
			ChangeBrightness(-dy, coeff);
		}

		public void ChangeBrightness(float delta, float coeff = 1.0f)
		{
			brightness += delta * coeff / 127;
			if (brightness > 4)
				brightness = 4;
			if (brightness < 1.0 / 64)
				brightness = 1.0f / 64;
		}

		public void ChangeContrast(float coeff = 1.0f)
		{
			ChangeContrast(-dy, coeff);
		}

		public void ChangeContrast(float delta, float coeff = 1.0f)
		{
			contrast += delta * coeff / 127;
			if (contrast > 2)
				contrast = 2;
			if (contrast < 1.0 / 64)
				contrast = 1.0f / 64;
		}

		public void ChangeBlackTolerance(int coeff = 1)
		{
			ChangeBlackTolerance(-dy, coeff);
		}

		public void ChangeBlackTolerance(int delta, int coeff = 1)
		{
			blackTolerance += delta * coeff;
			if (blackTolerance > 255)
				blackTolerance = 255;
			if (blackTolerance < 0)
				blackTolerance = 0;
		}

		public void ChangeWhiteTolerance(int coeff = 1)
		{
			ChangeWhiteTolerance(-dy, coeff);
		}

		public void ChangeWhiteTolerance(int delta, int coeff = 1)
		{
			whiteTolerance += delta * coeff;
			if (whiteTolerance > 255)
				whiteTolerance = 255;
			if (whiteTolerance < 0)
				whiteTolerance = 0;
		}

		public bool UseEq
		{
			get
			{
				return useEq;
			}
			set
			{
				useEq = value;
				Create(null, null, false);
				Draw();
			}
		}

		public DisplayMode DisplayMode
		{
			get
			{
				return displayMode;
			}
			set
			{
				displayMode = value;

				if (output is null)
					return;
				output.Invalidate();
			}
		}

		public int Interlacing
		{
			get
			{
				return interlacing;
			}
			set
			{
				interlacing = value;

				if (output is null)
					return;
				Create(null, null, false);
				output.Invalidate();
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		Color ApplyPalette(Color cr)
		{
			return Color.FromArgb(paletteEq.GetValue(ColorChannel.Red, cr.R), paletteEq.GetValue(ColorChannel.Green, cr.G), paletteEq.GetValue(ColorChannel.Blue, cr.B));
		}

		void MakeList(ref Bitmap bmp, OpenGL gl, double halfW, double halfH, double height)
		{
			BitmapData bData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, pixelFormat);
			unsafe
			{
				byte* pData = (byte*)bData.Scan0.ToPointer();
				int len = Math.Abs(bData.Stride);
				int lenW = len / bytesPerPixel * bytesPerPixel / interlacing;

				for (int i = 0; i < bData.Height / interlacing - 1; i++)
				{
					int i1 = i * interlacing;
					int i2 = (i + 1) * interlacing;

					double y1 = i1 - halfH;
					double y2 = i2 - halfH;

					gl.Begin(OpenGL.GL_QUAD_STRIP);

					for (int j = 0; j < lenW; j += bytesPerPixel)
					{
						int j1 = j * interlacing;

						double x = j1 / bytesPerPixel - halfW;

						byte* p1 = pData + i1 * len + j1;
						byte* p2 = pData + i2 * len + j1;

						byte r1 = *(p1 + 2);
						byte g1 = *(p1 + 1);
						byte b1 = *p1;

						byte r2 = *(p2 + 2);
						byte g2 = *(p2 + 1);
						byte b2 = *p2;

						double z1 = (ColorTransform.GetBrightness(r1, g1, b1) / 255.0 - 0.5) * height;
						double z2 = (ColorTransform.GetBrightness(r2, g2, b2) / 255.0 - 0.5) * height;

						if (UseEq && paletteFirst)
						{
							r1 = paletteEq.GetValue(ColorChannel.Red, r1);
							g1 = paletteEq.GetValue(ColorChannel.Green, g1);
							b1 = paletteEq.GetValue(ColorChannel.Blue, b1);

							r2 = paletteEq.GetValue(ColorChannel.Red, r2);
							g2 = paletteEq.GetValue(ColorChannel.Green, g2);
							b2 = paletteEq.GetValue(ColorChannel.Blue, b2);
						}

						int cr1 = ColorTransform.Brightness(r1, g1, b1, brightness);
						int cr2 = ColorTransform.Brightness(r2, g2, b2, brightness);
						cr1 = ColorTransform.Contrast((byte)(cr1 & 0x0000ff), (byte)((cr1 >> 8) & 0x00ff), (byte)(cr1 >> 16), contrast);
						cr2 = ColorTransform.Contrast((byte)(cr2 & 0x0000ff), (byte)((cr2 >> 8) & 0x00ff), (byte)(cr2 >> 16), contrast);

						r1 = (byte)(cr1 & 0x0000ff);
						g1 = (byte)((cr1 >> 8) & 0x00ff);
						b1 = (byte)(cr1 >> 16);

						r2 = (byte)(cr2 & 0x0000ff);
						g2 = (byte)((cr2 >> 8) & 0x00ff);
						b2 = (byte)(cr2 >> 16);

						bool bt1 = ColorTransform.BlackTolerance(r1, g1, b1, blackTolerance);
						bool bt2 = ColorTransform.BlackTolerance(r2, g2, b2, blackTolerance);
						bool wt1 = ColorTransform.WhiteTolerance(r1, g1, b1, whiteTolerance);
						bool wt2 = ColorTransform.WhiteTolerance(r2, g2, b2, whiteTolerance);
						if (UseEq && !paletteFirst)
						{
							r1 = paletteEq.GetValue(ColorChannel.Red, r1);
							g1 = paletteEq.GetValue(ColorChannel.Green, g1);
							b1 = paletteEq.GetValue(ColorChannel.Blue, b1);

							r2 = paletteEq.GetValue(ColorChannel.Red, r2);
							g2 = paletteEq.GetValue(ColorChannel.Green, g2);
							b2 = paletteEq.GetValue(ColorChannel.Blue, b2);
						}

						gl.Color(r1 / 255.0, g1 / 255.0, b1 / 255.0, bt1 && wt1 ? 1.0 : 0.0);
						gl.Vertex(x, bData.Stride > 0 ? y1 : -y1, -z1);

						gl.Color(r2 / 255.0, g2 / 255.0, b2 / 255.0, bt2 && wt2 ? 1.0 : 0.0);
						gl.Vertex(x, bData.Stride > 0 ? y2 : -y2, -z2);
					}

					gl.End();
				}
			}
			bmp.UnlockBits(bData);
		}

		//uint[] vBufID = { 0 };
		//uint[] cBufID = { 0 };
		//uint[] iBufID = { 0 };
		//uint[] vArrID = { 0 };
		//uint[] cArrID = { 0 };
		//int bufW = 0, bufH = 0;
		//void MakeList(ref Bitmap bmp, OpenGL gl, double halfW, double halfH, double height)
		//{
		//	BitmapData bData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, pixelFormat);
		//	unsafe
		//	{
		//		byte* pData = (byte*)bData.Scan0.ToPointer();
		//		int len = Math.Abs(bData.Stride);
		//		int lenW = len / bytesPerPixel * bytesPerPixel / interlacing;
		//		int h = bData.Height / interlacing;
		//		bufW = lenW / bytesPerPixel;
		//		bufH = h;

		//		IntPtr vBuf = Marshal.AllocHGlobal(bufH * bufW * 3 * sizeof(float));
		//		IntPtr cBuf = Marshal.AllocHGlobal(bufH * bufW * 4 * sizeof(byte));
		//		IntPtr iBuf = Marshal.AllocHGlobal((bufH - 1) * bufW * 2 * sizeof(uint));
		//		float* pVBuf = (float*)vBuf.ToPointer();
		//		byte* pCBuf = (byte*)cBuf.ToPointer();
		//		uint* pIBuf = (uint*)iBuf.ToPointer();

		//		for (int i = 0; i < h; i++)
		//		{
		//			double y = i * interlacing - halfH;
		//			for (int j = 0; j < lenW; j += bytesPerPixel)
		//			{
		//				int j1 = j * interlacing;

		//				double x = j1 / bytesPerPixel - halfW;

		//				byte* p = pData + i * len + j1;

		//				byte r = *(p + 2);
		//				byte g = *(p + 1);
		//				byte b = *p;

		//				double z = (ColorTransform.GetBrightness(r, g, b) / 255.0 - 0.5) * height;

		//				if (UseEq && paletteFirst)
		//				{
		//					r = paletteEq.GetValue(ColorChannel.Red, r);
		//					g = paletteEq.GetValue(ColorChannel.Green, g);
		//					b = paletteEq.GetValue(ColorChannel.Blue, b);
		//				}

		//				int cr = ColorTransform.Brightness(r, g, b, brightness);
		//				cr = ColorTransform.Contrast((byte)(cr & 0x0000ff), (byte)((cr >> 8) & 0x00ff), (byte)(cr >> 16), contrast);

		//				r = (byte)(cr & 0x0000ff);
		//				g = (byte)((cr >> 8) & 0x00ff);
		//				b = (byte)(cr >> 16);

		//				bool bt = ColorTransform.BlackTolerance(r, g, b, blackTolerance);
		//				bool wt = ColorTransform.WhiteTolerance(r, g, b, whiteTolerance);

		//				if (UseEq && !paletteFirst)
		//				{
		//					r = paletteEq.GetValue(ColorChannel.Red, r);
		//					g = paletteEq.GetValue(ColorChannel.Green, g);
		//					b = paletteEq.GetValue(ColorChannel.Blue, b);
		//				}

		//				int index = (i * lenW + j) / bytesPerPixel;
		//				pVBuf[index * 3] = (float)x;
		//				pVBuf[index * 3 + 1] = (float)y;
		//				pVBuf[index * 3 + 2] = (float)z;

		//				pCBuf[index * 4] = r;
		//				pCBuf[index * 4 + 1] = g;
		//				pCBuf[index * 4 + 2] = b;
		//				pCBuf[index * 4 + 3] = (byte)(bt && wt ? 255 : 0);

		//				int i1 = index;
		//				int i2 = index + bufW;
		//				if (bData.Stride < 0)
		//				{
		//					i1 = ((h - i - 1) * lenW + j) / bytesPerPixel;
		//					i2 = ((h - i - 2) * lenW + j) / bytesPerPixel;
		//				}
		//				if (i < h - 1)
		//				{
		//					pIBuf[index * 2] = (uint)i1;
		//					pIBuf[index * 2 + 1] = (uint)i2;
		//				}
		//			}
		//		}

		//		gl.DeleteBuffers(1, vBufID);
		//		gl.DeleteBuffers(1, cBufID);
		//		gl.DeleteBuffers(1, iBufID);

		//		gl.GenBuffers(1, vBufID);
		//		gl.GenBuffers(1, cBufID);
		//		gl.GenBuffers(1, iBufID);

		//		gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, vBufID[0]);
		//		gl.BufferData(OpenGL.GL_ARRAY_BUFFER, bufH * bufW * 3 * sizeof(float), vBuf, OpenGL.GL_STATIC_DRAW);

		//		gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, cBufID[0]);
		//		gl.BufferData(OpenGL.GL_ARRAY_BUFFER, bufH * bufW * 4 * sizeof(byte), cBuf, OpenGL.GL_DYNAMIC_DRAW);

		//		gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, 0);

		//		gl.BindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, iBufID[0]);
		//		gl.BufferData(OpenGL.GL_ELEMENT_ARRAY_BUFFER, (bufH - 1) * bufW * 2 * sizeof(uint), iBuf, OpenGL.GL_STATIC_DRAW);

		//		gl.BindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, 0);

		//		Marshal.FreeHGlobal(iBuf);
		//		Marshal.FreeHGlobal(cBuf);
		//		Marshal.FreeHGlobal(vBuf);
		//	}
		//	bmp.UnlockBits(bData);
		//}

		void Init()
		{
			InitXY(0, 0);
			orthoSz = new Size(1, 1);
			scale = 1;
		}

		Bitmap ChangePixelFormat(Bitmap btmp)
		{
			Bitmap clone = new Bitmap(btmp.Width, btmp.Height, pixelFormat);
			using (Graphics gr = Graphics.FromImage(clone))
				gr.DrawImage(btmp, 0, 0, btmp.Width, btmp.Height);
			return clone;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		int GetInSizeCount(int x, int y)
		{
			int count = 0;
			for (int i = 0; i < bmpSizes.Count; i++)
			{
				if (x < bmpSizes[i].Width && y < bmpSizes[i].Height)
					count++;
			}
			return count;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		byte Avg(byte b1, byte b2, int count)
		{
			return (byte)((b1 * count + b2) / (count + 1) + 0.5);
		}

		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		//Color Avg(Color cr1, Color cr2, int x, int y)
		//{
		//	int count = GetInSizeCount(x, y);
		//	byte r = (byte)((cr1.R * count + cr2.R) / (count + 1) + 0.5);
		//	byte g = (byte)((cr1.G * count + cr2.G) / (count + 1) + 0.5);
		//	byte b = (byte)((cr1.B * count + cr2.B) / (count + 1) + 0.5);
		//	return Color.FromArgb(r, g, b);
		//}

		void Merge(ref Bitmap bOrig, Bitmap bAdd)
		{
			BitmapData bDataOrig = bOrig.LockBits(new Rectangle(0, 0, bOrig.Width, bOrig.Height), ImageLockMode.ReadWrite, pixelFormat);
			BitmapData bDataAdd = bAdd.LockBits(new Rectangle(0, 0, bAdd.Width, bAdd.Height), ImageLockMode.ReadOnly, pixelFormat);
			unsafe
			{
				byte* pDataOrig = (byte*)bDataOrig.Scan0.ToPointer();
				byte* pDataAdd = (byte*)bDataAdd.Scan0.ToPointer();
				int lenOrig = Math.Abs(bDataOrig.Stride);
				int lenAdd = Math.Abs(bDataAdd.Stride);
				for (int i = 0; i < bDataAdd.Height; i++)
				{
					int iOrig = bDataOrig.Stride > 0 ? i : bDataOrig.Height - i - 1;
					int iAdd = bDataAdd.Stride > 0 ? i : bDataAdd.Height - i - 1;
					for (int j = 0; j < lenAdd / bytesPerPixel * bytesPerPixel; j += bytesPerPixel)
					{
						byte* pOrig = pDataOrig + iOrig * lenOrig + j;
						byte* pAdd = pDataAdd + iAdd * lenAdd + j;

						int count = GetInSizeCount(j / bytesPerPixel, i);
						*pOrig = Avg(*pOrig, *pAdd, count);
						*(pOrig + 1) = Avg(*(pOrig + 1), *(pAdd + 1), count);
						*(pOrig + 2) = Avg(*(pOrig + 2), *(pAdd + 2), count);
					}
				}
			}
			bAdd.UnlockBits(bDataAdd);
			bOrig.UnlockBits(bDataOrig);
		}

		public void Add(Bitmap[] btmp, OpenGLControl outpt)
		{
			if (btmp.Length == 0)
				return;

			int startIndex = 0;
			if (bmpSizes.Count == 0)
			{
				if (btmp[0].PixelFormat != pixelFormat)
					bmp = ChangePixelFormat(btmp[0]);
				else
					bmp = new Bitmap(btmp[0]);
				bmpSizes.Add(new Size(bmp.Width, bmp.Height));
				startIndex = 1;
			}

			int maxW = bmp.Width;
			int maxH = bmp.Height;
			for (int i = 0; i < btmp.Length; i++)
			{
				if (btmp[i].PixelFormat != pixelFormat)
					btmp[i] = ChangePixelFormat(btmp[i]);

				if (btmp[i].Width > maxW)
					maxW = btmp[i].Width;
				if (btmp[i].Height > maxH)
					maxH = btmp[i].Height;
			}

			if (maxW > bmp.Width || maxH > bmp.Height)
			{
				Bitmap bmpNew = new Bitmap(maxW, maxH, pixelFormat);
				using (Graphics gr = Graphics.FromImage(bmpNew))
					gr.DrawImage(bmp, 0, 0, bmp.Width, bmp.Height);
				bmp = bmpNew;
			}

			for (int i = startIndex; i < btmp.Length; i++)
			{
				Merge(ref bmp, btmp[i]);

				bmpSizes.Add(new Size(btmp[i].Width, btmp[i].Height));
			}

			Create(null, outpt, startIndex == 0 ? false : true);
		}

		void MakeProjectionMatrix(OpenGL gl, Size vport)
		{
			if (bmp is null)
				return;

			gl.MatrixMode(OpenGL.GL_PROJECTION);
			gl.LoadIdentity();

			orthoSz.Width = Math.Max(bmp.Width, bmp.Height);
			orthoSz.Height = orthoSz.Width;

			float ratio = (float)vport.Width / vport.Height;
			if (ratio > 1)
				orthoSz.Width = (int)(orthoSz.Width * ratio);
			else
				orthoSz.Height = (int)(orthoSz.Height / ratio);
			
			gl.Ortho(-orthoSz.Width / 2.0, orthoSz.Width / 2.0, -orthoSz.Height / 2.0, orthoSz.Height / 2.0, Math.Max(orthoSz.Width, orthoSz.Height) * -2000.0, Math.Max(orthoSz.Width, orthoSz.Height) * 2000.0);

			gl.MatrixMode(OpenGL.GL_MODELVIEW);
		}

		public void Create(Bitmap btmp = null, OpenGLControl outpt = null, bool init = true)
		{
			if (btmp != null)
			{
				if (btmp.PixelFormat != pixelFormat)
					bmp = ChangePixelFormat(btmp);
				else
					bmp = new Bitmap(btmp);
			}
			if (outpt != null)
				output = outpt;
			if (bmp is null || output is null)
				return;

			if (bmpSizes.Count == 0)
				bmpSizes.Add(new Size(bmp.Width, bmp.Height));

			OpenGL gl = output.OpenGL;

			if (init)
				Init();

			MakeProjectionMatrix(gl, output.ClientSize);

			gl.MatrixMode(OpenGL.GL_MODELVIEW);
			if (init)
				gl.LoadIdentity();

			if (init)
			{
				gl.Enable(OpenGL.GL_BLEND);
				gl.BlendFunc(OpenGL.GL_SRC_ALPHA, OpenGL.GL_ONE_MINUS_SRC_ALPHA);
			}

			double height = (bmp.Height + bmp.Width) / 4.0;
			double halfW = bmp.Width / 2.0;
			double halfH = bmp.Height / 2.0;

			gl.NewList(OpenGL.GL_LIST_BASE + 1, OpenGL.GL_COMPILE);
			MakeList(ref bmp, gl, halfW, halfH, height);
			gl.EndList();

			if (init)
			{
				gl.Rotate(180.0, 1.0, 0.0, 0.0);
				//Rotate(10, -10);
				scale = 0.8;
				gl.Scale(scale, scale, scale);
			}
		}

		public void Clear()
		{
			if (bmp != null)
			{
				bmp.Dispose();
				bmp = null;
			}
			bmpSizes.Clear();

			if (output is null)
				return;
			OpenGL gl = output.OpenGL;
			gl.NewList(OpenGL.GL_LIST_BASE + 1, OpenGL.GL_COMPILE);
			gl.EndList();
			output.Invalidate();
		}

		public void ChangeInterlacing(bool increase)
		{
			if (increase)
				interlacing++;
			else
				interlacing--;

			if (interlacing > maxInterlacing)
				interlacing = maxInterlacing;
			else if (interlacing <= 0)
				interlacing = 1;

			if (output is null)
				return;
			Create(null, null, false);
			output.Invalidate();
		}

		public void ChangeSize(bool increase)
		{
			if (increase)
				size++;
			else
				size--;

			if (size > maxSize)
				size = maxSize;
			else if (size <= 0)
				size = 1;

			if (output is null)
				return;
			output.Invalidate();
		}

		public void ChangeDisplayMode()
		{
			displayMode++;
			if (displayMode > DisplayMode.Points)
				displayMode = DisplayMode.Fill;

			if (output is null)
				return;
			output.Invalidate();
		}

		public Color BgColor
		{
			get
			{
				return bgColor;
			}
			set
			{
				bgColor = value;
				Draw();
			}
		}

		public void Draw()
		{
			if (output is null)
				return;

			OpenGL gl = output.OpenGL;

			gl.ClearColor(bgColor.R / 255.0f, bgColor.G / 255.0f, bgColor.B / 255.0f, 1.0f);
			gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);

			gl.LineWidth(size);
			gl.PointSize(size);

			if (displayMode == DisplayMode.Fill)
				gl.PolygonMode(FaceMode.FrontAndBack, PolygonMode.Filled);
			else if (displayMode == DisplayMode.Lines)
				gl.PolygonMode(FaceMode.FrontAndBack, PolygonMode.Lines);
			else
				gl.PolygonMode(FaceMode.FrontAndBack, PolygonMode.Points);

			gl.CallList(OpenGL.GL_LIST_BASE + 1);

			gl.Flush();
		}

		public void Resize()
		{
			if (output is null)
				return;

			OpenGL gl = output.OpenGL;

			MakeProjectionMatrix(gl, output.ClientSize);
			gl.Viewport(0, 0, output.Width, output.Height);

			output.Invalidate();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void RecalcXY(int x, int y)
		{
			dx = x - dx0;
			dy = y - dy0;

			dx0 = x;
			dy0 = y;
		}

		public void InitXY(int x, int y)
		{
			dx = 0;
			dy = 0;

			dx0 = x;
			dy0 = y;
		}

		public void Rotate(int x, int y)
		{
			if (output is null)
				return;

			OpenGL gl = output.OpenGL;

			RecalcXY(x, y);

			Matrix m = gl.GetModelViewMatrix();
			gl.Rotate(Convert.ToDouble(dy) / 2.0, m[0, 0], m[1, 0], m[2, 0]);
			gl.Finish();

			m = gl.GetModelViewMatrix();
			gl.Rotate(Convert.ToDouble(dx) / 2.0, m[0, 1], m[1, 1], m[2, 1]);

			output.Invalidate();
		}

		public void Translate(int x, int y)
		{
			if (output is null)
				return;

			OpenGL gl = output.OpenGL;

			RecalcXY(x, y);

			Matrix m = gl.GetModelViewMatrix();
			m[3, 0] += dx / Convert.ToDouble(output.ClientSize.Width) * orthoSz.Width;
			m[3, 1] += -dy / Convert.ToDouble(output.ClientSize.Height) * orthoSz.Height;
			//gl.LoadIdentity();
			gl.LoadMatrix(m.AsRowMajorArray);

			output.Invalidate();
		}

		public void Scale(int d)
		{
			if (output is null)
				return;

			OpenGL gl = output.OpenGL;

			double sc = d > 0 ? 5.0 / 4.0 : 4.0 / 5.0;
			scale *= sc;

			gl.Scale(sc, sc, sc);

			output.Invalidate();
		}

		public void imagePreview_BitmapTransform(object sender, BitmapTransform bt)
		{
			if (bt.bmp is null)
			{
				float ratio = (float)bmp.Width / bmp.Height;
				if (ratio < 1)
					bt.sz.Width = (int)(bt.sz.Height * ratio);
				else
					bt.sz.Height = (int)(bt.sz.Width / ratio);
				bt.bmp = new Bitmap(bt.sz.Width, bt.sz.Height, pixelFormat);
				using (Graphics gr = Graphics.FromImage(bt.bmp))
					gr.DrawImage(bmp, 0, 0, bt.sz.Width, bt.sz.Height);

				bt.bmpOrig = new Bitmap(bt.sz.Width, bt.sz.Height, Scene.pixelFormat);
				using (Graphics gr = Graphics.FromImage(bt.bmpOrig))
					gr.DrawImage(bt.bmp, 0, 0, bt.sz.Width, bt.sz.Height);
			}

			BitmapData bDataOrig = bt.bmpOrig.LockBits(new Rectangle(0, 0, bt.bmpOrig.Width, bt.bmpOrig.Height), ImageLockMode.ReadOnly, pixelFormat);
			BitmapData bData = bt.bmp.LockBits(new Rectangle(0, 0, bt.bmp.Width, bt.bmp.Height), ImageLockMode.ReadWrite, pixelFormat);
			unsafe
			{
				byte* pDataOrig = (byte*)bDataOrig.Scan0.ToPointer();
				byte* pData = (byte*)bData.Scan0.ToPointer();
				int len = Math.Abs(bDataOrig.Stride);
				int lenW = len / bytesPerPixel * bytesPerPixel;
				for (int i = 0; i < bDataOrig.Height; i++)
				{
					for (int j = 0; j < lenW; j += bytesPerPixel)
					{
						byte* pOrig = pDataOrig + i * len + j;
						byte* p = pData + i * len + j;

						byte r = *(pOrig + 2);
						byte g = *(pOrig + 1);
						byte b = *pOrig;

						if (UseEq && paletteFirst)
						{
							r = paletteEq.GetValue(ColorChannel.Red, r);
							g = paletteEq.GetValue(ColorChannel.Green, g);
							b = paletteEq.GetValue(ColorChannel.Blue, b);
						}
						int cr = ColorTransform.Brightness(r, g, b, brightness);
						cr = ColorTransform.Contrast((byte)(cr & 0x0000ff), (byte)((cr >> 8) & 0x00ff), (byte)(cr >> 16), contrast);

						r = (byte)(cr & 0x0000ff);
						g = (byte)((cr >> 8) & 0x00ff);
						b = (byte)(cr >> 16);

						bool transparent = false;
						if (!ColorTransform.BlackTolerance(r, g, b, blackTolerance) || !ColorTransform.WhiteTolerance(r, g, b, whiteTolerance))
						{
							r = 0;
							g = 0;
							b = 0;
							transparent = true;
						}
						if (!transparent && UseEq && !paletteFirst)
						{
							r = paletteEq.GetValue(ColorChannel.Red, r);
							g = paletteEq.GetValue(ColorChannel.Green, g);
							b = paletteEq.GetValue(ColorChannel.Blue, b);
						}

						*(p + 2) = r;
						*(p + 1) = g;
						*(p + 0) = b;
					}
				}
			}
			bt.bmp.UnlockBits(bData);
			bt.bmpOrig.UnlockBits(bDataOrig);
		}
	}
}
