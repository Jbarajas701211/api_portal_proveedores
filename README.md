# Portal Proveedores API Core Marti

## Descripción
API RESTful para la gestión de proveedores, usuarios y operaciones relacionadas en el portal de proveedores de Marti.

## Características principales
- Registro y autenticación de usuarios (proveedores y administradores)
- Gestión de proveedores y sus datos fiscales
- Asignación de roles y permisos
- Envío de notificaciones y activación de cuentas
- Integración con Google KMS para cifrado
- Auditoría y trazabilidad de eventos de usuario

## Tecnologías utilizadas
- .NET Core
- Entity Framework Core
- PostgreSQL
- Npgsql
- Swagger (OpenAPI)
- Google KMS

## Estructura del proyecto
- `Controllers/` — Controladores de la API
- `Models/` — Entidades y modelos de datos
- `Services/` — Lógica de negocio y servicios auxiliares
- `Data/` — DbContext y configuración de acceso a datos
- `Dto/` — Objetos de transferencia de datos
- `Helpers/` — Utilidades y helpers

## Configuración inicial
1. Clona el repositorio.
2. Configura la cadena de conexión a PostgreSQL en `appsettings.json`.
3. Aplica las migraciones de Entity Framework:


4. Configura las variables de entorno necesarias para Google KMS y otros servicios externos.

## Ejecución

El API estará disponible en `https://localhost:5001` (o el puerto configurado).

## Endpoints principales
- `/api/usuario/alta_cuenta` — Alta de cuenta de usuario
- `/api/proveedor` — Gestión de proveedores
- `/api/auth/login` — Autenticación de usuarios

Consulta la documentación Swagger en `/swagger` para ver todos los endpoints disponibles.

## Notas de desarrollo
- Asegúrate de mantener sincronizados los modelos de datos y la base de datos.
- Revisa las migraciones antes de aplicarlas en producción.
- Elimina cualquier configuración de clave foránea innecesaria en las entidades para evitar errores de mapeo.

## Licencia
Este proyecto es propiedad de Marti. Uso interno únicamente.