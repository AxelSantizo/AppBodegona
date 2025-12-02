using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;
using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace AppBodegona.Services
{
    public class DynamicPopup : PopupPage
    {
        public DynamicPopup(string titulo, string mensaje, Dictionary<string, Action> acciones, View contenidoAdicional = null)
        {
            this.BackgroundInputTransparent = false;

            var layout = new StackLayout
            {
                VerticalOptions = LayoutOptions.CenterAndExpand,
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                Children =
            {
                new Label
                {
                    Text = titulo,
                    FontSize = 20,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromHex("#333333"),
                    HorizontalTextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 10)
                }
            }
            };

            if (!string.IsNullOrEmpty(mensaje))
            {
                View contenidoMensaje;

                if (mensaje.Length > 500) // Puedes ajustar el umbral según el tamaño esperado
                {
                    contenidoMensaje = new ScrollView
                    {
                        HeightRequest = 400,
                        Content = new Label
                        {
                            Text = mensaje,
                            FontSize = 16,
                            TextColor = Color.FromHex("#555555"),
                            HorizontalTextAlignment = TextAlignment.Center,
                            Margin = new Thickness(0, 0, 0, 20)
                        }
                    };
                }
                else
                {
                    contenidoMensaje = new Label
                    {
                        Text = mensaje,
                        FontSize = 16,
                        TextColor = Color.FromHex("#555555"),
                        HorizontalTextAlignment = TextAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 20)
                    };
                }

                layout.Children.Add(contenidoMensaje);
            }

            if (contenidoAdicional != null)
            {
                layout.Children.Add(contenidoAdicional);
            }

            layout.Children.Add(GenerateButtonGrid(acciones));

            Content = new Frame
            {
                Padding = 20,
                CornerRadius = 10,
                BackgroundColor = Color.White,
                Margin = new Thickness(20, 0),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                HasShadow = true,
                Content = layout
            };

            BackgroundColor = Color.FromRgba(0, 0, 0, 0.6);
        }

        protected override bool OnBackgroundClicked()
        {
            return false;
        }

        protected override bool OnBackButtonPressed()
        {
            return true;
        }

        private Grid GenerateButtonGrid(Dictionary<string, Action> acciones)
        {
            var grid = new Grid
            {
                ColumnSpacing = 10,
                VerticalOptions = LayoutOptions.End,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                ColumnDefinitions = new ColumnDefinitionCollection()
            };

            int columnIndex = 0;
            foreach (var accion in acciones)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var button = new Button
                {
                    Text = accion.Key,
                    BackgroundColor = columnIndex == acciones.Count - 1 ? Color.Red : Color.Green,
                    TextColor = Color.White,
                    Command = new Command(async () =>
                    {
                        await PopupNavigation.Instance.PopAsync();
                        accion.Value?.Invoke();
                    })
                };
                Grid.SetColumn(button, columnIndex);
                grid.Children.Add(button);
                columnIndex++;
            }

            return grid;
        }
    }
}