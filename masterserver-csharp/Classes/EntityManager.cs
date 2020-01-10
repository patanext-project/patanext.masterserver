using System.Threading.Tasks;
using Google.Protobuf;
using project;
using project.P4Classes;

namespace P4TLBMasterServer
{
	public enum EntityOperation
	{
		Replace,
		Remove
	}

	public interface IEntityUpdateKey<TEntityDescription>
	{
		TEntityDescription Key { get; set; }
	}

	public interface IEntityUpdateValue<TComponent>
	{
		TComponent Value { get; set; }
	}

	public struct OnEntityUpdate<TEntityDescription, TComponent> : IEntityUpdateKey<TEntityDescription>, IEntityUpdateValue<TComponent>
	{
		public EntityOperation    Operation;
		public TEntityDescription Key   { get; set; }
		public TComponent         Value { get; set; }
	}

	public class EntityManager : ManagerBase
	{
		public static readonly string NotificationOnEntityUpdate = "OnEntityUpdate";

		private DatabaseManager databaseManager;

		public override void OnCreate()
		{
			base.OnCreate();

			databaseManager = World.GetOrCreateManager<DatabaseManager>();
		}

		public async Task<bool> DbExists<TEntityDescription>(TEntityDescription entity)
			where TEntityDescription : IEntityDescription
		{
			return await databaseManager.db.KeyExistsAsync(entity.GetEntityIdPath());
		}

		public async Task<bool> HasComponent<TEntityDescription, TComponent>(TEntityDescription entity)
			where TEntityDescription : IEntityDescription
		{
			return await databaseManager.db.SetContainsAsync(entity.GetEntityComponentListPath(), ComponentType<TComponent>.ComponentName);
		}

		public async Task<TComponent> GetComponent<TEntityDescription, TComponent, TMessage>(TEntityDescription entity)
			where TEntityDescription : IEntityDescription
			where TComponent : IComponent<TEntityDescription, TMessage>, new()
			where TMessage : class, IMessage<TMessage>
		{
			var component = new TComponent { };
			if (!await component.OnGetOperationRequested(entity, World, databaseManager))
				Logger.Error($"Component '{typeof(TComponent).Name}' couldn't be retrieved for entity: {entity.ToString()}", true);
			return component;
		}

		public async Task<bool> ReplaceComponent<TEntityDescription, TComponent, TMessage>(TEntityDescription entity, TComponent component)
			where TEntityDescription : IEntityDescription
			where TComponent : IComponent<TEntityDescription, TMessage>
			where TMessage : class, IMessage<TMessage>
		{
			if (!await HasComponent<TEntityDescription, TComponent>(entity))
			{
				await databaseManager.db.SetAddAsync(entity.GetEntityComponentListPath(), ComponentType<TComponent>.ComponentName);
			}

			var success = await component.OnUpdateOperationRequested(entity, World, databaseManager);
			World.Notify(this, NotificationOnEntityUpdate, new OnEntityUpdate<TEntityDescription, TComponent>
			{
				Key       = entity,
				Value     = component,
				Operation = EntityOperation.Replace
			});

			return success;
		}

		public async Task<bool> RemoveComponent<TEntityDescription, TComponent, TMessage>(TEntityDescription entity, TComponent component)
			where TEntityDescription : IEntityDescription
			where TComponent : IComponent<TEntityDescription, TMessage>
			where TMessage : class, IMessage<TMessage>
		{
			var success = await databaseManager.db.SetRemoveAsync(entity.GetEntityComponentListPath(), ComponentType<TComponent>.ComponentName);
			if (!success)
				Logger.Error("Couldn't remove component...", false);

			success &= await component.OnRemoveOperationRequested(entity, World, databaseManager);
			if (success)
				World.Notify(this, NotificationOnEntityUpdate, new OnEntityUpdate<TEntityDescription, TComponent>
				{
					Key       = entity,
					Value     = component,
					Operation = EntityOperation.Remove
				});
			return success;
		}

		public async Task<bool> RemoveComponent<TEntityDescription, TComponent, TMessage>(TEntityDescription entity)
			where TEntityDescription : IEntityDescription
			where TComponent : IComponent<TEntityDescription, TMessage>, IComponentNullRemoveSupport, new()
			where TMessage : class, IMessage<TMessage>
		{
			var success = await databaseManager.db.SetRemoveAsync(entity.GetEntityComponentListPath(), ComponentType<TComponent>.ComponentName);
			if (!success)
				Logger.Error("Couldn't remove component...", false);

			var component = new TComponent {WasNullRemoved = true};
			success &= await component.OnRemoveOperationRequested(entity, World, databaseManager);
			if (success)
				World.Notify(this, NotificationOnEntityUpdate, new OnEntityUpdate<TEntityDescription, TComponent>
				{
					Key       = entity,
					Value     = component,
					Operation = EntityOperation.Replace
				});
			return success;
		}
	}

	public static class ComponentType<T>
	{
		private static string s_ComponentName;

		public static string ComponentName
		{
			get
			{
				if (s_ComponentName != null)
					return s_ComponentName;

				var data = default(T);
				if (data is IComponentCustomName getter)
					return getter.GetComponentName();
				return typeof(T).Name.ToLower();
			}
		}
	}

	public interface IComponentCustomName
	{
		string GetComponentName();
	}
}