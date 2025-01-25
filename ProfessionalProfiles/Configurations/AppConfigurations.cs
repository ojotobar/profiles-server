using Serilog;
using Serilog.Exceptions;
using Serilog.Sinks.Elasticsearch;
using System.Reflection;

namespace ProfessionalProfiles.Configurations
{
    public class AppConfigurations
    {
        public static void ConfigureLogging(string elasticsearchUri)
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? string.Empty;
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env}.json", optional: true)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.WithExceptionDetails()
                .WriteTo.Debug()
                .WriteTo.Console()
                .WriteTo.Elasticsearch(ConfigureElasticSink(elasticsearchUri, env))
                .Enrich.WithProperty("Environment", env)
                .ReadFrom.Configuration(config)
                .CreateLogger();
        }

        public static ElasticsearchSinkOptions ConfigureElasticSink(string elasticsearchUri, string environment)
        {
            var conf = new ElasticsearchSinkOptions(new Uri(elasticsearchUri))
            {
                AutoRegisterTemplate = true,
                IndexFormat = $"{Assembly.GetExecutingAssembly().GetName()?.Name?.ToLower().Replace(".", "-")}-{environment.ToLower()}-{DateTime.UtcNow:yyyy-MM}",
                NumberOfReplicas = 1,
                NumberOfShards = 2,
            };
            return conf;
        }
    }
}
