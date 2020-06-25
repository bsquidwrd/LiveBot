using Discord.WebSocket;
using LiveBot.API.Helpers;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Models.Discord;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LiveBot.API.Controllers
{
    public class GuildsController : Controller
    {
        private IUnitOfWork _work;
        private DiscordShardedClient _client;

        public GuildsController(IUnitOfWorkFactory factory, DiscordShardedClient client)
        {
            _work = factory.Create();
            _client = client;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!(User?.Identity?.IsAuthenticated ?? false))
            {
                return Redirect("/");
            }

            var guildsUserCanManage = await DiscordHelper.GetGuildsUserCanManage(HttpContext);
            List<DiscordGuild> discordGuilds = new List<DiscordGuild>();

            foreach (var guild in guildsUserCanManage)
            {
                var discordGuild = await _work.GuildRepository.SingleOrDefaultAsync(i => i.DiscordId == guild.Id);
                if (discordGuild != null)
                    discordGuilds.Add(discordGuild);
            }
            return View(model: discordGuilds);
        }

        // GET <GuildsController>/5
        [HttpGet("[controller]/{id}")]
        public async Task<IActionResult> Subscriptions(ulong id)
        {
            if (!(User?.Identity?.IsAuthenticated ?? false))
            {
                return Redirect("/");
            }

            var guildsUserCanManage = await DiscordHelper.GetGuildsUserCanManage(HttpContext, i => i.Id == id);
            var guildUserCanManage = guildsUserCanManage.FirstOrDefault();
            var discordGuild = await _work.GuildRepository.SingleOrDefaultAsync(i => i.DiscordId == id);

            if (guildUserCanManage == null || discordGuild == null)
                return new NotFoundResult();

            return View(model: discordGuild);
        }
    }
}