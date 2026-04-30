using UnityEngine;

public class TitleUI : MonoBehaviour
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

    public void Play()
    {
        if (gm != null)
            gm.PlayFromTitle();
    }

    public void Quit()
    {
        if (gm != null)
            gm.QuitGame();
    }
}