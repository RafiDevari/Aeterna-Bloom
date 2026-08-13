using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void PlayGame()
    {
        SceneManager.LoadScene("RoomCreator");
    }

    public void Gallery()
    {
        SceneManager.LoadScene("Gallery");
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}