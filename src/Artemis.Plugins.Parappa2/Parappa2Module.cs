using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;              // <-- REQUIRED
using System.Runtime.InteropServices;
using Artemis.Core;
using Artemis.Core.Modules;
using Serilog;

namespace Artemis.Plugins.Parappa2
{
    public class Parappa2Module : Module<Parappa2DataModel>
    {
        private readonly ILogger _logger;
        private Process? _pcsx2;

        private const long RankAddress = 0x2018931C;

        public Parappa2Module(ILogger logger)
        {
            _logger = logger;
        }

        public override List<IModuleActivationRequirement> ActivationRequirements => new();

        public override void Enable()
        {
            _logger.Information("Parappa2Module enabled");
        }

        public override void Disable()
        {
            _pcsx2 = null;
            DataModel.IsAttached = false;
        }

        public override void Update(double deltaTime)
        {
            try
            {
                // Find PCSX2 if not attached
                if (_pcsx2 == null || _pcsx2.HasExited)
                {
                    _pcsx2 = Process.GetProcessesByName("pcsx2").FirstOrDefault();
                    DataModel.IsAttached = _pcsx2 != null;

                    if (_pcsx2 == null)
                        return;
                }

                // Read rank byte
                byte[] buffer = new byte[1];
                if (ReadProcessMemory(_pcsx2.Handle, (IntPtr)RankAddress, buffer, 1, out _))
                {
                    DataModel.RankLevel = buffer[0];
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error updating Parappa2Module");
            }
        }

        // WinAPI memory reader
        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            byte[] lpBuffer,
            int dwSize,
            out IntPtr lpNumberOfBytesRead);
    }
}