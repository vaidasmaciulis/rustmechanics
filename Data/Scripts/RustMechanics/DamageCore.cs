using System;
using System.Collections.Generic;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

/*
 *  Based on script by Rexxar.
 */

namespace AtmosphericDamage
{
	[MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
	public class DamageCore : MySessionComponentBase
	{
		/////////////////////CHANGE THESE FOR EACH PLANET////////////////////////////

		private const string PLANET_NAME = "EarthLike"; //this mod targets planet EarthLike
		private const int UPDATE_RATE = 300; //damage will apply every 200 frames
		private const float SMALL_SHIP_DAMAGE = 0.1f;
		private const int AVOID_RUST_CHANCE = 10;
		private const int AVOID_RUST_DOWN_CHANCE = 1;

		/////////////////////////////////////////////////////////////////////////////

		private readonly Random _random = new Random();
		private bool _init;
		private HashSet<MyPlanet> _planets = new HashSet<MyPlanet>();
		private int _updateCount = 0;
		private MyStringHash _rustHash;
		private MyStringHash _heavyRustHash;
		private bool _processing;
		private Queue<Action> _actionQueue = new Queue<Action>();
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

				MyAPIGateway.Parallel.Start(ProcessQueue);
				//ProcessQueue();

				//update our list of planets every 10 seconds in case people paste new planets
				if (++_updateCount % 600 == 0)
				{
					_planets.Clear();
					var entities = new HashSet<IMyEntity>();
					MyAPIGateway.Entities.GetEntities(entities);
					foreach (var entity in entities)
					{
						var planet = entity as MyPlanet;
						if (planet == null)
							continue;
						if (planet.StorageName.StartsWith(PLANET_NAME))
							_planets.Add(planet);
					}
				}

				if (_updateCount % UPDATE_RATE != 0)
					return;

				if (_processing) //worker thread is busy
				    return;

				_processing = true;
				MyAPIGateway.Parallel.Start(ProcessDamage);
				//ProcessDamage();
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
					var sphere = new BoundingSphereD(planet.PositionComp.GetPosition(), planet.AverageRadius + planet.AtmosphereAltitude);

					var topEntities = MyAPIGateway.Entities.GetTopMostEntitiesInSphere(ref sphere);
					foreach (var entity in topEntities)
					{
						var grid = entity as IMyCubeGrid;
						if (grid?.Physics != null)
						{
							if (grid.Closed || grid.MarkedForClose)
								continue;
							//TODO add check
							//if (IsPositionInSafeZone(grid.WorldAABB.Center))
							//	continue;
							if (IsInsideAirtightGrid(grid))
								continue;

							var blocks = new List<IMySlimBlock>();
							grid.GetBlocks(blocks);

							MyCubeGrid gridInternal = (MyCubeGrid)grid;

							foreach (var block in blocks)
							{
								QueueInvoke(() => RustBlock(block, grid, gridInternal));
							}
						}
					}
				}
			}
			catch (Exception e)
			{
				MyVisualScriptLogicProvider.ShowNotification("Exception: " + e, 5000);
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

			//initialize our planet list
			var entities = new HashSet<IMyEntity>();
			MyAPIGateway.Entities.GetEntities(entities);
			foreach (var entity in entities)
			{
				var planet = entity as MyPlanet;
				if (planet == null)
					continue;
				if (planet.StorageName.StartsWith(PLANET_NAME))
					_planets.Add(planet);
			}
		}

		private bool IsExternal(IMySlimBlock block, IMyCubeGrid grid)
		{
			Vector3D posBlock = grid.GridIntegerToWorld(block.Position);
			Vector3D posCenter = grid.WorldAABB.Center;
			Vector3D direction = posBlock - posCenter;
			direction.Normalize();

			Vector3I? blockPos = grid.RayCastBlocks(posBlock + direction * 50, posBlock);

			return grid.GetCubeBlock(blockPos.Value) == block;
		}

		private int GetOpenFacesCount(IMySlimBlock block, IMyCubeGrid grid)
		{
			List<Vector3I> neighbourPositions = new List<Vector3I>
			{
				block.Max + (Vector3I)Base6Directions.Directions[(int)block.Orientation.Up],
				block.Max + (Vector3I)Base6Directions.Directions[(int)block.Orientation.Forward],
				block.Max + (Vector3I)Base6Directions.Directions[(int)block.Orientation.Left],
				block.Min - (Vector3I)Base6Directions.Directions[(int)block.Orientation.Up],
				block.Min - (Vector3I)Base6Directions.Directions[(int)block.Orientation.Forward],
				block.Min - (Vector3I)Base6Directions.Directions[(int)block.Orientation.Left]
			};
			int openFacesCount = 0;
			foreach (Vector3I position in neighbourPositions)
			{
				if (grid.GetCubeBlock(position) != null)
					continue;
				if (grid.IsRoomAtPositionAirtight(position))
					continue;
				openFacesCount++;
			}
			return openFacesCount;
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
					return true;
			}

			return false;
		}

		/*private static bool IsPositionInSafeZone(Vector3D coords)
		{
			return !MySessionComponentSafeZones.IsActionAllowed(coords, MySafeZoneAction.Shooting);
		}*/

		private void RustBlock(IMySlimBlock block, IMyCubeGrid grid, MyCubeGrid gridInternal)
		{
			var openFaces = GetOpenFacesCount(block, grid);

			if (openFaces > 0)
			{
				var integrityMultiplier = 1;
				var avoidRustChance = AVOID_RUST_CHANCE * integrityMultiplier;
				var avoidRustDownChance = AVOID_RUST_DOWN_CHANCE * integrityMultiplier;
				if (block.SkinSubtypeId == _heavyRustHash)
				{
					if (block.IsFullyDismounted)
					{
						if (_random.Next(AVOID_RUST_CHANCE) == 0)
						{
							//TODO: check if updating physics can be dopne ouside the loop for optimization
							grid.RemoveBlock(block, true);
						}
					}
					else
					{
						if (_random.Next(AVOID_RUST_DOWN_CHANCE) == 0)
						{
							block?.DecreaseMountLevel(SMALL_SHIP_DAMAGE, null);
						}
					}
				}
				else
				{
					if (block.SkinSubtypeId == _rustHash)
					{
						if (_random.Next(AVOID_RUST_CHANCE) == 0)
						{
							gridInternal.ChangeColorAndSkin(gridInternal.GetCubeBlock(block.Position), skinSubtypeId: _heavyRustHash);
						}
					}
					else
					{
						if (_random.Next(AVOID_RUST_CHANCE) == 0)
						{
							gridInternal.ChangeColorAndSkin(gridInternal.GetCubeBlock(block.Position), skinSubtypeId: _rustHash);
						}
					}
				}
			}
		}

		//spread our invoke queue over many updates to avoid lag spikes
		private void ProcessQueue()
		{
			if (_actionQueue.Count == 0)
				return;
			//MyVisualScriptLogicProvider.ShowNotification("Queue size: " + _actionQueue.Count, 1000);

			for (int i = 0; i < _actionsPerTick; i++)
			{
				Action action;
				if (!_actionQueue.TryDequeue(out action))
					return;

				SafeInvoke(action);
			}
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
				MyAPIGateway.Utilities.InvokeOnGameThread(() =>
				{
					try
					{
						action();
					}
					catch (Exception e)
					{
						MyVisualScriptLogicProvider.ShowNotification("Exception: " + e, 5000);
					}
				});
			}
			catch (Exception e)
			{
				MyVisualScriptLogicProvider.ShowNotification("Exception: " + e, 5000);
			}
		}
	}
}
