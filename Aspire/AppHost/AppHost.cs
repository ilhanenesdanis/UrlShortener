var builder = DistributedApplication.CreateBuilder(args);

var redisPassword = builder.AddParameter("redis-password", secret: true);
var rabbitUser = builder.AddParameter("rabbitmq-username");
var rabbitPassword = builder.AddParameter("rabbitmq-password", secret: true);
var mongoUser = builder.AddParameter("mongo-username");
var mongoPassword = builder.AddParameter("mongo-password", secret: true);
var postgresUser = builder.AddParameter("postgres-username");
var postgresPassword = builder.AddParameter("postgres-password", secret: true);

//PostgreSQL
var postgres = builder.AddPostgres("postgres", postgresUser, postgresPassword)
                      .WithDataVolume();

var shortenerDb = postgres.AddDatabase("shortenerdb");

//Redis
var cache = builder.AddRedis("Redis", password: redisPassword)
                   .WithDataVolume()
                   .WithRedisInsight(redisInsight => redisInsight.WithHostPort(8001));

//RabbitMQ
var rabbitmq = builder.AddRabbitMQ("messaging", rabbitUser, rabbitPassword)
                      .WithDataVolume()
                      .WithManagementPlugin(port: 15672);

//MongoDB
var mongo = builder.AddMongoDB("mongo", userName: mongoUser, password: mongoPassword)
                   .WithDataVolume();

var analyticsDb = mongo.AddDatabase("analyticsdb");


builder.AddProject<Projects.Shortener_API>("API")
       .WithReference(shortenerDb)
       .WithReference(cache)
       .WithReference(rabbitmq)
       .WaitFor(shortenerDb)
       .WaitFor(cache)
       .WaitFor(rabbitmq);

builder.AddProject<Projects.Shortener_Analytics>("Analytics")
       .WithReference(rabbitmq)
       .WithReference(analyticsDb)
       .WaitFor(rabbitmq)
       .WaitFor(analyticsDb);


builder.Build().Run();
