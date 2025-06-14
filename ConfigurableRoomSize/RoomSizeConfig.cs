using Vintagestory.API.Common;

namespace ConfigurableRoomSize;

/// <summary>User‑editable JSON config loaded at startup.</summary>
public class RoomSizeConfigData
{
  public int MaxRoomSize = 14;
  public int MaxCellarSize = 7;
  public int AltMaxCellarSize = 9;
  public int AltMaxCellarVolume = 150;
}

public static class RoomSizeConfig
{
  public static RoomSizeConfigData cfg;
  
  public static void Load(ICoreAPI api)
  {
    cfg = api.LoadModConfig<RoomSizeConfigData>("ConfigurableRoomSize.json")
              ?? new RoomSizeConfigData();
    api.StoreModConfig(cfg, "ConfigurableRoomSize.json");
  }
}
