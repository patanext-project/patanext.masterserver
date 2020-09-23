using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using GameHost.Core.Ecs;
using GameHost.Core.Threading;
using GameHost.Injection;
using PataNext.MasterServer.Components.Account;
using project;
using project.DataBase;
using project.DataBase.Ecs;

namespace PataNext.MasterServer.DiscordBot
{
	[Name("Account Operations")]
	[Group("account")]
	public class DiscordAccountModule : ModuleBase<SocketCommandContext>
	{
		private readonly IEntityDatabase db;

		public DiscordAccountModule(WorldCollection worldCollection)
		{
			db = new ContextBindingStrategy(worldCollection.Ctx, true).Resolve<IEntityDatabase>();
		}
		
		[Command("info")]
		[Summary("Get information about your account (or another account)")]
		[Remarks("Sensitive information (eg: password, token) are excluded.")]
		public async Task Info()
		{
			
		}

		[Command("create")]
		[Summary("Create a new account if you do not possess one.")]
		[Remarks("This account will be tied to your Discord ID and will use your Username as login.")]
		public Task Create()
		{
			return Create(Context.User.Username);
		}

		[Command("create")]
		[Summary("Create a new account with a customized login if you do not possess one.")]
		[Remarks("This account will be tied to your Discord ID.")]
		public async Task Create(string wantedLogin)
		{
			var filter     = await db.GetMatchFilter<UserEntity>();
			filter.ByField((DiscordAccount c) => c.Id, Context.User.Id);

			var entityList = await filter.RunAsync();
			if (entityList.Count == 0)
			{
				if (string.IsNullOrEmpty(wantedLogin))
					wantedLogin = Context.User.Username;
				;
				if ((await filter.Reset()
				                 .ByField((UserAccount c) => c.Login, wantedLogin)
				                 .RunAsync()).Count > 0)
				{
					await ReplyAsync($"**Account Creation Failed!**\nAn account with login `{wantedLogin}` already exist!");
					return;
				}

				// create
				var userEntity = db.CreateEntity<UserEntity>();
				await userEntity.ReplaceAsync(new DiscordAccount(Context.User.Id));
				await userEntity.ReplaceAsync(new UserAccount {Login = wantedLogin, Password = string.Empty});

				var embed = new EmbedBuilder();
				embed.WithAuthor(Context.User);
				embed.WithTitle("Your new account has been created!");
				embed.WithFields(new EmbedFieldBuilder {Name = "Login", Value = (await userEntity.GetAsync<UserAccount>()).Login});
				embed.WithFooter("No password has been generated, generate one with 'account gentoken'");

				await ReplyAsync(embed: embed.Build());

				return;
			}

			await ReplyAsync("Your account already exist.\nUse the 'info' command to get details.");
		}

		[Command("disconnect")]
		[Summary("Disconnect your account from the MasterServer.")]
		public async Task Disconnect()
		{

		}

		[Command("gentoken")]
		[Summary("Regenerate password, the new password will be sent in PM.")]
		[Remarks("You cannot set a custom password for security reasons.")]
		public async Task RegeneratePassword()
		{ 
			var entityList = await (await db.GetMatchFilter<UserEntity>())
			                     .ByField((DiscordAccount c) => c.Id, Context.User.Id)
			                     .RunAsync(1);
			if (entityList.Count == 0)
				return; // reply that we didn't found any account

			var pwd = await regeneratePassword(entityList.First());

			var msg = await Context.User.SendMessageAsync($"**Generated Password** (if you forgot it, regenerate the password again)\n`{pwd}`\n\n*This message will get destroyed in 1 minute.*");

			// Remove warning about the async call being not completed right after this method.
#pragma warning disable
			Task.Delay(TimeSpan.FromMinutes(1)).ContinueWith(async t => await msg.DeleteAsync());
#pragma warning restore
		}

		// !!! THIS METHOD SHOULD NEVER HAVE A CUSTOM PASSWORD ENTERED BY THE USER.
		private async Task<string> regeneratePassword(DbEntityKey<UserEntity> entity)
		{
			var account = await entity.GetAsync<UserAccount>();
			// Generating a guid and getting the hashcode give some randomness
			var generatedPwd = Math.Abs(Guid.NewGuid()
			                                .GetHashCode())
			                       .ToString();

			account.SetPassword(generatedPwd);
			await entity.ReplaceAsync(account);

			// Don't return the hashed MD5 version of the password, we need to return the clear password to the user.
			// !!! THIS METHOD SHOULD NEVER HAVE A CUSTOM PASSWORD ENTERED BY THE USER.
			return generatedPwd;
		}
	}
}