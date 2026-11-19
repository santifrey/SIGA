# SIGA - Sistema Integral de Gestión Agrícola (Azafrán Mediterráneo)

Solución integral con Backend bajo **Clean Architecture** en **.NET 8 (C#)** y Frontend **SPA Modular (ES6+ / HTML5 / CSS3)** para la gestión de producción agrícola de azafrán, sucursales, clientes, empleados, stock por participante, movimientos comerciales con circuito formal de aprobación/contratos PDF y seguridad basada en roles y permisos (RBAC).

---

## 🏗 Arquitectura de la Solución

```text
SIGA/
├── SIGA.sln
├── src/
│   ├── SIGA.Domain/                   # Núcleo del dominio: Entidades, Enums, Interfaces de Repositorios
│   ├── SIGA.Application/              # Casos de uso: DTOs, Servicios de Aplicación, Interfaces de Negocio
│   ├── SIGA.Infrastructure/           # Persistencia EF Core (MySQL), DbContext, Repositorios, BCrypt, JWT
│   └── SIGA.API/                      # ASP.NET Core Web API (.NET 8), Controladores REST, Middleware, Autorización RBAC
└── frontend/                          # Cliente Web SPA (ES6 Modules, Plantillas HTML separadas, Componentes reutilizables)
```

> [!NOTE]
> **Eliminación de Swagger UI:** La interfaz Swagger (`/swagger`) fue removida del pipeline de la API para optimizar el rendimiento, reducir dependencias innecesarias y asegurar el entorno de producción (despliegue en contenedores / Google Cloud Run). La verificación operativa se realiza mediante el endpoint de salud `/api/v1/health` y el frontend web.

---

## 🗄 Base de Datos y Persistencia

El backend utiliza **MySQL** como motor relacional mediante el proveedor `Pomelo.EntityFrameworkCore.MySql`.

### Configuración de Credenciales mediante Variables de Entorno (`.env`)

Para evitar tener credenciales escritas en el código o en archivos de configuración versionados en git, la aplicación carga automáticamente las variables desde un archivo `.env` en la raíz (mediante `DotNetEnv`):

1. Cree su archivo `.env` a partir de la plantilla:
   ```bash
   cp .env.example .env
   ```
2. Configure sus parámetros de conexión a MySQL y JWT:
   ```env
   # Parámetros individuales de MySQL:
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

### Configuración en `appsettings.json`

```json
{
  "ConnectionStrings": {
    "MySqlConnection": "Server=localhost;Port=3306;Database=siga_db;User=root;Password=root;"
  }
}
```

- **`MySqlConnection`**: Conecta a **MySQL** utilizando la cadena de conexión configurada o resuelta desde variables de entorno. Incluye autodetección de versión de motor y resiliencia.

---

## 🚀 Cómo Ejecutar el Proyecto

### 1. Compilar y Ejecutar la API (.NET 8)
```powershell
dotnet build src/SIGA.API
dotnet run --project src/SIGA.API
```

La API escuchará por defecto en:
- **API Base:** `http://localhost:5000`
- **Health Check:** `http://localhost:5000/api/v1/health`

### 2. Ejecutar el Frontend
```powershell
python -m http.server 8080 --directory frontend
```
Disponible en: `http://localhost:8080`

---

## 🔑 Credenciales Iniciales y Activación de Cuentas (Data Seeding)

Al iniciar, el sistema ejecuta automáticamente el esquema y sembrado de datos iniciales (`DatabaseSeeder`):

- **Usuario Administrador:** `admin`
- **Contraseña:** `Admin123!`
- **Rol:** `ADMINISTRADOR` (rol de sistema protegido con acceso total)
- **Empresa Matriz (Participante ID #1):** Azafrán Mediterráneo S.A. (CUIT: `30-71234567-8`)
- **Sucursal Principal:** Sede Central - Villa General Belgrano
- **Alta de Empleados y Clientes:** Al registrar un nuevo empleado o cliente, se generan credenciales automáticas (**usuario = email**, **contraseña inicial = DNI**) con cambio obligatorio de contraseña en el primer inicio de sesión para activar la cuenta.

---

## 📡 Módulos y Endpoints Principales (`/api/v1`)

### 1. Autenticación, Perfil y Seguridad RBAC
- `POST /api/v1/auth/login`: Autenticación con usuario/email y emisión de JWT (con Rate Limiting e indicador de activación pendiente).
- `GET /api/v1/auth/me`: Perfil completo del usuario autenticado, rol único y permisos efectivos.
- `PUT /api/v1/auth/me`: Actualización de datos personales y de contacto del propio usuario.
- `GET|POST /api/v1/roles`: Listado y creación de roles con su matriz de permisos.
- `GET|PUT|DELETE /api/v1/roles/{id}`: Consulta, actualización de permisos asignados y eliminación (con salvaguarda para roles de sistema o con usuarios activos).
- `GET /api/v1/permisos`: Catálogo completo de permisos agrupados por módulo.

### 2. Personas, Clientes, Empleados y Usuarios
- `GET|POST|PUT|DELETE /api/v1/clientes`: Gestión de legajos de clientes, búsqueda paginada y baja lógica.
- `GET|POST|PUT|DELETE /api/v1/empleados`: Gestión de nómina interna (Persona + Empleado + Usuario con rol asignado) y desactivación con salvaguarda del último administrador activo.
- `GET|POST|PUT|DELETE /api/v1/personas`: Gestión de personas físicas y búsqueda por documento.
- `GET|POST|PUT|DELETE /api/v1/usuarios`: Administración de cuentas de usuario, cambio de estado y cambio/activación de contraseña (`POST /api/v1/usuarios/{id}/cambiar-password`).

### 3. Sucursales y Temporadas Agrícolas
- `GET|POST|PUT|DELETE /api/v1/sucursales`: Administración de sedes/campos operativos, capacidades y estados.
- `GET|POST /api/v1/temporadas`: Gestión de ciclos agrícolas anuales, consulta de temporada activa (`GET /api/v1/temporadas/activa`), avance de estado (`POST /api/v1/temporadas/{id}/cambiar-estado`) y cierre.

### 4. Producción Agrícola y Campo
- `GET|POST /api/v1/filas`: Gestión de filas/surcos por sucursal y temporada.
- `GET /api/v1/lotes`: Consulta de lotes plantados por temporada o por cliente titular.
- `GET|POST /api/v1/plantaciones`: Registro de plantaciones y distribución física de bulbos por calibre (Clases 0 a 4) en filas y lotes (`POST /api/v1/plantaciones/{id}/distribucion`).
- `GET|POST /api/v1/transferencias`: Cesión/transferencia de titularidad de lotes completos entre participantes (`POST /api/v1/transferencias/{id}/confirmar`, `anular` e historial por lote).
- `GET|POST /api/v1/cosechas`: Inicio de cosecha por sucursal/temporada, registro de pesajes/conteos por lote y calibre (`POST /api/v1/cosechas/{id}/detalle`) y cierre con acreditación automática de stock (`POST /api/v1/cosechas/{id}/cerrar`).

### 5. Movimientos Comerciales, Precios de Referencia y Stock
- `GET|POST|PUT /api/v1/movimientos-comerciales`: Gestión unificada de **Ventas a Cliente (Tipo 1)**, **Compras a Cliente con comisión (Tipo 2)** y **Traspasos entre Clientes (Tipo 3)**, incluyendo descomposición automática de **MIX** ($1/3$ Calibre 1, $1/3$ Calibre 2, $1/3$ Calibre 3).
- **Circuito formal de 5 estados en Movimientos Comerciales:**
  - `GET /api/v1/movimientos-comerciales/verificar-cliente-nuevo/{clienteId}`: Verifica si el comprador requiere contrato marco obligatorio.
  - `POST /api/v1/movimientos-comerciales/{id}/contrato`: Registro/actualización de contrato oficial de compraventa.
  - `POST /api/v1/movimientos-comerciales/{id}/aprobar`: Aprobación administrativa del contrato/operación.
  - `POST /api/v1/movimientos-comerciales/{id}/facturar`: Emisión o reemplazo de factura comercial.
  - `POST /api/v1/movimientos-comerciales/{id}/finalizar`: Aprobación final de factura e impacto físico irreversible en stock.
  - `POST /api/v1/movimientos-comerciales/{id}/rechazar`: Rechazo u observación con motivo obligatorio (y anulación automática de factura vigente si existía).
- `GET|POST|PUT /api/v1/precios-referencia`: Gestión de precios oficiales de referencia por temporada para las Clases 0 a 4 y MIX.
- `GET /api/v1/stock/por-cliente/{clienteId}` y `/api/v1/stock/disponibilidad`: Consulta de inventario físico disponible por calibre para la Empresa Central (`ID = 1`) o cualquier cliente.
- `GET|POST /api/v1/movimientos`: Historial de trazabilidad física de bulbos (ventas, compras, traspasos, plantaciones, cosechas y ajustes manuales de stock con confirmación/anulación).

### 6. Reportes Ejecutivos y Salud
- `GET /api/v1/reportes/resumen-ejecutivo`: KPIs generales del sistema y de la temporada activa.
- `GET /api/v1/reportes/cliente/{clienteId}/integral`: Ficha 360° del cliente (stock en custodia, derechos sobre lotes plantados e historial productivo).
- `GET /api/v1/reportes/produccion/temporada/{temporadaId}`: Comparativa siembra vs. cosecha por calibre, rendimiento por sucursal y trazabilidad por lote.
- `GET /api/v1/health`: Estado de salud de la API (`{"status": "Healthy", ...}`).
