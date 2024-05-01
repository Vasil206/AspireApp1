var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.WorkerService1>("workerservice1");

builder.Build().Run();
