using DRY.MailJetClient.Library.Extensions;
using Mongo.Common.MongoDB;
using ProfessionalProfiles.Configurations;
using ProfessionalProfiles.Extensions;
using ProfessionalProfiles.Graph;

var builder = WebApplication.CreateBuilder(args);
AppConfigurations.ConfigureLogging("http://localhost:9200");
// Configure Mongo DB Settings
// Get connection string from the env secrets
var config = builder.Configuration;
builder.Services.ConfigureMongoSettings(config["MongoConnection:ConnString"]!, config["MongoConnection:Database"]!);
builder.Services.ConfigureMongoIdentity(config["MongoConnection:ConnString"]!, config["MongoConnection:Database"]!);
builder.Services.ConfigureMailJet(config["MailJet:ApiKey"]!, config["MailJet:ApiSecret"]!, config["MailJet:Email"]!, "Professional Profiles");
builder.Services.ConfigureCors();
builder.Services.ConfigureDataAndServices();
builder.Services.ConfigureJWT(builder.Configuration);
builder.Services.AddGraphQLServer()
    .AddAuthorization()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddType<UploadType>()
    .AddHttpRequestInterceptor<CustomeRequestInterceptor>()
    .AddMutationConventions();

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<Program>>();
app.ConfigureExceptionHandler(logger);
await app.SeedSystemData(logger);
app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.UseWebSockets();
app.MapGraphQL("/profilesql");
//app.MapGet("/", () => "Welcome!");
app.MapGet("/", async context =>
{
    context.Response.ContentType = "text/html";
    await context.Response.SendFileAsync("wwwroot/ProfilesQL.html");
});

app.Run();