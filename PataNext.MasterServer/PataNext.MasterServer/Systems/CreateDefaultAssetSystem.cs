using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameHost.Core.Ecs;
using GameHost.Core.IO;
using Newtonsoft.Json;
using PataNext.MasterServer.Components.Asset;
using project.Core;
using project.Core.Components;
using project.Core.Entities;
using project.DataBase;
using project.DataBase.Implementations;
using RethinkDb.Driver;

namespace PataNext.MasterServer.Systems
{
	[RestrictToApplication(typeof(MasterServerApplication))]
	public class CreateDefaultAssetSystem : AppSystem
	{
		private IEntityDatabase db;
		private IStorage        storage;

		public CreateDefaultAssetSystem(WorldCollection collection) : base(collection)
		{
			DependencyResolver.Add(() => ref db);
			DependencyResolver.Add(() => ref storage);
		}

		protected override async void OnDependenciesResolved(IEnumerable<object> dependencies)
		{
			base.OnDependenciesResolved(dependencies);

			var assetStorage = await storage.GetOrCreateDirectoryAsync("assets");
			var kitStorage   = await assetStorage.GetOrCreateDirectoryAsync("kit");

			if (db is RethinkDbDatabaseImpl impl)
			{
				var table = await impl.GetEntityTable<AssetEntity>();
				await table.Filter(x => x.HasFields(nameof(CoreAsset))).Delete().RunAsync(impl.Connection);

				Task.WaitAll((await kitStorage.GetFilesAsync("*.json"))
				             .Select(file => file.GetContentAsync()
				                                 .ContinueWith(new Action<Task<byte[]>>(async task =>
				                                 {
					                                 var data = JsonConvert.DeserializeObject<Kit>(Encoding.UTF8.GetString(task.Result));

					                                 var assetEntity = impl.CreateEntity<AssetEntity>();
					                                 await assetEntity.ReplaceAsync(new CoreAsset());
					                                 await assetEntity.ReplaceAsync(new UnitKitAsset(data.Id, data.Name));
					                                 await assetEntity.ReplaceAsync(new AssetPointer("st", "pn", data.Id));
					                                 await assetEntity.ReplaceAsync(new AssetName(data.Name));
					                                 await assetEntity.ReplaceAsync(new AssetDescription(data.Description));
					                                 await assetEntity.ReplaceAsync(new AssetType("kit"));
				                                 })))
				             .ToArray());
			}
			else
				throw new InvalidOperationException("Find a way to implement the same thing in other databases.");
		}

		public struct Kit
		{
			[JsonProperty("id")]
			public string Id;
			
			[JsonProperty("name")]
			public string Name;
			
			[JsonProperty("description")]
			public string Description;
		}
	}
}