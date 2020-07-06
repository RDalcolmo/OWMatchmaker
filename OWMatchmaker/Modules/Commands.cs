using System;
using System.Linq;
using System.Threading.Tasks;
using OWMatchmaker.Models;
using Discord.Commands;
using OWMatchmaker.Handlers;
using Discord;
using Discord.Addons.Interactive;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Reflection;
using Birthday_Bot.Models;

namespace OWMatchmaker.Modules
{
	[Group("lobby")]
	[Alias("l")]
	[RequireBotPermission(GuildPermission.ManageMessages)]
	[RequireContext(ContextType.Guild)]
	public class Commands : ModuleBase
	{
		public IAPIHandler apiHandler;

		public Commands(IAPIHandler _apiHandler)
		{
			apiHandler = _apiHandler;
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

	public class HelpModule : ModuleBase
	{
		[Command("help")]
		public async Task Help()
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

	[Group("register")]
	[Alias("r")]
	[RequireContext(ContextType.DM)]
	public class RegistrationModule : ModuleBase
	{
		private readonly OWMatchmakerContext _dbContext;

		public RegistrationModule(OWMatchmakerContext dbContext)
		{
			_dbContext = dbContext;
		}

		[Command("player")]
		[Alias("p")]
		public async Task RegisterPlayer()
		{
			var playerID = (long)Context.User.Id;

			var player = await _dbContext.Players.FindAsync(playerID);

			if (player != null)
			{
				await ReplyAsync("You are already registered as a player!");
			}
			else
			{
				await _dbContext.Players.AddAsync(new Players() { UserId = playerID });
				var result = await _dbContext.SaveChangesAsync();
				if (result > 0)
				{
					await ReplyAsync("You have been successfully registered! Please set your SR using the command '!r sr SRNUMBER' and your role with '!r role'.");
				}
			}
		}

		[Command("sr")]
		public async Task RegisterSR([Remainder] short rating)
		{
			var playerID = (long)Context.User.Id;
			var player = await _dbContext.Players.FindAsync(playerID);

			if (player == null)
			{
				await ReplyAsync("We could not set your SR as you are currently not registered with our application. Please use the command '**!r p**' before setting your SR.");
				return;
			}

			await ReplyAsync($"You said {rating}");
		}

		[Command("role")]
		public async Task RegisterRole()
		{
			var playerID = (long)Context.User.Id;
			var player = await _dbContext.Players.FindAsync(playerID);
			
			if (player == null)
			{
				await ReplyAsync("We could not set your role as you are currently not registered with our application. Please use the command '**!r p**' before setting your role.");
				return;
			}

			var embed = new EmbedBuilder()
								.WithTitle("Select your role:")
								.WithDescription("🛡 **Tank**\n⚔ **DPS**\n💉 **Support**")
								.WithColor(new Color(0x32BD27))
								.WithTimestamp(DateTimeOffset.FromUnixTimeMilliseconds(1593910889108))
								.WithFooter(footer =>
								{
									footer.WithText("OW Matcher");
								}).Build();
			var emotes = new[]
			{
				new Emoji("⚔"),
				new Emoji("🛡"),
				new Emoji("💉"),
			};
			var sent = await ReplyAsync(null, false, embed);
			await sent.AddReactionsAsync(emotes);
			
			await _dbContext.ReactMessages.AddAsync(new ReactMessages() { MessageId = (long)sent.Id, ReactionType = 1 });
			await _dbContext.SaveChangesAsync();
		}
	}
}
