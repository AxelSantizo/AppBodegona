using AppBodegona.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using MySqlConnector;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System.Collections.ObjectModel;
using static AppBodegona.Views.IPConfig;
using Xamarin.Essentials;
using Rg.Plugins.Popup.Services;

namespace AppBodegona.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Existencia : ContentPage
    {

        public ObservableCollection<Producto> Productos { get; set; }

        public static readonly BindableProperty IdProperty =
        BindableProperty.Create(nameof(Id), typeof(string), typeof(Existencia), default(string));
        public new string Id
        {
            get => (string)GetValue(IdProperty);
            set => SetValue(IdProperty, value);
        }

        public Existencia()
        {
            InitializeComponent();
            InicializarConexion();
            InicializarComponentes();
            UpdateImage();
        }

        private void InicializarConexion()
        {
            try
            {
                bool isConnected = DatabaseConnection.TestConnection(DatabaseConnection.ConnectionString);
                if (!isConnected)
                {
                    var popup = new DynamicPopup("Error de Conexión", "No se pudo conectar a la base de datos.",
                        new Dictionary<string, Action>
                        {
                            { "OK", () => {} }
                        });
                    PopupNavigation.Instance.PushAsync(popup);
                }
            }
            catch (Exception ex)
            {
                var popup = new DynamicPopup("Error de Conexión", "Error al intentar conectar a la base de datos: " + ex.Message,
                    new Dictionary<string, Action>
                    {
                        { "OK", () => {} }
                    });
                PopupNavigation.Instance.PushAsync(popup);
            }

        }

        private void InicializarComponentes()
        {
            Productos = new ObservableCollection<Producto>();
            ResultadosListView.ItemsSource = Productos;

            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();
        }

        public void UpdateImage()
        {
            int idSucursal = Preferences.Get("ID_Sucursal", 0);

            if (idSucursal == 4)
            {
                Img.Source = "supermercadon.png";
                Img1.Source = "BannerSupermercadon.png";
            }
            else
            {
                Img.Source = "bodegona.png";
                Img1.Source = "BannerBodegona.png";
            }
        }

        protected override bool OnBackButtonPressed()
        {
            if (Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopupStack.Count > 0)
            {
                return base.OnBackButtonPressed();
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                var popup = new DynamicPopup(
                    "Confirmar",
                    "¿Desea salir?",
                    new Dictionary<string, Action>
                    {
                        { "No", () => {} },
                        { "Sí", () => System.Diagnostics.Process.GetCurrentProcess().CloseMainWindow() }
                    });

                PopupNavigation.Instance.PushAsync(popup);
            });
            return true;
        }

        public class Producto
        {
            public string Upc { get; set; }
            public string DescLarga { get; set; }
            public string Existencia { get; set; }
            public string Precio { get; set; }
            public string IdFamilia { get; set; }
        }

        private void Cambio_Clicked(object sender, EventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();

            eupc.Text = "";
            edescripcion.Text = "";

            if (eupc.IsVisible)
            {
                eupc.IsVisible = false;
                edescripcion.IsVisible = true;
            }
            else
            {
                eupc.IsVisible = true;
                edescripcion.IsVisible = false;
            }

            Contenedor.IsVisible = false;
            ResultadosListView.IsVisible = false;
        }

        private async void BProducto(object sender, EventArgs e)
        {
            var loadingPopup = new LoadingPopup();

            try
            {
                await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(loadingPopup);

                AppShell appShell = (AppShell)Application.Current.MainPage;
                appShell.ResetInactivityTimer();
                Productos.Clear();

                if (!string.IsNullOrEmpty(eupc.Text))
                {
                    string entryupcText = eupc.Text;

                    if (entryupcText.Length < 13)
                    {
                        int cerosToAdd = 13 - entryupcText.Length;
                        entryupcText = new string('0', cerosToAdd) + entryupcText;
                    }

                    string query = $"SELECT Upc, Precio, DescLarga, Nivel1, PrecioMaxNivel1, Existencia, GruposProductosId FROM productos WHERE Upc = '{entryupcText}'";

                    using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                    {
                        connection.Open();
                        using (MySqlCommand command = new MySqlCommand(query, connection))
                        {
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    Productos.Add(new Producto
                                    {
                                        Upc = reader["Upc"].ToString(),
                                        DescLarga = reader["DescLarga"].ToString(),
                                        Existencia = reader["Existencia"].ToString(),
                                        Precio = reader["Precio"].ToString(),
                                        IdFamilia = reader["GruposProductosId"].ToString()
                                    });
                                    Img.IsVisible = false;
                                    Contenedor.IsVisible = false;
                                    ResultadosListView.IsVisible = true;
                                }
                                else
                                {
                                    await PopupNavigation.Instance.PopAsync();
                                    var popup = new DynamicPopup(
                                    "Alerta",
                                    "No se encontraron productos con ese UPC.",
                                    new Dictionary<string, Action>
                                    {
                                        { "Aceptar", () => {} }
                                    });

                                    await PopupNavigation.Instance.PushAsync(popup);
                                }
                            }
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(edescripcion.Text))
                {
                    string entryDescText = edescripcion.Text;
                    string[] searchTerms = entryDescText.Split(' ');
                    string query = "SELECT Upc, Precio, Nivel1, PrecioMaxNivel1, DescLarga, Existencia, GruposProductosId FROM productos WHERE ";

                    for (int i = 0; i < searchTerms.Length; i++)
                    {
                        query += $"DescLarga LIKE @searchTerm{i}";
                        if (i < searchTerms.Length - 1)
                        {
                            query += " AND ";
                        }
                    }
                    query += @"
                             ORDER BY 
                             CASE 
                             WHEN Existencia > 0 THEN 1
                             WHEN Existencia = 0 THEN 2
                             ELSE 3
                             END, 
                             Existencia DESC";

                    using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                    {
                        connection.Open();
                        using (MySqlCommand command = new MySqlCommand(query, connection))
                        {
                            for (int i = 0; i < searchTerms.Length; i++)
                            {
                                command.Parameters.AddWithValue($"@searchTerm{i}", "%" + searchTerms[i] + "%");
                            }
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                Productos.Clear();
                                while (reader.Read())
                                {
                                    Productos.Add(new Producto
                                    {
                                        Upc = reader["Upc"].ToString(),
                                        DescLarga = reader["DescLarga"].ToString(),
                                        Existencia = reader["Existencia"].ToString(),
                                        Precio = reader["Precio"].ToString(),
                                        IdFamilia = reader["GruposProductosId"].ToString()
                                    });
                                }
                                if (Productos.Count == 0)
                                {
                                    await PopupNavigation.Instance.PopAsync();
                                    var popup = new DynamicPopup(
                                        "Alerta",
                                        "No se encontraron productos con esa descripción.",
                                        new Dictionary<string, Action>
                                        {
                                    { "Aceptar", () => {} }
                                        });

                                    await PopupNavigation.Instance.PushAsync(popup);
                                }
                                else
                                {
                                    Img.IsVisible = false;
                                    Contenedor.IsVisible = false;
                                    ResultadosListView.IsVisible = true;
                                }
                            }
                        }
                    }
                }
                else
                {
                    await PopupNavigation.Instance.PopAsync();
                    var popup = new DynamicPopup(
                        "Alerta",
                        "No hay datos para buscar.",
                        new Dictionary<string, Action>
                        {
                    { "Aceptar", () => {} }
                        });

                    await PopupNavigation.Instance.PushAsync(popup);
                }
            }
            catch (Exception ex)
            {
                await PopupNavigation.Instance.PopAsync();
                var popup = new DynamicPopup(
                    "Error de Conexión",
                    "Error al intentar conectar a la base de datos: " + ex.Message,
                    new Dictionary<string, Action>
                    {
                { "OK", () => {} }
                    });

                await PopupNavigation.Instance.PushAsync(popup);
            }
            finally
            {
                if (PopupNavigation.Instance.PopupStack.Contains(loadingPopup))
                {
                    await PopupNavigation.Instance.PopAsync();
                }
            }
        }

        private async void Buscar_Clicked(object sender, EventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();

            if (edescripcion.Text == "..Config..")
            {
                ResultadosListView.IsVisible = false;
                Img.IsVisible = true;
                await Shell.Current.GoToAsync(nameof(IPConfig));
            }
            else
            {
                BProducto(sender, e); 
            }
        }

        private void ListView_ItemSelectedFalse(object sender, SelectedItemChangedEventArgs e)
        {
            ResultadosListView.SelectedItem = null;
        }

        private async void ValidarNiv1(double costoProducto, string nivel1String, string precio1String, string existenciaString, string precioString, string descLarga)
        {
            int idsursal = Preferences.Get("ID_Sucursal", 0);

            if (!int.TryParse(nivel1String, out int nivel1))
            {
                return;
            }
            PNormal.TextColor = Color.FromHex("#FFF");
            MNormal.TextColor = Color.FromHex("#FFF");
            PNivel.TextColor = Color.FromHex("#FFF");
            MNivel1.TextColor = Color.FromHex("#FFF");

            double precio = Convert.ToDouble(precioString);
            double precioNormal = Convert.ToDouble(precio1String);

            double margenn = (precio - costoProducto) / (precio * 0.01);
            double margen1 = (precioNormal - costoProducto) / (precioNormal * 0.01);


            MNormal.Text = $"Margen: %{margenn:F2}";
            MNivel1.Text = $"Margen Nivel1: %{margen1:F2}";
            PNivel1.Text = "Sin Nivel 2";
            MNivel2.Text = null;


            if (margenn < 0)
            {
                PNormal.TextColor = Color.FromHex("#DF3068");
                MNormal.TextColor = Color.FromHex("#DF3068");
            }

            if (margen1 < 0)
            {
                PNivel.TextColor = Color.FromHex("#DF3068");
                MNivel1.TextColor = Color.FromHex("#DF3068");
            }


            if (nivel1 >= 3)
            {
                if (idsursal == 4)
                {
                    double fontSizeOferta = 50;
                    Oferta.FontSize = fontSizeOferta;

                    Oferta.Text = "A Partir De " + nivel1;
                    Precio.Text = Convert.ToDouble(precio1String).ToString("F2");
                    PNivel.Text = $"Precio Nivel1: A partir de {nivel1} a " + $"{double.Parse(precio1String):F2}";
                }
                else
                {
                    double fontSizeOferta = 90;
                    Oferta.FontSize = fontSizeOferta;
                    string nivelString = $"{nivel1}x";
                    double precioOferta = Math.Round(precioNormal * nivel1, 2);

                    Oferta.Text = "OFERTA";
                    Nivel.Text = nivelString;
                    Precio.Text = precioOferta.ToString("F2");
                    PNivel.Text = $"Precio Nivel1:  {nivelString}" + $"{precioOferta:F2}";
                }
                Exis.Text = $"Existencia: {existenciaString}";
                PNormal.Text = $"Precio Unidad: {double.Parse(precioString):F2}";
                Descripcion.Text = descLarga;
            }
            else if (nivel1 == 2)
            {
                if (idsursal == 4)
                {
                    double precioMitad = double.Parse(precioString) / 2;

                    if (double.TryParse(precio1String, out double precioMaxNivel1))
                    {
                        if (Math.Abs(precioMaxNivel1 - precioMitad) <= 0.02)
                        {
                            double fontSizeOferta = 90;
                            Oferta.FontSize = fontSizeOferta;

                            Oferta.Text = "2x1";
                            Nivel.Text = " ";
                            Precio.Text = double.Parse(precioString).ToString("F2");
                            PNivel.Text = $"Precio Nivel1: A partir de {nivel1} a " + $"{double.Parse(precio1String):F2}";

                        }
                        else
                        {
                            double fontSizeOferta = 50;
                            Oferta.FontSize = fontSizeOferta;

                            Oferta.Text = $"A Partir De {nivel1}";
                            Precio.Text = Convert.ToDouble(precio1String).ToString("F2");

                            PNivel.Text = $"Precio Nivel1: A partir de {nivel1} a " + $"{double.Parse(precio1String):F2}";
                        }
                        Descripcion.Text = descLarga;
                        Exis.Text = $"Existencia: {existenciaString}";
                        PNormal.Text = $"Precio Unidad: {double.Parse(precioString):F2}";
                    }
                    else
                    {
                        var popup = new DynamicPopup(
                        "Error",
                        "Error al consultar",
                        new Dictionary<string, Action>
                        {
                            { "OK", () => {} }
                         });

                        await PopupNavigation.Instance.PushAsync(popup);
                    }
                }
                else
                {
                    double fontSizeOferta = 90;
                    Oferta.FontSize = fontSizeOferta;
                    double precioMitad = double.Parse(precioString) / 2;

                    if (double.TryParse(precio1String, out double precioMaxNivel1))
                    {
                        if (Math.Abs(precioMaxNivel1 - precioMitad) <= 0.02)
                        {
                            Oferta.Text = "2x1";
                            Nivel.Text = " ";
                            Descripcion.Text = descLarga;
                            Precio.Text = double.Parse(precioString).ToString("F2");
                            Exis.Text = $"Existencia: {existenciaString}";
                            PNormal.Text = $"Precio Unidad: {double.Parse(precioString):F2}";
                            PNivel.Text = "Precio Nivel1: " + $"{double.Parse(precio1String):F2}";

                        }
                        else
                        {
                            string nivelString = $"{nivel1}x";
                            double precioOferta = Math.Round(precioNormal * nivel1, 2);

                            Oferta.Text = "OFERTA";
                            Nivel.Text = nivelString;
                            Descripcion.Text = descLarga;
                            Precio.Text = precioOferta.ToString("F2");
                            Exis.Text = $"Existencia: {existenciaString}";
                            PNormal.Text = $"PrecioUnidad: {double.Parse(precioString):F2}";
                            PNivel.Text = "Precio Nivel1: " + nivel1String + precioOferta;
                        }
                    }
                    else
                    {
                        var popup = new DynamicPopup(
                        "Error",
                        "Error al consultar",
                        new Dictionary<string, Action>
                        {
                            { "OK", () => {} }
                         });

                        await PopupNavigation.Instance.PushAsync(popup);
                    }
                }
            }
            else if (nivel1 == 1)
            {
                if (idsursal == 4)
                {
                    double fontSizeOferta = 50;
                    Oferta.FontSize = fontSizeOferta;

                    Oferta.Text = "A Partir De " + nivel1;
                    Precio.Text = Convert.ToDouble(precio1String).ToString("F2");
                    PNivel.Text = $"Precio Nivel1: A partir de {nivel1} a " + $"{double.Parse(precio1String):F2}";
                }
                else
                {
                    double fontSizeOferta = 90;
                    Oferta.FontSize = fontSizeOferta;
                    string nivelString = "  ";

                    Oferta.Text = "OFERTA";
                    Nivel.Text = nivelString;
                    Precio.Text = double.Parse(precio1String).ToString("F2");
                    PNivel.Text = "Precio Nivel1:  " + $"{double.Parse(precio1String):F2}";
                }
                Descripcion.Text = descLarga;
                Exis.Text = $"Existencia: {existenciaString}";
                PNormal.Text = $"Precio Normal: {double.Parse(precioString):F2}";
            }
        }

        private async void ValidarNiv2(double costoProducto, string nivel1String, string precio1String, string nivel2String, string precio2String, string existenciaString, string precioString, string descLarga)
        {
            int idsursal = Preferences.Get("ID_Sucursal", 0);

            if (!int.TryParse(nivel1String, out int nivel1) || !int.TryParse(nivel2String, out int nivel2))
            {
                return;
            }

            PNormal.TextColor = Color.FromHex("#FFF");
            MNormal.TextColor = Color.FromHex("#FFF");
            PNivel.TextColor = Color.FromHex("#FFF");
            MNivel1.TextColor = Color.FromHex("#FFF");
            PNivel1.TextColor = Color.FromHex("#FFF");
            MNivel2.TextColor = Color.FromHex("#FFF");

            double precioNormal = Convert.ToDouble(precioString);
            double precioNivel1 = Convert.ToDouble(precio1String);
            double precioNivel2 = Convert.ToDouble(precio2String);

            double margenn = (precioNormal - costoProducto) / (precioNormal * 0.01);
            double margen1 = (precioNivel1 - costoProducto) / (precioNivel1 * 0.01);
            double margen2 = (precioNivel2 - costoProducto) / (precioNivel2 * 0.01);

            MNormal.Text = $"Margen: %{margenn:F2}";
            MNivel1.Text = $"Margen Nivel1: %{margen1:F2}";
            MNivel2.Text = $"Margen Nivel2: %{margen2:F2}";

            if (margenn < 0)
            {
                PNormal.TextColor = Color.FromHex("#DF3068");
                MNormal.TextColor = Color.FromHex("#DF3068");
            }

            if (margen1 < 0)
            {
                PNivel.TextColor = Color.FromHex("#DF3068");
                MNivel1.TextColor = Color.FromHex("#DF3068");
            }

            if (margen2 < 0)
            {
                PNivel1.TextColor = Color.FromHex("#DF3068");
                MNivel2.TextColor = Color.FromHex("#DF3068");
            }

            if (nivel2 >= 3)
            {
                if (idsursal == 4)
                {
                    double fontSizeOferta = 50;
                    Oferta.FontSize = fontSizeOferta;

                    Oferta.Text = "A Partir De " + nivel2;
                    Precio.Text = Convert.ToDouble(precio2String).ToString("F2");
                    PNivel1.Text = $"Precio Nivel2: A partir de {nivel2} a " + $"{double.Parse(precio2String):F2}";
                }
                else
                {
                    double fontSizeOferta = 90;
                    Oferta.FontSize = fontSizeOferta;
                    string nivelString = $"{nivel2}x";
                    double precioOferta = Math.Round(precioNivel2 * nivel2, 2);

                    Oferta.Text = "OFERTA";
                    Nivel.Text = nivelString;
                    Precio.Text = precioOferta.ToString("F2");
                    PNivel1.Text = $"Precio Nivel2: {nivelString} {precioOferta:F2}";
                }
                Descripcion.Text = descLarga;
                Exis.Text = $"Existencia: {existenciaString}";
                PNormal.Text = $"Precio Unidad: {double.Parse(precioString):F2}";

                if (nivel1 > 1)
                {
                    if (idsursal == 4)
                    {
                        PNivel.Text = $"A partir de {nivel1} a" + precio1String;
                    }
                    else
                    {
                        string niv1 = $"{nivel1}x";
                        double precioNiv1 = Convert.ToDouble(precio1String);
                        double precioOfertaniv = Math.Round(precioNiv1 * nivel1, 2);

                        PNivel.Text = $"Precio Nivel1: {niv1} {precioOfertaniv:F2}";
                    }
                }
                else if (nivel1 == 1)
                {
                    if (idsursal == 4)
                    {
                        PNivel.Text = $"A partir de {nivel1} a" + precio1String;
                    }
                    else
                    {
                        PNivel.Text = $"Precio Nivel1: {double.Parse(precio1String):F2}";
                    }
                }
                else if (nivel1 == 0)
                {
                    PNivel.Text = "Sin Nivel 1";
                    MNivel1.Text = null;
                    PNivel.TextColor = Color.FromHex("#FFF");
                    MNivel1.TextColor = Color.FromHex("#FFF");
                }
            }
            else if (nivel2 == 2)
            {
                if (idsursal == 4)
                {
                    double fontSizeOferta = 50;
                    Oferta.FontSize = fontSizeOferta;

                    double precioMitad = double.Parse(precioString) / 2;

                    if (double.TryParse(precio2String, out double precioMaxNivel2))
                    {
                        if (Math.Abs(precioMaxNivel2 - precioMitad) <= 0.02)
                        {
                            Oferta.Text = "2x1";
                            Nivel.Text = " ";
                            Precio.Text = double.Parse(precioString).ToString("F2");
                            PNivel1.Text = $"Precio Nivel2: {double.Parse(precio2String):F2}";
                        }
                        else
                        {
                            Oferta.Text = $"A Partir De {nivel2}";
                            Precio.Text = Convert.ToDouble(precio2String).ToString("F2");
                            PNivel1.Text = $"A Partir De {nivel2} a" + Convert.ToDouble(precio2String).ToString("F2");
                        }
                        PNormal.Text = $"Precio Unidad: {double.Parse(precioString):F2}";
                        Exis.Text = $"Existencia: {existenciaString}";
                        Descripcion.Text = descLarga;

                        if (nivel1 > 1)
                        {
                            string niv1 = $"{nivel1}x";
                            double precioNiv1 = Convert.ToDouble(precio1String);
                            double precioOfertaniv = Math.Round(precioNiv1 * nivel1, 2);

                            PNivel.Text = $"Precio Nivel1: {niv1} {precioOfertaniv:F2}";
                        }
                        else if (nivel1 == 1)
                        {
                            PNivel.Text = $"Precio Nivel1: {double.Parse(precio1String):F2}";
                        }
                        else if (nivel1 == 0)
                        {
                            PNivel.Text = "Sin Nivel 1";
                            MNivel1.Text = null;
                            PNivel.TextColor = Color.FromHex("#FFF");
                            MNivel1.TextColor = Color.FromHex("#FFF");
                        }
                    }
                    else
                    {
                        var popup = new DynamicPopup(
                        "Error",
                        "Error al consultar",
                        new Dictionary<string, Action>
                        {
                            { "OK", () => {} }
                         });

                        await PopupNavigation.Instance.PushAsync(popup);
                    }
                }
                else
                {
                    double fontSizeOferta = 90;
                    Oferta.FontSize = fontSizeOferta;

                    double precioMitad = double.Parse(precioString) / 2;

                    if (double.TryParse(precio2String, out double precioMaxNivel2))
                    {
                        if (Math.Abs(precioMaxNivel2 - precioMitad) <= 0.02)
                        {
                            Oferta.Text = "2x1";
                            Nivel.Text = " ";
                            Descripcion.Text = descLarga;
                            Precio.Text = double.Parse(precioString).ToString("F2");
                            Exis.Text = $"Existencia: {existenciaString}";
                            PNormal.Text = $"Precio Unidad: {double.Parse(precioString):F2}";
                            PNivel1.Text = $"Precio Nivel2: {double.Parse(precio2String):F2}";
                        }
                        else
                        {
                            string nivelString = $"{nivel2}x";
                            double precioOferta = Math.Round(precioNivel2 * nivel2, 2);

                            Oferta.Text = "OFERTA";
                            Nivel.Text = nivelString;
                            Descripcion.Text = descLarga;
                            Precio.Text = precioOferta.ToString("F2");
                            Exis.Text = $"Existencia: {existenciaString}";
                            PNormal.Text = $"Precio Unidad: {double.Parse(precioString):F2}";
                            PNivel1.Text = $"Precio Nivel2: {nivel2}x" + precioOferta;
                        }

                        if (nivel1 > 1)
                        {
                            if (idsursal == 4)
                            {
                                PNivel.Text = $"A partir de {nivel1} a" + precio1String;
                            }
                            else
                            {
                                string niv1 = $"{nivel1}x";
                                double precioNiv1 = Convert.ToDouble(precio1String);
                                double precioOfertaniv = Math.Round(precioNiv1 * nivel1, 2);

                                PNivel.Text = $"Precio Nivel1: {niv1} {precioOfertaniv:F2}";
                            }
                        }
                        else if (nivel1 == 1)
                        {
                            if (idsursal == 4)
                            {
                                PNivel.Text = $"A partir de {nivel1} a" + precio1String;
                            }
                            else
                            {
                                PNivel.Text = $"Precio Nivel1: {double.Parse(precio1String):F2}";
                            }
                        }
                        else if (nivel1 == 0)
                        {
                            PNivel.Text = "Sin Nivel 1";
                            MNivel1.Text = null;
                            PNivel.TextColor = Color.FromHex("#FFF");
                            MNivel1.TextColor = Color.FromHex("#FFF");
                        }
                    }
                    else
                    {
                        var popup = new DynamicPopup(
                        "Error",
                        "Error al consultar",
                        new Dictionary<string, Action>
                        {
                            { "OK", () => {} }
                         });

                        await PopupNavigation.Instance.PushAsync(popup);
                    }
                }
            }
            else if (nivel2 == 1)
            {
                if (idsursal == 4)
                {
                    double fontSizeOferta = 50;
                    Oferta.FontSize = fontSizeOferta;

                    Oferta.Text = "A Partir De " + nivel2;
                    Precio.Text = Convert.ToDouble(precio2String).ToString("F2");
                    PNivel1.Text = $"Precio Nivel2: A partir de {nivel2} a " + $"{double.Parse(precio2String):F2}";
                }
                else
                {
                    double fontSizeOferta = 90;
                    Oferta.FontSize = fontSizeOferta;
                    string nivelString = "  ";

                    Oferta.Text = "OFERTA";
                    Nivel.Text = nivelString;
                    Precio.Text = double.Parse(precio2String).ToString("F2");
                    PNivel1.Text = $"Precio Nivel2: {double.Parse(precio2String):F2}";
                }

                Descripcion.Text = descLarga;
                Exis.Text = $"Existencia: {existenciaString}";
                PNormal.Text = $"Precio Unidad: {double.Parse(precioString):F2}";

                if (nivel1 > 1)
                {
                    if (idsursal == 4)
                    {
                        PNivel.Text = $"A partir de {nivel1} a" + precio1String;
                    }
                    else
                    {
                        string niv1 = $"{nivel1}x";
                        double precioNiv1 = Convert.ToDouble(precio1String);
                        double precioOfertaniv = Math.Round(precioNiv1 * nivel1, 2);

                        PNivel.Text = $"Precio Nivel1: {niv1} {precioOfertaniv:F2}";
                    }
                }
                else if (nivel1 == 1)
                {
                    if (idsursal == 4)
                    {
                        PNivel.Text = $"A partir de {nivel1} a" + precio1String;
                    }
                    else
                    {
                        PNivel.Text = $"Precio Nivel1: {double.Parse(precio1String):F2}";
                    }
                }
                else if (nivel1 == 0)
                {
                    PNivel.Text = "Sin Nivel 1";
                    MNivel1.Text = null;
                    PNivel.TextColor = Color.FromHex("#FFF");
                    MNivel1.TextColor = Color.FromHex("#FFF");
                }
            }
        }

        private async void ValidarNiv1F(double costoFamilia, string nivel1FString, string precio1FString, string nivel2FString, string precio2FString, string existenciaString, string precioFString, string descLarga)
        {
            int idsursal = Preferences.Get("ID_Sucursal", 0);

            if (!int.TryParse(nivel1FString, out int nivel1) || !int.TryParse(nivel2FString, out int nivel2))
            {
                return;
            }

            PNormal.TextColor = Color.FromHex("#FFF");
            MNormal.TextColor = Color.FromHex("#FFF");
            PNivel.TextColor = Color.FromHex("#FFF");
            MNivel1.TextColor = Color.FromHex("#FFF");

            double precioNormal = Convert.ToDouble(precioFString);
            double precioNivel1 = Convert.ToDouble(precio1FString);

            double margenn = (precioNormal - costoFamilia) / (precioNormal * 0.01);
            double margen1 = (precioNivel1 - costoFamilia) / (precioNivel1 * 0.01);

            PNivel.Text = $"Precio Nivel1: {precioNivel1}";
            PNivel1.Text = "Sin Nivel 2";

            MNormal.Text = $"Margen: %{margenn:F2}";
            MNivel1.Text = $"Margen Nivel1: %{margen1:F2}";
            MNivel2.Text = null;

            if (margenn < 0)
            {
                PNormal.TextColor = Color.FromHex("#DF3068");
                MNormal.TextColor = Color.FromHex("#DF3068");
            }

            if (margen1 < 0)
            {
                PNivel.TextColor = Color.FromHex("#DF3068");
                MNivel1.TextColor = Color.FromHex("#DF3068");
            }

            if (nivel1 >= 3)
            {
                if (idsursal == 4)
                {
                    double fontSizeOferta = 50;
                    Oferta.FontSize = fontSizeOferta;

                    Oferta.Text = "A Partir De " + nivel1;
                    Precio.Text = Convert.ToDouble(precio1FString).ToString("F2");
                    PNivel.Text = $"Precio Nivel1: A partir de {nivel1} a " + $"{double.Parse(precio1FString):F2}";
                }
                else
                {
                    double fontSizeOferta = 90;
                    Oferta.FontSize = fontSizeOferta;
                    string nivelString = $"{nivel1}x";
                    double precioOferta = Math.Round(precioNivel1 * nivel1, 2);

                    Oferta.Text = "OFERTA";
                    Nivel.Text = nivelString;
                    Precio.Text = precioOferta.ToString("F2");
                    PNivel.Text = $"Precio Nivel1: {nivelString} {precioOferta:F2}";
                }
                Descripcion.Text = descLarga;
                Exis.Text = $"Existencia: {existenciaString}";
                PNormal.Text = $"Precio Unidad: {double.Parse(precioFString):F2}";
            }
            else if (nivel1 == 2)
            {
                if (idsursal == 4)
                {
                    double precioMitad = double.Parse(precioFString) / 2;

                    if (double.TryParse(precio1FString, out double precioMaxNivel1))
                    {
                        if (Math.Abs(precioMaxNivel1 - precioMitad) <= 0.02)
                        {
                            double fontSizeOferta = 90;
                            Oferta.FontSize = fontSizeOferta;

                            Oferta.Text = "2x1";
                            Nivel.Text = " ";
                            Precio.Text = double.Parse(precioFString).ToString("F2");
                            PNivel.Text = $"Precio Nivel1: A partir de {nivel1} a " + $"{double.Parse(precio1FString):F2}";
                        }
                        else
                        {
                            double fontSizeOferta = 50;
                            Oferta.FontSize = fontSizeOferta;

                            Oferta.Text = $"A Partir De {nivel1}";
                            Precio.Text = Convert.ToDouble(precio1FString).ToString("F2");
                            PNivel.Text = $"Precio Nivel1: A partir de {nivel1} a " + $"{double.Parse(precio1FString):F2}";
                        }
                        Descripcion.Text = descLarga;
                        Exis.Text = $"Existencia: {existenciaString}";
                        PNormal.Text = $"Precio Unidad: {double.Parse(precioFString):F2}";
                    }
                    else
                    {
                        var popup = new DynamicPopup(
                        "Error",
                        "Error al consultar",
                        new Dictionary<string, Action>
                        {
                            { "OK", () => {} }
                         });

                        await PopupNavigation.Instance.PushAsync(popup);
                    }
                }
                else
                {
                    double fontSizeOferta = 90;
                    Oferta.FontSize = fontSizeOferta;
                    double precioMitad = double.Parse(precioFString) / 2;

                    if (double.TryParse(precio1FString, out double precioMaxNivel1))
                    {
                        if (Math.Abs(precioMaxNivel1 - precioMitad) <= 0.02)
                        {
                            Oferta.Text = "2x1";
                            Nivel.Text = " ";
                            Descripcion.Text = descLarga;
                            Precio.Text = double.Parse(precioFString).ToString("F2");
                            Exis.Text = $"Existencia: {existenciaString}";
                            PNormal.Text = $"Precio Unidad: {double.Parse(precioFString):F2}";
                            PNivel.Text = "Precio Nivel1: " + $"{double.Parse(precio1FString):F2}";
                        }
                        else
                        {
                            string nivelString = $"{nivel1}x";
                            double precioOferta = Math.Round(precioNivel1 * nivel1, 2);

                            Oferta.Text = "OFERTA";
                            Nivel.Text = nivelString;
                            Descripcion.Text = descLarga;
                            Precio.Text = precioOferta.ToString("F2");
                            Exis.Text = $"Existencia: {existenciaString}";
                            PNormal.Text = $"Precio Unidad: {double.Parse(precioFString):F2}";
                            PNivel.Text = $"Precio Nivel1: {nivel1}x {precioOferta:F2}";
                        }
                    }
                    else
                    {
                        var popup = new DynamicPopup(
                        "Error",
                        "Error al consultar",
                        new Dictionary<string, Action>
                        {
                            { "OK", () => {} }
                         });

                        await PopupNavigation.Instance.PushAsync(popup);
                    }
                }
            }
            else if (nivel1 == 1)
            {
                if (idsursal == 4)
                {
                    double fontSizeOferta = 50;
                    Oferta.FontSize = fontSizeOferta;

                    Oferta.Text = "A Partir De " + nivel1;
                    Precio.Text = Convert.ToDouble(precio1FString).ToString("F2");
                    PNivel.Text = $"Precio Nivel1: A partir de {nivel1} a " + $"{double.Parse(precio1FString):F2}";
                }
                else
                {
                    double fontSizeOferta = 90;
                    Oferta.FontSize = fontSizeOferta;
                    string nivelString = "  ";

                    Oferta.Text = "OFERTA";
                    Nivel.Text = nivelString;
                    Precio.Text = double.Parse(precio1FString).ToString("F2");
                    PNivel.Text = "Precio Nivel1: " + $"{double.Parse(precio1FString):F2}";
                }
                Descripcion.Text = descLarga;
                Exis.Text = $"Existencia: {existenciaString}";
                PNormal.Text = $"Precio Normal: {double.Parse(precioFString):F2}";
            }
            else if (nivel1 == 0)
            {
                if (nivel2 > 1)
                {
                    string niv2 = $"{nivel2}x";
                    double precioNiv2 = Convert.ToDouble(precio2FString);
                    double precioOfertaniv = Math.Round(precioNiv2 * nivel2, 2);

                    PNivel.Text = $"PrecioNivel2: {niv2} {precioOfertaniv:F2}";
                }
                else if (nivel2 == 1)
                {
                    PNivel.Text = $"PrecioNivel2: {double.Parse(precio2FString):F2}";
                }
                MNivel1.Text = null;
                PNivel.TextColor = Color.FromHex("#FFF");
                MNivel1.TextColor = Color.FromHex("#FFF");
            }
        }

        private async void ValidarNiv2F(double costoFamilia, string nivel1FString, string precio1FString, string nivel2FString, string precio2FString, string existenciaString, string precioFString, string descLarga)
        {
            int idsursal = Preferences.Get("ID_Sucursal", 0);

            if (!int.TryParse(nivel1FString, out int nivel1) || !int.TryParse(nivel2FString, out int nivel2))
            {
                return;
            }

            PNormal.TextColor = Color.FromHex("#FFF");
            MNormal.TextColor = Color.FromHex("#FFF");
            PNivel.TextColor = Color.FromHex("#FFF");
            MNivel1.TextColor = Color.FromHex("#FFF");
            PNivel1.TextColor = Color.FromHex("#FFF");
            MNivel2.TextColor = Color.FromHex("#FFF");

            double precioNormal = Convert.ToDouble(precioFString);
            double precioNivel1 = Convert.ToDouble(precio1FString);
            double precioNivel2 = Convert.ToDouble(precio2FString);

            double margenn = (precioNormal - costoFamilia) / (precioNormal * 0.01);
            double margen1 = (precioNivel1 - costoFamilia) / (precioNivel1 * 0.01);
            double margen2 = (precioNivel2 - costoFamilia) / (precioNivel2 * 0.01);

            MNormal.Text = $"Margen: %{margenn:F2}";
            MNivel1.Text = $"Margen Nivel1: %{margen1:F2}";
            MNivel2.Text = $"Margen Nivel2: %{margen2:F2}";

            if (margenn < 0)
            {
                PNormal.TextColor = Color.FromHex("#DF3068");
                MNormal.TextColor = Color.FromHex("#DF3068");
            }

            if (margen1 < 0)
            {
                PNivel.TextColor = Color.FromHex("#DF3068");
                MNivel1.TextColor = Color.FromHex("#DF3068");
            }

            if (margen2 < 0)
            {
                PNivel1.TextColor = Color.FromHex("#DF3068");
                MNivel2.TextColor = Color.FromHex("#DF3068");
            }

            if (nivel2 >= 3)
            {
                if (idsursal == 4)
                {
                    double fontSizeOferta = 50;
                    Oferta.FontSize = fontSizeOferta;

                    Oferta.Text = "A Partir De " + nivel2;
                    Precio.Text = Convert.ToDouble(precio2FString).ToString("F2");
                    PNivel1.Text = $"Precio Nivel2: A partir de {nivel2} a " + $"{double.Parse(precio2FString):F2}";
                }
                else
                {
                    double fontSizeOferta = 90;
                    Oferta.FontSize = fontSizeOferta;
                    string nivelString = $"{nivel2}x";
                    double precioOferta = Math.Round(precioNivel2 * nivel2, 2);

                    Oferta.Text = "OFERTA";
                    Nivel.Text = nivelString;
                    Precio.Text = precioOferta.ToString("F2");
                    PNivel1.Text = $"Precio Nivel2: {nivelString} {precioOferta:F2}";
                }
                Descripcion.Text = descLarga;
                Exis.Text = $"Existencia: {existenciaString}";
                PNormal.Text = $"Precio Unidad: {double.Parse(precioFString):F2}";

                if (nivel1 > 1)
                {
                    if (idsursal == 4)
                    {
                        PNivel.Text = $"A partir de {nivel1} a" + precio1FString;
                    }
                    else
                    {
                        string niv1 = $"{nivel1}x";
                        double precioNiv1 = Convert.ToDouble(precio1FString);
                        double precioOfertaniv = Math.Round(precioNiv1 * nivel1, 2);

                        PNivel.Text = $"Precio Nivel1: {niv1} {precioOfertaniv:F2}";
                    }
                }
                else if (nivel1 == 1)
                {
                    if (idsursal == 4)
                    {
                        PNivel.Text = $"A partir de {nivel1} a" + precio1FString;
                    }
                    else
                    {
                        PNivel.Text = $"Precio Nivel1: {double.Parse(precio1FString):F2}";
                    }
                }
                else if (nivel1 == 0)
                {
                    PNivel.Text = "Sin Nivel 1";
                    MNivel1.Text = null;
                    PNivel.TextColor = Color.FromHex("#FFF");
                    MNivel1.TextColor = Color.FromHex("#FFF");
                }
            }
            else if (nivel2 == 2)
            {
                if (idsursal == 4)
                {
                    double fontSizeOferta = 50;
                    Oferta.FontSize = fontSizeOferta;

                    double precioMitad = double.Parse(precioFString) / 2;

                    if (double.TryParse(precio2FString, out double precioMaxNivel2))
                    {
                        if (Math.Abs(precioMaxNivel2 - precioMitad) <= 0.02)
                        {
                            Oferta.Text = "2x1";
                            Nivel.Text = " ";
                            Precio.Text = double.Parse(precioFString).ToString("F2");
                            PNivel1.Text = $"Precio Nivel2: {double.Parse(precio2FString):F2}";
                        }
                        else
                        {
                            Oferta.Text = $"A Partir De {nivel2}";
                            Precio.Text = Convert.ToDouble(precio2FString).ToString("F2");
                            PNivel1.Text = $"A Partir De {nivel2} a" + Convert.ToDouble(precio2FString).ToString("F2");
                        }
                        PNormal.Text = $"Precio Unidad: {double.Parse(precioFString):F2}";
                        Exis.Text = $"Existencia: {existenciaString}";
                        Descripcion.Text = descLarga;

                        if (nivel1 > 1)
                        {
                            string niv1 = $"{nivel1}x";
                            double precioNiv1 = Convert.ToDouble(precio1FString);
                            double precioOfertaniv = Math.Round(precioNiv1 * nivel1, 2);

                            PNivel.Text = $"Precio Nivel1: {niv1} {precioOfertaniv:F2}";
                        }
                        else if (nivel1 == 1)
                        {
                            PNivel.Text = $"Precio Nivel1: {double.Parse(precio1FString):F2}";
                        }
                        else if (nivel1 == 0)
                        {
                            PNivel.Text = "Sin Nivel 1";
                            MNivel1.Text = null;
                            PNivel.TextColor = Color.FromHex("#FFF");
                            MNivel1.TextColor = Color.FromHex("#FFF");
                        }
                    }
                    else
                    {
                        var popup = new DynamicPopup(
                        "Error",
                        "Error al consultar",
                        new Dictionary<string, Action>
                        {
                            { "OK", () => {} }
                         });

                        await PopupNavigation.Instance.PushAsync(popup);
                    }
                }
                else
                {
                    double fontSizeOferta = 90;
                    Oferta.FontSize = fontSizeOferta;

                    double precioMitad = double.Parse(precioFString) / 2;

                    if (double.TryParse(precio2FString, out double precioMaxNivel2))
                    {
                        if (Math.Abs(precioMaxNivel2 - precioMitad) <= 0.02)
                        {
                            Oferta.Text = "2x1";
                            Nivel.Text = " ";
                            Descripcion.Text = descLarga;
                            Precio.Text = double.Parse(precioFString).ToString("F2");
                            Exis.Text = $"Existencia: {existenciaString}";
                            PNormal.Text = $"Precio Unidad: {double.Parse(precioFString):F2}";
                            PNivel1.Text = $"Precio Nivel2: {double.Parse(precio2FString):F2}";
                        }
                        else
                        {
                            string nivelString = $"{nivel2}x";
                            double precioOferta = Math.Round(precioNivel2 * nivel2, 2);

                            Oferta.Text = "OFERTA";
                            Nivel.Text = nivelString;
                            Descripcion.Text = descLarga;
                            Precio.Text = precioOferta.ToString("F2");
                            Exis.Text = $"Existencia: {existenciaString}";
                            PNormal.Text = $"Precio Unidad: {double.Parse(precioFString):F2}";
                            PNivel1.Text = $"Precio Nivel2: {nivel2}x" + precioOferta;
                        }

                        if (nivel1 > 1)
                        {
                            if (idsursal == 4)
                            {
                                PNivel.Text = $"A partir de {nivel1} a" + precio1FString;
                            }
                            else
                            {
                                string niv1 = $"{nivel1}x";
                                double precioNiv1 = Convert.ToDouble(precio1FString);
                                double precioOfertaniv = Math.Round(precioNiv1 * nivel1, 2);

                                PNivel.Text = $"Precio Nivel1: {niv1} {precioOfertaniv:F2}";
                            }
                        }
                        else if (nivel1 == 1)
                        {
                            if (idsursal == 4)
                            {
                                PNivel.Text = $"A partir de {nivel1} a" + precio1FString;
                            }
                            else
                            {
                                PNivel.Text = $"Precio Nivel1: {double.Parse(precio1FString):F2}";
                            }
                        }
                        else if (nivel1 == 0)
                        {
                            PNivel.Text = "Sin Nivel 1";
                            MNivel1.Text = null;
                            PNivel.TextColor = Color.FromHex("#FFF");
                            MNivel1.TextColor = Color.FromHex("#FFF");
                        }
                    }
                    else
                    {
                        var popup = new DynamicPopup(
                        "Error",
                        "Error al consultar",
                        new Dictionary<string, Action>
                        {
                            { "OK", () => {} }
                         });

                        await PopupNavigation.Instance.PushAsync(popup);
                    }
                }
            }
            else if (nivel2 == 1)
            {
                if (idsursal == 4)
                {
                    double fontSizeOferta = 50;
                    Oferta.FontSize = fontSizeOferta;

                    Oferta.Text = "A Partir De " + nivel2;
                    Precio.Text = Convert.ToDouble(precio2FString).ToString("F2");
                    PNivel1.Text = $"Precio Nivel2: A partir de {nivel2} a " + $"{double.Parse(precio2FString):F2}";
                }
                else
                {
                    double fontSizeOferta = 90;
                    Oferta.FontSize = fontSizeOferta;
                    string nivelString = "  ";

                    Oferta.Text = "OFERTA";
                    Nivel.Text = nivelString;
                    Precio.Text = double.Parse(precio2FString).ToString("F2");
                    PNivel1.Text = $"Precio Nivel2: {double.Parse(precio2FString):F2}";
                }

                Descripcion.Text = descLarga;
                Exis.Text = $"Existencia: {existenciaString}";
                PNormal.Text = $"Precio Unidad: {double.Parse(precioFString):F2}";

                if (nivel1 > 1)
                {
                    if (idsursal == 4)
                    {
                        PNivel.Text = $"A partir de {nivel1} a" + precio1FString;
                    }
                    else
                    {
                        string niv1 = $"{nivel1}x";
                        double precioNiv1 = Convert.ToDouble(precio1FString);
                        double precioOfertaniv = Math.Round(precioNiv1 * nivel1, 2);

                        PNivel.Text = $"Precio Nivel1: {niv1} {precioOfertaniv:F2}";
                    }
                }
                else if (nivel1 == 1)
                {
                    if (idsursal == 4)
                    {
                        PNivel.Text = $"A partir de {nivel1} a" + precio1FString;
                    }
                    else
                    {
                        PNivel.Text = $"Precio Nivel1: {double.Parse(precio1FString):F2}";
                    }
                }
                else if (nivel1 == 0)
                {
                    PNivel.Text = "Sin Nivel 1";
                    MNivel1.Text = null;
                    PNivel.TextColor = Color.FromHex("#FFF");
                    MNivel1.TextColor = Color.FromHex("#FFF");
                }
            }
        }

        private async void VerProducto(object sender, EventArgs e)
        {
            Oferta.Text = null;
            Nivel.Text = null;
            Descripcion.Text = null;
            Precio.Text = null;
            Exis.Text = null;
            PNormal.Text = null;
            PNivel.Text = null;

            VistaBuscar.IsVisible = false;
            Contenedor.IsVisible = true;

            if (sender is Button button && button.BindingContext is Producto producto)
            {
                string upc = producto.Upc;

                var productoFiltrado = Productos.FirstOrDefault(p => p.Upc == upc);

                string query = $"SELECT Costo, Precio, DescLarga, Nivel1, PrecioMaxNivel1, Nivel2, PrecioMaxNivel2, GruposProductosID, Existencia FROM productos WHERE Upc = '{upc}'";
                try
                {
                    using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                    {
                        connection.Open();
                        using (MySqlCommand command = new MySqlCommand(query, connection))
                        {
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    double costoProducto = Convert.ToDouble(reader["Costo"]);
                                    int gruposProductosID = Convert.ToInt32(reader["GruposProductosID"]);
                                    string descLarga = reader["DescLarga"].ToString();
                                    string precioString = reader["Precio"].ToString();
                                    string nivel1String = reader["Nivel1"].ToString();
                                    string precio1String = reader["PrecioMaxNivel1"].ToString();
                                    string nivel2String = reader["Nivel2"].ToString();
                                    string precio2String = reader["PrecioMaxNivel2"].ToString();
                                    string existenciaString = reader["Existencia"].ToString();

                                    Familia.Text = productoFiltrado.IdFamilia;
                                    reader.Close();

                                    if (gruposProductosID > 0)
                                    {
                                        VF.IsVisible = true;
                                        Regresar.IsVisible = false;

                                        string query1 = $"SELECT Costo, PrecioNormal, Nivel1, PrecioMaxNivel1, Nivel2, PrecioMaxNivel2 FROM gruposproductos WHERE Id = {gruposProductosID}";
                                        using (MySqlCommand command1 = new MySqlCommand(query1, connection))
                                        {
                                            using (MySqlDataReader reader1 = command1.ExecuteReader())
                                            {
                                                if (reader1.Read())
                                                {
                                                    double costoFamilia = Convert.ToDouble(reader["Costo"]);
                                                    string precioFString = reader1["PrecioNormal"].ToString();
                                                    string nivel1FString = reader1["Nivel1"].ToString();
                                                    string precio1FString = reader1["PrecioMaxNivel1"].ToString();
                                                    string nivel2FString = reader1["Nivel2"].ToString();
                                                    string precio2FString = reader1["PrecioMaxNivel2"].ToString();

                                                    if (!int.TryParse(nivel1FString, out int nivel1F) || !int.TryParse(nivel2FString, out int nivel2F))
                                                    {
                                                        return;
                                                    }

                                                    if (nivel2F > 0)
                                                    {
                                                        ValidarNiv2F(costoFamilia, nivel1FString, precio1FString, nivel2FString, precio2FString, existenciaString, precioFString, descLarga);
                                                    }
                                                    else if (nivel1F > 0)
                                                    {
                                                        ValidarNiv1F(costoFamilia, nivel1FString, precio1FString, nivel2FString, precio2FString, existenciaString, precioFString, descLarga);
                                                    }
                                                    else
                                                    {
                                                        double fontSizeOferta = 50;
                                                        Oferta.FontSize = fontSizeOferta;
                                                        string nivelFString = "  ";

                                                        Oferta.Text = "Precio Normal";
                                                        Nivel.Text = nivelFString;
                                                        Descripcion.Text = descLarga;
                                                        Precio.Text = double.Parse(precioFString).ToString("F2");
                                                        Exis.Text = $"Existencia: {existenciaString}";

                                                        PNormal.Text = $"Precio Unidad: {double.Parse(precioFString):F2}";

                                                        PNivel.Text = "Sin Nivel 1";
                                                        PNivel1.Text = "Sin Nivel 2";

                                                        MNivel1.Text = null;
                                                        MNivel2.Text = null;

                                                        PNormal.TextColor = Color.FromHex("#FFF");
                                                        MNormal.TextColor = Color.FromHex("#FFF");
                                                        PNivel.TextColor = Color.FromHex("#FFF");
                                                        PNivel1.TextColor = Color.FromHex("#FFF");
                                                        MNivel1.TextColor = Color.FromHex("#FFF");
                                                        MNivel2.TextColor = Color.FromHex("#FFF");

                                                        double precioNormalF = Convert.ToDouble(precioFString);

                                                        double margennF = (precioNormalF - costoFamilia) / (precioNormalF * 0.01);

                                                        MNormal.Text = $"Margen: %{margennF:F2}";

                                                        if (margennF < 0)
                                                        {
                                                            PNormal.TextColor = Color.FromHex("#DF3068");
                                                            MNormal.TextColor = Color.FromHex("#DF3068");
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        VF.IsVisible = false;
                                        Regresar.IsVisible = true;
                                        if (!int.TryParse(nivel1String, out int nivel1) || !int.TryParse(nivel2String, out int nivel2))
                                        {
                                            return;
                                        }

                                        if (nivel2 > 0)
                                        {
                                            ValidarNiv2(costoProducto, nivel1String, precio1String, nivel2String, precio2String, existenciaString, precioString, descLarga);
                                        }
                                        else if (nivel1 > 0)
                                        {
                                            ValidarNiv1(costoProducto, nivel1String, precio1String, existenciaString, precioString, descLarga);
                                        }
                                        else
                                        {
                                            double fontSizeOferta = 50;
                                            Oferta.FontSize = fontSizeOferta;
                                            string nivelString = "  ";

                                            Oferta.Text = "Precio Normal";
                                            Nivel.Text = nivelString;
                                            Descripcion.Text = descLarga;
                                            Precio.Text = double.Parse(precioString).ToString("F2");
                                            Exis.Text = $"Existencia: {existenciaString}";

                                            PNormal.Text = $"Precio Unidad: {double.Parse(precioString):F2}";

                                            PNivel.Text = "Sin Nivel 1";
                                            PNivel1.Text = "Sin Nivel 2";

                                            MNivel1.Text = null;
                                            MNivel2.Text = null;

                                            PNormal.TextColor = Color.FromHex("#FFF");
                                            MNormal.TextColor = Color.FromHex("#FFF");
                                            PNivel.TextColor = Color.FromHex("#FFF");
                                            MNivel1.TextColor = Color.FromHex("#FFF");
                                            PNivel1.TextColor = Color.FromHex("#FFF");
                                            MNivel2.TextColor = Color.FromHex("#FFF");

                                            double precioNormal = Convert.ToDouble(precioString);

                                            double margenn = (precioNormal - costoProducto) / (precioNormal * 0.01);

                                            MNormal.Text = $"Margen: %{margenn:F2}";

                                            if (margenn < 0)
                                            {
                                                PNormal.TextColor = Color.FromHex("#DF3068");
                                                MNormal.TextColor = Color.FromHex("#DF3068");
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    ResultadosListView.IsVisible = false;
                                    Descripcion.Text = "Producto no encontrado";
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    var popup = new DynamicPopup(
                    "Error de Conexión",
                    "Error al intentar conectar a la base de datos: " + ex.Message,
                    new Dictionary<string, Action>
                    {
                        { "OK", () => {} }
                    });

                    await PopupNavigation.Instance.PushAsync(popup);
                }
            }
        }

        private async void VerProducto_Clicked(object sender, EventArgs e)
        {
            var loadingPopup = new LoadingPopup();

            try
            {
                await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(loadingPopup);

                AppShell appShell = (AppShell)Application.Current.MainPage;
                appShell.ResetInactivityTimer();

                Img.IsVisible = false;

                VerProducto(sender, e);
            }
            catch (Exception ex)
            {
                var popup = new DynamicPopup(
                "Error",
                $"Se produjo un error: {ex.Message}",
                new Dictionary<string, Action>
                {
                    { "OK", () => {} }
                });

                await PopupNavigation.Instance.PushAsync(popup);

            }
            finally
            {
                await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync();
            }
        }

        private void Button_Focused(object sender, FocusEventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();

            if (sender is Entry entry)
            {
                Device.BeginInvokeOnMainThread(() => entry.CursorPosition = 0);
                if (entry.Text != null)
                {
                    Device.BeginInvokeOnMainThread(() => entry.SelectionLength = entry.Text.Length);
                }
            }
        }

        private void Button_Clicked_2(object sender, EventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();

            VistaBuscar.IsVisible = true;
            Img.IsVisible = true;
            ResultadosListView.IsVisible = false;
            Contenedor.IsVisible = false;
            eupc.Text = "";
            edescripcion.Text = "";
        }

        private void Reset_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();
        }

        private void Regresar_Clicked(object sender, EventArgs e)
        {
            Contenedor.IsVisible = false;
            VistaBuscar.IsVisible = true;
            VF.IsVisible = false;
        }

        private async void EscanearUPC_Clicled(object sender, EventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();

            eupc.Text = null;
            edescripcion.Text = null;
            Nivel.Text = null;
            Descripcion.Text = null;
            Precio.Text = null;
            Exis.Text = null;
            Oferta.Text = null;
            PNormal.Text = null;
            Precio.Text = null;

            try
            {
                var scanner = new ZXing.Mobile.MobileBarcodeScanner();
                var options = new ZXing.Mobile.MobileBarcodeScanningOptions
                {
                    PossibleFormats = new List<ZXing.BarcodeFormat> { ZXing.BarcodeFormat.All_1D }
                };

                // Texto superior e inferior del escáner
                // scanner.TopText = "Escanea el código QR";
                // scanner.BottomText = "Por favor, coloca el código QR en el área de escaneo";    

                var result = await scanner.Scan(options);

                if (result != null)
                {
                    eupc.Text = result.Text;

                    BProducto(sender, e);

                    ResultadosListView.IsVisible = true;
                }
                else
                {
                    eupc.Text = null;
                }
            }
            catch (Exception ex)
            {
                var popup = new DynamicPopup(
                "Error",
                ex.Message,
                new Dictionary<string, Action>
                {
                    { "OK", () => {} }
                });

                await PopupNavigation.Instance.PushAsync(popup);

            }
        }

        private async void VF_Clicked(object sender, EventArgs e)
        {
            var loadingPopup = new LoadingPopup();

            try
            {
                await PopupNavigation.Instance.PushAsync(loadingPopup);

                string FamiliaID = Familia.Text;
                string query = $"SELECT UPC, DescLarga FROM productos WHERE GruposProductosId = {FamiliaID}";
                List<string> productosList = new List<string>();

                using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                {
                    connection.Open();
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                productosList.Add($"UPC: {reader["Upc"]}\nDescLarga: {reader["DescLarga"]} \n");
                            }
                        }
                    }
                }

                string productosMensaje = productosList.Count > 0 ? string.Join("\n", productosList) : "No hay productos en esta familia.";

                await PopupNavigation.Instance.PopAsync();

                var popupProductos = new DynamicPopup(
                    "Productos de Familia:",
                    productosMensaje,
                    new Dictionary<string, Action>
                    {
                { "Aceptar", () => {} }
                    });

                await PopupNavigation.Instance.PushAsync(popupProductos);
            }
            catch (Exception ex)
            {
                await PopupNavigation.Instance.PopAsync();

                var popupError = new DynamicPopup(
                    "Error",
                    $"Se produjo un error: {ex.Message}",
                    new Dictionary<string, Action>
                    {
                { "OK", () => {} }
                    });

                await PopupNavigation.Instance.PushAsync(popupError);
            }
            finally
            {
                if (PopupNavigation.Instance.PopupStack.Contains(loadingPopup))
                {
                    await PopupNavigation.Instance.PopAsync();
                }
            }
        }
    }
}