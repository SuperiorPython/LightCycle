using UnityEngine;
using UnityEngine.UI;

public class WinUI : MonoBehaviour
{
    public GameObject panel;
    public GameManager gm;
    public Text finalScoreText;

    void Awake() => Hide();

    public void Show()
    {
        if (finalScoreText != null && gm != null)
            finalScoreText.text = gm.FinalScoreString();

        if (panel != null)
            panel.SetActive(true);
    }

    public void Hide()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    public void Retry()
    {
        if (gm != null)
            gm.Retry();
    }

    public void MainMenu()
    {
        if (gm != null)
            gm.ReturnToTitle();
    }
}