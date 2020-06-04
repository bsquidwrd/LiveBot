using LiveBot.Core.Repository.Interfaces.Monitor;
using System.Globalization;

namespace LiveBot.Discord.Helpers
{
    public class FormatStreamMessage
    {
        public FormatStreamMessage()
        {
        }

        /// <summary>
        /// Formats a notification string with the necessary parameters
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public string GetNotificationMessage(ILiveBotStream stream, string message)
        {
            return message
                .Replace("{Name}", stream.User.DisplayName, ignoreCase: true, culture: CultureInfo.CurrentCulture)
                .Replace("{Username}", stream.User.DisplayName, ignoreCase: true, culture: CultureInfo.CurrentCulture)
                .Replace("{Game}", stream.Game.Name, ignoreCase: true, culture: CultureInfo.CurrentCulture)
                .Replace("{Title}", stream.Title, ignoreCase: true, culture: CultureInfo.CurrentCulture)
                .Replace("{URL}", stream.GetStreamURL(), ignoreCase: true, culture: CultureInfo.CurrentCulture);
        }
    }
}