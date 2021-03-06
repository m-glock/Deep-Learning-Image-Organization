﻿using Android.Media;
using Android.Content;
using Android.Database;
using Android.Provider;
using DLuOvBamG.Droid;
using DLuOvBamG.Services;
using System;
using System.IO;
using System.Text.RegularExpressions;
using Xamarin.Forms;
using static Android.Provider.MediaStore.Images;
using DLToolkit.Forms.Controls;
using DLuOvBamG.Models;
using System.Linq;
using Point = Xamarin.Forms.Point;

[assembly: Dependency(typeof(ImageService))]
namespace DLuOvBamG.Droid
{
    class ImageService : IImageService
    {
        private static Android.Net.Uri InternalContentUri = MediaStore.Images.Media.InternalContentUri;
        private static Android.Net.Uri ExternalContentUri = MediaStore.Images.Media.ExternalContentUri;
        private Context CurrentContext = Android.App.Application.Context;
        private static readonly Regex r = new Regex(":");
        private static GeoService GeoService = new GeoService();

        public DateTime GetDateTaken(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new ArgumentException("file does not exist path:{0}", filePath);
            }
            ExifInterface exif = new ExifInterface(filePath);
            string dateTaken = exif.GetAttribute(ExifInterface.TagDatetime);
            dateTaken = r.Replace(dateTaken, "-", 2);
            return DateTime.Parse(dateTaken);
        }

        public byte[] GetFileBytes(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new ArgumentException("file does not exist path:{0}", filePath);
            }

            return File.ReadAllBytes(filePath);
        }

        public void DeleteImage(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                CallBroadCast(filePath);
            }
        }

        public void CallBroadCast(string filepath)
        {
            MediaScannerConnection.ScanFile(
                CurrentContext,
                new String[] { filepath },
                null,
                null
            );
        }

        public Picture[] GetAllImagesFromDevice(FlowObservableCollection<Grouping<string, Picture>> collection, DateTime? dateFilter)
        {
            Picture[] internalPictures = GetImagesFromUri(InternalContentUri, collection, dateFilter);

            Picture[] externalPictures = new Picture[0];
            Boolean isSDPresent = Android.OS.Environment.ExternalStorageState.Equals(Android.OS.Environment.MediaMounted);
            if (isSDPresent)
            {
                externalPictures = GetImagesFromUri(ExternalContentUri, collection, dateFilter);
            }

            if (externalPictures.Length != 0)
            {
                Picture[] result = new Picture[internalPictures.Length + externalPictures.Length];
                internalPictures.CopyTo(result, 0);
                externalPictures.CopyTo(result, internalPictures.Length);
                return result;
            }
            else
            {
                return internalPictures;
            }
        }

        private Picture[] GetImagesFromUri(Android.Net.Uri uri, FlowObservableCollection<Grouping<string, Picture>> collection, DateTime? dateFilter)
        {
            // A list of which columns to return. Passing null will return all columns, which is inefficient.
            string[] projection = {
                ImageColumns.Data,
                ImageColumns.BucketDisplayName,
                ImageColumns.Id,
                ImageColumns.Height,
                ImageColumns.Width,
                ImageColumns.IsPrivate,
                ImageColumns.Size,
                ImageColumns.DateAdded,
                ImageColumns.DateTaken,
            };
            // How to order the rows, formatted as an SQL ORDER BY clause
            string orderBy = ImageColumns.Id;

            string selection = null;
            string[] selectionArgs = null;

            if (dateFilter.HasValue)
            {
                DateTimeOffset dateTimeOffset = new DateTimeOffset(dateFilter.Value);
                selection = ImageColumns.DateAdded + ">?";
                selectionArgs = new string[] { "" + dateTimeOffset.ToUnixTimeSeconds() };
            }
            //Stores all the images from the gallery in Cursor
            ICursor cursor = CurrentContext.ContentResolver.Query(uri, projection, selection, selectionArgs, orderBy);
            //Total number of images
            int count = cursor.Count;

            //Create an array to store path to all the images
            Picture[] arrPictures = new Picture[count];
            int pathIndex = cursor.GetColumnIndex(ImageColumns.Data);
            int bucketIndex = cursor.GetColumnIndex(ImageColumns.BucketDisplayName);
            int idIndex = cursor.GetColumnIndex(ImageColumns.Id);
            int heightIndex = cursor.GetColumnIndex(ImageColumns.Height);
            int widthIndex = cursor.GetColumnIndex(ImageColumns.Width);
            int isPrivateIndex = cursor.GetColumnIndex(ImageColumns.IsPrivate);
            int sizeIndex = cursor.GetColumnIndex(ImageColumns.Size);
            int dateTakenIndex = cursor.GetColumnIndex(ImageColumns.DateTaken);
            int dateAddedIndex = cursor.GetColumnIndex(ImageColumns.DateAdded);

            for (int i = 0; i < count; i++)
            {
                cursor.MoveToPosition(i);
                string path = cursor.GetString(pathIndex);
                string bucketName = cursor.GetString(bucketIndex);
                string id = cursor.GetString(idIndex);
                string height = cursor.GetString(heightIndex);
                string width = cursor.GetString(widthIndex);
                string isPrivate = cursor.GetString(isPrivateIndex);
                string size = cursor.GetString(sizeIndex);
                string dateTaken = cursor.GetString(dateTakenIndex);
                string dateAdded = cursor.GetString(dateAddedIndex);
                DateTime datetimeTaken = ConvertFromUnixTimestamp(Convert.ToDouble(dateTaken) / 1000);
                DateTime datetimeAdded = ConvertFromUnixTimestamp(Convert.ToDouble(dateAdded));

                Point geoLocation = GeoService.GetGeoLocations(path);

                Picture newPicture = new Picture
                {
                    Uri = path,
                    Date = datetimeTaken,
                    Longitude = geoLocation.Y.ToString(),
                    Latitude = geoLocation.X.ToString(),
                    Size = size,
                    Height = height,
                    Width = width,
                    DirectoryName = bucketName,
                    ImageSource = ImageSource.FromFile(path),
                };
                arrPictures[i] = newPicture;
                AddPictureToGroup(collection, newPicture);
            }

            // The cursor should be freed up after use with close()
            cursor.Close();
            Console.WriteLine("all pictures done");
            return arrPictures;
        }

        private void AddPictureToGroup(FlowObservableCollection<Grouping<string, Models.Picture>> collection, Models.Picture picture)
        {
            foreach (var group in collection)
            {
                if (group.Key == picture.DirectoryName)
                {
                    group.Add(picture);
                    group.ColumnCount++;
                    collection.OrderByDescending(item => item.ColumnCount);
                    return;
                }
            }
            Grouping<string, Models.Picture> newGroup = new Grouping<string, Models.Picture>(picture.DirectoryName);
            newGroup.Add(picture);
            collection.Add(newGroup);
        }

        private static DateTime ConvertFromUnixTimestamp(double timestamp)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return origin.AddSeconds(timestamp);
        }
    }
}