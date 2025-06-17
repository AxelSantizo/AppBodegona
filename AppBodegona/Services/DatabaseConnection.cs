using System;
using Xamarin.Essentials;
using MySqlConnector;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;
using Xamarin.Forms;
using System.Collections.Generic;

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
                    TextColor = Color.Black,
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
                        TextColor = Color.Gray,
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
                    TextColor = Color.Gray,
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

namespace AppBodegona.Services
{
    public static class DatabaseConnection
    {
        public static string ConnectionString { get; private set; }
        public static string ConnectedServer { get; private set; }
        public static string ConnectionCentral { get; private set; }
        public static string ConnectionNexus { get; private set; }
        public static string DeviceName { get; private set; }

        static DatabaseConnection()
        {
            InitializeConnectionString();
            InitializeConnectionCentral();
            InitializeConnectionNexus();
            DeviceName = ObtenerNombreDispositivo();
        }

        public static string ObtenerNombreDispositivo()
        {
            try
            {
                string nombreDispositivo = DeviceInfo.Name;

                return nombreDispositivo;
            }
            catch (Exception ex)
            {
                return $"Error al obtener el nombre del dispositivo: {ex.Message}";
            }
        }


        public static void InitializeConnectionString()
        {
            //var server = Preferences.Get("ServerAddress", string.Empty);
            //var port = Preferences.Get("PortNumber", string.Empty);
            //var database = Preferences.Get("DatabaseName", string.Empty);
            //var username = Preferences.Get("Username", string.Empty);
            //var password = Preferences.Get("Password", string.Empty);

            var server = "192.168.110.161";
            var port = "3306";
            var database = "superpos";
            var username = "root";
            var password = "bode.24451988";

            if (!string.IsNullOrEmpty(server) && !string.IsNullOrEmpty(port) && !string.IsNullOrEmpty(database) && !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                var connectionString = $"Server={server};Port={port};Database={database};Uid={username};Pwd={password};";
                if (TestConnection(connectionString))
                {
                    ConnectionString = connectionString;
                    ConnectedServer = server;
                    return;
                }
            }
            ConnectionString = string.Empty;
        }

        public static void InitializeConnectionCentral()
        {
            var server = "192.168.110.161";
            var port = "3306"; 
            var database = "facturas_compras";
            var username = "root";
            var password = "bode.24451988";

            var connectionString = $"Server={server};Port={port};Database={database};Uid={username};Pwd={password};";
            if (TestConnection(connectionString))
            {
                ConnectionCentral = connectionString;
            }
            else
            {
                ConnectionCentral = string.Empty;
            }
        }

        public static void InitializeConnectionNexus()
        {
            var server = "192.168.110.161";
            var port = "3306";
            var database = "dbsucursales";
            var username = "root";
            var password = "bode.24451988";

            var connectionString = $"Server={server};Port={port};Database={database};Uid={username};Pwd={password};";
            if (TestConnection(connectionString))
            {
                ConnectionNexus = connectionString;
            }
            else
            {
                ConnectionNexus = string.Empty;
            }
        }

        public static MySqlConnection GetConnection()
        {
            if (string.IsNullOrEmpty(ConnectionString))
            {
                InitializeConnectionString();
            }
            return new MySqlConnection(ConnectionString);
        }

        public static MySqlConnection GetConnectionCentral()
        {
            if (string.IsNullOrEmpty(ConnectionCentral))
            {
                InitializeConnectionCentral();
            }
            return new MySqlConnection(ConnectionCentral);
        }

        public static MySqlConnection GetConnectionNexus()
        {
            if (string.IsNullOrEmpty(ConnectionNexus))
            {
                InitializeConnectionNexus();
            }
            return new MySqlConnection(ConnectionNexus);
        }

        public static bool TestConnection(string connectionString)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static void UpdateConnectionString(string server, string port, string database, string username, string password)
        {
            ConnectionString = $"Server={server};Port={port};Database={database};Uid={username};Pwd={password};";
            ConnectedServer = server;
        }

        public class ConnectionConfig
        {
            public string Server { get; set; }
            public string Port { get; set; }
            public string Database { get; set; }
            public string Uid { get; set; }
            public string Password { get; set; }
        }
    }
}
