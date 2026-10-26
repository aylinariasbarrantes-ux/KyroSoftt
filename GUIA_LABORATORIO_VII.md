# Laboratorio VII - KyroSoft

## Qué trae y qué falta hacer
Proyecto ASP.NET Core MVC con .NET 8, EF Core y SQL Server.
Ya existían pruebas xUnit de servicios, controladores y filtros.
Se agregó el pipeline; todavía hay que ejecutarlo y obtener evidencias en Azure DevOps.
No se ejecutaron compilación ni pruebas al preparar este ZIP: el entorno no tiene dotnet.

## Cambios realizados
1. Nuevo azure-pipelines.yml: push a main/master, agente Ubuntu, SDK 8,
   restore, build Release, todas las pruebas, TRX y publicación del reporte.
2. Nuevo SistemaReservasLaboratorios.Tests/Integration/AuthDatabaseIntegrationTests.cs:
   dos casos de integración de AuthService con EF Core, SQLite y PasswordHasher reales.
   Comprueba lectura desde un segundo contexto y aceptación/rechazo de contraseña.
3. SistemaReservasLaboratorios.Tests/SistemaReservasLaboratorios.Tests.csproj:
   se agregó Microsoft.EntityFrameworkCore.Sqlite 8.0.26, solo para pruebas.
4. Esta guía.
No se modificaron los controladores, servicios, vistas ni la conexión de la aplicación.
SQLite es una base temporal de pruebas: no prueba las migraciones ni particularidades
propias de SQL Server, ni el recorrido HTTP completo.

## 1. Preparar tu computadora
- Git instalado y acceso al repositorio del equipo (GitHub o Azure Repos).
- SDK .NET 8 instalado. Visual Studio por sí solo no confirma que tengas ese SDK.
- Cuenta y proyecto en Azure DevOps con permiso para crear pipelines.
- Agente disponible para ejecutar trabajos (Microsoft-hosted o uno propio).
- Internet para descargar paquetes NuGet.
Las pruebas no requieren arrancar la web ni configurar tu SQL Server local.
El CSS compilado de Tailwind ya viene incluido; este pipeline no regenera estilos.

## 2. Colocar los archivos
Descomprimí el ZIP. Si ya tenés el repositorio clonado, copiá dentro de él los cuatro
archivos indicados en Cambios realizados, respetando sus carpetas.
No copies la carpeta KyroSoftt-master como una subcarpeta adicional del repositorio.
El YAML y la solución .sln deben estar juntos en la raíz.
El ZIP descargado no contiene el historial .git: usá preferentemente el clon del repositorio.

## 3. Probar localmente
Abrí PowerShell o la terminal de Visual Studio en la carpeta que contiene la solución.
Ejecutá uno por uno:

```powershell
dotnet --list-sdks
dotnet restore SistemaReservasLaboratorios.sln
dotnet build SistemaReservasLaboratorios.sln --configuration Release --no-restore
dotnet test SistemaReservasLaboratorios.Tests/SistemaReservasLaboratorios.Tests.csproj --configuration Release --no-build --logger "trx;LogFileName=resultados.trx" --results-directory TestResults
```
Debe aparecer un SDK 8.x y el resumen de pruebas aprobadas. Si falla un paso,
corregí el primer error antes de seguir. No hay resultados aprobados garantizados en este ZIP.
El reporte local queda en TestResults/resultados.trx.

## 4. Subir a tu repositorio
Desde tu repositorio clonado, comprobá rama y remoto:

```powershell
git branch --show-current
git remote -v
git status
```
Si estás en main o master, agregá únicamente los archivos del laboratorio:

```powershell
git add azure-pipelines.yml GUIA_LABORATORIO_VII.md SistemaReservasLaboratorios.Tests/SistemaReservasLaboratorios.Tests.csproj SistemaReservasLaboratorios.Tests/Integration/AuthDatabaseIntegrationTests.cs
git commit -m "Agrega pipeline CI y pruebas de integracion"
git push
```
Si trabajás en otra rama, integrá los cambios a main/master mediante el flujo del equipo.
No uses git init ni cambies el remoto encima de un repositorio que ya tiene historial.
Si nadie tiene un clon: cloná primero el repositorio usando su URL real y copiá los
archivos encima. Si aún no existe un repositorio, crealo y subí el proyecto completo.

## 5. Crear el pipeline en Azure DevOps
1. Entrá al proyecto del equipo en https://dev.azure.com/.
2. Abrí Pipelines y seleccioná New pipeline (o Create Pipeline).
3. Elegí GitHub o Azure Repos Git según dónde esté el código.
4. Seleccioná el repositorio; autorizá el acceso si lo pide.
5. Elegí Existing Azure Pipelines YAML file.
6. Seleccioná main o master, y el archivo /azure-pipelines.yml.
7. Continuá y seleccioná Run.
8. Si pide autorización para usar recursos del proyecto, revisá y autorizá los del pipeline.
La primera ejecución puede ser manual: la evidencia de CI viene del siguiente push.

## 6. Revisar el resultado
Abrí la ejecución. Revisá instalación de SDK, restauración, compilación y pruebas.
Todo debe finalizar correctamente. En Tests revisá el total y los detalles.
En los artefactos de la ejecución estará reportes-pruebas con el TRX si todo terminó bien.
No se agregó publicación del programa: el laboratorio la marca como opcional.

## 7. Demostrar el disparo automático
Modificá una frase de esta guía, guardá y ejecutá:

```powershell
git add GUIA_LABORATORIO_VII.md
git commit -m "Comprueba ejecucion automatica CI"
git push
```
Sin presionar Run, comprobá que aparezca una nueva ejecución para ese commit.
Repetí con otro cambio pequeño para mostrar varias ejecuciones automáticas.

## 8. Evidencias para entregar
- Enlace al pipeline que funciona en la nube.
- azure-pipelines.yml comentado.
- Captura del historial con ejecuciones automáticas y sus commits.
- Captura de una ejecución exitosa con los pasos visibles.
- Captura de Tests con resultados, incluidas las pruebas de integración.
- Archivo TRX descargado o evidencia del reporte generado.
El PDF no exige un formato específico de Word: seguí las indicaciones del docente
sobre cómo adjuntar estas evidencias.

## Problemas frecuentes
- No hosted parallelism / sin capacidad de ejecución: la organización necesita
  habilitar capacidad para agentes hospedados o configurar un agente propio.
  Se resuelve en Azure DevOps; cambiar el YAML por sí solo no habilita capacidad.
- dotnet no se reconoce: instalá el SDK .NET 8 y volvé a abrir la terminal.
- No aparece ejecución tras push: verificá rama main/master, pipeline creado y
  archivo YAML en la raíz. Un commit local sin push no dispara CI.
- Falla restore: revisá el paquete indicado y acceso a NuGet.
- Falla una prueba existente: abrí el primer fallo y su detalle; no la deshabilités
  para que el pipeline aparezca verde.
- Si vas a ejecutar la web en tu PC, necesitás tu SQL Server y una conexión válida;
  eso es independiente de ejecutar estas pruebas aisladas.

## Referencias técnicas
- https://learn.microsoft.com/en-us/azure/devops/pipelines/ecosystems/dotnet-core?view=azure-devops
- https://learn.microsoft.com/en-us/azure/devops/pipelines/tasks/reference/publish-test-results-v2?view=azure-pipelines
