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

namespace OWMatchmaker.Controllers
{
	[Route("api/oauth")]
	[ApiController]
	public class OAuthController : ControllerBase
	{
		private readonly IConfiguration _config;
		private readonly OWMatchmakerContext _dbContext;
		private readonly DiscordSocketClient _discord;

		public OAuthController(IConfiguration config, OWMatchmakerContext dbContext, DiscordSocketClient discord)
		{
			_discord = discord;
			_dbContext = dbContext;
			_config = config;
		}

		// GET api/values/5
		[HttpGet]
		public async Task<IActionResult> Get([FromQuery] string code, ulong state)
		{
			var tokenModel = await GetAccessToken(code);

			if (tokenModel == null)
				return BadRequest();

			var userInfo = await GetUserInfo(tokenModel.access_token);
			userInfo.battletag = userInfo.battletag.Replace("#", "-");

			var userStats = await GetOWUserRating(userInfo.battletag);

			await _dbContext.Players.AddAsync(new Players() { UserId = (long)state, BattleTag = userInfo.battletag, Sr = userStats.Rating });
			var result = await _dbContext.SaveChangesAsync();

			if (result > 0)
			{
				var discordUser = _discord.GetUser(state);
				await discordUser.SendMessageAsync("Registration complete!");
			}

			return Ok();
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
