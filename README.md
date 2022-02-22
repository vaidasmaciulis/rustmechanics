Blocks will slowly rust over time while in atmosphere of configured planets.

All blocks that is not covered by other blocks or airtight spaces in all directions will be affected by rust.

By default Earth, Alien, Pertam and Venus (that is any modded planet that has "Venus" in it's name) planets are configured.

Grids inside SafeZone that has damage disabled will not rust.

Server side only scripts! (Should work on Xbox dedicated servers, but not tested)

!!WARNING!! This mod is still somewhat experimental. Bugs are expected, please report if encountered. Also careful adding this to your already build world without backup saves.


[h1]Configuration[/h1]

This mod can be configured per save game:

Create and save game with this mod added.
Open Storage directory of your save, i.e.: 
[code]C:\Users\<User Name>\AppData\Roaming\SpaceEngineers\Saves\<Some number>\<Save Game name>\Storage\<Some number>.sbm_RustMechanics
[/code]
config.xml file should be inside.
Open it with Notepad or other text editor

You will see planets config:
[code]  <planets>
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
  </planets>
[/code]

More planets, vanilla or custom can be added. PlanetNameContains is any part of planet name. AverageMinutesToStartRusting is how much minutes on average will it take for rust to start appearing on blocks. The further rusting depends on block integrity. I.e. it will take about 20 times more for large light armor block until block completely dissapears.


[h2]Integrations[/h2]

Any modded planet that has atmosphere can be used with this mod.


[h1]Acknowledgements[/h1]

Mod is based on Atmospheric Damage script by [b]Rexxar[/b]. Could not find the original link, if someone has it, please let me know.