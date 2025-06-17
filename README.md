# AppBodegona

AppBodegona es una aplicación móvil multiplataforma desarrollada con Xamarin.Forms, orientada a la gestión de inventarios, productos y reportes en sucursales de "La Bodegona". Permite a los usuarios consultar, modificar y reportar información clave de productos, facilitando la operación diaria en tienda y la toma de decisiones.

## Módulos principales

### 1. Existencias
Permite consultar el inventario físico de productos en la sucursal.  
- Búsqueda por código de barras o descripción.
- Visualización de existencias actuales.
- Escaneo de productos para consulta rápida.
- Cambio de sucursal según configuración.

### 2. Cambio de Precio
Facilita la modificación de precios y márgenes de productos individuales o familias completas.
- Edición de precios, márgenes y niveles de precio.
- Historial de cambios para trazabilidad.
- Validaciones para evitar errores comunes.
- Exportación de reportes de cambios.

### 3. Modificación de Familias
Permite modificar precios y márgenes de grupos de productos (familias) de forma masiva.
- Consulta de productos por familia.
- Edición y actualización de precios/márgenes para todos los productos de la familia.
- Visualización de detalles de la familia y sus productos asociados.

### 4. Fechas (Fechas Cortas)
Gestión y reporte de productos próximos a vencer.
- Registro de productos con fechas de vencimiento.
- Generación de reportes de productos con fechas cortas.
- Exportación de reportes a Excel.
- Control de acceso por usuario y sucursal.

### 5. Recepción (En desarrollo)
Módulo para registrar y controlar la recepción de mercancía.
- Registro de productos recibidos.
- Validación contra órdenes de compra (planeado).
- Reportes de recepción (planeado).

## Otras características

- **Escaneo de códigos de barras y QR** para agilizar búsquedas y configuraciones.
- **Popups y notificaciones** para alertas, confirmaciones y formularios.
- **Configuración dinámica** de conexión a base de datos mediante formulario o QR.
- **Control de acceso** por usuario y nivel.
- **Persistencia de preferencias** y configuraciones con Xamarin.Essentials.
- **Exportación de reportes** a Excel con EPPlus.

## Estructura del proyecto

AppBodegona/
├── AppBodegona/                # Proyecto principal (.NET Standard 2.0)
│   ├── Views/                  # Vistas y code-behind de cada módulo
│   │   ├── Existencia.xaml
│   │   ├── Existencia.xaml.cs
│   │   ├── Precio.xaml
│   │   ├── Precio.xaml.cs
│   │   ├── Familias.xaml
│   │   ├── Familias.xaml.cs
│   │   ├── FechasCortas.xaml
│   │   ├── FechasCortas.xaml.cs
│   │   ├── Recepcion.xaml
│   │   ├── Recepcion.xaml.cs
│   │   ├── IPConfig.xaml
│   │   ├── IPConfig.xaml.cs
│   │   └── Login.xaml / .cs
│   ├── Services/               # Servicios de conexión y utilidades
│   ├── Models/                 # Modelos de datos (si aplica)
│   ├── App.xaml
│   ├── App.xaml.cs
│   ├── AppShell.xaml
│   ├── AppShell.xaml.cs
│   └── ...                     # Otros archivos y recursos
├── AppBodegona.Android/        # Proyecto Android (MonoAndroid v13.0)
│   ├── MainActivity.cs
│   └── ...                     # Recursos y configuración Android
├── AppBodegona.iOS/            # Proyecto iOS (Xamarin.iOS v1.0)
│   ├── AppDelegate.cs
│   └── ...                     # Recursos y configuración iOS
├── README.md                   # Documentación del proyecto
├── AppBodegona.sln             # Solución de Visual Studio
└── ...                         # Otros archivos de configuración


## Dependencias principales

- Xamarin.Forms
- Xamarin.Essentials
- MySqlConnector
- ZXing.Net.Mobile (escaneo de códigos)
- Rg.Plugins.Popup (popups)
- EPPlus (exportación a Excel)
- Newtonsoft.Json

## Instalación y ejecución

1. Clona el repositorio.
2. Configura las cadenas de conexión en la app o mediante el escaneo de QR.
3. Compila y ejecuta en Visual Studio seleccionando la plataforma deseada (Android/iOS).

---

**Nota:** El módulo de Recepción está en desarrollo y puede no estar disponible en todas las versiones.
