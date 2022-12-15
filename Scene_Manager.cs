using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Scene_Manager : MonoBehaviour
{
    public void LoadGame()
    {
        Initiate.Fade("GameScene", Color.black, 2f);
    }

    public void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void LoadDeadScene()
    {
        SceneManager.LoadScene("DeadMenu");
    }

    public void LoadControlsScene()
    {
        SceneManager.LoadScene("ControsScene");
    }

    public void LoadVictoryScene()
    {
        SceneManager.LoadScene("VictoryScene");
    }

}
