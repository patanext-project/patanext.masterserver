using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using Grpc.Core;
using Grpc.Core.Interceptors;
using P4TLB.MasterServer;
using P4TLBMasterServer;
using P4TLBMasterServer.Discord;
using P4TLBMasterServer.DiscordBot;
using project.Messages;
using StackExchange.Redis;

namespace project
{
	class Program
	{
		static void Main(string[] args)
		{
			// host port of the masterserver.
			var port = 4242;

			// Create world with implementation and managers
			// 'mapInstanceImpl' is dictionary with the implementation (grpc services)
			var mapInstanceImpl = new Dictionary<Type, object>();
			var world = new World(mapInstanceImpl);
			
			// Search available gRpc service through reflection
			var implementations = SearchServiceImplementations(mapInstanceImpl);

			// Create server...
			var server = new Server
			{
				Ports = {new ServerPort("localhost", port, ServerCredentials.Insecure)}
			};

			// Add implementations...
			foreach (var impl in implementations)
			{
				server.Services.Add(impl);
			}

			// Create some managers
			world.GetOrCreateManager<ClientManager>();
			world.GetOrCreateManager<DiscordBotManager>();
			world.GetOrCreateManager<DiscordLobby>();
			world.GetOrCreateManager<DiscordLoginRoute>();

			// Connect to Redis
			var dbMgr = world.GetOrCreateManager<DatabaseManager>();
			{
				var conf = ConfigurationOptions.Parse("localhost");
				dbMgr.SetConnection(ConnectionMultiplexer.Connect(conf));
			}
			
			// World variables are not set automatically in services, so set it
			// todo: it should be set automatically in future
			world.GetImplInstance<AuthenticationServiceImpl>().World = world;

			// Start the server
			server.Start();

			var userMgr = world.GetOrCreateManager<UserDatabaseManager>();
			for (var i = 0; i != 2; i++)
			{
				var id = userMgr.GetIdFromLogin("server_" + i);
				if (id > 0)
					continue;
				var account = userMgr.CreateAccount("server_" + i, out var success);
			}

			Console.WriteLine("The server is currently listening...\nPress a key to exit.");
			while (true)
			{
				if (Console.KeyAvailable)
					break;

				// Update the world
				world.Update();
			}
		}

		private static IEnumerable<ServerServiceDefinition> SearchServiceImplementations(Dictionary<Type, object> mapInstanceImpl)
		{
			//var result = new List<(Type binder, Type impl)>();
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();

			// May be complicated:
			// We search for all classes that have a 'ImplementationAttribute' on them (to get the binder type)
			// Then we get the 'BindService' method from the binder (it's a static method)
			// Then we create an instance of the service implementation
			// Then we call BindService with the new instance
			// Then we add the service to the list.
			return
			(
				from types
					in assemblies.Select(a => a.DefinedTypes)
				from type
					in types
				let attr = type.GetCustomAttribute<ImplementationAttribute>()
				where attr != null
				let binder = attr.Binder
				let bindMethod = binder.GetMethods().First(m => m.Name == "BindService" && m.GetParameters().Length == 1)
				let instance = mapInstanceImpl[type] = Activator.CreateInstance(type)
				select (ServerServiceDefinition) bindMethod.Invoke(null, new[] {instance})
			).ToList();
		}
	}
}