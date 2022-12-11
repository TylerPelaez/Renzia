using System;
using System.Collections.Generic;
using UnityEngine;
using Util;
using Object = System.Object;

public class GameController : MonoBehaviour
{
	[SerializeField]
	private MapController mapController;
	[SerializeField]
	private UIController uiController;
	[SerializeField]
	private CameraController cameraController;
	private FiniteStateMachine<GameState> stateMachine = new FiniteStateMachine<GameState>();

	private LinkedList<Unit> initiativeOrder;
	private Unit roundStartPreviousUnit;
	private Unit roundStartNextUnit;
	
	[field: SerializeField]
	public int RoundCount { get; private set; }

	
	// TODO: Pick this more intelligently
	[field: SerializeField]
	public MissionObjective Objective { get; private set; } = MissionObjective.KILL_ALL_ENEMIES;

	void Start()
    {
	    Screen.SetResolution(1920,1080,true);
	    stateMachine.Add(new PlayerTurnState(mapController, uiController, this, cameraController));
		stateMachine.Add(new EnemyTurnState(mapController, this));
		stateMachine.Add(new State<GameState>(GameState.TRANSITION));
		stateMachine.Add(new State<GameState>(GameState.PAUSED));

		SetupInitiativeOrder();
		RoundCount = 1;
		uiController.SetMissionObjectiveText(Objective);
		
		AddressablesManager manager = AddressablesManager.Instance;
		if (manager.Loaded)
		{
			OnAddressablesLoadComplete(null, EventArgs.Empty);
		}
		else
		{
			manager.OnLoadComplete += OnAddressablesLoadComplete;
		}
    }

	private void SetupInitiativeOrder()
	{
		Unit[] allUnits = FindObjectsOfType<Unit>();
		List<Unit> orderedUnits = new List<Unit>(allUnits);
		orderedUnits.Sort((firstUnit, secondUnit) => secondUnit.Initiative.CompareTo(firstUnit.Initiative));
		initiativeOrder = new LinkedList<Unit>();

		foreach (var unit in orderedUnits)
		{
			initiativeOrder.AddLast(unit);
			unit.OnDeath += OnUnitDeath;
		}

		roundStartNextUnit = initiativeOrder.First.Value;
		roundStartPreviousUnit = initiativeOrder.Last.Value;
		
		uiController.ResetInitiativeOrderUI(initiativeOrder);
	}

    void FixedUpdate()
	{
		stateMachine.FixedUpdate();
	}

	void Update()
	{
		stateMachine.Update();
	}

	private void OnAddressablesLoadComplete(Object sender, EventArgs arg)
	{
		StartCurrentUnitTurn();
	}

	public void OnUnitTurnFinished()
	{
		Unit finishedUnit = GetCurrentTurnUnit();
		initiativeOrder.RemoveFirst();
		initiativeOrder.AddLast(finishedUnit);

		if (finishedUnit == roundStartPreviousUnit && GetCurrentTurnUnit() == roundStartNextUnit)
		{
			RoundCount++;
		}

		stateMachine.SetCurrentState(GameState.TRANSITION);
		uiController.OnTurnEnded();

		if (EvaluateObjective())
		{
			return;
		}

		StartCurrentUnitTurn();
	}

	public Unit GetCurrentTurnUnit()
	{
		return initiativeOrder.First.Value;
	}

	private void StartCurrentUnitTurn()
	{
		Unit currentUnit = GetCurrentTurnUnit();
		switch (currentUnit.Team)
		{
			case Team.PLAYER:
				stateMachine.SetCurrentState(GameState.PLAYER_TURN);
				cameraController.Unlock();
				cameraController.SmoothMoveToThenUnlock(currentUnit);
				break;
			case Team.ENEMY:
				stateMachine.SetCurrentState(GameState.ENEMY_TURN);
				cameraController.FollowUnit(currentUnit);
				break;
		}

		uiController.OnTurnStarted(currentUnit, RoundCount);
	}

	public void OnUnitDeath(Object deadUnit, EventArgs args)
	{
		if (deadUnit is not Unit unit)
		{
			Debug.LogError("Unit Death did not get a unit object!");
			return;
		}

		if (GetCurrentTurnUnit() == unit)
		{
			// I think if a unit is able to suicide, this could happen.  Maybe explosion or something?
			// TODO: Handle this
		}

		if (initiativeOrder.Count == 1)
		{
			// Let's not allow this to happen. Game over?
			// TODO: handle this
			Debug.LogError("Only one unit and it died!");
			return;
		}

		// Changing the round start/end defining units if one of them died
		if (unit == roundStartNextUnit)
		{
			LinkedListNode<Unit> nextUnitNode = initiativeOrder.Find(unit);
			if (nextUnitNode == null)
			{
				Debug.LogError("dead unit is not in initiative order! Shit will now be broken");
				return;
			}

			if (nextUnitNode.Next == null)
			{
				nextUnitNode = initiativeOrder.First;
			}

			roundStartNextUnit = nextUnitNode.Next!.Value;
		} else if (unit == roundStartPreviousUnit)
		{
			LinkedListNode<Unit> previousUnitNode = initiativeOrder.Find(unit);
			if (previousUnitNode == null)
			{
				Debug.LogError("dead unit is not in initiative order! Shit will now be broken");
				return;
			}

			if (previousUnitNode.Previous == null)
			{
				previousUnitNode = initiativeOrder.Last;
			}

			roundStartPreviousUnit = previousUnitNode.Previous!.Value;
		}
		

		mapController.OnUnitDeath(unit);
		initiativeOrder.Remove(unit);

		if (stateMachine.GetState(GameState.PLAYER_TURN) is TurnState turnState)
		{
			turnState.OnUnitDeath(unit);
		}
		
		uiController.ResetInitiativeOrderUI(initiativeOrder);

		EvaluateObjective();
		EvaluateLoss();
	}

	public LinkedList<Unit> GetAllUnits()
	{
		return initiativeOrder;
	}

	private bool EvaluateObjective()
	{
		bool objectiveComplete = false;
		switch (Objective)
		{
			case MissionObjective.KILL_ALL_ENEMIES:
				bool foundEnemy = false;
				foreach (var unit in initiativeOrder)
				{
					if (unit.Team == Team.ENEMY)
					{
						foundEnemy = true;
						break;
					}
				}

				objectiveComplete = !foundEnemy;
				break;
		}

		if (objectiveComplete)
		{
			stateMachine.SetCurrentState(GameState.PAUSED);
			uiController.ShowOutcome(true);
		}

		return objectiveComplete;
	}

	private bool EvaluateLoss()
	{
		bool foundPlayer = false;
		foreach (var unit in initiativeOrder)
		{
			if (unit.Team == Team.PLAYER)
			{
				foundPlayer = true;
				break;
			}
		}

		if (!foundPlayer)
		{
			stateMachine.SetCurrentState(GameState.PAUSED);
			uiController.ShowOutcome(false);
		}

		return !foundPlayer;
	}

	public void MoveUnit(Unit unit, Vector3Int newPosition, Action onCompleteCallback)
	{
		List<Vector3Int> shortestPath = mapController.GetShortestPath(unit.CurrentTile.GridPos, newPosition);
		mapController.MoveUnit(unit, newPosition);
		List<Vector3> shortestPathWorldPositions = new List<Vector3>();
		foreach (var gridPos in shortestPath)
		{
			Vector3 position = mapController.CellToWorld(gridPos);
			// TODO: FIX THIS if you want animations to work with higher z-value tiles
			position.z = 3;
			shortestPathWorldPositions.Add(position);
		}
		
		uiController.SetEnabled(false, unit, RoundCount);
		unit.StartMove(shortestPathWorldPositions, () => OnUnitMovementComplete(unit, onCompleteCallback));
	}

	private void OnUnitMovementComplete(Unit unit, Action callback)
	{
		uiController.SetEnabled(true, unit, RoundCount);
		callback?.Invoke();
	}

	public int GetPlayerActionPoints()
	{
		return ((PlayerTurnState)stateMachine.GetState(GameState.PLAYER_TURN)).ActionPoints;
	}
}
