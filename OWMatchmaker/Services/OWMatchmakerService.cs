﻿using OWMatchmaker.Handlers;
using OWMatchmaker.Models;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using OWMatchmaker.Models;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Extensions.Configuration;

namespace OWMatchmaker.Services
{
	public class OWMatchmakerService
	{
		private readonly DiscordSocketClient _discord;
		private readonly CommandService _commands;
		private IServiceProvider _provider;
		private readonly OWMatchmakerContext _dbContext;
		private readonly IConfiguration _config;

		public OWMatchmakerService(IServiceProvider provider, DiscordSocketClient discord, CommandService commands, OWMatchmakerContext dbContext, IConfiguration config)
		{
			_config = config;
			_dbContext = dbContext;
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
			var listOfMessages = await _dbContext.RegistrationMessages.ToListAsync();

			foreach (var message in listOfMessages)
			{
				TimeSpan span = DateTime.Now.Subtract(message.ExpiresIn.Value);
				if (span.TotalSeconds > 0)
				{
					var discordUser = _discord.GetUser((ulong)message.OwnerId);

					var getDMchannel = await discordUser.GetOrCreateDMChannelAsync();
					var getMessage = (await getDMchannel.GetMessageAsync((ulong)message.MessageId)) as IUserMessage;

					var builder = new EmbedBuilder()
										.WithTitle("Link Expired")
										.WithColor(new Color(0x9B4800))
										.WithFooter(footer => {
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

		private async Task OnReactionAdded(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
		{
			//if (arg3.User.Value.IsBot)
			//	return;
			
			//var messageID = (long)arg3.MessageId;
			//var userID = (long)arg3.UserId;
			//var player = await _dbContext.Players.FindAsync(userID);

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
