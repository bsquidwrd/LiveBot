using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Interfaces.Streams;
using LiveBot.Core.Repository.Models.Streams;
using LiveBot.Core.Repository.Static;
using Microsoft.EntityFrameworkCore;

namespace LiveBot.Repository.Streams
{
    /// <inheritdoc/>
    public class UserRepository : ModelRepository<StreamUser>, IUserRepository
    {
        /// <inheritdoc/>
        public UserRepository(LiveBotDBContext context) : base(context)
        {
        }

        /// <inheritdoc/>
        public async Task UpdateProfilesAsync(ServiceEnum serviceType, IReadOnlyCollection<ILiveBotUser> profiles)
        {
            if (profiles.Count == 0)
                return;

            // Last one wins if the same id appears twice, so the dictionary build cannot throw.
            Dictionary<string, ILiveBotUser> profilesById = profiles
                .GroupBy(p => p.Id)
                .ToDictionary(g => g.Key, g => g.Last());
            List<string> ids = profilesById.Keys.ToList();

            List<StreamUser> streamUsers = await DbSet
                .Where(u => u.Deleted == false && u.ServiceType == serviceType && ids.Contains(u.SourceID))
                .ToListAsync()
                .ConfigureAwait(false);

            foreach (StreamUser streamUser in streamUsers)
            {
                ILiveBotUser profile = profilesById[streamUser.SourceID];
                streamUser.Username = profile.Username;
                streamUser.DisplayName = profile.DisplayName;
                streamUser.AvatarURL = profile.AvatarURL;
                streamUser.ProfileURL = profile.ProfileURL;
            }

            try
            {
                // SaveChanges only writes entities whose values actually changed.
                await Context.SaveChangesAsync().ConfigureAwait(false);
            }
            finally
            {
                // The caller reuses one context across batches; a failed save must not be retried by the next batch.
                Context.ChangeTracker.Clear();
            }
        }
    }
}