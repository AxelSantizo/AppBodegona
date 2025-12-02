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
    public partial class ConfigCentral : ContentPage
    {
        public static string ServerAddressCentral { get; private set; }
        public static string PortNumberCentral { get; private set; }
        public static string DatabaseNameCentral { get; private set; }
        public static string UsernameCentral { get; private set; }
        public static string PasswordCentral { get; private set; }

        public ConfigCentral()
        {
            InitializeComponent();

            // Recuperar las preferencias al iniciar
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

                // Asigna los valores de las entradas a las variables públicas
                ServerAddressCentral = Server.Text;
                PortNumberCentral = Port.Text;
                DatabaseNameCentral = Database.Text;
                UsernameCentral = User.Text;
                PasswordCentral = Pass.Text;

                // Probar la conexión antes de guardar
                var connectionString = $"Server={ServerAddressCentral};Port={PortNumberCentral};Database={DatabaseNameCentral};Uid={UsernameCentral};Pwd={PasswordCentral};";

                if (DatabaseConnection.TestConnection(connectionString))
                {
                    // Guarda los datos
                    //DatabaseConnection.UpdateConnectionStringCentral(ServerAddressCentral, PortNumberCentral, DatabaseNameCentral, UsernameCentral, PasswordCentral);
                    SavePreferences();

                    // Cierra el popup
                    await CerrarPopupSiEstaAbierto();

                    // Navega a Existencia
                    await Shell.Current.GoToAsync("///Existencia");
                }
                else
                {
                    await CerrarPopupSiEstaAbierto();
                    await DisplayAlert("Error", "No se pudo conectar a la base de datos. Verifique los datos ingresados.", "OK");
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
            Preferences.Set("ServerAddress", ServerAddressCentral);
            Preferences.Set("PortNumber", PortNumberCentral);
            Preferences.Set("DatabaseName", DatabaseNameCentral);
            Preferences.Set("Username", UsernameCentral);
            Preferences.Set("Password", PasswordCentral);
        }

        private async void Back_Clicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("///Existencia");
        }
    }
}