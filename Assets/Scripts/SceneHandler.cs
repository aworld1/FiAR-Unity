using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneHandler : MonoBehaviour {
    public static void SwitchScene(string scene) {
        SceneManager.LoadScene(scene);
    }
}
