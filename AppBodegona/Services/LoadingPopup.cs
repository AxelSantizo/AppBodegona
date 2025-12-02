using Rg.Plugins.Popup.Pages;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace AppBodegona.Services
{
    public class LoadingPopup : PopupPage
    {
        public LoadingPopup()
        {
            Content = new Frame
            {
                Padding = 20,
                CornerRadius = 10,
                BackgroundColor = Color.White,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                HasShadow = true,
                Content = new StackLayout
                {
                    VerticalOptions = LayoutOptions.CenterAndExpand,
                    HorizontalOptions = LayoutOptions.CenterAndExpand,
                    Children =
                {
                    new ActivityIndicator
                    {
                        IsRunning = true,
                        Color = Color.Black,
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center
                    },
                    new Label
                    {
                        Text = "Cargando...",
                        FontSize = 18,
                        TextColor = Color.Black,
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center
                    }
                }
                }
            };
            BackgroundColor = Color.FromRgba(0, 0, 0, 0.3);
        }

        protected override bool OnBackgroundClicked()
        {
            return false;
        }

        protected override bool OnBackButtonPressed()
        {
            return true;
        }
    }
}
