using Artemis.Core.Modules;

namespace Artemis.Plugins.Parappa2
{
    public class Parappa2DataModel : DataModel
    {
        public string GameId { get; set; } = string.Empty;
        public string Rank { get; set; } = string.Empty;
        public byte Score { get; set; } = 0;
    }
}