using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Plugins;
using SwiftlyS2.Shared.ProtobufDefinitions;

namespace Whitelist;

[PluginMetadata(Id = "Whitelist", Version = "1.2.0", Name = "Whitelist", Author = "verneri")]
public partial class Whitelist(ISwiftlyCore core) : BasePlugin(core) {

    private PluginConfig _config = null!;
    private HashSet<string> _whitelist = new();
    private string WhitelistFilePath => Path.Combine(Core.PluginPath, "whitelist.txt");

    public override void Load(bool hotReload)
    {
        const string ConfigFileName = "config.jsonc";
        const string ConfigSection = "Whitelist";
        Core.Configuration
            .InitializeJsonWithModel<PluginConfig>(ConfigFileName, ConfigSection)
            .Configure(cfg => cfg.AddJsonFile(
                Core.Configuration.GetConfigPath(ConfigFileName),
                optional: false,
                reloadOnChange: true));

        ServiceCollection services = new();
        services.AddSwiftly(Core)
            .AddOptionsWithValidateOnStart<PluginConfig>()
            .BindConfiguration(ConfigSection);
        var provider = services.BuildServiceProvider();
        _config = provider.GetRequiredService<IOptions<PluginConfig>>().Value;

        Core.Event.OnMapLoad += OnMapLoad;

        Core.GameEvent.HookPost<EventPlayerConnectFull>(OnPlayerConnectFull);

        Core.Command.RegisterCommand($"{_config.AddCommand}", OnWlcommand, false, $"{_config.PermissionForCommands}");
        Core.Command.RegisterCommand($"{_config.RemoveCommand}", OnUwlcommand, false, $"{_config.PermissionForCommands}");

        if (_config.Mode != 1 && _config.Mode != 2)
        {
            Core.Logger.LogCritical("Config.Mode is invalid. Please use 1 for whitelist and 2 for blacklisting.");
        }
    }

    public override void Unload() {

    }
    private void OnMapLoad(IOnMapLoadEvent @event)
    {
        LoadWhitelist();
    }
    private void LoadWhitelist()
    {
        _whitelist.Clear();
        if (!File.Exists(WhitelistFilePath))
        {
            File.WriteAllText(WhitelistFilePath, "");
            Core.Logger.LogInformation($"Whitelist file not found. Created the file at '{WhitelistFilePath}'.");
            return;
        }

        var lines = File.ReadAllLines(WhitelistFilePath);
        int count = 0;
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                _whitelist.Add(trimmed);
                count++;
            }
        }
        Core.Logger.LogInformation($"Loaded {count} SteamID(s) from whitelist.");
    }
    private void SaveWhitelist()
    {
        File.WriteAllLines(WhitelistFilePath, _whitelist);
    }
    private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event)
    {
        if (@event == null)
            return HookResult.Continue;
        var player = @event.Accessor.GetPlayer("userid");
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        var steamId = player.SteamID.ToString();

        if (_config.Mode == 1)
        {
            if (!_whitelist.Contains(steamId))
            {
                player.Kick("You are not whitelisted.", ENetworkDisconnectionReason.NETWORK_DISCONNECT_REJECT_RESERVED_FOR_LOBBY);
            }

        }
        else if (_config.Mode == 2) 
        {
            if (_whitelist.Contains(steamId))
            {
                player.Kick("You are not whitelisted.", ENetworkDisconnectionReason.NETWORK_DISCONNECT_REJECT_RESERVED_FOR_LOBBY);
            }
        }

            return HookResult.Continue;
    }

    private void OnWlcommand(ICommandContext context)
    {
        if (context.Args.Length < 1)
        {
            context.Reply(Core.Localizer["usage.hint", _config.AddCommand]);
            return;
        }

        var steamId = context.Args[0];

        if (steamId.Length != 17)
        {
            context.Reply(Core.Localizer["invalid.steamid"]);
            return;
        }
        if (_whitelist.Add(steamId))
        {
            SaveWhitelist();
            context.Reply(Core.Localizer["whitelist.added", steamId]);
        }
        else
        {
            context.Reply(Core.Localizer["whitelist.contains", steamId]);
        }
    }

    private void OnUwlcommand(ICommandContext context)
    {
        if (context.Args.Length < 1)
        {
            context.Reply(Core.Localizer["usage.hint", _config.RemoveCommand]);
            return;
        }
        var steamId = context.Args[0];

        if (steamId.Length != 17)
        {
            context.Reply(Core.Localizer["invalid.steamid"]);
            return;
        }

        if (_whitelist.Remove(steamId))
        {
            SaveWhitelist();
            context.Reply(Core.Localizer["whitelist.removed", steamId]);
        }
        else
        {
            context.Reply(Core.Localizer["whitelist.notfound", steamId]);
        }
    }
}