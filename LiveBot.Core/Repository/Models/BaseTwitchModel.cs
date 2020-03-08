namespace LiveBot.Core.Repository.Models
{
    public abstract class BaseTwitchModel<T> : BaseModel<T>
        where T : BaseTwitchModel<T>
    {
        public ulong TwitchId { get; set; }
        public string Name { get; set; }
    }
}