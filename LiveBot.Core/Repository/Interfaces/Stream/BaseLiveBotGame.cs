namespace LiveBot.Core.Repository.Interfaces.Stream
{
    public abstract class BaseLiveBotGame : ILiveBotGame
    {
        public BaseLiveBotGame()
        {
        }

        public string Id { get; set; }

        public string Name { get; set; }

        public string Image { get; set; }

        public override string ToString()
        {
            return $@"{Name}";
        }
    }
}