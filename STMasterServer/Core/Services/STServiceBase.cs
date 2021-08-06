using System.Collections.Generic;
using GameHost.Core.Ecs;
using GameHost.Core.Threading;
using GameHost.Injection;
using MagicOnion;
using MagicOnion.Server;
using MagicOnion.Server.Hubs;

namespace project.Core.Services
{
	public abstract class STServiceBase<T> : ServiceBase<T>
		where T : IServiceMarker
	{
		public readonly WorldCollection    World;
		public readonly DependencyResolver DependencyResolver;

		public STServiceBase(WorldCollection worldCollection)
		{
			World = worldCollection;

			DependencyResolver = new DependencyResolver
			(
				new ContextBindingStrategy(worldCollection.Ctx, true).Resolve<IScheduler>(),
				worldCollection.Ctx,
				GetType().Name
			);
			DependencyResolver.DefaultStrategy = new DefaultAppObjectStrategy(this, worldCollection);
			DependencyResolver.OnComplete(OnDependenciesCompleted);
		}

		protected virtual void OnDependenciesCompleted(IEnumerable<object> obj)
		{
		}
	}
	
	public abstract class STStreamingHubBase<THubInterface, TReceiver> : StreamingHubBase<THubInterface, TReceiver>
		where THubInterface : IStreamingHub<THubInterface, TReceiver>
	{
		public readonly WorldCollection    World;
		public readonly DependencyResolver DependencyResolver;

		public STStreamingHubBase(WorldCollection worldCollection)
		{
			World = worldCollection;

			DependencyResolver = new DependencyResolver
			(
				new ContextBindingStrategy(worldCollection.Ctx, true).Resolve<IScheduler>(),
				worldCollection.Ctx,
				GetType().Name
			);
			DependencyResolver.DefaultStrategy = new DefaultAppObjectStrategy(this, worldCollection);
			DependencyResolver.OnComplete(OnDependenciesCompleted);
		}

		protected virtual void OnDependenciesCompleted(IEnumerable<object> obj)
		{
		}
	}
}