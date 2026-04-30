using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CountdownUI : MonoBehaviour
{
    public GameObject panel;
    public Text countdownText;

    void Awake()
    {
        Hide();
    }

    public IEnumerator PlayCountdown(float seconds)
    {
        Show();

        int count = Mathf.CeilToInt(seconds);

        while (count > 0)
        {
            countdownText.text = count.ToString();
            yield return new WaitForSeconds(1f);
            count--;
        }

        countdownText.text = "GO!";
        yield return new WaitForSeconds(0.5f);

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
}