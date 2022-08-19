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
            var app = WebApplication.CreateBuilder(args).Build();
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
                        .GroupBy(x => string.Join("", x.Reverse().TakeWhile(c => char.IsDigit(c)).Reverse()))
                        .ToDictionary(x => x.Key, x => x.ToList().Select(l => new { Label = l, Value = RegistryKey.GetValue(l) }));
        }

    }
}
