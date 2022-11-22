using System;
using System.Collections.Generic;
using UnityEngine;
using Util;
using Object = System.Object;

public class GameController : MonoBehaviour
{
	public MapController mapController;
	public UIController uiController;
	private FiniteStateMachine<GameState> stateMachine = new FiniteStateMachine<GameState>();

	private LinkedList<Unit> initiativeOrder;
	private Unit roundStartPreviousUnit;
	private Unit roundStartNextUnit;
	public int RoundCount { get; private set; }

	void Start()
    {
		AddressablesManager manager = AddressablesManager.Instance;
		manager.OnLoadComplete += OnAddressablesLoadComplete;
	    
		stateMachine.Add(new PlayerTurnState(mapController, this));
		stateMachine.Add(new EnemyTurnState(mapController, this));
		stateMachine.Add(new State<GameState>(GameState.TRANSITION));

		SetupInitiativeOrder();
		RoundCount = 1;
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
				break;
			case Team.ENEMY:
				stateMachine.SetCurrentState(GameState.ENEMY_TURN);
				break;
		}
	}

	public void OnUnitDeath(Object unit, EventArgs args)
	{
		if (unit is not Unit)
		{
			Debug.LogError("Unit Death did not get a unit object!");
			return;
		}

		mapController.OnUnitDeath((Unit) unit);
		initiativeOrder.Remove((Unit) unit);
	}

	public LinkedList<Unit> GetAllUnits()
	{
		return initiativeOrder;
	}
}
