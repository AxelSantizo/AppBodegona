using AppBodegona.Services;
using MySqlConnector;
using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System.Linq;
using System.Globalization;
using Xamarin.Essentials;
using System.Collections.ObjectModel;
using Rg.Plugins.Popup.Services;
using System.Threading.Tasks;

namespace AppBodegona.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Recepcion : ContentPage
    {
        private readonly List<Proveedores> proveedor = new List<Proveedores>(); 
        private List<DetalleScann> detalle = new List<DetalleScann>();
        public ObservableCollection<Producto> Productos { get; set; }
        public Recepcion()
        {
            InitializeComponent();
            VerificarConexiones();
            CargarRazonSocial();
            CargarSucursal();

            var cultura = new CultureInfo("es-ES");
            CultureInfo.DefaultThreadCurrentCulture = cultura;
            CultureInfo.DefaultThreadCurrentUICulture = cultura;

            Productos = new ObservableCollection<Producto>();
            ListViewProducto.ItemsSource = Productos;

            var tapGestureRecognizer = new TapGestureRecognizer();
            tapGestureRecognizer.Tapped += (s, e) =>
            {
                AppShell appShell = (AppShell)Application.Current.MainPage;
                appShell.ResetInactivityTimer();
            };
        }

        private void VerificarConexiones()
        {
            var conexiones = new Dictionary<string, string>
            {
                { "Sucursal", DatabaseConnection.ConnectionString },
                { "Central", DatabaseConnection.ConnectionCentral },
                { "Nexus", DatabaseConnection.ConnectionNexus }
            };

            foreach (var conexion in conexiones)
            {
                try
                {
                    bool isConnected = DatabaseConnection.TestConnection(conexion.Value);
                    if (!isConnected)
                    {
                        MostrarPopupError($"No se pudo conectar a la base de datos de {conexion.Key}.");
                    }
                }
                catch (Exception ex)
                {
                    MostrarPopupError($"Error al intentar conectar a la base de datos de {conexion.Key}: {ex.Message}");
                }
            }
        }

        private async void MostrarPopupError(string mensaje)
        {
            await PopupNavigation.Instance.PushAsync(new DynamicPopup(
                "Error de Conexión",
                mensaje,
                new Dictionary<string, Action>
                {
            { "Aceptar", () => { } }
                }
            ));
        }

        protected override bool OnBackButtonPressed()
        {
            if (Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopupStack.Count > 0)
            {
                return base.OnBackButtonPressed();
            }

            Device.BeginInvokeOnMainThread(async () =>
            {
                await PopupNavigation.Instance.PushAsync(new DynamicPopup(
                    "Confirmar",
                    "¿Desea cerrar la aplicacion?",
                    new Dictionary<string, Action>
                    {
                { "Sí", () => System.Diagnostics.Process.GetCurrentProcess().CloseMainWindow() },
                { "No", () => { } }
                    }
                ));
            });

            return true;
        }

        protected async override void OnAppearing()
        {
            ProveedoresVista.IsVisible = false;
            viewSearchProducto.IsVisible = false;
            base.OnAppearing();
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();

            if (string.IsNullOrEmpty(appShell.Usuario))
            {
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
                    NavigationService.DestinationPage = "Recepcion";
                    await Shell.Current.GoToAsync("Login");
                }
            }
            else
            {
                if (appShell.IdNivel != "5" && appShell.IdNivel != "11")
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

        private void FocusedText(object sender, FocusEventArgs e)
        {
            if (sender is Entry entry)
            {
                Device.BeginInvokeOnMainThread(() => entry.CursorPosition = 0);
                if (entry.Text != null)
                {
                    Device.BeginInvokeOnMainThread(() => entry.SelectionLength = entry.Text.Length);
                }
            }
        }

        public class Proveedores
        {
            public int IdProveedor { get; set; }
            public string NombreProveedor { get; set; }
            public string Nit { get; set; }
        }

        public class RazonesSociales
        {
            public string Id { get; set; }
            public string NombreRazon { get; set; }
            public string Nit { get; set; }

            public override string ToString()
            {
                return $"{NombreRazon} ({Nit})";
            }
        }

        public class DetalleScann
        {
            public string UPC { get; set; }
            public string Descripcion { get; set; }
            public int Cantidad { get; set; }
            public int Bonificacion { get; set; }
            public string Vencimiento { get; set; }
            public int Id { get; set; }

        }

        public class Producto
        {
            public string Upc { get; set; }
            public string DescLarga { get; set; }
            public string Existencia { get; set; }
        }

        int ConfirmProveedor = 0;
        int ConfirmNIT = 0;
        int ConfirmSerie = 0;
        int ConfirmNumero = 0;
        int ConfirmMonto = 0;
        int ConfirmFecha = 0;
        int ConfirmRazon = 0; 
        int idInventarioGenerado = 0;
        int idFacturaGenerada = 0;

        int idUsuario;
        string NombreUsuario;
        string Usuario;
        int idSucursal;
        string nombreSucursal;

        private void CargarSucursal()
        {
            string query = "SELECT Nombre, IdSucursal FROM sucursales WHERE Local = 1 GROUP BY IdSucursal";

            using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
            {
                connection.Open();
                try
                {
                    MySqlCommand command = new MySqlCommand(query, connection);
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            idSucursal = Convert.ToInt32(reader["IdSucursal"]);
                            nombreSucursal = reader["Nombre"].ToString();

                            Console.WriteLine($"[DEBUG] Sucursal cargada - ID: {idSucursal}, Nombre: {nombreSucursal}");
                        }
                        else
                        {
                            Device.BeginInvokeOnMainThread(() =>
                            {
                                PopupNavigation.Instance.PushAsync(new DynamicPopup(
                                    "Información",
                                    "No se encontraron sucursales disponibles.",
                                    new Dictionary<string, Action>
                                    {
                                        { "Aceptar", () => { } }
                                    }
                                ));
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        PopupNavigation.Instance.PushAsync(new DynamicPopup(
                            "Error",
                            $"No se pudo obtener los datos: {ex.Message}",
                            new Dictionary<string, Action>
                            {
                                { "Aceptar", () => { } }
                            }
                        ));
                    });
                }
            }

            Console.WriteLine($"[DEBUG] Datos de Sucursal: ID = {idSucursal}, Nombre = {nombreSucursal}");
        }


        private void RegresarProveedor_Clicked(object sender, EventArgs e)
        {
            RegresarProveedor.IsVisible = false;
            VistaDatos.IsVisible = true;
            ProveedoresVista.IsVisible = false;
            SaveDetail.IsVisible = true;
            Proveedor.Text = string.Empty;
            NITProveedor.Text = string.Empty;
        }

        private void LoadProveedor()
        {
            string query = "SELECT Id, Nombre, Nit FROM proveedores_facturas ORDER BY Nombre ASC";
            proveedor.Clear();
            using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
            {
                connection.Open();
                try
                {
                    MySqlCommand command = new MySqlCommand(query, connection);
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int idProveedor = Convert.ToInt32(reader["Id"]);
                            string nombreProveedor = reader["Nombre"].ToString();
                            string nitProveedor = reader["Nit"].ToString();
                            proveedor.Add(new Proveedores { IdProveedor = idProveedor, NombreProveedor = nombreProveedor, Nit = nitProveedor });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        PopupNavigation.Instance.PushAsync(new DynamicPopup(
                            "Error",
                            $"No se pudo obtener los datos: {ex.Message}",
                            new Dictionary<string, Action>
                            {
                                { "Aceptar", () => { } }
                            }
                        ));
                    });
                }
            }
            ListViewProveedor.ItemsSource = proveedor;
            ListViewProveedor.ItemSelected += ListViewProveedor_ItemSelected;
        }

        private void ListViewProveedor_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem == null)
                return;

            var selectedProveedor = (Proveedores)e.SelectedItem;
            Proveedor.Text = $"{selectedProveedor.NombreProveedor}";
            NITProveedor.Text = $"{selectedProveedor.Nit}";
            IdProveedor.Text = $"{selectedProveedor.IdProveedor}";

            ((ListView)sender).SelectedItem = null;

            if (!string.IsNullOrEmpty(Proveedor.Text))
            {
                CheckProveedor.IsVisible = true;
                CheckProveedor.BackgroundColor = Color.FromHex("#2B2B2B");
                CheckProveedor.BorderColor = Color.FromHex("#ff4141");
                CheckProveedor.BorderWidth = 1;
                CheckProveedor.Text = "✔";
                CheckProveedor.TextColor = Color.FromHex("#ff4141");
                CheckProveedor.IsEnabled = true;
            }
            else
            {
                CheckProveedor.IsVisible = false;
            }

            SaveDetail.IsVisible = true;
            RegresarProveedor.IsVisible = false;
            ProveedoresVista.IsVisible = false;
            VistaDatos.IsVisible = true;
            Proveedor.Unfocus();
        }

        private void FiltroProveedor_Focused(object sender, FocusEventArgs e)
        {
            ProveedoresVista.IsVisible = true;
            VistaDatos.IsVisible = false;
            SaveDetail.IsVisible = false;
            RegresarProveedor.IsVisible = true;
            proveedor.Clear();
            if (!DatabaseConnection.TestConnection(DatabaseConnection.ConnectionCentral))
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    PopupNavigation.Instance.PushAsync(new DynamicPopup(
                        "Error de Conexión",
                        "No se pudo conectar a la base de datos.",
                        new Dictionary<string, Action>
                        {
                { "Aceptar", () => { } }
                        }
                    ));
                });
                return;
            }
            else
            {
                LoadProveedor();
                if (sender is Entry entry)
                {
                    Device.BeginInvokeOnMainThread(() => entry.CursorPosition = 0);
                    if (entry.Text != null)
                    {
                        Device.BeginInvokeOnMainThread(() => entry.SelectionLength = entry.Text.Length);
                    }
                }
                ListViewProveedor.IsVisible = true;
            }
        }

        private void FiltroProveedor_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filtro = Proveedor.Text.ToLower();
            if (filtro.Length >= 3)
            {
                var palabrasFiltro = filtro.Split(' ');

                List<Proveedores> proveedorFiltrado = proveedor
                    .Where(d => palabrasFiltro.All(palabra => d.NombreProveedor.ToLower().Contains(palabra)))
                    .ToList();

                ListViewProveedor.ItemsSource = proveedorFiltrado;
            }
            else
            {
                ListViewProveedor.ItemsSource = proveedor;
            }

            if (!string.IsNullOrEmpty(Proveedor.Text))
            {
                CheckProveedor.IsVisible = true;
                CheckProveedor.BackgroundColor = Color.FromHex("#2B2B2B");
                CheckProveedor.BorderColor = Color.FromHex("#ff4141");
                CheckProveedor.BorderWidth = 1;
                CheckProveedor.Text = "✔";
                CheckProveedor.TextColor = Color.FromHex("#ff4141");
                CheckProveedor.IsEnabled = true;
            }
            else
            {
                CheckProveedor.IsVisible = false;
            }
        }

        private void NITProveedor_TextChanged(object sender, TextChangedEventArgs e)
        {
            string textoFiltrado = new string(e.NewTextValue.Where(c => char.IsLetterOrDigit(c)).ToArray());

            if (textoFiltrado != e.NewTextValue)
            {
                NITProveedor.Text = textoFiltrado;
            }
        }

        private async void NITProveedor_Completed(object sender, EventArgs e)
        {
            var loadingPopup = new LoadingPopup();

            if (!Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopupStack.Contains(loadingPopup))
            {
                await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(loadingPopup);
            }

            string SearchNIT = NITProveedor.Text;

            if (string.IsNullOrWhiteSpace(SearchNIT))
            {
                if (Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopupStack.Contains(loadingPopup))
                {
                    await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync();
                }

                await PopupNavigation.Instance.PushAsync(new DynamicPopup(
                    "Error",
                    "Por favor, ingrese un NIT válido.",
                    new Dictionary<string, Action>
                    {
                { "Aceptar", () => { } }
                    }
                ));
                return;
            }

            string query = "SELECT Id, Nombre, Nit FROM proveedores WHERE REPLACE(Nit, '-', '') = @SearchNIT";
            proveedor.Clear();

            using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
            {
                try
                {
                    connection.Open();

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@SearchNIT", SearchNIT);

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        bool hasResults = false;

                        while (reader.Read())
                        {
                            hasResults = true;

                            int idProveedor = Convert.ToInt32(reader["Id"]);
                            string nombreProveedor = reader["Nombre"].ToString();
                            string nitProveedor = reader["Nit"].ToString();

                            proveedor.Add(new Proveedores
                            {
                                IdProveedor = idProveedor,
                                NombreProveedor = nombreProveedor,
                                Nit = nitProveedor
                            });

                            Proveedor.Text = nombreProveedor;
                            NITProveedor.Text = nitProveedor;
                            IdProveedor.Text = idProveedor.ToString();

                            CheckProveedor.IsVisible = !string.IsNullOrEmpty(NITProveedor.Text);
                            CheckProveedor.BackgroundColor = Color.FromHex("#2B2B2B");
                            CheckProveedor.BorderColor = string.IsNullOrEmpty(NITProveedor.Text) ? Color.Transparent : Color.FromHex("#ff4141");
                            CheckProveedor.BorderWidth = 1;
                            CheckProveedor.Text = "✔";
                            CheckProveedor.TextColor = Color.FromHex("#ff4141");
                            CheckProveedor.IsEnabled = true;
                        }

                        if (!hasResults)
                        {
                            if (Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopupStack.Contains(loadingPopup))
                            {
                                await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync();
                            }

                            await PopupNavigation.Instance.PushAsync(new DynamicPopup(
                                "Sin resultados",
                                "No se encontró un proveedor con el NIT proporcionado.",
                                new Dictionary<string, Action>
                                {
                            { "Aceptar", () => { } }
                                }
                            ));
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopupStack.Contains(loadingPopup))
                    {
                        await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync();
                    }

                    await PopupNavigation.Instance.PushAsync(new DynamicPopup(
                        "Error",
                        $"No se pudo obtener los datos: {ex.Message}",
                        new Dictionary<string, Action>
                        {
                    { "Aceptar", () => { } }
                        }
                    ));
                }
                finally
                {
                    if (Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopupStack.Contains(loadingPopup))
                    {
                        await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync();
                    }

                    if (connection.State == System.Data.ConnectionState.Open)
                    {
                        connection.Close();
                    }
                }
            }
        }

        private async void CheckProveedor_Clicked(object sender, EventArgs e)
        {
            List<string> camposVacios = new List<string>();

            if (string.IsNullOrWhiteSpace(Proveedor.Text))
                camposVacios.Add("Proveedor");

            if (string.IsNullOrWhiteSpace(NITProveedor.Text))
                camposVacios.Add("NIT Proveedor");

            if (camposVacios.Any())
            {
                string mensaje = "Debe de completar los campos, falta:\n\n" + string.Join("\n", camposVacios);
                await PopupNavigation.Instance.PushAsync(new DynamicPopup(
                    "Campos Vacíos",
                    mensaje,
                    new Dictionary<string, Action>
                    {
                { "Aceptar", () => { } }
                    }
                ));
                return;
            }

            var tcs = new TaskCompletionSource<bool>();

            await PopupNavigation.Instance.PushAsync(new DynamicPopup(
                "Confirmar",
                $"¿El proveedor y el NIT ingresados son correctos?\n\nPROVEEDOR: {Proveedor.Text} \n\nNIT: {NITProveedor.Text}",
                new Dictionary<string, Action>
                {
            { "Sí", () => tcs.TrySetResult(true) },
            { "No", () => tcs.TrySetResult(false) }
                }
            ));

            bool VerifyProveedor = await tcs.Task;

            if (!VerifyProveedor)
            {
                ConfirmProveedor = 0;
                ConfirmNIT = 0;
                CheckProveedor.IsVisible = true;
                CheckProveedor.BackgroundColor = Color.FromHex("#2B2B2B");
                CheckProveedor.BorderColor = Color.FromHex("#ff4141");
                CheckProveedor.BorderWidth = 1;
                CheckProveedor.Text = "✔";
                CheckProveedor.TextColor = Color.FromHex("#ff4141");
                CheckProveedor.IsEnabled = true;
                Proveedor.IsReadOnly = false;
                Proveedor.TextColor = Color.FromHex("#FFF");
                NITProveedor.IsReadOnly = false;
                NITProveedor.TextColor = Color.FromHex("#FFF");
                return;
            }
            ConfirmProveedor = 1;
            ConfirmNIT = 1;
            CheckProveedor.IsVisible = true;
            CheckProveedor.BackgroundColor = Color.FromHex("#2B2B2B");
            CheckProveedor.BorderColor = Color.FromHex("#5de445");
            CheckProveedor.BorderWidth = 1;
            CheckProveedor.Text = "✔";
            CheckProveedor.TextColor = Color.FromHex("#5de445");
            CheckProveedor.IsEnabled = true;
            Proveedor.IsReadOnly = true;
            Proveedor.TextColor = Color.FromHex("#5de445");
            NITProveedor.IsReadOnly = true;
            NITProveedor.TextColor = Color.FromHex("#5de445");
        }

        private void SerieFactura_TextChanged(object sender, TextChangedEventArgs e)
        {
            string textoFiltrado = new string(e.NewTextValue.Where(c => char.IsLetterOrDigit(c)).ToArray());

            if (textoFiltrado != e.NewTextValue)
            {
                SerieFactura.Text = textoFiltrado;
            }

            if (!string.IsNullOrEmpty(SerieFactura.Text))
            {
                CheckSerie.IsVisible = true;
                CheckSerie.BackgroundColor = Color.FromHex("#2B2B2B");
                CheckSerie.BorderColor = Color.FromHex("#ff4141");
                CheckSerie.BorderWidth = 1;
                CheckSerie.Text = "✔";
                CheckSerie.TextColor = Color.FromHex("#ff4141");
                CheckSerie.IsEnabled = true;
            }
            else
            {
                CheckSerie.IsVisible = false;
            }
        }

        private async void CheckSerie_Clicked(object sender, EventArgs e)
        {
            var tcs = new TaskCompletionSource<bool>();

            await PopupNavigation.Instance.PushAsync(new DynamicPopup(
                "Confirmar",
                $"¿La serie de factura ingresada es correcta?\n\nSERIE: {SerieFactura.Text}",
                new Dictionary<string, Action>
                {
            { "Sí", () => tcs.TrySetResult(true) },
            { "No", () => tcs.TrySetResult(false) }
                }
            ));

            bool VerifySerie = await tcs.Task;

            if (!VerifySerie)
            {
                ConfirmSerie = 0;
                CheckSerie.IsVisible = true;
                CheckSerie.BackgroundColor = Color.FromHex("#2B2B2B");
                CheckSerie.BorderColor = Color.FromHex("#ff4141");
                CheckSerie.BorderWidth = 1;
                CheckSerie.Text = "✔";
                CheckSerie.TextColor = Color.FromHex("#ff4141");
                CheckSerie.IsEnabled = true;
                SerieFactura.IsReadOnly = false;
                SerieFactura.TextColor = Color.FromHex("#FFF");
                return;
            }
            ConfirmSerie = 1;
            CheckSerie.IsVisible = true;
            CheckSerie.BackgroundColor = Color.FromHex("#2B2B2B");
            CheckSerie.BorderColor = Color.FromHex("#5de445");
            CheckSerie.BorderWidth = 1;
            CheckSerie.Text = "✔";
            CheckSerie.TextColor = Color.FromHex("#5de445");
            CheckSerie.IsEnabled = true;
            SerieFactura.IsReadOnly = true;
            SerieFactura.TextColor = Color.FromHex("#5de445");
        }

        private void NumeroFactura_TextChanged(object sender, TextChangedEventArgs e)
        {
            string textoFiltrado = new string(e.NewTextValue.Where(c => char.IsDigit(c)).ToArray());

            if (textoFiltrado != e.NewTextValue)
            {
                NumeroFactura.Text = textoFiltrado;
            }

            if (!string.IsNullOrEmpty(NumeroFactura.Text))
            {
                CheckNumero.IsVisible = true;
                CheckNumero.BackgroundColor = Color.FromHex("#2B2B2B");
                CheckNumero.BorderColor = Color.FromHex("#ff4141");
                CheckNumero.BorderWidth = 1;
                CheckNumero.Text = "✔";
                CheckNumero.TextColor = Color.FromHex("#ff4141");
                CheckNumero.IsEnabled = true;
            }
            else
            {
                CheckNumero.IsVisible = false;
            }
        }

        private async void CheckNumero_Clicked(object sender, EventArgs e)
        {
            var tcs = new TaskCompletionSource<bool>();

            await PopupNavigation.Instance.PushAsync(new DynamicPopup(
                "Confirmar",
                $"¿El número de factura ingresado es correcto?\n\nNÚMERO: {NumeroFactura.Text}",
                new Dictionary<string, Action>
                {
                    { "Sí", () => tcs.TrySetResult(true) },
                    { "No", () => tcs.TrySetResult(false) }
                }
            ));

            bool VerifyNumero = await tcs.Task;

            if (!VerifyNumero)
            {
                ConfirmNumero = 0;
                CheckNumero.IsVisible = true;
                CheckNumero.BackgroundColor = Color.FromHex("#2B2B2B");
                CheckNumero.BorderColor = Color.FromHex("#ff4141");
                CheckNumero.BorderWidth = 1;
                CheckNumero.Text = "✔";
                CheckNumero.TextColor = Color.FromHex("#ff4141");
                CheckNumero.IsEnabled = true;
                NumeroFactura.IsReadOnly = false;
                NumeroFactura.TextColor = Color.FromHex("#FFF");
                return;
            }
            ConfirmNumero = 1;
            CheckNumero.IsVisible = true;
            CheckNumero.BackgroundColor = Color.FromHex("#2B2B2B");
            CheckNumero.BorderColor = Color.FromHex("#5de445");
            CheckNumero.BorderWidth = 1;
            CheckNumero.Text = "✔";
            CheckNumero.TextColor = Color.FromHex("#5de445");
            CheckNumero.IsEnabled = true;
            NumeroFactura.IsReadOnly = true;
            NumeroFactura.TextColor = Color.FromHex("#5de445");
        }

        private void MontoFactura_TextChanged(object sender, TextChangedEventArgs e)
        {
            var entry = sender as Entry;

            if (string.IsNullOrWhiteSpace(entry.Text))
                return;

            string texto = entry.Text;
            if (!System.Text.RegularExpressions.Regex.IsMatch(texto, @"^\d*\.?\d{0,2}$"))
            {
                texto = texto.Remove(texto.Length - 1);
                entry.Text = texto;
            }

            if (!string.IsNullOrEmpty(MontoFactura.Text))
            {
                CheckMonto.IsVisible = true;
                CheckMonto.BackgroundColor = Color.FromHex("#2B2B2B");
                CheckMonto.BorderColor = Color.FromHex("#ff4141");
                CheckMonto.BorderWidth = 1;
                CheckMonto.Text = "✔";
                CheckMonto.TextColor = Color.FromHex("#ff4141");
                CheckMonto.IsEnabled = true;
            }
            else
            {
                CheckMonto.IsVisible = false;
            }
        }

        private async void CheckMonto_Clicked(object sender, EventArgs e)
        {
            var tcs = new TaskCompletionSource<bool>();

            await PopupNavigation.Instance.PushAsync(new DynamicPopup(
                "Confirmar",
                $"¿El monto de factura ingresado es correcto?\n\nMONTO: {MontoFactura.Text}",
                new Dictionary<string, Action>
                {
                    { "Sí", () => tcs.TrySetResult(true) },
                    { "No", () => tcs.TrySetResult(false) }
                }
            ));

            bool VerifyMonto = await tcs.Task;

            if (!VerifyMonto)
            {
                ConfirmMonto = 0;
                CheckMonto.IsVisible = true;
                CheckMonto.BackgroundColor = Color.FromHex("#2B2B2B");
                CheckMonto.BorderColor = Color.FromHex("#ff4141");
                CheckMonto.BorderWidth = 1;
                CheckMonto.Text = "✔";
                CheckMonto.TextColor = Color.FromHex("#ff4141");
                CheckMonto.IsEnabled = true;
                MontoFactura.IsReadOnly = false;
                MontoFactura.TextColor = Color.FromHex("#FFF");
                return;
            }
            ConfirmMonto = 1;
            CheckMonto.IsVisible = true;
            CheckMonto.BackgroundColor = Color.FromHex("#2B2B2B");
            CheckMonto.BorderColor = Color.FromHex("#5de445");
            CheckMonto.BorderWidth = 1;
            CheckMonto.Text = "✔";
            CheckMonto.TextColor = Color.FromHex("#5de445");
            CheckMonto.IsEnabled = true;
            MontoFactura.IsReadOnly = true;
            MontoFactura.TextColor = Color.FromHex("#5de445");
        }

        private async void CheckFecha_Clicked(object sender, EventArgs e)
        {
            var tcs = new TaskCompletionSource<bool>();

            await PopupNavigation.Instance.PushAsync(new DynamicPopup(
                "Confirmar",
                $"¿La fecha de factura ingresada es correcta?\n\nFECHA: {FechaFactura.Date.ToString("dddd, dd 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("es-ES"))}",
                new Dictionary<string, Action>
                {
            { "Sí", () => tcs.TrySetResult(true) },
            { "No", () => tcs.TrySetResult(false) }
                }
            ));

            bool VerifyFecha = await tcs.Task;

            if (!VerifyFecha)
            {
                ConfirmFecha = 0;
                CheckFecha.IsVisible = true;
                CheckFecha.BackgroundColor = Color.FromHex("#2B2B2B");
                CheckFecha.BorderColor = Color.FromHex("#ff4141");
                CheckFecha.BorderWidth = 1;
                CheckFecha.Text = "✔";
                CheckFecha.TextColor = Color.FromHex("#ff4141");
                CheckFecha.IsEnabled = true;
                Overlay.IsVisible = false;
                FechaFactura.TextColor = Color.FromHex("#FFF");
                return;
            }
            ConfirmFecha = 1;
            CheckFecha.IsVisible = true;
            CheckFecha.BackgroundColor = Color.FromHex("#2B2B2B");
            CheckFecha.BorderColor = Color.FromHex("#5de445");
            CheckFecha.BorderWidth = 1;
            CheckFecha.Text = "✔";
            CheckFecha.TextColor = Color.FromHex("#5de445");
            CheckFecha.IsEnabled = true;
            Overlay.IsVisible = true;
            FechaFactura.TextColor = Color.FromHex("#5de445");
        }

        private void CargarRazonSocial()
        {
            string serverAddress = Preferences.Get("ServerAddress", string.Empty);
            if (string.IsNullOrEmpty(serverAddress))
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    PopupNavigation.Instance.PushAsync(new DynamicPopup(
                        "Error",
                        "No se encontró la dirección del servidor en las preferencias.",
                        new Dictionary<string, Action>
                        {
                            { "Aceptar", () => { } }
                        }
                    ));
                });
                return;
            }

            string idRazonSocial = null;
            var razonesSociales = new List<RazonesSociales>();

            using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionNexus))
            {
                try
                {
                    connection.Open();

                    string querySucursal = "SELECT RazonSocial FROM sucursales WHERE serverr = @server";
                    MySqlCommand commandSucursal = new MySqlCommand(querySucursal, connection);
                    commandSucursal.Parameters.AddWithValue("@server", serverAddress);

                    using (MySqlDataReader reader = commandSucursal.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            idRazonSocial = reader["RazonSocial"].ToString();
                        }
                    }

                    if (string.IsNullOrEmpty(idRazonSocial))
                    {
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            PopupNavigation.Instance.PushAsync(new DynamicPopup(
                                "Error",
                                "No se encontró una razón social para este servidor.",
                                new Dictionary<string, Action>
                                {
                            { "Aceptar", () => { } }
                                }
                            ));
                        });
                        return;
                    }

                    string queryRazonesSociales = "SELECT id, nombrerazon, nit FROM razonessociales";
                    MySqlCommand commandRazonesSociales = new MySqlCommand(queryRazonesSociales, connection);

                    using (MySqlDataReader reader = commandRazonesSociales.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            razonesSociales.Add(new RazonesSociales
                            {
                                NombreRazon = reader["nombrerazon"].ToString(),
                                Nit = reader["nit"].ToString(),
                                Id = reader["id"].ToString()
                            });
                        }
                    }

                    RazonSocial.ItemsSource = razonesSociales;

                    var razonSeleccionada = razonesSociales.FirstOrDefault(r => r.Id == idRazonSocial);
                    if (razonSeleccionada != null)
                    {
                        RazonSocial.SelectedItem = razonSeleccionada;
                    }
                }
                catch (Exception ex)
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        PopupNavigation.Instance.PushAsync(new DynamicPopup(
                            "Error",
                            $"Hubo un problema al cargar las razones sociales: {ex.Message}",
                            new Dictionary<string, Action>
                            {
                        { "Aceptar", () => { } }
                            }
                        ));
                    });
                }
            }
        }

        private async void CheckRazon_Clicked(object sender, EventArgs e)
        {
            if (RazonSocial.SelectedItem is RazonesSociales seleccionada)
            {
                var tcs = new TaskCompletionSource<bool>();

                await PopupNavigation.Instance.PushAsync(new DynamicPopup(
                    "Confirmar",
                    $"¿El NIT y la Razón Social ingresados son correctos?\n\nNIT: {seleccionada.Nit}\n\nRazón Social: {seleccionada.NombreRazon}",
                    new Dictionary<string, Action>
                    {
                { "Sí", () => tcs.TrySetResult(true) },
                { "No", () => tcs.TrySetResult(false) }
                    }
                ));

                bool VerifyRazon = await tcs.Task;

                if (!VerifyRazon)
                {
                    ConfirmRazon = 0;
                    CheckRazon.IsVisible = true;
                    CheckRazon.BackgroundColor = Color.FromHex("#2B2B2B");
                    CheckRazon.BorderColor = Color.FromHex("#ff4141");
                    CheckRazon.BorderWidth = 1;
                    CheckRazon.Text = "✔";
                    CheckRazon.TextColor = Color.FromHex("#ff4141");
                    CheckRazon.IsEnabled = true;
                    OverlayRazon.IsVisible = false;
                    RazonSocial.TextColor = Color.FromHex("#FFF");
                    return;
                }
                ConfirmRazon = 1;
                CheckRazon.IsVisible = true;
                CheckRazon.BackgroundColor = Color.FromHex("#2B2B2B");
                CheckRazon.BorderColor = Color.FromHex("#5de445");
                CheckRazon.BorderWidth = 1;
                CheckRazon.Text = "✔";
                CheckRazon.TextColor = Color.FromHex("#5de445");
                CheckRazon.IsEnabled = true;
                OverlayRazon.IsVisible = true;
                RazonSocial.TextColor = Color.FromHex("#5de445");
            }
            else
            {
                await PopupNavigation.Instance.PushAsync(new DynamicPopup(
                "Error",
                "Por favor, seleccione una razón social.",
                new Dictionary<string, Action>
                    {
                        { "Aceptar", () => { } }
                    }
                ));
            }
        }

        private void SaveDetail_Clicked(object sender, EventArgs e)
        {
            List<string> camposVacios = new List<string>();
            if (string.IsNullOrWhiteSpace(Proveedor.Text))
                camposVacios.Add("Proveedor");

            if (string.IsNullOrWhiteSpace(NITProveedor.Text))
                camposVacios.Add("NIT Proveedor");

            if (string.IsNullOrWhiteSpace(SerieFactura.Text))
                camposVacios.Add("Serie Factura");

            if (string.IsNullOrWhiteSpace(NumeroFactura.Text))
                camposVacios.Add("Número Factura");

            if (string.IsNullOrWhiteSpace(MontoFactura.Text))
                camposVacios.Add("Monto Factura");

            if (FechaFactura.Date == default)
                camposVacios.Add("Fecha Factura");

            if (RazonSocial.SelectedItem == null)
                camposVacios.Add("Razón Social");

            if (camposVacios.Any())
            {
                string mensaje = "Por favor complete todos los campos, falta:\n\n" + string.Join("\n", camposVacios);

                Device.BeginInvokeOnMainThread(() =>
                {
                    PopupNavigation.Instance.PushAsync(new DynamicPopup(
                        "¡Error, Campos vacios!",
                        mensaje,
                        new Dictionary<string, Action>
                        {
                    { "Aceptar", () => { } }
                        }
                    ));
                });
                return;
            }

            List<string> camposNoConfirmados = new List<string>();
            if (ConfirmProveedor != 1)
                camposNoConfirmados.Add("Proveedor");

            if (ConfirmNIT != 1)
                camposNoConfirmados.Add("NIT Proveedor");

            if (ConfirmSerie != 1)
                camposNoConfirmados.Add("Serie Factura");

            if (ConfirmNumero != 1)
                camposNoConfirmados.Add("Número Factura");

            if (ConfirmMonto != 1)
                camposNoConfirmados.Add("Monto Factura");

            if (ConfirmFecha != 1)
                camposNoConfirmados.Add("Fecha Factura");

            if (ConfirmRazon != 1)
                camposNoConfirmados.Add("Razón Social");

            if (camposNoConfirmados.Any())
            {
                string mensaje = "Por favor confirme todos los datos, falta:\n\n" + string.Join("\n", camposNoConfirmados);

                Device.BeginInvokeOnMainThread(() =>
                {
                    PopupNavigation.Instance.PushAsync(new DynamicPopup(
                        "¡Error, Confirme los datos!",
                        mensaje,
                        new Dictionary<string, Action>
                        {
                    { "Aceptar", () => { } }
                        }
                    ));
                });
                return;
            }
            
            if (Application.Current.MainPage is AppShell appShell)
            {
                idUsuario = int.TryParse(appShell.Id, out int idParsed) ? idParsed : 0;
                NombreUsuario = appShell.Usuario;
                Usuario = appShell.NombreUsuario;

                VerificarFacturaCentral();
            }
        }

        public void VerificarFacturaCentral()
        {
            int idProveedor = int.Parse(IdProveedor.Text);
            var nombreProveedor = Proveedor.Text?.Trim();
            var serieFactura = SerieFactura.Text?.Trim();
            int numeroFactura = int.Parse(NumeroFactura.Text);
            int idRazon = int.Parse(((RazonesSociales)RazonSocial.SelectedItem).Id);
            var nombreRazon = ((RazonesSociales)RazonSocial.SelectedItem).NombreRazon;

            try
            {
                using (var connection = new MySqlConnection(DatabaseConnection.ConnectionCentral))
                {
                    connection.Open();

                    string query = @"
                                   SELECT Id 
                                   FROM facturas_compras 
                                   WHERE IdProveedor = @IdProveedor 
                                   AND NombreProveedor = @NombreProveedor 
                                   AND IdRazon = @IdRazon 
                                   AND NombreRazon = @NombreRazon 
                                   AND Serie = @Serie 
                                   AND Numero = @Numero";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@IdProveedor", idProveedor);
                        command.Parameters.AddWithValue("@NombreProveedor", nombreProveedor);
                        command.Parameters.AddWithValue("@IdRazon", idRazon);
                        command.Parameters.AddWithValue("@NombreRazon", nombreRazon);
                        command.Parameters.AddWithValue("@Serie", serieFactura);
                        command.Parameters.AddWithValue("@Numero", numeroFactura);

                        var result = command.ExecuteScalar();

                        if (result != null && int.TryParse(result.ToString(), out int idInventario))
                        {
                            Device.BeginInvokeOnMainThread(() =>
                            {
                                PopupNavigation.Instance.PushAsync(new DynamicPopup(
                                    "Error (Central)",
                                    $"La factura ya está asignada al inventario con ID: {idInventario}",
                                    new Dictionary<string, Action>
                                    {
                                { "Aceptar", () => { } }
                                    }
                                ));
                            });
                        }
                        else
                        {
                            VerificarFacturaLocal();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    PopupNavigation.Instance.PushAsync(new DynamicPopup(
                        "Error",
                        $"Ocurrió un error al verificar la factura: {ex.Message}",
                        new Dictionary<string, Action>
                        {
                    { "Aceptar", () => { } }
                        }
                    ));
                });
            }
        }

        public void VerificarFacturaLocal()
        {
            int idProveedor = int.Parse(IdProveedor.Text);
            var nombreProveedor = Proveedor.Text?.Trim();
            var serieFactura = SerieFactura.Text?.Trim();
            int numeroFactura = int.Parse(NumeroFactura.Text);
            int idRazon = int.Parse(((RazonesSociales)RazonSocial.SelectedItem).Id);
            var nombreRazon = ((RazonesSociales)RazonSocial.SelectedItem).NombreRazon;

            try
            {
                using (var connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                {
                    connection.Open();

                    string query = @"
                                   SELECT Id 
                                   FROM facturas_compras 
                                   WHERE IdProveedor = @IdProveedor 
                                   AND NombreProveedor = @NombreProveedor 
                                   AND IdRazon = @IdRazon 
                                   AND NombreRazon = @NombreRazon 
                                   AND Serie = @Serie 
                                   AND Numero = @Numero";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@IdProveedor", idProveedor);
                        command.Parameters.AddWithValue("@NombreProveedor", nombreProveedor);
                        command.Parameters.AddWithValue("@IdRazon", idRazon);
                        command.Parameters.AddWithValue("@NombreRazon", nombreRazon);
                        command.Parameters.AddWithValue("@Serie", serieFactura);
                        command.Parameters.AddWithValue("@Numero", numeroFactura);

                        var result = command.ExecuteScalar();

                        if (result != null && int.TryParse(result.ToString(), out int idInventario))
                        {
                            Device.BeginInvokeOnMainThread(() =>
                            {
                                PopupNavigation.Instance.PushAsync(new DynamicPopup(
                                    "Error (Local)",
                                    $"La factura ya está asignada al inventario con ID: {idInventario}",
                                    new Dictionary<string, Action>
                                    {
                                { "Aceptar", () => { } }
                                    }
                                ));
                            });
                        }
                        else
                        {
                            viewFactura.IsVisible = false;
                            viewDetalle.IsVisible = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    PopupNavigation.Instance.PushAsync(new DynamicPopup(
                        "Error",
                        $"Ocurrió un error al verificar la factura: {ex.Message}",
                        new Dictionary<string, Action>
                        {
                    { "Aceptar", () => { } }
                        }
                    ));
                });
            }
        }

        private async void ContinueDetail_Clicked(object sender, EventArgs e)
        {
            viewFactura.IsVisible = false;

            viewDetalle.IsVisible = true;
        }

        private async void Regresar_Clicked(object sender, EventArgs e)
        {
            viewFactura.IsVisible = true;

            viewDetalle.IsVisible = false;
        }

        private async void EscanearUPC_Clicled(object sender, EventArgs e)
        {
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();

            UPCScann.Text = string.Empty;
            DescripcionScann.Text = string.Empty;

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
                    UPCScann.Text = result.Text;


                    ObtenerDescripcionProducto(result.Text);

                }
                else
                {
                    UPCScann.Text = null;
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

        private void UPCScann_TextChanged(object sender, TextChangedEventArgs e)
        {
            var entry = sender as Entry;
            var texto = entry.Text;

            if (!string.IsNullOrEmpty(texto) && !texto.All(char.IsDigit))
            {
                texto = new string(texto.Where(char.IsDigit).ToArray());
            }

            if (texto.Length > 13)
            {
                texto = texto.Substring(0, 13);
            }

            entry.Text = texto;
        }

        private void UPCScann_Completed(object sender, EventArgs e)
        {
            var entry = sender as Entry;
            var texto = entry.Text;

            if (!string.IsNullOrEmpty(texto) && texto.Length < 13)
            {
                entry.Text = texto.PadLeft(13, '0');
            }

            ObtenerDescripcionProducto(entry.Text);
        }

        private void ObtenerDescripcionProducto(string upc)
        {
            try
            {
                using (var connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                {
                    connection.Open();

                    string query = "SELECT desclarga FROM productos WHERE UPC = @UPC";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UPC", upc);

                        var result = command.ExecuteScalar();

                        if (result != null)
                        {
                            DescripcionScann.Text = result.ToString();
                            DescripcionScann.IsReadOnly = true;
                            UPCScann.IsReadOnly = true;
                            Limpiar.IsVisible = true;
                        }
                        else
                        {
                            DescripcionScann.Text = null;
                            DescripcionScann.IsReadOnly = false;
                            UPCScann.IsReadOnly = false;
                            Limpiar.IsVisible = false;

                            PopupNavigation.Instance.PushAsync(new DynamicPopup(
                                "Error",
                                $"Producto no encontrado.",
                                new Dictionary<string, Action>
                                {
                                    { "Aceptar", () => { } }
                                }
                            ));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                PopupNavigation.Instance.PushAsync(new DynamicPopup(
                    "Error",
                    $"Ocurrió un error al obtener la descripción: {ex.Message}",
                    new Dictionary<string, Action>
                    {
                { "Aceptar", () => { } }
                    }
                ));
            }
        }

        private void DescripcionScann_Focused(object sender, FocusEventArgs e)
        {
            if (sender is Entry entry)
            {
                Device.BeginInvokeOnMainThread(() => entry.CursorPosition = 0);
                if (entry.Text != null)
                {
                    Device.BeginInvokeOnMainThread(() => entry.SelectionLength = entry.Text.Length);
                }
            }

            viewUPCScann.IsVisible = false;
            viewIngreso.IsVisible = false;
            viewSearchProducto.IsVisible = true;
            Cancelar.IsVisible = true;
            Limpiar.IsVisible = false;

        }

        private async void DescripcionScann_Completed(object sender, EventArgs e)
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

                string entryDescText = DescripcionScann.Text;
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
                                    Existencia = reader["Existencia"].ToString()
                                });
                            }

                            if (Productos.Count == 0)
                            {
                                if (Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopupStack.Contains(loadingPopup))
                                {
                                    await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync();
                                }

                                await PopupNavigation.Instance.PushAsync(new DynamicPopup(
                                    "Alerta",
                                    "No se encontraron productos con esa descripción.",
                                    new Dictionary<string, Action>
                                    {
                                { "Aceptar", () => { } }
                                    }
                                ));
                            }

                            ListViewProducto.ItemsSource = Productos;
                            ListViewProducto.ItemSelected += ListViewProducto_ItemSelected;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopupStack.Contains(loadingPopup))
                {
                    await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync();
                }

                await PopupNavigation.Instance.PushAsync(new DynamicPopup(
                    "Error de Conexión",
                    $"Error al intentar conectar a la base de datos: {ex.Message}",
                    new Dictionary<string, Action>
                    {
                { "Aceptar", () => { } }
                    }
                ));
            }
            finally
            {
                if (Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopupStack.Contains(loadingPopup))
                {
                    await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync();
                }
            }
        }

        private void ListViewProducto_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem == null)
                return;

            var selectedProducto = (Producto)e.SelectedItem;
            UPCScann.Text = $"{selectedProducto.Upc}";
            DescripcionScann.Text = $"{selectedProducto.DescLarga}";

            ((ListView)sender).SelectedItem = null;

            viewUPCScann.IsVisible = true;
            viewIngreso.IsVisible = true;
            viewSearchProducto.IsVisible = false;
            Cancelar.IsVisible = false;

            DescripcionScann.IsReadOnly = true;
            UPCScann.IsReadOnly = true;
            Limpiar.IsVisible = true;
            
            DescripcionScann.Unfocus();
        }

        private void Cancelar_Clicked(object sender, EventArgs e)
        {
            viewUPCScann.IsVisible = true;
            viewIngreso.IsVisible = true;
            viewSearchProducto.IsVisible = false;
            Cancelar.IsVisible = false;

            DescripcionScann.Text = string.Empty;
            UPCScann.Text = string.Empty;
        }

        private void Limpiar_Clicked(object sender, EventArgs e)
        {
            UPCScann.Text = "";
            DescripcionScann.Text = null;

            UPCScann.IsReadOnly = false;
            DescripcionScann.IsReadOnly = false;
            Limpiar.IsVisible = false;
        }

        private void CantidadScann_TextChanged(object sender, TextChangedEventArgs e)
        {
            var entry = sender as Entry;
            var texto = entry.Text;

            if (!string.IsNullOrEmpty(texto))
            {
                texto = new string(texto.Where(c => char.IsDigit(c) || c == '.').ToArray());

                if (texto.Count(c => c == '.') > 1)
                {
                    texto = texto.Remove(texto.LastIndexOf('.'));
                }

                if (texto.Contains("."))
                {
                    var partes = texto.Split('.');
                    var parteEntera = partes[0];
                    var parteDecimal = partes.Length > 1 ? partes[1] : string.Empty;

                    if (parteDecimal.Length > 2)
                    {
                        parteDecimal = parteDecimal.Substring(0, 2);
                    }

                    texto = $"{parteEntera}.{parteDecimal}";
                }
                entry.Text = texto;
            }
        }

        private async void CantidadScann_Completed(object sender, EventArgs e)
        {
            var errores = new List<string>();

            if (string.IsNullOrWhiteSpace(UPCScann.Text))
                errores.Add("UPC");
            if (string.IsNullOrWhiteSpace(DescripcionScann.Text))
                errores.Add("Descripción");
            if (string.IsNullOrWhiteSpace(CantidadScann.Text))
                errores.Add("Cantidad");

            if (errores.Any())
            {
                await PopupNavigation.Instance.PushAsync(new DynamicPopup(
                    "Error",
                    $"Faltan los siguientes campos por llenar: {string.Join(", ", errores)}",
                    new Dictionary<string, Action> { { "Aceptar", () => { } } }
                ));
                return;
            }

            string upc = UPCScann.Text.Trim();
            string descripcion = DescripcionScann.Text.Trim();
            int cantidad = int.Parse(CantidadScann.Text.Trim());

            if (idInventarioGenerado == 0)
            {
                InsertarEnInventarios();
                InsertarEnHistorialInventarios();
                InsertarFacturaLocal();

                CheckProveedor.IsVisible = false;
                CheckSerie.IsVisible = false;
                CheckNumero.IsVisible = false;
                CheckMonto.IsVisible = false;
                CheckFecha.IsVisible = false;
                CheckRazon.IsVisible = false;
                SaveDetail.IsVisible = false;

                ContinueDetail.IsVisible = true;



                if (idInventarioGenerado == 0)
                {
                    await PopupNavigation.Instance.PushAsync(new DynamicPopup(
                        "Error",
                        "No se pudo asignar un ID de inventario. Por favor, intente nuevamente.",
                        new Dictionary<string, Action> { { "Aceptar", () => { } } }
                    ));
                    return;
                }
            }

            var productoExistente = detalle.FirstOrDefault(p => p.UPC == upc && p.Descripcion == descripcion);

            if (productoExistente != null)
            {
                await PopupNavigation.Instance.PushAsync(new DynamicPopup(
                    "Producto existente",
                    "El producto ya existe en la lista. ¿Desea agregar la cantidad al existente?",
                    new Dictionary<string, Action>
                    {
                        { "Sí", () =>
                            {
                                productoExistente.Cantidad += cantidad;

                                SumaryActualizar(productoExistente);

                                ListViewDetalle.ItemsSource = null;
                                ListViewDetalle.ItemsSource = detalle;

                                UPCScann.Text = string.Empty;
                                DescripcionScann.Text = string.Empty;
                                CantidadScann.Text = string.Empty;
                            }
                        },
                        { "No", () =>
                            {
                                UPCScann.Text = string.Empty;
                                DescripcionScann.Text = string.Empty;
                                CantidadScann.Text = string.Empty;
                                CantidadScann.Text = string.Empty;
                            }
                        }
                    }
                ));

                return;
            }

            detalle.Add(new DetalleScann
            {
                UPC = upc,
                Descripcion = descripcion,
                Cantidad = cantidad,
                Bonificacion = 0,
                Vencimiento = FechaScann.Date.ToString("dd/MM/yyyy")
            });

            ListViewDetalle.ItemsSource = null;
            ListViewDetalle.ItemsSource = detalle;
            ListViewDetalle.ItemSelected += ListViewDetalle_ItemSelected;

            UPCScann.Text = string.Empty;
            DescripcionScann.Text = string.Empty;
            CantidadScann.Text = string.Empty;

            UPCScann.IsReadOnly = false;
            DescripcionScann.IsReadOnly = false;

            InsertarDetalleInventario();
        }

        public void InsertarEnInventarios()
        {
            int idProveedor = int.Parse(IdProveedor.Text);
            var nombreProveedor = Proveedor.Text?.Trim();
            var serieFactura = SerieFactura.Text?.Trim();
            int numeroFactura = int.Parse(NumeroFactura.Text);
            string fechaFactura = FechaFactura.Date.ToString("yyyy/MM/dd");
            int idRazon = int.Parse(((RazonesSociales)RazonSocial.SelectedItem).Id);
            var nombreRazon = ((RazonesSociales)RazonSocial.SelectedItem).NombreRazon;

            try
            {
                using (var connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                {
                    connection.Open();

                    string insertQuery = @"
                                         INSERT INTO inventarios 
                                         (IdUsuarios, Fecha, Operacion, FechaHoraI, IdProveedores, Serie, Numero, FechaFactura, Estado, 
                                         FechaHoraF, Proveedor, Usuario, TipoDeRecepcion, IdRazon, NombreRazon) 
                                         VALUES 
                                         (@IdUsuario, @Fecha, 0, @FechaHoraI, @IdProveedores, @Serie, @Numero, @FechaFactura, @Estado, 
                                         @FechaHoraF, @Proveedor, @Usuario, @TipoDeRecepcion, @IdRazon, @NombreRazon);";

                    using (var insertCommand = new MySqlCommand(insertQuery, connection))
                    {
                        insertCommand.Parameters.AddWithValue("@IdUsuario", idUsuario);
                        insertCommand.Parameters.AddWithValue("@Fecha", DateTime.Now.ToString("yyyy-MM-dd"));
                        insertCommand.Parameters.AddWithValue("@FechaHoraI", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        insertCommand.Parameters.AddWithValue("@IdProveedores", idProveedor);
                        insertCommand.Parameters.AddWithValue("@Serie", serieFactura);
                        insertCommand.Parameters.AddWithValue("@Numero", numeroFactura);
                        insertCommand.Parameters.AddWithValue("@FechaFactura", fechaFactura);
                        insertCommand.Parameters.AddWithValue("@Estado", 0);
                        insertCommand.Parameters.AddWithValue("@FechaHoraF", "0000-00-00 00:00:00");
                        insertCommand.Parameters.AddWithValue("@Proveedor", nombreProveedor);
                        insertCommand.Parameters.AddWithValue("@Usuario", NombreUsuario);
                        insertCommand.Parameters.AddWithValue("@TipoDeRecepcion", 1);
                        insertCommand.Parameters.AddWithValue("@IdRazon", idRazon);
                        insertCommand.Parameters.AddWithValue("@NombreRazon", nombreRazon);

                        insertCommand.ExecuteNonQuery();
                    }

                    string selectQuery = @"
                                         SELECT idInventarios 
                                         FROM inventarios 
                                         WHERE IdUsuarios = @IdUsuario 
                                           AND Serie = @Serie 
                                           AND Numero = @Numero 
                                           AND FechaFactura = @FechaFactura 
                                           AND Proveedor = @Proveedor
                                           AND IdRazon = @IdRazon 
                                         ORDER BY FechaHoraI DESC 
                                         LIMIT 1;";

                    using (var selectCommand = new MySqlCommand(selectQuery, connection))
                    {
                        selectCommand.Parameters.AddWithValue("@IdUsuario", idUsuario);
                        selectCommand.Parameters.AddWithValue("@Serie", serieFactura);
                        selectCommand.Parameters.AddWithValue("@Numero", numeroFactura);
                        selectCommand.Parameters.AddWithValue("@FechaFactura", fechaFactura);
                        selectCommand.Parameters.AddWithValue("@Proveedor", nombreProveedor);
                        selectCommand.Parameters.AddWithValue("@IdRazon", idRazon);

                        var result = selectCommand.ExecuteScalar();
                        if (result != null)
                        {
                            idInventarioGenerado = Convert.ToInt32(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                PopupNavigation.Instance.PushAsync(new DynamicPopup(
                    "Error",
                    $"Ocurrió un error al insertar en inventarios: {ex.Message}",
                    new Dictionary<string, Action>
                    {
                { "Aceptar", () => { } }
                    }
                ));
            }
        }
        
        public void InsertarFacturaLocal()
        {
            int idProveedor = int.Parse(IdProveedor.Text);
            var nombreProveedor = Proveedor.Text?.Trim();
            var serieFactura = SerieFactura.Text?.Trim();
            int numeroFactura = int.Parse(NumeroFactura.Text);
            decimal montoFactura = decimal.Parse(MontoFactura.Text, NumberStyles.Any, CultureInfo.InvariantCulture);
            string fechaFactura = FechaFactura.Date.ToString("yyyy/MM/dd");
            int idRazon = int.Parse(((RazonesSociales)RazonSocial.SelectedItem).Id);
            var nombreRazon = ((RazonesSociales)RazonSocial.SelectedItem).NombreRazon;

            try
            {
                using (var connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                {
                    connection.Open();

                    string insertQuery = @"
                                         INSERT INTO facturas_compras 
                                         (IdProveedor, NombreProveedor, IdUsuarioIngresa, NombreUsuarioIngresa, IdRazon, NombreRazon, Serie, Numero, MontoFactura,
                                          FechaRecepcion, FechaIngreso, FechaFactura, IdInventarios)
                                         VALUES 
                                         (@IdProveedores, @Proveedor, @IdUsuario, @Usuario, @IdRazon, @NombreRazon, @Serie, @Numero, @Monto, @Fecha, 
                                          @FechaHoraI, @FechaFactura, @idInventario)";

                    using (var insertCommand = new MySqlCommand(insertQuery, connection))
                    {
                        insertCommand.Parameters.AddWithValue("@IdProveedores", idProveedor);
                        insertCommand.Parameters.AddWithValue("@Proveedor", nombreProveedor);
                        insertCommand.Parameters.AddWithValue("@IdUsuario", idUsuario);
                        insertCommand.Parameters.AddWithValue("@Usuario", NombreUsuario);
                        insertCommand.Parameters.AddWithValue("@IdRazon", idRazon);
                        insertCommand.Parameters.AddWithValue("@NombreRazon", nombreRazon);
                        insertCommand.Parameters.AddWithValue("@Serie", serieFactura);
                        insertCommand.Parameters.AddWithValue("@Numero", numeroFactura);
                        insertCommand.Parameters.AddWithValue("@Monto", montoFactura);
                        insertCommand.Parameters.AddWithValue("@Fecha", DateTime.Now.ToString("yyyy-MM-dd"));
                        insertCommand.Parameters.AddWithValue("@FechaHoraI", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        insertCommand.Parameters.AddWithValue("@FechaFactura", fechaFactura);
                        insertCommand.Parameters.AddWithValue("@idInventario", idInventarioGenerado);

                        insertCommand.ExecuteNonQuery();
                    }

                    string selectQuery = @"
                                         SELECT id 
                                         FROM facturas_compras 
                                         WHERE IdUsuarioIngresa = @IdUsuario 
                                         AND Serie = @Serie 
                                         AND Numero = @Numero 
                                         AND NombreProveedor = @Proveedor
                                         AND IdRazon = @IdRazon 
                                         ORDER BY FechaIngreso DESC 
                                         LIMIT 1;";

                    using (var selectCommand = new MySqlCommand(selectQuery, connection))
                    {
                        selectCommand.Parameters.AddWithValue("@IdUsuario", idUsuario);
                        selectCommand.Parameters.AddWithValue("@Serie", serieFactura);
                        selectCommand.Parameters.AddWithValue("@Numero", numeroFactura);
                        selectCommand.Parameters.AddWithValue("@Proveedor", nombreProveedor);
                        selectCommand.Parameters.AddWithValue("@IdRazon", idRazon);

                        var result = selectCommand.ExecuteScalar();
                        if (result != null)
                        {
                            idFacturaGenerada = Convert.ToInt32(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                PopupNavigation.Instance.PushAsync(new DynamicPopup(
                    "Error",
                    $"Ocurrió un error al insertar en facturas_compras (Local): {ex.Message}",
                    new Dictionary<string, Action>
                    {
                { "Aceptar", () => { } }
                    }
                ));
            }
        }

        public void InsertarEnHistorialInventarios()
        {
            try
            {
                using (var connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                {
                    connection.Open();

                    string terminal = DatabaseConnection.ObtenerNombreDispositivo();

                    string insertarHistorialQuery = @"
                                                    INSERT INTO historial_inventarios 
                                                    (IdInventario, IdUsuario, Usuario, Fecha, FechaHora, Terminal, Sistema, Descripcion) 
                                                    VALUES 
                                                    (@IdInventario, @IdUsuario, @Usuario, @Fecha, @FechaHora, @Terminal, @Sistema, @Descripcion)";

                    using (var insertarCommand = new MySqlCommand(insertarHistorialQuery, connection))
                    {
                        insertarCommand.Parameters.AddWithValue("@IdInventario", idInventarioGenerado);
                        insertarCommand.Parameters.AddWithValue("@IdUsuario", idUsuario);
                        insertarCommand.Parameters.AddWithValue("@Usuario", NombreUsuario);
                        insertarCommand.Parameters.AddWithValue("@Fecha", DateTime.Now.ToString("yyyy-MM-dd"));
                        insertarCommand.Parameters.AddWithValue("@FechaHora", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        insertarCommand.Parameters.AddWithValue("@Terminal", terminal);
                        insertarCommand.Parameters.AddWithValue("@Sistema", "Android");
                        insertarCommand.Parameters.AddWithValue("@Descripcion", "Nuevo ---> Ingreso");

                        insertarCommand.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                PopupNavigation.Instance.PushAsync(new DynamicPopup(
                    "Error",
                    $"Ocurrió un error al insertar en historial_inventarios: {ex.Message}",
                    new Dictionary<string, Action>
                    {
                { "Aceptar", () => { } }
                    }
                ));
            }
        }

        public void InsertarDetalleInventario()
        {
            try
            {
                if (detalle.Count == 0)
                {
                    PopupNavigation.Instance.PushAsync(new DynamicPopup(
                        "Error",
                        "No hay productos en la lista para insertar en el detalle de inventario.",
                        new Dictionary<string, Action>
                        {
                    { "Aceptar", () => { } }
                        }
                    ));
                    return;
                }

                var ultimoProducto = detalle.LastOrDefault();
                if (ultimoProducto == null)
                {
                    PopupNavigation.Instance.PushAsync(new DynamicPopup(
                        "Error",
                        "No se pudo obtener el producto de la lista.",
                        new Dictionary<string, Action>
                        {
                    { "Aceptar", () => { } }
                        }
                    ));
                    return;
                }

                using (var connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                {
                    connection.Open();

                    string obtenerDatosProductoQuery = "SELECT Costo, IdProveedores, IdDepartamentos FROM productos WHERE Upc = @Upc";
                    decimal costo = 0;
                    long idProveedor = 0, idDepartamento = 0;

                    using (var datosCommand = new MySqlCommand(obtenerDatosProductoQuery, connection))
                    {
                        datosCommand.Parameters.AddWithValue("@Upc", ultimoProducto.UPC);
                        using (var reader = datosCommand.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                costo = Convert.ToDecimal(reader["Costo"]);
                                idProveedor = Convert.ToInt64(reader["IdProveedores"]);
                                idDepartamento = Convert.ToInt64(reader["IdDepartamentos"]);
                            }
                        }
                    }

                    string obtenerUnidadesFardoQuery = "SELECT Cantidad, UPCPaquete FROM productospaquetes WHERE Upc = @Upc";
                    long unidadesFardo = 0;
                    long upcPaquete = 0;

                    using (var fardoCommand = new MySqlCommand(obtenerUnidadesFardoQuery, connection))
                    {
                        fardoCommand.Parameters.AddWithValue("@Upc", ultimoProducto.UPC);
                        using (var reader = fardoCommand.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                unidadesFardo = reader["Cantidad"] != DBNull.Value ? Convert.ToInt64(reader["Cantidad"]) : 0;
                                upcPaquete = reader["UPCPaquete"] != DBNull.Value ? Convert.ToInt64(reader["UPCPaquete"]) : 0;
                            }
                        }
                    }

                    long idRazonSocial = long.Parse(((RazonesSociales)RazonSocial.SelectedItem).Id);

                    string insertarDetalleQuery = @"
                                                  INSERT INTO detalleinventarios 
                                                  (IdInventarios, Upc, Cantidad, Dun14, UnidadesFardo, CantidadBonificada, Descripcion, Costo, 
                                                   IdProveedor, IdDepartamento, FechaVencimiento, IdRazonSocial) 
                                                  VALUES 
                                                  (@IdInventarios, @Upc, @Cantidad, @Dun14, @UnidadesFardo, @CantidadBonificada, @Descripcion, @Costo, 
                                                   @IdProveedor, @IdDepartamento, @FechaVencimiento, @IdRazonSocial)";

                    using (var insertarCommand = new MySqlCommand(insertarDetalleQuery, connection))
                    {
                        insertarCommand.Parameters.AddWithValue("@IdInventarios", idInventarioGenerado);
                        insertarCommand.Parameters.AddWithValue("@Upc", ultimoProducto.UPC);
                        insertarCommand.Parameters.AddWithValue("@Cantidad", ultimoProducto.Cantidad);
                        insertarCommand.Parameters.AddWithValue("@Dun14", upcPaquete);
                        insertarCommand.Parameters.AddWithValue("@UnidadesFardo", unidadesFardo);
                        insertarCommand.Parameters.AddWithValue("@CantidadBonificada", ultimoProducto.Bonificacion);
                        insertarCommand.Parameters.AddWithValue("@Descripcion", ultimoProducto.Descripcion);
                        insertarCommand.Parameters.AddWithValue("@Costo", costo);
                        insertarCommand.Parameters.AddWithValue("@IdProveedor", idProveedor);
                        insertarCommand.Parameters.AddWithValue("@IdDepartamento", idDepartamento);
                        insertarCommand.Parameters.AddWithValue("@FechaVencimiento", DateTime.ParseExact(ultimoProducto.Vencimiento, "dd/MM/yyyy", null).ToString("yyyy-MM-dd 00:00:00"));
                        insertarCommand.Parameters.AddWithValue("@IdRazonSocial", idRazonSocial);

                        insertarCommand.ExecuteNonQuery();
                    }

                    string validarIdQuery = @"
                                            SELECT LAST_INSERT_ID() 
                                            FROM detalleinventarios 
                                            WHERE IdInventarios = @IdInventarios AND Upc = @Upc AND Descripcion = @Descripcion";

                    using (var validarCommand = new MySqlCommand(validarIdQuery, connection))
                    {
                        validarCommand.Parameters.AddWithValue("@IdInventarios", idInventarioGenerado);
                        validarCommand.Parameters.AddWithValue("@Upc", ultimoProducto.UPC);
                        validarCommand.Parameters.AddWithValue("@Descripcion", ultimoProducto.Descripcion);

                        var idResult = validarCommand.ExecuteScalar();
                        if (idResult != null && long.TryParse(idResult.ToString(), out long idDetalle))
                        {
                            ultimoProducto.Id = (int)idDetalle; 
                        }
                        else
                        {
                            throw new Exception("No se pudo validar el ID generado para el producto insertado.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                PopupNavigation.Instance.PushAsync(new DynamicPopup(
                    "Error",
                    $"Ocurrió un error al insertar el detalle del inventario: {ex.Message}",
                    new Dictionary<string, Action>
                    {
                { "Aceptar", () => { } }
                    }
                ));
            }
        }

        private void SumaryActualizar(DetalleScann productoExistente)
        {
            try
            {
                using (var connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                {
                    connection.Open();

                    string query = @"
                                   UPDATE detalleinventarios
                                   SET Cantidad = @Cantidad
                                   WHERE IdInventarios = @IdInventarios AND Upc = @Upc";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Cantidad", productoExistente.Cantidad);
                        command.Parameters.AddWithValue("@IdInventarios", idInventarioGenerado);
                        command.Parameters.AddWithValue("@Upc", productoExistente.UPC);

                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                PopupNavigation.Instance.PushAsync(new DynamicPopup(
                    "Error",
                    $"Ocurrió un error al actualizar la cantidad del producto en la base de datos: {ex.Message}",
                    new Dictionary<string, Action> { { "Aceptar", () => { } } }
                ));
            }
        }

        private bool isPopupOpen = false;

        private async void ListViewDetalle_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (isPopupOpen || e.SelectedItem == null) return;

            isPopupOpen = true;

            if (e.SelectedItem is DetalleScann selectedItem)
            {
                ListViewDetalle.SelectedItem = null;

                var acciones = new Dictionary<string, Action>
                {
                    { "Editar", () =>
                        {
                            EditarProducto(selectedItem);
                        }
                    },
                    { "Eliminar", () =>
                        {
                            EliminarProductoDetalle(selectedItem);
                        }
                    },
                    { "Cancelar", () => { } }
                };

                await PopupNavigation.Instance.PushAsync(new DynamicPopup(
                    "Opciones",
                    $"¿Qué desea hacer con el producto?\n\nnDescripción: {selectedItem.Descripcion}\nUPC: {selectedItem.UPC}",
                    acciones
                ));
            }
            isPopupOpen = false;
        }

        private async void EditarProducto(DetalleScann producto)
        {
            var cantidadEntry = new Entry
            {
                Text = producto.Cantidad.ToString("F2"),
                Keyboard = Keyboard.Numeric,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            cantidadEntry.Focused += (s, e) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    cantidadEntry.CursorPosition = 0;
                    cantidadEntry.SelectionLength = cantidadEntry.Text?.Length ?? 0;
                });
            };

            var bonificacionEntry = new Entry
            {
                Text = producto.Bonificacion.ToString("F2"),
                Keyboard = Keyboard.Numeric,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            bonificacionEntry.Focused += (s, e) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    bonificacionEntry.CursorPosition = 0;
                    bonificacionEntry.SelectionLength = bonificacionEntry.Text?.Length ?? 0;
                });
            };

            var fechaPicker = new DatePicker
            {
                Date = DateTime.TryParseExact(producto.Vencimiento, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var fecha)
                    ? fecha
                    : DateTime.Now,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            var contenidoAdicional = new StackLayout
            {
                Children =
                {
                    new Label { Text = $"UPC: {producto.UPC}", FontSize = 16, TextColor = Color.Black },
                    new Label { Text = $"Nombre: {producto.Descripcion}\n", FontSize = 16, TextColor = Color.Black },
                    new Label { Text = "Cantidad:", FontSize = 14, TextColor = Color.Black },
                    cantidadEntry,
                    new Label { Text = "Bonificación:", FontSize = 14, TextColor = Color.Black },
                    bonificacionEntry,
                    new Label { Text = "Fecha Vencimiento:", FontSize = 14, TextColor = Color.Black },
                    fechaPicker,
                    new Label { Text = "", FontSize = 14, TextColor = Color.Black }
                }
            };

            await PopupNavigation.Instance.PushAsync(new DynamicPopup(
                "Editar producto",
                null,
                new Dictionary<string, Action>
                {
                    {
                        "Aceptar",
                        () =>
                        {
                            producto.Cantidad = int.TryParse(cantidadEntry.Text, out var cantidad) ? cantidad : producto.Cantidad;
                            producto.Bonificacion = int.TryParse(bonificacionEntry.Text, out var bonificacion) ? bonificacion : producto.Bonificacion;
                            producto.Vencimiento = fechaPicker.Date.ToString("dd/MM/yyyy");


                            ActualizarProductoDetalle(producto);

                            ListViewDetalle.ItemsSource = null;
                            ListViewDetalle.ItemsSource = detalle;
                        }
                    },
                    {
                        "Cancelar",
                        () => {}
                    }
                },
                contenidoAdicional 
            ));
        }

        private void ActualizarProductoDetalle(DetalleScann producto)
        {
            try
            {
                using (var connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                {
                    connection.Open();

                    string updateQuery = @"
                                         UPDATE detalleinventarios 
                                         SET Cantidad = @Cantidad, 
                                         CantidadBonificada = @CantidadBonificada, 
                                         FechaVencimiento = @FechaVencimiento 
                                         WHERE Iddetalleinventarios = @Id AND Upc = @Upc";

                    using (var command = new MySqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Cantidad", producto.Cantidad);
                        command.Parameters.AddWithValue("@CantidadBonificada", producto.Bonificacion);
                        command.Parameters.AddWithValue("@FechaVencimiento", DateTime.ParseExact(producto.Vencimiento, "dd/MM/yyyy", null).ToString("yyyy-MM-dd 00:00:00"));
                        command.Parameters.AddWithValue("@Id", producto.Id);
                        command.Parameters.AddWithValue("@Upc", producto.UPC);

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected == 0)
                        {
                            PopupNavigation.Instance.PushAsync(new DynamicPopup(
                                "Error",
                                "No se pudo actualizar el producto",
                                new Dictionary<string, Action> { { "Aceptar", () => { } } }
                            ));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                PopupNavigation.Instance.PushAsync(new DynamicPopup(
                    "Error",
                    $"Ocurrió un error al actualizar el producto: {ex.Message}",
                    new Dictionary<string, Action> { { "Aceptar", () => { } } }
                ));
            }
        }

        private void EliminarProductoDetalle(DetalleScann producto)
        {
            try
            {
                using (var connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                {
                    connection.Open();

                    string deleteQuery = "DELETE FROM detalleinventarios WHERE Iddetalleinventarios = @Id AND Upc = @Upc";

                    using (var command = new MySqlCommand(deleteQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Id", producto.Id);
                        command.Parameters.AddWithValue("@Upc", producto.UPC);

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            detalle.Remove(producto);

                            ListViewDetalle.ItemsSource = null;
                            ListViewDetalle.ItemsSource = detalle;
                        }
                        else
                        {
                            PopupNavigation.Instance.PushAsync(new DynamicPopup(
                                "Error",
                                "No se pudo eliminar el producto de la base de datos.",
                                new Dictionary<string, Action>
                                {
                                    { "Aceptar", () => { } }
                                }
                            ));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                PopupNavigation.Instance.PushAsync(new DynamicPopup(
                    "Error",
                    $"Ocurrió un error al eliminar el producto: {ex.Message}",
                    new Dictionary<string, Action>
                    {
                { "Aceptar", () => { } }
                    }
                ));
            }
        }

        private async void VaciarDetalle_Clicked(object sender, EventArgs e)
        {
            await PopupNavigation.Instance.PushAsync(new DynamicPopup(
                "Confirmar",
                "¿Está seguro que desea vaciar el detalle del inventario?",
                new Dictionary<string, Action>
                {
            {
                "Aceptar",
                () =>
                {
                    VaciarDetalleInventario(idInventarioGenerado);
                }
            },
            { "Cancelar", () => { } }
                }
            ));
        }
        private void VaciarDetalleInventario(long idInventarioGenerado)
        {
            try
            {
                using (var connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                {
                    connection.Open();

                    string deleteQuery = "DELETE FROM detalleinventarios WHERE IdInventarios = @IdInventarios";

                    using (var command = new MySqlCommand(deleteQuery, connection))
                    {
                        command.Parameters.AddWithValue("@IdInventarios", idInventarioGenerado);
                        int rowsAffected = command.ExecuteNonQuery();

                        detalle.Clear();
                        ListViewDetalle.ItemsSource = null;
                        ListViewDetalle.ItemsSource = detalle;
                    }
                }
            }
            catch (Exception ex)
            {
                PopupNavigation.Instance.PushAsync(new DynamicPopup(
                    "Error",
                    $"Ocurrió un error al eliminar el detalle del inventario: {ex.Message}",
                    new Dictionary<string, Action> { { "Aceptar", () => { } } }
                ));
            }
        }

        private async void ContinuarDetalle_Clicked(object sender, EventArgs e)
        {
            if (!detalle.Any())
            {
                await PopupNavigation.Instance.PushAsync(new DynamicPopup(
                    "Error",
                    "No hay productos en el detalle del inventario.",
                    new Dictionary<string, Action> { { "Aceptar", () => { } } }
                ));
                return;
            }

            int skus = detalle.Count;
            int totalUnidades = 0;
            int totalBonificacion = 0;
            decimal costoTotal = 0;

            using (var connection = new MySqlConnection(DatabaseConnection.ConnectionString))
            {
                connection.Open();
                foreach (var producto in detalle)
                {
                    totalUnidades += producto.Cantidad;
                    totalBonificacion += producto.Bonificacion;

                    string query = "SELECT Costo FROM productos WHERE Upc = @Upc";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Upc", producto.UPC);

                        var result = command.ExecuteScalar();
                        if (result != null)
                        {
                            decimal costo = Convert.ToDecimal(result);
                            costoTotal += costo * producto.Cantidad;
                        }

                        Skus.Text = skus.ToString("F2"); ;
                        Unidades.Text = totalUnidades.ToString("F2");
                        Fardos.Text = "0.00";
                        BonificacionU.Text = totalBonificacion.ToString("F2");
                        BonificacionF.Text = "0.00";
                        CostoTotal.Text = costoTotal.ToString("F2");

                        viewDetalle.IsVisible = false;
                        viewConfirmar.IsVisible = true;
                        Limpiar.IsVisible = false;
                    }
                }
            }
        }

        private void RegresarDetalle_Clicked(object sender, EventArgs e)
        {
            viewDetalle.IsVisible = true;
            viewConfirmar.IsVisible = false;

        }

        private async void ConfirmarInventario_Clicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DescripcionEditor.Text))
            {
                BordeObservacion.BorderColor = Color.FromHex("#f13b3b");

                await PopupNavigation.Instance.PushAsync(new DynamicPopup(
                    "Advertencia",
                    "Debe llenar las observaciones para poder continuar.",
                    new Dictionary<string, Action> { { "Aceptar", () => { } } }
                ));
                return;
            }

            BordeObservacion.BorderColor = Color.FromHex("#ffff");

            await PopupNavigation.Instance.PushAsync(new DynamicPopup(
                "Confirmar Inventario",
                "¿Desea confirmar el inventario?",
                new Dictionary<string, Action>
                {
            {
                "Aceptar",
                () =>
                {
                    ConfirmarTablaInventario();
                    ConfirmarDetalleInventario();
                    ConfirmarFacturaLocal();
                    ConfirmarHistorialInventario();
                    ConfirmarFacturaCentral();
                    ResetControls();

                    PopupNavigation.Instance.PushAsync(new DynamicPopup(
                        "Éxito",
                        $"El id del inventario generado es: {idInventarioGenerado}\n\n¡RECUERDE DE REALIZAR EL RECHEQUEO EN CORI PARA FINALIZAR!.",
                        new Dictionary<string, Action>
                        {
                            { "Aceptar", () => 
                                {
                                    viewFactura.IsVisible = true;
                                    viewDetalle.IsVisible = false;
                                    viewConfirmar.IsVisible = false;
                                } 
                            }
                        }
                    ));

                }
            },
            { "Cancelar", () => { } }
                }
            ));
        }

        private void ConfirmarTablaInventario()
        {
            try
            {
                using (var connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                {
                    connection.Open();

                    string obtenerUPCsQuery = "SELECT Upc FROM detalleinventarios WHERE IdInventarios = @IdInventarios";
                    var upcList = new List<string>();

                    using (var upcCommand = new MySqlCommand(obtenerUPCsQuery, connection))
                    {
                        upcCommand.Parameters.AddWithValue("@IdInventarios", idInventarioGenerado);
                        using (var reader = upcCommand.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                upcList.Add(reader["Upc"].ToString());
                            }
                        }
                    }

                    if (!upcList.Any())
                    {
                        throw new Exception("No se encontraron UPCs asociados al inventario.");
                    }

                    string obtenerDepartamentosQuery = "SELECT IdDepartamentos FROM productos WHERE Upc = @Upc";
                    var departamentoCount = new Dictionary<int, int>();

                    foreach (var upc in upcList)
                    {
                        using (var deptCommand = new MySqlCommand(obtenerDepartamentosQuery, connection))
                        {
                            deptCommand.Parameters.AddWithValue("@Upc", upc);
                            var result = deptCommand.ExecuteScalar();
                            if (result != null)
                            {
                                int idDepartamento = Convert.ToInt32(result);
                                if (departamentoCount.ContainsKey(idDepartamento))
                                {
                                    departamentoCount[idDepartamento]++;
                                }
                                else
                                {
                                    departamentoCount[idDepartamento] = 1;
                                }
                            }
                        }
                    }

                    if (!departamentoCount.Any())
                    {
                        throw new Exception("No se encontraron departamentos para los UPCs asociados.");
                    }

                    int idDepartamentoMasFrecuente = departamentoCount
                        .OrderByDescending(kvp => kvp.Value)
                        .First().Key;

                    string actualizarInventarioQuery = @"
                                                       UPDATE inventarios 
                                                       SET Observaciones = @Observaciones,
                                                       IdUsuarioFinalizo = @IdUsuarioFinalizo,
                                                       UsuarioFinalizo = @UsuarioFinalizo,
                                                       Estado = @Estado,
                                                       IdDepartamentos = @IdDepartamento,
                                                       FechaHoraF = @FechaHoraFinalizo   
                                                       WHERE IdInventarios = @IdInventarios";

                    using (var updateCommand = new MySqlCommand(actualizarInventarioQuery, connection))
                    {
                        updateCommand.Parameters.AddWithValue("@Observaciones", DescripcionEditor.Text?.Trim());
                        updateCommand.Parameters.AddWithValue("@IdUsuarioFinalizo", idUsuario);
                        updateCommand.Parameters.AddWithValue("@UsuarioFinalizo", Usuario);
                        updateCommand.Parameters.AddWithValue("@Estado", 5);
                        updateCommand.Parameters.AddWithValue("@IdDepartamento", idDepartamentoMasFrecuente);
                        updateCommand.Parameters.AddWithValue("@FechaHoraFinalizo", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        updateCommand.Parameters.AddWithValue("@IdInventarios", idInventarioGenerado);

                        updateCommand.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                PopupNavigation.Instance.PushAsync(new DynamicPopup(
                    "Error",
                    $"Ocurrió un error al confirmar el inventario: {ex.Message}",
                    new Dictionary<string, Action>
                    {
                { "Aceptar", () => { } }
                    }
                ));
            }
        }

        public void ConfirmarDetalleInventario()
        {
            try
            {
                using (var connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                {
                    connection.Open();

                    string queryDetalles = "SELECT UPC, Cantidad, CantidadBonificada FROM detalleinventarios WHERE IdInventarios = @IdInventarios";
                    var productosDetalle = new List<(string UPC, double Cantidad, double CantidadBonificada)>();

                    using (var command = new MySqlCommand(queryDetalles, connection))
                    {
                        command.Parameters.AddWithValue("@IdInventarios", idInventarioGenerado);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string upc = reader["UPC"].ToString();
                                int cantidad = Convert.ToInt32(reader["Cantidad"]);
                                int cantidadbonificada = Convert.ToInt32(reader["CantidadBonificada"]);
                                productosDetalle.Add((upc, cantidad, cantidadbonificada));
                            }
                        }
                    }

                    foreach (var producto in productosDetalle)
                    {
                        string upc = producto.UPC;
                        double cantidad = producto.Cantidad;
                        double cantidadbonificada = producto.CantidadBonificada;

                        string queryExistencia = "SELECT Existencia FROM productos WHERE UPC = @UPC";
                        int existenciaActual = 0;

                        using (var command = new MySqlCommand(queryExistencia, connection))
                        {
                            command.Parameters.AddWithValue("@UPC", upc);
                            var result = command.ExecuteScalar();
                            if (result != null)
                            {
                                existenciaActual = Convert.ToInt32(result);
                            }
                        }

                        double nuevaExistencia = existenciaActual + cantidad + cantidadbonificada;

                        string updateQuery = @"
                                             UPDATE detalleinventarios 
                                             SET 
                                             ExistenciaAnterior = @ExistenciaAnterior,
                                             ExistenciaActual = @ExistenciaActual,
                                             Cantidad_Rechequeo = @CantidadRechequeo,
                                             Opero = 1
                                             WHERE IdInventarios = @IdInventarios AND UPC = @UPC";

                        using (var updateCommand = new MySqlCommand(updateQuery, connection))
                        {
                            updateCommand.Parameters.AddWithValue("@ExistenciaAnterior", existenciaActual);
                            updateCommand.Parameters.AddWithValue("@ExistenciaActual", nuevaExistencia);
                            updateCommand.Parameters.AddWithValue("@CantidadRechequeo", cantidad);
                            updateCommand.Parameters.AddWithValue("@IdInventarios", idInventarioGenerado);
                            updateCommand.Parameters.AddWithValue("@UPC", upc);

                            updateCommand.ExecuteNonQuery();
                        }
                    }
                    ConfirmarKardex();
                    ActualizarExistencia();
                }
            }
            catch (Exception ex)
            {
                PopupNavigation.Instance.PushAsync(new DynamicPopup(
                    "Error",
                    $"Ocurrió un error al confirmar el detalle del inventario: {ex.Message}",
                    new Dictionary<string, Action>
                    {
                { "Aceptar", () => { } }
                    }
                ));
            }
        }

        public void ConfirmarFacturaLocal()
        {
            try
            {
                using (var connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                {
                    connection.Open();

                    string updateQuery = @"
                                         UPDATE facturas_compras
                                         SET 
                                         IdUsuarioFinaliza = @IdUsuarioFinaliza,
                                         NombreUsuarioFinaliza = @NombreUsuarioFinaliza,
                                         Estado = @Estado,
                                         FechaFinalizado = @FechaFinalizacion,
                                         Observaciones = @Observaciones,
                                         IdSucursal = @IdSucursal,
                                         Sucursal = @Sucursal,
                                         Actualizado = @Actualizado,
                                         IdFacturaCompra = @IdFacturaCompra
                                         WHERE Id = @IdFactura";

                    using (var command = new MySqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@IdUsuarioFinaliza", idUsuario);
                        command.Parameters.AddWithValue("@NombreUsuarioFinaliza", Usuario);
                        command.Parameters.AddWithValue("@Estado", 0);
                        command.Parameters.AddWithValue("@FechaFinalizacion", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        command.Parameters.AddWithValue("@Observaciones", DescripcionEditor.Text.Trim());
                        command.Parameters.AddWithValue("@IdSucursal", idSucursal);
                        command.Parameters.AddWithValue("@Sucursal", nombreSucursal);
                        command.Parameters.AddWithValue("@Actualizado", 1);
                        command.Parameters.AddWithValue("@IdFacturaCompra", 1);
                        command.Parameters.AddWithValue("@IdFactura", idFacturaGenerada);

                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                PopupNavigation.Instance.PushAsync(new DynamicPopup(
                    "Error",
                    $"Ocurrió un error al confirmar la factura local: {ex.Message}",
                    new Dictionary<string, Action>
                    {
                { "Aceptar", () => { } }
                    }
                ));
            }
        }

        public void ConfirmarHistorialInventario()
        {
            try
            {
                using (var connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                {
                    connection.Open();

                    string terminal = DatabaseConnection.ObtenerNombreDispositivo();

                    string insertQuery = @"
                                 INSERT INTO historial_inventarios 
                                 (IdInventario, IdUsuario, Usuario, IdEstado,Fecha, FechaHora, Terminal, Sistema, Descripcion) 
                                 VALUES 
                                 (@IdInventario, @IdUsuario, @Usuario, @IdEstado, @Fecha, @FechaHora, @Terminal, @Sistema, @Descripcion)";

                    using (var command = new MySqlCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@IdInventario", idInventarioGenerado);
                        command.Parameters.AddWithValue("@IdUsuario", idUsuario);
                        command.Parameters.AddWithValue("@Usuario", NombreUsuario);
                        command.Parameters.AddWithValue("@IdEstado", 5);
                        command.Parameters.AddWithValue("@Fecha", DateTime.Now.ToString("yyyy-MM-dd"));
                        command.Parameters.AddWithValue("@FechaHora", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        command.Parameters.AddWithValue("@Terminal", terminal);
                        command.Parameters.AddWithValue("@Sistema", "Android");
                        command.Parameters.AddWithValue("@Descripcion", "Ingreso ---> Re-Chequeo");

                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                PopupNavigation.Instance.PushAsync(new DynamicPopup(
                    "Error",
                    $"Ocurrió un error al confirmar el historial del inventario: {ex.Message}",
                    new Dictionary<string, Action>
                    {
                { "Aceptar", () => { } }
                    }
                ));
            }
        }

        public void ConfirmarKardex()
        {
            try
            {
                List<(string upc, string descripcion, int existenciaAnterior, int existenciaActual, int unidades, int idDetalle)> productos = new List<(string, string, int, int, int, int)>();

                using (var connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                {
                    connection.Open();

                    string selectQuery = @"
                                 SELECT UPC, Descripcion, ExistenciaAnterior, ExistenciaActual, Cantidad, IdDetalleinventarios
                                 FROM detalleinventarios 
                                 WHERE IdInventarios = @IdInventarios";

                    using (var selectCommand = new MySqlCommand(selectQuery, connection))
                    {
                        selectCommand.Parameters.AddWithValue("@IdInventarios", idInventarioGenerado);

                        using (var reader = selectCommand.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var upc = reader["UPC"].ToString();
                                var descripcion = reader["Descripcion"].ToString();
                                var existenciaAnterior = Convert.ToInt32(reader["ExistenciaAnterior"]);
                                var existenciaActual = Convert.ToInt32(reader["ExistenciaActual"]);
                                var unidades = Convert.ToInt32(reader["Cantidad"]);
                                var idDetalle = Convert.ToInt32(reader["IdDetalleinventarios"]);

                                productos.Add((upc, descripcion, existenciaAnterior, existenciaActual, unidades, idDetalle));
                            }
                        }
                    }
                }

                using (var connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                {
                    connection.Open();

                    foreach (var producto in productos)
                    {
                        string insertQuery = @"
                                     INSERT INTO kardex 
                                     (Upc, Descripcion, ExistenciaAnterior, ExistenciaActual, Unidades, Identificador, Fecha, FechaHora, IdDetalle, Operacion) 
                                     VALUES 
                                     (@Upc, @Descripcion, @ExistenciaAnterior, @ExistenciaActual, @Unidades, @Identificador, @Fecha, @FechaHora, @IdDetalle, @Operacion)";

                        using (var insertCommand = new MySqlCommand(insertQuery, connection))
                        {
                            insertCommand.Parameters.AddWithValue("@Upc", producto.upc);
                            insertCommand.Parameters.AddWithValue("@Descripcion", producto.descripcion);
                            insertCommand.Parameters.AddWithValue("@ExistenciaAnterior", producto.existenciaAnterior);
                            insertCommand.Parameters.AddWithValue("@ExistenciaActual", producto.existenciaActual);
                            insertCommand.Parameters.AddWithValue("@Unidades", producto.unidades);
                            insertCommand.Parameters.AddWithValue("@Identificador", 7);
                            insertCommand.Parameters.AddWithValue("@Fecha", DateTime.Now.ToString("yyyy-MM-dd"));
                            insertCommand.Parameters.AddWithValue("@FechaHora", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                            insertCommand.Parameters.AddWithValue("@IdDetalle", producto.idDetalle);
                            insertCommand.Parameters.AddWithValue("@Operacion", 0);

                            insertCommand.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                PopupNavigation.Instance.PushAsync(new DynamicPopup(
                    "Error",
                    $"Ocurrió un error al confirmar el kardex: {ex.Message}",
                    new Dictionary<string, Action>
                    {
                { "Aceptar", () => { } }
                    }
                ));
            }
        }

        public void ConfirmarFacturaCentral()
        {
            try
            {
                int idProveedor = int.Parse(IdProveedor.Text);
                var nombreProveedor = Proveedor.Text?.Trim();
                var observaciones = DescripcionEditor.Text?.Trim();
                var serieFactura = SerieFactura.Text?.Trim();
                int numeroFactura = int.Parse(NumeroFactura.Text);
                decimal montoFactura = decimal.Parse(MontoFactura.Text, NumberStyles.Any, CultureInfo.InvariantCulture);
                string fechaFactura = FechaFactura.Date.ToString("yyyy/MM/dd");
                int idRazon = int.Parse(((RazonesSociales)RazonSocial.SelectedItem).Id);
                var nombreRazon = ((RazonesSociales)RazonSocial.SelectedItem).NombreRazon;
                var nitProveedor = NITProveedor.Text?.Trim();

                using (var connection = new MySqlConnection(DatabaseConnection.ConnectionCentral))
                {
                    connection.Open();

                    string insertQuery = @"
                                         INSERT INTO facturas_compras 
                                         (IdProveedor, NombreProveedor, IdUsuarioIngresa, NombreUsuarioIngresa, IdRazon, NombreRazon, Serie, Numero,
                                         MontoFactura, Estado, FechaRecepcion, FechaIngreso, FechaFactura, Observaciones, IdSucursal, Sucursal, 
                                         IdInventory, NIT, IdSucursalCori)
                                         VALUES 
                                         (@IdProveedores, @Proveedor, @IdUsuario, @Usuario, @IdRazon, @NombreRazon, @Serie, @Numero, @Monto, @Estado, @Fecha, 
                                         @FechaHoraI, @FechaFactura, @Observaciones, @IdSucursal, @Sucursal, @idInventario, @NIT, @IdSucursalCori)";

                    using (var insertCommand = new MySqlCommand(insertQuery, connection))
                    {
                        insertCommand.Parameters.AddWithValue("@IdProveedores", idProveedor);
                        insertCommand.Parameters.AddWithValue("@Proveedor", nombreProveedor);
                        insertCommand.Parameters.AddWithValue("@IdUsuario", idUsuario); 
                        insertCommand.Parameters.AddWithValue("@Usuario", NombreUsuario); 
                        insertCommand.Parameters.AddWithValue("@IdRazon", idRazon);
                        insertCommand.Parameters.AddWithValue("@NombreRazon", nombreRazon);
                        insertCommand.Parameters.AddWithValue("@Serie", serieFactura);
                        insertCommand.Parameters.AddWithValue("@Numero", numeroFactura);
                        insertCommand.Parameters.AddWithValue("@Monto", montoFactura);
                        insertCommand.Parameters.AddWithValue("@Estado", 0);
                        insertCommand.Parameters.AddWithValue("@Fecha", DateTime.Now.ToString("yyyy-MM-dd"));
                        insertCommand.Parameters.AddWithValue("@FechaHoraI", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        insertCommand.Parameters.AddWithValue("@FechaFactura", fechaFactura);
                        insertCommand.Parameters.AddWithValue("@Observaciones", observaciones);
                        insertCommand.Parameters.AddWithValue("@IdSucursal", idSucursal);
                        insertCommand.Parameters.AddWithValue("@Sucursal", nombreSucursal);
                        insertCommand.Parameters.AddWithValue("@idInventario", idInventarioGenerado); 
                        insertCommand.Parameters.AddWithValue("@NIT", nitProveedor);
                        insertCommand.Parameters.AddWithValue("@IdSucursalCori", 2);

                        insertCommand.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                PopupNavigation.Instance.PushAsync(new DynamicPopup(
                    "Error",
                    $"Ocurrió un error al insertar en facturas_compras (Central): {ex.Message}",
                    new Dictionary<string, Action>
                    {
                { "Aceptar", () => { } }
                    }
                ));
            }
        }

        public void ActualizarExistencia()
        {
            try
            {
                var productos = new List<(string upc, int existenciaActual)>();

                using (var connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                {
                    connection.Open();
                    string selectDetalleQuery = @"
                                                SELECT UPC, ExistenciaActual 
                                                FROM detalleinventarios 
                                                WHERE IdInventarios = @IdInventarios";

                    using (var selectCommand = new MySqlCommand(selectDetalleQuery, connection))
                    {
                        selectCommand.Parameters.AddWithValue("@IdInventarios", idInventarioGenerado);

                        using (var reader = selectCommand.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                productos.Add((
                                    reader["UPC"].ToString(),
                                    Convert.ToInt32(reader["ExistenciaActual"])
                                ));
                            }
                        }
                    }
                }

                using (var connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                {
                    connection.Open();

                    foreach (var producto in productos)
                    {
                        string updateProductoQuery = @"
                                                     UPDATE productos 
                                                     SET Existencia = @ExistenciaActual 
                                                     WHERE UPC = @UPC";

                        using (var updateCommand = new MySqlCommand(updateProductoQuery, connection))
                        {
                            updateCommand.Parameters.AddWithValue("@ExistenciaActual", producto.existenciaActual);
                            updateCommand.Parameters.AddWithValue("@UPC", producto.upc);

                            updateCommand.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                PopupNavigation.Instance.PushAsync(new DynamicPopup(
                    "Error",
                    $"Ocurrió un error al actualizar las existencias: {ex.Message}",
                    new Dictionary<string, Action>
                    {
                { "Aceptar", () => { }
                }
                    }
                ));
            }
        }


        private void ResetControls()
        {
            int ConfirmProveedor = 0;
            int ConfirmNIT = 0;
            int ConfirmSerie = 0;
            int ConfirmNumero = 0;
            int ConfirmMonto = 0;
            int ConfirmFecha = 0;
            int ConfirmRazon = 0;
            int idInventarioGenerado = 0;
            int idFacturaGenerada = 0;

            DescripcionEditor.Text = string.Empty;

            Skus.Text = string.Empty;
            Unidades.Text = string.Empty;
            Fardos.Text = string.Empty;
            BonificacionU.Text = string.Empty;
            BonificacionF.Text = string.Empty;
            CostoTotal.Text = string.Empty;


            detalle.Clear();
            ListViewDetalle.ItemsSource = null;
            ListViewDetalle.ItemsSource = detalle;

            CantidadScann.Text = string.Empty;
            DescripcionScann.Text = string.Empty;
            UPCScann.Text = string.Empty;
            MontoFactura.Text = string.Empty;
            NumeroFactura.Text = string.Empty;
            SerieFactura.Text = string.Empty;
            NITProveedor.Text = string.Empty;
            Proveedor.Text = string.Empty;

            CantidadScann.IsReadOnly = false;
            DescripcionScann.IsReadOnly = false;
            UPCScann.IsReadOnly = false;
            MontoFactura.IsReadOnly = false;
            NumeroFactura.IsReadOnly = false;
            SerieFactura.IsReadOnly = false;
            NITProveedor.IsReadOnly = false;
            Proveedor.IsReadOnly = false;

            RazonSocial.TextColor = Color.White;
            FechaFactura.TextColor = Color.White;
            MontoFactura.TextColor = Color.White;
            NumeroFactura.TextColor = Color.White;
            SerieFactura.TextColor = Color.White;
            NITProveedor.TextColor = Color.White;
            Proveedor.TextColor = Color.White;

            ResetCheckButton1(CheckRazon);
            ResetCheckButton1(CheckFecha);
            ResetCheckButton(CheckMonto);
            ResetCheckButton(CheckNumero);
            ResetCheckButton(CheckSerie);
            ResetCheckButton(CheckProveedor);

            OverlayRazon.IsVisible = false;
            Overlay.IsVisible = false;
        }

        private void ResetCheckButton(Button button)
        {
            button.IsVisible = false;
            button.BorderColor = Color.FromHex("#ff4141");
            button.TextColor = Color.FromHex("#ff4141");
        }

        private void ResetCheckButton1(Button button)
        {
            button.BorderColor = Color.FromHex("#ff4141");
            button.TextColor = Color.FromHex("#ff4141");
        }

    }
}
