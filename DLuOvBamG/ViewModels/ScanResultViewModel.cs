﻿using DLuOvBamG.Models;
using DLuOvBamG.Views;
using System;
using System.Collections.Generic;
using System.Windows.Input;
using Xamarin.Forms;

namespace DLuOvBamG.ViewModels
{
    public class ScanResultViewModel : BaseViewModel
    {
        public INavigation Navigation;
        public Dictionary<ScanOptionsEnum, double> Options;

        public ScanResultViewModel()
        {

        }

        public ICommand openBlurryPicsPage => new Command(async () =>
        {
            Console.WriteLine("blurry chosen");
            //await Navigation.PushAsync(new ScanOptionDisplayPage());
        });

        public ICommand openDarkPicsPage => new Command(async () =>
        {
            Console.WriteLine("dark chosen");
            //await Navigation.PushAsync(new ScanOptionDisplayPage());
        });

        public ICommand openSimilarPicsPage => new Command(async () =>
        {
            Console.WriteLine("similar chosen");
            //await Navigation.PushAsync(new ScanOptionDisplayPage());
        });

        /*public ICommand ShowImages => new Command(async () => {
                //TODO: get correct Group (Enum), slidervalue and pictureList
                ScanOptionsEnum option = ScanOptionsEnum.blurryPics;
                double sliderValue = 0.5;
                List<List<Picture>> pictures = new List<List<Picture>>();
                pictures.Add(new List<Picture>());
                await Navigation.PushAsync(new ScanOptionDisplayPage(option, sliderValue, pictures));
        });*/
    }
}
