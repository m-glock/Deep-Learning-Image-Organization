﻿using Android.Graphics;
using DLuOvBamG.Models;

namespace DLuOvBamG
{
    public class BrightnessClassifier
    {
        public float Threshold = ScanOptionsEnum.darkPics.GetDefaultPresicionValue() * 10;
        private int thresholdDark = 30;
        private int thresholdBright = 600;

        private int width = 128; 
        private int height = 128;

        public double[] Classify(byte[] bytes)
        {
            // change and resize picture
            Bitmap bitmap = BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length);
            Bitmap resizedBitmap = Bitmap.CreateScaledBitmap(bitmap, width, height, true);

            int[] intValues = new int[width * height];
            resizedBitmap.GetPixels(intValues, 0, resizedBitmap.Width, 0, 0, resizedBitmap.Width, resizedBitmap.Height);

            // count pixel that are too bright or too dark
            int pixel = 0;
            int darkPixels = 0;
            int brightPixels = 0;
            for (int i = 0; i < width; ++i)
            {
                for (int j = 0; j < height; ++j)
                {
                    int val = intValues[pixel++];

                    int red = (val >> 16) & 0x000000FF;
                    int green = (val >> 8) & 0x000000FF;
                    int blue = (val) & 0x000000FF;

                    if (red + green + blue < thresholdDark)
                        darkPixels++;
                    if (red + green + blue > thresholdBright)
                        brightPixels++;

                }
            }

            // determine how many pixel are too bright/dark and whether the whole picture qualifies as too dark/bright
            double darkPercentage = (float)darkPixels / (width * height) * 100;
            double brightPercentage = (float)brightPixels / (width * height) * 100;

            return new double[] { darkPercentage, brightPercentage }; 
        }
    }
}
