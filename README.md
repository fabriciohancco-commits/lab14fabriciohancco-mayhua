# Laboratorio 14 — Inventario .NET 8
API web para productos y categorías. La relación es uno a muchos: cada categoría agrupa productos y cada producto referencia su categoría con una clave foránea.

## Ejecutar en Rider
Abre `lab14fabriciohancco.sln` con .NET 8 y ejecuta el proyecto. EF Core crea `lab14fabriciohancco.db` con dos categorías iniciales. La página principal permite listar, agregar y eliminar productos.

## Conectar DataGrip
1. Crea una fuente de datos **SQLite**.
2. Elige el archivo `lab14fabriciohancco/lab14fabriciohancco.db`, creado tras el primer inicio de la aplicación.
3. Pulsa **Test Connection**; si se solicita, descarga el driver SQLite y guarda la conexión.

Tablas: `Categories` y `Products`; FK `Products.CategoryId` → `Categories.Id`.

## Publicación local
Desde la carpeta de la solución ejecuta:
```powershell
dotnet publish .\lab14fabriciohancco\lab14fabriciohancco.csproj -c Release -o .\published
```

## GitHub Actions y Render
`.github/workflows/dotnet.yml` restaura, compila en Release, publica y almacena el artefacto al subir cambios a `main` o `master`. `Dockerfile` y `render.yaml` sirven para crear el Web Service en Render; al conectar el repositorio se puede habilitar el despliegue automático.

Render Free no conserva archivos locales entre despliegues, así que la SQLite de demostración allí puede reiniciarse. En local el archivo persiste y se abre en DataGrip.

## Conclusiones
- La publicación en Release genera una salida lista para ejecutar y separada de los fuentes.
- GitHub Actions automatiza restauración, compilación y publicación del artefacto.
- Render conecta el repositorio con una aplicación accesible mediante una URL.
- Las claves primarias y foráneas entre categorías y productos mantienen la integridad relacional.
