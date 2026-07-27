var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql")
    .AddDatabase("dairydna");

var api = builder.AddProject<Projects.DairyDNA_Api>("api")
    .WithReference(sql)
    .WaitFor(sql)
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.DairyDNA_Web>("web")
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();
