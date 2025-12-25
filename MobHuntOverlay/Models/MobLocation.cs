using System.Collections.Generic;

namespace MobHuntOverlay.Models;

public class MobLocationData
{
    public string Version { get; set; } = string.Empty;
    public List<TerritoryData> Data { get; set; } = new();
}

public class TerritoryData
{
    public uint TerritoryTypeId { get; set; }
    public string InternalName { get; set; } = string.Empty;
    public List<MobData> Mobs { get; set; } = new();
}

public class MobData
{
    public uint BNpcNameId { get; set; }
    public string MobName { get; set; } = string.Empty;
    public string Rank { get; set; } = string.Empty;
    public List<Position> Locations { get; set; } = new();
}

public class Position
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
}
