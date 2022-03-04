using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonLauncherScene : MonoBehaviour
{

    GameObject gameManager;
    private void Start()
    {
        gameManager = GameObject.Find("GameManager");
    }
    public void OnTutoClicked()
    {
        // launch tuto
    }
    public void OnNextSceneClicked()
    {
        LauncherManager launcher = gameManager.GetComponent<LauncherManager>();
        if(launcher != null)
        {
            if(launcher.first != 0)
            {
                switch (launcher.first)
                {
                    case 1: SceneManager.LoadScene("TradiScene");
                        break;
                    case 2:
                        SceneManager.LoadScene("2DVRTist");
                        break;
                    case 3:
                        SceneManager.LoadScene("Main");
                        break;
                    default:
                        break;
                }
                launcher.first = 0;
            }
            else if(launcher.second != 0)
            {
                switch (launcher.second)
                {
                    case 1:
                        SceneManager.LoadScene("TradiScene");
                        break;
                    case 2:
                        SceneManager.LoadScene("2DVRTist");
                        break;
                    case 3:
                        SceneManager.LoadScene("Main");
                        break;
                    default:
                        break;
                }
                launcher.second = 0;
            }
            else
            {
                switch (launcher.scenes[0])
                {
                    case 1:
                        SceneManager.LoadScene("TradiScene");
                        break;
                    case 2:
                        SceneManager.LoadScene("2DVRTist");
                        break;
                    case 3:
                        SceneManager.LoadScene("Main");
                        break;
                    default:
                        break;
                }
                launcher.scenes.Clear();
            }
        }
    }
}
