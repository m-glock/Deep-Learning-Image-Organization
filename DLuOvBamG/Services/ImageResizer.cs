﻿using System.IO;
using Android.Graphics;


namespace DLuOvBamG
{
    public static class ImageResizer
    {

        public static byte[] ResizeImage(byte[] imageData, float width, float height)
        {
            return ResizeImageAndroid(imageData, width, height);
        }
    
        public static byte[] ResizeImageAndroid(byte[] imageData, float width, float height)
        {
            // Load the bitmap 
            Bitmap originalImage = BitmapFactory.DecodeByteArray(imageData, 0, imageData.Length);
            
            float ZielHoehe = 0;
            float ZielBreite = 0;
            
            var Hoehe = originalImage.Height;
            var Breite = originalImage.Width;
            
            if (Hoehe > Breite) // Höhe (71 für Avatar) ist Master
            {
                ZielHoehe = height;
                float teiler = Hoehe / height;
                ZielBreite = Breite / teiler;
            }
            else // Breite (61 für Avatar) ist Master
            {
                ZielBreite = width;
                float teiler = Breite / width;
                ZielHoehe = Hoehe / teiler;
            }
            
            Bitmap resizedImage = Bitmap.CreateScaledBitmap(originalImage, (int)ZielBreite, (int)ZielHoehe, false);
            
            using (MemoryStream ms = new MemoryStream())
            {
                resizedImage.Compress(Bitmap.CompressFormat.Jpeg, 100, ms);
                return ms.ToArray();
            }
        }

    }
}