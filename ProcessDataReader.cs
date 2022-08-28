using System.Management;
using System.Runtime.Versioning;

namespace restfulhwinfo
{
    [SupportedOSPlatform("windows")]
    class ProcessesWmiReader
    {
        public record ProcessRecord(
            uint Id,
            string Name,
            ulong CpuUtilization,
            ulong GpuUtilization,
            float MemoryUtilization
        );
        private int ProcessorCount = Environment.ProcessorCount;
        private float TotalPhysicalMemory;
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
                .Select(mo =>
                {
                    uint processId = (uint)mo["IDProcess"];
                    processIdToGpu.TryGetValue(processId.ToString(), out ulong gpuUtilization);

                    return new ProcessRecord(
                        Id: (uint)mo["IDProcess"],
                        Name: (string)mo["Name"],
                        CpuUtilization: ((ulong)mo["PercentProcessorTime"]) / (ulong)ProcessorCount,
                        GpuUtilization: gpuUtilization,
                        MemoryUtilization: (BToGB((ulong)mo["WorkingSetPrivate"]) * 100) / TotalPhysicalMemory
                    );
                })
                .OrderByDescending(p => p.CpuUtilization)
                .Take(10)
                .ToList();
        }

        private float BToGB(ulong value)
        {
            return value / (1024 * 1024 * 1024);
        }
    }
}
