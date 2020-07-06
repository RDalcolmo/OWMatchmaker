using System.Threading.Tasks;
using OWMatchmaker.Models;

namespace OWMatchmaker.Handlers
{
	public interface IAPIHandler
	{
		Task CreateMessage(MessageModel message, long channelID);
		Task<bool> IsInGuild(long guildId, long userId);
	}
}