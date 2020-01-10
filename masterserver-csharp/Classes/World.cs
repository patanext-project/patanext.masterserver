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
			var list = m_MappedManager.ToList();
			foreach (var manager in list)
			{
				manager.Value.OnUpdate();
			}
		}

		public void Notify<T>(object caller, string eventName, T data)
		{
			Console.WriteLine($"notification: {eventName}:{data.GetType()}");
			try
			{
				var list = m_MappedManager.ToList();
				foreach (var (key, manager) in list)
				{
					manager.OnNotification(caller, eventName, data);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
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