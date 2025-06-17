using AppBodegona.Services;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using MySqlConnector;
using Rg.Plugins.Popup.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AppBodegona.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Login : ContentPage
    {
        public Entry Entry { get; set; }
        public Button Button { get; set; }

        public Login()
        {
            InitializeComponent();
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

        private async void OnIngresarClicked(object sender, EventArgs e)
        {
            var loadingPopup = new LoadingPopup();

            try
            {
                await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PushAsync(loadingPopup);

                string usertext = Usuario.Text;
                string passtext = Contraseña.Text;

                if (string.IsNullOrEmpty(usertext) && string.IsNullOrEmpty(passtext))
                {
                    await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync();
                    var popup = new DynamicPopup(
                    "Error",
                    "Por favor ingrese su usuario y contraseña.",
                    new Dictionary<string, Action>
                    {
                        { "OK", () => {} }
                    });

                    await PopupNavigation.Instance.PushAsync(popup);
                    return;
                }
                else if (string.IsNullOrEmpty(usertext))
                {
                    await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync();
                    var popup = new DynamicPopup(
                    "Error",
                    "Por favor ingrese su usuario.",
                    new Dictionary<string, Action>
                    {
                        { "OK", () => {} }
                    });

                    await PopupNavigation.Instance.PushAsync(popup);

                    Usuario.Focus();
                    return;
                }
                else if (string.IsNullOrEmpty(passtext))
                {
                    await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync(); var popup = new DynamicPopup(
                    "Error",
                    "Por favor ingrese su contraseña.",
                    new Dictionary<string, Action>
                    {
                        { "OK", () => {} }
                    });

                    await PopupNavigation.Instance.PushAsync(popup);
                    Contraseña.Focus();
                    return;
                }

                string queryUserCheck = "SELECT COUNT(*) FROM usuarios WHERE Usuario = @usuario";
                string query = $"SELECT Id, IdNivel, NombreCompleto FROM usuarios WHERE Usuario = @usuario AND Password = @contraseña";

                using (MySqlConnection connection = new MySqlConnection(DatabaseConnection.ConnectionString))
                {
                    connection.Open();

                    using (MySqlCommand commandUserCheck = new MySqlCommand(queryUserCheck, connection))
                    {
                        commandUserCheck.Parameters.AddWithValue("@usuario", usertext);
                        int userCount = Convert.ToInt32(commandUserCheck.ExecuteScalar());

                        if (userCount == 0)
                        {
                            await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync();
                            var popup = new DynamicPopup(
                            "Error",
                            "Usuario incorrecto.",
                            new Dictionary<string, Action>
                            {
                                { "OK", () => {} }
                            });

                            await PopupNavigation.Instance.PushAsync(popup);
                            Usuario.Focus();
                            return;
                        }
                    }

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@usuario", usertext);
                        command.Parameters.AddWithValue("@contraseña", passtext);
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int idNivel = Convert.ToInt32(reader["IdNivel"]);
                                string Nombre = reader["NombreCompleto"].ToString();
                                string IdUsuario = reader["Id"].ToString();

                                if (Application.Current.MainPage is AppShell appShell)
                                {
                                    appShell.NombreUsuario = usertext;
                                    appShell.Usuario = Nombre;
                                    appShell.Id = IdUsuario;
                                    appShell.IdNivel = idNivel.ToString(); 
                                    appShell.IsLoggedIn = true;
                                }

                                await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync();

                                if (!string.IsNullOrEmpty(NavigationService.DestinationPage))
                                {
                                    await Shell.Current.GoToAsync($"///{NavigationService.DestinationPage}");
                                }
                                else
                                {
                                    await Shell.Current.GoToAsync("///Existencia");
                                }

                                Usuario.Text = string.Empty;
                                Contraseña.Text = string.Empty;
                            }
                            else
                            {
                                await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync();
                                var popup = new DynamicPopup(
                                    "Error",
                                    "Usuario o contraseña incorrectos.",
                                    new Dictionary<string, Action>
                                    {
                                        { "OK", () => {} }
                                    });

                                await PopupNavigation.Instance.PushAsync(popup);
                                Contraseña.Focus();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync();
                var popup = new DynamicPopup(
                "Error",
                $"Ocurrió un error: {ex.Message}",
                new Dictionary<string, Action>
                {
                    { "OK", () => {} }
                });

                await PopupNavigation.Instance.PushAsync(popup);
            }
        }

        private async void Cancelar_Clicked(object sender, EventArgs e)
        {
            Usuario.Text = string.Empty;
            Contraseña.Text = string.Empty;

            await Shell.Current.GoToAsync($"///{NavigationService.DestinationPage}");
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

        private void Usuario_Completed(object sender, EventArgs e)
        {
            VerificarCampos();
        }

        private void Contraseña_Completed(object sender, EventArgs e)
        {
            VerificarCampos();
        }

        private void VerificarCampos()
        {
            if (string.IsNullOrEmpty(Usuario.Text))
            {
                Usuario.Focus();
            }
            else if (string.IsNullOrEmpty(Contraseña.Text))
            {
                Contraseña.Focus();
            }
            else
            {
                OnIngresarClicked(this, EventArgs.Empty);
            }
        }
    }
}
