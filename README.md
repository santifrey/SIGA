# SIGA - Sistema Integral de Gestión Agrícola (Azafrán Mediterráneo)

Solución Backend construida bajo **Clean Architecture** en **.NET 7 (C#)** para la gestión integral de producción agrícola, sucursales, clientes, empleados, stock, movimientos comerciales y seguridad por roles y permisos.

---

## 🏗 Arquitectura de la Solución

```
SIGA/
├── SIGA.sln
├── src/
│   ├── SIGA.Domain/                   # Núcleo del dominio: Entidades, Enums, Interfaces de Repositorios
│   ├── SIGA.Application/              # Casos de uso: DTOs, Servicios de Aplicación, Interfaces de Negocio
│   ├── SIGA.Infrastructure/           # Persistencia EF Core (MySQL / SQLite), DbContext, Repositorios, BCrypt, JWT
│   └── SIGA.API/                      # ASP.NET Core Web API, Controladores REST, Middleware, Autorización
├── frontend/                          # Cliente Web SPA (Vanilla JavaScript ES6 Modules, HTML5, CSS3)
└── tests/
    ├── SIGA.UnitTests/                # Pruebas unitarias xUnit
    └── SIGA.E2ETests/                 # Pruebas de integración E2E por niveles
```

> [!NOTE]
> **Eliminación de Swagger UI:** La interfaz Swagger (`/swagger`) fue removida del pipeline de la API para optimizar el rendimiento, reducir dependencias innecesarias y asegurar el entorno de producción (despliegue en contenedores / Google Cloud Run). La verificación operativa se realiza mediante el endpoint de salud `/api/v1/health`, pruebas automatizadas y el frontend web.

---

## 🗄 Base de Datos y Persistencia

El backend soporta **MySQL** como motor relacional principal (en sustitución de Microsoft SQL Server / MSSQL) mediante el proveedor `Pomelo.EntityFrameworkCore.MySql`, manteniendo además soporte opcional para **SQLite** para desarrollo local rápido.

### Configuración de Credenciales mediante Variables de Entorno (`.env`)

Para evitar tener credenciales escritas en el código o en archivos de configuración versionados en git, la aplicación carga automáticamente las variables desde un archivo `.env` en la raíz (mediante `DotNetEnv`):

1. Cree su archivo `.env` a partir de la plantilla:
   ```bash
   cp .env.example .env
   ```
2. Configure sus parámetros de conexión a MySQL:
   ```env
   USE_SQLITE=false

   # Parámetros individuales:
   DB_HOST=localhost
   DB_PORT=3306
   DB_DATABASE=siga_db
   DB_USER=root
   DB_PASSWORD=su_contraseña_secreta

   # O alternativamente la cadena completa:
   # MYSQL_CONNECTION_STRING=Server=localhost;Port=3306;Database=siga_db;User=root;Password=su_contraseña;
   ```

> [!TIP]
> En despliegues en la nube (Google Cloud Run, AWS, Azure, Railway, Docker, etc.), no hace falta crear un archivo `.env`: simplemente defina estas mismas variables de entorno en la configuración del servicio/contenedor.

### Configuración de Respaldo (`appsettings.json`)

```json
{
  "UseSqlite": false,
  "ConnectionStrings": {
    "MySqlConnection": "Server=localhost;Port=3306;Database=siga_db;User=root;Password=root;",
    "SqliteConnection": "Data Source=siga_dev.db"
  }
}
```

- **`UseSqlite: false`**: Conecta a **MySQL** utilizando la cadena de conexión `MySqlConnection`. Incluye autodetección de versión de motor y resiliencia con reintentos automáticos (`EnableRetryOnFailure`).
- **`UseSqlite: true`**: Utiliza SQLite (`siga_dev.db`) para pruebas locales sin requerir un servidor de base de datos activo.

---

## 🚀 Cómo Ejecutar la API

### 1. Compilación
```powershell
dotnet build src/SIGA.API
```

### 2. Ejecutar Pruebas Unitarias
```powershell
dotnet test tests/SIGA.UnitTests
```

### 3. Ejecutar la API
```powershell
dotnet run --project src/SIGA.API
```

La API escuchará por defecto en:
- **API Base:** `http://localhost:5000`
- **Health Check:** `http://localhost:5000/api/v1/health`

### 4. Ejecutar el Frontend
```powershell
python -m http.server 8080 --directory frontend
```
Disponible en: `http://localhost:8080`

---

## 🔑 Credenciales Iniciales (Data Seeding)

Al iniciar, el sistema ejecuta automáticamente las migraciones/esquema y el sembrado de datos iniciales en la base de datos configurada:

- **Usuario:** `admin`
- **Contraseña:** `Admin123!`
- **Rol:** `ADMINISTRADOR`
- **Empresa Matriz:** Azafrán Mediterráneo S.A. (CUIT: `30-71234567-8`)
- **Sucursal Principal:** Sede Central - Villa General Belgrano

---

## 📡 Módulos y Endpoints Principales (`/api/v1`)

### 1. Autenticación y Autorización
- `POST /api/v1/auth/login`: Autenticación con credenciales y emisión de JWT (protegido con Rate Limiting).
- `GET /api/v1/auth/me`: Datos del usuario autenticado, roles y permisos asignados.
- `GET /api/v1/roles`: Listado de roles del sistema.
- `POST /api/v1/roles`: Creación de roles personalizados.
- `GET /api/v1/permisos`: Catálogo de permisos disponibles por módulo.
- `PUT /api/v1/roles/{id}/permisos`: Asignación dinámica de permisos a un rol.

### 2. Personas, Clientes y Empleados
- `GET|POST|PUT|DELETE /api/v1/personas`: Gestión de personas físicas y datos de contacto.
- `GET|POST|PUT|DELETE /api/v1/clientes`: Gestión de clientes, asignación de código único y seguimiento.
- `GET|POST|PUT /api/v1/empleados`: Gestión de legajos, sucursal asignada y roles laborales.
- `GET|POST|PUT /api/v1/usuarios`: Administración de cuentas de usuario, estado (activo/inactivo) y vinculación.

### 3. Sucursales y Temporadas
- `GET|POST|PUT /api/v1/sucursales`: Administración de sedes, capacidades máximas y estados operativos.
- `GET|POST|PUT /api/v1/temporadas`: Gestión de períodos agrícolas (apertura, cierre y transición).

### 4. Producción Agrícola y Campo
- `GET|POST|PUT /api/v1/filas`: Gestión de filas por sucursal y temporada.
- `GET|POST|PUT /api/v1/lotes`: Trazabilidad de lotes de bulbos.
- `GET|POST /api/v1/plantaciones`: Registro de siembra y distribución en filas.
- `GET|POST /api/v1/cosechas`: Registro de cosecha clasificada por calibres.
- `GET|POST /api/v1/transferencias`: Transferencia de lotes entre sucursales.

### 5. Gestión Comercial y Stock
- `GET|POST /api/v1/movimientos-comerciales`: Registro unificado de ventas, compras, reinversiones y devoluciones.
- `PUT /api/v1/movimientos-comerciales/{id}/estado`: Flujo de aprobación (Registrada, Aprobada, Rechazada, Cancelada).
- `GET|POST|PUT /api/v1/precios-referencia`: Fijación y consulta de precios de referencia por calibre/temporada.
- `GET /api/v1/stock/sucursal/{sucursalId}`: Consulta consolidada de stock de bulbos por clase.
- `GET /api/v1/reportes/ejecutivo`: Indicadores ejecutivos, KPIs financieros y resumen de clientes.

### 6. Salud del Servicio
- `GET /api/v1/health`: Estado de salud de la API (`{"status": "Healthy", ...}`).
