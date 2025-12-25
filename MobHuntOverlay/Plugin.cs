using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System;
using System.IO;
using System.Text.Json;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using MobHuntOverlay.Services;
using MobHuntOverlay.Models;
using MobHuntOverlay.Windows;

namespace MobHuntOverlay;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    private const string CommandName = "/mobhunt";

    public readonly WindowSystem WindowSystem = new("MobHuntOverlay");
    private DebugWindow DebugWindow { get; init; }
    private MapMarkerManager MapMarkerManager { get; init; }

    public Plugin()
    {
        MapMarkerManager = new MapMarkerManager(ClientState, Log);
        LoadMobLocationData();

        DebugWindow = new DebugWindow(ClientState, ObjectTable, DataManager, Log);
        WindowSystem.AddWindow(DebugWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open MobHuntOverlay debug window"
        });

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi += ToggleDebugWindow;

        Log.Information("MobHuntOverlay loaded successfully");
    }

    private void LoadMobLocationData()
    {
        try
        {
            var jsonPath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "Data", "MobLocations.json");
            if (!File.Exists(jsonPath))
            {
                Log.Error($"MobLocations.json not found at {jsonPath}");
                return;
            }

            var jsonContent = File.ReadAllText(jsonPath);
            var data = JsonSerializer.Deserialize<MobLocationData>(jsonContent);
            if (data != null)
            {
                MapMarkerManager.LoadMobLocationData(data);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load mob location data");
        }
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleDebugWindow;

        WindowSystem.RemoveAllWindows();
        DebugWindow.Dispose();
        MapMarkerManager.Dispose();

        CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        ToggleDebugWindow();
    }

    private void ToggleDebugWindow()
    {
        DebugWindow.Toggle();
    }
}
