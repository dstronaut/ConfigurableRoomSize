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
    public override void StartClientSide(ICoreClientAPI api)
    {
        int valueFromServer = api.World.Config.GetInt("ServerSideValue", 0);
        RoomSizeConfig.cfg.MaxRoomSize = api.World.Config.GetInt("configurableroomsize.MaxRoomSize", RoomSizeConfig.cfg.MaxRoomSize);
        RoomSizeConfig.cfg.MaxCellarSize = api.World.Config.GetInt("configurableroomsize.MaxCellarSize", RoomSizeConfig.cfg.MaxCellarSize);
        RoomSizeConfig.cfg.AltMaxCellarSize = api.World.Config.GetInt("configurableroomsize.AltMaxCellarSize", RoomSizeConfig.cfg.AltMaxCellarSize);
        RoomSizeConfig.cfg.AltMaxCellarVolume = api.World.Config.GetInt("configurableroomsize.AltMaxCellarVolume", RoomSizeConfig.cfg.AltMaxCellarVolume);
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        api.World.Config.SetInt("configurableroomsize.MaxRoomSize", RoomSizeConfig.cfg.MaxRoomSize);
        api.World.Config.SetInt("configurableroomsize.MaxCellarSize", RoomSizeConfig.cfg.MaxCellarSize);
        api.World.Config.SetInt("configurableroomsize.AltMaxCellarSize", RoomSizeConfig.cfg.AltMaxCellarSize);
        api.World.Config.SetInt("configurableroomsize.AltMaxCellarVolume", RoomSizeConfig.cfg.AltMaxCellarVolume);
    }
}
