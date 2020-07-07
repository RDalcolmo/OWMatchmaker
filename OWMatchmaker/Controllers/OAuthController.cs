using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Birthday_Bot.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace OWMatchmaker.Controllers
{
	[Route("api/oauth")]
	[ApiController]
	public class OAuthController : ControllerBase
	{
		private readonly IConfiguration _config;
		private readonly OWMatchmakerContext _dbContext;

		public OAuthController(IConfiguration config, OWMatchmakerContext dbContext)
		{
			_dbContext = dbContext;
			_config = config;
		}

		// GET api/values/5
		[HttpGet]
		public async Task Get([FromQuery] string code, string state)
		{
			var tokenModel = await GetAccessToken(code);

			if (tokenModel == null)
				return;

			var userInfo = await GetUserInfo(tokenModel.access_token);
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
	}
}
