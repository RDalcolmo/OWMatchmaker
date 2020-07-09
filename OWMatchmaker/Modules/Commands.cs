using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using OWMatchmaker.Controllers;
using OWMatchmaker.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OWMatchmaker.Modules
{
	[Group("lobby")]
	[Alias("l")]
	[RequireBotPermission(GuildPermission.ManageMessages)]
	[RequireContext(ContextType.Guild)]
	public class LobbyModule : ModuleBase
	{
		private readonly IConfiguration _config;

		public LobbyModule(IConfiguration config)
		{
			_config = config;
		}

		[Command("create")]
		[Alias("c")]
		[RequireBotPermission(GuildPermission.AddReactions)]
		public async Task CreateLobby()
		{
			using (var _dbContext = new OWMatchmakerContext())
			{
				var player = await _dbContext.Players.FindAsync((long)Context.User.Id);
				
				if (player == null)
				{
					await Context.User.SendMessageAsync("In order to create a lobby, you must connect your BattleNet account with our service. Please type `!register` to begin the process.");
					await Context.Message.DeleteAsync();
					return;
				}

				var lobby = await _dbContext.Lobbies.FindAsync((long)Context.User.Id);
				
				if (lobby != null)
				{
					await Context.User.SendMessageAsync("You already have a lobby running. Please use the command `!lobby end` before creating a new one.");
					await Context.Message.DeleteAsync();
					return;
				}

				var builder = new EmbedBuilder()
									.WithTitle($"Lobby Owner: {player.BattleTag}")
									.WithDescription("React below to join: 🛡 Tanks, ⚔ DPS, 💉 Support, ❌ Leave Lobby.")
									.WithColor(new Color(0x9B4800))
									.WithFooter(footer => {
										footer
											.WithText("owmatcher.com")
											.WithIconUrl(_config["DiscordFooterIconURL"]);
									})
									.AddField("Spectators", "<empty>")
									.AddField("Team 1", "<empty slot>\n<empty slot>\n<empty slot>\n<empty slot>\n<empty slot>\n<empty slot>", true)
									.AddField("Team 2", "<empty slot>\n<empty slot>\n<empty slot>\n<empty slot>\n<empty slot>\n<empty slot>", true);
				var embed = builder.Build();
				var lobbyMessage = await ReplyAsync(embed: embed).ConfigureAwait(false);

				var emotes = new[]
				{
					new Emoji("🛡"),
					new Emoji("⚔"),
					new Emoji("💉"),
					new Emoji("❌"),
				};
				await lobbyMessage.AddReactionsAsync(emotes);

				await _dbContext.Lobbies.AddAsync(new Lobbies() { LobbyId = (long)lobbyMessage.Id, OwnerId = (long)Context.User.Id });
				await _dbContext.SaveChangesAsync();


				await Context.Message.DeleteAsync();
			}
		}

		[Command("shuffle")]
		[Alias("s")]
		public async Task ShuffleLobby()
		{
			

			
		}

		[Command("end")]
		[Alias("e")]
		public async Task EndLobby()
		{
			try
			{
				using (var _dbContext = new OWMatchmakerContext())
				{
					//The Include is so that we can delete all the Child keys from table Matches.
					var lobby = await _dbContext.Lobbies.Include(x => x.Matches).FirstOrDefaultAsync(u => u.OwnerId == (long)Context.User.Id);

					if (lobby == null)
					{
						await Context.User.SendMessageAsync("Command failed. You have no running lobbies.");
						await Context.Message.DeleteAsync();
						return;
					}
					_dbContext.Lobbies.Remove(lobby);
					var result = await _dbContext.SaveChangesAsync();

					if (result > 0)
					{
						await Context.User.SendMessageAsync("Your lobby has been closed. You may now create a new lobby.");
						await Context.Message.DeleteAsync();
						await Context.Channel.DeleteMessageAsync((ulong)lobby.LobbyId);
						return;
					}

				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
		}


		[Command("help")]
		[Alias("h")]
		public async Task HelpLobby()
		{
			var builder = new EmbedBuilder()
								.WithTitle("Here is a list of commands")
								.WithDescription("`!lobby create [Alias !l c]` - Creates a lobby. You may only have one lobby open at a time. The cap of players that can join is 24.\n`!lobby end [Alias !l e]` - Ends a lobby session. Use this when you're done hosting a lobby.\n`!lobby shuffle [Alias !l s]` - Shuffles a lobby. There must be at least 12 players in the lobby before executing the shuffle command.")
								.WithColor(new Color(0x9B4800))
								.WithFooter(footer => {
									footer
										.WithText("owmatcher.com")
										.WithIconUrl(_config["DiscordFooterIconURL"]);
								});
			var embed = builder.Build();
			await ReplyAsync(embed: embed).ConfigureAwait(false);
		}
	}

	
	public class HelpModule : ModuleBase
	{
		private readonly IConfiguration _config;

		public HelpModule(IConfiguration config)
		{
			_config = config;
		}

		[Command("help")]
		[Alias("h")]
		public async Task HelpCommand()
		{
			var builder = new EmbedBuilder()
								.WithTitle("Here is a list of commands")
								.WithDescription("`!register [Alias !r]` - Registers the player to the database. User most login using their BattleNet account.\n`!refresh` - Refreshes the user's SR. In most cases a user's SR will be 0 if their profile is not public, or they have not played a competitive game this season.\n`!lobby help [Alias !l help/h]` - Display information for creating a lobby.")
								.WithColor(new Color(0x9B4800))
								.WithFooter(footer => {
									footer
										.WithText("owmatcher.com")
										.WithIconUrl(_config["DiscordFooterIconURL"]);
								});
			var embed = builder.Build();
			await Context.User.SendMessageAsync(embed: embed).ConfigureAwait(false);
			await Context.Message.DeleteAsync();
		}
	}

	[RequireContext(ContextType.DM)]
	public class RegistrationModule : ModuleBase
	{
		private readonly IConfiguration _config;

		public RegistrationModule(IConfiguration config)
		{
			_config = config;
		}

		[Command("register")]
		[Alias("r")]
		public async Task RegisterPlayer()
		{
			var initializedMessage = await ReplyAsync("Initializing registration. Please make sure your Overwatch Profile is set to public prior to registration. Follow the link below to complete registration.");

			var builder = new EmbedBuilder()
								.WithTitle("Click Here to Register")
								.WithUrl(_config["BlizzardOAuthURL"] + initializedMessage.Id)
								.WithColor(new Color(0x9B4800))
								.WithFooter(footer => {
									footer
										.WithIconUrl(_config["DiscordFooterIconURL"])
										.WithText("owmatcher.com");
								})
								.AddField("Registration Program", "Welcome Hero! My name is Matcher and I will guide you through this process.\nClick the link above to Authorize.");
			var embed = builder.Build();

			var messageSent = await ReplyAsync(embed: embed).ConfigureAwait(false);

			using (var _dbContext = new OWMatchmakerContext())
			{
				await _dbContext.RegistrationMessages.AddAsync(new RegistrationMessages() { InitializedMessageId = (long)initializedMessage.Id, MessageId = (long)messageSent.Id, OwnerId = (long)Context.User.Id, ExpiresIn = DateTime.Now.AddMinutes(5) });
				await _dbContext.SaveChangesAsync();
			}
		}

		[Command("refresh")]
		public async Task RefreshStats()
		{
			using (var _dbContext = new OWMatchmakerContext())
			{
				var player = await _dbContext.Players.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == (long)Context.User.Id);
				var message = await ReplyAsync("Synchronizing your SR, please wait...");
				if (player == null)
				{
					await ReplyAsync("I'm sorry but unfortunately I couldn't find a registered BattleTag. Please register `!register`.");
					return;
				}

				OAuthController controller = new OAuthController(_config, null);

				var playerStats = await controller.GetOWUserRating(player.BattleTag);
				player.Sr = playerStats.Rating;

				_dbContext.Players.Update(player);
				var result = await _dbContext.SaveChangesAsync();

				if (result > 0)
				{
					await message.ModifyAsync(u => u.Content = $"Your SR has been set to `{player.Sr}`, if you believe that this is wrong, please ensure your profile is set to public and the current season's placements have been played. In addition, there may be some delay for the data to update.");
				}
			}
		}

		//[Command("role")]
		//public async Task RegisterRole()
		//{
		//	var playerID = (long)Context.User.Id;
		//	var player = await _dbContext.Players.FindAsync(playerID);

		//	if (player == null)
		//	{
		//		await ReplyAsync("We could not set your role as you are currently not registered with our application. Please use the command '**!r p**' before setting your role.");
		//		return;
		//	}

		//	var embed = new EmbedBuilder()
		//						.WithTitle("Select your role:")
		//						.WithDescription("🛡 **Tank**\n⚔ **DPS**\n💉 **Support**")
		//						.WithColor(new Color(0x32BD27))
		//						.WithTimestamp(DateTimeOffset.FromUnixTimeMilliseconds(1593910889108))
		//						.WithFooter(footer =>
		//						{
		//							footer.WithText("OW Matcher");
		//						}).Build();
		//	var emotes = new[]
		//	{
		//		new Emoji("⚔"),
		//		new Emoji("🛡"),
		//		new Emoji("💉"),
		//	};
		//	var sent = await ReplyAsync(null, false, embed);
		//	await sent.AddReactionsAsync(emotes);

		//	await _dbContext.Messages.AddAsync(new Messages() { MessageId = (long)sent.Id, Type = 1 });
		//	await _dbContext.SaveChangesAsync();
		//}
	}
}
