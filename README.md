# Laboratorio 14 — Inventario .NET 8

Aplicación para registrar productos relacionados con categorías. La pantalla permite buscar, agregar, editar y eliminar productos; cada cambio se guarda en SQLite.

## Ejecutar en Rider

Abre `lab14fabriciohancco.sln` y ejecuta el perfil `http`. El proyecto local usa la base gestionada desde DataGrip en `C:/Users/user/DataGripProjects/default/lab14-inventory.db`.

## Base de datos en DataGrip

La fuente `Lab14 Inventario` apunta al archivo SQLite externo al código. Las tablas son `Categories` y `Products`, con la clave foránea `Products.CategoryId` → `Categories.Id`. Si necesitas recrear el esquema, abre `database/setup.sql` en DataGrip y ejecútalo sobre esa fuente. La configuración local no incorpora el archivo `.db` al repositorio.

## Publicar en Release

Desde la carpeta de la solución:

```powershell
dotnet publish .\lab14fabriciohancco\lab14fabriciohancco.csproj -c Release -o .\published
```

Verifica que `published/` incluya la aplicación y sus dependencias.

## GitHub Actions y Render

`.github/workflows/dotnet.yml` restaura, compila en Release, publica y guarda el artefacto en cada push a `main`. `Dockerfile` y `render.yaml` configuran un Web Service para conectar desde Render al repositorio.

La instancia gratuita de Render usa un archivo SQLite temporal; los datos de producción pueden reiniciarse al redesplegar.

## Conclusiones

- Publicar en Release crea una salida separada lista para ejecutar.
- GitHub Actions automatiza compilación y publicación del artefacto.
- Render permite conectar el repositorio con una aplicación web desplegada.
- Las claves primarias y foráneas relacionan productos con categorías y mantienen la integridad de los datos.
