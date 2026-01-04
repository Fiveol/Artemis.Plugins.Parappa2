using Artemis.Core;
using Artemis.Core.Modules;
using Artemis.Core.Services;
using System;
using System.Collections.Generic;

public class Parappa2Module : Module<Parappa2DataModel>
{
    private readonly IPineService _pine;

    // Rank address from CE table:
    private const long RankAddress = 0x2018931C;

    public override List<IModuleActivationRequirement> ActivationRequirements => null;

    public Parappa2Module(IPineService pine)
    {
        _pine = pine;
    }

    public override void Enable()
    {
        // Poll memory every 50ms
        AddTimedUpdate(TimeSpan.FromMilliseconds(50), UpdateMemory);
    }

    public override void Disable()
    {
    }

    private void UpdateMemory(double deltaTime)
    {
        try
        {
            // Read rank level (0–12)
            byte rank = _pine.ReadByte(RankAddress);
            DataModel.RankLevel = rank;
        }
        catch
        {
            // Ignore read errors (PCSX2 not running, etc.)
        }
    }

    // Optional: allow writing rank from other code
    public void SetRank(byte value)
    {
        try
        {
            _pine.WriteByte(RankAddress, value);
        }
        catch
        {
        }
    }
}