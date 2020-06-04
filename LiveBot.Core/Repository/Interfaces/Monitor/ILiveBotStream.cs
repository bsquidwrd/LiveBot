using System;

namespace LiveBot.Core.Repository.Interfaces.Monitor
{
    /// <summary>
    /// Represents a generic Stream for use within the bot, usually returned by a Monitoring Service
    /// </summary>
    public interface ILiveBotStream : ILiveBotBase
    {
        public ILiveBotUser User { get; }
        public ILiveBotGame Game { get; }
        public string Id { get; }
        public string Title { get; }
        public DateTime StartTime { get; }
        public string ThumbnailURL { get; }

        /// <summary>
        /// Gets the stream URL
        /// </summary>
        /// <returns></returns>
        public string GetStreamURL();
    }
}