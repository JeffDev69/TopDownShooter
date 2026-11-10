using System;
using System.Collections.Generic;
using UnityEngine;

// Centrale audioregelaar voor spelgeluiden en korte UI-geluiden.
public sealed class GameAudio : MonoBehaviour
{
    public enum Cue
    {
        Pistol, Automatic, Shotgun, HeavyGun, Launcher, RayGun, Flamethrower,
        Reload, Empty, Melee, ZombieGroan, EnemyHit, EnemyDeath, PlayerHit, PlayerDeath,
        PowerUp, Purchase, Error, RoundStart, RoundClear, Boss, Explosion
    }

    private const int SampleRate = 22050;
    private static GameAudio instance;
    private readonly Dictionary<Cue, AudioClip> clips = new Dictionary<Cue, AudioClip>();
    private readonly Dictionary<Cue, float> nextAllowed = new Dictionary<Cue, float>();
    private AudioClip[] zombieClips = Array.Empty<AudioClip>();
    private AudioSource effectsSource;
    private AudioSource musicSource;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        EnsureInstance();
    }

    private static GameAudio EnsureInstance()
    {
        if (instance != null) return instance;
        GameObject audioObject = new GameObject("Game Audio");
        instance = audioObject.AddComponent<GameAudio>();
        DontDestroyOnLoad(audioObject);
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        effectsSource = gameObject.AddComponent<AudioSource>();
        effectsSource.playOnAwake = false;
        effectsSource.spatialBlend = 0f;
        effectsSource.volume = 0.72f;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;
        musicSource.loop = true;
        musicSource.volume = 0.13f;

        BuildSoundLibrary();
        musicSource.clip = Resources.Load<AudioClip>("Audio/World/HorrorAmbience");
        if (musicSource.clip != null) musicSource.Play();
    }

    public static void Play(Cue cue, float volume = 1f, float pitch = 1f)
    {
        GameAudio audio = EnsureInstance();
        if (audio.effectsSource == null) return;

        AudioClip clip;
        if ((cue == Cue.ZombieGroan || cue == Cue.EnemyDeath) &&
            audio.zombieClips.Length > 0)
        {
            clip = audio.zombieClips[UnityEngine.Random.Range(0, audio.zombieClips.Length)];
        }
        else if (!audio.clips.TryGetValue(cue, out clip)) return;

        float cooldown = CooldownFor(cue);
        if (audio.nextAllowed.TryGetValue(cue, out float allowedAt) && Time.unscaledTime < allowedAt) return;
        audio.nextAllowed[cue] = Time.unscaledTime + cooldown;

        audio.effectsSource.pitch = Mathf.Clamp(pitch, 0.55f, 1.65f);
        audio.effectsSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        audio.effectsSource.pitch = 1f;
    }

    public static void PlayWeapon(PlayerShoot.WeaponType weapon)
    {
        float weaponTone = (((int)weapon % 7) - 3) * 0.025f;
        float variation = UnityEngine.Random.Range(0.97f, 1.03f) + weaponTone;
        switch (weapon)
        {
            case PlayerShoot.WeaponType.Shotgun:
            case PlayerShoot.WeaponType.DoubleBarrel:
            case PlayerShoot.WeaponType.SPAS12:
            case PlayerShoot.WeaponType.AA12:
                Play(Cue.Shotgun, 1f, variation);
                break;
            case PlayerShoot.WeaponType.Sniper:
            case PlayerShoot.WeaponType.Dragunov:
            case PlayerShoot.WeaponType.BarrettM82:
            case PlayerShoot.WeaponType.Revolver:
            case PlayerShoot.WeaponType.DesertEagle:
                Play(Cue.HeavyGun, 1f, variation);
                break;
            case PlayerShoot.WeaponType.RocketLauncher:
            case PlayerShoot.WeaponType.RPG7:
                Play(Cue.Launcher, 1f, variation);
                break;
            case PlayerShoot.WeaponType.RayGun:
                Play(Cue.RayGun, 0.9f, variation);
                break;
            case PlayerShoot.WeaponType.Flamethrower:
                Play(Cue.Flamethrower, 0.65f, variation);
                break;
            case PlayerShoot.WeaponType.SMG:
            case PlayerShoot.WeaponType.AssaultRifle:
            case PlayerShoot.WeaponType.Minigun:
            case PlayerShoot.WeaponType.BurstRifle:
            case PlayerShoot.WeaponType.Uzi:
            case PlayerShoot.WeaponType.P90:
            case PlayerShoot.WeaponType.M4A1:
            case PlayerShoot.WeaponType.Famas:
            case PlayerShoot.WeaponType.SCARH:
            case PlayerShoot.WeaponType.RPK:
            case PlayerShoot.WeaponType.M60:
            case PlayerShoot.WeaponType.Galil:
                Play(Cue.Automatic, 0.82f, variation);
                break;
            default:
                Play(Cue.Pistol, 0.88f, variation);
                break;
        }
    }

    private static float CooldownFor(Cue cue)
    {
        switch (cue)
        {
            case Cue.EnemyHit: return 0.035f;
            case Cue.EnemyDeath: return 0.08f;
            case Cue.ZombieGroan: return 1.1f;
            case Cue.Flamethrower: return 0.9f;
            case Cue.Empty: return 0.18f;
            default: return 0f;
        }
    }

    private void BuildSoundLibrary()
    {
        AudioClip pistol = Resources.Load<AudioClip>("Audio/Weapons/Pistol");
        AudioClip magnum = Resources.Load<AudioClip>("Audio/Weapons/Magnum");
        AudioClip shotgun = Resources.Load<AudioClip>("Audio/Weapons/Shotgun");
        clips[Cue.Pistol] = pistol;
        clips[Cue.Automatic] = pistol;
        clips[Cue.Shotgun] = shotgun;
        clips[Cue.HeavyGun] = magnum;
        clips[Cue.Launcher] = shotgun;
        clips[Cue.RayGun] = CreateSweep("Ray Gun", 0.22f, 760f, 170f, 0.66f, 0);
        clips[Cue.Flamethrower] = Resources.Load<AudioClip>("Audio/Weapons/Flamethrower");
        clips[Cue.Reload] = Resources.Load<AudioClip>("Audio/Weapons/Reload");
        clips[Cue.Empty] = CreateSweep("Empty", 0.055f, 950f, 620f, 0.34f, 0);
        clips[Cue.Melee] = CreateSweep("Melee", 0.14f, 210f, 75f, 0.55f, 91);
        zombieClips = Resources.LoadAll<AudioClip>("Audio/Zombies/zombies");
        clips[Cue.EnemyHit] = CreateImpact("Enemy Hit", 0.07f, 95f, 0.38f, 103);
        clips[Cue.PlayerHit] = CreateImpact("Player Hit", 0.16f, 72f, 0.7f, 131);
        clips[Cue.PlayerDeath] = CreateSweep("Player Death", 0.75f, 180f, 34f, 0.7f, 149);
        clips[Cue.PowerUp] = CreateArpeggio("Power Up", new[] { 523f, 659f, 784f, 1047f }, 0.12f);
        clips[Cue.Purchase] = CreateArpeggio("Purchase", new[] { 440f, 554f, 659f }, 0.1f);
        clips[Cue.Error] = CreateArpeggio("Error", new[] { 180f, 135f }, 0.13f);
        clips[Cue.RoundStart] = CreateArpeggio("Round Start", new[] { 196f, 247f, 294f, 392f }, 0.16f);
        clips[Cue.RoundClear] = CreateArpeggio("Round Clear", new[] { 392f, 494f, 587f }, 0.18f);
        clips[Cue.Boss] = CreateSweep("Boss", 0.95f, 92f, 34f, 0.85f, 163);
        clips[Cue.Explosion] = Resources.Load<AudioClip>("Audio/Weapons/Explosion");
    }

    private static AudioClip CreateImpact(string name, float duration, float tone, float volume, int seed)
    {
        return CreateClip(name, duration, (i, time, progress) =>
        {
            float envelope = Mathf.Pow(1f - progress, 2.7f);
            float body = Mathf.Sin(2f * Mathf.PI * tone * time) * 0.45f;
            float noise = HashNoise(i, seed) * Mathf.Lerp(0.9f, 0.2f, progress);
            return (body + noise) * envelope * volume;
        });
    }

    private static AudioClip CreateSweep(string name, float duration, float startFrequency,
        float endFrequency, float volume, int seed)
    {
        return CreateClip(name, duration, (i, time, progress) =>
        {
            float frequency = Mathf.Lerp(startFrequency, endFrequency, progress);
            float tone = Mathf.Sin(2f * Mathf.PI * frequency * time);
            float noise = seed == 0 ? 0f : HashNoise(i, seed) * 0.3f;
            return (tone * 0.7f + noise) * Mathf.Pow(1f - progress, 1.4f) * volume;
        });
    }

    private static AudioClip CreateArpeggio(string name, float[] notes, float noteLength)
    {
        float duration = notes.Length * noteLength;
        return CreateClip(name, duration, (i, time, progress) =>
        {
            int note = Mathf.Min(notes.Length - 1, Mathf.FloorToInt(time / noteLength));
            float noteProgress = (time - note * noteLength) / noteLength;
            return Mathf.Sin(2f * Mathf.PI * notes[note] * time) *
                Mathf.Sin(Mathf.PI * noteProgress) * 0.42f;
        });
    }

    private static AudioClip CreateClip(string name, float duration, Func<int, float, float, float> sample)
    {
        int sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * SampleRate));
        float[] data = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float time = (float)i / SampleRate;
            data[i] = Mathf.Clamp(sample(i, time, (float)i / sampleCount), -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static float HashNoise(int sample, int seed)
    {
        unchecked
        {
            uint value = (uint)(sample * 374761393 + seed * 668265263);
            value = (value ^ (value >> 13)) * 1274126177u;
            return ((value ^ (value >> 16)) / (float)uint.MaxValue) * 2f - 1f;
        }
    }
}
