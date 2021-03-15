

using System;
using System.Reflection;
using Elastic.Apm.SerilogEnricher;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Elasticsearch;
using Serilog.Formatting.Json;
using Serilog.Sinks.Elasticsearch;
using Serilog.Sinks.File;

namespace WebApplication3
{
	public class Program
	{
		public static void Main(string[] args)
		{
			//var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
			//var configuration = new ConfigurationBuilder()
			//	.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
			//	.AddJsonFile(
			//		$"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json",
			//		optional: true)
			//	.Build();

			var qwe = $"{Assembly.GetExecutingAssembly().GetName().Name.ToLower()}-{DateTime.UtcNow:yyyy-MM}";
			Log.Logger = new LoggerConfiguration()
				.Enrich.FromLogContext()
				.Enrich.WithElasticApmCorrelationInfo()
				.MinimumLevel.Override("Microsoft", LogEventLevel.Information)
				.WriteTo.Console(new ElasticsearchJsonFormatter())
				.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://localhost:9200"))
				{
					AutoRegisterTemplate = true,
					IndexFormat = $"syslog-{DateTime.UtcNow:yyyy-MM}",
					AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv6,
					ModifyConnectionSettings = x => x.BasicAuthentication("vova", "falkon"),
					CustomFormatter = new ElasticsearchJsonFormatter(),
					FailureCallback = e => Console.WriteLine("Unable to submit event " + e.MessageTemplate),
					EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog |
					                   EmitEventFailureHandling.WriteToFailureSink |
					                   EmitEventFailureHandling.RaiseCallback,
					FailureSink = new FileSink("./failures.txt", new JsonFormatter(), null)
				})
				//.Enrich.WithProperty("Environment", environment)
				//.ReadFrom.Configuration(configuration)
				.CreateLogger();
			try
			{
				Log.Information("Starting up");
				CreateHostBuilder(args).Build().Run();
			}
			catch (Exception ex)
			{
				Log.Fatal(ex, "Application start-up failed");
			}
			finally
			{
				Log.CloseAndFlush();
			}
		}

		public static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
				.UseSerilog() // <- Add this line
				.ConfigureWebHostDefaults(webBuilder =>
				{
					webBuilder.UseStartup<Startup>();
				});
	}
}
