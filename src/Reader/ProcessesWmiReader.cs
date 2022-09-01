using System.Management;
using System.Runtime.Versioning;
using Newtonsoft.Json;

namespace WinHwMetrics
{
    [SupportedOSPlatform("windows")]
    class ProcessesWmiReader
    {
        public record ProcessRecord(
            string Name,
            [property:JsonProperty("CPU%")]
            ulong CpuUtilization,
            [property:JsonProperty("GPU%")]
            ulong GpuUtilization,
            [property:JsonProperty("MEM%")]
            double MemUtilization
        );

        private int ProcessorCount = Environment.ProcessorCount;
        private double TotalPhysicalMemory;
        private ManagementObjectSearcher cpuSearcher;
        private ManagementObjectSearcher gpuSearcher;

        public ProcessesWmiReader()
        {
            ManagementScope scope = new ManagementScope($@"\\localhost\ROOT\CIMV2");
            scope.Connect();

            this.cpuSearcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT * FROM Win32_PerfFormattedData_PerfProc_Process"));
            this.gpuSearcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT * FROM Win32_PerfFormattedData_GPUPerformanceCounters_GPUEngine WHERE Name LIKE '%_engtype_3D'"));
            this.TotalPhysicalMemory = BToGB(
                new ManagementObjectSearcher(scope, new ObjectQuery("SELECT Capacity FROM Win32_PhysicalMemory"))
                .Get()
                .Cast<ManagementObject>()
                .Aggregate(0UL, (a, mo) => a + (ulong)mo["Capacity"])
            );
        }

        public List<ProcessRecord> ReadData()
        {
            Dictionary<string, ulong> processIdToGpu = this
                .gpuSearcher
                .Get()
                .Cast<ManagementObject>()
                .Select(mo =>
                    new
                    {
                        Id = ((string)mo["Name"]).Split("_")[1],
                        UtilizationPercentage = (ulong)mo["UtilizationPercentage"]
                    }
                )
                .GroupBy(e => e.Id)
                .ToDictionary(
                    e => e.Key,
                    e => e.ToList().Aggregate(0UL, (a, el) => a + el.UtilizationPercentage)
                );

            return this.cpuSearcher
                .Get()
                .Cast<ManagementObject>()
                .GroupBy(mo => ((string)mo["Name"]).Split("#")[0])
                .Where(e => !new List<string> { "_Total", "Idle" }.Contains(e.Key))
                .Select(e => e.Aggregate(
                        new ProcessRecord
                        (
                            Name: e.Key,
                            CpuUtilization: 0UL,
                            GpuUtilization: 0UL,
                            MemUtilization: 0UL
                        ),
                        (a, mo) =>
                        {
                            processIdToGpu.TryGetValue(mo["IDProcess"].ToString()!, out ulong gpuUtilization);

                            return new ProcessRecord(
                                Name: e.Key,
                                CpuUtilization: a.CpuUtilization + ((ulong)mo["PercentProcessorTime"]) / (ulong)ProcessorCount,
                                GpuUtilization: a.GpuUtilization + gpuUtilization,
                                MemUtilization: Math.Round(a.MemUtilization + (BToGB((ulong)mo["WorkingSetPrivate"]) * 100) / TotalPhysicalMemory)
                            );
                        }
                    )
                )
                .OrderByDescending(p => p.CpuUtilization)
                .ThenByDescending(p => p.GpuUtilization)
                .ThenByDescending(p => p.MemUtilization)
                .Take(3)
                .ToList();
        }

        private double BToGB(ulong value)
        {
            return value / (1024D * 1024D * 1024D);
        }
    }
}
