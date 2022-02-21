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
	[MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
	public class DamageCore : MySessionComponentBase
	{
		/////////////////////CHANGE THESE FOR EACH PLANET////////////////////////////

		private const string PLANET_NAME = "EarthLike"; //this mod targets planet EarthLike
		private const int UPDATE_RATE = 300; //damage will apply every 5 seconds
		private const float RUST_DAMAGE = 1f;
		private const double RUST_PERCENTAGE_DOUBLE = 1;

		/////////////////////////////////////////////////////////////////////////////

		private readonly Random _random = new Random();
		private bool _init;
		private HashSet<MyPlanet> _planets = new HashSet<MyPlanet>();
		private int _updateCount = 0;
		private MyStringHash _rustHash;
		private MyStringHash _heavyRustHash;
		private bool _processing;
		private Queue<Action> _actionQueue = new Queue<Action>();
		private Queue<Action> _slowQueue = new Queue<Action>();
		private int _actionsPerTick = 0;
		private int _skipTicks = 0;

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

				//MyAPIGateway.Parallel.Start(ProcessQueue);
				ProcessQueue();
				ProcessSlowQueue();

				//update our list of planets every 19 seconds in case people paste new planets
				if (++_updateCount % 1170 == 0)
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

							if (InSafezone(grid))
								continue;

							if (IsInsideAirtightGrid(grid))
								continue;

							var blocks = new List<IMySlimBlock>();
							grid.GetBlocks(blocks);

							MyCubeGrid gridInternal = (MyCubeGrid)grid;

							/*int a = 0;
							Stopwatch stopWatch = new Stopwatch();
							stopWatch.Start();
							foreach (var block in blocks)
							{
								if (_random.NextDouble() < RUST_PERCENTAGE_DOUBLE)
									a++;
							}
							stopWatch.Stop();
							// Get the elapsed time as a TimeSpan value.
							TimeSpan ts = stopWatch.Elapsed;
							MyVisualScriptLogicProvider.ShowNotification("Time to randomize: " + ts + " blocks: " + blocks.Count, 5000);*/

							foreach (var block in blocks)
							{
								if (_random.NextDouble() < RUST_PERCENTAGE_DOUBLE)
								{
									if (HasOpenFaces(block, grid, blocks.Count))
									{
										if (block.SkinSubtypeId == _heavyRustHash)
										{
											if (_slowQueue.Count < UPDATE_RATE)
												_slowQueue.Enqueue(() => DamageBlock(block, gridInternal)); ;
										}
										else
										{
											QueueInvoke(() => RustBlockPaint(block, gridInternal));
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

		/*private bool IsExternal(IMySlimBlock block, IMyCubeGrid grid)
		{
			Vector3D posBlock = grid.GridIntegerToWorld(block.Position);
			Vector3D posCenter = grid.WorldAABB.Center;
			Vector3D direction = posBlock - posCenter;
			direction.Normalize();

			Vector3I? blockPos = grid.RayCastBlocks(posBlock + direction * 50, posBlock);

			return grid.GetCubeBlock(blockPos.Value) == block;
		}*/

		/*private int GetOpenFacesCount(IMySlimBlock block, IMyCubeGrid grid)
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
				{
					//MyVisualScriptLogicProvider.ShowNotification("Found neibor block", 1000);
					continue;
				}
				if (grid.IsRoomAtPositionAirtight(position))
				{
					//MyVisualScriptLogicProvider.ShowNotification("Found neibor airtigh", 1000);
					continue;
				}
				openFacesCount++;
			}
			//MyVisualScriptLogicProvider.ShowNotification("Found open faces: " + openFacesCount, 1000);

			return openFacesCount;
		}*/

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
					//if(grid.GetCubeBlock(position).FatBlock != null && grid.GetCubeBlock(position).FatBlock?.EntityId == block.FatBlock?.EntityId)
					//	MyVisualScriptLogicProvider.ShowNotification("Block colission found", 3000);
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

		public static bool InSafezone(IMyEntity ent)
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
				//_heavyActionsThisTick++;
			}
			else
			{
				block?.DecreaseMountLevel(RUST_DAMAGE, null, true);
				//_heavyActionsThisTick++;
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
				if (_skipTicks > 0)
				{
					_skipTicks--;
					return;
				}
				Action action;
				if (!_actionQueue.TryDequeue(out action))
					return;

				SafeInvoke(action);
			}
		}

		private void ProcessSlowQueue()
		{
			//MyVisualScriptLogicProvider.ShowNotification("Sklow Queue size: " + _slowQueue.Count, 1000);
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
						MyVisualScriptLogicProvider.ShowNotification("Exception: " + e + e.InnerException, 5000);
					}
				});
			}
			catch (Exception e)
			{
				MyVisualScriptLogicProvider.ShowNotification("Exception: " + e + e.InnerException, 5000);
			}
		}
	}
}
