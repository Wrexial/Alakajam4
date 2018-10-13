using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using FMOD;
using FMODUnity;
using FMOD.Studio;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<Transform>().gameObject.AddComponent<AudioManager>();
            }

            return _instance;
        }
        private set { _instance = value; }
    }
    private static AudioManager _instance;

    public Button MuteButton;
    public Image MuteButtonImage;
    public Sprite SoundOff;
    public Sprite SoundOn;

    private Bus _musicBus;
    private bool _isMuted;

    private void Awake()
    {
        Instance = this;
        MuteButton.onClick.AddListener(ToggleHandle);
        SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
        _musicBus = RuntimeManager.GetBus("bus:/Master");
    }

    private void SceneManager_activeSceneChanged(Scene current, Scene next)
    {
    }

    private void ToggleHandle()
    {
        _musicBus.getMute(out _isMuted);
        if (_isMuted)
        {
            _musicBus.setMute(false);
            MuteButtonImage.sprite = SoundOn;
        }
        else
        {
            _musicBus.setMute(true);
            MuteButtonImage.sprite = SoundOff;
        }
    }
}