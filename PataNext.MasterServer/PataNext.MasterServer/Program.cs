using System;
using System.Threading;
using GameHost.Core.Ecs;
using GameHost.Game;
using GameHost.IO;
using MagicOnion.Server;
using project;
using project.Core;
using project.DataBase;
using project.DataBase.Implementations;
using RethinkDb.Driver;

[assembly: AllowAppSystemResolving]

namespace PataNext.MasterServer
{
	class Program
	{
		static void Main(string[] args)
		{
			using var bootstrap = new GameBootstrap();
			bootstrap.GameEntity.Set(new GameName("PataNext.MasterServer"));
			bootstrap.GameEntity.Set(new GameUserStorage(new LocalStorage(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/PataNextMasterServer")));

			bootstrap.GameEntity.Set(typeof(MasterServerApplication));

			bootstrap.Global.Context.BindExisting<IEntityDatabase>(new RethinkDbDatabaseImpl(RethinkDB.R.Connection()
			                                                                         //.Hostname("192.168.1.42")
			                                                                         .Hostname("127.0.0.1")
			                                                                         .Db("PataNext")
			                                                                         .Connect()));

			bootstrap.Setup();
			while (bootstrap.Loop())
			{
				Thread.Sleep(5);
			}
		}
	}
}