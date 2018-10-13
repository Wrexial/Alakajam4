using MEC;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AutoLaunchScene : MonoBehaviour
{
    public float Delay = 2;
    public int SceneToLoad = 1;

    public void Awake()
    {
        Timing.CallDelayed(Delay, () => { SceneManager.LoadScene(SceneToLoad); });
    }
}