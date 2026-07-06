using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Простой синглтон-менеджер звука. Клипы накидываются в инспекторе.
/// Источники создаются в рантайме: музыка/эмбиент — зациклены, SFX/UI — PlayOneShot.
/// Вызовы делаются из нужных мест как AudioManager.Instance?.PlayX().
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Микшер")]
    [Tooltip("Аудио-микшер проекта. Источники разводятся по группам Music/Ambient/Effects/UI по имени.")]
    [SerializeField] private AudioMixer mixer;

    [Header("Музыка / фон")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private AudioClip seaAmbient;
    [Tooltip("Автоматически запускать музыку и эмбиент при старте.")]
    [SerializeField] private bool playMusicOnStart = true;

    [Header("Геймплей")]
    [Tooltip("Тап/клик во время игры (не зависит от попадания в навмеш).")]
    [SerializeField] private AudioClip gameClick;
    [Tooltip("Одиночный звук шага (проигрывается по animation event'ам).")]
    [SerializeField] private AudioClip footstep;
    [SerializeField] private AudioClip chestOpen;
    [Tooltip("Апгрейд персонажа / получение меча.")]
    [SerializeField] private AudioClip swordUpgrade;
    [SerializeField] private AudioClip manYes;

    [Header("Бой")]
    [Tooltip("Удар меча героя (победа).")]
    [SerializeField] private AudioClip swordHit;
    [Tooltip("Смерть гоблина (победа).")]
    [SerializeField] private AudioClip enemyDeath;
    [Tooltip("Удар врага С оружием (поражение).")]
    [SerializeField] private AudioClip enemyAttackArmed;
    [Tooltip("Удар врага БЕЗ оружия — кулак (поражение).")]
    [SerializeField] private AudioClip enemyAttackUnarmed;
    [Tooltip("Смерть персонажа (поражение).")]
    [SerializeField] private AudioClip heroDeath;

    [Header("UI")]
    [Tooltip("Клик по любому UI-элементу.")]
    [SerializeField] private AudioClip uiClick;
    [Tooltip("Окно победы (конец уровня).")]
    [SerializeField] private AudioClip winUi;
    [Tooltip("Окно поражения.")]
    [SerializeField] private AudioClip loseUi;

    [Header("Счёт")]
    [Tooltip("Тик на каждое очко при перетекании счёта.")]
    [SerializeField] private AudioClip scoreTick;

    private AudioSource _musicSource;
    private AudioSource _ambientSource;
    private AudioSource _sfxSource;
    private AudioSource _uiSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _musicSource = CreateSource("Music", true, GetGroup("Music"));
        _ambientSource = CreateSource("Ambient", true, GetGroup("Ambient"));
        _sfxSource = CreateSource("SFX", false, GetGroup("Effects"));
        _uiSource = CreateSource("UI", false, GetGroup("UI"));
    }

    // Ищет группу микшера по имени (Music/Ambient/Effects/UI).
    private AudioMixerGroup GetGroup(string groupName)
    {
        if (mixer == null)
            return null;

        var groups = mixer.FindMatchingGroups(groupName);
        return groups.Length > 0 ? groups[0] : null;
    }

    private void Start()
    {
        if (!playMusicOnStart)
            return;

        PlayMusic();
        PlayAmbient();
    }

    private AudioSource CreateSource(string sourceName, bool loop, AudioMixerGroup group)
    {
        var go = new GameObject(sourceName);
        go.transform.SetParent(transform);

        var src = go.AddComponent<AudioSource>();
        src.loop = loop;
        src.playOnAwake = false;
        // Громкость целиком регулируется в микшере (группы Music/Ambient/Effects/UI).
        if (group != null)
            src.outputAudioMixerGroup = group;

        return src;
    }

    // --- Музыка / фон ---
    public void PlayMusic()
    {
        if (backgroundMusic == null) return;
        _musicSource.clip = backgroundMusic;
        _musicSource.Play();
    }

    public void PlayAmbient()
    {
        if (seaAmbient == null) return;
        _ambientSource.clip = seaAmbient;
        _ambientSource.Play();
    }
    
    public void PlayManYes() => PlaySfx(manYes);
    public void PlayFootstep() => PlaySfx(footstep);
    public void PlayGameClick() => PlaySfx(gameClick);
    public void PlayChestOpen() => PlaySfx(chestOpen);
    public void PlaySwordUpgrade() => PlaySfx(swordUpgrade);
    public void PlaySwordHit() => PlaySfx(swordHit);
    public void PlayEnemyDeath() => PlaySfx(enemyDeath);
    public void PlayEnemyAttack(bool armed) => PlaySfx(armed ? enemyAttackArmed : enemyAttackUnarmed);
    public void PlayHeroDeath() => PlaySfx(heroDeath);
    public void PlayScoreTick() => PlaySfx(scoreTick);

    // UI-звуки идут в отдельный источник (группа UI микшера).
    public void PlayUiClick() => PlayUi(uiClick);
    public void PlayWinUi() => PlayUi(winUi);
    public void PlayLoseUi() => PlayUi(loseUi);

    private void PlaySfx(AudioClip clip)
    {
        if (clip == null) return;
        _sfxSource.PlayOneShot(clip);
    }

    private void PlayUi(AudioClip clip)
    {
        if (clip == null) return;
        _uiSource.PlayOneShot(clip);
    }
}
