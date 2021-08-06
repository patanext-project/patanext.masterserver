using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using BidirectionalMap;
using GameHost.Core.Ecs;
using project.DataBase;
using STMasterServer.Shared.Services;

namespace project.Core.Systems
{
	public class ConnectedUserSystem : AppSystem
	{
		private Dictionary<DbEntityRepresentation<UserEntity>, string> userTokenMap;
		
		public ConnectedUserSystem(WorldCollection collection) : base(collection)
		{
			userTokenMap = new Dictionary<DbEntityRepresentation<UserEntity>, string>();
		}

		public string GetOrCreateToken(DbEntityRepresentation<UserEntity> userEntity)
		{
			if (userTokenMap.TryGetValue(userEntity, out var token))
				return token;
			
			userTokenMap.Add(userEntity, token = GetToken(userEntity.Value));
			return token;
		}

		public bool Match(UserToken userToken) => Match(new DbEntityRepresentation<UserEntity> {Value = userToken.Representation}, userToken.Token);

		public bool TryMatch(UserToken userToken, out DbEntityRepresentation<UserEntity> rep)
		{
			rep = new DbEntityRepresentation<UserEntity> {Value = userToken.Representation};
			return Match(rep, userToken.Token);
		}

		public bool Match(DbEntityRepresentation<UserEntity> userEntity, string toMatch)
		{
			if (!userTokenMap.TryGetValue(userEntity, out var currToken))
				return false;
			return currToken.Equals(toMatch);
		}

		private static string GetToken(string rep)
		{
			var length     = 5;
			var privateStr = default(string);
			using (var rng = new RNGCryptoServiceProvider())
			{
				var bytes = new byte[(length * 6 + 7) / 8];
				rng.GetBytes(bytes);
				privateStr = Convert.ToBase64String(bytes);
			}

			privateStr = privateStr.Replace(':', 'c');

			return $"{Environment.TickCount64:X}{privateStr}".ToLower();
		}
	}
}