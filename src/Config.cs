using System;
using System.Collections.Generic;
using System.Text;

namespace Whitelist;
public class PluginConfig
{
    public string WhitelistCommand { get; set; } = "wl";
    public string UnWhitelistCommand { get; set; } = "uwl";
    public string PermissionForCommands { get; set; } = "admin.ban";
}
