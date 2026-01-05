using Artemis.Core;
using Artemis.Core.Modules;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Artemis.Plugins.Parappa2
{
    [PluginFeature(Name = "Parappa2")]
    public class Parappa2Module : Module<Parappa2DataModel>
    {
        private readonly ILogger _logger;

        private static readonly string[] Pcsx2ProcessNames =
        {
            "pcsx2",
            "pcsx2-qt",
            "pcsx2-stable",
            "pcsx2-nightly",
            "pcsx2-parappa"
        };

        public Parappa2Module(ILogger logger)
        {
            _logger = logger;
        }

        public override List<IModuleActivationRequirement> ActivationRequirements => null;

        public override void Enable()
        {
            AddTimedUpdate(TimeSpan.FromSeconds(1), _ => PollPcsx2(), "PollPCSX2");
            _logger.Information("Parappa2Module enabled, polling PCSX2 every 1s");
        }

        public override void Disable()
        {
            _logger.Information("Parappa2Module disabled");
            DataModel.IsConnectedToPine = false;
            DataModel.IsPTR2Running = false;
            DataModel.GameId = string.Empty;
            DataModel.GameTitle = string.Empty;
            DataModel.Rank = string.Empty;
        }

        public override void Update(double deltaTime)
        {
            // No per-frame work, handled by timed update
        }

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

            DataModel.IsConnectedToPine = pcsx2Running;

            if (pcsx2Running)
            {
                try
                {
                    using var pineClient = new PineClient(_logger, "127.0.0.1", 28011);
                    var gameId = pineClient.GetGameId();
                    var title = pineClient.GetTitle();

                    DataModel.GameId = gameId;
                    DataModel.GameTitle = title;
                    DataModel.IsPTR2Running = string.Equals(gameId, "SCUS-97167", StringComparison.OrdinalIgnoreCase);

                    // Read Rank from EE RAM address 0x18931C
                    byte rankValue = pineClient.Read8(0x18931C);
                    DataModel.Rank = rankValue.ToString("X2");

                    _logger.Debug("Poll result: GameId={GameId}, Title={Title}, Rank={Rank}, IsPTR2Running={IsPTR2Running}",
                        gameId, title, DataModel.Rank, DataModel.IsPTR2Running);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to query PINE during poll");
                    DataModel.GameId = string.Empty;
                    DataModel.GameTitle = string.Empty;
                    DataModel.Rank = string.Empty;
                    DataModel.IsPTR2Running = false;
                }
            }
            else
            {
                DataModel.GameId = string.Empty;
                DataModel.GameTitle = string.Empty;
                DataModel.Rank = string.Empty;
                DataModel.IsPTR2Running = false;
            }
        }
    }
}