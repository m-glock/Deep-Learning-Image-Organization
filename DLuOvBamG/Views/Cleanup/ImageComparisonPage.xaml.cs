﻿using DLuOvBamG.Models;
using DLuOvBamG.Services.Gestures;
using DLuOvBamG.ViewModels;
using System.Collections.Generic;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace DLuOvBamG.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ImageComparisonPage : CustomBackButtonPage
    {
        private ImageComparisonViewModel VM;

        public ImageComparisonPage(List<Picture> pictures, Picture mainPic)
        {
            Picture comparingPicture = mainPic;

            List<CarouselViewItem> picsForCarousel = new List<CarouselViewItem>();
            foreach (Picture pic in pictures)
            {
                if (!pic.Equals(mainPic)) picsForCarousel.Add(new CarouselViewItem(pic.Uri, comparingPicture.Uri));
            }

            VM = new ImageComparisonViewModel(this, picsForCarousel);
            BindingContext = VM;

            InitializeComponent();

            // get carousel view to proper size depending on the screen size
            DisplayInfo mainDisplayInfo = DeviceDisplay.MainDisplayInfo;
            double width = mainDisplayInfo.Width / mainDisplayInfo.Density;
            ImageCarouselView.PeekAreaInsets = width < 350 ? 100 : 135;

            VM.CarouselViewMain = ImageMainView;
            VM.BinImage = BinImage;

            // show safety alert when clickin the navigation back button
            if (EnableBackButtonOverride)
            {
                this.CustomBackButtonAction = () =>
                {
                    VM.ShowAlertSelectionLost();
                };
            }
        }

        public void ImageTouched(object sender, TouchActionEventArgs args)
        {
            Image currentPicture = sender as Image;
            CarouselViewItem currentPictureItem = (CarouselViewItem)ImageMainView.CurrentItem;

            if (!currentPictureItem.IsMarkedForDeletion())
            {
                switch (args.Type)
                {
                    /*case TouchActionType.Moved:
                        Console.WriteLine("moved");
                        break;*/
                    case TouchActionType.Pressed:
                        //Console.WriteLine("tap started"); 
                        VM.OnPressedAsync(currentPicture);
                        break;
                    case TouchActionType.Released:
                    case TouchActionType.Cancelled:
                    case TouchActionType.Exited:
                        //Console.WriteLine("tap stopped");
                        VM.OnReleasedAsync(currentPicture);
                        break;
                    default:
                        break;
                }
            }
        }


        /**
         * change trash icon depending on whether the current image is marked to be deleted or not
         * delete_64px.png: 
         * delete_restore_64px.png: customisation of delete_64px.png
         */
        private void CurrentItemChanged(object sender, CurrentItemChangedEventArgs e)
        {
            CarouselViewItem currentPicture = (CarouselViewItem)e.CurrentItem;
            BinImage.Source = currentPicture.IsMarkedForDeletion() ? "delete_restore_64px.png" : "delete_64px.png";
        }
    }
}