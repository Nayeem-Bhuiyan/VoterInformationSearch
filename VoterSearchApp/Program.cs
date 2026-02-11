using Microsoft.AspNetCore.Http.Features;
using VoterSearchApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();
// Configure request size limits
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 1073741824; // 1GB in bytes
});
builder.Services.AddSingleton<IDataStorageService, JsonFileStorageService>();
builder.Services.AddScoped<IPdfParserService, BanglaPdfParserService>();

var app = builder.Build();
// Configure Kestrel server options
app.Lifetime.ApplicationStarted.Register(() =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Maximum request body size set to 1GB");
});
// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
// Increase request size limit middleware
app.Use(async (context, next) =>
{
    context.Features.Get<IHttpMaxRequestBodySizeFeature>()!.MaxRequestBodySize = 1073741824; // 1GB
    await next.Invoke();
});
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Voter}/{action=Index}/{id?}");

// Create necessary directories
var webRootPath = app.Environment.WebRootPath;
var directories = new[] { "data", "uploads" };
foreach (var dir in directories)
{
    var fullPath = Path.Combine(webRootPath, dir);
    if (!Directory.Exists(fullPath))
    {
        Directory.CreateDirectory(fullPath);
    }
}

app.Run();