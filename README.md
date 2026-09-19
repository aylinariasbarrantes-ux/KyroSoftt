# Sistema de Reservas de Laboratorios

Proyecto del curso ISW-622 - Pruebas de Software, UTN. Equipo: Kyro Group.

## Definición de Hecho (DoD)

Una historia de usuario se considera "Hecha" cuando cumple con lo siguiente:

- El código compila sin errores ni warnings críticos.
- La funcionalidad cumple con todos los criterios de aceptación de la HU.
- El código fue probado manualmente contra los criterios de aceptación.
- El código fue subido (push) al repositorio con un commit que referencia el PBI correspondiente (ej. `AB#1`).
- No hay errores visibles en la consola del navegador ni en la terminal.
- El PBI y sus tasks fueron movidos a "Done" en Azure Boards.

## Notas de la solución

### Arquitectura

El proyecto está desarrollado en **ASP.NET Core MVC (C#)**, siguiendo una arquitectura por capas:

- **Views** (Frontend): interfaz web que ve el usuario (Razor Views).
- **Controllers** (API/entrada): reciben las peticiones HTTP y coordinan la respuesta.
- **Services** (lógica de negocio): contienen las reglas de validación y procesamiento.
- **Data** (acceso a datos): `AppDbContext` (Entity Framework Core) gestiona la conexión con la base de datos.
- **Models** (entidades): representan las tablas/objetos del sistema.

Para HU-1 (Inicio de sesión):
- `Models/Usuario.cs`: entidad con Id, NombreUsuario, Password y Rol.
- `Data/AppDbContext.cs`: contexto de Entity Framework Core, define la tabla `Usuarios` y los datos semilla iniciales.
- `Services/AuthService.cs`: consulta la base de datos y valida las credenciales del usuario.
- `Controllers/LoginController.cs`: recibe el formulario de login, llama al servicio y maneja la sesión.
- `Views/Login/Index.cshtml`: formulario de inicio de sesión.

**Persistencia:** el equipo decidió usar **SQL Server LocalDB** con **Entity Framework Core** desde esta etapa, en lugar de colecciones en memoria, para tener una base sólida de cara a futuras historias de usuario (como HU10 - Persistencia de información) y reflejar un escenario más realista de desarrollo.

## Implementación

### Cómo ejecutar el proyecto localmente

1. Clonar el repositorio: `git clone https://github.com/aylinariasbarrantes-ux/KyroSofft.git`
2. Abrir `SistemaReservasLaboratorios.sln` en Visual Studio.
3. Restaurar los paquetes NuGet (Visual Studio lo hace automáticamente al abrir el proyecto).
4. Aplicar las migraciones de base de datos desde la Consola del Administrador de Paquetes:
   ```
   Update-Database
   ```
   Esto crea la base de datos `ReservasLaboratoriosDb` en SQL Server LocalDB con los usuarios semilla.

5. Ejecutar el proyecto con F5 (o el botón "Run").

6. Navegar a `https://localhost:{puerto}/Login` para acceder al login.

### Usuarios de prueba

| Usuario   | Contraseña | Rol            |
|-----------|------------|----------------|
| admin     | admin123   | Administrador  |
| usuario1  | user123    | Usuario        |

### Deployment

Por ahora el proyecto se ejecuta localmente en el entorno de desarrollo, usando SQL Server LocalDB (incluido con Visual Studio, no requiere instalación adicional). Se documentará un ambiente de despliegue en la nube más adelante en el curso, cuando se configure integración continua.

## Pruebas

Pruebas realizadas para HU-1 - Inicio de sesión:

| # | Prueba | Datos | Resultado esperado | Resultado obtenido |
|---|--------|-------|---------------------|---------------------|
| 1 | Login correcto (Administrador) | admin / admin123 | Acceso permitido | Correcto |
| 2 | Login correcto (Usuario) | usuario1 / user123 | Acceso permitido | Correcto |
| 3 | Contraseña incorrecta | admin / contraseña errónea | Mensaje "Usuario o contraseña incorrectos." | Correcto |
| 4 | Campos vacíos | (vacío) / (vacío) | Mensaje "Debe ingresar usuario y contraseña." | Correcto |