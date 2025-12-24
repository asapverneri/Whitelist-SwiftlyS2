using System;
using System.Collections.Generic;
using System.Text;

namespace Whitelist;
public class PluginConfig
{
    public int Mode { get; set; } = 1;
    public string AddCommand { get; set; } = "wl";
    public string RemoveCommand { get; set; } = "uwl";
    public string PermissionForCommands { get; set; } = "admin.ban";
}
