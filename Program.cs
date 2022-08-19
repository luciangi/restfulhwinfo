using Microsoft.Win32;
using System.Runtime.Versioning;

namespace restfulhwinfo
{
    [SupportedOSPlatform("windows")]
    class Program
    {
        const string Url = "http://localhost:60000";
        const string KeyName = "SOFTWARE\\HWiNFO64\\VSB";
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
                () => Results.Json(GetValueNames())
            );

            app.Run(Url);
        }

        private static object GetValueNames()
        {
            return RegistryKey
                        .GetValueNames()
                        .ToList()
                        .GroupBy(e => string.Join("", e.Reverse().TakeWhile(c => char.IsDigit(c)).Reverse()))
                        .ToDictionary(e => e.Key, e => e.ToList().ToDictionary(label => label, label => RegistryKey.GetValue(label)));
        }
    }
}
