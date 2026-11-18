using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
if (builder.Environment.IsProduction())
{
    var databaseUrl = builder.Configuration["DATABASE_URL"]
        ?? throw new InvalidOperationException("Render must provide DATABASE_URL.");
    var postgresConnection = DatabaseConnectionFactory.BuildPostgresConnectionString(databaseUrl);
    builder.Services.AddDbContext<LabDbContext>(options => options.UseNpgsql(postgresConnection));
}
else
{
    var sqliteConnection = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=lab14-inventory.db";
    builder.Services.AddDbContext<LabDbContext>(options => options.UseSqlite(sqliteConnection));
}
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.SwaggerDoc("v1", new OpenApiInfo
{ Title = "Lab 14 · Inventario", Version = "v1", Description = "API para gestionar productos y categorías." }));

var app = builder.Build();

// Render initializes its cloud PostgreSQL schema on startup.
// Local development uses the database created and managed from DataGrip.
if (app.Environment.IsProduction())
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<LabDbContext>();
    await db.Database.EnsureCreatedAsync();
    if (!await db.Categories.AnyAsync())
    {
        db.Categories.AddRange(
            new Category { Name = "Accesorios", Description = "Accesorios de computación" },
            new Category { Name = "Periféricos", Description = "Dispositivos de entrada y salida" });
        await db.SaveChangesAsync();
    }
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).WithTags("Sistema");
app.MapGet("/api/categories", async (LabDbContext db) =>
    await db.Categories.AsNoTracking().OrderBy(category => category.Name).ToListAsync()).WithTags("Categorías");

app.MapPost("/api/categories", async (CategoryInput input, LabDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(input.Name))
        return Results.BadRequest(new { message = "El nombre de la categoría es obligatorio." });

    var category = new Category { Name = input.Name.Trim(), Description = input.Description?.Trim() };
    db.Categories.Add(category);
    try
    {
        await db.SaveChangesAsync();
    }
    catch (DbUpdateException)
    {
        return Results.Conflict(new { message = "Ya existe una categoría con ese nombre." });
    }

    return Results.Created($"/api/categories/{category.Id}", category);
});

app.MapGet("/api/products", async (LabDbContext db) =>
    await db.Products.AsNoTracking().Include(product => product.Category)
        .OrderBy(product => product.Name)
        .Select(product => new ProductView(product.Id, product.Name, product.Price,
            product.CategoryId, product.Category.Name))
        .ToListAsync());

app.MapPost("/api/products", async (ProductInput input, LabDbContext db) =>
{
    var error = await ValidateProduct(input, db);
    if (error is not null) return Results.BadRequest(new { message = error });

    var product = new Product
    {
        Name = input.Name.Trim(),
        Price = input.Price,
        CategoryId = input.CategoryId
    };
    db.Products.Add(product);
    await db.SaveChangesAsync();
    return Results.Created($"/api/products/{product.Id}", product);
});

app.MapPut("/api/products/{id:int}", async (int id, ProductInput input, LabDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound();

    var error = await ValidateProduct(input, db);
    if (error is not null) return Results.BadRequest(new { message = error });

    product.Name = input.Name.Trim();
    product.Price = input.Price;
    product.CategoryId = input.CategoryId;
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/api/products/{id:int}", async (int id, LabDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound();

    db.Products.Remove(product);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();

static async Task<string?> ValidateProduct(ProductInput input, LabDbContext db)
{
    if (string.IsNullOrWhiteSpace(input.Name)) return "Escribe el nombre del producto.";
    if (input.Price < 0) return "El precio no puede ser negativo.";
    return await db.Categories.AnyAsync(category => category.Id == input.CategoryId)
        ? null
        : "Selecciona una categoría válida.";
}

sealed class LabDbContext(DbContextOptions<LabDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>().HasIndex(category => category.Name).IsUnique();
        modelBuilder.Entity<Product>().Property(product => product.Price).HasPrecision(10, 2);
        modelBuilder.Entity<Product>()
            .HasOne(product => product.Category)
            .WithMany(category => category.Products)
            .HasForeignKey(product => product.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

sealed class Category
{
    public int Id { get; set; }
    [Required, MaxLength(80)] public string Name { get; set; } = "";
    [MaxLength(240)] public string? Description { get; set; }
    public List<Product> Products { get; set; } = [];
}

sealed class Product
{
    public int Id { get; set; }
    [Required, MaxLength(120)] public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}

sealed record CategoryInput(string Name, string? Description);
sealed record ProductInput(string Name, decimal Price, int CategoryId);
sealed record ProductView(int Id, string Name, decimal Price, int CategoryId, string CategoryName);

static class DatabaseConnectionFactory
{
    public static string BuildPostgresConnectionString(string databaseUrl)
    {
        var uri = new Uri(databaseUrl);
        var credentials = uri.UserInfo.Split(':', 2);
        if (credentials.Length != 2 || string.IsNullOrWhiteSpace(uri.AbsolutePath.Trim('/')))
            throw new InvalidOperationException("DATABASE_URL is not a valid PostgreSQL URL.");

        return new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = uri.AbsolutePath.Trim('/'),
            Username = Uri.UnescapeDataString(credentials[0]),
            Password = Uri.UnescapeDataString(credentials[1]),
            SslMode = SslMode.Prefer
        }.ConnectionString;
    }
}
