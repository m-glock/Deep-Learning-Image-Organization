﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace DLuOvBamG.Services
{
    public class ImageFileStorage : IImagagFileStorage
    {
        public Task DeleteFileAsync(string filePath)
        {
            throw new NotImplementedException();
        }

        public async Task<string[]> GetFilesFromDirectory(string folderPath)
        {
            var status = await CheckAndRequestExternalStoragePermissionAsync();
            if (status != PermissionStatus.Granted)
            {
                // Notify user permission was denied
                string[] empty = new string[] { };
                return empty;
            }

            string[] filePaths = Directory.GetFiles(folderPath, "*.jpg");
            return filePaths;
        }

        public Task<string> ReadFileAsync(string filePath)
        {
            throw new NotImplementedException();
        }

        private async Task<PermissionStatus> CheckAndRequestExternalStoragePermissionAsync()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.StorageRead>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.StorageRead>();
            }

            return status;
        }
    }
}
