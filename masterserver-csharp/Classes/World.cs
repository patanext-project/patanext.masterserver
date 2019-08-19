using System;
using System.Collections.Generic;

namespace P4TLBMasterServer
{
	public class World
	{
		private Dictionary<Type, object> m_MappedImplementationInstance;
		private Dictionary<Type, ManagerBase> m_MappedManager;

		public World(Dictionary<Type, object> mappedImplementationInstance)
		{
			m_MappedImplementationInstance = mappedImplementationInstance;
			m_MappedManager = new Dictionary<Type, ManagerBase>();
		}

		/// <summary>
		/// Get an implementation (gRpc service)
		/// </summary>
		/// <typeparam name="T">The service type</typeparam>
		/// <returns>Return an implementation</returns>
		public T GetImplInstance<T>()
			where T : class
		{
			return (T) m_MappedImplementationInstance[typeof(T)];
		}

		/// <summary>
		/// Get or create a manager
		/// </summary>
		/// <typeparam name="T">Type of the manager</typeparam>
		/// <returns>The manager</returns>
		public T GetOrCreateManager<T>()
			where T : ManagerBase, new()
		{
			if (!m_MappedManager.TryGetValue(typeof(T), out var obj))
			{
				var gen = new T();
				m_MappedManager[typeof(T)] = gen;

				gen.World = this;
				gen.OnCreate();
				
				return gen;
			}

			return (T) obj;
		}

		/// <summary>
		/// Update the world
		/// </summary>
		public void Update()
		{
			foreach (var manager in m_MappedManager)
			{
				manager.Value.OnUpdate();
			}
		}
	}

	public abstract class ManagerBase
	{
		internal World World;
		
		public virtual void OnCreate() {}
		public virtual void OnUpdate() {}
	}
}