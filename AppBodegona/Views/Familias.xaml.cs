using System;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using MySqlConnector;
using AppBodegona.Services;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using Xamarin.Essentials;
using System.Threading.Tasks;
using Rg.Plugins.Popup.Services;

namespace AppBodegona.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Familias : ContentPage
    {
        public ObservableCollection<Familia> Family { get; set; }
        public ObservableCollection<Producto> Productos { get; set; } = new ObservableCollection<Producto>();
        public ObservableCollection<ProductoFamilia> ProductosFamilia { get; set; } = new ObservableCollection<ProductoFamilia>();

        public Familias()
        {
            InitializeComponent();
            UpdateImage2();
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

            Family = new ObservableCollection<Familia>();
            Productos = new ObservableCollection<Producto>();
            ProductosFamilia = new ObservableCollection<ProductoFamilia>();
            ResultadosListView.ItemsSource = Family;
            FamiliaListView.ItemsSource = Productos;
            ProductoListView.ItemsSource = ProductosFamilia;

            var tapGestureRecognizer = new TapGestureRecognizer();
            tapGestureRecognizer.Tapped += (s, e) =>
            {
                AppShell appShell = (AppShell)Application.Current.MainPage;
                appShell.ResetInactivityTimer();
            };
            MainContentView.GestureRecognizers.Add(tapGestureRecognizer);
        }
        public void UpdateImage2()
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
                UpdateImage2();

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
                    NavigationService.DestinationPage = "Familias";
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

        public class Familia
        {
            public string Codigo { get; set; }
            public string Nombre { get; set; }
        }

        private void Cambio_Clicked(object sender, EventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();

            Family.Clear();
            cfamilia.Text = "";
            nfamilia.Text = "";
            ResultadosListView.IsVisible = false;
            Img.IsVisible = true;

            if (cfamilia.IsVisible)
            {
                cfamilia.IsVisible = false;
                nfamilia.IsVisible = true;
            }
            else
            {
                cfamilia.IsVisible = true;
                nfamilia.IsVisible = false;
            }
        }

        private void ListView_ItemSelectedFalse(object sender, SelectedItemChangedEventArgs e)
        {
            ResultadosListView.SelectedItem = null;
            FamiliaListView.SelectedItem = null;
            ProductoListView.SelectedItem = null;
        }
        private void Limpiar_Clicked(object sender, EventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();

            Img.IsVisible = true;
            cfamilia.Text = "";
            nfamilia.Text = "";
            ResultadosListView.IsVisible = false;
        }

        private async void Buscar_Clicked(object sender, EventArgs e)
        {
            var loadingPopup = new LoadingPopup();

            try
            {
                await PopupNavigation.Instance.PushAsync(loadingPopup);

                AppShell appShell = (AppShell)Application.Current.MainPage;
                appShell.ResetInactivityTimer();

                ResultadosListView.IsVisible = true;
                Family.Clear();

                if (!string.IsNullOrEmpty(cfamilia.Text))
                {
                    string entryidText = cfamilia.Text;
                    string query = $"SELECT Id, Nombre FROM gruposproductos WHERE Id = '{entryidText}'";

                    using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                    {
                        connection.Open();
                        using (MySqlCommand command = new MySqlCommand(query, connection))
                        {
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    Family.Add(new Familia
                                    {
                                        Codigo = reader["Id"].ToString(),
                                        Nombre = reader["Nombre"].ToString()
                                    });

                                    Img.IsVisible = false;
                                }
                                else
                                {
                                    await PopupNavigation.Instance.PopAsync();

                                    var popup = new DynamicPopup(
                                        "Alerta",
                                        "No se encontró la familia.",
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
                else if (!string.IsNullOrEmpty(nfamilia.Text))
                {
                    string entryDescText = nfamilia.Text;
                    string[] searchTerms = entryDescText.Split(' ');
                    string query = "SELECT Id, Nombre FROM gruposproductos WHERE ";

                    for (int i = 0; i < searchTerms.Length; i++)
                    {
                        query += $"Nombre LIKE @searchTerm{i}";
                        if (i < searchTerms.Length - 1)
                        {
                            query += " AND ";
                        }
                    }
                    query += " ORDER BY Id Asc";

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
                                Family.Clear();
                                while (reader.Read())
                                {
                                    Family.Add(new Familia
                                    {
                                        Codigo = reader["Id"].ToString(),
                                        Nombre = reader["Nombre"].ToString()
                                    });
                                    Img.IsVisible = false;
                                }
                                if (Family.Count == 0)
                                {
                                    await PopupNavigation.Instance.PopAsync();

                                    var popup = new DynamicPopup(
                                        "Alerta",
                                        "No se encontraron familias con esa descripción.",
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
                if (PopupNavigation.Instance.PopupStack.Contains(loadingPopup))
                {
                    await PopupNavigation.Instance.PopAsync();
                }
            }
        }

        private async void Todo_Clicked(object sender, EventArgs e)
        {
            var loadingPopup = new LoadingPopup();

            try
            {
                await PopupNavigation.Instance.PushAsync(loadingPopup);

                AppShell appShell = (AppShell)Application.Current.MainPage;
                appShell.ResetInactivityTimer();

                ResultadosListView.IsVisible = true;
                Img.IsVisible = false;

                string query = "SELECT Id, Nombre FROM gruposproductos ORDER BY Id Asc";

                using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                {
                    connection.Open();
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            Family.Clear();
                            while (reader.Read())
                            {
                                Family.Add(new Familia
                                {
                                    Codigo = reader["Id"].ToString(),
                                    Nombre = reader["Nombre"].ToString()
                                });
                            }

                            if (Family.Count == 0)
                            {
                                await PopupNavigation.Instance.PopAsync();

                                var popup = new DynamicPopup(
                                    "Alerta",
                                    "No se encontraron familias.",
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

        public class Producto
        {
            public string Upc { get; set; }
            public string DescLarga { get; set; }
            public string Costo { get; set; }
            public string Existencia { get; set; }
        }

        public class ProductoFamilia
        {
            public string Upc { get; set; }
            public string DescLarga { get; set; }
            public string Costo { get; set; }
            public string Existencia { get; set; }
        }

        private async void EditarFamilia(object sender, EventArgs e)
        {
            var loadingPopup = new LoadingPopup();

            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();

            await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(loadingPopup);

            if (sender is Button button && button.BindingContext is Familia familia)
            {
                string id = familia.Codigo;
                var familiaFiltrada = Family.FirstOrDefault(f => f.Codigo == id);
                if (familiaFiltrada != null)
                {
                }

                try
                {
                    string query = $"SELECT Id, Nombre FROM gruposproductos WHERE Id = @Id";
                    using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                    {
                        connection.Open();
                        using (MySqlCommand command = new MySqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@Id", id);
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    string Id = reader["Id"].ToString();
                                    string nombreString = reader["Nombre"].ToString();

                                    codigof.Text = Id;
                                    descf.Text = nombreString;
                                }
                            }
                        }
                    }

                    string query1 = $"SELECT Upc, DescLarga, Costo FROM productos WHERE GruposProductosId = '{id}'";
                    using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                    {
                        connection.Open();
                        using (MySqlCommand command = new MySqlCommand(query1, connection))
                        {
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync();
                                Productos.Clear();
                                while (reader.Read())
                                {
                                    Productos.Add(new Producto
                                    {
                                        Upc = reader["Upc"].ToString(),
                                        DescLarga = reader["DescLarga"].ToString(),
                                        Costo = Convert.ToDecimal(reader["Costo"]).ToString("F2")
                                    });
                                    Img.IsVisible = false;
                                    VistaBusqueda.IsVisible = false;
                                    VistaFamilia.IsVisible = true;
                                }
                                if (Productos.Count == 0)
                                {
                                    var popup = new DynamicPopup(
                                    "Alerta",
                                    "La familia se encuentra vacia.",
                                    new Dictionary<string, Action>
                                    {
                                        { "Aceptar", () => {} }
                                    });

                                    await PopupNavigation.Instance.PushAsync(popup);

                                    Img.IsVisible = false;
                                    VistaBusqueda.IsVisible = false;
                                    VistaFamilia.IsVisible = true;
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

        private async void EditarF_Clicked(object sender, EventArgs e)
        {

            try
            {
                EditarFamilia(sender, e);
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
        }

        private async void EliminarF_Clicked(object sender, EventArgs e)
        {
            var loadingPopup = new LoadingPopup();

            try
            {
                await PopupNavigation.Instance.PushAsync(loadingPopup);

                AppShell appShell = (AppShell)Application.Current.MainPage;
                appShell.ResetInactivityTimer();

                if (sender is Button button && button.BindingContext is Producto producto)
                {
                    string upc = producto.Upc;
                    var productoFiltrado = Productos.FirstOrDefault(p => p.Upc == upc);

                    if (productoFiltrado != null)
                    {
                        var tcs = new TaskCompletionSource<bool>();

                        var popupConfirmacion = new DynamicPopup(
                            "Advertencia",
                            "¿Desea eliminar el producto de la familia?",
                            new Dictionary<string, Action>
                            {
                                { "Si", () => tcs.SetResult(true) },
                                { "No", () => tcs.SetResult(false) }
                            });

                        await PopupNavigation.Instance.PushAsync(popupConfirmacion);
                        bool confirmacion = await tcs.Task;

                        if (!confirmacion)
                        {
                            await PopupNavigation.Instance.PopAsync();
                            return;
                        }

                        try
                        {
                            string query = $"UPDATE productos SET GruposProductosId = '0' WHERE Upc = '{upc}'";

                            using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                            {
                                connection.Open();
                                using (MySqlCommand command = new MySqlCommand(query, connection))
                                {
                                    int result = command.ExecuteNonQuery();

                                    await PopupNavigation.Instance.PopAsync();

                                    if (result > 0)
                                    {
                                        Productos.Remove(productoFiltrado);

                                        var popupExito = new DynamicPopup(
                                            "Éxito",
                                            "Producto eliminado correctamente",
                                            new Dictionary<string, Action>
                                            {
                                                { "OK", () => {} }
                                            });

                                        await PopupNavigation.Instance.PushAsync(popupExito);
                                    }
                                    else
                                    {
                                        var popupError = new DynamicPopup(
                                            "Error",
                                            "No se pudo eliminar el producto",
                                            new Dictionary<string, Action>
                                            {
                                                { "OK", () => {} }
                                            });

                                        await PopupNavigation.Instance.PushAsync(popupError);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            await PopupNavigation.Instance.PopAsync();

                            var popupErrorConexion = new DynamicPopup(
                                "Error de Conexión",
                                $"Error al intentar conectar a la base de datos: {ex.Message}",
                                new Dictionary<string, Action>
                                {
                                    { "OK", () => {} }
                                });

                            await PopupNavigation.Instance.PushAsync(popupErrorConexion);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await PopupNavigation.Instance.PopAsync();

                var popupErrorGeneral = new DynamicPopup(
                    "Error",
                    $"Se produjo un error inesperado: {ex.Message}",
                    new Dictionary<string, Action>
                    {
                        { "OK", () => {} }
                    });

                await PopupNavigation.Instance.PushAsync(popupErrorGeneral);
            }
            finally
            {
                if (PopupNavigation.Instance.PopupStack.Contains(loadingPopup))
                {
                    await PopupNavigation.Instance.PopAsync();
                }
            }
        }

        private void Regresar_Clicked(object sender, EventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();

            VistaFamilia.IsVisible = false;
            VistaBusqueda.IsVisible = true;
            Img.IsVisible = false;
        }

        private void Agregar_Clicked(object sender, EventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();

            VistaAgregarProducto.IsVisible = true;
            VistaFamilia.IsVisible = false;
            Img.IsVisible = true;
            ProductoListView.IsVisible = false;

        }

        private void CambioTextProduc_Clicked(object sender, EventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();

            ProductosFamilia.Clear();
            descproduc.Text = "";
            upcproduc.Text = "";
            ProductoListView.IsVisible = false;

            if (descproduc.IsVisible)
            {
                descproduc.IsVisible = false;
                upcproduc.IsVisible = true;
            }
            else
            {
                descproduc.IsVisible = true;
                upcproduc.IsVisible = false;
            }
        }

        private async void BuscarProduc(object sender, EventArgs e)
        {
            var loadingPopup = new LoadingPopup();

            try
            {
                await PopupNavigation.Instance.PushAsync(loadingPopup);

                if (!string.IsNullOrEmpty(upcproduc.Text))
                {
                    Img.IsVisible = false;
                    string entryupcText = upcproduc.Text;

                    if (entryupcText.Length < 13)
                    {
                        int cerosToAdd = 13 - entryupcText.Length;
                        entryupcText = new string('0', cerosToAdd) + entryupcText;
                    }

                    string query = $"SELECT Upc, Costo, DescLarga, Existencia FROM productos WHERE Upc = '{entryupcText}' AND GruposProductosId = '0'";

                    using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                    {
                        connection.Open();
                        using (MySqlCommand command = new MySqlCommand(query, connection))
                        {
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    ProductosFamilia.Add(new ProductoFamilia
                                    {
                                        Upc = reader["Upc"].ToString(),
                                        DescLarga = reader["DescLarga"].ToString(),
                                        Existencia = reader["Existencia"].ToString(),
                                        Costo = reader["Costo"].ToString()
                                    });
                                }
                                else
                                {
                                    await PopupNavigation.Instance.PopAsync();

                                    var popup = new DynamicPopup(
                                        "Alerta",
                                        "No se encontraron productos con ese UPC o ya pertenece a una familia.",
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
                else if (!string.IsNullOrEmpty(descproduc.Text))
                {
                    Img.IsVisible = false;
                    string entryDescText = descproduc.Text;
                    string[] searchTerms = entryDescText.Split(' ');
                    string query = "SELECT Upc, Costo, DescLarga, Existencia FROM productos WHERE ";

                    for (int i = 0; i < searchTerms.Length; i++)
                    {
                        query += $"DescLarga LIKE @searchTerm{i} AND ";
                    }
                    query += "GruposProductosId = '0'";
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
                                ProductosFamilia.Clear();
                                while (reader.Read())
                                {
                                    ProductosFamilia.Add(new ProductoFamilia
                                    {
                                        Upc = reader["Upc"].ToString(),
                                        DescLarga = reader["DescLarga"].ToString(),
                                        Existencia = reader["Existencia"].ToString(),
                                        Costo = reader["Costo"].ToString()
                                    });
                                }
                                if (ProductosFamilia.Count == 0)
                                {
                                    await PopupNavigation.Instance.PopAsync();

                                    var popup = new DynamicPopup(
                                        "Alerta",
                                        "No se encontraron productos con esa descripción o ya pertenecen a una familia.",
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
                    $"Error al intentar conectar a la base de datos: {ex.Message}",
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

        private void BuscarProduc_Clicked(object sender, EventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();

            ProductoListView.IsVisible = true;
            ProductosFamilia.Clear();
            BuscarProduc(sender, e);
        }

        private async void EscanearProducto_Clicked(object sender, EventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();

            upcproduc.Text = null;
            descproduc.Text = null;
            ProductosFamilia.Clear();

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
                    upcproduc.Text = result.Text;

                    BuscarProduc(sender, e);

                    ProductoListView.IsVisible = true;

                }
                else
                {
                    upcproduc.Text = null;
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

        private void LimpiarProduc_Clicked(object sender, EventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();

            ProductosFamilia.Clear();
            descproduc.Text = "";
            upcproduc.Text = "";
            ProductoListView.IsVisible = false;
            Img.IsVisible = true;
        }

        private async void AgregarFamilia_Clicked(object sender, EventArgs e)
        {
            var loadingPopup = new LoadingPopup(); 

            try
            {
                if (!Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopupStack.Contains(loadingPopup))
                {
                    await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(loadingPopup);
                }

                AppShell appShell = (AppShell)Application.Current.MainPage;
                appShell.ResetInactivityTimer();

                if (sender is Button button && button.BindingContext is ProductoFamilia productoFamilia)
                {
                    string upc = productoFamilia.Upc;
                    string familiaId = codigof.Text;

                    var productoFamiliaFiltrado = ProductosFamilia.FirstOrDefault(p => p.Upc == upc);
                    if (productoFamiliaFiltrado != null)
                    {
                        await CerrarPopupSiEstaAbierto();
                        var tcs = new TaskCompletionSource<bool>();

                        var popupConfirmacion = new DynamicPopup(
                            "Confirmar",
                            $"¿Desea agregar el producto a la familia?\n\nUPC: {upc}\nDescripción: {productoFamiliaFiltrado.DescLarga}",
                            new Dictionary<string, Action>
                            {
                                { "Si", () => tcs.SetResult(true) },
                                { "No", () => tcs.SetResult(false) }
                            });

                        await PopupNavigation.Instance.PushAsync(popupConfirmacion);
                        bool continueWithNegativeMargins = await tcs.Task;

                        if (!continueWithNegativeMargins)
                        {
                            return;
                        }

                        await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(loadingPopup);

                        string updateProductValuesQuery = @"
                                                          UPDATE productos 
                                                          SET 
                                                          Costo = @Costo, 
                                                          Precio = @PrecioNormal, 
                                                          Nivel1 = @Nivel1, 
                                                          PrecioMaxNivel1 = @PrecioMaxNivel1, 
                                                          Nivel2 = @Nivel2, 
                                                          PrecioMaxNivel2 = @PrecioMaxNivel2 
                                                          WHERE Upc = @Upc";

                        string insertHistorialQuery = @"
                                                      INSERT INTO historialcambios (IdUsuarios, Usuario, Fecha, FechaHora, Upc, TipoCambio, DoubleAnt, DoubleAct) 
                                                      VALUES (@IdUsuario, @Usuario, @Fecha, @FechaHora, @Upc, @TipoCambio, @DoubleAnt, @DoubleAct)";

                        string updateProductQuery = $"UPDATE productos SET GruposProductosId = @FamiliaId WHERE Upc = @Upc";
                        string productoQuery = $"SELECT Precio, Costo, Nivel1, PrecioMaxNivel1, Nivel2, PrecioMaxNivel2, Nivel3, PrecioMaxNivel3 FROM productos WHERE Upc = @Upc";
                        string familiaQuery = $"SELECT PrecioNormal, Costo, Nivel1, PrecioMaxNivel1, Nivel2, PrecioMaxNivel2, Nivel3, PrecioMaxNivel3 FROM gruposproductos WHERE ID = @FamiliaId";

                        using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                        {
                            connection.Open();

                            using (MySqlTransaction transaction = connection.BeginTransaction())
                            {
                                try
                                {
                                    var productoDatos = new Dictionary<string, double>();
                                    using (MySqlCommand productoCommand = new MySqlCommand(productoQuery, connection, transaction))
                                    {
                                        productoCommand.Parameters.AddWithValue("@Upc", upc);
                                        using (MySqlDataReader productoReader = productoCommand.ExecuteReader())
                                        {
                                            if (productoReader.Read())
                                            {
                                                productoDatos["Precio"] = Convert.ToDouble(productoReader["Precio"]);
                                                productoDatos["Costo"] = Convert.ToDouble(productoReader["Costo"]);
                                                productoDatos["Nivel1"] = Convert.ToDouble(productoReader["Nivel1"]);
                                                productoDatos["PrecioMaxNivel1"] = Convert.ToDouble(productoReader["PrecioMaxNivel1"]);
                                                productoDatos["Nivel2"] = Convert.ToDouble(productoReader["Nivel2"]);
                                                productoDatos["PrecioMaxNivel2"] = Convert.ToDouble(productoReader["PrecioMaxNivel2"]);
                                                productoDatos["Nivel3"] = Convert.ToDouble(productoReader["Nivel3"]);
                                                productoDatos["PrecioMaxNivel3"] = Convert.ToDouble(productoReader["PrecioMaxNivel3"]);
                                            }
                                        }
                                    }

                                    var familiaDatos = new Dictionary<string, double>();
                                    using (MySqlCommand familiaCommand = new MySqlCommand(familiaQuery, connection, transaction))
                                    {
                                        familiaCommand.Parameters.AddWithValue("@FamiliaId", familiaId);
                                        using (MySqlDataReader familiaReader = familiaCommand.ExecuteReader())
                                        {
                                            if (familiaReader.Read())
                                            {
                                                familiaDatos["PrecioNormal"] = Convert.ToDouble(familiaReader["PrecioNormal"]);
                                                familiaDatos["Costo"] = Convert.ToDouble(familiaReader["Costo"]);
                                                familiaDatos["Nivel1"] = Convert.ToDouble(familiaReader["Nivel1"]);
                                                familiaDatos["PrecioMaxNivel1"] = Convert.ToDouble(familiaReader["PrecioMaxNivel1"]);
                                                familiaDatos["Nivel2"] = Convert.ToDouble(familiaReader["Nivel2"]);
                                                familiaDatos["PrecioMaxNivel2"] = Convert.ToDouble(familiaReader["PrecioMaxNivel2"]);
                                                familiaDatos["Nivel3"] = Convert.ToDouble(familiaReader["Nivel3"]);
                                                familiaDatos["PrecioMaxNivel3"] = Convert.ToDouble(familiaReader["PrecioMaxNivel3"]);
                                            }
                                        }
                                    }

                                    var diferencias = new List<(int TipoCambio, double ValorAnterior, double ValorNuevo)>();
                                    if (productoDatos["Costo"] != familiaDatos["Costo"])
                                        diferencias.Add((12, productoDatos["Costo"], familiaDatos["Costo"]));

                                    if (productoDatos["Precio"] != familiaDatos["PrecioNormal"])
                                        diferencias.Add((13, productoDatos["Precio"], familiaDatos["PrecioNormal"]));

                                    if (productoDatos["Nivel1"] != familiaDatos["Nivel1"])
                                        diferencias.Add((14, productoDatos["Nivel1"], familiaDatos["Nivel1"]));

                                    if (productoDatos["PrecioMaxNivel1"] != familiaDatos["PrecioMaxNivel1"])
                                        diferencias.Add((15, productoDatos["PrecioMaxNivel1"], familiaDatos["PrecioMaxNivel1"]));

                                    if (productoDatos["Nivel2"] != familiaDatos["Nivel2"])
                                        diferencias.Add((16, productoDatos["Nivel2"], familiaDatos["Nivel2"]));

                                    if (productoDatos["PrecioMaxNivel2"] != familiaDatos["PrecioMaxNivel2"])
                                        diferencias.Add((17, productoDatos["PrecioMaxNivel2"], familiaDatos["PrecioMaxNivel2"]));

                                    using (MySqlCommand updateValuesCommand = new MySqlCommand(updateProductValuesQuery, connection, transaction))
                                    {
                                        updateValuesCommand.Parameters.AddWithValue("@Costo", familiaDatos["Costo"]);
                                        updateValuesCommand.Parameters.AddWithValue("@PrecioNormal", familiaDatos["PrecioNormal"]);
                                        updateValuesCommand.Parameters.AddWithValue("@Nivel1", familiaDatos["Nivel1"]);
                                        updateValuesCommand.Parameters.AddWithValue("@PrecioMaxNivel1", familiaDatos["PrecioMaxNivel1"]);
                                        updateValuesCommand.Parameters.AddWithValue("@Nivel2", familiaDatos["Nivel2"]);
                                        updateValuesCommand.Parameters.AddWithValue("@PrecioMaxNivel2", familiaDatos["PrecioMaxNivel2"]);
                                        updateValuesCommand.Parameters.AddWithValue("@Upc", upc);
                                        updateValuesCommand.ExecuteNonQuery();
                                    }

                                    foreach (var (TipoCambio, ValorAnterior, ValorNuevo) in diferencias)
                                    {
                                        using (MySqlCommand historialCommand = new MySqlCommand(insertHistorialQuery, connection, transaction))
                                        {
                                            historialCommand.Parameters.AddWithValue("@IdUsuario", appShell.Id);
                                            historialCommand.Parameters.AddWithValue("@Usuario", appShell.Usuario);
                                            historialCommand.Parameters.AddWithValue("@Fecha", DateTime.Now.Date);
                                            historialCommand.Parameters.AddWithValue("@FechaHora", DateTime.Now);
                                            historialCommand.Parameters.AddWithValue("@Upc", upc);
                                            historialCommand.Parameters.AddWithValue("@TipoCambio", TipoCambio);
                                            historialCommand.Parameters.AddWithValue("@DoubleAnt", ValorAnterior);
                                            historialCommand.Parameters.AddWithValue("@DoubleAct", ValorNuevo);
                                            historialCommand.ExecuteNonQuery();
                                        }
                                    }

                                    using (MySqlCommand updateCommand = new MySqlCommand(updateProductQuery, connection, transaction))
                                    {
                                        updateCommand.Parameters.AddWithValue("@FamiliaId", familiaId);
                                        updateCommand.Parameters.AddWithValue("@Upc", upc);
                                        updateCommand.ExecuteNonQuery();
                                    }

                                    Productos.Add(new Producto
                                    {
                                        Upc = upc,
                                        DescLarga = productoFamiliaFiltrado.DescLarga,
                                        Costo = familiaDatos["Costo"].ToString("F2") 
                                    });

                                    transaction.Commit();

                                    VistaAgregarProducto.IsVisible = false;
                                    VistaFamilia.IsVisible = true;

                                    await CerrarPopupSiEstaAbierto();
                                    var popup = new DynamicPopup(
                                    "Éxito",
                                    "Producto agregado a la familia.",
                                    new Dictionary<string, Action>
                                    {
                                        { "OK", () => {} }
                                    });

                                    await PopupNavigation.Instance.PushAsync(popup);

                                }
                                catch (Exception ex)
                                {
                                    transaction.Rollback();
                                    await CerrarPopupSiEstaAbierto();
                                    var popup = new DynamicPopup(
                                    "Error",
                                    $"Error: {ex.Message}",
                                    new Dictionary<string, Action>
                                    {
                                        { "OK", () => {} }
                                    });

                                    await PopupNavigation.Instance.PushAsync(popup);

                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await CerrarPopupSiEstaAbierto();
                var popup = new DynamicPopup(
                "Error",
                $"Error: {ex.Message}",
                new Dictionary<string, Action>
                {
                    { "OK", () => {} }
                });

                await PopupNavigation.Instance.PushAsync(popup);

            }
        }

        private async Task CerrarPopupSiEstaAbierto()
        {
            if (Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopupStack.Count > 0)
            {
                await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync();
            }
        }


        private void Volver_Clicked(object sender, EventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();

            VistaAgregarProducto.IsVisible = false;
            Img.IsVisible = false;
            VistaFamilia.IsVisible = true;
        }

        private void Reset_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();
        }
        private void Button_Focused(object sender, FocusEventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();

            if (sender is Entry entry)
            {
                // Selecciona todo el texto
                Device.BeginInvokeOnMainThread(() => entry.CursorPosition = 0);
                if (entry.Text != null)
                {
                    Device.BeginInvokeOnMainThread(() => entry.SelectionLength = entry.Text.Length);
                }
            }
        }
    }
}