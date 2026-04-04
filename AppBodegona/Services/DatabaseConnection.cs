using System;
using Xamarin.Essentials;
using MySqlConnector;

namespace AppBodegona.Services
{
    public static class DatabaseConnection
    {
        public static string ConnectionString { get; private set; }
        public static string ConnectedServer { get; private set; }
        public static string ConnectionCentral { get; private set; }
        public static string ConnectionNexus { get; private set; }
        public static string DeviceName { get; private set; }

        // Si esta en true, se ignoran configuraciones por scanner/preferences.
        public static bool ForceLocalConnections { get; set; } = false;

        //// Configuracion local compartida para todas las conexiones
        //private static readonly string LocalServer = "192.168.0.3";
        //private static readonly string LocalPort = "3306";
        //private static readonly string LocalUsername = "root";
        //private static readonly string LocalPassword = "123456";

        //// Bases por modulo
        //private static readonly string LocalDatabaseSucursal = "superpos";
        //private static readonly string LocalDatabaseCentral = "facturas_compras";
        //private static readonly string LocalDatabaseNexus = "dbsucursales";

        static DatabaseConnection()
        {
            InitializeConnectionString();
            InitializeConnectionCentral();
            InitializeConnectionNexus();
            DeviceName = ObtenerNombreDispositivo();
        }

        public static string ObtenerNombreDispositivo()
        {
            try
            {
                string nombreDispositivo = DeviceInfo.Name;

                return nombreDispositivo;
            }
            catch (Exception ex)
            {
                return $"Error al obtener el nombre del dispositivo: {ex.Message}";
            }
        }

        //public static string server = "192.168.0.7";
        //public static string username = "reportes";
        //public static string password = "bode.24451988";
        //public static string port = "3306";

        public static void InitializeConnectionString()
        {
            // Configuracion local activa (deshabilitada)
            //var server = LocalServer;
            //var port = LocalPort;
            //var database = LocalDatabaseSucursal;
            //var username = LocalUsername;
            //var password = LocalPassword;

            // Configuracion por preferencias/productiva (deshabilitada temporalmente)
            //var server = "172.30.67.25";
            //var username = "reportes";
            //var password = "bode.24451988";
            //var port = "3306";
            //var database = "superpos";

            var server = Preferences.Get("ServerAddress", string.Empty);
            var port = Preferences.Get("PortNumber", string.Empty);
            var database = Preferences.Get("DatabaseName", string.Empty);
            var username = Preferences.Get("Username", string.Empty);
            var password = Preferences.Get("Password", string.Empty);


            if (!string.IsNullOrEmpty(server) && !string.IsNullOrEmpty(port) && !string.IsNullOrEmpty(database) && !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                var connectionString = $"Server={server};Port={port};Database={database};Uid={username};Pwd={password};";
                if (TestConnection(connectionString))
                {
                    ConnectionString = connectionString;
                    ConnectedServer = server;
                    return;
                }
            }
            ConnectionString = string.Empty;
        }

        public static void InitializeConnectionCentral()
        {
            // Configuracion local activa (deshabilitada)
            //var server = LocalServer;
            //var username = LocalUsername;
            //var password = LocalPassword;
            //var port = LocalPort;
            //var database = LocalDatabaseCentral;

            // Configuracion productiva (deshabilitada temporalmente)
            //var server = Preferences.Get("ServerAddressCentral", string.Empty);
            //var port = Preferences.Get("PortNumberCentral", string.Empty);
            //var database = Preferences.Get("DatabaseNameCentral", string.Empty);
            //var username = Preferences.Get("UsernameCentral", string.Empty);
            //var password = Preferences.Get("PasswordCentral", string.Empty);

            var server = "172.30.1.25";
            var username = "reportes";
            var password = "laBodegona2445.1988";
            var port = "3306";
            var database = "facturas_compras";

            var connectionString = $"Server={server};Port={port};Database={database};Uid={username};Pwd={password};";
            if (TestConnection(connectionString))
            {
                ConnectionCentral = connectionString;
            }
            else
            {
                ConnectionCentral = string.Empty;
            }
        }

        public static void InitializeConnectionNexus()
        {
            // Configuracion local activa (deshabilitada)
            //var server = LocalServer;
            //var username = LocalUsername;
            //var password = LocalPassword;
            //var port = LocalPort;
            //var database = LocalDatabaseNexus;

            // Configuracion productiva (deshabilitada temporalmente)
            //var server = Preferences.Get("ServerAddressNexus", string.Empty);
            //var port = Preferences.Get("PortNumberNexus", string.Empty);
            //var database = Preferences.Get("DatabaseNameNexus", string.Empty);
            //var username = Preferences.Get("UsernameNexus", string.Empty);
            //var password = Preferences.Get("PasswordNexus", string.Empty);

            var server = "172.30.1.27";
            var username = "compras";
            var password = "bode.24451988";
            var port = "3306";
            var database = "dbsucursales";

            var connectionString = $"Server={server};Port={port};Database={database};Uid={username};Pwd={password};";
            if (TestConnection(connectionString))
            {
                ConnectionNexus = connectionString;
            }
            else
            {
                ConnectionNexus = string.Empty;
            }
        }

        public static MySqlConnection GetConnection()
        {
            if (string.IsNullOrEmpty(ConnectionString))
            {
                InitializeConnectionString();
            }
            return new MySqlConnection(ConnectionString);
        }

        public static MySqlConnection GetConnectionCentral()
        {
            if (string.IsNullOrEmpty(ConnectionCentral))
            {
                InitializeConnectionCentral();
            }
            return new MySqlConnection(ConnectionCentral);
        }

        public static MySqlConnection GetConnectionNexus()
        {
            if (string.IsNullOrEmpty(ConnectionNexus))
            {
                InitializeConnectionNexus();
            }
            return new MySqlConnection(ConnectionNexus);
        }

        public static bool TestConnection(string connectionString)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static void UpdateConnectionString(string server, string port, string database, string username, string password)
        {
            if (ForceLocalConnections)
            {
                // Mantener la conexion local fija durante pruebas.
                InitializeConnectionString();
                return;
            }

            ConnectionString = $"Server={server};Port={port};Database={database};Uid={username};Pwd={password};";
            ConnectedServer = server;
        }

        public class ConnectionConfig
        {
            public string Server { get; set; }
            public string Port { get; set; }
            public string Database { get; set; }
            public string Uid { get; set; }
            public string Password { get; set; }
        }
    }
}
