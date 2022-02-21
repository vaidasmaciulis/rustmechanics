using System;
using System.Collections.Generic;
using System.Diagnostics;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

/*
 *  Based on script by Rexxar.
 */

namespace RustMechanics
{
	public struct RustyPlanet
	{
		public MyPlanet MyPlanet;
		public double RustProbability;
	}

	[MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
	public class RustMechanics : MySessionComponentBase
	{
		private const int UPDATE_RATE = 600; //rust will apply every 10 seconds
		private const float RUST_DAMAGE = 1f;

		private readonly Random _random = new Random();
		private bool _init;
		private HashSet<RustyPlanet> _planets = new HashSet<RustyPlanet>();
		private int _updateCount = 0;
		private MyStringHash _rustHash;
		private MyStringHash _heavyRustHash;
		private bool _processing;
		private Queue<Action> _actionQueue = new Queue<Action>();
		private Queue<Action> _slowQueue = new Queue<Action>();
		private int _actionsPerTick = 0;

		public override void UpdateBeforeSimulation()
		{
			try
			{
				if (MyAPIGateway.Session == null)
					return;

				//server only please
				if (!(MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE || MyAPIGateway.Multiplayer.IsServer))
					return;

				if (!_init)
					Initialize();

				ProcessQueue();
				ProcessSlowQueue();

				//update our list of planets every 19 seconds in case people paste new planets
				if (++_updateCount % 1170 == 0)
				{
					UpdatePlanetsList();
				}

				if (_updateCount % UPDATE_RATE != 0)
					return;

				if (_processing) //worker thread is busy
				    return;

				_processing = true;
				MyAPIGateway.Parallel.Start(ProcessDamage);
			}
			catch (Exception e)
			{
				MyVisualScriptLogicProvider.ShowNotification("Exception: " + e, 5000);
			}
		}

		private void ProcessDamage()
		{
			try
			{
				//MyVisualScriptLogicProvider.ShowNotification("Player position: " + MyVisualScriptLogicProvider.GetPlayersPosition(), 1000);
				foreach (var planet in _planets)
				{
					var sphere = new BoundingSphereD(planet.MyPlanet.PositionComp.GetPosition(), planet.MyPlanet.AverageRadius + planet.MyPlanet.AtmosphereAltitude);

					var topEntities = MyAPIGateway.Entities.GetTopMostEntitiesInSphere(ref sphere);
					foreach (var entity in topEntities)
					{
						var grid = entity as IMyCubeGrid;
						if (grid?.Physics != null)
						{
							if (grid.Closed || grid.MarkedForClose)
								continue;

							if (InSafeZone(grid))
								continue;

							if (IsInsideAirtightGrid(grid))
								continue;

							var blocks = new List<IMySlimBlock>();
							grid.GetBlocks(blocks);

							MyCubeGrid gridInternal = (MyCubeGrid)grid;

							foreach (var block in blocks)
							{
								if (_random.NextDouble() < planet.RustProbability)
								{
									if (HasOpenFaces(block, grid, blocks.Count))
									{
										if (block.SkinSubtypeId == _heavyRustHash)
										{
											if (_slowQueue.Count < UPDATE_RATE)
												_slowQueue.Enqueue(() => DamageBlock(block, gridInternal));
										}
										else
										{
											_actionQueue.Enqueue(() => RustBlockPaint(block, gridInternal));
										}
									}
								}
							}
						}
					}
				}
			}
			catch (Exception e)
			{
				//MyVisualScriptLogicProvider.ShowNotification("Exception: " + e, 5000);
			}
			finally
			{
				_actionsPerTick = _actionQueue.Count / UPDATE_RATE + 1;
				_processing = false;
			}
		}

		private void Initialize()
		{
			_init = true;

			_rustHash = MyStringHash.GetOrCompute("Rusty_Armor");
			_heavyRustHash = MyStringHash.GetOrCompute("Heavy_Rust_Armor");

			UpdatePlanetsList();
		}

		private void UpdatePlanetsList()
		{
			_planets.Clear();
			var entities = new HashSet<IMyEntity>();
			MyAPIGateway.Entities.GetEntities(entities, x => x is MyPlanet);
			foreach (var entitiy in entities)
			{
				MyPlanet myPlanet = (MyPlanet)entitiy;
				foreach (var planetConfig in Config.planetsConfig.planets)
				{
					if (myPlanet.StorageName.Contains(planetConfig.PlanetNameContains))
					{
						_planets.Add(new RustyPlanet()
						{
							MyPlanet = myPlanet,
							//3600 - game ticks per minute
							RustProbability = UPDATE_RATE / (3600 * planetConfig.AverageMinutesToStartRusting)
						});
					}
				}
			}
		}

		private bool HasOpenFaces(IMySlimBlock block, IMyCubeGrid grid, int blocksInGrid)
		{
			// Not possible to cover all sides without at least 6 blocks
			if (blocksInGrid <= 6)
				return true;

			List<Vector3I> neighbourPositions = new List<Vector3I>
			{
				block.Max + new Vector3I(1,0,0),
				block.Max + new Vector3I(0,1,0),
				block.Max + new Vector3I(0,0,1),
				block.Min - new Vector3I(1,0,0),
				block.Min - new Vector3I(0,1,0),
				block.Min - new Vector3I(0,0,1)
			};

			foreach (Vector3I position in neighbourPositions)
			{
				//MyVisualScriptLogicProvider.ShowNotification("Position: " + position, 3000);
				if (grid.GetCubeBlock(position) != null)
				{
					//MyVisualScriptLogicProvider.ShowNotification("Found neibor block", 1000);
					continue;
				}
				if (grid.IsRoomAtPositionAirtight(position))
				{
					//MyVisualScriptLogicProvider.ShowNotification("Found neibor airtigh", 1000);
					continue;
				}
				return true;
			}
			return false;
		}

		private static bool IsInsideAirtightGrid(IMyEntity grid)
		{
			if (grid?.Physics == null)
				return false;

			BoundingSphereD sphere = grid.WorldVolume;
			List<IMyEntity> entities = MyAPIGateway.Entities.GetTopMostEntitiesInSphere(ref sphere);

			foreach (IMyEntity entity in entities)
			{
				if (entity == null)
					continue;

				if (entity.EntityId == grid.EntityId)
					continue;

				var parentGrid = entity as IMyCubeGrid;
				if (parentGrid == null)
					continue;
				if (parentGrid.IsRoomAtPositionAirtight(parentGrid.WorldToGridInteger(sphere.Center)))
				{
					//MyVisualScriptLogicProvider.ShowNotification(grid + "is inside airtight grid" , 1000);
					return true;
				}
			}
			return false;
		}

		public static bool InSafeZone(IMyEntity ent)
		{
			return !MySessionComponentSafeZones.IsActionAllowed((MyEntity)ent, CastHax(MySessionComponentSafeZones.AllowedActions, 0x1));
		}

		public static T CastHax<T>(T typeRef, object castObj) => (T)castObj;

		private void RustBlockPaint(IMySlimBlock block, MyCubeGrid gridInternal)
		{
			MyCube myCube;
			gridInternal.TryGetCube(block.Position, out myCube);
			if (myCube == null)
				return;

			if (block.SkinSubtypeId == _rustHash)
			{
				gridInternal.ChangeColorAndSkin(myCube.CubeBlock, skinSubtypeId: _heavyRustHash);
			}
			else
			{
				gridInternal.ChangeColorAndSkin(myCube.CubeBlock, skinSubtypeId: _rustHash);
			}
		}

		private void DamageBlock(IMySlimBlock block, MyCubeGrid gridInternal)
		{
			if (block.IsFullyDismounted)
			{
				//MyVisualScriptLogicProvider.ShowNotification("Removing block id: " + block.FatBlock.EntityId, 1000);
				//TODO which faster? any difference?
				//grid.RemoveBlock(block, true);
				gridInternal.RazeBlock(block.Position);
			}
			else
			{
				block?.DecreaseMountLevel(RUST_DAMAGE, null, true);
				//MyVisualScriptLogicProvider.ShowNotification("Decreasing for block to: " + block.BuildIntegrity, 1000);
			}
		}

		//spread our invoke queue over many updates to avoid lag spikes
		private void ProcessQueue()
		{
			//MyVisualScriptLogicProvider.ShowNotification("Queue size: " + _actionQueue.Count, 1000);
			if (_actionQueue.Count == 0)
				return;
			for (int i = 0; i < _actionsPerTick; i++)
			{
				Action action;
				if (!_actionQueue.TryDequeue(out action))
					return;

				SafeInvoke(action);
			}
		}

		private void ProcessSlowQueue()
		{
			//MyVisualScriptLogicProvider.ShowNotification("Slow Queue size: " + _slowQueue.Count, 1000);
			if (_slowQueue.Count == 0)
				return;

			Action action;
			if (!_slowQueue.TryDequeue(out action))
				return;

			SafeInvoke(action);
		}

		private void QueueInvoke(Action action)
		{
			_actionQueue.Enqueue(action);
		}

		//wrap invoke in try/catch so we don't crash on unexpected error
		//what we're doing isn't critical, so don't bother logging the errors
		private void SafeInvoke(Action action)
		{
			try
			{
				//action();
				MyAPIGateway.Utilities.InvokeOnGameThread(() =>
				{
					try
					{
						action();
					}
					catch (Exception e)
					{
						//MyVisualScriptLogicProvider.ShowNotification("Exception: " + e + e.InnerException, 5000);
					}
				});
			}
			catch (Exception e)
			{
				//MyVisualScriptLogicProvider.ShowNotification("Exception: " + e + e.InnerException, 5000);
			}
		}
	}
}
