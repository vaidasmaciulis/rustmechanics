Blocks will slowly rust over time while in atmosphere of configured planets.

All blocks that is not covered by other blocks or airtight spaces in all directions will be affected by rust.

By default Earth, Alien, Pertam and Venus (that is any modded planet that has "Venus" in it's name) planets are configured.

Grids inside SafeZone that has damage disabled will not rust.

Rusting of powered grids can be disabled in config.

Rusting of specific block types can be disabled in config.

Server side only scripts! (Should work on Xbox dedicated servers, but not tested)

!!WARNING!! This mod is still somewhat experimental. Bugs are expected, please report if encountered. Also careful adding this to your already build world without backup saves.


[h2]F.A.Q[/h2]

- How to repair rusted block?

Wield it up and paint. Only texture needs to be applied to fix rust.

- Does weather affect rusting?

No. Weather effects are not implemented, but it's something to think about for the future updates.


[h2]Configuration[/h2]

This mod can be configured per save game:

Create and save game with this mod added.
Open Storage directory of your save, i.e.: 
[code]C:\Users\<User Name>\AppData\Roaming\SpaceEngineers\Saves\<Some number>\<Save Game name>\Storage\<Some number>.sbm_RustMechanics
[/code]
config1.2.xml file should be inside. (make sure to edit the latest version if there is more than one)
Open it with Notepad or other text editor

You will see planets config:
[code]  <OnlyRustUnpoweredGrids>false</OnlyRustUnpoweredGrids>
  <Planets>
    <Planet>
      <PlanetNameContains>Earth</PlanetNameContains>
      <AverageMinutesToStartRusting>300</AverageMinutesToStartRusting>
    </Planet>
    <Planet>
      <PlanetNameContains>Alien</PlanetNameContains>
      <AverageMinutesToStartRusting>180</AverageMinutesToStartRusting>
    </Planet>
    <Planet>
      <PlanetNameContains>Pertam</PlanetNameContains>
      <AverageMinutesToStartRusting>60</AverageMinutesToStartRusting>
    </Planet>
    <Planet>
      <PlanetNameContains>Venus</PlanetNameContains>
      <AverageMinutesToStartRusting>10</AverageMinutesToStartRusting>
    </Planet>
  </Planets>
  <BlockSubtypeContainsBlackList>
    <string>Concrete</string>
    <string>Wood</string>
  </BlockSubtypeContainsBlackList>
[/code]

OnlyRustUnpoweredGrids - will rust only unpowered (abandoned) grids if set to true.

More planets, vanilla or custom can be added. PlanetNameContains is any part of planet name. AverageMinutesToStartRusting is how much minutes on average will it take for rust to start appearing on blocks. The further rusting depends on block integrity. I.e. it will take about 20 times more for large light armor block until block completely dissapear.

BlockSubtypeContainsBlackList - blocks that subtype name contains any of the string from this list, will not rust. Keep in mind this checks block subtype name, NOT texture name. So armor block painted in concrete or wood texture will still rust, but for example Concrete block from AQD Concrete mod or Wood Armor block from Tree Harvest mod will not rust.
Any vanilla or modded block names can be added to the list.


[h2]Integrations[/h2]

Any modded planet that has atmosphere can be used with this mod.

Any modded block will rust if it supports textures.

To make rust maintenance more realistic it is recomended to use this mod together with [url=https://steamcommunity.com/sharedfiles/filedetails/?id=500818376]Paint Gun[/url] mod, while [url=https://steamcommunity.com/sharedfiles/filedetails/?id=2046319599]disabling vanilla painting[/url]


[h2]Acknowledgements[/h2]

Mod is based on Atmospheric Damage script by [b]Rexxar[/b]. Could not find the original link, if someone has it, please let me know.

Ships in screenshots:
[url=https://steamcommunity.com/sharedfiles/filedetails/?id=2562576691]Astron, interplanetary tanker/hauler (No mods)[/url] by OctoBooze
[url=https://steamcommunity.com/sharedfiles/filedetails/?id=2617139013]“Frontier” Scientific Research Exploration System(No Mod)[/url] by ARC17Alpha
[url=https://steamcommunity.com/sharedfiles/filedetails/?id=2652038922]SpaceX Starship (1:1 scale)[/url] by me