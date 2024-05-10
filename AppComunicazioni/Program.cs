using apiSanges.Service;
using AppComunicazioni.Data;
using AppComunicazioni.Properties;
using Microsoft.EntityFrameworkCore;
<<<<<<< HEAD

=======
>>>>>>> a83e006b9b45e528823e5d74a65e458bed30f817

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ComDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ComDbContext")));
builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

<<<<<<< HEAD

=======
>>>>>>> a83e006b9b45e528823e5d74a65e458bed30f817
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
<<<<<<< HEAD

=======
>>>>>>> a83e006b9b45e528823e5d74a65e458bed30f817
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
