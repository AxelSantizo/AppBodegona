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
using static AppBodegona.Views.Recepcion;
using OfficeOpenXml;
using System.IO;
using OfficeOpenXml.Style;


namespace AppBodegona.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class FechasCortas : ContentPage
    {
        private List<DetalleScann> detalle = new List<DetalleScann>();
        public ObservableCollection<Producto> Productos { get; set; }
        
        private bool regresandoDeScanner = false;

        public FechasCortas()
        {
            InitializeComponent();
            VerificarConexiones();

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

        int idUsuario;
        string NombreUsuario;
        string Usuario;
        int idSucursal;
        string nombreSucursal;

        public class DetalleScann
        {
            public string UPC { get; set; }
            public string Descripcion { get; set; }
            public double Cantidad { get; set; }
            public string Vencimiento { get; set; }

        }

        public class Producto
        {
            public string Upc { get; set; }
            public string DescLarga { get; set; }
            public string Existencia { get; set; }
            public string Costo { get; set; }
            public string IdDepartamento { get; set; }
            public string IdProveedor { get; set; }
        }

        public class Reporte
        {
            public string UPC { get; set; }
            public string Descripcion { get; set; }
            public double Existencia { get; set; }
            public double Costo { get; set; }
            public double SubCosto { get; set; }
            public string FechaVencimiento { get; set; }
            public int DiasDeVida { get; set; }
            public double Ventas { get; set; }
            public double Proyeccion { get; set; }
            public double InventarioFinal { get; set; }
            public string SeDesaloja { get; set; }
            public string Departamento { get; set; }
            public string Proveedor { get; set; }
            public string Encargado { get; set; }
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
            ObtenerDatos();
            base.OnAppearing();
            AppShell appShell = (AppShell)Application.Current.MainPage;
            appShell.ResetInactivityTimer();


            if (regresandoDeScanner)
            {
                regresandoDeScanner = false;
                return;
            }


            if (string.IsNullOrEmpty(appShell.Usuario))
            {
                var tcs = new TaskCompletionSource<bool>();

                Nombre.Text = string.Empty;

                var popup = new DynamicPopup(
                    "Advertencia",
                    "¿Tiene usuario para continuar?",
                    new Dictionary<string, Action>
                    {
                        { "Si", () => tcs.SetResult(true) },
                        { "No", () => tcs.SetResult(false) }
                    });

                await PopupNavigation.Instance.PushAsync(popup);
                bool loginistrue = await tcs.Task;

                if (!loginistrue)
                {
                    viewReporteNiv5.IsVisible = false;
                    viewReporteNiv1.IsVisible = true;
                    viewNombreUsuario.IsVisible = true;
                    Nombre.Text = null;
                    Nombre.IsReadOnly = false;

                }
                else
                {
                    NavigationService.DestinationPage = "FechasCortas";
                    await Shell.Current.GoToAsync("Login");
                }
            }
            else
            {
                if (appShell.IdNivel != "5" && appShell.IdNivel != "11")
                {
                    viewReporteNiv5.IsVisible = false;
                    viewReporteNiv1.IsVisible = true;
                    viewNombreUsuario.IsVisible = true;
                    Nombre.Text = NombreUsuario;
                    Nombre.IsReadOnly = true;
                }
                else
                {

                    Device.BeginInvokeOnMainThread(async () =>
                    {
                        viewReporteNiv1.IsVisible = false;
                        viewReporteNiv5.IsVisible = true;
                    });

                    await Task.Delay(300);

                    FechaReporte.Date = DateTime.Now; 

                    CargarEncargados(FechaReporte.Date);
                    CargarReporte(DateTime.Now);
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

        private void ObtenerDatos()
        {
            if (Application.Current.MainPage is AppShell appShell)
            {
                idUsuario = int.TryParse(appShell.Id, out int idParsed) ? idParsed : 0;
                NombreUsuario = appShell.Usuario;
                Usuario = appShell.NombreUsuario;

                idSucursal = Preferences.Get("IDSUCURSALGLOBAL", 0); 
                nombreSucursal = Preferences.Get("NOMBRESUCURSALGLOBAL", "Desconocido"); 
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

            if (int.TryParse(entry.Text, out int codigoUPC))
            {
                ObtenerDescripcionProducto(codigoUPC);
            }
        }

        private async void EscanearUPC_Clicled(object sender, EventArgs e)
        {
            try
            {
                if (Application.Current?.MainPage is AppShell appShell)
                {
                    appShell.ResetInactivityTimer();
                }

                if (UPCScann != null)
                    UPCScann.Text = string.Empty;

                if (DescripcionScann != null)
                    DescripcionScann.Text = string.Empty;

                regresandoDeScanner = false;

                var scanner = new ZXing.Mobile.MobileBarcodeScanner();
                var options = new ZXing.Mobile.MobileBarcodeScanningOptions
                {
                    PossibleFormats = new List<ZXing.BarcodeFormat> { ZXing.BarcodeFormat.All_1D }
                };

                var result = await scanner.Scan(options);

                if (result != null && !string.IsNullOrEmpty(result.Text))
                {
                    if (UPCScann != null)
                        UPCScann.Text = result.Text;

                    regresandoDeScanner = true;

                    if (int.TryParse(result.Text, out int codigoUPC))
                    {
                        ObtenerDescripcionProducto(codigoUPC);
                    }
                }
                else
                {
                    regresandoDeScanner = true;
                    if (UPCScann != null)
                        UPCScann.Text = string.Empty; 
                }
            }
            catch (Exception ex)
            {
                regresandoDeScanner = true;

                try
                {
                    var popup = new DynamicPopup(
                        "Error",
                        ex.Message ?? "Error desconocido",
                        new Dictionary<string, Action>
                        {
                            { "OK", () => {} }
                        });
                    await PopupNavigation.Instance.PushAsync(popup);
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine($"Error en escáner: {ex.Message}");
                }
            }
        }

        private void ObtenerDescripcionProducto(int upc)
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

            viewNombreUsuario.IsVisible = false;
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

            viewNombreUsuario.IsVisible = true;
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
            viewNombreUsuario.IsVisible = true;
            viewUPCScann.IsVisible = true;
            viewIngreso.IsVisible = true;
            viewSearchProducto.IsVisible = false;
            Cancelar.IsVisible = false;
            UPCScann.Text = string.Empty;
            DescripcionScann = null;
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
            string fechaVencimiento = FechaScann.Date.ToString("dd/MM/yyyy");

            if (!int.TryParse(CantidadScann.Text.Trim(), out int cantidad) || cantidad <= 0)
            {
                await PopupNavigation.Instance.PushAsync(new DynamicPopup(
                    "Error",
                    "La cantidad debe ser un número válido mayor a 0.",
                    new Dictionary<string, Action> { { "Aceptar", () => { } } }
                ));
                return;
            }

            var productoExistente = detalle.FirstOrDefault(p => p.UPC == upc && p.Descripcion == descripcion);

            if (productoExistente != null)
            {
                if (productoExistente.Vencimiento == fechaVencimiento)
                {
                    await PopupNavigation.Instance.PushAsync(new DynamicPopup(
                        "Producto existente",
                        "El producto ya existe con la misma fecha. ¿Desea sumar la cantidad al existente?",
                        new Dictionary<string, Action>
                        {
                    { "Sí", () =>
                        {
                            productoExistente.Cantidad += cantidad;
                            ActualizarLista();
                            LimpiarCampos();
                        }
                    },
                    { "No", () => LimpiarCampos() }
                        }
                    ));

                    return;
                }
                else
                {
                    await PopupNavigation.Instance.PushAsync(new DynamicPopup(
                        "Producto existente con otra fecha",
                        "El producto ya existe pero con otra fecha. ¿Desea sumarlo al existente o agregarlo por separado?",
                        new Dictionary<string, Action>
                        {
                    { "Sumar al existente", () =>
                        {
                            productoExistente.Cantidad += cantidad;
                            productoExistente.Vencimiento = fechaVencimiento;
                            ActualizarLista();
                            LimpiarCampos();
                        }
                    },
                    { "Agregar por separado", () =>
                        {
                            detalle.Add(new DetalleScann
                            {
                                UPC = upc,
                                Descripcion = descripcion,
                                Cantidad = cantidad,
                                Vencimiento = fechaVencimiento
                            });

                            ActualizarLista();
                            LimpiarCampos();
                        }
                    }
                        }
                    ));

                    return;
                }
            }

            detalle.Add(new DetalleScann
            {
                UPC = upc,
                Descripcion = descripcion,
                Cantidad = cantidad,
                Vencimiento = fechaVencimiento
            });

            ActualizarLista();
            LimpiarCampos();
            ListViewDetalle.ItemSelected += ListViewDetalle_ItemSelected;
        }


        private void ActualizarLista()
        {
            ListViewDetalle.ItemsSource = null;
            ListViewDetalle.ItemsSource = detalle;
        }

        private void LimpiarCampos()
        {
            UPCScann.Text = string.Empty;
            DescripcionScann.Text = string.Empty;
            CantidadScann.Text = string.Empty;

            UPCScann.IsReadOnly = false;
            DescripcionScann.IsReadOnly = false;
            Limpiar.IsVisible = false;
        }

        private async void ListViewDetalle_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem is DetalleScann productoSeleccionado)
            {
                await PopupNavigation.Instance.PushAsync(new DynamicPopup(
                    "Opciones de Producto",
                    "¿Qué acción desea realizar?",
                    new Dictionary<string, Action>
                    {
                { "Editar", async () => await ModificarProducto(productoSeleccionado) },
                { "Eliminar", () => EliminarProducto(productoSeleccionado) },
                { "Cancelar", () => { } }
                    }
                ));

                ListViewDetalle.SelectedItem = null;
            }
        }

        private async Task ModificarProducto(DetalleScann producto)
        {
            var tcs = new TaskCompletionSource<bool>();

            var cantidadEntry = new Entry
            {
                Placeholder = "Nueva cantidad",
                Keyboard = Keyboard.Numeric,
                Text = producto.Cantidad.ToString("0.00", CultureInfo.InvariantCulture) 
            };

            var fechaPicker = new DatePicker
            {
                Date = DateTime.TryParse(producto.Vencimiento, out DateTime fecha) ? fecha : DateTime.Today,
                Format = "dd/MM/yyyy"
            };

            var layout = new StackLayout
            {
                Children =
        {
            new Label { Text = $"UPC: {producto.UPC}", FontAttributes = FontAttributes.Bold, FontSize = 16 },
            new Label { Text = $"Descripción: {producto.Descripcion}", FontSize = 14 },
            new Label { Text = "Nueva Cantidad:", FontSize = 14 },
            cantidadEntry,
            new Label { Text = "Nueva Fecha de Vencimiento:", FontSize = 14 },
            fechaPicker
        }
            };

            var popup = new DynamicPopup(
                "Modificar Producto",
                "",
                new Dictionary<string, Action>
                {
            { "Aceptar", () =>
                {
                    if (double.TryParse(cantidadEntry.Text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double nuevaCantidad) && nuevaCantidad > 0)
                    {
                        producto.Cantidad = Math.Round(nuevaCantidad, 2); 
                        producto.Vencimiento = fechaPicker.Date.ToString("dd/MM/yyyy");

                        ActualizarLista();

                        tcs.SetResult(true);
                    }
                    else
                    {
                        tcs.SetResult(false);
                    }
                }
            },
            { "Cancelar", () => tcs.SetResult(false) }
                },
                layout
            );

            await PopupNavigation.Instance.PushAsync(popup);
            await tcs.Task;
        }


        private async void EliminarProducto(DetalleScann producto)
        {
            await PopupNavigation.Instance.PushAsync(new DynamicPopup(
                "Confirmar Eliminación",
                $"¿Seguro que desea eliminar {producto.Descripcion}?",
                new Dictionary<string, Action>
                {
                    { "Sí", () =>
                        {
                            detalle.Remove(producto);
                            ActualizarLista();
                        }
                    },
                    { "No", () => { } }
                }
            ));
        }

        private void ContinuarDetalle_Clicked(object sender, EventArgs e)
        {
            try
            {
                if (detalle == null || !detalle.Any())
                {
                    PopupNavigation.Instance.PushAsync(new DynamicPopup(
                        "Lista Vacía",
                        "No hay productos en el detalle.",
                        new Dictionary<string, Action> { { "Aceptar", () => { } } }
                    ));
                    return;
                }

                if (idUsuario == 0)
                {
                    idUsuario = 0;
                }

                if (string.IsNullOrWhiteSpace(NombreUsuario))
                {
                    if (!string.IsNullOrWhiteSpace(Nombre.Text))
                    {
                        NombreUsuario = Nombre.Text.Trim();
                    }
                    else
                    {
                        PopupNavigation.Instance.PushAsync(new DynamicPopup(
                            "Error",
                            "Debe ingresar el nombre de quien reporta.",
                            new Dictionary<string, Action> { { "Aceptar", () => { } } }
                        ));
                        return;
                    }
                }

                using (var connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                {
                    connection.Open();

                    DateTime fechaActual = DateTime.Now;
                    DateTime fechaMesAtras = fechaActual.AddMonths(-1); 

                    foreach (var p in detalle)
                    {
                        if (string.IsNullOrWhiteSpace(p.UPC) ||
                            string.IsNullOrWhiteSpace(p.Descripcion) ||
                            string.IsNullOrWhiteSpace(p.Vencimiento) ||
                            p.Cantidad <= 0)
                        {
                            continue;
                        }

                        DateTime fechaVencimiento;
                        bool fechaValida = DateTime.TryParseExact(
                            p.Vencimiento, "dd/MM/yyyy",
                            CultureInfo.InvariantCulture, DateTimeStyles.None,
                            out fechaVencimiento);

                        if (!fechaValida)
                        {
                            PopupNavigation.Instance.PushAsync(new DynamicPopup(
                                "Error",
                                $"Formato de fecha incorrecto: {p.Vencimiento} en el producto {p.UPC}.",
                                new Dictionary<string, Action> { { "Aceptar", () => { } } }
                            ));
                            continue;
                        }

                        string selectQuery = @"
                                             SELECT IdDepartamentos, IdProveedores, Existencia, Costo 
                                             FROM productos 
                                             WHERE UPC = @UPC
                                             LIMIT 1;";

                        int idDepartamento = 0;
                        int idProveedor = 0;
                        double existencia = 0;
                        double costo = 0;

                        using (var selectCommand = new MySqlCommand(selectQuery, connection))
                        {
                            selectCommand.Parameters.AddWithValue("@UPC", p.UPC.Trim());

                            using (var reader = selectCommand.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    idDepartamento = reader.GetInt32("IdDepartamentos");
                                    idProveedor = reader.GetInt32("IdProveedores");
                                    existencia = reader.GetDouble("Existencia");
                                    costo = reader.GetDouble("Costo");
                                }
                            }
                        }

                        string ventasQuery = @"
                    SELECT SUM(cantidad) AS VentasMensuales
                    FROM ventasdiarias
                    WHERE UPC = @UPC
                    AND fecha BETWEEN @FechaMesAtras AND @FechaActual;";

                        double ventasMensuales = 0;

                        using (var ventasCommand = new MySqlCommand(ventasQuery, connection))
                        {
                            ventasCommand.Parameters.AddWithValue("@UPC", p.UPC.Trim());
                            ventasCommand.Parameters.AddWithValue("@FechaMesAtras", fechaMesAtras.ToString("yyyy-MM-dd"));
                            ventasCommand.Parameters.AddWithValue("@FechaActual", fechaActual.ToString("yyyy-MM-dd"));

                            var result = ventasCommand.ExecuteScalar();
                            if (result != DBNull.Value && result != null)
                            {
                                ventasMensuales = Convert.ToDouble(result);
                            }
                        }

                        string insertDetalleQuery = @"
                    INSERT INTO reportefechascortas (UPC, Descripcion, Cantidad, FechaVencimiento, IdUsuario, Encargado, 
                    IdSucursal, NombreSucursal, IdDepartamento, IdProveedor, Existencia, Costo, FechaHoraReporte, Ventas) 
                    VALUES (@UPC, @Descripcion, @Cantidad, @FechaVencimiento, @IdUsuario, @Encargado, 
                    @IdSucursal, @NombreSucursal, @IdDepartamento, @IdProveedor, @Existencia, @Costo, @FechaHoraReporte, @VentasMensuales);";

                        using (var insertDetalleCommand = new MySqlCommand(insertDetalleQuery, connection))
                        {
                            insertDetalleCommand.Parameters.AddWithValue("@UPC", p.UPC.Trim());
                            insertDetalleCommand.Parameters.AddWithValue("@Descripcion", p.Descripcion.Trim());
                            insertDetalleCommand.Parameters.AddWithValue("@Cantidad", p.Cantidad);
                            insertDetalleCommand.Parameters.AddWithValue("@FechaVencimiento", fechaVencimiento.ToString("yyyy-MM-dd"));

                            insertDetalleCommand.Parameters.AddWithValue("@IdUsuario", idUsuario);
                            insertDetalleCommand.Parameters.AddWithValue("@Encargado", NombreUsuario);
                            insertDetalleCommand.Parameters.AddWithValue("@IdSucursal", idSucursal);
                            insertDetalleCommand.Parameters.AddWithValue("@NombreSucursal", nombreSucursal);

                            insertDetalleCommand.Parameters.AddWithValue("@IdDepartamento", idDepartamento);
                            insertDetalleCommand.Parameters.AddWithValue("@IdProveedor", idProveedor);
                            insertDetalleCommand.Parameters.AddWithValue("@Existencia", existencia);
                            insertDetalleCommand.Parameters.AddWithValue("@Costo", costo);
                            insertDetalleCommand.Parameters.AddWithValue("@FechaHoraReporte", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                            insertDetalleCommand.Parameters.AddWithValue("@VentasMensuales", ventasMensuales);

                            insertDetalleCommand.ExecuteNonQuery();
                        }
                    }
                }

                PopupNavigation.Instance.PushAsync(new DynamicPopup(
                    "Éxito",
                    "El reporte fue enviado correctamente.",
                    new Dictionary<string, Action> { { "Aceptar", () => { } } }
                ));

                detalle.Clear();
                ActualizarLista();

                Nombre.Text = string.Empty;
                UPCScann.Text = string.Empty;
                DescripcionScann.Text = string.Empty;
                CantidadScann.Text = string.Empty;

                Nombre.IsReadOnly = false;
                UPCScann.IsReadOnly = false;
                DescripcionScann.IsReadOnly = false;
                Limpiar.IsVisible = false;

            }
            catch (Exception ex)
            {
                PopupNavigation.Instance.PushAsync(new DynamicPopup(
                    "Error",
                    $"Ocurrió un error al insertar en el reporte: {ex.Message}",
                    new Dictionary<string, Action> { { "Aceptar", () => { } } }
                ));
            }
        }


        private async void VaciarDetalle_Clicked(object sender, EventArgs e)
        {
            await PopupNavigation.Instance.PushAsync(new DynamicPopup(
                "Confirmar",
                "¿Está seguro que desea vaciar el detalle del reporte?",
                new Dictionary<string, Action>
                {
            {
                "Aceptar",
                () =>
                {
                    detalle.Clear();
                    ListViewDetalle.ItemsSource = null;
                    ListViewDetalle.ItemsSource = detalle;
                }
            },
            { "Cancelar", () => { } }
                }
            ));
        }

        private void CargarEncargados(DateTime fechaSeleccionada)
        {
            string fechaMysql = fechaSeleccionada.ToString("yyyy-MM-dd");
            var encargadosList = new List<string>();

            using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
            {
                try
                {
                    connection.Open();

                    string query = @"
                                    SELECT DISTINCT Encargado 
                                    FROM reportefechascortas 
                                    WHERE DATE(FechaHoraReporte) = @FechaSeleccionada;";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@FechaSeleccionada", fechaMysql);

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string encargado = reader["Encargado"].ToString().Trim();
                                if (!string.IsNullOrWhiteSpace(encargado))
                                {
                                    encargadosList.Add(encargado);
                                }
                            }
                        }
                    }

                    if (encargadosList.Any())
                    {
                        UsuariosPicker.ItemsSource = encargadosList;
                    }
                    else
                    {
                        UsuariosPicker.ItemsSource = new List<string> { "No hay registros" };
                    }
                }
                catch (Exception ex)
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        PopupNavigation.Instance.PushAsync(new DynamicPopup(
                            "Error",
                            $"Hubo un problema al cargar los encargados: {ex.Message}",
                            new Dictionary<string, Action> { { "Aceptar", () => { } } }
                        ));
                    });
                }
            }
        }

        private void CargarReporte(DateTime fechaSeleccionada)
        {
            var loadingPopup = new LoadingPopup();
            string fechaMysql = fechaSeleccionada.ToString("yyyy-MM-dd");
            var reporteLista = new List<Reporte>();

            using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
            {
                try
                {
                    Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(loadingPopup);

                    connection.Open();

                    string query = @"
                                    SELECT 
                                        r.UPC, 
                                        r.Descripcion, 
                                        r.Existencia, 
                                        r.Costo, 
                                        r.FechaVencimiento, 
                                        r.Cantidad, 
                                        r.Ventas, 
                                        p.Nombre AS Proveedor, 
                                        d.Nombre AS Departamento, 
                                        r.Encargado, 
                                        r.IdSucursal, 
                                        r.NombreSucursal,
                                        (r.Cantidad * r.Costo) AS SubCosto,
                                        DATEDIFF(r.FechaVencimiento, CURDATE()) AS DiasDeVida,
                                        CASE 
                                            WHEN r.Ventas IS NOT NULL THEN (r.Ventas / 30) * DATEDIFF(r.FechaVencimiento, CURDATE())
                                            ELSE 0
                                        END AS Proyeccion,
                                        (r.Existencia - 
                                            CASE 
                                                WHEN r.Ventas IS NOT NULL THEN (r.Ventas / 30) * DATEDIFF(r.FechaVencimiento, CURDATE())
                                                ELSE 0
                                            END
                                        ) AS InventarioFinal,
                                        CASE 
                                            WHEN (r.Existencia - 
                                                CASE 
                                                    WHEN r.Ventas IS NOT NULL THEN (r.Ventas / 30) * DATEDIFF(r.FechaVencimiento, CURDATE())
                                                    ELSE 0
                                                END
                                            ) < 0.01 THEN 'SI'
                                            ELSE 'NO'
                                        END AS SeDesaloja
                                    FROM reportefechascortas r
                                    LEFT JOIN proveedores p ON r.IdProveedor = p.Id
                                    LEFT JOIN departamentos d ON r.IdDepartamento = d.Id
                                    WHERE DATE(r.FechaHoraReporte) = @FechaSeleccionada;";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@FechaSeleccionada", fechaMysql);

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                reporteLista.Add(new Reporte
                                {
                                    UPC = reader["UPC"].ToString().Trim(),
                                    Descripcion = reader["Descripcion"].ToString().Trim(),
                                    Existencia = reader.IsDBNull(reader.GetOrdinal("Existencia")) ? 0 : Convert.ToDouble(reader["Existencia"]),
                                    Costo = reader.IsDBNull(reader.GetOrdinal("Costo")) ? 0 : Convert.ToDouble(reader["Costo"]),
                                    SubCosto = reader.IsDBNull(reader.GetOrdinal("SubCosto")) ? 0 : Convert.ToDouble(reader["SubCosto"]),
                                    FechaVencimiento = reader.IsDBNull(reader.GetOrdinal("FechaVencimiento"))
                                        ? "" : Convert.ToDateTime(reader["FechaVencimiento"]).ToString("yyyy-MM-dd"),
                                    DiasDeVida = reader.IsDBNull(reader.GetOrdinal("DiasDeVida")) ? 0 : Convert.ToInt32(reader["DiasDeVida"]),
                                    Ventas = reader.IsDBNull(reader.GetOrdinal("Ventas")) ? 0 : Convert.ToDouble(reader["Ventas"]),
                                    Proyeccion = reader.IsDBNull(reader.GetOrdinal("Proyeccion")) ? 0 : Convert.ToDouble(reader["Proyeccion"]),
                                    InventarioFinal = reader.IsDBNull(reader.GetOrdinal("InventarioFinal")) ? 0 : Convert.ToDouble(reader["InventarioFinal"]),
                                    SeDesaloja = reader["SeDesaloja"].ToString(),
                                    Departamento = reader["Departamento"].ToString(),
                                    Proveedor = reader["Proveedor"].ToString(),
                                    Encargado = reader["Encargado"].ToString().Trim()
                                });
                            }
                        }
                    }

                    if (PopupNavigation.Instance.PopupStack.Contains(loadingPopup))
                    {
                        PopupNavigation.Instance.PopAsync();
                    }

                    if (reporteLista.Any())
                    {
                        ListViewReporte.ItemsSource = reporteLista;
                    }
                    else
                    {
                        ListViewReporte.ItemsSource = new List<Reporte>();
                        PopupNavigation.Instance.PushAsync(new DynamicPopup(
                            "Sin Resultados",
                            "No hay reportes para la fecha seleccionada.",
                            new Dictionary<string, Action> { { "Aceptar", () => { } } }
                        ));
                    }
                }
                catch (Exception ex)
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        PopupNavigation.Instance.PushAsync(new DynamicPopup(
                            "Error",
                            $"Ocurrió un problema al cargar el reporte: {ex.Message}",
                            new Dictionary<string, Action> { { "Aceptar", () => { } } }
                        ));
                    });
                }
            }
        }

        private void FechaReporte_DateSelected(object sender, DateChangedEventArgs e)
        {
            CargarEncargados(e.NewDate);
            CargarReporte(e.NewDate);
        }

        private async void ExportarReporte()
        {
            try
            {
                if (ListViewReporte.ItemsSource == null || !ListViewReporte.ItemsSource.Cast<object>().Any())
                {
                    await PopupNavigation.Instance.PushAsync(new DynamicPopup(
                        "Error",
                        "No hay datos para exportar.",
                        new Dictionary<string, Action> { { "Aceptar", () => { } } }
                    ));
                    return;
                }

                var datos = ListViewReporte.ItemsSource.Cast<Reporte>().ToList();

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Reporte");

                    worksheet.Cells["A1"].Value = "Corporación Bodegona";
                    worksheet.Cells["A1"].Style.Font.Size = 14;
                    worksheet.Cells["A1"].Style.Font.Bold = true;

                    worksheet.Cells["A2"].Value = "Reporte General de Fechas de Vencimiento";
                    worksheet.Cells["A2"].Style.Font.Size = 12;
                    worksheet.Cells["A2"].Style.Font.Bold = true;

                    worksheet.Cells["A4"].Value = "Sucursal:";
                    worksheet.Cells["A5"].Value = "Fecha de Reporte:";
                    worksheet.Cells["A6"].Value = "Reportado por:";
                    worksheet.Cells["A7"].Value = "Departamento:";

                    worksheet.Cells["B4"].Value = nombreSucursal;
                    worksheet.Cells["B5"].Value = DateTime.Now.ToString("dd/MM/yyyy");
                    worksheet.Cells["B6"].Value = NombreUsuario;
                    worksheet.Cells["B7"].Value = "ABARROTES";  

                    worksheet.Cells["A4:A7"].Style.Font.Bold = true;

                    string[] encabezados = {
                                                "Escanear", "UPC", "Descripción", "Existencia Física", "Costo", "SubCosto",
                                                "Fecha de vencimiento", "Días de Vida", "Venta de los últimos 30 días",
                                                "Proyección de Ventas", "Inventario Final", "¿Se Desaloja?", "Departamento", "Proveedor", "Encargado de Área"
                                            };

                    for (int i = 0; i < encabezados.Length; i++)
                    {
                        worksheet.Cells[10, i + 1].Value = encabezados[i];
                        worksheet.Cells[10, i + 1].Style.Font.Bold = true;
                        worksheet.Cells[10, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[10, i + 1].Style.Fill.BackgroundColor.SetColor(Color.Blue);
                        worksheet.Cells[10, i + 1].Style.Font.Color.SetColor(Color.White);
                    }

                    int row = 11;
                    foreach (var item in datos)
                    {
                        worksheet.Cells[row, 2].Value = item.UPC;
                        worksheet.Cells[row, 3].Value = item.Descripcion;
                        worksheet.Cells[row, 4].Value = item.Existencia;
                        worksheet.Cells[row, 5].Value = item.Costo;
                        worksheet.Cells[row, 6].Value = item.SubCosto;

                        DateTime fecha;
                        if (DateTime.TryParse(item.FechaVencimiento, out fecha))
                        {
                            worksheet.Cells[row, 7].Value = fecha.ToString("dd/MM/yyyy");
                        }
                        else
                        {
                            worksheet.Cells[row, 7].Value = "Fecha inválida";
                        }

                        worksheet.Cells[row, 8].Value = item.DiasDeVida;
                        worksheet.Cells[row, 9].Value = item.Ventas;
                        worksheet.Cells[row, 10].Value = item.Proyeccion;
                        worksheet.Cells[row, 11].Value = item.InventarioFinal;
                        worksheet.Cells[row, 12].Value = item.SeDesaloja;
                        worksheet.Cells[row, 13].Value = item.Departamento;
                        worksheet.Cells[row, 14].Value = item.Proveedor;
                        worksheet.Cells[row, 15].Value = item.Encargado;

                        if (item.DiasDeVida <= 0)
                        {
                            worksheet.Cells[row, 8].Style.Fill.PatternType = ExcelFillStyle.Solid;
                            worksheet.Cells[row, 8].Style.Fill.BackgroundColor.SetColor(Color.Red);
                            worksheet.Cells[row, 8].Style.Font.Color.SetColor(Color.White);
                        }

                        if (item.InventarioFinal < 1)
                        {
                            worksheet.Cells[row, 11].Style.Fill.PatternType = ExcelFillStyle.Solid;
                            worksheet.Cells[row, 11].Style.Fill.BackgroundColor.SetColor(Color.Red);
                            worksheet.Cells[row, 11].Style.Font.Color.SetColor(Color.White);
                        }

                        if (item.SeDesaloja == "SI")
                        {
                            worksheet.Cells[row, 12].Style.Fill.PatternType = ExcelFillStyle.Solid;
                            worksheet.Cells[row, 12].Style.Fill.BackgroundColor.SetColor(Color.Green);
                            worksheet.Cells[row, 12].Style.Font.Color.SetColor(Color.White);
                        }

                        row++;
                    }

                    worksheet.Cells.AutoFitColumns();

                    string nombreArchivo = $"ReporteFechasCortas_{DateTime.Now:yyyyMMdd}.xlsx";
                    string rutaArchivo = Path.Combine(FileSystem.CacheDirectory, nombreArchivo);

                    File.WriteAllBytes(rutaArchivo, package.GetAsByteArray());

                    await PopupNavigation.Instance.PushAsync(new DynamicPopup(
                        "Éxito",
                        $"El archivo {nombreArchivo} se generó correctamente.",
                        new Dictionary<string, Action>
                        {
                            { "Abrir", () => AbrirArchivoExcel(rutaArchivo) },
                            { "Compartir", () => CompartirArchivoExcel(rutaArchivo) }
                        }
                    ));
                }
            }
            catch (Exception ex)
            {
                await PopupNavigation.Instance.PushAsync(new DynamicPopup(
                    "Error",
                    $"Ocurrió un error al exportar el archivo: {ex.Message}",
                    new Dictionary<string, Action> { { "Aceptar", () => { } } }
                ));
            }
        }

        private async void AbrirArchivoExcel(string ruta)
        {
            try
            {
                await Launcher.OpenAsync(new OpenFileRequest
                {
                    File = new ReadOnlyFile(ruta)
                });
            }
            catch (Exception ex)
            {
                await PopupNavigation.Instance.PushAsync(new DynamicPopup(
                    "Error",
                    $"No se pudo abrir el archivo: {ex.Message}",
                    new Dictionary<string, Action> { { "Aceptar", () => { } } }
                ));
            }
        }

        private async void CompartirArchivoExcel(string rutaArchivo)
        {
            try
            {
                if (!File.Exists(rutaArchivo))
                {
                    await PopupNavigation.Instance.PushAsync(new DynamicPopup(
                        "Error",
                        "No se encontró el archivo para compartir.",
                        new Dictionary<string, Action> { { "Aceptar", () => { } } }
                    ));
                    return;
                }

                await Share.RequestAsync(new ShareFileRequest
                {
                    Title = "Compartir Reporte",
                    File = new ShareFile(rutaArchivo)
                });
            }
            catch (Exception ex)
            {
                await PopupNavigation.Instance.PushAsync(new DynamicPopup(
                    "Error",
                    $"Ocurrió un error al compartir el archivo: {ex.Message}",
                    new Dictionary<string, Action> { { "Aceptar", () => { } } }
                ));
            }
        }


        private void Compartir_Clicked(object sender, EventArgs e)
        {
            ExportarReporte();
        }
    }
}