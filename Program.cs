using Microsoft.Win32;
using System.Runtime.Versioning;
using System.Diagnostics;

namespace restfulhwinfo
{
    [SupportedOSPlatform("windows")]
    class Program
    {
        static IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();
        static string Url = configuration.GetSection("ServerUrl").Value;
        static string KeyName = configuration.GetSection("HwInfoRegistryKeyName").Value;
        static RegistryKey RegistryKey = Registry.CurrentUser.OpenSubKey(KeyName) ?? throw new InvalidOperationException("Registry key not found!");

        static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddCors(p => p.AddPolicy("corsapp", builder =>
            {
                builder.WithOrigins("*").AllowAnyMethod().AllowAnyHeader();
            }));
            var app = builder.Build();

            app.UseCors("corsapp");
            app.MapGet(
                "/",
                () => Results.Json(GetHwInfoSensorData())
            );

            app.Run(Url);
        }

        private static object GetHwInfoSensorData()
        {
            return RegistryKey
                        .GetValueNames()
                        .ToList()
                        .GroupBy(e => string.Join("", e.Reverse().TakeWhile(c => char.IsDigit(c)).Reverse()))
                        .ToDictionary(e => e.Key, e => e.ToList().ToDictionary(label => label, label => RegistryKey.GetValue(label)));
        }
    }
}
