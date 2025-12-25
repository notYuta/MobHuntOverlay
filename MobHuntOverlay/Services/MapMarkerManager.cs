using System;
using System.Numerics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using MobHuntOverlay.Models;

namespace MobHuntOverlay.Services;

public unsafe class MapMarkerManager : IDisposable
{
    private readonly IClientState clientState;
    private readonly IPluginLog log;

    private MobLocationData? mobLocationData;
    private TerritoryData? currentTerritoryData;
    private ushort lastTerritoryId;

    // マーカーアイコンID (60561 = 赤い丸マーカー)
    private const uint MarkerIconId = 60561;

    public MapMarkerManager(IClientState clientState, IPluginLog log)
    {
        this.clientState = clientState;
        this.log = log;

        this.clientState.TerritoryChanged += OnTerritoryChanged;
    }

    public void LoadMobLocationData(MobLocationData data)
    {
        mobLocationData = data;
        log.Information($"Loaded mob location data version {data.Version}");

        // 初回ロード時に現在のテリトリーをチェック
        UpdateCurrentTerritory(clientState.TerritoryType);
    }

    private void OnTerritoryChanged(ushort territoryId)
    {
        UpdateCurrentTerritory(territoryId);
    }

    private void UpdateCurrentTerritory(ushort territoryId)
    {
        if (mobLocationData == null) return;
        if (lastTerritoryId == territoryId) return;

        lastTerritoryId = territoryId;
        currentTerritoryData = mobLocationData.Data.Find(t => t.TerritoryTypeId == territoryId);

        if (currentTerritoryData != null)
        {
            log.Information($"Territory changed to {currentTerritoryData.InternalName}, adding markers...");
            AddMarkersForCurrentTerritory();
        }
        else
        {
            log.Debug($"No mob data for territory {territoryId}");
        }
    }

    private void AddMarkersForCurrentTerritory()
    {
        if (currentTerritoryData == null) return;

        var agentMap = AgentMap.Instance();
        if (agentMap == null) return;

        foreach (var mob in currentTerritoryData.Mobs)
        {
            foreach (var location in mob.Locations)
            {
                // ワールド座標を直接使用
                var worldPos = new Vector3(location.X, location.Y, location.Z);

                // マーカーを追加（マップとミニマップ両方）
                if (!agentMap->AddMapMarker(worldPos, MarkerIconId, scale: 0))
                {
                    log.Warning($"Failed to add map marker for {mob.MobName} at ({location.X}, {location.Y}, {location.Z})");
                }
                else
                {
                    log.Debug($"Added marker for {mob.MobName} at ({location.X}, {location.Y}, {location.Z})");
                }

                // ミニマップにもマーカーを追加
                if (!agentMap->AddMiniMapMarker(worldPos, MarkerIconId, scale: 0))
                {
                    log.Warning($"Failed to add minimap marker for {mob.MobName}");
                }
            }
        }
    }

    /// <summary>
    /// 手動でマーカーを再追加（マップを開いた時などに呼び出し可能）
    /// </summary>
    public void RefreshMarkers()
    {
        AddMarkersForCurrentTerritory();
    }

    public void Dispose()
    {
        clientState.TerritoryChanged -= OnTerritoryChanged;
    }
}
