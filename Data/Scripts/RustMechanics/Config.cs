using Sandbox.Game;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;

namespace RustMechanics
{
	public struct Planet
	{
		public string PlanetNameContains;
		public double AverageMinutesToStartRusting;
	}

	public struct RustConfig
	{
		public bool OnlyRustUnpoweredGrids;
		public List<Planet> Planets;
		public List<string> BlockSubtypeContainsBlackList;
	}

	[MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
	public class Config : MySessionComponentBase
	{
		public static RustConfig rustConfig = new RustConfig()
		{
			OnlyRustUnpoweredGrids = false,
			Planets = new List<Planet>()
			{
				new Planet { PlanetNameContains = "Earth", AverageMinutesToStartRusting = 300},
				new Planet { PlanetNameContains = "Alien", AverageMinutesToStartRusting = 180},
				new Planet { PlanetNameContains = "Pertam", AverageMinutesToStartRusting = 60},
				new Planet { PlanetNameContains = "Venus", AverageMinutesToStartRusting = 10},
			},
			BlockSubtypeContainsBlackList = new List<string>()
			{
				"Concrete",
				"Wood",
			}
		};

		public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
		{
			try
			{
				string configFileName = "config1.2.xml";
				if (MyAPIGateway.Utilities.FileExistsInWorldStorage(configFileName, typeof(RustConfig)))
				{
					var textReader = MyAPIGateway.Utilities.ReadFileInWorldStorage(configFileName, typeof(RustConfig));
					var configXml = textReader.ReadToEnd();
					textReader.Close();
					rustConfig = MyAPIGateway.Utilities.SerializeFromXML<RustConfig>(configXml);
				}
				else
				{
					var textWriter = MyAPIGateway.Utilities.WriteFileInWorldStorage(configFileName, typeof(RustConfig));
					textWriter.Write(MyAPIGateway.Utilities.SerializeToXML(rustConfig));
					textWriter.Flush();
					textWriter.Close();
				}
			}
			catch (Exception e)
			{
				//MyAPIGateway.Utilities.ShowMessage("RustMechanics", "Exception: " + e);
			}
		}
	}
}
