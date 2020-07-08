using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Core.Logging;
using P4TLB.MasterServer;
using P4TLBMasterServer;
using P4TLBMasterServer.Discord;
using P4TLBMasterServer.DiscordBot;
using project.Messages;
using project.P4Classes;
using StackExchange.Redis;

namespace project
{
	public struct OnProgramInitialized {}
	
	
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
			
			GrpcEnvironment.SetLogger(new ConsoleLogger());
			
			// Search available gRpc service through reflection
			var implementations = SearchServiceImplementations(mapInstanceImpl);

			// Create server...
			var server = new Server
			{
				Ports = {new ServerPort(IPAddress.Any.ToString(), port, ServerCredentials.Insecure)}
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
			world.GetOrCreateManager<P4CreateFormationOnceManager>();
			world.GetOrCreateManager<P4CreateUnitInFormationOnce>();
			world.GetOrCreateManager<UnitKitComponentManager>();
			world.GetOrCreateManager<RelayStandardEventToClient>();
			
			// Create forgotten managers...
			foreach (var type in SearchManagers())
			{
				System.Console.WriteLine("creating " + type.FullName);
				world.GetOrCreateManager(type);
			}

			// Connect to Redis
			var dbMgr = world.GetOrCreateManager<DatabaseManager>();
			{
				var conf = ConfigurationOptions.Parse("localhost");
				dbMgr.SetConnection(ConnectionMultiplexer.Connect(conf));
			}
			
			// inject world to implementations...
			foreach (var (type, implementation) in mapInstanceImpl)
			{
				foreach (var property in type.GetRuntimeProperties())
				{
					if (property.PropertyType == typeof(World))
						property.SetValue(implementation, world);
					if (property.PropertyType.IsSubclassOf(typeof(ManagerBase)))
						property.SetValue(implementation, world.GetOrCreateManager(property.PropertyType));
				}
			}

			// Start the server
			server.Start();

			var userMgr = world.GetOrCreateManager<UserDatabaseManager>();
			for (var i = 0; i != 2; i++)
			{
				var getIdTask = userMgr.GetIdFromLogin("server_" + i);
				getIdTask.Wait();
				
				if (getIdTask.Result > 0)
					continue;
				var account = userMgr.CreateAccount("server_" + i, out var success);
			}
			
			world.Notify(null, string.Empty, new OnProgramInitialized {});

			Console.WriteLine("The server is currently listening...\nPress a key to exit.");
			while (true)
			{
				//System.Console.WriteLine(Console.In.Peek());
				/*if (Console.In.Peek() == 0)
					break;*/

				// Update the world
				world.Update();
			}
		}

		private static List<ServerServiceDefinition> SearchServiceImplementations(Dictionary<Type, object> mapInstanceImpl)
		{
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

		private static List<Type> SearchManagers()
		{
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			return
			(
				from types
					in assemblies.Select(a => a.GetTypes())
				from type
					in types.Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(ManagerBase)))
				select type
			).ToList();
		}
	}
}