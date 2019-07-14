using System;
using System.Collections.Generic;

namespace P4TLBMasterServer
{
	public class World
	{
		private Dictionary<Type, object> m_MappedImplementationInstance;
		private Dictionary<Type, object> m_MappedManager;

		public World(Dictionary<Type, object> mappedImplementationInstance)
		{
			m_MappedImplementationInstance = mappedImplementationInstance;
			m_MappedManager = new Dictionary<Type, object>();
		}

		public T GetImplInstance<T>()
			where T : class
		{
			return (T) m_MappedImplementationInstance[typeof(T)];
		}

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
	}

	public abstract class ManagerBase
	{
		internal World World;
		
		public virtual void OnCreate() {}
	}
}