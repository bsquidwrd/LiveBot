using LiveBot.Core.Repository.Static;

namespace LiveBot.Core.Repository.Models.Streams
{
    public class StreamGame : BaseModel<StreamGame>
    {
        public ServiceEnum ServiceType { get; set; }
        public string SourceId { get; set; }
        public string Name { get; set; }
        public string ThumbnailURL { get; set; }
    }
}