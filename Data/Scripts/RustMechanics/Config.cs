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

	public struct PlanetsConfig
	{
		public List<Planet> planets;
	}

	[MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
	public class Config : MySessionComponentBase
	{
		public static PlanetsConfig planetsConfig = new PlanetsConfig()
		{
			planets = new List<Planet>()
			{
				new Planet { PlanetNameContains = "Earth", AverageMinutesToStartRusting = 300},
				new Planet { PlanetNameContains = "Alien", AverageMinutesToStartRusting = 180},
				new Planet { PlanetNameContains = "Pertam", AverageMinutesToStartRusting = 60},
				new Planet { PlanetNameContains = "Venus", AverageMinutesToStartRusting = 10},
			}
		};

		public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
		{
			try
			{
				string configFileName = "config.xml";
				if (MyAPIGateway.Utilities.FileExistsInWorldStorage(configFileName, typeof(PlanetsConfig)))
				{
					var textReader = MyAPIGateway.Utilities.ReadFileInWorldStorage(configFileName, typeof(PlanetsConfig));
					var configXml = textReader.ReadToEnd();
					textReader.Close();
					planetsConfig = MyAPIGateway.Utilities.SerializeFromXML<PlanetsConfig>(configXml);
				}
				else
				{
					var textWriter = MyAPIGateway.Utilities.WriteFileInWorldStorage(configFileName, typeof(PlanetsConfig));
					textWriter.Write(MyAPIGateway.Utilities.SerializeToXML(planetsConfig));
					textWriter.Flush();
					textWriter.Close();
				}
			}
			catch (Exception e)
			{
				MyAPIGateway.Utilities.ShowMessage("RustMechanics", "Exception: " + e);
				MyVisualScriptLogicProvider.ShowNotification(e.Message, 5000);
			}
		}
	}
}
