using AppBodegona.Services;
using System;
using System.Collections.ObjectModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using MySqlConnector;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xamarin.Essentials;
using Rg.Plugins.Popup.Services;
using System.Linq;

namespace AppBodegona.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Precio : ContentPage
    {
        public ObservableCollection<Producto> Productos { get; set; }
        public List<string> UpcList { get; private set; }

        public Precio()
        {
            InitializeComponent();
            UpdateImage1();

            UpcList = new List<string>();

            try
            {
                bool isConnected = DatabaseConnection.TestConnection(DatabaseConnection.ConnectionString);
                if (!isConnected)
                {
                    var popup = new DynamicPopup(
                    "Error de Conexión",
                    "No se pudo conectar a la base de datos.",
                    new Dictionary<string, Action>
                    {
                        { "OK", () => {} }
                    });

                    PopupNavigation.Instance.PushAsync(popup);
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

                PopupNavigation.Instance.PushAsync(popup);
            }

            Productos = new ObservableCollection<Producto>();
            ResultadosListView.ItemsSource = Productos;

            var tapGestureRecognizer = new TapGestureRecognizer();
            tapGestureRecognizer.Tapped += (s, e) =>
            {
                AppShell appShell = (AppShell)Application.Current.MainPage;
                appShell.ResetInactivityTimer();
            };
            MainContentView.GestureRecognizers.Add(tapGestureRecognizer);
        }


        public void UpdateImage1()
        {
            int idSucursal = Preferences.Get("ID_Sucursal", 0);

            if (idSucursal == 4)
            {
                Img.Source = "supermercadon.png";
            }
            else
            {
                Img.Source = "bodegona.png";
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
                { "Si", () => System.Diagnostics.Process.GetCurrentProcess().CloseMainWindow() },
                { "No", () => {} }
                    });

                PopupNavigation.Instance.PushAsync(popup);
            });
            return true;
        }

        protected async override void OnAppearing()
        {
            base.OnAppearing();
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();

            if (string.IsNullOrEmpty(appShell.Usuario))
            {
                UpdateImage1();

                var tcs = new TaskCompletionSource<bool>();

                var popup = new DynamicPopup(
                    "Advertencia",
                    "Para continuar, inicie sesión",
                    new Dictionary<string, Action>
                    {
                { "Aceptar", () => tcs.SetResult(true) },
                { "Cancelar", () => tcs.SetResult(false) }
                    });

                await PopupNavigation.Instance.PushAsync(popup);
                bool loginistrue = await tcs.Task;

                if (!loginistrue)
                {
                    await Shell.Current.GoToAsync($"//{nameof(Existencia)}");
                }
                else
                {
                    NavigationService.DestinationPage = "Precio";
                    await Shell.Current.GoToAsync("Login");
                }
            }
            else
            {
                if (appShell.IdNivel != "11")
                {
                    var popup = new DynamicPopup(
                        "Acceso Denegado",
                        "No tiene permiso para acceder a esta página.",
                        new Dictionary<string, Action>
                        {
                    { "OK", async () => await Shell.Current.GoToAsync($"//{nameof(Existencia)}") }
                        });

                    await PopupNavigation.Instance.PushAsync(popup);
                }
            }
        }

        private void ListView_ItemSelectedFalse(object sender, SelectedItemChangedEventArgs e)
        {
            ResultadosListView.SelectedItem = null;
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

        public class Producto
        {
            public string Upc { get; set; }
            public string DescLarga { get; set; }
            public string Existencia { get; set; }
            public string Precio { get; set; }
        }

        private void Cambio_Clicked(object sender, EventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();
            Productos.Clear();
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
            ResultadosListView.IsVisible = false;
        }

        private async void BProducto(object sender, EventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(eupc.Text))
                {
                    string entryupcText = eupc.Text;

                    if (entryupcText.Length < 13)
                    {
                        int cerosToAdd = 13 - entryupcText.Length;
                        entryupcText = new string('0', cerosToAdd) + entryupcText;
                    }

                    string query = $"SELECT Upc, Precio, DescLarga, Nivel1, PrecioMaxNivel1, Existencia FROM productos WHERE Upc = '{entryupcText}'";

                    using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                    {
                        connection.Open();
                        using (MySqlCommand command = new MySqlCommand(query, connection))
                        {
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    Productos.Clear();
                                    Productos.Add(new Producto
                                    {
                                        Upc = reader["Upc"].ToString(),
                                        DescLarga = reader["DescLarga"].ToString(),
                                        Existencia = reader["Existencia"].ToString(),
                                        Precio = reader["Precio"].ToString()
                                    });

                                    Img.IsVisible = false;
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
                                    return;
                                }
                            }
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(edescripcion.Text))
                {
                    string entryDescText = edescripcion.Text;
                    string[] searchTerms = entryDescText.Split(' ');
                    string query = "SELECT Upc, Precio, Nivel1, PrecioMaxNivel1, DescLarga, Existencia FROM productos WHERE ";

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
                                        Precio = reader["Precio"].ToString()
                                    });

                                    Img.IsVisible = false;
                                    ResultadosListView.IsVisible = true;
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
                                    return;
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
                    return;
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
            }
        }

        private async void Buscar_Clicked(object sender, EventArgs e)
        {
            var loadingPopup = new LoadingPopup();
            await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(loadingPopup);

            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();
            BProducto(sender, e);
            await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync();
        }

        private async void Escanear_Clicked(object sender, EventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();

            Img.IsVisible = false;
            eupc.Text = null;
            edescripcion.Text = null;
            Productos.Clear();

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
                    var loadingPopup = new LoadingPopup(); 
                    await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(loadingPopup);
                    eupc.Text = result.Text;

                    BProducto(sender, e);

                    ResultadosListView.IsVisible = true;
                    await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync();

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

        private void Limpiar_Clicked(object sender, EventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();
            Productos.Clear();
            Img.IsVisible = true;
            ResultadosListView.IsVisible = false;
            eupc.Text = "";
            edescripcion.Text = "";
        }

        private void VerProducto(object sender, EventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();
            if (sender is Button button && button.BindingContext is Producto producto)
            {
                string upc = producto.Upc;

                VistaBusqueda.IsVisible = false;
                VistaCambio.IsVisible = true;

                string query = $"SELECT DescLarga, Precio, Costo, Margen, Nivel1, PrecioMaxNivel1, Margen1, Nivel2, PrecioMaxNivel2, Margen2, GruposProductosId FROM productos WHERE Upc = '{upc}'";

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
                                    string descLarga = reader["DescLarga"].ToString();
                                    decimal precio = Convert.ToDecimal(reader["Precio"]);
                                    decimal costo = Convert.ToDecimal(reader["Costo"]);
                                    decimal margen = Convert.ToDecimal(reader["Margen"]);

                                    int nivel1 = Convert.ToInt32(reader["Nivel1"]);
                                    decimal precio1 = Convert.ToDecimal(reader["PrecioMaxNivel1"]);
                                    decimal margen1 = Convert.ToDecimal(reader["Margen1"]);

                                    int nivel2 = Convert.ToInt32(reader["Nivel2"]);
                                    decimal precio2 = Convert.ToDecimal(reader["PrecioMaxNivel2"]);
                                    decimal margen2 = Convert.ToDecimal(reader["Margen2"]);


                                    upcc.Text = upc;
                                    descripcionc.Text = descLarga;
                                    costoc.Text = costo.ToString("F2");
                                    precioc.Text = precio.ToString("F2");
                                    margenc.Text = margen.ToString("F2");

                                    N1.Text = nivel1.ToString();
                                    P1.Text = precio1.ToString("F2");
                                    M1.Text = margen1.ToString("F2");

                                    N2.Text = nivel2.ToString();
                                    P2.Text = precio2.ToString("F2");
                                    M2.Text = margen2.ToString("F2");

                                    Img.IsVisible = false;
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

                    PopupNavigation.Instance.PushAsync(popup);
                }
            }
        }

        private async void VerFamilia(object sender, EventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();
            Img.IsVisible = false;

            if (sender is Button button && button.BindingContext is Producto producto)
            {
                string upc = producto.Upc;
                string query = $"SELECT GruposProductosId FROM productos WHERE Upc = '{upc}'";

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
                                    int gruposProductosId = reader.GetInt32("GruposProductosId");

                                    if (gruposProductosId > 0)
                                    {
                                        string query1 = $"SELECT Id, Nombre, PrecioNormal, Costo, Margen, Nivel1, PrecioMaxNivel1, Margen1, Nivel2, PrecioMaxNivel2, Margen2 FROM gruposproductos WHERE Id = '{gruposProductosId}'";

                                        using (MySqlConnection connection1 = new MySqlConnection(DatabaseConnection.ConnectionString))
                                        {
                                            connection1.Open();
                                            using (MySqlCommand command1 = new MySqlCommand(query1, connection1))
                                            {
                                                using (MySqlDataReader reader1 = command1.ExecuteReader())
                                                {
                                                    if (reader1.Read())
                                                    {
                                                        string id = reader1["Id"].ToString();
                                                        string nombre = reader1["Nombre"].ToString();
                                                        decimal precioNormal = Convert.ToDecimal(reader1["PrecioNormal"]);
                                                        decimal costo = Convert.ToDecimal(reader1["Costo"]);
                                                        decimal margen = Convert.ToDecimal(reader1["Margen"]);

                                                        int nivel1 = Convert.ToInt32(reader1["Nivel1"]);
                                                        decimal precioMaxNivel1 = Convert.ToDecimal(reader1["PrecioMaxNivel1"]);
                                                        decimal margen1 = Convert.ToDecimal(reader1["Margen1"]);

                                                        int nivel2 = Convert.ToInt32(reader1["Nivel2"]);
                                                        decimal precioMaxNivel2 = Convert.ToDecimal(reader1["PrecioMaxNivel2"]);
                                                        decimal margen2 = Convert.ToDecimal(reader1["Margen2"]);

                                                        upcc.Text = id;
                                                        descripcionc.Text = nombre;
                                                        costoc.Text = costo.ToString("F2");
                                                        precioc.Text = precioNormal.ToString("F2");
                                                        margenc.Text = margen.ToString("F2");

                                                        N1.Text = nivel1.ToString();
                                                        P1.Text = precioMaxNivel1.ToString("F2");
                                                        M1.Text = margen1.ToString("F2");

                                                        N2.Text = nivel2.ToString();
                                                        P2.Text = precioMaxNivel2.ToString("F2");
                                                        M2.Text = margen2.ToString("F2");

                                                        VistaBusqueda.IsVisible = false;
                                                        VistaCambio.IsVisible = true;

                                                        BuscarProductosPorFamilia(gruposProductosId);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        var popup = new DynamicPopup(
                                        "Alerta",
                                        "La familia del producto es 0",
                                        new Dictionary<string, Action>
                                        {
                                            { "Aceptar", () => {} }
                                        });

                                        await PopupNavigation.Instance.PushAsync(popup);
                                    }
                                }
                                else
                                {
                                    var popup = new DynamicPopup(
                                    "Alerta",
                                    "Producto no encontrado",
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

        private async void BuscarProductosPorFamilia(int gruposProductosId)
        {
            string query = $"SELECT Upc FROM productos WHERE GruposProductosId = {gruposProductosId}";

            try
            {
                using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                {
                    connection.Open();
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            UpcList.Clear();
                            while (reader.Read())
                            {
                                string upc = reader["Upc"].ToString();
                                UpcList.Add(upc);
                            }

                            if (UpcList.Count > 0)
                            {
                                string upcMessage = string.Join(", ", UpcList);
                            }
                            else
                            {
                                var popup = new DynamicPopup(
                                "Información",
                                "No se encontraron productos para esta familia.",
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

        private async void Editar_Clicked(object sender, EventArgs e)
        {
            var loadingPopup = new LoadingPopup(); 
            await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(loadingPopup);
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();
            if (sender is Button button && button.BindingContext is Producto producto)
            {
                string upc = producto.Upc;

                string query = $"SELECT GruposProductosId FROM productos WHERE Upc = '{upc}'";

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
                                    int gruposProductosId = reader.GetInt32("GruposProductosId");

                                    if (gruposProductosId > 0)
                                    {
                                        await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync();

                                        var popup = new DynamicPopup(
                                        "Advertencia",
                                        "Se modificará la familia y sus productos!",
                                        new Dictionary<string, Action>
                                        {
                                            { "OK", () => {VerFamilia(sender, e);} }
                                        });

                                        await PopupNavigation.Instance.PushAsync(popup);
                                    }
                                    else
                                    {
                                        VerProducto(sender, e);
                                        await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync();
                                    }
                                }
                                else
                                {
                                    var popup = new DynamicPopup(
                                    "Alerta",
                                    "Producto no encontrado",
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

        private void Button_Clicked_1(object sender, EventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();
            Img.IsVisible = false;
            VistaBusqueda.IsVisible = true;
            VistaCambio.IsVisible = false;
        }

        private async Task ActualizarFamiliaYProductos(int gruposProductosId, decimal nuevoPrecio, decimal nuevoMargen, int nivel1, decimal precio1, decimal margen1, int nivel2, decimal precio2, decimal margen2)
        {
            string query = @"UPDATE productos SET 
                   Precio = @NuevoPrecio, 
                   Margen = @NuevoMargen,
                   Nivel1 = @Nivel1,
                   PrecioMaxNivel1 = @Precio1,
                   Margen1 = @Margen1,
                   Nivel2 = @Nivel2,
                   PrecioMaxNivel2 = @Precio2,
                   Margen2 = @Margen2
                   WHERE GruposProductosId = @GruposProductosId";

            try
            {
                using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                {
                    await connection.OpenAsync();
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@NuevoPrecio", nuevoPrecio);
                        command.Parameters.AddWithValue("@NuevoMargen", nuevoMargen);
                        command.Parameters.AddWithValue("@Nivel1", nivel1);
                        command.Parameters.AddWithValue("@Precio1", precio1);
                        command.Parameters.AddWithValue("@Margen1", margen1);
                        command.Parameters.AddWithValue("@Nivel2", nivel2);
                        command.Parameters.AddWithValue("@Precio2", precio2);
                        command.Parameters.AddWithValue("@Margen2", margen2);
                        command.Parameters.AddWithValue("@GruposProductosId", gruposProductosId);

                        await command.ExecuteNonQueryAsync();
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

        private async void Guardar_Clicked(object sender, EventArgs e)
        {
            var loadingPopup = new LoadingPopup();

            try
            {
                CalcularPrecioyMargen();
                AppShell appShell = (AppShell)Application.Current.MainPage;
                appShell.ResetInactivityTimer();

                string upc = upcc.Text;
                string precioString = precioc.Text;
                string margenString = margenc.Text;

                string nivel1String = N1.Text;
                string precio1String = P1.Text;
                string margen1String = M1.Text;

                string nivel2String = N2.Text;
                string precio2String = P2.Text;
                string margen2String = M2.Text;

                if (string.IsNullOrWhiteSpace(upc) ||
                    string.IsNullOrWhiteSpace(precioString) ||
                    string.IsNullOrWhiteSpace(margenString) ||
                    string.IsNullOrWhiteSpace(nivel1String) ||
                    string.IsNullOrWhiteSpace(precio1String) ||
                    string.IsNullOrWhiteSpace(margen1String) ||
                    string.IsNullOrWhiteSpace(nivel2String) ||
                    string.IsNullOrWhiteSpace(precio2String) ||
                    string.IsNullOrWhiteSpace(margen2String))
                {
                    throw new Exception("Todos los campos deben estar completos.");
                }

                int nivel1, nivel2;
                decimal precio, precio1, precio2;
                decimal margen = 0m, margen1 = 0m, margen2 = 0m;

                if (!decimal.TryParse(precioString, out precio) || precio <= 0)
                    throw new Exception("El precio principal no puede ser 0.");

                if (!int.TryParse(nivel1String, out nivel1) || !decimal.TryParse(precio1String, out precio1) || !decimal.TryParse(margen1String, out margen1))
                    throw new Exception("Los campos del Nivel 1 deben ser válidos.");

                if ((nivel1 > 0 || precio1 > 0 || margen1 > 0) && (nivel1 == 0 || precio1 == 0 || margen1 == 0))
                    throw new Exception("Los campos del Nivel 1 no pueden quedar a 0.");

                if (!int.TryParse(nivel2String, out nivel2) || !decimal.TryParse(precio2String, out precio2) || !decimal.TryParse(margen2String, out margen2))
                    throw new Exception("Los campos del Nivel 2 deben ser válidos.");

                if ((nivel2 > 0 || precio2 > 0 || margen2 > 0) && (nivel2 == 0 || precio2 == 0 || margen2 == 0))
                    throw new Exception("Los campos del Nivel 2 no pueden quedar a 0.");

                if (margen < 0 || margen1 < 0 || margen2 < 0)
                {
                    var tcs = new TaskCompletionSource<bool>();

                    var popup = new DynamicPopup(
                        "Advertencia",
                        "Uno o más márgenes son negativos. ¿Desea continuar?",
                        new Dictionary<string, Action>
                        {
                    { "No", () => tcs.SetResult(false) },
                    { "Sí", () => tcs.SetResult(true) }
                        });

                    await PopupNavigation.Instance.PushAsync(popup);
                    bool continueWithNegativeMargins = await tcs.Task;

                    if (!continueWithNegativeMargins)
                        throw new Exception("Operación cancelada por el usuario.");
                }

                if (nivel1 > 0 && nivel2 > 0 && nivel1 >= nivel2)
                    throw new Exception("El nivel 2 debe ser mayor al nivel 1.");

                if (precio1 > 0 && precio2 > 0 && precio1 <= precio2)
                    throw new Exception("El precio del nivel 2 debe ser menor al precio de nivel 1.");

                bool isGroupProduct = upc.Length < 13;
                bool success = false;

                string query = isGroupProduct
                    ? @"UPDATE gruposproductos SET 
                        PrecioNormal = @Precio,
                        Margen = @Margen,
                        Nivel1 = @Nivel1, 
                        PrecioMaxNivel1 = @Precio1, 
                        Margen1 = @Margen1, 
                        Nivel2 = @Nivel2, 
                        PrecioMaxNivel2 = @Precio2, 
                        Margen2 = @Margen2
                        WHERE Id = @Upc"
                    : @"UPDATE productos SET 
                        Precio = @Precio, 
                        Margen = @Margen, 
                        Nivel1 = @Nivel1, 
                        PrecioMaxNivel1 = @Precio1, 
                        Margen1 = @Margen1, 
                        Nivel2 = @Nivel2, 
                        PrecioMaxNivel2 = @Precio2, 
                        Margen2 = @Margen2
                        WHERE Upc = @Upc";

                ValidarCambios();

                using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                {
                    connection.Open();
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Precio", precio);
                        command.Parameters.AddWithValue("@Margen", margen);
                        command.Parameters.AddWithValue("@Nivel1", nivel1);
                        command.Parameters.AddWithValue("@Precio1", precio1);
                        command.Parameters.AddWithValue("@Margen1", margen1);
                        command.Parameters.AddWithValue("@Nivel2", nivel2);
                        command.Parameters.AddWithValue("@Precio2", precio2);
                        command.Parameters.AddWithValue("@Margen2", margen2);
                        command.Parameters.AddWithValue("@Upc", upc);

                        int result = command.ExecuteNonQuery();
                        if (result > 0)
                        {
                            success = true;
                        }
                        else
                        {
                            throw new Exception("No se pudo actualizar.");
                        }
                    }
                }

                if (success)
                {
                    var popup = new DynamicPopup(
                        "Éxito",
                        isGroupProduct ? "Familia y productos actualizados correctamente" : "Producto actualizado correctamente",
                        new Dictionary<string, Action>
                        {
                    { "OK", () => {} }
                        });

                    await PopupNavigation.Instance.PushAsync(popup);
                }

                upcc.Text = null;
                descripcionc.Text = null;
                costoc.Text = null;
                precioc.Text = null;
                margenc.Text = null;
                N1.Text = null;
                P1.Text = null;
                M1.Text = null;
                N2.Text = null;
                P2.Text = null;
                M2.Text = null;

                VistaCambio.IsVisible = false;
                VistaBusqueda.IsVisible = true;
                Img.IsVisible = false;
            }
            catch (Exception ex)
            {
                await PopupNavigation.Instance.PopAsync();
                var popup = new DynamicPopup(
                    "Error",
                    ex.Message,
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

        public class HistorialCambiosHelper
        {
            public static async Task InsertarHistorialCambio(string upc, int tipocambio, string valorAntiguo, string valorNuevo)
            {
                AppShell appShell = (AppShell)Application.Current.MainPage;
                string query = "INSERT INTO historialcambios (IdUsuarios, Usuario, Fecha, FechaHora, Upc, TipoCambio, DoubleAnt, DoubleAct) " +
                               "VALUES (@IdUsuario, @Usuario, @Fecha, @FechaHora, @Upc, @TipoCambio, @ValorAntiguo, @ValorNuevo)";

                using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@IdUsuario", appShell.Id);
                    command.Parameters.AddWithValue("@Usuario", appShell.Usuario);
                    command.Parameters.AddWithValue("@Upc", upc);
                    command.Parameters.AddWithValue("@TipoCambio", tipocambio);
                    command.Parameters.AddWithValue("@ValorAntiguo", valorAntiguo);
                    command.Parameters.AddWithValue("@ValorNuevo", valorNuevo);
                    command.Parameters.AddWithValue("@Fecha", DateTime.Now.Date);
                    command.Parameters.AddWithValue("@FechaHora", DateTime.Now);

                    try
                    {
                        connection.Open();
                        int result = command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        var popup = new DynamicPopup(
                        "Error",
                        "Error al insertar historial: " + ex.Message,
                        new Dictionary<string, Action>
                        {
                            { "OK", () => {} }
                        });

                        await PopupNavigation.Instance.PushAsync(popup);
                    }
                }
            }
        }

        public async void ValidarCambios()
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();

            string upc = upcc.Text;
            string precioString = precioc.Text;

            string nivel1String = N1.Text;
            string precio1String = P1.Text;

            string nivel2String = N2.Text;
            string precio2String = P2.Text;


            if (decimal.TryParse(precioString, out decimal precio))
            {
                if (upc.Length == 13)
                {
                    await ValidarCambiosProducto(upc, precioString, nivel1String, precio1String, nivel2String, precio2String);
                }
                else if (upc.Length < 13)
                {
                    await ValidarCambiosGrupoProducto(upc, precioString, nivel1String, precio1String, nivel2String, precio2String);
                }
            }
        }

        private async Task ValidarCambiosProducto(string upc, string precioString, string nivel1String, string precio1String, string nivel2String, string precio2String)
        {
            string query1 = $"SELECT Precio, Nivel1, PrecioMaxNivel1, Nivel2, PrecioMaxNivel2 FROM productos WHERE Upc = '{upc}'";

            try
            {
                using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                {
                    connection.Open();
                    using (MySqlCommand command = new MySqlCommand(query1, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                await CompararYAgregarCambio(upc, 13, Convert.ToDecimal(reader["Precio"]).ToString("F2"), precioString);
                                await CompararYAgregarCambio(upc, 14, reader["Nivel1"].ToString(), nivel1String);
                                await CompararYAgregarCambio(upc, 15, Convert.ToDecimal(reader["PrecioMaxNivel1"]).ToString("F2"), precio1String);
                                await CompararYAgregarCambio(upc, 16, reader["Nivel2"].ToString(), nivel2String);
                                await CompararYAgregarCambio(upc, 17, Convert.ToDecimal(reader["PrecioMaxNivel2"]).ToString("F2"), precio2String);
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

        private async Task ValidarCambiosGrupoProducto(string codigoFamilia, string precioString, string nivel1String, string precio1String, string nivel2String, string precio2String)
        {
            string query2 = $"SELECT PrecioNormal, Nivel1, PrecioMaxNivel1, Nivel2, PrecioMaxNivel2, Nivel3, PrecioMaxNivel3 FROM gruposproductos WHERE Id = '{codigoFamilia}'";

            try
            {
                using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                {
                    connection.Open();
                    using (MySqlCommand command = new MySqlCommand(query2, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                await CompararYAgregarCambio(codigoFamilia, 21, Convert.ToDecimal(reader["PrecioNormal"]).ToString("F2"), precioString);
                                await CompararYAgregarCambio(codigoFamilia, 22, reader["Nivel1"].ToString(), nivel1String);
                                await CompararYAgregarCambio(codigoFamilia, 23, Convert.ToDecimal(reader["PrecioMaxNivel1"]).ToString("F2"), precio1String);
                                await CompararYAgregarCambio(codigoFamilia, 24, reader["Nivel2"].ToString(), nivel2String);
                                await CompararYAgregarCambio(codigoFamilia, 25, Convert.ToDecimal(reader["PrecioMaxNivel2"]).ToString("F2"), precio2String);

                                foreach (var upc in UpcList)
                                {
                                    await CompararYAgregarCambio(upc, 13, Convert.ToDecimal(reader["PrecioNormal"]).ToString("F2"), precioString);
                                    await CompararYAgregarCambio(upc, 14, reader["Nivel1"].ToString(), nivel1String);
                                    await CompararYAgregarCambio(upc, 15, Convert.ToDecimal(reader["PrecioMaxNivel1"]).ToString("F2"), precio1String);
                                    await CompararYAgregarCambio(upc, 16, reader["Nivel2"].ToString(), nivel2String);
                                    await CompararYAgregarCambio(upc, 17, Convert.ToDecimal(reader["PrecioMaxNivel2"]).ToString("F2"), precio2String);
                                }
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

        private async Task CompararYAgregarCambio(string upc, int tipoCambioId, string valorAntiguo, string valorNuevo)
        {
            if (valorAntiguo != valorNuevo)
            {
                await HistorialCambiosHelper.InsertarHistorialCambio(upc, tipoCambioId, valorAntiguo, valorNuevo);
            }
        }

        public void CalcularPrecioyMargen()
        {
            string costoText = costoc.Text;

            string precioText = precioc.Text;
            string margenText = margenc.Text;

            string precio1Text = P1.Text;
            string margen1Text = M1.Text;

            string precio2Text = P2.Text;
            string margen2Text = M2.Text;

            decimal.TryParse(costoText, out decimal costo);
            decimal.TryParse(precioText, out decimal precio);
            decimal.TryParse(margenText, out decimal margen);
            decimal.TryParse(precio1Text, out decimal precio1);
            decimal.TryParse(margen1Text, out decimal margen1);
            decimal.TryParse(precio2Text, out decimal precio2);
            decimal.TryParse(margen2Text, out decimal margen2);

            if (string.IsNullOrWhiteSpace(precioText) || precio == 0)
            {
                if (string.IsNullOrWhiteSpace(margenText) || margen == 0)
                {
                    precioc.Text = "0.00";
                    margenc.Text = "0.00";
                }
                else
                {
                    decimal precioN = costo / (1 - (margen * 0.01m));
                    precioc.Text = precioN.ToString("F2");
                }
            }
            else
            {
                decimal margenN = ((precio - costo) / (precio * 0.01m));
                if (margenN > 99)
                {
                    margenc.Text = "99.99";
                }
                else if (margenN < -99)
                {
                    margenc.Text = "-99.99";
                }
                else
                {
                    margenc.Text = margenN.ToString("F2");
                }
            }

            if (string.IsNullOrWhiteSpace(precio1Text) || precio1 == 0)
            {
                if (string.IsNullOrWhiteSpace(margen1Text) || margen1 == 0)
                {
                    P1.Text = "0.00";
                    M1.Text = "0.00";
                }
                else
                {
                    decimal precio1N = costo / (1 - (margen1 * 0.01m));
                    P1.Text = precio1N.ToString("F2");
                }
            }
            else
            {
                decimal margen1N = ((precio1 - costo) / (precio1 * 0.01m));
                if (margen1N > 99)
                {
                    M1.Text = "99.99";
                }
                else if (margen1N < -99)
                {
                    M1.Text = "-99.99";
                }
                else
                {
                    M1.Text = margen1N.ToString("F2");
                }
            }

            if (string.IsNullOrWhiteSpace(precio2Text) || precio2 == 0)
            {
                if (string.IsNullOrWhiteSpace(margen2Text) || margen2 == 0)
                {
                    P2.Text = "0.00";
                    M2.Text = "0.00";
                }
                else
                {
                    decimal precio2N = costo / (1 - (margen2 * 0.01m));
                    P2.Text = precio2N.ToString("F2");
                }
            }
            else
            {
                decimal margen2N = ((precio2 - costo) / (precio2 * 0.01m));
                if (margen2N > 99)
                {
                    M2.Text = "99.99";
                }
                else if (margen2N < -99)
                {
                    M2.Text = "-99.99";
                }
                else
                {
                    M2.Text = margen2N.ToString("F2");
                }
            }
        }


        public void CalcularPrecio()
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();

            string costoText = costoc.Text;
            string precioText = precioc.Text;
            string precio1Text = P1.Text;
            string precio2Text = P2.Text;

            decimal.TryParse(costoText, out decimal costo);

            CalcularYActualizarMargen(costo, precioText, margenc);
            CalcularYActualizarMargen(costo, precio1Text, M1);
            CalcularYActualizarMargen(costo, precio2Text, M2);
        }

        private void CalcularYActualizarMargen(decimal costo, string precioText, Entry margenEntry)
        {
            if (string.IsNullOrEmpty(precioText) || precioText == "0")
            {
                margenEntry.Text = "0.00";
            }
            else if (decimal.TryParse(precioText, out decimal precio) && precio != 0)
            {
                decimal margen = ((precio - costo) / (precio * 0.01m));
                if (margen > 99)
                {
                    margenEntry.Text = "99.99";
                }
                else if (margen < -99)
                {
                    margenEntry.Text = "-99.99";
                }
                else
                {
                    margenEntry.Text = margen.ToString("F2");
                }
            }
            else
            {
                margenEntry.Text = "0.00";
            }
        }

        public void CalcularMargen()
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();

            string costoText = costoc.Text;
            string margenText = margenc.Text;
            string margen1Text = M1.Text;
            string margen2Text = M2.Text;

            decimal.TryParse(costoText, out decimal costo);

            CalcularYActualizarPrecio(costo, margenText, precioc);
            CalcularYActualizarPrecio(costo, margen1Text, P1);
            CalcularYActualizarPrecio(costo, margen2Text, P2);
        }

        private void CalcularYActualizarPrecio(decimal costo, string margenText, Entry precioEntry)
        {
            if (string.IsNullOrEmpty(margenText) || margenText == "0")
            {
                precioEntry.Text = "0.00";
            }
            else if (decimal.TryParse(margenText, out decimal margen) && margen != 0)
            {
                decimal precio = costo / (1 - (margen * 0.01m));
                precioEntry.Text = precio.ToString("F2");
            }
            else
            {
                precioEntry.Text = "0.00";
            }
        }

        public void Precioc_Completed(object sender, EventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();
            CalcularPrecio();
        }

        private void Margenc_Completed(object sender, EventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();
            CalcularMargen();
        }

        private void Reset_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();
        }

        private void OnPriceTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is Entry entry)
            {
                string newText = entry.Text;

                if (string.IsNullOrWhiteSpace(newText))
                {
                    return;
                }

                if (!IsValidNonNegativeDecimal(newText))
                {
                    entry.TextChanged -= OnPriceTextChanged;
                    entry.Text = e.OldTextValue;
                    entry.TextChanged += OnPriceTextChanged;
                }
            }
        }

        private bool IsValidNonNegativeDecimal(string text)
        {
            if (decimal.TryParse(text, out decimal result))
            {
                int decimalPlaces = BitConverter.GetBytes(decimal.GetBits(result)[3])[2];
                if (decimalPlaces <= 2 && result <= 99999)
                {
                    return result >= 0;
                }
            }
            return false;
        }


        private void OnLevelTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is Entry entry)
            {
                string newText = entry.Text;

                if (string.IsNullOrWhiteSpace(newText))
                {
                    return;
                }

                if (!IsValidNonNegativeInteger(newText))
                {
                    entry.TextChanged -= OnLevelTextChanged;
                    entry.Text = e.OldTextValue;
                    entry.TextChanged += OnLevelTextChanged;
                }
            }
        }

        private bool IsValidNonNegativeInteger(string text)
        {
            if (int.TryParse(text, out int result))
            {
                return result >= 0;
            }
            return false;
        }

        private void OnMarginTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is Entry entry)
            {
                string newText = entry.Text;

                if (string.IsNullOrWhiteSpace(newText))
                {
                    return;
                }

                if (!IsValidMargin(newText))
                {
                    entry.TextChanged -= OnMarginTextChanged;
                    entry.Text = e.OldTextValue;
                    entry.TextChanged += OnMarginTextChanged;
                }
            }
        }

        private bool IsValidMargin(string text)
        {
            if (decimal.TryParse(text, out decimal result))
            {
                int decimalPlaces = BitConverter.GetBytes(decimal.GetBits(result)[3])[2];
                if (decimalPlaces <= 2 && result >= -99.99m && result <= 99.99m)
                {
                    return true;
                }
            }
            return false;
        }
    }
}