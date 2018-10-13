using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadBankAndScene : MonoBehaviour
{
    public int SceneToLoad;

    [FMODUnity.BankRef]
    public List<string> banks;

    private void Awake()
    {
        foreach (string b in banks)
        {
            FMODUnity.RuntimeManager.LoadBank(b, true);
        }
    }

    void Update()
    {
        var loaded = true;

        for (int i = 0; i < banks.Count; i++)
        {
            if (!FMODUnity.RuntimeManager.HasBankLoaded(banks[i]))
            {
                loaded = false;
            }
        }
        if (loaded)
        {
            SceneManager.LoadScene(SceneToLoad, LoadSceneMode.Single);
        }
    }

}
