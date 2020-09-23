using System;
using System.Security.Cryptography;
using System.Text;
using project.DataBase;
using project.DataBase.Ecs;
using RethinkDb.Driver.Net;
using static RethinkDb.Driver.RethinkDB;

namespace project
{
	public struct UserEntity : IEntityDescription
	{
	}

	public struct UserAccount : IEntityComponent<UserEntity>
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
			Password = CreateMD5(clear);
		}

		private static string CreateMD5(string input)
		{
			// Use input string to calculate MD5 hash
			using var md5        = MD5.Create();
			var       inputBytes = Encoding.ASCII.GetBytes(input);
			var       hashBytes  = md5.ComputeHash(inputBytes);

			// Convert the byte array to hexadecimal string
			var sb = new StringBuilder();
			foreach (var t in hashBytes)
			{
				sb.Append(t.ToString("X2"));
			}
			return sb.ToString();
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