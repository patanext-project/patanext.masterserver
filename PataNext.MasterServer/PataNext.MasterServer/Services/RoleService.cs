using System.Collections.Generic;
using System.Linq;
using GameHost.Core.Ecs;
using Grpc.Core;
using JetBrains.Annotations;
using MagicOnion;
using PataNext.MasterServer.Components.Asset;
using PataNext.MasterServerShared.Services;
using project.Core.Entities;
using project.Core.Services;
using project.DataBase;

namespace PataNext.MasterServer.Services
{
	public class RoleService : STServiceBase<IRoleService>, IRoleService
	{
		private IEntityDatabase db;

		public RoleService([NotNull] WorldCollection worldCollection) : base(worldCollection)
		{
			DependencyResolver.Add(() => ref db);
		}

		public async UnaryResult<Dictionary<string, string[]>> GetAllowedEquipments(string roleId)
		{
			await DependencyResolver.AsTask;

			var asset = db.GetEntity<AssetEntity>(roleId);
			if (asset.IsNull)
				throw new RpcException(new(StatusCode.NotFound, $"Role asset '{roleId}' not found"));

			if (!await asset.HasAsync<AssetRoleData>())
				throw new RpcException(new(StatusCode.NotFound, $"Asset is not a role asset"));

			var roleData   = await asset.GetAsync<AssetRoleData>();
			var dictionary = new Dictionary<string, string[]>(roleData.AllowedEquipments.Count);
			foreach (var (attachmentDb, validTypesDb) in roleData.AllowedEquipments)
			{
				var array = new string[validTypesDb.Length];
				for (var i = 0; i < validTypesDb.Length; i++)
					array[i] = validTypesDb[i].Value;

				dictionary[attachmentDb.Value] = array;
			}

			return dictionary;
		}
	}
}