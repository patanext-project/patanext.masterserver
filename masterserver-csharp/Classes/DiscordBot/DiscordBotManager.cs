using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace P4TLBMasterServer.DiscordBot
{
	public class DiscordBotManager : ManagerBase
	{
		private DiscordSocketClient m_Client;
		private CommandService m_Commands;

		private IServiceProvider ServiceProvider =>
			new ServiceCollection()
				.AddSingleton(m_Commands)
				.AddSingleton(World)
				.BuildServiceProvider();
		
		public override void OnCreate()
		{
			m_Client = new DiscordSocketClient();
			m_Commands = new CommandService();
			m_Commands.Log += (msg) =>
			{
				Console.WriteLine($"DISCORD COMMANDS -> {msg}");
				return Task.CompletedTask;
			};
			
			m_Client.Log += (msg) =>
			{
				Console.WriteLine($"DISCORD -> {msg}");
				return Task.CompletedTask;
			};
			m_Client.MessageReceived += HandleCommandAsync;

			// 'P4MS-DCB' is the secret bot token
			var token = Environment.GetEnvironmentVariable("P4MS-DCB", EnvironmentVariableTarget.User);
			if (!string.IsNullOrEmpty(token))
			{
				CreateDiscordBot(token);
			}
		}

		private async void CreateDiscordBot(string token)
		{
			await m_Client.LoginAsync(TokenType.Bot, token);
			await m_Client.StartAsync();
			
			await m_Client.SetGameAsync("0 online | 0 searching", string.Empty, ActivityType.Watching);
			await m_Client.SetStatusAsync(UserStatus.Online);

			await m_Commands.AddModulesAsync(Assembly.GetEntryAssembly(), ServiceProvider);
		}

		private async Task HandleCommandAsync(SocketMessage messageParam)
		{
			var message = messageParam as SocketUserMessage;
			if (message == null)
				return;

			var strCursor = 0;

			if ((!message.HasCharPrefix('&', ref strCursor) && !message.HasMentionPrefix(m_Client.CurrentUser, ref strCursor))
			    || message.Author.IsBot)
				return;
			
			await m_Commands.ExecuteAsync(new SocketCommandContext(m_Client, message), strCursor, ServiceProvider);
		}

		public async void UpdateOnlineCounter(int online, int searching)
		{
			await m_Client.SetGameAsync($"{online} online | {searching} searching");
		}


		private int m_PreviousOnlineCount;
		private int m_FlipStatusAt;
		private bool m_ShowOnline;

		public override void OnUpdate()
		{
			var clientMgr = World.GetOrCreateManager<ClientManager>();
			if (m_PreviousOnlineCount != clientMgr.ConnectedCount)
			{
				m_PreviousOnlineCount = clientMgr.ConnectedCount;
				UpdateOnlineCounter(m_PreviousOnlineCount, 0);
				m_ShowOnline = false;
				m_FlipStatusAt = Environment.TickCount + 5_000;
			}

			if (m_FlipStatusAt < Environment.TickCount)
			{
				m_FlipStatusAt = Environment.TickCount + 10_000;
				if (m_ShowOnline)
				{
					UpdateOnlineCounter(m_PreviousOnlineCount, 0);
				}
				else
				{
					m_Client.SetGameAsync("P4 | &help");
				}

				m_ShowOnline = !m_ShowOnline;
			}
		}
	}
}