using System.Runtime.Versioning;

namespace restfulhwinfo
{
    [SupportedOSPlatform("windows")]
    class Program
    {
        public static IConfigurationRoot Configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();
        private static string Url = Configuration.GetSection("ServerUrl").Value;
        private static HwInfoSensorsReader HwInfoSensorsReader = new HwInfoSensorsReader();
        private static ProcessesWmiReader ProcessesWmiReader = new ProcessesWmiReader();

        static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddCors(p => p.AddPolicy("corsapp", b => { b.WithOrigins("*").AllowAnyMethod().AllowAnyHeader(); }));

            var app = builder.Build();
            app.UseCors("corsapp");
            app.MapGet(
                "/",
                () => Results.Json(
                    new
                    {
                        sensors = HwInfoSensorsReader.ReadData(),
                        processes = ProcessesWmiReader.ReadData()
                    }
                )
            );

            app.Run(Url);
        }
    }
}
