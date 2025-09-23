# Vintage Story - Configurable Room Size Mod

This mod allows you to configure maximum room sizes in Vintage Story, so you can create larger interiors and still be warm.

![cover](cover.jpg "Cover Image")

## How to use

Launch the game/server with the mod to generate config files. 
Then edit `ConfigurableRoomSize.json` in ModConfig folder in respective client/server game files (server needs restarting). 
When playing online config is synchronized with the server.

```[json]
{
  "MaxRoomSize": 24,
  "MaxCellarSize": 7,
  "AltMaxCellarSize": 9,
  "AltMaxCellarVolume": 150
}
```

That's all!

### How to Contribute

Fork the project and make pull request with your changes.

### Acknowledgments

- Thanks to [@BiggBenn](https://github.com/BiggBenn) for updating the mod to 1.21 and providing multiplayer sync.
