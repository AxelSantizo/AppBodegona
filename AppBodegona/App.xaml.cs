using AppBodegona.Services;
using AppBodegona.Views;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System.Diagnostics;

namespace AppBodegona
{
    public partial class App : Application
    {

        private const string UpdateCheckUrl = "https://raw.githubusercontent.com/Santizo00/AppBodegona1/master/version.json";

        public App()
        {
            InitializeComponent();
            MainPage = new AppShell();
        }

        protected async override void OnStart()
        {
            base.OnStart();

            // Verificar si hay actualizaciones disponibles
            await CheckForUpdates();

            // Verificar si es la primera vez que se inicia la aplicación
            bool isFirstLaunch = Preferences.Get("IsFirstLaunch", true);
            if (isFirstLaunch)
            {
                // Mostrar la página de configuración
                await MainPage.Navigation.PushAsync(new IPConfig());

                // Establecer el indicador a false para que no se muestre de nuevo
                Preferences.Set("IsFirstLaunch", false);
            }
        }

        protected async override void OnSleep()
        {
            base.OnSleep();

            // Verificar si hay actualizaciones disponibles cada vez que la app se reanuda
            await CheckForUpdates();
        }

        protected async override void OnResume()
        {
            base.OnResume();

            // Verificar si hay actualizaciones disponibles cada vez que la app se reanuda
            await CheckForUpdates();
        }

        public async Task NavigateToLogin()
        {
            await MainPage.Navigation.PushAsync(new Login());
        }

        public async Task CheckForUpdates()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var response = await client.GetStringAsync(UpdateCheckUrl);
                    var json = JObject.Parse(response);

                    // Obtener la versión más reciente del JSON
                    string latestVersion = json["latestVersion"].ToString();
                    string updateUrl = json["updateUrl"].ToString();

                    // Obtener la versión actual de la app
                    string currentVersionString = VersionTracking.CurrentVersion; // Obtener como string
                    Version currentVersion = new Version(currentVersionString);  // Convertir a Version
                    Version latestVersionParsed = new Version(latestVersion);  // Convertir la versión del JSON a Version

                    // Comparar las versiones
                    if (latestVersionParsed > currentVersion)
                    {
                        // Mostrar alerta de actualización
                        bool update = await MainPage.DisplayAlert(
                            "Actualización disponible",
                            "Hay una nueva versión disponible. ¿Desea actualizar ahora?",
                            "Actualizar", "Cancelar");

                        if (update)
                        {
                            // Abrir el enlace de actualización en el navegador
                            await Launcher.OpenAsync(new Uri(updateUrl));
                        }
                        else
                        {
                            // Si el usuario cancela, cierra la aplicación
                            CloseApplication();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Manejar error en caso de que no se pueda obtener la versión
                await MainPage.DisplayAlert("Error", $"Error al verificar actualizaciones: {ex.Message}", "OK");
            }
        }

        // Método para cerrar la aplicación
        private void CloseApplication()
        {
            System.Diagnostics.Process.GetCurrentProcess().Kill(); // Cierra la aplicación
        }


    }
}
