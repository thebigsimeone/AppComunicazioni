using apiSanges.Service;
using AppComunicazioni.Data;
using AppComunicazioni.Properties;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ComDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ComDbContext")));
builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();


builder.Services.AddControllersWithViews();
builder.Services.AddAutoMapper(typeof(MappingProfile));
builder.Services.AddSingleton<IEmailService, EmailService>();


var app = builder.Build();

var supportedCultures = new[] { "it-IT" };
var localizationOptions = new RequestLocalizationOptions().SetDefaultCulture(supportedCultures[0])
                                                          .AddSupportedCultures(supportedCultures)
                                                          .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{

    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
