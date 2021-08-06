using System;
using System.Security.Cryptography;
using System.Text;
using project.DataBase;
using project.DataBase.Ecs;
using RethinkDb.Driver.Net;
using STMasterServer.Shared.Services.Authentication;
using static RethinkDb.Driver.RethinkDB;

namespace project
{
	public struct UserEntity : IEntityDescription
	{
	}

	public struct UserAccount : IEntityComponent
	{
		public string Login;
		/// <summary>
		/// Hashed password of the user. Do not set a clear password here.
		/// </summary>
		/// <remarks>
		///	To set a hashed password from a clear one, use <see cref="SetPassword"/>
		/// </remarks>
		public string Password;

		public void SetPassword(string clear)
		{
			Password = StandardAuthUtility.CreateMD5(clear);
		}
	}

	public class Test
	{
		public void kek()
		{
			/*void oui(string str)
			{}

			oui(null);
			
			IEntityDatabase db = null;

			Guid.NewGuid()
			
			var userKey = db.CreateEntity<UserEntity>();
			userKey.ReplaceComponent(new UserAccount {Login = "guerro", Password = "123"});
			
			IConnection connection = null;
			R.Db()
			R.Table("authors").Update("");*/
		}
	}
}