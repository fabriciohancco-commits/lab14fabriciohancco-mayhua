using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=lab14fabriciohancco.db";
builder.Services.AddDbContext<LabDbContext>(options => options.UseSqlite(connectionString));
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LabDbContext>();
    await db.Database.EnsureCreatedAsync();
    if (!await db.Categories.AnyAsync())
    {
        db.Categories.AddRange(new Category { Name = "Accesorios", Description = "Accesorios de computación" }, new Category { Name = "Periféricos", Description = "Dispositivos de entrada y salida" });
        await db.SaveChangesAsync();
    }
}

app.MapGet("/", () => Results.Content("""
<!doctype html><html lang="es"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Lab 14 · Inventario</title><style>body{font:16px system-ui;max-width:850px;margin:3rem auto;padding:0 1rem;color:#172033}h1{color:#174ea6}form{display:flex;gap:.6rem;flex-wrap:wrap;margin:1rem 0}input,button{padding:.65rem;border:1px solid #ccd3df;border-radius:8px}button{background:#174ea6;color:white;cursor:pointer}li{padding:.55rem;border-bottom:1px solid #eee}.muted{color:#657084}</style></head><body><h1>Inventario · Laboratorio 14</h1><p class="muted">API .NET 8 con base de datos relacional SQLite.</p><form id="form"><input id="name" placeholder="Nombre del producto" required><input id="price" type="number" min="0" step="0.01" placeholder="Precio" required><input id="category" type="number" min="1" value="1" placeholder="ID categoría" required><button>Agregar</button></form><ul id="products"></ul><script>async function load(){let r=await fetch('/api/products');let p=await r.json();document.querySelector('#products').innerHTML=p.map(x=>`<li><b>${x.name}</b> — S/ ${x.price.toFixed(2)} · ${x.categoryName} <button onclick="del(${x.id})">Eliminar</button></li>`).join('')||'<li>Sin productos todavía</li>'}async function del(id){await fetch('/api/products/'+id,{method:'DELETE'});load()}document.querySelector('#form').onsubmit=async e=>{e.preventDefault();let r=await fetch('/api/products',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({name:document.querySelector('#name').value,price:Number(document.querySelector('#price').value),categoryId:Number(document.querySelector('#category').value)})});if(!r.ok){alert(await r.text());return}e.target.reset();load()};load();</script></body></html>
""", "text/html; charset=utf-8"));
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/api/categories", async (LabDbContext db) => await db.Categories.OrderBy(c => c.Id).ToListAsync());
app.MapPost("/api/categories", async (CategoryInput input, LabDbContext db) => { var category = new Category { Name = input.Name.Trim(), Description = input.Description?.Trim() }; db.Categories.Add(category); await db.SaveChangesAsync(); return Results.Created($"/api/categories/{category.Id}", category); });
app.MapGet("/api/products", async (LabDbContext db) => await db.Products.Include(p => p.Category).OrderBy(p => p.Id).Select(p => new ProductView(p.Id, p.Name, p.Price, p.CategoryId, p.Category.Name)).ToListAsync());
app.MapPost("/api/products", async (ProductInput input, LabDbContext db) => { if (string.IsNullOrWhiteSpace(input.Name) || input.Price < 0) return Results.BadRequest("El nombre es obligatorio y el precio no puede ser negativo."); if (!await db.Categories.AnyAsync(c => c.Id == input.CategoryId)) return Results.BadRequest("La categoría indicada no existe."); var product = new Product { Name = input.Name.Trim(), Price = input.Price, CategoryId = input.CategoryId }; db.Products.Add(product); await db.SaveChangesAsync(); return Results.Created($"/api/products/{product.Id}", product); });
app.MapPut("/api/products/{id:int}", async (int id, ProductInput input, LabDbContext db) => { var product = await db.Products.FindAsync(id); if (product is null) return Results.NotFound(); if (string.IsNullOrWhiteSpace(input.Name) || input.Price < 0 || !await db.Categories.AnyAsync(c => c.Id == input.CategoryId)) return Results.BadRequest("Revisa el nombre, precio y categoría."); product.Name = input.Name.Trim(); product.Price = input.Price; product.CategoryId = input.CategoryId; await db.SaveChangesAsync(); return Results.Ok(product); });
app.MapDelete("/api/products/{id:int}", async (int id, LabDbContext db) => { var product = await db.Products.FindAsync(id); if (product is null) return Results.NotFound(); db.Products.Remove(product); await db.SaveChangesAsync(); return Results.NoContent(); });
app.Run();

sealed class LabDbContext(DbContextOptions<LabDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>(); public DbSet<Product> Products => Set<Product>();
    protected override void OnModelCreating(ModelBuilder modelBuilder) { modelBuilder.Entity<Category>().HasIndex(c => c.Name).IsUnique(); modelBuilder.Entity<Product>().Property(p => p.Price).HasPrecision(10, 2); modelBuilder.Entity<Product>().HasOne(p => p.Category).WithMany(c => c.Products).HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict); }
}
sealed class Category { public int Id { get; set; } [Required, MaxLength(80)] public string Name { get; set; } = ""; [MaxLength(240)] public string? Description { get; set; } public List<Product> Products { get; set; } = []; }
sealed class Product { public int Id { get; set; } [Required, MaxLength(120)] public string Name { get; set; } = ""; public decimal Price { get; set; } public int CategoryId { get; set; } public Category Category { get; set; } = null!; }
sealed record CategoryInput(string Name, string? Description);
sealed record ProductInput(string Name, decimal Price, int CategoryId);
sealed record ProductView(int Id, string Name, decimal Price, int CategoryId, string CategoryName);
