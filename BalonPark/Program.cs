using System.Diagnostics;
using BalonPark.Data;
using BalonPark.Services;
using BalonPark.Pages.Admin;
using BalonPark.Middleware;
using Serilog;

// Serilog Yapılandırması - appsettings.json'dan okuyacak ama önce basic config
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information() // Development için Information seviyesi
    .WriteTo.Console() // Console'a yaz
    .WriteTo.File(
        path: "logs/error-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
        retainedFileCountLimit: 30,
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error) // Dosyaya sadece Error
    .CreateLogger();

try
{
    Log.Information("Serilog yapılandırması başlatılıyor...");

    var builder = WebApplication.CreateBuilder(args);

    // Serilog'u ASP.NET Core'a entegre et ve appsettings.json'dan yapılandırmayı oku
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.File(
            path: "logs/error-.txt",
            rollingInterval: RollingInterval.Day,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}",
            retainedFileCountLimit: 30,
            restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error));

    // Add services to the container.
    builder.Services.AddRazorPages();
    builder.Services.AddControllers();
    builder.Services.AddHttpContextAccessor();

    // Session Configuration - Admin oturumu çıkış yapılana kadar açık kalsın
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromDays(30); // 30 gün; çıkış yapılana kadar oturum düşmez
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.Name = ".BalonPark.Session";
    });

    // Memory Cache Configuration
    builder.Services.AddMemoryCache();

    // Dapper Context
    builder.Services.AddSingleton<DapperContext>();

    // SQL Migration Runner (uygulama başlarken Migrations/*.sql çalıştırılır)
    builder.Services.AddScoped<SqlMigrationRunner>();

    // Repositories (Örnek)
    builder.Services.AddScoped<ExampleRepository>();
    builder.Services.AddScoped<SettingsRepository>();
    builder.Services.AddScoped<CategoryRepository>();
    builder.Services.AddScoped<SubCategoryRepository>();
    builder.Services.AddScoped<ProductRepository>();
    builder.Services.AddScoped<ProductImageRepository>();
    builder.Services.AddScoped<BlogRepository>();

    // Services
    builder.Services.AddHttpClient<CurrencyService>();
    builder.Services.AddScoped<CurrencyService>();
    builder.Services.AddScoped<ICurrencyCookieService, CurrencyCookieService>();
    builder.Services.AddScoped<ICacheService, CacheService>();
    builder.Services.AddScoped<IUrlService, UrlService>();
    builder.Services.AddScoped<IEmailService, EmailService>();

    // Mail Service - Singleton (connection pooling için)
    builder.Services.AddSingleton<IMailService, MailService>();

    // PDF Generation Service
    builder.Services.AddScoped<PdfService>();

    // Excel Generation Service
    builder.Services.AddScoped<ExcelService>();

    // Google Shopping Service
    builder.Services.AddScoped<IGoogleShoppingService, GoogleShoppingService>();

    // Blog Service
    builder.Services.AddScoped<IBlogService, BlogService>();

    // AI Service
    builder.Services.AddHttpClient<AiService>();
    builder.Services.AddScoped<IAiService, AiService>();

    var app = builder.Build();

    // SQL Migrations - uygulama başlarken Migrations klasöründeki scriptleri çalıştır
    using (var migrationScope = app.Services.CreateScope())
    {
        var migrationRunner = migrationScope.ServiceProvider.GetRequiredService<SqlMigrationRunner>();
        try
        {
            await migrationRunner.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "SQL migration çalıştırılırken hata (veritabanı erişilemiyor olabilir). Uygulama devam ediyor.");
        }
    }

    // BaseAdminPage için SettingsRepository'yi set et (cache'den Settings yükleme için)
    Log.Information("SettingsRepository yükleniyor...");
    using (var scope = app.Services.CreateScope())
    {
        var settingsRepository = scope.ServiceProvider.GetRequiredService<SettingsRepository>();
        BaseAdminPage.SetSettingsRepository(settingsRepository);
    }
    Log.Information("SettingsRepository yüklendi.");

    // Global Exception Handling Middleware - Tüm hataları yakala
    app.UseGlobalExceptionHandling();

    // Serilog Request Logging - Sadece Error ve üzeri
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.GetLevel = (httpContext, elapsed, ex) =>
        {
            // Sadece hatalı istekleri logla
            if (ex != null || httpContext.Response.StatusCode >= 500)
                return Serilog.Events.LogEventLevel.Error;
            if (httpContext.Response.StatusCode >= 400)
                return Serilog.Events.LogEventLevel.Warning;
            return Serilog.Events.LogEventLevel.Debug; // Normal istekler loglanmaz (min level Error)
        };
    });

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }
    else
    {
        app.UseDeveloperExceptionPage();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();

    // 404 ve diğer hata kodları için özel sayfa (404 = NotFound.cshtml)
    app.UseStatusCodePagesWithReExecute("/NotFound", "?statusCode={0}");

    app.UseSession();
    app.UseAuthorization();

    app.MapRazorPages();
    app.MapControllers();

    // Tarayıcıyı açacak URL (launchSettings / ASPNETCORE_URLS veya varsayılan)
    var urls = app.Urls.ToList();
    var openUrl = urls.FirstOrDefault()
        ?? app.Configuration["ASPNETCORE_URLS"]?.Split(';', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim()
        ?? "http://localhost:5152";
    if (!openUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        openUrl = "http://" + openUrl;
    if (!openUrl.EndsWith("/"))
        openUrl += "/";

    // Development ortamında sunucu ayağa kalktıktan sonra tarayıcıyı otomatik aç
    // Not: dotnet run/dotnet watch CLI'da launchBrowser güvenilir çalışmıyor; bu yöntem her ortamda çalışır.
    if (app.Environment.IsDevelopment())
    {
        app.Lifetime.ApplicationStarted.Register(() =>
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(1500);
                try
                {
                    if (OperatingSystem.IsWindows())
                        Process.Start(new ProcessStartInfo { FileName = openUrl, UseShellExecute = true });
                    else if (OperatingSystem.IsMacOS())
                        Process.Start("open", openUrl);
                    else
                        Process.Start("xdg-open", openUrl);
                    Log.Information("Tarayıcı açıldı: {Url}", openUrl);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Tarayıcı otomatik açılamadı. Adresi manuel açın: {Url}", openUrl);
                }
            });
        });
    }

    await app.RunAsync();
    Log.Information("Uygulama durduruldu.");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Uygulama başlatılırken kritik hata oluştu!");
    throw;
}
finally
{
    Log.Information("Uygulama kapatılıyor...");
    Log.CloseAndFlush();
}