using System.Diagnostics;
using BalonPark.Data;
using BalonPark.Services;
using BalonPark.Services.Accounting;
using BalonPark.Pages.Admin;
using BalonPark.Middleware;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Sinks.MSSqlServer;

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
    builder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.File(
                path: "logs/error-.txt",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}",
                retainedFileCountLimit: 30,
                restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error);

        // Error seviyesindeki logları SQL Server'a yaz (ConnectionString varsa)
        var connectionString = context.Configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            var sinkOptions = new MSSqlServerSinkOptions
            {
                TableName = "ErrorLogs",
                SchemaName = "dbo",
                AutoCreateSqlTable = true
            };
            configuration.WriteTo.MSSqlServer(
                connectionString: connectionString,
                sinkOptions: sinkOptions,
                restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error);
        }
    });

    // Türkçe kültür: model binder ondalık/tarih ayırıcılarını tr-TR ile yorumlar (ör. 23.333,33 → decimal)
    var trCulture = new System.Globalization.CultureInfo("tr-TR");
    builder.Services.Configure<Microsoft.AspNetCore.Builder.RequestLocalizationOptions>(opts =>
    {
        opts.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(trCulture);
        opts.SupportedCultures = [trCulture];
        opts.SupportedUICultures = [trCulture];
        // Tarayıcı/cookie/query-string ile kültür değiştirilmesin; uygulama hep tr-TR kullanır.
        opts.RequestCultureProviders = [];
    });

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

    // Repositories
    builder.Services.AddScoped<SettingsRepository>();
    builder.Services.AddScoped<CategoryRepository>();
    builder.Services.AddScoped<SubCategoryRepository>();
    builder.Services.AddScoped<ProductRepository>();
    builder.Services.AddScoped<ProductImageRepository>();
    builder.Services.AddScoped<BlogRepository>();
    builder.Services.AddScoped<ErrorLogRepository>();
    builder.Services.AddScoped<AccountingCompanyRepository>();
    builder.Services.AddScoped<CounterpartyRepository>();
    builder.Services.AddScoped<InvoiceRepository>();
    builder.Services.AddScoped<InvoiceAttachmentRepository>();
    builder.Services.AddScoped<AccountMovementRepository>();

    builder.Services.Configure<AccountingStorageOptions>(builder.Configuration.GetSection(AccountingStorageOptions.SectionName));
    builder.Services.AddScoped<FileSystemInvoiceBlobStorage>();
    builder.Services.AddScoped<FtpInvoiceBlobStorage>();
    builder.Services.AddScoped<IInvoiceBlobStorage>(sp =>
    {
        var options = sp.GetRequiredService<IOptions<AccountingStorageOptions>>().Value;
        var provider = options.StorageProvider?.Trim();
        if (string.Equals(provider, "Ftp", StringComparison.OrdinalIgnoreCase))
            return sp.GetRequiredService<FtpInvoiceBlobStorage>();

        return sp.GetRequiredService<FileSystemInvoiceBlobStorage>();
    });

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

    // Invoice PDF Parser (e-fatura / e-arşiv PDF'inden veri çıkarma)
    builder.Services.AddScoped<InvoicePdfParserService>();

    // Excel Generation Service
    builder.Services.AddScoped<ExcelService>();

    // Google Shopping Service
    builder.Services.AddScoped<IGoogleShoppingService, GoogleShoppingService>();

    // Yandex Shopping (YML feed): TCMB + CBR kurlarından TRY→RUB
    builder.Services.AddScoped<IYandexExchangeRateService, YandexExchangeRateService>();
    builder.Services.AddScoped<IYandexShoppingService, YandexShoppingService>();

    // Google Analytics Service (anlık raporlar, veritabanına kaydetmez, memory cache)
    builder.Services.AddScoped<IGoogleAnalyticsService, BalonPark.Services.GoogleAnalytics.GoogleAnalyticsService>();

    // Blog Service
    builder.Services.AddScoped<IBlogService, BlogService>();

    // AI Service
    builder.Services.AddHttpClient<AiService>();
    builder.Services.AddScoped<IAiService, AiService>();

    // Gemini Imagen (ürün görseli üretimi)
    builder.Services.AddHttpClient<GeminiImageService>();
    builder.Services.AddScoped<IGeminiImageService, GeminiImageService>();

    var app = builder.Build();

    Log.Information("Ortam: {Environment}. Veritabanı migration'ları çalıştırılıyor…", app.Environment.EnvironmentName);

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

    Log.Information("HTTP pipeline kuruluyor; Kestrel dinlemeye geçecek.");

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

    // Yerelde yalnızca http:// dinleniyorsa UseHttpsRedirection "https port" uyarısı üretir; prod'da açık kalsın.
    if (!app.Environment.IsDevelopment())
        app.UseHttpsRedirection();

    // Canonical domain redirect: www.balonpark.com -> balonpark.com (301)
    // Google Merchant Center domain tutarliligi icin kritik
    app.UseCanonicalDomain();

    // Static files: cache + security headers (Lighthouse cache-insight iyileştirmesi)
    var staticFileOptions = new StaticFileOptions
    {
        OnPrepareResponse = ctx =>
        {
            var path = ctx.Context.Request.Path.Value ?? "";
            // CSS, JS, images, fonts: 1 yıl (dosya adında versiyon/hash ile cache busting yapın)
            if (path.StartsWith("/css/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/js/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/assets/", StringComparison.OrdinalIgnoreCase))
            {
                ctx.Context.Response.Headers.CacheControl = "public,max-age=31536000,immutable";
            }
            // uploads (user content): kısa cache
            else if (path.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            {
                ctx.Context.Response.Headers.CacheControl = "public,max-age=86400"; // 1 gün
            }
            // Security headers for static files
            ctx.Context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        }
    };
    app.UseStaticFiles(staticFileOptions);

    app.UseRequestLocalization();
    app.UseRouting();
    app.UseSecurityHeaders();

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
    Log.Information("Kestrel başlatılıyor. Adres: {Url}", openUrl);

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