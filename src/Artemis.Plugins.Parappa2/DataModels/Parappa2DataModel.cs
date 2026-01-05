using Artemis.Core.Modules;

namespace Artemis.Plugins.Parappa2
{
    public class Parappa2DataModel : DataModel
    {
        public bool IsConnectedToPine { get; set; }
        public bool IsPTR2Running { get; set; }
        public string GameId { get; set; } = string.Empty;
        public string GameTitle { get; set; } = string.Empty;
        public string Rank { get; set; } = string.Empty;
    }
}