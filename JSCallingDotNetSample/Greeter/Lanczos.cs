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

    // 參考 http://blog.csdn.net/yangzl2008/article/details/6693678 原Java Code , 網址失效
    unsafe class Lanczos
    {
        int nDots;
        int nHalfDots;
        const double PI = Math.PI;
        const double support = 3.0;
        double[] contrib ;
        double[] normContrib ;
        int scaleWidth;
        int height_org, width_org;

        uint[] ImageBuf_org ;

        uint[] ImageBuf_2nd ;

        uint[] ImageBuf_dest ;

        byte[] image_bytes_org;

        //Bitmap rgbImage = null;

        public string call_test(string a)
        {
            return "from c# " + a;
        }

        public Lanczos(byte[] image_bytes , int org_w , int org_h   )
        {
            //rgbImage = t;

            image_bytes_org = image_bytes;

            width_org = org_w;
            height_org = org_h;
            ImageBuf_org = new uint[width_org * height_org];// (uint*)Marshal.AllocHGlobal(sizeof(uint) * width_org * height_org);

            for (int y = 0; y < height_org; y++)
            {
                for (int x = 0; x < width_org; x++)
                {
                    /*red = imgData[PixelIndex * 4 +0]
                    green = imgData[(PixelIndex * 4) + 1]
                    blue = imgData[(PixelIndex * 4) + 2]
                    alpha = imgData[(PixelIndex * 4) + 3]*/
                    int offset = y * width_org + x;
                    //ImageBuf_org[y * width + x] = (uint)((data->red << 16) | (data->green << 8) | data->blue);
                    ImageBuf_org[y * width_org + x] = (uint)((image_bytes[offset*4] << 16) | (image_bytes[offset*4+1] << 8) | image_bytes[offset*4+2]);

                }

            }

        }



        public byte [] ResizeLanczos(int rwidth, int rheight)
        {

            Console.WriteLine("\nresize image...");

            Console.WriteLine(rwidth + ":" + rheight);


            //Marshal.FreeHGlobal((IntPtr)ImageBuf_dest);


            ImageBuf_dest = new uint[rwidth * rheight]; //;// (uint*)Marshal.AllocHGlobal(sizeof(uint) * rwidth * rheight);

            int w = rwidth, h = rheight;

            double sx = w / (double)width_org;
            double sy = h / (double)height_org;

            w = (int)(sx * width_org);
            h = (int)(sy * height_org);

            scaleWidth = w;

            if (determineResultSize(w, h) == 1)
            {
                Console.WriteLine("resize image done.");
                return image_bytes_org;
            }

            Stopwatch st = new Stopwatch();
            calContrib();

            st.Restart();

            //Bitmap pbFinalOut = VerticalFiltering(HorizontalFiltering(rgbImage, w), h);


            HorizontalFiltering_Ptr(width_org, height_org, rwidth);
            VerticalFiltering_Ptr(rwidth, height_org, rheight);
       

            st.Stop();
            Console.WriteLine("reszie time : " + st.ElapsedMilliseconds);


            Console.WriteLine("resize image done.");
 
            byte[] ret_bytes = new byte[rwidth * rheight * 4];

            for (int y = 0; y < rheight; y++)
            {
                for (int x = 0; x < rwidth; x++)
                {
                    /*red = imgData[PixelIndex * 4 +0]
                    green = imgData[(PixelIndex * 4) + 1]
                    blue = imgData[(PixelIndex * 4) + 2]
                    alpha = imgData[(PixelIndex * 4) + 3]*/

                    uint pixel = ImageBuf_dest[y * rwidth+ x];

                    byte r = (byte)((pixel >> 16) & 0xff);
                    byte g = (byte)((pixel >> 8) & 0xff);
                    byte b = (byte)(pixel & 0xff);
                    

                    int offset = y * rwidth + x;

                    ret_bytes[offset*4] = r;
                    ret_bytes[offset*4 + 1] = g;
                    ret_bytes[offset*4 + 2] = b;
                    ret_bytes[offset * 4 + 3] = 255;// ;


                    //ImageBuf_org[y * width + x] = (uint)((data->red << 16) | (data->green << 8) | data->blue);
                    //ImageBuf_dest[y * width_org + x] = (uint)((ret_bytes[offset * 4] << 16) | (ret_bytes[offset * 4 + 1] << 8) | ret_bytes[offset * 4 + 2]);

                }

            }


            return ret_bytes;

        }


        double LanczosCal(int i, int inWidth, int outWidth, double Support)
        {
            double x = (double)i * (double)outWidth / (double)inWidth;
            return Math.Sin(x * PI) / (x * PI) * Math.Sin(x * PI / Support) / (x * PI / Support);
        }

        void calContrib()
        {
            nHalfDots = (int)((double)width_org * support / (double)scaleWidth);
            nDots = nHalfDots * 2 + 1;

            contrib = new double[nDots];
            normContrib = new double[nDots];

            int center = nHalfDots;
            contrib[center] = 1.0;

            double weight = 0.0;

            for (int i = 1; i <= center; i++)
            {
                contrib[center + i] = LanczosCal(i, width_org, scaleWidth, support);
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
            scaleH = (double)w / (double)width_org;
            scaleV = (double)h / (double)height_org;
            if (scaleH >= 1.0 && scaleV >= 1.0)
                return 1;
            return 0;
        }

        private void VerticalFiltering_Ptr(int iW, int iH, int iOutH)
        {
            ImageBuf_dest = new uint[iW * iOutH];// (uint*)Marshal.AllocHGlobal(sizeof(uint) * iW * iOutH);

            for(int y=0; y<iOutH; y++)
            //Parallel.For(0, iOutH, y =>
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
            }//);

           // Marshal.FreeHGlobal((IntPtr)ImageBuf_2nd);
        }

        private void HorizontalFiltering_Ptr(int dwInW, int dwInH, int iOutW)
        {
            ImageBuf_2nd = new uint[iOutW * dwInH]; //  (uint*)Marshal.AllocHGlobal(sizeof(uint) * iOutW * dwInH);

           for(int x= 0 ; x<iOutW; x++)
            //Parallel.For(0, iOutW, x =>
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
                        uint pixel = ImageBuf_org[y * width_org + i];

                        valueRed += ((pixel >> 16) & 0xff) * tmp;
                        valueGreen += ((pixel >> 8) & 0xff) * tmp;
                        valueBlue += (pixel & 0xff) * tmp;
                    }

                    byte r = (byte)((valueRed > 255) ? 255 : ((valueRed < 0) ? 0 : valueRed));
                    byte g = (byte)((valueGreen > 255) ? 255 : ((valueGreen < 0) ? 0 : valueGreen));
                    byte b = (byte)((valueBlue > 255) ? 255 : ((valueBlue < 0) ? 0 : valueBlue));
                    ImageBuf_2nd[y * iOutW + x] = (uint)(r << 16 | g << 8 | b);

                }

            }//);

           // Marshal.FreeHGlobal((IntPtr)ImageBuf_org);
        }

    }
}
