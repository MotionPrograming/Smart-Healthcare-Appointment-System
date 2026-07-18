Environment.SetEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT", "true");
Environment.SetEnvironmentVariable("DOTNET_NEVER_RESTRICT_HTTPS_TO_LOCALHOST", "true");

var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL
var postgres = builder.AddPostgres("postgres")
    .AddDatabase("shasdb");


// RabbitMQ
var rabbitmq = builder.AddRabbitMQ("rabbitmq");


// Services

var identity = builder.AddProject<Projects.Identity_API>("identity-api")
    .WithReference(postgres)
    .WithReference(rabbitmq);


var doctor = builder.AddProject<Projects.Doctor_API>("doctor-api")
    .WithReference(postgres)
    .WithReference(rabbitmq);


var appointment = builder.AddProject<Projects.Appointment_API>("appointment-api")
    .WithReference(postgres)
    .WithReference(rabbitmq);


var notification = builder.AddProject<Projects.Notification_API>("notification-api")
    .WithReference(rabbitmq);


var gateway = builder.AddProject<Projects.SHAS_Gateway>("gateway");


builder.Build().Run();