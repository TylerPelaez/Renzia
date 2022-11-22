using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI actionPointLabel;
    [SerializeField]
    private Button endTurnButton;
    [SerializeField]
    private Button attackButton;

    public event EventHandler OnEndTurnButtonClicked;
    public event EventHandler OnAttackButtonClicked;
    
    private void Start()
    {
        endTurnButton.onClick.AddListener(() => OnEndTurnButtonClicked?.Invoke(this, EventArgs.Empty));
        attackButton.onClick.AddListener(() => OnAttackButtonClicked?.Invoke(this, EventArgs.Empty));
    }

    public void SetActionPointLabel(int actionPoints)
    {
        actionPointLabel.text = "Action Points: " + actionPoints;
    }
}
