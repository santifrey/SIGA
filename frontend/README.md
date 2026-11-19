# SIGA - Frontend SPA Modular

**SIGA** (*Sistema Integral de Gestión Agrícola - Azafrán Mediterráneo*) cuenta con un cliente web **Single Page Application (SPA)** construido con **HTML5 semántico**, **CSS3 modular** y **JavaScript moderno (ES6 Modules)** sin dependencias de frameworks pesados.

La arquitectura separa limpiamente las **plantillas de estructura HTML** (`frontend/templates/`), los **componentes de interfaz reutilizables** (`frontend/js/components/`), los **servicios de comunicación con la API** (`frontend/js/services/`) y los **controladores de vistas** (`frontend/js/views/`).

---

## 🎨 Identidad Visual y Paleta de Diseño

La interfaz implementa una paleta corporativa inspirada en el **Azafrán Mediterráneo**:
- **Púrpura Flor de Azafrán (`--primary`: `#4A154B`, `#5E2750`)**: Solidez institucional y navegación principal.
- **Ámbar Dorado / Pistilo Azafrán (`--accent`: `#E58E26`, `#D97706`)**: Llamados a la acción, indicadores destacados y estados activos.
- **Superficies y Contrastes (`--bg-app`: `#F8F9FC`, `--surface`: `#FFFFFF`, `--border`: `#E2E8F0`)**: Limpieza visual para lectura intensiva de tablas, métricas y formularios.

---

## 📁 Estructura del Proyecto Frontend

```text
frontend/
├── index.html                                      # Punto de entrada de la SPA y precarga de módulos
├── README.md                                       # Documentación técnica del frontend
├── assets/
│   └── MODELO_DE_CONTRATO_PARA_CLIENTES.pdf        # Plantilla legal PDF para generación de contratos
├── libs/
│   └── pdf-lib.min.js                              # Librería cliente para rellenado y edición de PDFs
├── css/
│   ├── variables.css                               # Tokens de diseño (colores, sombras, radios, tipografía)
│   ├── reset.css                                   # Normalización CSS moderna
│   ├── main.css                                    # Estilos base y utilidades generales
│   ├── layout.css                                  # Shell de la aplicación (Sidebar colapsable, Topbar, Responsive)
│   ├── components.css                              # Botones, Cards, Badges, Modales y Toasts
│   ├── forms.css                                   # Controles de formulario, switches y validaciones
│   └── tables.css                                  # Tablas de datos, estados vacíos y controles de paginación
├── templates/                                      # Plantillas HTML estáticas cargadas bajo demanda (con caché)
│   ├── app-layout.html                             # Layout principal (Sidebar de 5 secciones y Topbar)
│   ├── login.html                                  # Formulario de inicio de sesión
│   ├── dashboard.html                              # Tablero principal de bienvenida y KPIs
│   ├── movimientos-comerciales.html                # Vista y modales del circuito comercial unificado
│   ├── historial-movimientos.html                  # Vista de trazabilidad física de bulbos
│   ├── ajustes-stock.html                          # Vista y modales de ajustes manuales de stock
│   ├── stock.html                                  # Consulta de stock por titular y calibre
│   ├── temporadas.html                             # Gestión de ciclos agrícolas anuales
│   ├── plantaciones.html                           # Distribución física en filas y lotes
│   ├── transferencias.html                         # Cesión de lotes completos entre titulares
│   ├── cosechas.html                               # Recolección por sucursal y cierre de cosecha
│   ├── clientes.html                               # Listado paginado de clientes
│   ├── sucursales.html                             # Gestión de sucursales en modo tarjetas o tabla
│   ├── empleados.html                              # Gestión de nómina interna de empleados
│   ├── roles.html                                  # Gestión de roles y matriz de permisos
│   ├── precios-referencia.html                     # Lista oficial de precios por temporada y calibre
│   ├── reportes.html                               # Reportes ejecutivos, ficha 360° y rendimiento
│   ├── perfil.html                                 # Perfil del usuario autenticado
│   ├── cliente-modal.html                          # Plantilla del modal compartido de alta/edición de cliente
│   ├── confirm-delete-modal.html                   # Plantilla del modal compartido de confirmación de baja
│   └── activacion-cuenta-modal.html                # Plantilla del modal de activación de cuenta en primer ingreso
└── js/
    ├── config.js                                   # Detección automática de entorno y URL de la API REST
    ├── router.js                                   # Enrutador Hash SPA con Auth Guards y Permission Guards (RBAC)
    ├── app.js                                      # Orquestador del Shell, menú colapsable persistente y sesión
    ├── utils/
    │   ├── api-client.js                           # Cliente HTTP Fetch con Bearer JWT, manejo de 400/401 y errores
    │   ├── template-loader.js                      # Cargador asíncrono de plantillas HTML con caché en memoria
    │   ├── storage.js                              # Persistencia de sesión (JWT y datos del usuario)
    │   ├── toast.js                                # Notificaciones flotantes animadas
    │   ├── icons.js                                # Generador de íconos SVG reutilizables (prohibición, etc.)
    │   ├── escape-html.js                          # Sanitización de strings contra XSS
    │   └── html.js                                 # Re-exportador de utilidades HTML seguras
    ├── components/                                 # Componentes UI reutilizables entre múltiples vistas
    │   ├── cliente-modal.js                        # Modal compartido de creación/edición de clientes
    │   ├── confirm-delete-modal.js                 # Modal compartido de confirmación de baja/eliminación
    │   ├── activacion-cuenta-modal.js              # Modal de cambio obligatorio de contraseña inicial (DNI)
    │   ├── bulbos-calibres-grid.js                 # Grilla reutilizable de 5 calibres (Clases 0..4) con suma en vivo
    │   └── pagination.js                           # Controles de paginación reutilizables
    ├── services/                                   # Capa de servicios de dominio y comunicación API
    │   ├── auth-service.js                         # Login, logout, perfil, permisos RBAC y cambio de contraseña
    │   ├── movimientos-comerciales-service.js      # Circuito comercial (Ventas, Compras, Traspasos, MIX, estados)
    │   ├── contrato-generator-service.js           # Generación y caché de contratos PDF en navegador con pdf-lib
    │   ├── precios-referencia-service.js           # Precios oficiales por temporada (Clases 0..4 y MIX)
    │   ├── movimientos-service.js                  # Historial físico de bulbos y ajustes manuales de stock
    │   ├── stock-service.js                        # Stock disponible por participante (Empresa #1 o Clientes)
    │   ├── temporadas-service.js                   # Ciclos agrícolas y transiciones de estado
    │   ├── plantaciones-service.js                 # Filas, lotes y distribución física de siembra
    │   ├── transferencias-service.js               # Transferencias de titularidad de lotes
    │   ├── cosechas-service.js                     # Cosechas por sucursal, pesajes por lote y acreditación
    │   ├── clientes-service.js                     # CRUD y baja lógica de clientes
    │   ├── sucursales-service.js                   # CRUD de sucursales/campos
    │   ├── empleados-service.js                    # CRUD y desactivación de empleados internos
    │   ├── roles-service.js                        # CRUD de roles y catálogo de permisos
    │   └── reportes-service.js                     # Resumen ejecutivo, ficha 360° de cliente y reporte productivo
    └── views/                                      # Controladores de vistas de la SPA
        ├── login-view.js                           # Inicio de sesión y disparo de activación inicial
        ├── dashboard-view.js                       # Panel de control y accesos rápidos
        ├── movimientos-comerciales-view.js         # Coordinador de Movimientos Comerciales (KPIs, filtros, tabla)
        ├── movimientos-comerciales/                # Submódulos del circuito de Movimientos Comerciales
        │   ├── movimientos-helpers.js              # Resolución de participantes, estados de 5 fases y botones RBAC
        │   ├── movimiento-form-modal.js            # Modal de alta/edición multi-tipo, cálculo MIX y stock en vivo
        │   ├── movimiento-detalle-modal.js         # Modal de detalle, comprobantes y línea de tiempo de auditoría
        │   └── movimiento-acciones-modal.js        # Modal de aprobación, contratos PDF, facturación y rechazo
        ├── historial-movimientos-view.js           # Historial cronológico y trazabilidad física de bulbos
        ├── ajustes-stock-view.js                   # Ingresos/egresos manuales de stock con motivo obligatorio
        ├── stock-view.js                           # Consulta de existencias por calibre y titular
        ├── temporadas-view.js                      # Administración de temporadas y avance de fase
        ├── plantaciones-view.js                    # Siembra y asignación de lotes por fila
        ├── transferencias-view.js                  # Cesión de lotes completos entre titulares
        ├── cosechas-view.js                        # Registro de cosecha por sucursal y acreditación de stock
        ├── clientes-view.js                        # Administración de clientes
        ├── sucursales-view.js                      # Administración de sucursales (vista tarjetas / tabla)
        ├── empleados-view.js                       # Administración de empleados y asignación de rol
        ├── roles-view.js                           # Administración de roles y matriz de permisos por módulo
        ├── precios-referencia-view.js              # Configuración de precios de referencia por temporada
        ├── reportes-view.js                        # Reportes ejecutivos, estado 360° de cliente y temporada
        └── perfil-view.js                          # Consulta y edición del perfil del usuario actual
```

---

## 🚀 Cómo Ejecutar la Aplicación

Dado que utiliza módulos ES6 nativos (`import`/`export`) y carga asíncrona de plantillas HTML (`fetch`), debe servirse desde un servidor HTTP local:

### Opción 1: Con Python
```bash
python -m http.server 8080 --directory frontend
```
Luego abrir: `http://localhost:8080`

### Opción 2: Con Node.js / NPX
```bash
cd frontend
npx serve .
```

---

## 🔐 Credenciales y Control de Acceso (RBAC)

| Rol | Usuario / Email | Contraseña |
| :--- | :--- | :--- |
| **Administrador** | `admin` | `Admin123!` |
| **Nuevos Empleados / Clientes** | Correo electrónico registrado | Número de **DNI** (exige cambio obligatorio en el 1° ingreso) |

- **Filtrado dinámico de navegación y rutas:** El menú lateral de 5 secciones colapsables ([app.js](js/app.js)) y el enrutador ([router.js](js/router.js)) verifican los permisos del usuario activo (`MOVCOM_VER`, `STOCK_VER`, `PLANTACIONES_VER`, `EMPLEADOS_VER`, `ROLES_VER`, etc.) ocultando opciones no autorizadas y bloqueando el acceso directo por URL.
