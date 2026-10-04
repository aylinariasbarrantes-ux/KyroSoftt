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
- `Models/Usuario.cs`: entidad con Id, NombreUsuario, PasswordHash y Rol. `PasswordHash` guarda el hash de la contraseña, nunca texto plano.
- `Data/AppDbContext.cs`: contexto de Entity Framework Core, define la tabla `Usuarios` (sin datos semilla).
- `Data/DbSeeder.cs`: crea la base de datos y siembra los usuarios de prueba al arrancar la app, guardando las contraseñas hasheadas con `PasswordHasher<Usuario>` de ASP.NET Core Identity.
- `Services/AuthService.cs`: busca al usuario por nombre y valida la contraseña con `VerifyHashedPassword`.
- `Controllers/LoginController.cs`: recibe el formulario de login, llama al servicio y maneja la sesión.
- `Views/Login/Index.cshtml`: formulario de inicio de sesión.

**Persistencia:** el equipo decidió usar **SQL Server LocalDB** con **Entity Framework Core** desde esta etapa, en lugar de colecciones en memoria, para tener una base sólida de cara a futuras historias de usuario (como HU10 - Persistencia de información) y reflejar un escenario más realista de desarrollo.

**Seguridad:** las contraseñas se almacenan hasheadas con `PasswordHasher<Usuario>` de ASP.NET Core Identity (PBKDF2 con salt aleatorio). Por el salt aleatorio, los usuarios de prueba se siembran al arrancar la app y no con `HasData` de EF Core, ya que este último generaría una migración nueva en cada arranque.

## Implementación

### Cómo ejecutar el proyecto localmente

1. Clonar el repositorio: `git clone https://github.com/aylinariasbarrantes-ux/KyroSofft.git`
2. Abrir `SistemaReservasLaboratorios.sln` en Visual Studio.
3. Restaurar los paquetes NuGet (Visual Studio lo hace automáticamente al abrir el proyecto).
4. Ejecutar el proyecto con F5 (o el botón "Run").

Al arrancar, la aplicación aplica automáticamente las migraciones pendientes y crea la base de datos `ReservasLaboratoriosDb` en SQL Server LocalDB. Si la tabla `Usuarios` está vacía, siembra los usuarios de prueba con la contraseña hasheada (ver `Data/DbSeeder.cs`). Por eso **no es necesario ejecutar `Update-Database` manualmente** para empezar a usar la app; el comando sigue disponible si se desea aplicar la migración sin arrancar la aplicación.

5. Navegar a `https://localhost:{puerto}/Login` para acceder al login.

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
| 4 | Usuario inexistente | noexiste / admin123 | Mensaje "Usuario o contraseña incorrectos." | Correcto |
| 5 | Campos vacíos | (vacío) / (vacío) | Mensaje "Debe ingresar usuario y contraseña." | Correcto |


Pruebas realizadas para HU-2 - Listar laboratorios:

| # | Prueba | Datos | Resultado esperado | Resultado obtenido |
|---|--------|-------|---------------------|---------------------|
| 1 | La lista muestra los laboratorios | Pantalla /Reserva, selector de laboratorio | Se ven Laboratorio A, B y C | Correcto |
| 2 | Datos del laboratorio correctos | Detalle de reserva y coincidencia de horarios | Se ven nombre, ubicación y capacidad | Parcial: el admin ve nombre y ubicación; usuario1 solo ve nombre y horario; la capacidad no se muestra en ninguna pantalla |
| 3 | Estado del laboratorio visible | Comparar los 3 laboratorios | Se distingue "Habilitado" de "Fuera de servicio" | Pendiente: el estado del laboratorio no se muestra en la pantalla |
| 4 | Selección de laboratorio | Cambiar de Laboratorio A a B y a C | El calendario cambia al laboratorio elegido | Correcto |

Pruebas realizadas para HU-3 - Consultar disponibilidad:

| # | Prueba | Datos | Resultado esperado | Resultado obtenido |
|---|--------|-------|---------------------|---------------------|
| 5 | Horario válido y disponible | Lab B, 04/10/2026, 8:00 a 9:00 a. m. | Disponible | Correcto: el calendario muestra el horario libre (la consulta se hace en el calendario, sin formulario) |
| 6 | Horario con conflicto | Lab A, 04/10/2026, 2:00 a 3:00 p. m. | No disponible | Correcto: el calendario muestra el horario ocupado por la reserva de 1:30 a 3:30 p. m. |
| 7 | Hora final antes que inicial | Inicio 2:00 p. m., fin 1:00 p. m. | Mensaje de error | Pendiente: no hay formulario donde ingresar horas |
| 8 | Fecha anterior a hoy | 30/09/2026 | Mensaje de error | Pendiente: no hay formulario donde ingresar fecha |
| 9 | Laboratorio fuera de servicio | Laboratorio "Fuera de servicio", horario válido | Nunca disponible | Pendiente: el estado del laboratorio no es visible |
| 10 | Reserva cancelada | Reserva con Estado "Cancelada" | No afecta la disponibilidad | Pendiente: no hay reservas canceladas cargadas |