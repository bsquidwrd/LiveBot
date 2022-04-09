namespace LiveBot.Core.Repository.Static
{
    /// <summary>
    /// Represents a Monitoring Service
    /// </summary>
    public enum ServiceEnum
    {
        Twitch = 1,
    }

    public static class ServiceUtils
    {
        public static uint GetAlertColor(this ServiceEnum serviceEnum) =>
            serviceEnum switch
            {
                ServiceEnum.Twitch => 0x9146FF,
                _ => 0xFFFFFF,
            };
    }
}