using System.Threading.Tasks;
using Google.Protobuf;

namespace P4TLBMasterServer
{
	public interface IEntityDescription
	{
		string GetEntityIdPath();
		string GetEntityComponentPath(string componentName);
		string GetEntityComponentListPath();
	}

	public interface IComponentNullRemoveSupport
	{
		bool WasNullRemoved { get; set; }
	}

	public interface IComponent<TEntity, TDbData>
		where TEntity : IEntityDescription
		where TDbData : class, IMessage<TDbData>
	{
		TDbData Serialized { get; set; }

		Task<bool> OnGetOperationRequested(TEntity    entity, World world, DatabaseManager dbMgr);
		Task<bool> OnUpdateOperationRequested(TEntity entity, World world, DatabaseManager dbMgr);
		Task<bool> OnRemoveOperationRequested(TEntity entity, World world, DatabaseManager dbMgr);

		void CheckComponentValidity(TEntity entity, World world);
	}

	public static class ComponentInvoke
	{
		public static async Task<TMessage> Get<TEntity, TComponent, TMessage>(TEntity entity, DatabaseManager dbMgr)
			where TEntity : IEntityDescription
			where TComponent : IComponent<TEntity, TMessage>
			where TMessage : class, IMessage<TMessage>, new()
		{
			return await dbMgr.GetAsync<TMessage>(entity.GetEntityComponentPath(ComponentType<TComponent>.ComponentName));
		}

		public static async Task<bool> Update<TEntity, TComponent, TMessage>(TEntity entity, TComponent component, DatabaseManager dbMgr)
			where TEntity : IEntityDescription
			where TComponent : IComponent<TEntity, TMessage>
			where TMessage : class, IMessage<TMessage>
		{
			return await dbMgr.SetAsync(entity.GetEntityComponentPath(ComponentType<TComponent>.ComponentName), component.Serialized);
		}

		public static async Task<bool> Remove<TEntity, TComponent, TMessage>(TEntity entity, DatabaseManager dbMgr)
			where TEntity : IEntityDescription
			where TComponent : IComponent<TEntity, TMessage>
			where TMessage : class, IMessage<TMessage>
		{
			return await dbMgr.db.KeyDeleteAsync(entity.GetEntityComponentPath(ComponentType<TComponent>.ComponentName));
		}
	}
}