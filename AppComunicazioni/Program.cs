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

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5185); // Ascolta su tutte le interfacce alla porta 5185
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.MimeTypes = new[] { "text/javascript", "text/css", "application/json" };
});

ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

// Configurazione del contesto del database - Scoped è corretto per evitare problemi di concorrenza
builder.Services.AddDbContext<ComDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ComDbContext")));

// Servizi di contesto HTTP e sessione
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Timeout della sessione
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Registrazione dei servizi
builder.Services.AddHostedService<CleanupBackgroundService>();
builder.Services.AddHostedService<NotificationBackgroundService>();

builder.Services.AddScoped<IEmailService, EmailService>(); // Servizio legato alla logica email
builder.Services.AddScoped<IMonitoringService, MonitoringService>(); // Monitoraggio
builder.Services.AddScoped<IFiltroComunicazioniService, FiltroComunicazioniService>(); // Filtro comunicazioni
builder.Services.AddScoped<IContabilitàService, ContabilitàService>(); // Servizio contabilità
builder.Services.AddScoped<IExcelService, ExcelService>(); // Servizio legato alla logica file excel
builder.Services.AddScoped<ISendMailService, SendMailService>(); // Servizio all'invio email di smarco
builder.Services.AddScoped<IViewBagService, ViewBagService>(); // Servizio di ViewBag per la view dei Servizi nelle select
builder.Services.AddScoped<IRetryService, RetryService>();

builder.Services.AddScoped<CleanupService>(); // Servizio pulizia dati vecchi

// Registrazione AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Aggiunta delle funzionalità per HTTP Client, MVC e API Explorer
builder.Services.AddHttpClient();
builder.Services.AddMvc(); // AddMvc è più flessibile per l'uso di API e Views insieme
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseCors("AllowAll");
app.UseResponseCompression();

// Configurazione della cultura - Impostata prima per garantire coerenza durante tutte le richieste
var cultureInfo = new CultureInfo("it-IT");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// Gestione degli errori e delle pagine di stato
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

// Aggiungi middleware di compressione
app.Use(async (context, next) =>
{
    if (!context.Request.Headers.ContainsKey("Accept-Encoding"))
    {
        context.Request.Headers["Accept-Encoding"] = "gzip, br";
    }
    await next.Invoke();
});

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Proteggi file specifici, ad esempio site.js
        var path = ctx.File.PhysicalPath;
        if (path.EndsWith("site.js"))
        {
            // Imposta cache e header per protezione
            ctx.Context.Response.Headers.Append("Cache-Control", "no-store");
            ctx.Context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        }
    }
});


// Uso della sessione
app.UseSession();

// Configurazione dei file statici
app.UseStaticFiles();

// Routing
app.UseRouting();

// Autorizzazione - Posizionato correttamente per proteggere le risorse dopo il routing
app.UseAuthorization();

// Mappatura delle route per il controller - Posizionata dopo UseRouting per assicurare che le route siano configurate correttamente
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
