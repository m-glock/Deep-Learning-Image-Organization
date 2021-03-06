using System;
﻿using DLuOvBamG.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Xamarin.Forms;
using DLuOvBamG.Views;
using GalaSoft.MvvmLight.Messaging;
using System.Linq;

namespace DLuOvBamG.ViewModels
{
    public class ScanOptionDisplayViewModel : BaseViewModel, INotifyPropertyChanged
	{
        public ObservableCollection<ObservableCollection<Picture>> pictures;
		public double precision;
		public INavigation Navigation;
		public Image SelectedImage;
		public event PropertyChangedEventHandler PropertyChanged;
        private ScanOptionsEnum Option;

        #region propertychanged
        public double Precision
        {
            set
            {
                if (precision != value)
                {
                    precision = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Precision"));
                }
            }
            get
            {
                return precision;
            }
        }
        public ObservableCollection<ObservableCollection<Picture>> Pictures
        {
            set
            {
                pictures = value;

                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("Pictures"));
                }
            }
            get
            {
                return pictures;
            }
        }
        #endregion

        public ScanOptionDisplayViewModel(ScanOptionsEnum option, ObservableCollection<ObservableCollection<Picture>> pictures, INavigation navigation)
        {
			Title = "Cleanup Results";
            Option = option;
            Pictures = pictures;
            Navigation = navigation;

            // register for event that picture has been deleted
            Messenger.Default.Register<PictureDeletedEvent>(this, OnPictureDeleted);
        }

		public ObservableCollection<Picture> GetPictureListForGroup(int groupID)
		{
			if (groupID > Pictures.Count) return null;
			return Pictures[groupID];
		}

        public async void OpenComparisonPage(Picture comparingPicture, string groupID)
        {
            try
            {
                int id = int.Parse(groupID);
                List<Picture> pictures = new List<Picture>(Pictures[id]);
                await Navigation.PushAsync(new ImageComparisonPage(pictures, comparingPicture));
            }
            catch (FormatException)
            {
                Console.WriteLine($"Unable to parse '{groupID}'");
            }

        }

        public async void OpenImageDetailViewPage(Picture picture)
        {
            await Navigation.PushAsync(new ImageDetailPage(picture));
        }

        public void OnPictureDeleted(PictureDeletedEvent e)
        {
            int deletedPictureId = e.GetPictureId();
            for(int i = 0; i < Pictures.Count; ++i)
            {
                ObservableCollection<Picture> collection = Pictures[i];
                List<Picture> deletedPicture = collection.Where(picture => picture.Id == deletedPictureId).ToList();
                if (deletedPicture.Count > 0) collection.Remove(deletedPicture[0]);
                if (collection.Count < 3) Pictures.Remove(collection);
            }
        }

        public ICommand UpdatePicturesAfterValueChange => new Command(async () =>
        {
            Dictionary<ScanOptionsEnum, double> dictChangedValue = new Dictionary<ScanOptionsEnum, double>();
            dictChangedValue.Add(Option, Precision);
            App.tf.FillPictureLists(dictChangedValue, App.CurrentSortKey, App.CurrentGroup);

            List<List<Picture>> pictures = App.tf.GetAllPicturesForOption(Option);
            ObservableCollection<ObservableCollection<Picture>> obsvPictures = new ObservableCollection<ObservableCollection<Picture>>();
            foreach (List<Picture> picturesList in pictures)
            {
                obsvPictures.Add(new ObservableCollection<Picture>(picturesList));
            }
            Pictures = obsvPictures;
        });
    }
}