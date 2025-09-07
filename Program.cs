using FotoPrint.Services;
using System.IO.Compression;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddSingleton<ConfigService>();
builder.Services.AddSingleton<FileService>();
builder.Services.AddHostedService<PrintSchedulerService>();

// Ouvir em todas interfaces na porta 5000 (HTTP)
builder.WebHost.UseUrls("http://0.0.0.0:5000");

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();

// Endpoint para baixar ZIP do Backup
app.MapGet("/download-zip", (IWebHostEnvironment env) =>
{
    var backupDir = Path.Combine(env.WebRootPath, "Backup");
    if (!Directory.Exists(backupDir)) return Results.NotFound();

    var tempZip = Path.Combine(Path.GetTempPath(), $"backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}.zip");
    if (File.Exists(tempZip)) File.Delete(tempZip);

    ZipFile.CreateFromDirectory(backupDir, tempZip);
    var bytes = File.ReadAllBytes(tempZip);
    File.Delete(tempZip);

    return Results.File(bytes, "application/zip", "backup_fotos.zip");
});

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
