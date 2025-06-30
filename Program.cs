using Microsoft.EntityFrameworkCore;
using System;
using WebApp2.Data;
using WebApp2.Services;

namespace WebApp2;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var connectionStringREPDB = builder.Configuration.GetConnectionString("OlcDB");

        builder.Services.AddDbContextFactory<ApplicationDbContext>(options => options.UseOracle(connectionStringREPDB));
        builder.Services.AddSingleton<IAppRepository, AppRepository>();
        builder.Services.AddSingleton<ILuceneHProviderService>( s => {
            var path = GetIndexPath("", Environment.CurrentDirectory);
            EnsureIndexDirectoryExists(path);
            return new LuceneHProviderService(path); }
        );
        builder.Services.AddMemoryCache();

        // Add services to the container.
        builder.Services.AddControllersWithViews();
        builder.Services.AddHttpClient();
        builder.Services.AddHostedService<CacheBootstrapService>();
        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.Run();
    }

    private static string GetIndexPath(string? configuredPath, string contentRootPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return Path.GetFullPath(configuredPath);
        }

        // Default path in the application directory
        return Path.Combine(contentRootPath, "App_Data", "LuceneIndex", "Healthcare");
    }

    private static void EnsureIndexDirectoryExists(string indexPath)
    {
        try
        {
            Directory.CreateDirectory(indexPath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to create Lucene index directory at {indexPath}. " +
                $"Please ensure the application has write permissions to this location.", ex);
        }
    }
}