using System;
using System.Threading.Tasks;
using GameHost.Game;
using Newtonsoft.Json;
using project.DataBase.Implementations;
using RethinkDb.Driver;

namespace project
{
	class Program
	{
		public static async Task Main()
		{
			var db = new RethinkDbDatabaseImpl(RethinkDB.R.Connection()
			                                            .Hostname("127.0.0.1")
			                                            .Db("PataNext")
			                                            .Connect());
			
			/**/
		}
	}
}