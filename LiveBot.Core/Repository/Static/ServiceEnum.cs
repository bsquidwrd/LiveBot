namespace LiveBot.Core.Repository.Static
{
    /// <summary>
    /// Represents a Monitoring Service
    /// </summary>
    public enum ServiceEnum
    {
        Twitch = 1
    }

    public static class ServiceUtils
    {
        public static uint GetAlertColor(this ServiceEnum serviceEnum)
        {
            // Default Twitch Purple because it's pretty
            uint HexColor = 0x9146FF;
            switch (serviceEnum)
            {
                case ServiceEnum.Twitch:
                    HexColor = 0x9146FF;
                    break;

                default:
                    break;
            }
            return HexColor;
        }
    }
}