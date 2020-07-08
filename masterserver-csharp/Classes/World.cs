using System;
using System.Collections.Generic;
using System.Linq;

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

		public object GetOrCreateManager(Type type)
		{
			if (!m_MappedManager.TryGetValue(type, out var obj))
			{
				var gen = (ManagerBase) Activator.CreateInstance(type);
				m_MappedManager[type] = gen;

				gen.World = this;
				gen.OnCreate();
				
				return gen;
			}

			return obj;	
		}
		
		/// <summary>
		/// Get or create a manager
		/// </summary>
		/// <typeparam name="T">Type of the manager</typeparam>
		/// <returns>The manager</returns>
		public T GetOrCreateManager<T>()
			where T : ManagerBase, new()
		{
			return (T) GetOrCreateManager(typeof(T));
		}

		private List<ManagerBase> m_SystemList = new List<ManagerBase>();
		/// <summary>
		/// Update the world
		/// </summary>
		public void Update()
		{
			if (m_MappedManager.Count != m_SystemList.Count)
			{
				m_SystemList.Clear();
				foreach (var manager in m_MappedManager.Values)
				{
					m_SystemList.Add(manager);
				}
			}

			foreach (var manager in m_SystemList)
			{
				manager.OnUpdate();
			}
		}

		public void Notify<T>(object caller, string eventName, T data)
		{
			Console.WriteLine($"notification: {eventName}:{data.GetType()}");
			try
			{
				foreach (var manager in m_SystemList)
				{
					manager.OnNotification(caller, eventName, data);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Notify Exception:\n" + ex.ToString());
				throw;
			}
		}
	}

	public abstract class ManagerBase
	{
		internal World World;
		
		public virtual void OnCreate() {}
		public virtual void OnUpdate() {}
		public virtual void OnNotification<T>(object caller, string eventName, T data) {}
	}
}