using HarmonyLib;
using System.Reflection;

using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Config;
using Vintagestory.API.Common;

namespace ConfigurableRoomSize;

public class ConfigurableRoomSizeModSystem : ModSystem
{
    private Harmony? harmony;
    public override void Start(ICoreAPI api)
    {
        RoomSizeConfig.Load(api);                               // 1) read JSON
        harmony = new Harmony("configurableroomsize");          // 2) create patcher
        harmony.PatchAll(Assembly.GetExecutingAssembly());      // 3) apply patches
        Mod.Logger.Notification($"[ConfigurableRoomSize] Loaded | MaxRoomSize = {RoomSizeConfig.cfg.MaxRoomSize}, " +
                                $"MaxCellarSize = {RoomSizeConfig.cfg.MaxCellarSize}, " +
                                $"AltMaxCellarSize = {RoomSizeConfig.cfg.AltMaxCellarSize}, " +
                                $"AltMaxCellarVolume = {RoomSizeConfig.cfg.AltMaxCellarVolume}");
    }

    public override void Dispose()
    {
        harmony?.UnpatchAll("configurableroomsize");
    }

}
