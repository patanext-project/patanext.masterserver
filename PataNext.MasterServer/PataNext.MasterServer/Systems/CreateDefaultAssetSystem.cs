using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameHost.Core;
using GameHost.Core.Ecs;
using GameHost.Core.IO;
using GameHost.Native.Char;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PataNext.MasterServer.Components.Asset;
using PataNext.MasterServer.Utils;
using project.Core;
using project.Core.Components;
using project.Core.Entities;
using project.DataBase;
using project.DataBase.Implementations;
using RethinkDb.Driver;
using ZLogger;

namespace PataNext.MasterServer.Systems
{
	[RestrictToApplication(typeof(MasterServerApplication))]
	public class CreateDefaultAssetSystem : AppSystem
	{
		private IEntityDatabase db;
		private IStorage        storage;

		private ILogger logger;

		public TaskCompletionSource<bool> CompletionTaskSource { get; }

		public CreateDefaultAssetSystem(WorldCollection collection) : base(collection)
		{
			CompletionTaskSource = new TaskCompletionSource<bool>();

			DependencyResolver.Add(() => ref db);
			DependencyResolver.Add(() => ref storage);
			DependencyResolver.Add(() => ref logger);
		}

		protected override async void OnDependenciesResolved(IEnumerable<object> dependencies)
		{
			base.OnDependenciesResolved(dependencies);

			var assetStorage = new RecursiveLocalStorage(await storage.GetOrCreateDirectoryAsync("assets"));
			// ^ this will allow us to research in sub directories...

			var itemTypeStorage      = await assetStorage.GetOrCreateDirectoryAsync("item_type");
			var equipmentRootStorage = await assetStorage.GetOrCreateDirectoryAsync("equip_root");
			var songItemStorage     = await assetStorage.GetOrCreateDirectoryAsync("song");
			var roleStorage          = await assetStorage.GetOrCreateDirectoryAsync("role");
			var kitStorage           = await assetStorage.GetOrCreateDirectoryAsync("kit");
			var archetypeStorage     = await assetStorage.GetOrCreateDirectoryAsync("archetype");
			var hierarchyStorage     = await assetStorage.GetOrCreateDirectoryAsync("hierarchy");
			var equipmentStorage     = await assetStorage.GetOrCreateDirectoryAsync("equipment");
			var abilityStorage       = await assetStorage.GetOrCreateDirectoryAsync("ability");

			Console.WriteLine("Recreating assets...");
			if (db is RethinkDbDatabaseImpl impl)
			{
				var table = await impl.GetEntityTable<AssetEntity>();
				await table.Filter(x => x.HasFields(nameof(CoreAsset))).Delete().RunAsync(impl.Connection);

				async Task<DbEntityKey<AssetEntity>> createAsset(string id, string name, string description, string group)
				{
					var groupHash = CharBufferUtility.ComputeHashCode(group);
					var idHash    = CharBufferUtility.ComputeHashCode(id);
					
					var guid = new Guid(groupHash, (short) idHash, (short) (idHash - short.MaxValue), (byte) id[0], 0, 0, 0, 0, 0, 0, 0);

					var assetEntity = impl.CreateEntity<AssetEntity>(guid.ToString());
					await assetEntity.ReplaceAsync(new CoreAsset());
					await assetEntity.ReplaceAsync(new AssetPointer("st", "pn", $"{group}/{id}"));
					await assetEntity.ReplaceAsync(new AssetName(name));
					await assetEntity.ReplaceAsync(new AssetDescription(description));
					await assetEntity.ReplaceAsync(new AssetType(group));

					return assetEntity;
				}

				var itemTypeMap  = new Dictionary<string, DbEntityRepresentation<AssetEntity>>(128);
				var equipRootMap = new Dictionary<string, DbEntityRepresentation<AssetEntity>>(16);
				var songItemMap = new Dictionary<string, DbEntityRepresentation<AssetEntity>>(16);
				var abilityMap = new Dictionary<string, DbEntityRepresentation<AssetEntity>>(256)
				{
					[string.Empty] = default
				};
				var roleMap      = new Dictionary<string, DbEntityRepresentation<AssetEntity>>(128);

				Task.WaitAll((await itemTypeStorage.GetFilesAsync("*.json"))
				             .Select(async file =>
				             {
					             var data        = JsonConvert.DeserializeObject<ItemType>(Encoding.UTF8.GetString(await file.GetContentAsync()));
					             var assetEntity = await createAsset(data.Id, data.Name, data.Description, "item_type");

					             lock (itemTypeMap)
					             {
						             itemTypeMap[$"st.pn/item_type/{data.Id}"] = assetEntity;
						             itemTypeMap[$"/item_type/{data.Id}"]      = assetEntity;
					             }
				             })
				             .ToArray());

				Task.WaitAll((await equipmentRootStorage.GetFilesAsync("*.json"))
				             .Select(async file =>
				             {
					             var data        = JsonConvert.DeserializeObject<EquipmentRoot>(Encoding.UTF8.GetString(await file.GetContentAsync()));
					             var assetEntity = await createAsset(data.Id, data.Name, data.Description, "equip_root");

					             lock (equipRootMap)
					             {
						             equipRootMap[$"st.pn/equip_root/{data.Id}"] = assetEntity;
						             equipRootMap[$"/equip_root/{data.Id}"]      = assetEntity;
					             }
				             })
				             .ToArray());
				
				Task.WaitAll((await songItemStorage.GetFilesAsync("*.json"))
				             .Select(async file =>
				             {
					             var data        = JsonConvert.DeserializeObject<Song>(Encoding.UTF8.GetString(await file.GetContentAsync()));
					             var assetEntity = await createAsset(data.Id, data.Name, data.Description, "song");

					             lock (songItemMap)
					             {
						             songItemMap[$"st.pn/item/song/{data.Id}"] = assetEntity;
						             songItemMap[$"/item/song/{data.Id}"]      = assetEntity;
					             }
				             })
				             .ToArray());
				
				Task.WaitAll((await abilityStorage.GetFilesAsync("*.json"))
				             .Select(async file =>
				             {
					             var data        = JsonConvert.DeserializeObject<Ability>(Encoding.UTF8.GetString(await file.GetContentAsync()));
					             var assetEntity = await createAsset(data.Id, data.Name, data.Description, "ability");

					             lock (abilityMap)
					             {
						             abilityMap[$"st.pn/ability/{data.Id}"] = assetEntity;
						             abilityMap[$"/ability/{data.Id}"]      = assetEntity;
					             }
				             })
				             .ToArray());

				Task.WaitAll((await roleStorage.GetFilesAsync("*.json"))
				             .Select(async file =>
				             {
					             var data             = JsonConvert.DeserializeObject<Role>(Encoding.UTF8.GetString(await file.GetContentAsync()));
					             var allowedEquipment = new Dictionary<DbEntityRepresentation<AssetEntity>, DbEntityRepresentation<AssetEntity>[]>();
					             foreach (var (key, values) in data.AllowedEquipments)
					             {
						             if (!equipRootMap.TryGetValue(key, out var eqRootRepresentation))
						             {
							             logger.ZLogError("Invalid EqRoot {0} on Role {1}", key, data.Id);
							             return;
						             }

						             var equipmentTypes = new DbEntityRepresentation<AssetEntity>[values.Length];
						             for (var index = 0; index < values.Length; index++)
						             {
							             var equipType = values[index];
							             if (!itemTypeMap.TryGetValue(equipType, out var itemTypeRepresentation))
							             {
								             logger.ZLogError("Invalid ItemType {0} on Role {1}", equipType, data.Id);
								             return;
							             }

							             equipmentTypes[index] = itemTypeRepresentation;
						             }
						             
						             allowedEquipment[eqRootRepresentation] = equipmentTypes;
					             }

					             var defaultAbilities = new Dictionary<DbEntityRepresentation<AssetEntity>, ComboAbilityView>()
					             {
						             [songItemMap["/item/song/march"]]    = new(mid: abilityMap["/ability/default/march"]),
						             [songItemMap["/item/song/jump"]]     = new(mid: abilityMap["/ability/default/jump"]),
						             [songItemMap["/item/song/retreat"]]  = new(mid: abilityMap["/ability/default/retreat"]),
						             [songItemMap["/item/song/charge"]]   = new(mid: abilityMap["/ability/default/charge"]),
						             [songItemMap["/item/song/party"]]    = new(mid: abilityMap["/ability/default/party"]),
						             [songItemMap["/item/song/backward"]] = new(mid: abilityMap["/ability/default/backward"]),
					             };
					             data.DefaultAbilities ??= new();
					             foreach (var (key, view) in data.DefaultAbilities)
					             {
						             defaultAbilities[songItemMap[key]] = view.Length switch
						             {							             
							             3 => new(abilityMap[view[0]], abilityMap[view[1]], abilityMap[view[2]]),
							             2 => new(abilityMap[view[0]], abilityMap[view[1]]),
							             1 => new(mid: abilityMap[view[0]]),
							             _ => throw new ArgumentOutOfRangeException($"{nameof(view.Length)}: {view.Length} [0,2]")
						             };
					             }

					             var assetEntity = await createAsset(data.Id, data.Name, data.Description, "role");
					             await assetEntity.ReplaceAsync(new AssetRoleData(
						             allowedEquipment,
						             new()
						             {
							             [string.Empty] = defaultAbilities
						             },
						             new[] {string.Empty})
					             );

					             lock (roleMap)
					             {
						             roleMap[$"st.pn/role/{data.Id}"] = assetEntity;
						             roleMap[$"/role/{data.Id}"]      = assetEntity;
					             }
				             })
				             .ToArray());

				Task.WaitAll((await kitStorage.GetFilesAsync("*.json"))
				             .Select(async file =>
				             {
					             var data  = JsonConvert.DeserializeObject<Kit>(Encoding.UTF8.GetString(await file.GetContentAsync()));
					             if (data.Roles is null)
						             throw new NullReferenceException($"{nameof(data.Roles)} on kit {data.Id}");
					             
					             var roles = new DbEntityRepresentation<AssetEntity>[data.Roles.Length];
					             for (var index = 0; index < data.Roles.Length; index++)
					             {
						             var role = data.Roles[index];
						             if (!roleMap.TryGetValue(role, out var roleAssetEntity))
						             {
							             logger.ZLogError("Kit {0} role {1} not found", data.Id, role);
							             return;
						             }

						             roles[index] = roleAssetEntity;
					             }

					             var assetEntity = await createAsset(data.Id, data.Name, data.Description, "kit");
					             await assetEntity.ReplaceAsync(new AssetKitData(roles));
				             })
				             .ToArray());

				Task.WaitAll((await archetypeStorage.GetFilesAsync("*.json"))
				             .Select(async file =>
				             {
					             var data = JsonConvert.DeserializeObject<Archetype>(Encoding.UTF8.GetString(await file.GetContentAsync()));
					             await createAsset(data.Id, data.Name, data.Description, "archetype");
				             })
				             .ToArray());

				Task.WaitAll((await hierarchyStorage.GetFilesAsync("*.json"))
				             .Select(async file =>
				             {
					             var data = JsonConvert.DeserializeObject<Hierarchy>(Encoding.UTF8.GetString(await file.GetContentAsync()));
					             await createAsset(data.Id, data.Name, data.Description, "hierarchy");
				             })
				             .ToArray());

				Task.WaitAll((await equipmentStorage.GetFilesAsync("*.json"))
				             .Select(async file =>
				             {
					             var data = JsonConvert.DeserializeObject<Equipment>(Encoding.UTF8.GetString(await file.GetContentAsync()));
					             if (string.IsNullOrEmpty(data.ItemType))
					             {
						             data.ItemType = Path.GetDirectoryName(file.FullName)!
						                             .Replace(equipmentStorage.CurrentPath!, string.Empty)
						                             .Replace("_-", "/")
						                             [1..];
					             }

					             if (!itemTypeMap.TryGetValue(data.ItemType, out var itemTypeAssetEntity))
					             {
						             logger.ZLogError("No equipment type found for {0}", data.ItemType);
						             return;
					             }

					             var assetKey = await createAsset(data.ItemType[(data.ItemType.LastIndexOf('/')+1)..] + "/" + data.Id, data.Name, data.Description, "equipment");
					             await assetKey.ReplaceAsync(new AssetItemType(itemTypeAssetEntity));

					             if (data.IsDefault)
					             {
						             Console.WriteLine("set default");
						             await itemTypeAssetEntity.ToEntity(db).ReplaceAsync(new AssetItemTypeDefaultEquipmentTarget(assetKey));
					             }

				             })
				             .ToArray());

				CompletionTaskSource.SetResult(true);
			}
			else
				throw new InvalidOperationException("Find a way to implement the same thing in other databases.");

			Console.WriteLine("Default Assets recreated.");
		}

		public class AssetBase
		{
			[JsonProperty("id")]
			public string Id;

			[JsonProperty("name")]
			public string Name;

			[JsonProperty("description")]
			public string Description;
		}

		public class ItemType : AssetBase
		{

		}
		
		public class Ability : AssetBase
		{

		}

		public class Role : AssetBase
		{
			[JsonProperty("equipments")]
			// { "EquipmentRoot": ["EquipmentAsset0", "EquipmentAsset1"] }
			// The first equipment asset will be used as the default one
			public Dictionary<string, string[]> AllowedEquipments;

			[JsonProperty("defaultAbilities")]
			// { "Combo": ["Up", "Middle", "Top"] | ["Up", "Middle"] | ["Middle"] }
			public Dictionary<string, string[]> DefaultAbilities;
		}

		public class ItemBase : AssetBase
		{
			[JsonProperty("itemType")]
			public string ItemType;
		}

		public class Song : ItemBase
		{

		}

		public class Kit : AssetBase
		{
			[JsonProperty("roles")]
			public string[] Roles;
		}

		public class EquipmentRoot : AssetBase
		{

		}

		public class Archetype : AssetBase
		{

		}

		public class Hierarchy : AssetBase
		{

		}

		public class Equipment : ItemBase
		{
			[JsonProperty("default")]
			public bool IsDefault;
			
			[JsonProperty("stats")]
			public Dictionary<string, object> Statistics;
		}
	}
}