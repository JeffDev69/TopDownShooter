using UnityEngine;
using UnityEngine.SceneManagement;

// Laadt de huidige scène opnieuw wanneer de speler op de herstartknop klikt.
public class ReloadScene : MonoBehaviour
{
    public void ReloadCurrentScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
