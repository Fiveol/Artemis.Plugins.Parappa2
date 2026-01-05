using Artemis.Core;
using Artemis.Core.Modules;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Artemis.Plugins.Parappa2
{
    [PluginFeature(Name = "Parappa2")]
    public class Parappa2Module : Module<Parappa2DataModel>
    {
        private static readonly string[] Pcsx2ProcessNames =
        {
            "pcsx2",
            "pcsx2-qt",
            "pcsx2-stable",
            "pcsx2-nightly",
            "pcsx2-parappa"
        };

        public override List<IModuleActivationRequirement> ActivationRequirements => new();

        public override void Enable()
        {
            // Poll every 100 milliseconds
            AddTimedUpdate(TimeSpan.FromMilliseconds(100), _ => PollPcsx2(), "PollPCSX2");
        }

        public override void Disable()
        {
            DataModel.GameId = string.Empty;
            DataModel.Rank = string.Empty;
            DataModel.Score = 0;
        }

        public override void Update(double deltaTime) { }

        private void PollPcsx2()
        {
            bool pcsx2Running = false;
            foreach (var name in Pcsx2ProcessNames)
            {
                if (Process.GetProcessesByName(name).Length > 0)
                {
                    pcsx2Running = true;
                    break;
                }
            }

            if (pcsx2Running)
            {
                try
                {
                    using var pineClient = new PineClient("127.0.0.1", 28011);
                    DataModel.GameId = pineClient.GetGameId();
                    DataModel.Rank = pineClient.Read8(0x18931C).ToString("X2");
                    DataModel.Score = pineClient.Read8(0x189338);
                }
                catch
                {
                    DataModel.GameId = string.Empty;
                    DataModel.Rank = string.Empty;
                    DataModel.Score = 0;
                }
            }
            else
            {
                DataModel.GameId = string.Empty;
                DataModel.Rank = string.Empty;
                DataModel.Score = 0;
            }
        }
    }
}