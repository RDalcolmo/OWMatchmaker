using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OWMatchmaker.Handlers;
using OWMatchmaker.Models;
using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq;

namespace OWMatchmaker.Services
{
	public class OWMatchmakerService
	{
		private readonly DiscordSocketClient _discord;
		private readonly CommandService _commands;
		private IServiceProvider _provider;
		private readonly IConfiguration _config;

		public OWMatchmakerService(IServiceProvider provider, DiscordSocketClient discord, CommandService commands, IConfiguration config)
		{
			_config = config;
			_discord = discord;
			_commands = commands;
			_provider = provider;

			_discord.MessageReceived += HandleCommandAsync;
			_discord.Ready += OnReady;
			_discord.ReactionAdded += OnReactionAdded;
			DateTimeHandler.IntervalTimeElapsed += DateTimeHandler_IntervalTimeElapsed;
		}

		private async void DateTimeHandler_IntervalTimeElapsed(object sender, EventArgs e)
		{
			using (var _dbContext = new OWMatchmakerContext())
			{
				var listOfMessages = await _dbContext.RegistrationMessages.AsQueryable().ToListAsync();

				foreach (var message in listOfMessages)
				{
					TimeSpan span = DateTime.Now.Subtract(message.ExpiresIn.Value);
					if (span.TotalSeconds > 0)
					{
						var discordUser = _discord.GetUser((ulong)message.OwnerId);

						//Getting the DM channel and any registration messages that need to be expired.
						var getDMchannel = await discordUser.GetOrCreateDMChannelAsync();
						var getMessage = (await getDMchannel.GetMessageAsync((ulong)message.MessageId)) as IUserMessage;

						var builder = new EmbedBuilder()
											.WithTitle("Link Expired")
											.WithColor(new Color(0x9B4800))
											.WithFooter(footer =>
											{
												footer
													.WithIconUrl(_config["DiscordFooterIconURL"])
													.WithText("owmatcher.com");
											})
											.AddField("Registration Program", "You were too slow! The URL has expired.\nPlease input `!register` again.");
						var embed = builder.Build();

						await getMessage.ModifyAsync(u => u.Embed = embed);

						_dbContext.RegistrationMessages.Remove(message);
						await _dbContext.SaveChangesAsync();
					}
				}
			}
		}

		private async Task OnReactionAdded(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
		{			
			//Prevent the bot from deleting his own reactions
			if (arg3.User.Value.IsBot)
				return;

			var message = await arg1.GetOrDownloadAsync();

			//We only care if the reactions added are to the bot message.
			if (!(message.Author.Id == _discord.CurrentUser.Id))
				return;

			var messageID = (long)arg3.MessageId;
			var userID = (long)arg3.UserId;

			using (var _dbContext = new OWMatchmakerContext())
			{
				//Checking if the player registered before attempting to join a lobby
				var player = await _dbContext.Players.FindAsync(userID);
				if (player == null)
				{
					await message.RemoveReactionAsync(arg3.Emote, arg3.User.Value);
					await arg3.User.GetValueOrDefault().SendMessageAsync("We could not set your role as you do not have a BattleNet account connected to our service. Please go through the registration process. Type `!register` to begin.");
					return;
				}

				//Check if the message being reacted to is a lobby.
				//Prevents reactions from being deleted where they shouldn't be.
				var lobbyOwner = await _dbContext.Lobbies.Include(p => p.Owner).FirstOrDefaultAsync(u => u.LobbyId == messageID);

				if (lobbyOwner == null)
					return;

				//Remove all user reactions from a Lobby message.
				await message.RemoveReactionAsync(arg3.Emote, arg3.User.Value);

				//Get all the players in the lobby in order to build a spectator list
				var playersInLobby = await _dbContext.Matches.AsQueryable().Where(l => l.LobbyId == messageID).Include(p => p.Player).ToListAsync();
				string spectators = "";
				foreach (var person in playersInLobby)
				{
					if (person.Role == (short)Role.Tank)
						spectators += $"{person.Player.BattleTag} (SR: {person.Player.Sr}) [Role: Tank] | ";
					else if (person.Role == (short)Role.DPS)
						spectators += $"{person.Player.BattleTag} (SR: {person.Player.Sr}) [Role: DPS] | ";
					else if (person.Role == (short)Role.Support)
						spectators += $"{person.Player.BattleTag} (SR: {person.Player.Sr}) [Role: Support] | ";
				}
				
				var matches = await _dbContext.Matches.FindAsync(userID);
				int result = 0;

				switch (arg3.Emote.Name)
				{
					case "🛡":
						if (matches == null)
						{
							await _dbContext.Matches.AddAsync(new Matches() { LobbyId = messageID, PlayerId = userID, MatchesPlayed = 0, Role = (short)Role.Tank });
						}
						else
						{
							matches.Role = (short)Role.Tank;
							_dbContext.Update(matches);
						}
						spectators += spectators.Replace($"{player.BattleTag} (SR: {player.Sr}) [Role: Support] | ", "");
						spectators += $"{player.BattleTag} (SR: {player.Sr}) [Role: Tank] | ";
						break;
					case "⚔":
						if (matches == null)
						{
							await _dbContext.Matches.AddAsync(new Matches() { LobbyId = messageID, PlayerId = userID, MatchesPlayed = 0, Role = (short)Role.DPS });
						}
						else
						{
							matches.Role = (short)Role.DPS;
							_dbContext.Update(matches);
						}
						spectators += $"{player.BattleTag} (SR: {player.Sr}) [Role: DPS] | ";
						break;
					case "💉":
						if (matches == null)
						{
							await _dbContext.Matches.AddAsync(new Matches() { LobbyId = messageID, PlayerId = userID, MatchesPlayed = 0, Role = (short)Role.Support });
						}
						else
						{
							matches.Role = (short)Role.Support;
							_dbContext.Update(matches);
						}
						spectators += $"{player.BattleTag} (SR: {player.Sr}) [Role: Support] | ";
						break;
					case "❌":
						if (matches == null)
						{
							//Do nothing?? The player technically isn't in the lobby to begin with.
							//However, we might want to play around with how we want to set the player's priority in getting a game, here.
						}
						else
						{
							_dbContext.Remove(matches);
						}
						spectators += spectators.Replace($"{player.BattleTag} (SR: {player.Sr}) [Role: Support] | ", "");
						break;
					default:
						break;
				}

				result = await _dbContext.SaveChangesAsync();
				if (result > 0)
				{
					var builder = new EmbedBuilder()
									.WithTitle($"Lobby Owner: {lobbyOwner.Owner.BattleTag}")
									.WithDescription("React below to join: 🛡 Tanks, ⚔ DPS, 💉 Support, ❌ Leave Lobby.")
									.WithColor(new Color(0x9B4800))
									.WithFooter(footer => {
										footer
											.WithText("owmatcher.com")
											.WithIconUrl(_config["DiscordFooterIconURL"]);
									})
									.AddField("Spectators", spectators)
									.AddField("Team 1", "<empty slot>\n<empty slot>\n<empty slot>\n<empty slot>\n<empty slot>\n<empty slot>", true)
									.AddField("Team 2", "<empty slot>\n<empty slot>\n<empty slot>\n<empty slot>\n<empty slot>\n<empty slot>", true);
					var embed = builder.Build();

					await ((await arg2.GetMessageAsync((ulong)messageID)) as IUserMessage).ModifyAsync(u => u.Embed = embed);
				}
			}
			

			//if (player == null)
			//{
			//	await arg2.SendMessageAsync("We could not set your role as you are currently not registered with our application. Please use the command '**!r p**' before setting your role.");
			//}

			//var message = await _dbContext.Messages.FindAsync(messageID);

			//if (message.Type == 1)
			//{
			//	switch (arg3.Emote.Name)
			//	{
			//		case "🛡":
			//			await arg2.SendMessageAsync("We have set your role to Tank. To change your role, ");
			//			break;
			//		case "⚔":
			//			await arg2.SendMessageAsync("We have set your role to DPS.");
			//			break;
			//		case "💉":
			//			await arg2.SendMessageAsync("We have set your role to Support.");
			//			break;
			//		default:
			//			break;
			//	}
			//	_dbContext.Messages.Remove(message);
			//	await _dbContext.SaveChangesAsync();

			//	await arg2.DeleteMessageAsync(arg3.MessageId);
			//}
		}

		private async Task OnReady()
		{
			Console.WriteLine("Bot is ready");
			//var user = _discord.GetUser(236922795781521410);
			//await user.SendMessageAsync("Bitch");
		}

		public async Task InitializeAsync(IServiceProvider provider)
		{
			_provider = provider;
			await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);
		}

		private async Task HandleCommandAsync(SocketMessage arg)
		{
			// Bail out if it's a System Message.
			var msg = arg as SocketUserMessage;
			if (msg == null)
				return;

			// Create a number to track where the prefix ends and the command begins
			int pos = 0;
			// Replace the '!' with whatever character
			// you want to prefix your commands with.
			// Uncomment the second half if you also want
			// commands to be invoked by mentioning the bot instead.
			if (msg.HasCharPrefix('!', ref pos) /* || msg.HasMentionPrefix(_client.CurrentUser, ref pos) */)
			{
				// Create a Command Context.
				var context = new SocketCommandContext(_discord, msg);

				// Execute the command. (result does not indicate a return value, 
				// rather an object stating if the command executed succesfully).
				var result = await _commands.ExecuteAsync(context, pos, _provider);

				// Uncomment the following lines if you want the bot
				// to send a message if it failed (not advised for most situations).
				//if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
				//    await msg.Channel.SendMessageAsync(result.ErrorReason);
			}
		}
	}
}
