using UnityEngine;

// Reloadt de current scene wanneer de player op de restart UI button klikt.
public class ReloadScene : MonoBehaviour
{
    public void ReloadCurrentScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}
