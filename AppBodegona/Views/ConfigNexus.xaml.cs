using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppBodegona.Services;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using MySqlConnector;
using ZXing.Mobile;
using Newtonsoft.Json;

namespace AppBodegona.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ConfigNexus : ContentPage
    {
        // Variables públicas para almacenar los valores
        public static string ServerAddressNexus { get; private set; }
        public static string PortNumberNexus { get; private set; }
        public static string DatabaseNameNexus { get; private set; }
        public static string UsernameNexus { get; private set; }
        public static string PasswordNexus { get; private set; }

        public ConfigNexus()
        {
            InitializeComponent();

            Server.Text = Preferences.Get("ServerAddressCentral", string.Empty);
            Port.Text = Preferences.Get("PortNumberCentral", string.Empty);
            Database.Text = Preferences.Get("DatabaseNameCentral", string.Empty);
            User.Text = Preferences.Get("UsernameCentral", string.Empty);
            Pass.Text = Preferences.Get("PasswordCentral", string.Empty);
        }

        // Sobrescribe el método OnBackButtonPressed
        protected override bool OnBackButtonPressed()
        {
            // Verificar si hay popups abiertos
            if (Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopupStack.Count > 0)
            {
                // Deja que el popup maneje el evento
                return base.OnBackButtonPressed();
            }

            // Si no hay popups, ejecuta la lógica personalizada para la página
            Device.BeginInvokeOnMainThread(async () =>
            {
                bool result = await this.DisplayAlert(
                    "Confirmar",
                    "¿Desea salir?",
                    "Sí",
                    "No");

                if (result)
                {
                    // Si el usuario elige 'Sí', cierra la aplicación
                    System.Diagnostics.Process.GetCurrentProcess().CloseMainWindow();
                }
            });

            // Indicar que hemos manejado el evento
            return true;
        }

        private async void ScanQR_Clicked(object sender, EventArgs e)
        {
            try
            {
                var scanner = new ZXing.Mobile.MobileBarcodeScanner();
                var result = await scanner.Scan();

                if (result != null)
                {
                    var decryptedJson = result.Text;

                    var configData = JsonConvert.DeserializeObject<Dictionary<string, string>>(decryptedJson);

                    // Desencriptar y asignar
                    Server.Text = EncryptionService.Decrypt(configData["Host"]);
                    Port.Text = EncryptionService.Decrypt(configData["Port"]);
                    Database.Text = EncryptionService.Decrypt(configData["Database"]);
                    User.Text = EncryptionService.Decrypt(configData["UserName"]);
                    Pass.Text = EncryptionService.Decrypt(configData["Password"]);

                    Guardar();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo leer o desencriptar el QR.\n{ex.Message}", "OK");
            }
        }

        public async void Guardar()
        {
            var loadingPopup = new LoadingPopup(); // Crear el popup del spinner

            try
            {
                // Mostrar el spinner
                if (!Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopupStack.Contains(loadingPopup))
                {
                    await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(loadingPopup);
                }

                // Asignar datos a las variables públicas
                ServerAddressNexus = Server.Text;
                PortNumberNexus = Port.Text;
                DatabaseNameNexus = Database.Text;
                UsernameNexus = User.Text;
                PasswordNexus = Pass.Text;

                var connectionString = $"Server={ServerAddressNexus};Port={PortNumberNexus};Database={DatabaseNameNexus};Uid={UsernameNexus};Pwd={PasswordNexus};";

                if (DatabaseConnection.TestConnection(connectionString))
                {
                    // Actualizar global y guardar
                    //DatabaseConnection.UpdateConnectionStringNexus(ServerAddressNexus, PortNumberNexus, DatabaseNameNexus, UsernameNexus, PasswordNexus);
                    SavePreferences();

                    await CerrarPopupSiEstaAbierto();

                    // Navegar a existencia
                    await Shell.Current.GoToAsync("///Existencia");
                }
                else
                {
                    await CerrarPopupSiEstaAbierto();
                    await DisplayAlert("Error", "No se pudo conectar a la base de datos. Por favor, verifique los datos ingresados.", "OK");
                }
            }
            catch (Exception ex)
            {
                await CerrarPopupSiEstaAbierto();
                await DisplayAlert("Error", $"Ocurrió un error: {ex.Message}", "OK");
            }
        }


        // Método helper para cerrar el popup si está abierto
        private async Task CerrarPopupSiEstaAbierto()
        {
            if (Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopupStack.Count > 0)
            {
                await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync();
            }
        }


        private void SavePreferences()
        {
            // Guarda las preferencias usando Xamarin.Essentials
            Preferences.Set("ServerAddress", ServerAddressNexus);
            Preferences.Set("PortNumber", PortNumberNexus);
            Preferences.Set("DatabaseName", DatabaseNameNexus);
            Preferences.Set("Username", UsernameNexus);
            Preferences.Set("Password", PasswordNexus);
        }

        private async void Back_Clicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("///Existencia");
        }

    }
}