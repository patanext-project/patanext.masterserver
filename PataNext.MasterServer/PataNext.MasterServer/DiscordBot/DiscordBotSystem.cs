using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using GameHost.Core.Ecs;
using Microsoft.Extensions.DependencyInjection;
using PataNext.MasterServer.Providers;
using PataNext.MasterServer.Systems;
using PataNext.MasterServer.Systems.Core;
using project.Core;
using project.DataBase;

namespace PataNext.MasterServer.DiscordBot
{
	[RestrictToApplication(typeof(MasterServerApplication))]
	public class DiscordBotSystem : AppSystem
	{
		private IEntityDatabase db;
		private UnitProvider    unitProvider;
		private UnitRoleSystem  roleSystem;
		private UnitPresetProfileSystem unitProfileSystem;

		private GameSaveProvider gameSaveProvider;

		private DiscordSocketClient client;
		private CommandService      commands;

		private IServiceProvider ServiceProvider =>
			new ServiceCollection()
				.AddSingleton(commands)
				.AddSingleton(World)
				.AddSingleton(db)
				.AddSingleton(unitProvider)
				.AddSingleton(gameSaveProvider)
				.AddSingleton(roleSystem)
				.AddSingleton(unitProfileSystem)
				.BuildServiceProvider();

		public DiscordBotSystem(WorldCollection collection) : base(collection)
		{
			DependencyResolver.Add(() => ref db);
			DependencyResolver.Add(() => ref unitProvider);
			DependencyResolver.Add(() => ref gameSaveProvider);
			DependencyResolver.Add(() => ref roleSystem);
			DependencyResolver.Add(() => ref unitProfileSystem);

			client   = new DiscordSocketClient();
			commands = new CommandService();
			commands.Log += (msg) =>
			{
				Console.WriteLine($"DISCORD COMMANDS -> {msg}");
				return Task.CompletedTask;
			};

			client.Log += (msg) =>
			{
				Console.WriteLine($"DISCORD -> {msg}");
				return Task.CompletedTask;
			};
			client.MessageReceived += HandleCommandAsync;
		}

		protected override void OnDependenciesResolved(IEnumerable<object> dependencies)
		{
			base.OnDependenciesResolved(dependencies);

			// 'P4MS-DCB' is the secret bot token
			var token = Environment.GetEnvironmentVariable("P4MS-DCB", EnvironmentVariableTarget.User);
			if (!string.IsNullOrEmpty(token))
			{
				CreateDiscordBot(token);
			}
		}

		private async void CreateDiscordBot(string token)
		{
			await client.LoginAsync(TokenType.Bot, token);
			await client.StartAsync();

			await client.SetGameAsync($"& | @mention", string.Empty, ActivityType.Listening);
			await client.SetStatusAsync(UserStatus.Online);

			await commands.AddModulesAsync(Assembly.GetEntryAssembly(), ServiceProvider);
		}

		private async Task HandleCommandAsync(SocketMessage messageParam)
		{
			var message = messageParam as SocketUserMessage;
			if (message == null)
				return;

			var strCursor = 0;
			if ((!message.HasStringPrefix("&", ref strCursor) && !message.HasMentionPrefix(client.CurrentUser, ref strCursor))
			    || message.Author.IsBot)
				return;

			await commands.ExecuteAsync(new SocketCommandContext(client, message), strCursor, ServiceProvider);
		}
	}
}