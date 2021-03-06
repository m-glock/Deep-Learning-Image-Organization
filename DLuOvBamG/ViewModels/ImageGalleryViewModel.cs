﻿using DLuOvBamG.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;
using Xamarin.Forms;
using DLuOvBamG.Views;
using DLuOvBamG.Services;
using System.IO;
using System.Linq;
using DLToolkit.Forms.Controls;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;
using Xamarin.Forms.Internals;
using Xamarin.Essentials;

namespace DLuOvBamG.ViewModels
{
    public delegate void PictureDeletedEventHandler(object source, PictureDeletedEvent e);
    public class ImageGalleryViewModel : INotifyPropertyChanged
    {
        readonly IImageService imageService = DependencyService.Get<IImageService>();
        readonly ImageFileStorage imageFileStorage = new ImageFileStorage();
        readonly IClassifier classifier = App.Classifier;
        readonly ImageOrganizationDatabase db = App.Database;
        readonly GeoService geoService = new GeoService();

        private FlowObservableCollection<Grouping<string, Picture>> albumItems;
        private FlowObservableCollection<Grouping<string, Picture>> groupedItems;
        private string SelectedGroup { get; set; }
        private string SelectedSort { get; set; }
        public List<Picture> Items { get; set; }
        public INavigation Navigation;
        public event PropertyChangedEventHandler PropertyChanged;

        #region propertyChanged
        public FlowObservableCollection<Grouping<string, Picture>> GroupedItems
        {
            set
            {
                groupedItems = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GroupedItems"));
            }

            get
            {
                return groupedItems;
            }
        }

        public FlowObservableCollection<Grouping<string, Picture>> AlbumItems
        {
            set
            {
                albumItems = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AlbumItems"));
            }

            get
            {
                return albumItems;
            }
        }
        #endregion

        public ImageGalleryViewModel()
        {
            Items = new List<Picture>();
            GroupedItems = new FlowObservableCollection<Grouping<string, Picture>>();
            AlbumItems = new FlowObservableCollection<Grouping<string, Picture>>();
            SelectedGroup = "";
            Messenger.Default.Register<PictureDeletedEvent>(this, OnPictureDeleted);
        }

        public async void GetPictures()
        {
            // try to get pictures from db, if this fails load them and put them in db
            List<Picture> pictures = await db.GetPicturesAsync();

            if (pictures.Count == 0)
            {
                Picture[] devicePictures = await imageFileStorage.GetPicturesFromDevice(AlbumItems, null);
                bool picturesSaved = await SavePicturesInDB(devicePictures);
                pictures = devicePictures.ToList();
                await Task.Run(async () =>
                {
                    int[] savedCategoryTags = await SaveCategoryTagsInDB();
                    var classified = await ClassifyPictures(pictures);
                    classifier.FeatureVectors = pictures.Select(picture => App.tf.ByteToDoubleArray(picture.FeatureVector)).ToList();
                    classifier.FillFeatureVectorMatix();
                    Console.WriteLine("classify ready");
                });
            }
            else
            {
                // get new pictures taken since last reading date
                DateTime dateFilter = imageFileStorage.GetAppPropertyReadingDate();
                Picture[] newDevicePictures = await imageFileStorage.GetPicturesFromDevice(AlbumItems, dateFilter);
                // save new pictures in db and classify them
                bool picturesSaved = await SavePicturesInDB(newDevicePictures);
                pictures.AddRange(newDevicePictures);
                // set image sources and remove non exist pictures
                pictures = SetImageSources(pictures);
                // set album items
                List<Grouping<string, Picture>> grouped = GroupPicturesByDirectory(pictures);
                AlbumItems = new FlowObservableCollection<Grouping<string, Picture>>(grouped);
                
                // update thumbnail of album
                OrderAllAlbumsByDate();

                await Task.Run(async () =>
                {
                    // classify new pictures
                    await ClassifyPictures(newDevicePictures.ToList());
                    // fill feature vector matrik
                    if (classifier.FeatureVectors.Count == 0)
                        classifier.FeatureVectors = pictures.Select(picture => App.tf.ByteToDoubleArray(picture.FeatureVector)).ToList();
                    classifier.FillFeatureVectorMatix();
                });
            }
            SelectedSort = "Directory";
            App.CurrentSortKey = "Directory";
            SetLocations(pictures);
            Items = pictures;
        }

        /*
         * Sets ImageSource of all pictures. Deletes Picture from DB if file dont exist
         */
        List<Picture> SetImageSources(List<Picture> pictures)
        {
            List<Picture> picturesWithSource = new List<Picture>();
            pictures.ForEach(async picture =>
            {
                // if file exists
                if (File.Exists(picture.Uri))
                {
                    picture.ImageSource = ImageSource.FromFile(picture.Uri);
                    picturesWithSource.Add(picture);
                }
                else
                {
                    // else delete image from db
                    int deletedPicture = await imageFileStorage.DeleteFileAsync(picture);
                }
            });
            return picturesWithSource;
        }

        async void SetLocations(List<Picture> pictures)
        {
            // filter pictures withoud geo locations and already set location
            List<Picture> pictureWithGeoLocation = pictures.Where(
                picture =>  picture.Latitude != "0" && picture.Longitude != "0" && picture.Location == null
            ).ToList();

            // set locations of all pictures
            if(pictureWithGeoLocation.Count > 0)
            {
                var setLocationTasks = pictureWithGeoLocation.Select(picture => SetLocation(picture));
                await Task.WhenAll(setLocationTasks);
            }
        }

        async Task SetLocation(Picture picture)
        {
            double latitude = Convert.ToDouble(picture.Latitude);
            double longitude = Convert.ToDouble(picture.Longitude);
            Placemark placemark = await geoService.GetPlacemark(latitude, longitude);
            if(placemark != null)
            {
                picture.Location = placemark.Locality;
                db.SavePictureAsync(picture);
            }
        }

        void OrderAllAlbumsByDate()
        {
            AlbumItems.ForEach(album =>
            {
                List<Picture> orderd = album.OrderByDescending(item => item.Date).ToList();
                album.Clear();
                album.AddRange(orderd);
            });
        }

        List<Grouping<string, Picture>> GroupPicturesByDate(List<Picture> pictures)
        {
            return pictures
                .OrderByDescending(item => item.Date)
                .GroupBy(item => item.Date.Date.ToShortDateString())
                .Select(itemGroup => new Grouping<string, Picture>(itemGroup.Key, itemGroup, itemGroup.Count()))
                .ToList();
        }

        List<Grouping<string, Picture>> GroupPicturesByLocation(List<Picture> pictures)
        {
            var grouped = pictures
                .OrderByDescending(item => item.Date)
                .GroupBy(item => item.Location)
                .Select(itemGroup => new Grouping<string, Picture>(itemGroup.Key, itemGroup, itemGroup.Count()))
                .ToList();
            return grouped;
        }

        /*
         * create a group for each category tag
         */
        async Task<List<Grouping<string, Picture>>> GroupPicturesByCategoryAsync(List<Picture> pictures)
        {
            var tags = await db.GetCategoryTagsWithPicturesAsync();
            var withPictures = tags.Where(tag => tag.Pictures.Count > 0);
            List<Grouping<string, Picture>> grouped = new List<Grouping<string, Picture>>();
            withPictures.ForEach(tag =>
            {
                List<Picture> withImageSource = SetImageSources(tag.Pictures);
                Grouping<string, Picture> group = new Grouping<string, Picture>(tag.Name, withImageSource, withImageSource.Count);
                grouped.Add(group);
            });
            return grouped;
        }

        List<Grouping<string, Picture>> GroupPicturesByDirectory(List<Picture> pictures)
        {
            return pictures
                .OrderByDescending(item => item.Date)
                .GroupBy(item => item.DirectoryName)
                .Select(itemGroup => new Grouping<string, Picture>(itemGroup.Key, itemGroup, itemGroup.Count()))
                .OrderByDescending(item => item.ColumnCount)
                .ToList();
        }

        async Task<bool> SavePicturesInDB(Picture[] pictures)
        {
            if (pictures.Length > 0)
            {
                var tasks = pictures.Select(picture => db.SavePictureAsync(picture));
                await Task.WhenAll(tasks);
                return true;
            }
            return false;
        }

        async Task<int[]> SaveCategoryTagsInDB()
        {
            IAssetsService assetsService = DependencyService.Get<IAssetsService>();
            List<string> labels = assetsService.LoadClassificationLabels();
            List<CategoryTag> categoryTags = labels.Select(label =>
                {
                    return new CategoryTag()
                    {
                        Name = label,
                        IsCustom = false
                    };
                }
            ).ToList();
            var categoryTagsTasks = categoryTags.Select(categoryTag => db.SaveCategoryTagAsync(categoryTag));
            var categoryTagIdArray = await Task.WhenAll(categoryTagsTasks);
            return categoryTagIdArray;
        }

        async Task<bool> ClassifyPictures(List<Picture> pictures)
        {
            if (pictures.Count > 0)
            {
                var classifyTasks = pictures.Select(picture => ClassifyPicture(picture));
                await Task.WhenAll(classifyTasks);

                return true;
            }
            return false;
        }

        async Task ClassifyPicture(Picture picture)
        {
            // get classifications above 10% and put them in a list
            List<CategoryTag> categoryTags = new List<CategoryTag>();
            if (picture.CategoryTags is null)
            {
                picture.CategoryTags = new List<CategoryTag>();
            }
            byte[] fileBytes = imageService.GetFileBytes(picture.Uri);
            classifier.ChangeModel(ScanOptionsEnum.similarPics);

            // get classifications from classifier
            List<ModelClassification> modelClassifications = await classifier.ClassifySimilar(fileBytes);
            var currentVector = GetBytes(classifier.FeatureVectors[classifier.FeatureVectors.Count - 1]);

            // map strings to CategoryTag objects
            modelClassifications.ForEach(classification =>
            {
                CategoryTag categoryTag = new CategoryTag
                {
                    Name = classification.TagName
                };
                categoryTags.Add(categoryTag);
            });

            // find or insert all category tag objects
            categoryTags.ForEach(categoryTag => categoryTag.FindOrInsert());

            // add the categoryTags, now with id, to the picture and update it
            categoryTags.ForEach(categoryTag =>
            {
                picture.CategoryTags.Add(categoryTag);
            });
            picture.FeatureVector = currentVector;
            db.SavePictureAsync(picture);
        }

        public async void OnGroupOptionsSelected(string selectedOption)
        {
            // group picutres by selected options
            List<Grouping<string, Picture>> groupedPictures;
            switch (selectedOption)
            {
                case "Directory":
                    groupedPictures = GroupPicturesByDirectory(Items);
                    break;
                case "Date":
                    groupedPictures = GroupPicturesByDate(Items);
                    break;
                case "Location":
                    groupedPictures = GroupPicturesByLocation(Items);
                    break;
                case "Category":
                    groupedPictures = await GroupPicturesByCategoryAsync(Items);
                    break;
                default:
                    AlbumItems = AlbumItems;
                    return;
            };

            App.CurrentSortKey = selectedOption;
            SelectedSort = selectedOption;
            AlbumItems = new FlowObservableCollection<Grouping<string, Picture>>(groupedPictures);
        }

        public ICommand ItemTappedCommand
        {
            get
            {
                return new Command(async (sender) =>
                {
                    var Item = sender as Picture;

                    foreach (var picture in Items)
                    {
                        if (picture.Id == Item.Id)
                        {
                            Console.WriteLine("tapped {0}", picture.Id);
                            await Navigation.PushAsync(new ImageDetailPage(picture), true);
                        }
                    }

                });
            }
        }

        public ICommand GroupedItemTappedCommand 
        {
            get
            {
                return new Command(async (sender) =>
                {
                    // get selected group
                    Grouping<string, Picture> selectedGroup = sender as Grouping<string, Picture>;

                    // get grouped item of selected album
                    List<Grouping<string, Picture>> grouped = GroupPicturesByDate(selectedGroup.ToList());
                    GroupedItems = new FlowObservableCollection<Grouping<string, Picture>>(grouped);

                    // set currently selected group
                    SelectedGroup = selectedGroup.Key;
                    App.CurrentSortKey = SelectedSort; 
                    App.CurrentGroup = selectedGroup.Key;

                    // navigate to image grid
                    await Navigation.PushAsync(new ImageGrid(selectedGroup.Key), true);

                });
            }
        }

        public ICommand OpenCleanupPage => new Command(async () =>
        {
            await Navigation.PushAsync(new CleanupPage());
        });

        public void OnPictureDeleted(PictureDeletedEvent e)
        {
            int deletedPictureId = e.GetPictureId();

            // find picture object
            int pictureIndex = Items.FindIndex(pic => pic.Id == deletedPictureId);
            Picture picture = Items[pictureIndex];
            Items.RemoveAt(pictureIndex);

            // delte picture from album
            string albumKey = SelectedGroup;
            AlbumItems.ForEach(group =>
            {
                // select correct album
                if (group.Key == albumKey)
                {
                    // get index of picture and delete
                    int groupIndex = group.ToList().FindIndex(pic => pic.Id == deletedPictureId);
                    group.RemoveAt(groupIndex);

                    // re-group pictures from album
                    List<Grouping<string, Picture>> grouped = GroupPicturesByDate(group.ToList());
                    GroupedItems = new FlowObservableCollection<Grouping<string, Picture>>(grouped);
                    return;
                }
            });
        }

        private byte[] GetBytes(double[] values)
        {
            return values.SelectMany(value => BitConverter.GetBytes(value)).ToArray();
        }
    }
}