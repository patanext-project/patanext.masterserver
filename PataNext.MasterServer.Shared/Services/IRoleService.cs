using System.Collections.Generic;
using MagicOnion;

namespace PataNext.MasterServerShared.Services
{
	public interface IRoleService : IService<IRoleService>
	{
		UnaryResult<Dictionary<string, string[]>> GetAllowedEquipments(string roleId);
	}
}