using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
  private const string MusicVolumeKey = "Coursework_MusicVolume";
  private const string SfxVolumeKey = "Coursework_SfxVolume";

  private static AudioManager instance;

  public static AudioManager Instance
  {
    get
    {
      if (instance == null)
      {
        instance = FindFirstObjectByType<AudioManager>();
      }

      return instance;
    }
  }

  public float MusicVolume => musicVolume;
  public float SfxVolume => sfxVolume;

  private AudioSource bgmSource;
  private AudioSource sfxSource;

  private float musicVolume = 1f;
  private float sfxVolume = 1f;

  private AudioClip elvesGathering;
  private AudioClip epicBattleAndWarMusic;
  private AudioClip warriorsAssembleFullMix;
  private AudioClip attack1Sfx;
  private AudioClip attack2Sfx;
  private AudioClip attack3Sfx;
  private AudioClip loseSfx;
  private AudioClip finalWinSfx;
  private AudioClip pickupSfx;

  void Awake()
  {
    if (instance != null && instance != this)
    {
      Destroy(gameObject);
      return;
    }

    instance = this;

    LoadSavedVolumes();
    CacheAudioClips();
    SetupSources();

    SceneManager.sceneLoaded += OnSceneLoaded;
  }

  void Start()
  {
    ApplySceneMusic(SceneManager.GetActiveScene().name);
  }

  void OnDestroy()
  {
    if (instance == this)
    {
      SceneManager.sceneLoaded -= OnSceneLoaded;
      instance = null;
    }
  }

  void OnSceneLoaded(Scene scene, LoadSceneMode mode)
  {
    ApplySceneMusic(scene.name);
  }

  public void SetMusicVolume(float value)
  {
    musicVolume = Mathf.Clamp01(value);

    if (bgmSource != null)
    {
      bgmSource.volume = musicVolume;
    }

    PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
    PlayerPrefs.Save();
  }

  public void SetSfxVolume(float value)
  {
    sfxVolume = Mathf.Clamp01(value);

    if (sfxSource != null)
    {
      sfxSource.volume = sfxVolume;
    }

    PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
    PlayerPrefs.Save();
  }

  public void PlayAttackSfx(int attackType)
  {
    switch (attackType)
    {
      case 2:
        PlayOneShot(attack2Sfx);
        break;
      case 3:
        PlayOneShot(attack3Sfx);
        break;
      default:
        PlayOneShot(attack1Sfx);
        break;
    }
  }

  public void PlayPickupSfx()
  {
    PlayOneShot(pickupSfx);
  }

  public void PlayLoseSfx()
  {
    PlayOneShot(loseSfx);
  }

  public void PlayFinalWinSfx()
  {
    PlayOneShot(finalWinSfx);
  }

  void LoadSavedVolumes()
  {
    musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 0.8f);
    sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
  }

  void CacheAudioClips()
  {
    elvesGathering = Resources.Load<AudioClip>("Audio/Bgm/ElvesGathering");
    epicBattleAndWarMusic = Resources.Load<AudioClip>("Audio/Bgm/EpicBattleAndWarMusic");
    warriorsAssembleFullMix = Resources.Load<AudioClip>("Audio/Bgm/WarriorsAssembleFullMix");

    attack1Sfx = Resources.Load<AudioClip>("Audio/Sfx/Attack1");
    attack2Sfx = Resources.Load<AudioClip>("Audio/Sfx/Attack2");
    attack3Sfx = Resources.Load<AudioClip>("Audio/Sfx/Attack3");
    loseSfx = Resources.Load<AudioClip>("Audio/Sfx/Lose");
    finalWinSfx = Resources.Load<AudioClip>("Audio/Sfx/FinalWin");
    pickupSfx = Resources.Load<AudioClip>("Audio/Sfx/Pickup");
  }

  void SetupSources()
  {
    AudioSource[] sources = GetComponents<AudioSource>();

    if (sources.Length > 0)
    {
      bgmSource = sources[0];
    }
    else
    {
      bgmSource = gameObject.AddComponent<AudioSource>();
    }

    if (sources.Length > 1)
    {
      sfxSource = sources[1];
    }
    else
    {
      sfxSource = gameObject.AddComponent<AudioSource>();
    }

    ConfigureSource(bgmSource, true, musicVolume);
    ConfigureSource(sfxSource, false, sfxVolume);
  }

  void ConfigureSource(AudioSource source, bool loop, float volume)
  {
    source.playOnAwake = false;
    source.loop = loop;
    source.spatialBlend = 0f;
    source.volume = volume;
    source.ignoreListenerPause = true;
  }

  void ApplySceneMusic(string sceneName)
  {
    AudioClip targetClip = GetSceneMusic(sceneName);

    if (bgmSource == null)
    {
      return;
    }

    bgmSource.volume = musicVolume;

    if (targetClip == null)
    {
      if (bgmSource.isPlaying)
      {
        bgmSource.Stop();
      }

      bgmSource.clip = null;
      return;
    }

    if (bgmSource.clip == targetClip)
    {
      if (!bgmSource.isPlaying)
      {
        bgmSource.Play();
      }

      return;
    }

    bgmSource.Stop();
    bgmSource.clip = targetClip;
    bgmSource.Play();
  }

  AudioClip GetSceneMusic(string sceneName)
  {
    switch (sceneName)
    {
      case "MainMenu":
      case "Level_01":
        return elvesGathering;
      case "Level_02":
        return epicBattleAndWarMusic;
      case "Level_03":
        return warriorsAssembleFullMix;
      default:
        return null;
    }
  }

  void PlayOneShot(AudioClip clip)
  {
    if (sfxSource == null || clip == null)
    {
      return;
    }

    sfxSource.PlayOneShot(clip, sfxVolume);
  }
}
