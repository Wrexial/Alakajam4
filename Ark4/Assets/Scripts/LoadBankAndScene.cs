using System.Collections.Generic;
using UnityEngine;

public class LoadBankAndScene : MonoBehaviour
{
    [FMODUnity.BankRef]
    public List<string> banks;
    public GameObject PlayButton;
    public GameObject AudioPrefab;
    private bool init = false;

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

        if (loaded && !init)
        {
            init = true;
            Instantiate(AudioPrefab);
            PlayButton.SetActive(true);
        }
    }

}
