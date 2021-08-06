using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using GameHost.Core.Ecs;
using Grpc.Core;
using Grpc.Net.Client;
using MagicOnion.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using project.Core;
using project.DataBase;

namespace PataNext.MasterServer.Systems
{
	[RestrictToApplication(typeof(MasterServerApplication))]
	public class StartMasterServerGrpcSystem : AppSystem
	{
		private IHost host;

		private IEntityDatabase         entityDatabase;
		private CancellationTokenSource ccs;
		
		public StartMasterServerGrpcSystem(WorldCollection collection) : base(collection)
		{
			AddDisposable(ccs = new CancellationTokenSource());
			
			DependencyResolver.Add(() => ref entityDatabase);
		}

		class Startup
		{
			public Startup(IConfiguration configuration)
			{
				Configuration = configuration;
			}

			public IConfiguration Configuration { get; }

			// This method gets called by the runtime. Use this method to add services to the container.
			public void ConfigureServices(IServiceCollection services)
			{
				services.AddControllersWithViews();

				services.AddGrpc(); // MagicOnion depends on ASP.NET Core gRPC service.
				services.AddMagicOnion(opt =>
				{
					opt.IsReturnExceptionStackTraceInErrorDetail = true;
				});
			}

			// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
			public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
			{
				if (env.IsDevelopment())
				{
					app.UseDeveloperExceptionPage();
				}
				
				app.UseRouting();

				app.UseEndpoints(endpoints =>
				{
					endpoints.MapMagicOnionHttpGateway("_", app.ApplicationServices.GetService<MagicOnionServiceDefinition>().MethodHandlers, GrpcChannel.ForAddress("http://0.0.0.0:5001", new GrpcChannelOptions
					{
						HttpClient = new HttpClient(new HttpClientHandler
						{
							ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
						})
					}));
					endpoints.MapMagicOnionSwagger("swagger", app.ApplicationServices.GetService<MagicOnionServiceDefinition>().MethodHandlers, "/_/");

					endpoints.MapMagicOnionService();
					endpoints.MapGet("/", async context =>
					{
						await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
					});
				});
			}
		}

		protected override void OnDependenciesResolved(IEnumerable<object> dependencies)
		{
			base.OnDependenciesResolved(dependencies);
			
			GrpcEnvironment.SetLogger(new Grpc.Core.Logging.ConsoleLogger());

			var hostBuilder = Host.CreateDefaultBuilder()
			                      .ConfigureServices(services =>
			                      {
				                      services.AddSingleton(World);
				                      services.AddSingleton(entityDatabase);
			                      })
			                      .ConfigureWebHostDefaults(webBuilder =>
			                      {
				                      webBuilder.UseKestrel(kestrel =>
				                      {
					                      kestrel.ConfigureEndpointDefaults(endpointOptions =>
					                      {
						                      endpointOptions.Protocols = HttpProtocols.Http2;
					                      });
				                      });
				                      
				                      webBuilder.UseStartup<Startup>();
				                      webBuilder.UseUrls("http://0.0.0.0:5000/");
			                      });

			(host = hostBuilder.Build()).RunAsync(ccs.Token);
			
			/*ApplicationBuilder app = null;
			AddDisposable(host = hostBuilder.ConfigureServices((hostContext, services) =>
			{
				services.AddGrpc();
				services.AddMagicOnion();
				services.AddRouting();

				app = new ApplicationBuilder(services.BuildServiceProvider());
				
				if (hostContext.HostingEnvironment.IsDevelopment())
				{
					app.UseDeveloperExceptionPage();
				}

				app.UseRouting();
				app.UseEndpoints(endpoints =>
				{
					endpoints.MapMagicOnionHttpGateway("_", app.ApplicationServices.GetService<MagicOnion.Server.MagicOnionServiceDefinition>().MethodHandlers, GrpcChannel.ForAddress("https://localhost:5001"));
					endpoints.MapMagicOnionSwagger("swagger", app.ApplicationServices.GetService<MagicOnion.Server.MagicOnionServiceDefinition>().MethodHandlers, "/_/");

					endpoints.MapMagicOnionService();
				});
			}).Start());
			
			app.Build().Invoke(new DefaultHttpContext())
			   .ContinueWith(t =>
			   {
				   Console.WriteLine($"{t.Exception}");
			   });*/
			
			Console.WriteLine("GRPC - Server Started!");
		}
	}
}