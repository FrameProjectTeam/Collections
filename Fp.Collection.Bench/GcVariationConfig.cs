using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace Fp.Collection.Bench
{
	class GcVariationConfig : ManualConfig
	{
		public GcVariationConfig()
		{
			AddJob(Job.MediumRun.WithGcServer(true).WithGcForce(true).WithId("ServerForce"));
			AddJob(Job.MediumRun.WithGcServer(true).WithGcForce(false).WithId("Server"));
			AddJob(Job.MediumRun.WithGcServer(false).WithGcForce(true).WithId("Workstation"));
			AddJob(Job.MediumRun.WithGcServer(false).WithGcForce(false).WithId("WorkstationForce"));
		}
	}
}