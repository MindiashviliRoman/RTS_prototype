using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuLogic : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.Escape)) {
            QuiteApp();
        }
    }

    public void OnStartBttn() {
        SceneManager.LoadSceneAsync(1);
    }

    public void OnQuiteBttn() {
        QuiteApp();
    }

    private void QuiteApp() {
        Application.Quit();
    }
}
