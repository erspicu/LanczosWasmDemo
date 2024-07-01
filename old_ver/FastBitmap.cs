using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanczosAlg
{
    unsafe public class FastBitmap
    {
        public struct PixelData
        {
            public byte blue;
            public byte green;
            public byte red;
            public byte alpha;
        }

        public Bitmap workingBitmap = null;
        public int width = 0;
        private BitmapData bitmapData = null;
        public Byte* pBase = null;


        public int Width;
        public int Height;

        public FastBitmap(Bitmap inputBitmap)
        {
            workingBitmap = inputBitmap;
            Width = inputBitmap.Width;
            Height = inputBitmap.Height;
        }

        public void LockImage()
        {
            Rectangle bounds = new Rectangle(Point.Empty, workingBitmap.Size);

            width = (int)(bounds.Width * sizeof(PixelData));
            if (width % 4 != 0) width = 4 * (width / 4 + 1);

            //Lock Image
            bitmapData = workingBitmap.LockBits(bounds, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            pBase = (Byte*)bitmapData.Scan0.ToPointer();
        }

        public PixelData* pixelData = null;

        public Color GetPixel(int x, int y)
        {
            pixelData = (PixelData*)(pBase + y * width + x * sizeof(PixelData));
            return Color.FromArgb(pixelData->alpha, pixelData->red, pixelData->green, pixelData->blue);
        }

        public Color GetPixelNext()
        {
            pixelData++;
            return Color.FromArgb(pixelData->alpha, pixelData->red, pixelData->green, pixelData->blue);
        }

        public void SetPixel(int x, int y, int color)
        {
            *(int*)(pBase + (y * width) + (x << 2)) = color;
        }

        public void UnlockImage()
        {
            workingBitmap.UnlockBits(bitmapData);
            bitmapData = null;
            pBase = null;
        }
    }
}
