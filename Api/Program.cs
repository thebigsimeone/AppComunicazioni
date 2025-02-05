using AppComunicazioni.Data;
using AppComunicazioni.Interface;
using AppComunicazioni.Properties;
using AppComunicazioni.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "AppComunicazioni")
});


// Configura Kestrel per ascoltare su IP e porta specifica
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5280); // Usa la stessa porta di AppComunicazioni
});

// Leggi la stringa di connessione
var connectionString = builder.Configuration.GetConnectionString("ComDbContext");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("La stringa di connessione non è stata trovata.");
}

// Configura il DbContext
builder.Services.AddDbContext<ComDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddControllers();

builder.Services.AddScoped<IExcelService, ExcelService>();
builder.Services.AddScoped<IFornitoreService, FornitoreService>();

builder.Services.AddAutoMapper(typeof(MappingProfile));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configura Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AppComunicazioni API",
        Version = "v1",
        Description = "API per la gestione delle comunicazioni e il processing dei file Excel"
    });
});

var app = builder.Build();

// Middleware per Swagger in modalità sviluppo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "AppComunicazioni API v1");
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
