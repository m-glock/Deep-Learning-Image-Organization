﻿using DLuOvBamG.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DLuOvBamG.Services
{
    public class ImageOrganizationDatabase
    {
        // static field to ensure that a single database connection is used for the lifetime of the APP
        static readonly Lazy<SQLiteAsyncConnection> lazyInitializer = new Lazy<SQLiteAsyncConnection>(() =>
        {
            return new SQLiteAsyncConnection(Constants.DatabasePath, Constants.Flags);
        });

        static SQLiteAsyncConnection Database => lazyInitializer.Value;
        static bool initialized = false;

        public ImageOrganizationDatabase()
        {
            InitializeAsync().SafeFireAndForget(false);
        }

        // check if table to save Pictures already exists, else create one
        async Task InitializeAsync()
        {
            if (!initialized)
            {
                if (!Database.TableMappings.Any(m => m.MappedType.Name == typeof(Picture).Name))
                {
                    await Database.CreateTablesAsync(CreateFlags.None, typeof(Picture)).ConfigureAwait(false);
                    initialized = true;
                }
            }
        }

        public Task<List<Picture>> GetPicturesAsync()
        {
            return Database.Table<Picture>().ToListAsync();
        }


        public Task<Picture> GetPicutreAsync(int id)
        {
            return Database.Table<Picture>().Where(i => i.Id == id).FirstOrDefaultAsync();
        }

        public Task<int> SavePictureAsync(Picture picture)
        {
            if (picture.Id != 0)
            {
                return Database.UpdateAsync(picture);
            }
            else
            {
                return Database.InsertAsync(picture);
            }
        }

        public Task<int> DeletePictureAsync(Picture picture)
        {
            return Database.DeleteAsync(picture);
        }
    }
}
