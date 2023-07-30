using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TPSBR
{
    public class DispatchToGame : MonoBehaviour
    {
        public string sceneName;
        public void LoadTheGame()
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}
