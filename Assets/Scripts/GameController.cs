using UnityEngine;

public class GameController : MonoBehaviour
{
	public GameObject sceneCamera;
	public MapController mapController;
	private FiniteStateMachine<GameState> stateMachine = new FiniteStateMachine<GameState>();
	
    void Start()
    {
		stateMachine.Add(new PlayerTurnState(mapController, this));
		stateMachine.Add(new EnemyTurnState(mapController, this));
		
		// Testin and Debuggin
		stateMachine.SetCurrentState(GameState.PLAYER_TURN);
    }

	void FixedUpdate()
	{
		stateMachine.FixedUpdate();
	}

	void Update()
	{
		stateMachine.Update();
	}


	public void OnPlayerTurnFinished()
	{
		stateMachine.SetCurrentState(GameState.ENEMY_TURN);
	}

	public void OnEnemyTurnFinished()
	{
		stateMachine.SetCurrentState(GameState.PLAYER_TURN);
	}
}
