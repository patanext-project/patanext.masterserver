using System.Threading.Tasks;
using MagicOnion;
using STMasterServer.Shared.Services;

namespace PataNext.MasterServerShared.Services
{
	public interface IGameSaveService : IService<IGameSaveService>
	{
		UnaryResult<string>   CreateSave(UserToken userToken, string name);
		UnaryResult<string[]> ListSaves(string     userGuid);

		UnaryResult<SaveDetails> GetDetails(string         saveId);
		UnaryResult<string>      GetFavoriteSave(string    userId);
		UnaryResult<bool>        SetFavoriteSave(UserToken userToken, string saveId);
	}

	public struct SaveDetails
	{
		public string AlmightyName;
		public string UserId;
	}
}