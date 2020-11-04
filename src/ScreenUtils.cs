using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

namespace GallonHelpers
{

    /// <summary>
    /// Created by Lilian Gallon, 11/01/2020
    /// 
    /// Provides utility functions to read the content of
    /// the main screen. Works with full-screen apps. Optimized
    /// as much as possible.
    /// 
    /// </summary>
    public static class ScreenUtils
    {
        /// <summary>
        /// 
        /// Takes a screenshot of the main screen. The placement of
        /// that screenshot corresponds to the rect given in argument.
        /// If the given rectangle is empty, it will take a screenshot
        /// of the main screen from (0, 0) to (1920, 1080).
        /// 
        /// !! YOU NEED TO DISPOSE THE RETURNED BITMAP TO PREVENT MEMORY LEAKS !!
        /// 
        /// TODO: find the screensize?
        /// 
        /// </summary>
        /// <param name="area">Rectangle that defines the screenshot</param>
        /// <returns>A bitmap containing the image data</returns>
        public static Bitmap CaptureFromScreen(Rectangle area)
        {
            if (area == Rectangle.Empty)
            {
                // We suppose that the user has a 1920x1080 screen
                area = new Rectangle(0, 0, 1920, 1080);
            }

            // Prepare resources
            Bitmap screenshot = new Bitmap(area.Width, area.Height);
            Graphics capture = Graphics.FromImage(screenshot);

            // Perform screenshot
            capture.CopyFromScreen(area.X, area.Y, 0, 0, area.Size, CopyPixelOperation.SourceCopy);

            // Clear resources
            capture.Dispose();

            return screenshot;
        }

        /// <summary>
        /// It calculates the average color of the given bitmap.
        /// Works with 32 and 24 bit images.
        /// </summary>
        /// 
        /// <param name="image">The image</param>
        /// <returns>The average color of that image</returns>
        public static Color CalculateAverageColor(Bitmap image)
        {
            int red = 0;
            int green = 0;
            int blue = 0;
            int minDiversion = 15; // drop pixels that do not differ by at least minDiversion between color values (white, gray or black)
            int dropped = 0; // keep track of dropped pixels
            long[] totals = new long[] { 0, 0, 0 };
            int bppModifier = image.PixelFormat == PixelFormat.Format24bppRgb ? 3 : 4; // cutting corners, will fail on anything else but 32 and 24 bit images

            BitmapData srcData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, image.PixelFormat);
            int stride = srcData.Stride;
            IntPtr Scan0 = srcData.Scan0;

            unsafe
            {
                byte* p = (byte*)(void*)Scan0;

                for (int y = 0; y < image.Height; y++)
                {
                    for (int x = 0; x < image.Width; x++)
                    {
                        int idx = (y * stride) + x * bppModifier;

                        red   = p[idx + 2];
                        green = p[idx + 1];
                        blue  = p[idx];

                        if (Math.Abs(red - green) > minDiversion || Math.Abs(red - blue) > minDiversion || Math.Abs(green - blue) > minDiversion)
                        {
                            totals[2] += red;
                            totals[1] += green;
                            totals[0] += blue;
                        }
                        else
                        {
                            dropped++;
                        }
                    }
                }
            }

            int count = image.Width * image.Height - dropped;

            int avgR = count > 0 ? (int)(totals[2] / count) : 0;
            int avgG = count > 0 ? (int)(totals[1] / count) : 0;
            int avgB = count > 0 ? (int)(totals[0] / count) : 0;

            return Color.FromArgb(avgR, avgG, avgB);
        }

        /// <summary>
        /// 
        /// Takes a screenshot (ScreenUtils#CaptureFromScreen()) and
        /// computes the average color (ScreenUtils#CalculateAverageColor()).
        /// 
        /// Also clears the allocated resources to prevent memory leaks.
        /// 
        /// </summary>
        /// <param name="area">Rectangle that defines the screenshot</param>
        /// <returns>The average color of the main screen for the given area</returns>
        public static Color CalculateAverageScreenColorAt(Rectangle area)
        {
            Bitmap screenshot = CaptureFromScreen(area);
            Color color = CalculateAverageColor(screenshot);

            // Needed to prevent memory leaks
            screenshot.Dispose();

            return color;
        }
    }
}
