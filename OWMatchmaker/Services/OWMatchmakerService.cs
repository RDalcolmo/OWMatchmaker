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
			_discord.ReactionAdded += OnReactionAdded;
			RegistrationMessageTimeHandler.IntervalTimeElapsed += DateTimeHandler_IntervalTimeElapsed;
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
			var reactedUser = arg3.User.GetValueOrDefault();
			if (reactedUser.IsBot)
				return;

			var message = await arg1.GetOrDownloadAsync();

			//We only care if the reactions added are to the bot message.
			if (!(message.Author.Id == _discord.CurrentUser.Id))
				return;

			var messageID = (long)arg3.MessageId;
			var userID = (long)arg3.UserId;

			using (var _dbContext = new OWMatchmakerContext())
			{
				//Check if the message being reacted to is a lobby.
				//Get all the players in the lobby in order to build a spectator list.
				//Gets the players informations within the lobby.
				//Prevents reactions from being deleted where they shouldn't be.
				var lobby = await _dbContext.Lobbies.Include(p => p.Owner).Include(m => m.Matches).ThenInclude(p => p.Player).FirstOrDefaultAsync(u => u.LobbyId == messageID);
				var matches = lobby.Matches.FirstOrDefault(u => u.PlayerId == userID);

				if (lobby == null)
					return;

				//Remove all user reactions from a Lobby message.
				await message.RemoveReactionAsync(arg3.Emote, reactedUser);

				//Check if the lobby is full
				if (lobby.Matches.Count >= 24 && matches == null)
				{
					await reactedUser.SendMessageAsync("The lobby is full and it cannot accept more players, please wait for a player to dropout before joining again.");
					return;
				}

				//Checking if the player registered before attempting to join a lobby
				var player = await _dbContext.Players.FindAsync(userID);
				if (player == null)
				{
					await reactedUser.SendMessageAsync("We could not set your role as you do not have a BattleNet account connected to our service. Please go through the registration process. Type `!register` to begin.");
					return;
				}

				string spectators = "";
				string teamOne = "<empty slot>\n<empty slot>\n<empty slot>\n<empty slot>\n<empty slot>\n<empty slot>";
				string teamTwo = "<empty slot>\n<empty slot>\n<empty slot>\n<empty slot>\n<empty slot>\n<empty slot>";

				Role roleValue = new Role();
				foreach (var person in lobby.Matches)
				{
					roleValue = (Role)person.Role;

					if (person.Team == (short)Team.Spectator)
					{
						spectators += $"{person.Player.BattleTag} (SR: {person.Player.Sr}) [{roleValue}] | ";
					}
					else if (person.Team == (short)Team.TeamOne)
					{
						teamOne = teamOne.ReplaceFirst("<empty slot>", $"[{roleValue}] {person.Player.BattleTag} (SR: {person.Player.Sr})");
					}
					else if (person.Team == (short)Team.TeamTwo)
					{
						teamTwo = teamTwo.ReplaceFirst("<empty slot>", $"[{roleValue}] {person.Player.BattleTag} (SR: {person.Player.Sr})");
					}
				}
				

				int result = 0;

				switch (arg3.Emote.Name)
				{
					case "🛡":
						if (matches == null)
						{
							await _dbContext.Matches.AddAsync(new Matches() { LobbyId = messageID, PlayerId = userID, MatchesPlayed = 0, Role = (short)Role.Tank, Team = (short)Team.Spectator });
							spectators += $"{player.BattleTag} (SR: {player.Sr}) [Tank] | ";
						}
						else
						{
							matches.Role = (short)Role.Tank;
							_dbContext.Update(matches);

							if (matches.Team == (short)Team.Spectator)
								spectators = spectators.Replace($"{player.BattleTag} (SR: {player.Sr}) [{roleValue}] | ", $"{player.BattleTag} (SR: {player.Sr}) [Tank] | ");
							else if (matches.Team == (short)Team.TeamOne)
								teamOne = spectators.Replace($"[{roleValue}] {player.BattleTag} (SR: {player.Sr})", $"[Tank] {player.BattleTag} (SR: {player.Sr})");
							else if (matches.Team == (short)Team.TeamTwo)
								teamTwo = spectators.Replace($"[{roleValue}] {player.BattleTag} (SR: {player.Sr})", $"[Tank] {player.BattleTag} (SR: {player.Sr})");
						}			
						break;
					case "⚔":
						if (matches == null)
						{
							await _dbContext.Matches.AddAsync(new Matches() { LobbyId = messageID, PlayerId = userID, MatchesPlayed = 0, Role = (short)Role.DPS, Team = (short)Team.Spectator });
							spectators += $"{player.BattleTag} (SR: {player.Sr}) [DPS] | ";
						}
						else
						{
							matches.Role = (short)Role.DPS;
							_dbContext.Update(matches);

							if (matches.Team == (short)Team.Spectator)
								spectators = spectators.Replace($"{player.BattleTag} (SR: {player.Sr}) [{roleValue}] | ", $"{player.BattleTag} (SR: {player.Sr}) [DPS] | ");
							else if (matches.Team == (short)Team.TeamOne)
								teamOne = spectators.Replace($"[{roleValue}] {player.BattleTag} (SR: {player.Sr})", $"[DPS] {player.BattleTag} (SR: {player.Sr})");
							else if (matches.Team == (short)Team.TeamTwo)
								teamTwo = spectators.Replace($"[{roleValue}] {player.BattleTag} (SR: {player.Sr})", $"[DPS] {player.BattleTag} (SR: {player.Sr})");
						}
						break;
					case "💉":
						if (matches == null)
						{
							await _dbContext.Matches.AddAsync(new Matches() { LobbyId = messageID, PlayerId = userID, MatchesPlayed = 0, Role = (short)Role.Support, Team = (short)Team.Spectator });
							spectators += $"{player.BattleTag} (SR: {player.Sr}) [Support] | ";
						}
						else
						{
							matches.Role = (short)Role.Support;
							_dbContext.Update(matches);

							if (matches.Team == (short)Team.Spectator)
								spectators = spectators.Replace($"{player.BattleTag} (SR: {player.Sr}) [{roleValue}] | ", $"{player.BattleTag} (SR: {player.Sr}) [Support] | ");
							else if (matches.Team == (short)Team.TeamOne)
								teamOne = spectators.Replace($"[{roleValue}] {player.BattleTag} (SR: {player.Sr})", $"[Support] {player.BattleTag} (SR: {player.Sr})");
							else if (matches.Team == (short)Team.TeamTwo)
								teamTwo = spectators.Replace($"[{roleValue}] {player.BattleTag} (SR: {player.Sr})", $"[Support] {player.BattleTag} (SR: {player.Sr})");
						}	
						break;
					case "❌":
						if (matches == null)
						{
							//Do nothing?? The player technically isn't in the lobby to begin with.
							//However, we might want to play around with how we want to set the player's priority in getting a game, here.
						}
						else
						{
							if (lobby.Matches.Count <= 1)
							{
								spectators = "<empty>";
							}
							else
							{
								if (matches.Team == (short)Team.Spectator)
									spectators = spectators.Replace($"{player.BattleTag} (SR: {player.Sr}) [{roleValue}] | ", string.Empty);
								else if (matches.Team == (short)Team.TeamOne)
									teamOne = teamOne.Replace($"[{roleValue}] {player.BattleTag} (SR: {player.Sr})", "<empty slot>");
								else if (matches.Team == (short)Team.TeamTwo)
									teamTwo = teamTwo.Replace($"[{roleValue}] {player.BattleTag} (SR: {player.Sr})", "<empty slot>");
							}

							_dbContext.Remove(matches);
						}
						break;
					default:
						break;
				}

				result = await _dbContext.SaveChangesAsync();
				if (result > 0)
				{
					var builder = new EmbedBuilder()
									.WithTitle($"Lobby Owner: {lobby.Owner.BattleTag} | Slots Open: {24 - lobby.Matches.Count}")
									.WithDescription("React below to join: 🛡 Tanks, ⚔ DPS, 💉 Support, ❌ Leave Lobby.")
									.WithColor(new Color(0x9B4800))
									.WithFooter(footer => {
										footer
											.WithText("owmatcher.com")
											.WithIconUrl(_config["DiscordFooterIconURL"]);
									})
									.AddField("Spectators", spectators)
									.AddField("Team 1", teamOne, true)
									.AddField("Team 2", teamTwo, true);
					var embed = builder.Build();

					await message.ModifyAsync(u => u.Embed = embed);
				}
			}
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
