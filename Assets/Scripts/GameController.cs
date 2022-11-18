using UnityEngine;

public class GameController : MonoBehaviour
{
	public GameObject sceneCamera;
	public MapController mapController;
	private FiniteStateMachine<GameState> stateMachine = new FiniteStateMachine<GameState>();
	
    void Start()
    {
		// Testin and Debuggin
		stateMachine.Add(new PlayerTurnState(mapController));
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
}
