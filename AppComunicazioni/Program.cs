using apiSanges.Service;
using AppComunicazioni.Data;
using AppComunicazioni.Interface;
using AppComunicazioni.Properties;
using AppComunicazioni.Service;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

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

// Registrazione dei servizi - Utilizzo di Scoped, Transient e Singleton in base alle esigenze
builder.Services.AddHostedService<NotificationBackgroundService>(); // Hosted service
builder.Services.AddScoped<IEmailService, EmailService>(); // Servizio legato alla logica email
builder.Services.AddScoped<IMonitoringService, MonitoringService>(); // Monitoraggio
builder.Services.AddScoped<IFiltroComunicazioniService, FiltroComunicazioniService>(); // Filtro comunicazioni
builder.Services.AddScoped<IContabilitaService, ContabilitaService>(); // Servizio contabilità

// Registrazione AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Aggiunta delle funzionalità per HTTP Client, MVC e API Explorer
builder.Services.AddHttpClient();
builder.Services.AddMvc(); // AddMvc è più flessibile per l'uso di API e Views insieme
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

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
