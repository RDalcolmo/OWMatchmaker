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
	public class Commands : ModuleBase
	{
		public Commands()
		{
			
		}

		[Command("create")]
		[Alias("c")]
		[RequireBotPermission(GuildPermission.AddReactions)]
		public async Task CreateLobby()
		{

		}

		[Command("shuffle")]
		[Alias("s")]
		public async Task ShuffleLobby()
		{

		}
	}

	[Group("help")]
	[Alias("h")]
	public class HelpModule : ModuleBase
	{
		[Command("register")]
		[Alias("r")]
		public async Task RegistrationHelp()
		{
			await Context.User.SendMessageAsync(
				"Here is a list of commands:\n" +
				"```\n" +
				"ADMIN COMMANDS:\n\n" +
				"!bcast set - Assigns the current channel to the broadcasting list.\n" +
				"!bcast remove - Removes the bot from broadcasting to the guild.\n" +
				"```\n" +
				"```\n" +
				"USER COMMANDS:\n\n" +
				"!bday next - Broadcast a list of birthdays within the next 14 days.\n" +
				"```"
				);
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

			var messageSent = await ReplyAsync(null, embed: embed).ConfigureAwait(false);

			using (var _dbContext = new OWMatchmakerContext())
			{
				await _dbContext.RegistrationMessages.AddAsync(new RegistrationMessages() { InitializedMessageId = (long)initializedMessage.Id, MessageId = (long)messageSent.Id, OwnerId = (long)Context.User.Id, ExpiresIn = DateTime.Now.AddMinutes(10) });
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
