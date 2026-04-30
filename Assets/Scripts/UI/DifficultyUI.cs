using UnityEngine;

public class DifficultyUI : MonoBehaviour
{
    public GameObject panel;
    public GameManager gm;

    void Awake()
    {
        Hide();
    }

    public void Show()
    {
        if (panel != null)
            panel.SetActive(true);
    }

    public void Hide()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    public void Easy()
    {
        gm.SelectDifficulty(AIController.Difficulty.Easy);
    }

    public void Medium()
    {
        gm.SelectDifficulty(AIController.Difficulty.Medium);
    }

    public void Hard()
    {
        gm.SelectDifficulty(AIController.Difficulty.Hard);
    }

    public void Elite()
    {
        gm.SelectDifficulty(AIController.Difficulty.Elite);
    }

    public void Back()
    {
        gm.SetState(GameState.Title);
    }
}