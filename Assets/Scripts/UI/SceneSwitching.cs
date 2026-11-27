using UnityEngine;
using UnityEngine.SceneManagement;


public class SceneSwitching : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene(0);
    }
}
