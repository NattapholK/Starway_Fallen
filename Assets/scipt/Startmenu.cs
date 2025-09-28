using UnityEngine;
using UnityEngine.SceneManagement;

public class Startmenu : MonoBehaviour
{
    public void GoToScene(string sceneName){
        SceneManager.LoadScene(sceneName);    
    }
    
}
