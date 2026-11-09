# SIGA - Sistema Integral de Gestión Agrícola (Azafrán Mediterráneo)

Solución Backend construida bajo **Clean Architecture** en **.NET 7 (C#)** para la gestión integral de producción, sucursales, clientes, usuarios y seguridad.

---

## 🏗 Arquitectura de la Solución

```
SIGA/
├── SIGA.sln
├── src/
│   ├── SIGA.Domain/                   # Núcleo del dominio: Entidades, Enums, Interfaces de Repositorios
│   ├── SIGA.Application/              # Casos de uso: DTOs, Servicios de Aplicación, Interfaces de Negocio
│   ├── SIGA.Infrastructure/           # Persistencia EF Core, DbContext, Repositorios, BCrypt, JWT Service
│   └── SIGA.API/                      # ASP.NET Core Web API, Controladores REST, Middleware, Swagger, Program
└── tests/
    └── SIGA.UnitTests/                # Pruebas unitarias xUnit (BCrypt, JWT, Casos de Uso, Entidades)
```

---

## 🚀 Cómo Ejecutar la API

### 1. Compilación
```powershell
dotnet build
```

### 2. Ejecutar Pruebas Unitarias
```powershell
dotnet test
```

### 3. Ejecutar la API
```powershell
cd src/SIGA.API
dotnet run
```
La aplicación iniciará automáticamente y abrirá Swagger en:
`http://localhost:5000/swagger` (o `https://localhost:7001/swagger`)

---

## 🔑 Credenciales Iniciales (Data Seeding)

El sistema crea automáticamente la base de datos (por defecto SQLite `siga_dev.db` para desarrollo inmediato o SQL Server configurable vía `appsettings.json`) con los siguientes datos iniciales:

- **Usuario:** `admin`
- **Contraseña:** `Admin123!`
- **Rol:** `ADMINISTRADOR`
- **Empresa Matriz:** Azafrán Mediterráneo S.A. (CUIT: 30-71234567-8)
- **Sucursal Principal:** Sede Central - Villa General Belgrano

---

## 📡 Endpoints Principales (`/api/v1`)

- **Autenticación**:
  - `POST /api/v1/auth/login`: Autentica credenciales y emite token JWT con roles.
  - `GET /api/v1/auth/me`: Retorna los datos del usuario autenticado.
- **Personas**:
  - `GET /api/v1/personas`: Listar personas (soporta soft-delete).
  - `GET /api/v1/personas/{id}`: Detalle de persona.
  - `POST /api/v1/personas`: Crear nueva persona.
  - `PUT /api/v1/personas/{id}`: Modificar persona.
  - `DELETE /api/v1/personas/{id}`: Eliminación lógica (soft-delete).
- **Usuarios**:
  - `GET /api/v1/usuarios`: Listar usuarios con roles.
  - `POST /api/v1/usuarios`: Crear usuario (puede asociar o crear persona).
  - `PUT /api/v1/usuarios/{id}/estado`: Activar, inactivar o bloquear usuario.
  - `POST /api/v1/usuarios/{id}/roles`: Asignar roles.
- **Clientes**:
  - `GET /api/v1/clientes`: Listado y búsqueda por nombre, DNI o código.
  - `POST /api/v1/clientes`: Alta de cliente y asignación de código.
  - `PUT /api/v1/clientes/{id}`: Modificación de cliente.
  - `DELETE /api/v1/clientes/{id}`: Baja lógica.
- **Sucursales**:
  - `GET /api/v1/sucursales`: Listar sucursales activas.
  - `POST /api/v1/sucursales`: Crear nueva sucursal.
  - `PUT /api/v1/sucursales/{id}`: Actualizar capacidad y ubicación.
