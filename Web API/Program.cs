using CertificateManager.Data;
using CertificateManager.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseWindowsService();

builder.WebHost.ConfigureKestrel((context, options) =>
{
    var port = context.Configuration.GetValue<int>("ListeningPort", 5301);
    options.ListenAnyIP(port, listenOptions =>
    {
        var certConfig = context.Configuration.GetSection("Certificate");
        var storeName = certConfig.GetValue<string>("StoreName");
        var thumbprint = certConfig.GetValue<string>("Thumbprint");

        if (!string.IsNullOrEmpty(storeName) && !string.IsNullOrEmpty(thumbprint))
        {
            listenOptions.UseHttps(httpsOptions =>
            {
                var store = new System.Security.Cryptography.X509Certificates.X509Store(
                    storeName, System.Security.Cryptography.X509Certificates.StoreLocation.LocalMachine);
                store.Open(System.Security.Cryptography.X509Certificates.OpenFlags.ReadOnly);
                var certs = store.Certificates.Find(
                    System.Security.Cryptography.X509Certificates.X509FindType.FindByThumbprint, thumbprint, false);
                store.Close();

                if (certs.Count == 0)
                    throw new InvalidOperationException($"Certificate with thumbprint '{thumbprint}' not found in store '{storeName}'.");

                httpsOptions.ServerCertificate = certs[0];
            });
            listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
        }
    });
});

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' not found.");

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddTransient<CertificateService>();
builder.Services.AddTransient<Validation>();
builder.Services.AddHttpClient();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
