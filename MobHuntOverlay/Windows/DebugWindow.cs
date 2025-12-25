using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;

namespace MobHuntOverlay.Windows;

public unsafe class DebugWindow : Window, IDisposable
{
    private readonly IClientState clientState;
    private readonly IObjectTable objectTable;
    private readonly IDataManager dataManager;
    private readonly IPluginLog log;

    private const uint MarkerIconId = 60561;

    public DebugWindow(IClientState clientState, IObjectTable objectTable, IDataManager dataManager, IPluginLog log)
        : base("MobHuntOverlay Debug###MobHuntDebug")
    {
        this.clientState = clientState;
        this.objectTable = objectTable;
        this.dataManager = dataManager;
        this.log = log;

        Size = new Vector2(400, 350);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var player = objectTable.LocalPlayer;
        if (player == null)
        {
            ImGui.Text("Player not available");
            return;
        }

        // === テリトリー・マップ情報 ===
        ImGui.TextColored(new Vector4(1, 1, 0, 1), "=== Territory / Map Info ===");
        
        var territoryId = clientState.TerritoryType;
        var mapId = clientState.MapId;
        
        ImGui.Text($"TerritoryTypeId: {territoryId}");
        ImGui.Text($"MapId: {mapId}");
        
        string mapName = "Unknown";
        ushort sizeFactor = 100;
        short offsetX = 0;
        short offsetY = 0;
        
        if (dataManager.GetExcelSheet<Map>().TryGetRow(mapId, out var mapRow))
        {
            mapName = mapRow.PlaceName.ValueNullable?.Name.ToString() ?? "Unknown";
            sizeFactor = mapRow.SizeFactor;
            offsetX = mapRow.OffsetX;
            offsetY = mapRow.OffsetY;
            
            ImGui.Text($"Map Name: {mapName}");
            ImGui.Text($"SizeFactor: {sizeFactor}, OffsetX: {offsetX}, OffsetY: {offsetY}");
        }
        
        ImGui.Separator();
        
        // === プレイヤー座標 ===
        ImGui.TextColored(new Vector4(0, 1, 1, 1), "=== Player Position ===");
        
        var pos = player.Position;
        ImGui.Text($"World Position:");
        ImGui.Text($"  X: {pos.X:F2}");
        ImGui.Text($"  Y: {pos.Y:F2}");
        ImGui.Text($"  Z: {pos.Z:F2}");
        
        // マップ座標も表示
        if (sizeFactor > 0)
        {
            var mapCoord = WorldToMapCoord(pos, sizeFactor, offsetX, offsetY);
            ImGui.Text($"Map Coord: ({mapCoord.X:F1}, {mapCoord.Y:F1})");
        }
        
        ImGui.Separator();
        
        // === コピーボタン ===
        ImGui.TextColored(new Vector4(0, 1, 0, 1), "=== Copy to Clipboard ===");
        
        // JSON形式でコピー
        if (ImGui.Button("Copy Position (JSON)"))
        {
            var json = $"{{ \"X\": {pos.X:F2}, \"Y\": {pos.Y:F2}, \"Z\": {pos.Z:F2} }}";
            ImGui.SetClipboardText(json);
            log.Information($"Copied to clipboard: {json}");
        }
        
        ImGui.SameLine();
        
        // TerritoryTypeIdもコピー
        if (ImGui.Button("Copy TerritoryTypeId"))
        {
            ImGui.SetClipboardText(territoryId.ToString());
            log.Information($"Copied TerritoryTypeId: {territoryId}");
        }
        
        ImGui.Separator();
        
        // === マーカー操作 ===
        ImGui.TextColored(new Vector4(1, 0.5f, 0, 1), "=== Marker Controls ===");
        
        // マーカー追加ボタン
        if (ImGui.Button("Add Marker at Current Position"))
        {
            AddMarkerAtPlayerPosition();
        }

        ImGui.SameLine();

        // マーカークリアボタン
        if (ImGui.Button("Clear All Markers"))
        {
            ClearAllMarkers();
        }
    }

    private void AddMarkerAtPlayerPosition()
    {
        var player = objectTable.LocalPlayer;
        if (player == null) return;

        var agentMap = AgentMap.Instance();
        if (agentMap == null) return;

        var pos = player.Position;

        // AddMapMarkerはワールド座標を受け取る
        if (agentMap->AddMapMarker(pos, MarkerIconId))
        {
            log.Information($"Added marker at world pos: ({pos.X:F2}, {pos.Y:F2}, {pos.Z:F2})");
        }
        else
        {
            log.Warning("Failed to add marker");
        }
    }

    private void ClearAllMarkers()
    {
        var agentMap = AgentMap.Instance();
        if (agentMap == null) return;

        agentMap->ResetMapMarkers();
        agentMap->ResetMiniMapMarkers();
        log.Information("Cleared all markers");
    }

    /// <summary>
    /// ワールド座標をマップ座標に変換（表示用）
    /// </summary>
    private Vector2 WorldToMapCoord(Vector3 worldPos, ushort sizeFactor, short offsetX, short offsetY)
    {
        var scale = sizeFactor / 100.0f;
        var mapX = ((worldPos.X + offsetX) * scale / 50.0f) + 21.0f;
        var mapY = ((worldPos.Z + offsetY) * scale / 50.0f) + 21.0f;
        return new Vector2(mapX, mapY);
    }
}
