﻿using SQLite;
using SQLiteNetExtensions.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace DLuOvBamG.Models
{
    public class CategoryTag
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }

        [ManyToMany(typeof(PictureTags))]
        public List<Picture> Pictures { get; set; }
    }
}
