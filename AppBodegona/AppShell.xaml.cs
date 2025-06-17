using AppBodegona.Views;
using System;
using System.Timers;
using Xamarin.Forms;

namespace AppBodegona
{
    public partial class AppShell : Xamarin.Forms.Shell
    {
        private const int InactivityThreshold = 1200000;
        private Timer _timer;
        private DateTime _lastInteractionTime;

        public static readonly BindableProperty NombreUsuarioProperty =
        BindableProperty.Create(nameof(NombreUsuario), typeof(string), typeof(AppShell), default(string));
        public string NombreUsuario
        {
            get => (string)GetValue(NombreUsuarioProperty);
            set => SetValue(NombreUsuarioProperty, value);
        }

        public static readonly BindableProperty UsuarioProperty =
        BindableProperty.Create(nameof(Usuario), typeof(string), typeof(AppShell), default(string));
        public string Usuario
        {
            get => (string)GetValue(UsuarioProperty);
            set => SetValue(UsuarioProperty, value);
        }

        public static readonly BindableProperty IdProperty =
        BindableProperty.Create(nameof(Id), typeof(string), typeof(AppShell), default(string));
        public new string Id
        {
            get => (string)GetValue(IdProperty);
            set => SetValue(IdProperty, value);
        }

        public static readonly BindableProperty IdNivelProperty =
        BindableProperty.Create(nameof(IdNivel), typeof(string), typeof(AppShell), default(string));
        public string IdNivel
        {
            get => (string)GetValue(IdNivelProperty);
            set => SetValue(IdNivelProperty, value);
        }

        public static readonly BindableProperty IsLoggedInProperty =
        BindableProperty.Create(nameof(IsLoggedIn), typeof(bool), typeof(AppShell), default(bool), propertyChanged: OnIsLoggedInChanged);
        public bool IsLoggedIn
        {
            get => (bool)GetValue(IsLoggedInProperty);
            set => SetValue(IsLoggedInProperty, value);
        }

        public static readonly BindableProperty IsNotLoggedInProperty =
        BindableProperty.Create(nameof(IsNotLoggedIn), typeof(bool), typeof(AppShell), default(bool), propertyChanged: OnNotIsLoggedInChanged);
        public bool IsNotLoggedIn
        {
            get => (bool)GetValue(IsNotLoggedInProperty);  
            set => SetValue(IsNotLoggedInProperty, value);
        }

        public AppShell()
        {
            InitializeComponent();
            RegisterRoutes();
            StartInactivityTimer();
            Usuario = string.Empty;
            IsLoggedIn = false;
            IsNotLoggedIn = true;
            BindingContext = this;
        }

        private void RegisterRoutes()
        {
            Routing.RegisterRoute("Login", typeof(Login));
            Routing.RegisterRoute("IPConfig", typeof(IPConfig));
        }

        private void StartInactivityTimer()
        {
            _timer = new Timer();
            _timer.Elapsed += OnTimedEvent;
            _timer.AutoReset = true;
        }

        private static void OnIsLoggedInChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var shell = (AppShell)bindable;
            bool isLoggedIn = (bool)newValue;

            shell.SetValue(IsNotLoggedInProperty, !isLoggedIn);

            if (isLoggedIn)
            {
                shell.ResetInactivityTimer();
            }
            else
            {
                shell._timer.Stop();
            }
        }

        private static void OnNotIsLoggedInChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var shell = (AppShell)bindable;
            bool isNotLoggedIn = (bool)newValue;

            shell.IsLoggedIn = !isNotLoggedIn;
        }

        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            if (IsLoggedIn && (DateTime.Now - _lastInteractionTime).TotalMilliseconds > InactivityThreshold)
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    _timer.Stop();
                    IsLoggedIn = false;
                    Usuario = string.Empty;
                    Id = string.Empty;
                    IdNivel = string.Empty;
                    await DisplayAlert("Alerta", "Sesión finalizada por inactividad", "Ok");
                    await Shell.Current.GoToAsync($"//Existencia");
                });
            }
        }

        public void ResetInactivityTimer()
        {
            if (IsLoggedIn)
            {
                _lastInteractionTime = DateTime.Now;
                _timer.Start();
            }
        }

        private async void Button_Clicked(object sender, EventArgs e)
        {
            IsLoggedIn = false;
            Usuario = string.Empty;
            Id = string.Empty;
            IdNivel = string.Empty;
            await Shell.Current.GoToAsync($"//Existencia");
        }

        private void Entry_TextChanged(object sender, TextChangedEventArgs e)
        {
            IsLoggedIn = true;
        }

        private async void Button_Clicked_1(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync($"//Existencia");
            NavigationService.DestinationPage = "Existencia";
            await Shell.Current.GoToAsync("Login");
        }

    }

    public static class NavigationService
    {
        private static string _destinationPage;

        public static string DestinationPage
        {
            get => _destinationPage;
            set => _destinationPage = value;
        }
    }
}
