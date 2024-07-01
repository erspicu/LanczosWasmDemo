using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static System.Net.Mime.MediaTypeNames;

namespace LanczosAlg
{

    //此版本適合在.net framework環境上使用

    // 參考 http://blog.csdn.net/yangzl2008/article/details/6693678 原Java Code , 網址失效
    unsafe class Lanczos
    {
        int nDots;
        int nHalfDots;
        const double PI = Math.PI;
        const double support = 3.0;
        double[] contrib;
        double[] normContrib;
        int scaleWidth;
        int height, width;

        uint* ImageBuf_org = null;

        uint* ImageBuf_2nd = null;

        uint* ImageBuf_dest = null;

        Bitmap rgbImage = null;

        public string call_test(string a)
        {
            return "from c# " + a;
        }
        public Lanczos()
        {
        }
        public Lanczos(Bitmap t)
        {
            rgbImage = t;

            width = t.Width;
            height = t.Height;

            ImageBuf_org = (uint*)Marshal.AllocHGlobal(sizeof(uint) * width * height);

            FastBitmap processor = new FastBitmap(t);
            processor.LockImage();

            FastBitmap.PixelData* data;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    data = (FastBitmap.PixelData*)(processor.pBase + y * processor.width + x * pdsize); ;

                    ImageBuf_org[y * width + x] = (uint)((data->red << 16) | (data->green << 8) | data->blue);

                }

            }

            processor.UnlockImage();
        }

        //Marshal.FreeHGlobal((IntPtr) _output);

        /*public Bitmap GetOutput()
        {
            return new Bitmap(256 * 5, 240 * 5, 256 * 5 * 4, PixelFormat.Format32bppRgb, (IntPtr)_output);
        }*/


        public Bitmap ResizeLanczos(int rwidth, int rheight)
        {

            Console.WriteLine("\nresize image...");

            Marshal.FreeHGlobal((IntPtr)ImageBuf_dest);


            ImageBuf_dest = (uint*)Marshal.AllocHGlobal(sizeof(uint) * rwidth * rheight);

            int w = rwidth, h = rheight;

            double sx = w / (double)width;
            double sy = h / (double)height;

            w = (int)(sx * width);
            h = (int)(sy * height);

            scaleWidth = w;

            if (determineResultSize(w, h) == 1)
            {
                Console.WriteLine("resize image done.");
                return rgbImage;
            }

            Stopwatch st = new Stopwatch();
            calContrib();

            st.Restart();

            //Bitmap pbFinalOut = VerticalFiltering(HorizontalFiltering(rgbImage, w), h);

            HorizontalFiltering_Ptr(width, height, rwidth);
            VerticalFiltering_Ptr(rwidth, height, rheight);


            st.Stop();
            Console.WriteLine("reszie time : " + st.ElapsedMilliseconds);


            Console.WriteLine("resize image done.");
            //return pbFinalOut;
            return new Bitmap(rwidth, rheight, rwidth * 4, PixelFormat.Format32bppRgb, (IntPtr)ImageBuf_dest);
        }


        double LanczosCal(int i, int inWidth, int outWidth, double Support)
        {
            double x = (double)i * (double)outWidth / (double)inWidth;
            return Math.Sin(x * PI) / (x * PI) * Math.Sin(x * PI / Support) / (x * PI / Support);
        }

        void calContrib()
        {
            nHalfDots = (int)((double)width * support / (double)scaleWidth);
            nDots = nHalfDots * 2 + 1;

            contrib = new double[nDots];
            normContrib = new double[nDots];

            int center = nHalfDots;
            contrib[center] = 1.0;

            double weight = 0.0;

            for (int i = 1; i <= center; i++)
            {
                contrib[center + i] = LanczosCal(i, width, scaleWidth, support);
                weight += contrib[center + i];
            }

            for (int i = center - 1; i >= 0; i--)
                contrib[i] = contrib[center * 2 - i];

            weight = weight * 2 + (float)1.0;

            for (int i = 0; i <= center; i++)
                normContrib[i] = contrib[i] / weight;

            for (int i = center + 1; i < nDots; i++)
                normContrib[i] = normContrib[center * 2 - i];
        }

        int determineResultSize(int w, int h)
        {
            double scaleH, scaleV;
            scaleH = (double)w / (double)width;
            scaleV = (double)h / (double)height;
            if (scaleH >= 1.0 && scaleV >= 1.0)
                return 1;
            return 0;
        }

        private void VerticalFiltering_Ptr(int iW, int iH, int iOutH)
        {
            ImageBuf_dest = (uint*)Marshal.AllocHGlobal(sizeof(uint) * iW * iOutH);

            Parallel.For(0, iOutH, y =>
            {

                int Y = (int)(((double)y) * ((double)iH) / ((double)iOutH) + 0.5);

                int start;
                int startY = Y - nHalfDots;

                int stop;
                int stopY = Y + nHalfDots;

                start = (startY < 0) ? nHalfDots - Y : 0;
                startY = (startY < 0) ? 0 : startY;


                stop = (stopY > (int)(iH - 1)) ? nHalfDots + (iH - 1 - Y) : stop = nHalfDots * 2;
                stopY = (stopY > (int)(iH - 1)) ? iH - 1 : stopY;

                double weight = 0;
                int _i = 0;
                for (_i = start; _i <= stop; _i++) weight += contrib[_i];
                double[] _tmpContrib = new double[nDots];
                for (_i = start; _i <= stop; _i++) _tmpContrib[_i] = contrib[_i] / weight;

                for (int x = 0; x < iW; x++)
                {
                    double valueRed = 0.0, valueGreen = 0.0, valueBlue = 0.0;
                    for (int i = startY, j = start; i <= stopY; i++, j++)
                    {
                        double tmp = _tmpContrib[j];
                        uint pixel = ImageBuf_2nd[i * iW + x];

                        valueRed += ((pixel >> 16) & 0xff) * tmp;
                        valueGreen += ((pixel >> 8) & 0xff) * tmp;
                        valueBlue += (pixel & 0xff) * tmp;
                    }

                    byte r = (byte)((valueRed > 255) ? 255 : ((valueRed < 0) ? 0 : valueRed));
                    byte g = (byte)((valueGreen > 255) ? 255 : ((valueGreen < 0) ? 0 : valueGreen));
                    byte b = (byte)((valueBlue > 255) ? 255 : ((valueBlue < 0) ? 0 : valueBlue));
                    ImageBuf_dest[y * iW + x] = (uint)(r << 16 | g << 8 | b);
                }
            });

            Marshal.FreeHGlobal((IntPtr)ImageBuf_2nd);
        }

        private void HorizontalFiltering_Ptr(int dwInW, int dwInH, int iOutW)
        {
            ImageBuf_2nd = (uint*)Marshal.AllocHGlobal(sizeof(uint) * iOutW * dwInH);

            Parallel.For(0, iOutW, x =>
            {

                int X = (int)(((double)x) * ((double)dwInW) / ((double)iOutW) + 0.5);
                int y = 0;

                int start;
                int startX = X - nHalfDots;

                int stop;
                int stopX = X + nHalfDots;

                start = (startX < 0) ? nHalfDots - X : 0;
                startX = (startX < 0) ? 0 : startX;

                stop = (stopX > (dwInW - 1)) ? nHalfDots + (dwInW - 1 - X) : nHalfDots * 2;
                stopX = (stopX > (dwInW - 1)) ? dwInW - 1 : stopX;

                double weight = 0;
                int _i = 0;
                for (_i = start; _i <= stop; _i++)
                    weight += contrib[_i];
                double[] _tmpContrib = new double[nDots];
                for (_i = start; _i <= stop; _i++)
                    _tmpContrib[_i] = contrib[_i] / weight;

                for (y = 0; y < dwInH; y++)
                {
                    double valueBlue = 0, valueGreen = 0, valueRed = 0;
                    for (int i = startX, j = start; i <= stopX; i++, j++)
                    {
                        double tmp = _tmpContrib[j];
                        uint pixel = ImageBuf_org[y * width + i];

                        valueRed += ((pixel >> 16) & 0xff) * tmp;
                        valueGreen += ((pixel >> 8) & 0xff) * tmp;
                        valueBlue += (pixel & 0xff) * tmp;
                    }

                    byte r = (byte)((valueRed > 255) ? 255 : ((valueRed < 0) ? 0 : valueRed));
                    byte g = (byte)((valueGreen > 255) ? 255 : ((valueGreen < 0) ? 0 : valueGreen));
                    byte b = (byte)((valueBlue > 255) ? 255 : ((valueBlue < 0) ? 0 : valueBlue));
                    ImageBuf_2nd[y * iOutW + x] = (uint)(r << 16 | g << 8 | b);

                }

            });

            Marshal.FreeHGlobal((IntPtr)ImageBuf_org);
        }



        Bitmap VerticalFiltering(Bitmap pbImage, int iOutH)
        {
            int iW = pbImage.Width;
            int iH = pbImage.Height;

            Bitmap pbOut = new Bitmap(iW, iOutH, PixelFormat.Format24bppRgb);

            FastBitmap processor = new FastBitmap(pbImage);
            processor.LockImage();

            FastBitmap processor_out = new FastBitmap(pbOut);
            processor_out.LockImage();


            Parallel.For(0, iOutH, y =>
            {

                FastBitmap.PixelData* data;

                int Y = (int)(((double)y) * ((double)iH) / ((double)iOutH) + 0.5);

                int start;
                int startY = Y - nHalfDots;

                int stop;
                int stopY = Y + nHalfDots;

                start = (startY < 0) ? nHalfDots - Y : 0;
                startY = (startY < 0) ? 0 : startY;


                stop = (stopY > (int)(iH - 1)) ? nHalfDots + (iH - 1 - Y) : stop = nHalfDots * 2;
                stopY = (stopY > (int)(iH - 1)) ? iH - 1 : stopY;

                double weight = 0;
                int _i = 0;
                for (_i = start; _i <= stop; _i++) weight += contrib[_i];
                double[] _tmpContrib = new double[nDots];
                for (_i = start; _i <= stop; _i++) _tmpContrib[_i] = contrib[_i] / weight;

                for (int x = 0; x < iW; x++)
                {
                    double valueRed = 0.0, valueGreen = 0.0, valueBlue = 0.0;
                    for (int i = startY, j = start; i <= stopY; i++, j++)
                    {
                        double tmp = _tmpContrib[j];
                        data = (FastBitmap.PixelData*)(processor.pBase + i * processor.width + x * pdsize);
                        valueRed += data->red * tmp;
                        valueGreen += data->green * tmp;
                        valueBlue += data->blue * tmp;
                    }

                    data = (FastBitmap.PixelData*)(processor_out.pBase + y * processor_out.width + x * pdsize);

                    data->red = (byte)((valueRed > 255) ? 255 : ((valueRed < 0) ? 0 : valueRed));
                    data->green = (byte)((valueGreen > 255) ? 255 : ((valueGreen < 0) ? 0 : valueGreen));
                    data->blue = (byte)((valueBlue > 255) ? 255 : ((valueBlue < 0) ? 0 : valueBlue));
                }


            });
            processor.UnlockImage();
            processor_out.UnlockImage();

            return pbOut;

        }

        int pdsize = sizeof(FastBitmap.PixelData);

        Bitmap HorizontalFiltering(Bitmap bufImage, int iOutW)
        {
            int dwInW = bufImage.Width;
            int dwInH = bufImage.Height;

            Bitmap pbOut = new Bitmap(iOutW, dwInH, PixelFormat.Format24bppRgb);

            FastBitmap processor = new FastBitmap(bufImage);
            processor.LockImage();

            FastBitmap processor_out = new FastBitmap(pbOut);
            processor_out.LockImage();

            Parallel.For(0, iOutW, x =>
            {
                FastBitmap.PixelData* data;

                int X = (int)(((double)x) * ((double)dwInW) / ((double)iOutW) + 0.5);
                int y = 0;

                int start;
                int startX = X - nHalfDots;

                int stop;
                int stopX = X + nHalfDots;

                start = (startX < 0) ? nHalfDots - X : 0;
                startX = (startX < 0) ? 0 : startX;

                stop = (stopX > (dwInW - 1)) ? nHalfDots + (dwInW - 1 - X) : nHalfDots * 2;
                stopX = (stopX > (dwInW - 1)) ? dwInW - 1 : stopX;

                double weight = 0;
                int _i = 0;
                for (_i = start; _i <= stop; _i++)
                    weight += contrib[_i];
                double[] _tmpContrib = new double[nDots];
                for (_i = start; _i <= stop; _i++)
                    _tmpContrib[_i] = contrib[_i] / weight;

                for (y = 0; y < dwInH; y++)
                {
                    double valueBlue = 0, valueGreen = 0, valueRed = 0;
                    for (int i = startX, j = start; i <= stopX; i++, j++)
                    {
                        double tmp = _tmpContrib[j];
                        data = (FastBitmap.PixelData*)(processor.pBase + y * processor.width + i * pdsize);

                        valueRed += data->red * tmp;
                        valueGreen += data->green * tmp;
                        valueBlue += data->blue * tmp;
                    }

                    data = (FastBitmap.PixelData*)(processor_out.pBase + y * processor_out.width + x * pdsize);
                    data->red = (byte)((valueRed > 255) ? 255 : ((valueRed < 0) ? 0 : valueRed));
                    data->green = (byte)((valueGreen > 255) ? 255 : ((valueGreen < 0) ? 0 : valueGreen));
                    data->blue = (byte)((valueBlue > 255) ? 255 : ((valueBlue < 0) ? 0 : valueBlue));
                }

            });

            processor.UnlockImage();
            processor_out.UnlockImage();
            return pbOut;
        }
    }
}
