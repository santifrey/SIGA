# SIGA - Frontend Base (Etapa 2)

**SIGA** (*Sistema Integral de Gestión y Administración*) es una plataforma web moderna, elegante y responsiva construida con tecnologías web estándar: **HTML5 semántico**, **CSS3 modular** y **JavaScript moderno (ES6+ modular)** sin dependencias de frameworks externos pesados.

---

## 🎨 Identidad Visual y Paleta de Diseño
La interfaz implementa una paleta corporativa inspirada en el **Azafrán Mediterráneo**:
- **Púrpura Flor de Azafrán Profundo / Índigo (`--primary`: `#4A154B`, `#5E2750`)**: Transmite solidez institucional y elegancia empresarial.
- **Ámbar Dorado / Pistilo Azafrán (`--accent`: `#E58E26`, `#D97706`)**: Detalles de alta visibilidad, llamados a la acción y estados activos.
- **Superficies y Contrastes (`--bg-app`: `#F8F9FC`, `--surface`: `#FFFFFF`, `--border`: `#E2E8F0`)**: Máxima legibilidad, limpieza visual y descansada experiencia de lectura.

---

## 📁 Estructura del Proyecto

```text
frontend/
├── index.html                      # Shell SPA y punto de entrada
├── README.md                       # Documentación técnica
├── css/
│   ├── variables.css               # Tokens de colores, sombras, radios y tipografía
│   ├── reset.css                   # Normalización y reset moderno
│   ├── main.css                    # Estilos base, scrollbar personalizada y utilidades
│   ├── layout.css                  # Estructura del Shell (Sidebar, Topbar, Responsive Drawer)
│   ├── components.css              # Botones, Cards, Badges, Modales y Alertas Toast
│   ├── forms.css                   # Inputs estilizados, switches, labels y feedback de error
│   └── tables.css                  # Tablas interactivas, estados vacíos y paginación
└── js/
    ├── config.js                   # Configuración global y URL base de la API REST
    ├── router.js                   # Enrutador Hash SPA con Auth Guards y Query Params
    ├── app.js                      # Orquestador del Shell y ciclo de vida de vistas
    ├── utils/
    │   ├── storage.js              # Abstracción segura de sesión (Token JWT + Usuario)
    │   ├── api-client.js           # Cliente Fetch con inyector Bearer y captura de 401
    │   └── toast.js                # Sistema de notificaciones toast flotantes animadas
    ├── services/
    │   ├── auth-service.js         # Autenticación (Login, Logout, Usuario actual)
    │   ├── clientes-service.js     # CRUD de Clientes, Personas y Baja Lógica (Soft Delete)
    │   ├── sucursales-service.js   # CRUD de Sucursales y Capacidad Operativa
    │   ├── stock-service.js        # Consulta de Stock de Bulbos por Cliente (Clases 0 a 4)
    │   └── movimientos-service.js  # Registro, confirmación y anulación de movimientos
    └── views/
        ├── login-view.js           # Pantalla de Login con toggle de contraseña y demo rápida
        ├── dashboard-view.js       # Métricas ejecutivas, bienvenida y accesos directos
        ├── clientes-view.js        # Tabla paginada, buscador en vivo y modal de Persona
        ├── sucursales-view.js      # Tarjetas/Tabla, capacidad y modal de gestión
        ├── stock-view.js           # Consulta simple y directa de stock por cliente
        └── movimientos-view.js     # Listado, confirmación, anulación y nuevo movimiento simple
```

---

## 🚀 Cómo Ejecutar la Aplicación

Dado que utiliza módulos ES6 nativos (`import`/`export`), se recomienda servir el directorio `frontend/` mediante cualquier servidor HTTP local:

### Opción 1: Con Python (disponible habitualmente en cualquier sistema)
```bash
cd frontend
python -m http.server 8080
```
Luego abrir: `http://localhost:8080`

### Opción 2: Con Node.js / NPX
```bash
cd frontend
npx serve .
# o también:
npx live-server .
```

### Opción 3: Con la extensión Live Server de Visual Studio Code
Hacer clic derecho sobre `index.html` y seleccionar **"Open with Live Server"**.

---

## 🔐 Credenciales de Demostración

La aplicación cuenta con botones de auto-completado en la pantalla de inicio de sesión:

| Rol | Usuario / Email | Contraseña |
| :--- | :--- | :--- |
| **Administrador** | `admin@siga.com` | `admin123` |
| **Operador** | `operador@siga.com` | `operador123` |

---

## ⚙️ Conexión con Backend REST & Modo Híbrido

1. **Backend en Vivo**:
   - Configurado por defecto para consumir `http://localhost:5000/api/v1` (o modificable en `js/config.js` o mediante la variable global `window.SIGA_API_URL`).
   - Envía el header `Authorization: Bearer <token_jwt>` en cada petición.
   - En caso de recibir `401 Unauthorized`, revoca automáticamente el token y redirige a `#login`.

2. **Modo Autónomo / Fallback (Offline / Demo)**:
   - Si el backend REST aún no está levantado, `api-client.js` detecta la indisponibilidad de red y utiliza de forma transparente el repositorio local con datos precargados realistas (semilla de clientes y sucursales argentinas).
   - Permite probar el 100% de los flujos de creación, edición, baja lógica, paginación, filtros y toasts de manera interactiva sin bloqueos.
