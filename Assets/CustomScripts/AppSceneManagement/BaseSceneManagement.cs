using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BaseSceneManagement : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartBaxter()
    {
        // Index '1' is linked to the Scenes/MainScene in the "Scenes in Build"
        // section under File -> Build Settings.
        SceneManager.LoadScene(1);
    }

    public void StartBaxterFpv()
    {
        // Index '2' is linked to the Scenes/FpvScene2 in the "Scenes in Build"
        // section under File -> Build Settings.
        SceneManager.LoadScene(2);
    }
}
