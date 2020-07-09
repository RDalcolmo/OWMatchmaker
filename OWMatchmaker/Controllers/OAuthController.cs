using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using OWMatchmaker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Discord.WebSocket;
using Discord;
using Microsoft.EntityFrameworkCore;

namespace OWMatchmaker.Controllers
{
	[Route("api/oauth")]
	[ApiController]
	public class OAuthController : ControllerBase
	{
		private readonly IConfiguration _config;
		private readonly DiscordSocketClient _discord;

		public OAuthController(IConfiguration config, DiscordSocketClient discord)
		{
			_discord = discord;
			_config = config;
		}

		[HttpGet("{id}")]
		public ActionResult<IEnumerable<string>> Get(int id)
		{
			return new string[] { "value1", "value2" };
		}

		// GET api/values/5
		[HttpGet]
		public async Task<IActionResult> Get([FromQuery] string code, ulong state)
		{
			using (var _dbContext = new OWMatchmakerContext())
			{
				var initializedMessage = await _dbContext.RegistrationMessages.FindAsync((long)state);

				if (initializedMessage == null)
					return BadRequest();

				var userId = initializedMessage.OwnerId;

				var tokenModel = await GetAccessToken(code);

				if (tokenModel == null)
					return BadRequest();

				var userInfo = await GetUserInfo(tokenModel.access_token);
				userInfo.battletag = userInfo.battletag.Replace("#", "-");

				var userStats = await GetOWUserRating(userInfo.battletag);

				var player = await _dbContext.Players.FindAsync(userId);

				if (player == null)
					await _dbContext.Players.AddAsync(new Players() { UserId = userId, BattleTag = userInfo.battletag, Sr = userStats.Rating });
				else
				{
					player.BattleTag = userInfo.battletag;
					player.Sr = userStats.Rating;
					_dbContext.Players.Update(player);
				}

				var result = await _dbContext.SaveChangesAsync();

				if (result > 0)
				{
					var discordUser = _discord.GetUser((ulong)userId);

					var getDMchannel = await discordUser.GetOrCreateDMChannelAsync();
					var getMessage = (await getDMchannel.GetMessageAsync((ulong)initializedMessage.MessageId)) as IUserMessage;

					var builder = new EmbedBuilder()
									.WithTitle("Registration Program")
									.WithDescription($"Registration Successful: {userInfo.battletag}")
									.WithColor(new Color(0x9B4800))
									.WithFooter(footer =>
									{
										footer
											.WithIconUrl(_config["DiscordFooterIconURL"])
											.WithText("owmatcher.com");
									})
									.AddField($"Username", $"{userInfo.battletag} (Default)", true)
									.AddField("SR", $"{userStats.Rating}\nIf you believe this value is incorrect:\n1) Verify your profile is set to public.\n2) Complete the current season's placements.\n3) Use the command `!refresh` to refresh your SR.", true);
					var embed = builder.Build();

					await getMessage.ModifyAsync(u => u.Embed = embed);

					await discordUser.SendMessageAsync("Registration complete!");

					_dbContext.RegistrationMessages.Remove(initializedMessage);
					await _dbContext.SaveChangesAsync();
				}
			}

			return Ok("Registration successful, you may close this window!");
		}

		public async Task<BlizzardAccessTokenModel> GetAccessToken(string code)
		{
			var authUrl = _config["BlizzardTokenURL"] + $"{code}&redirect_uri={_config["BlizzardRedirectURI"]}&scope=openid";
			using (var httpClient = new HttpClient())
			{
				httpClient.DefaultRequestHeaders.Add("client_id", _config["BlizzardClientID"]);
				httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _config["BlizzardAuthHeader"]);
				var response = await httpClient.PostAsync(authUrl, null);

				if (response.IsSuccessStatusCode)
				{
					var result = await response.Content.ReadAsStringAsync();

					return JsonConvert.DeserializeObject<BlizzardAccessTokenModel>(result);
				}
				else
				{
					return null;
				}
			}
		}

		public async Task<BlizzardUserInfoModel> GetUserInfo(string accessToken)
		{
			var authUrl = _config["BlizzardUserInfoURL"];
			using (var httpClient = new HttpClient())
			{
				httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
				var response = await httpClient.GetAsync(authUrl);

				if (response.IsSuccessStatusCode)
				{
					var result = await response.Content.ReadAsStringAsync();

					return JsonConvert.DeserializeObject<BlizzardUserInfoModel>(result);
				}
				else
				{
					return null;
				}
			}
		}

		public async Task<BlizzardUserStatsModel> GetOWUserRating(string battletag)
		{
			var authUrl = _config["OWUserInfoURL"] + $"{battletag}/profile";
			using (var httpClient = new HttpClient())
			{
				var response = await httpClient.GetAsync(authUrl);

				if (response.IsSuccessStatusCode)
				{
					var result = await response.Content.ReadAsStringAsync();

					return JsonConvert.DeserializeObject<BlizzardUserStatsModel>(result);
				}
				else
				{
					return null;
				}
			}
		}
	}
}
