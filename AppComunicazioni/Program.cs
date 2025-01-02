using apiSanges.Service;
using AppComunicazioni.Data;
using AppComunicazioni.Interface;
using AppComunicazioni.Properties;
using AppComunicazioni.Service;
using AppComunicazioni.Services;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Chiave di crittografia - 32 caratteri per AES
var encryptionKey = "12345678901234567890123456789012";
builder.Services.AddSingleton<IEncryptionService>(provider => new EncryptionService(encryptionKey));

// Configurazione Kestrel
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5185);
});

// Configurazione CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Abilita la compressione delle risposte
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.MimeTypes = new[] { "text/javascript", "text/css", "application/json" };
});

ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

// Configurazione del contesto del database
builder.Services.AddDbContext<ComDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ComDbContext")
    ?? throw new InvalidOperationException("La stringa di connessione non può essere null.")));

// Servizi HTTP e sessione
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Timeout sessione
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true; // Necessario per GDPR
});

// Registrazione dei servizi
builder.Services.AddHostedService<CleanupBackgroundService>();
builder.Services.AddHostedService<NotificationBackgroundService>();

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISendMailService, SendMailService>();

builder.Services.AddScoped<IMonitoringService, MonitoringService>();
builder.Services.AddScoped<IStopMonitoringService, StopMonitoringService>();

builder.Services.AddScoped<IFiltroComunicazioniService, FiltroComunicazioniService>();
builder.Services.AddScoped<IPaginationService, PaginationService>();

builder.Services.AddScoped<IContabilitàService, ContabilitàService>();

builder.Services.AddScoped<IExcelService, ExcelService>();
builder.Services.AddScoped<IViewBagService, ViewBagService>();
builder.Services.AddScoped<IRetryService, RetryService>();

builder.Services.AddScoped<CleanupService>();

// Registrazione di AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Aggiunta delle funzionalità per HTTP Client, MVC e API Explorer
builder.Services.AddHttpClient();
builder.Services.AddMvc();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Verifica app.Environment per prevenire possibili null
if (app.Environment != null)
{
    app.UseCors("AllowAll");
    app.UseResponseCompression();
    // Configurazione della cultura  
    var cultureInfo = new CultureInfo("it-IT");
    CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
    CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

    // Gestione degli errori  
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        app.UseStatusCodePagesWithReExecute("/error/{0}");
        app.UseHsts();
    }
    else
    {
        app.UseDeveloperExceptionPage();
    }

    // Middleware di compressione  
    app.Use(async (context, next) =>
    {
        if (!context.Request.Headers.ContainsKey("Accept-Encoding"))
        {
            context.Request.Headers["Accept-Encoding"] = "gzip, br";
        }
        await next.Invoke();
    });

    app.UseStaticFiles();
    app.UseSession();
    app.UseRouting();
    app.UseAuthorization();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.Run();
}
else
{
    throw new InvalidOperationException("La configurazione di 'app.Environment' è null. Verificare il contesto di esecuzione.");
}