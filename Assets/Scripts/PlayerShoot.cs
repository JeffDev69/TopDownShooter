using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using TMPro;

// Regelt twee weapon slots, shooting, ammo, reloads, melee en de weapon HUD text.
public class PlayerShoot : MonoBehaviour
{
    public enum WeaponType
    {
        Empty = -1,
        Pistol = 0,
        Shotgun,
        SMG,
        Sniper,
        AssaultRifle,
        DoubleBarrel,
        Minigun,
        BurstRifle,
        RocketLauncher,
        RayGun,
        Flamethrower,
        Revolver,
        DesertEagle,
        Uzi,
        P90,
        M4A1,
        Famas,
        SCARH,
        SPAS12,
        AA12,
        Dragunov,
        BarrettM82,
        RPK,
        M60,
        Galil,
        RPG7
    }
    public enum WeaponRarity { Common, Rare, Epic, Legendary }
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 0.3f;

    [Header("Weapon UI")]
    public TextMeshProUGUI weaponText;

    [Header("Ammo")]
    public float reloadDuration = 1.5f;
    private int damageMultiplier = 1;
    private float rapidFireScale = 1f;
    // Carry fractional upgrade damage forward instead of rounding every pellet up.
    private readonly float[] damageRemainders = new float[2];
    private readonly int[] magazineAmmo = new int[2];
    private readonly int[] reserveAmmo = new int[2];
    private readonly int[] packTiers = new int[2];
    private readonly WeaponRarity[] weaponRarities = { WeaponRarity.Common, WeaponRarity.Common };
    private int reserveMagazineMultiplier = 5;
    private bool reloading;
    private float reloadEndsAt;
    private float maxAmmoMessageUntil;
    private bool showingMaxAmmo;
    private string powerUpMessage = "MAX AMMO!";

    [Header("Insta-Kill")]
    public float instaKillDuration = 30f;
    private static float instaKillUntil;
    private int displayedInstaKillSeconds;
    public static bool InstaKillActive => Time.time < instaKillUntil;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPowerUps()
    {
        instaKillUntil = 0f;
    }

    [Header("Gun Bash (V or Right Click)")]
    public int meleeDamage = 2;
    public float meleeRange = 1.5f;
    public float meleeRadius = 0.65f;
    public float meleeCooldown = 0.6f;
    private float nextMeleeTime;
    private float meleeMessageUntil;
    private bool showingMelee;

    [Header("Shotgun")]
    [HideInInspector] public bool shotgunUnlocked;
    public int shotgunPellets = 4;
    public float shotgunSpread = 30f;
    public float shotgunFireRate = 0.95f;

    [Header("SMG")]
    public float smgFireRate = 0.18f;
    public float smgSpread = 8f;
    public int smgDamage = 1;

    [Header("Sniper")]
    public float sniperFireRate = 1.55f;
    public int sniperDamage = 6;
    public float sniperSpeedMultiplier = 3f;

    [Header("Assault Rifle")]
    public float assaultRifleFireRate = 0.34f;
    public int assaultRifleDamage = 2;
    public float assaultRifleSpread = 5f;

    [Header("Double Barrel")]
    public int doubleBarrelPellets = 6;
    public float doubleBarrelSpread = 42f;
    public float doubleBarrelFireRate = 1.3f;

    [Header("Minigun")]
    public float minigunFireRate = 0.15f;
    public float minigunSpread = 14f;
    public int minigunDamage = 1;

    [Header("Burst Rifle")]
    public int burstRifleDamage = 2;
    public int burstShotCount = 3;
    public float burstShotDelay = 0.12f;
    public float burstRifleCooldown = 1.05f;

    [Header("Rocket Launcher")]
    public int rocketDamage = 6;
    public float rocketExplosionRadius = 1.8f;
    public float rocketFireRate = 1.8f;
    public float rocketSpeedMultiplier = 0.8f;

    [Header("Ray Gun")]
    public int rayGunDamage = 3;
    public float rayGunFireRate = 0.6f;
    public float rayGunSpeedMultiplier = 2f;

    [Header("Flamethrower")]
    public int flamethrowerDamage = 1;
    public float flamethrowerRange = 4.2f;
    public float flamethrowerConeAngle = 42f;
    public float flamethrowerFireRate = 0.24f;

    [Header("Pack-a-Punch")]
    public int maxPackTier = 3;
    public float packedDamageMultiplier = 1.35f;
    public float packedMagazineMultiplier = 1.2f;
    public float packedFireRateMultiplier = 0.95f;

    private float nextShotTime;
    private bool bursting;
    private GunMuzzleFlash muzzleFlash;
    private FlamethrowerVisual flamethrowerVisual;
    private PlayerAim playerAim;
    private readonly WeaponType[] weaponSlots = { WeaponType.Pistol, WeaponType.Empty };
    private int activeSlot;
    public WeaponType CurrentWeapon => weaponSlots[activeSlot];
    public bool CurrentWeaponPacked => packTiers[activeSlot] > 0;
    public int CurrentPackTier => packTiers[activeSlot];
    public WeaponRarity CurrentWeaponRarity => weaponRarities[activeSlot];
    public string CurrentWeaponFullName => FormatPlainWeapon(activeSlot);
    public string LoadoutText => "1: " + FormatPlainWeapon(0)
        + " | 2: " + FormatPlainWeapon(1);

    public static string GetWeaponName(WeaponType weapon)
    {
        switch (weapon)
        {
            case WeaponType.Pistol: return "M1911";
            case WeaponType.Shotgun: return "REMINGTON 870";
            case WeaponType.SMG: return "MP5";
            case WeaponType.Sniper: return "L96A1";
            case WeaponType.AssaultRifle: return "AK-47";
            case WeaponType.DoubleBarrel: return "DOUBLE BARREL";
            case WeaponType.Minigun: return "M134 MINIGUN";
            case WeaponType.BurstRifle: return "M16A4";
            case WeaponType.RocketLauncher: return "RPG-7";
            case WeaponType.RayGun: return "RAY GUN";
            case WeaponType.Flamethrower: return "FLAMETHROWER";
            case WeaponType.Revolver: return "MAGNUM REVOLVER";
            case WeaponType.DesertEagle: return "DESERT EAGLE";
            case WeaponType.Uzi: return "UZI";
            case WeaponType.P90: return "P90";
            case WeaponType.M4A1: return "M4A1";
            case WeaponType.Famas: return "FAMAS";
            case WeaponType.SCARH: return "SCAR-H";
            case WeaponType.SPAS12: return "SPAS-12";
            case WeaponType.AA12: return "AA-12";
            case WeaponType.Dragunov: return "DRAGUNOV";
            case WeaponType.BarrettM82: return "BARRETT M82";
            case WeaponType.RPK: return "RPK";
            case WeaponType.M60: return "M60";
            case WeaponType.Galil: return "GALIL";
            case WeaponType.RPG7: return "RPG-7";
            default: return "EMPTY";
        }
    }

    void Start()
    {
        playerAim = GetComponent<PlayerAim>();
        muzzleFlash = GetComponent<GunMuzzleFlash>();
        if (muzzleFlash == null) muzzleFlash = gameObject.AddComponent<GunMuzzleFlash>();
        flamethrowerVisual = GetComponent<FlamethrowerVisual>();
        if (flamethrowerVisual == null) flamethrowerVisual = gameObject.AddComponent<FlamethrowerVisual>();
        FillSlotAmmo(0);
        UpdateWeaponText();
    }

    void Update()
    {
        int instaKillSeconds = Mathf.Max(0, Mathf.CeilToInt(instaKillUntil - Time.time));
        if (instaKillSeconds != displayedInstaKillSeconds)
        {
            displayedInstaKillSeconds = instaKillSeconds;
            UpdateWeaponText();
        }
        int previousSlot = activeSlot;
        if (GameInput.SwitchPressed && weaponSlots[1 - activeSlot] != WeaponType.Empty)
            activeSlot = 1 - activeSlot;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
                activeSlot = 0;

            if (Keyboard.current.digit2Key.wasPressedThisFrame && weaponSlots[1] != WeaponType.Empty)
                activeSlot = 1;
        }

        if (previousSlot != activeSlot)
        {
            reloading = false;
            UpdateWeaponText();
        }

        if (reloading && Time.time >= reloadEndsAt)
        {
            int needed = MagazineSizeForSlot(activeSlot) - magazineAmmo[activeSlot];
            int transferred = Mathf.Min(needed, reserveAmmo[activeSlot]);
            magazineAmmo[activeSlot] += transferred;
            reserveAmmo[activeSlot] -= transferred;
            reloading = false;
            GameAudio.Play(GameAudio.Cue.Reload, 0.6f, 1.18f);
            UpdateWeaponText();
        }

        if (GameInput.ReloadPressed)
            BeginReload();

        if (GameInput.MeleePressed)
            GunBash();

        if (showingMelee && Time.time >= meleeMessageUntil)
        {
            showingMelee = false;
            UpdateWeaponText();
        }

        if (showingMaxAmmo && Time.time >= maxAmmoMessageUntil)
        {
            showingMaxAmmo = false;
            UpdateWeaponText();
        }

        bool fireRequested = GameInput.FireRequested(IsAutomaticWeapon(CurrentWeapon));

        if (fireRequested && Time.time >= nextShotTime)
        {
            Shoot();
        }
    }

    void Shoot()
    {
        if (reloading || CurrentWeapon == WeaponType.Empty)
            return;
        if (magazineAmmo[activeSlot] <= 0)
        {
            GameAudio.Play(GameAudio.Cue.Empty);
            nextShotTime = Time.time + 0.2f;
            return;
        }
        if (bulletPrefab == null)
        {
            Debug.LogError("Bullet Prefab is not assigned!");
            return;
        }

        if (firePoint == null)
        {
            Debug.LogError("FirePoint is not assigned!");
            return;
        }

        playerAim?.RefreshWeaponVisual();
        if (IsBurstWeapon(CurrentWeapon))
        {
            if (!bursting) StartCoroutine(FireBurst(activeSlot));
            return;
        }

        if (CurrentWeapon == WeaponType.Flamethrower)
        {
            FireFlamethrower();
            return;
        }

        // A shotgun volley consumes one shell, not one round per pellet.
        magazineAmmo[activeSlot]--;
        UpdateWeaponText();
        if (muzzleFlash != null) muzzleFlash.Show(firePoint, CurrentWeapon);
        GameAudio.PlayWeapon(CurrentWeapon);
        int bulletCount = PelletCount(CurrentWeapon);
        float spread = SpreadFor(CurrentWeapon);
        var ignoredColliders = new List<Collider2D>(GetComponentsInChildren<Collider2D>());

        // Ignore older bullets as well as pellets from this shot.
        foreach (Bullet existingBullet in FindObjectsByType<Bullet>(FindObjectsSortMode.None))
            ignoredColliders.AddRange(existingBullet.GetComponentsInChildren<Collider2D>());

        for (int i = 0; i < bulletCount; i++)
        {
            float angleOffset = RandomSpreadFor(CurrentWeapon);

            if (bulletCount > 1)
            {
                float step = spread / (bulletCount - 1);
                angleOffset = -spread * 0.5f + step * i;
            }

            GameObject projectile = Instantiate(
                bulletPrefab,
                firePoint.position,
                firePoint.rotation * Quaternion.Euler(0f, 0f, angleOffset)
            );

            // Configure this instance, never the shared bullet prefab.
            Bullet bullet = projectile.GetComponent<Bullet>();
            if (bullet != null)
            {
                ConfigureBullet(bullet, CurrentWeapon);
                bullet.damage = ModifiedDamage(bullet.damage, activeSlot);
                if (IsExplosiveWeapon(CurrentWeapon))
                    bullet.explosionDamage = bullet.damage;
            }


            if (IsExplosiveWeapon(CurrentWeapon))
                ProjectileVisuals.Apply(projectile, new Color(1f, 0.35f, 0.04f, 1f), 1.8f, 0.35f);
            else if (CurrentWeapon == WeaponType.RayGun)
                ProjectileVisuals.Apply(projectile, new Color(0.25f, 1f, 0.35f, 1f), 1.35f, 0.22f);
            else if (CurrentWeaponPacked)
                ProjectileVisuals.Apply(projectile, new Color(0.95f, 0.25f, 1f, 1f), 1.2f, 0.18f);

            Collider2D[] projectileColliders = projectile.GetComponentsInChildren<Collider2D>();
            foreach (Collider2D projectileCollider in projectileColliders)
            {
                foreach (Collider2D ignoredCollider in ignoredColliders)
                {
                    if (ignoredCollider != null && ignoredCollider != projectileCollider)
                        Physics2D.IgnoreCollision(projectileCollider, ignoredCollider);
                }
            }
            ignoredColliders.AddRange(projectileColliders);
        }

        float delay = FireDelayFor(CurrentWeapon);
        delay *= FireRateScale(activeSlot);
        nextShotTime = Time.time + Mathf.Max(0.05f, delay);
    }

    private IEnumerator FireBurst(int slot)
    {
        bursting = true;
        WeaponType burstWeapon = weaponSlots[slot];
        float cooldown = FireDelayFor(burstWeapon) * FireRateScale(slot);
        nextShotTime = Time.time + Mathf.Max(0.1f, cooldown);
        int shots = Mathf.Max(1, burstShotCount);

        for (int i = 0; i < shots; i++)
        {
            if (activeSlot != slot || weaponSlots[slot] != burstWeapon ||
                reloading || magazineAmmo[slot] <= 0) break;

            playerAim?.RefreshWeaponVisual();
            magazineAmmo[slot]--;
            UpdateWeaponText();
            if (muzzleFlash != null) muzzleFlash.Show(firePoint, weaponSlots[slot]);
            GameAudio.PlayWeapon(weaponSlots[slot]);

            GameObject projectile = Instantiate(bulletPrefab, firePoint.position,
                firePoint.rotation * Quaternion.Euler(0f, 0f, RandomSpreadFor(burstWeapon)));
            Bullet bullet = projectile.GetComponent<Bullet>();
            if (bullet != null)
                bullet.damage = ModifiedDamage(BurstDamageFor(weaponSlots[slot]), slot);
            if (packTiers[slot] > 0)
                ProjectileVisuals.Apply(projectile, new Color(0.95f, 0.25f, 1f, 1f), 1.2f, 0.18f);
            IgnoreProjectileCollisions(projectile);

            if (i < shots - 1)
                yield return new WaitForSeconds(Mathf.Max(0.02f,
                    burstShotDelay * (burstWeapon == WeaponType.Famas ? 0.7f : 1f) * FireRateScale(slot)));
        }

        bursting = false;
    }

    private void FireFlamethrower()
    {
        magazineAmmo[activeSlot]--;
        float delay = flamethrowerFireRate;
        delay *= FireRateScale(activeSlot);
        nextShotTime = Time.time + Mathf.Max(0.05f, delay);
        UpdateWeaponText();
        GameAudio.PlayWeapon(CurrentWeapon);

        Vector2 origin = firePoint.position;
        Vector2 direction = ((Vector2)firePoint.right).normalized;
        float range = Mathf.Max(0.5f, flamethrowerRange);
        float halfAngle = Mathf.Clamp(flamethrowerConeAngle * 0.5f, 5f, 80f);
        float minimumDot = Mathf.Cos(halfAngle * Mathf.Deg2Rad);
        var damagedEnemies = new HashSet<EnemyHealth>();
        int tickDamage = ModifiedDamage(flamethrowerDamage, activeSlot);

        foreach (Collider2D target in Physics2D.OverlapCircleAll(origin + direction * range * 0.5f, range * 0.7f))
        {
            EnemyHealth enemy = target.GetComponentInParent<EnemyHealth>();
            if (enemy == null || !damagedEnemies.Add(enemy)) continue;

            Vector2 toEnemy = (Vector2)enemy.transform.position - origin;
            float distance = toEnemy.magnitude;
            if (distance > range || distance < 0.01f || Vector2.Dot(direction, toEnemy / distance) < minimumDot)
                continue;
            if (MeleeBlocked(origin, target.ClosestPoint(origin))) continue;

            enemy.TakeDamage(tickDamage);
        }

        if (flamethrowerVisual != null)
            flamethrowerVisual.Show(firePoint, range, flamethrowerConeAngle);
    }

    private void IgnoreProjectileCollisions(GameObject projectile)
    {
        Collider2D[] projectileColliders = projectile.GetComponentsInChildren<Collider2D>();
        foreach (Collider2D projectileCollider in projectileColliders)
        {
            foreach (Collider2D playerCollider in GetComponentsInChildren<Collider2D>())
                Physics2D.IgnoreCollision(projectileCollider, playerCollider);

            foreach (Bullet existingBullet in FindObjectsByType<Bullet>(FindObjectsSortMode.None))
            {
                if (existingBullet.gameObject == projectile) continue;
                foreach (Collider2D otherBulletCollider in existingBullet.GetComponentsInChildren<Collider2D>())
                    Physics2D.IgnoreCollision(projectileCollider, otherBulletCollider);
            }
        }
    }

    public void UnlockShotgun()
    {
        GiveWeapon(WeaponType.Shotgun);
    }

    // Public so a UI button can also call this attack later.
    public void GunBash()
    {
        if (!isActiveAndEnabled || firePoint == null || Time.time < nextMeleeTime) return;
        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health != null && health.currentHealth <= 0) return;

        nextMeleeTime = Time.time + Mathf.Max(0.1f, meleeCooldown);
        nextShotTime = Mathf.Max(nextShotTime, Time.time + 0.25f);
        reloading = false;
        showingMelee = true;
        GameAudio.Play(GameAudio.Cue.Melee, 0.8f, Random.Range(0.94f, 1.06f));
        meleeMessageUntil = Time.time + 0.25f;
        UpdateWeaponText();

        Vector2 origin = transform.position;
        Vector2 direction = ((Vector2)firePoint.right).normalized;
        float range = Mathf.Max(0.2f, meleeRange);
        float radius = Mathf.Clamp(meleeRadius, 0.1f, range * 0.5f);
        Vector2 center = origin + direction * (range - radius);
        var hitEnemies = new HashSet<EnemyHealth>();
        foreach (Collider2D target in Physics2D.OverlapCircleAll(center, radius))
        {
            EnemyHealth enemy = target.GetComponentInParent<EnemyHealth>();
            if (enemy == null || hitEnemies.Contains(enemy)) continue;
            Vector2 hitPoint = target.ClosestPoint(origin);
            if (Vector2.Dot(hitPoint - origin, direction) < 0f) continue;
            if (MeleeBlocked(origin, hitPoint)) continue;
            hitEnemies.Add(enemy);
            enemy.TakeDamage(Mathf.Max(1, meleeDamage));
        }
    }

    private bool MeleeBlocked(Vector2 origin, Vector2 target)
    {
        foreach (RaycastHit2D hit in Physics2D.LinecastAll(origin, target))
        {
            Collider2D collider = hit.collider;
            if (collider == null || collider.isTrigger || collider.transform.IsChildOf(transform)) continue;
            if (collider.GetComponentInParent<EnemyHealth>() != null ||
                collider.GetComponentInParent<Bullet>() != null) continue;
            return true;
        }
        return false;
    }

    public WeaponType GiveRandomMysteryWeapon()
    {
        WeaponType reward = (WeaponType)Random.Range(1, (int)WeaponType.RPG7 + 1);
        GiveWeapon(reward, RollRarity());
        return reward;
    }

    private void GiveWeapon(WeaponType weapon)
    {
        GiveWeapon(weapon, WeaponRarity.Common);
    }

    private void GiveWeapon(WeaponType weapon, WeaponRarity rarity)
    {
        // First reward fills slot two. Later rewards replace only the held gun.
        if (weaponSlots[1] == WeaponType.Empty)
            activeSlot = 1;

        weaponSlots[activeSlot] = weapon;
        packTiers[activeSlot] = 0;
        weaponRarities[activeSlot] = rarity;
        damageRemainders[activeSlot] = 0f;
        reloading = false;
        FillSlotAmmo(activeSlot);
        UpdateWeaponText();
    }

    private bool IsBurstWeapon(WeaponType weapon)
    {
        return weapon == WeaponType.BurstRifle || weapon == WeaponType.Famas;
    }

    private static bool IsAutomaticWeapon(WeaponType weapon)
    {
        switch (weapon)
        {
            case WeaponType.SMG:
            case WeaponType.AssaultRifle:
            case WeaponType.Minigun:
            case WeaponType.Flamethrower:
            case WeaponType.Uzi:
            case WeaponType.P90:
            case WeaponType.M4A1:
            case WeaponType.SCARH:
            case WeaponType.AA12:
            case WeaponType.RPK:
            case WeaponType.M60:
            case WeaponType.Galil:
                return true;
            default:
                return false;
        }
    }

    private bool IsExplosiveWeapon(WeaponType weapon)
    {
        return weapon == WeaponType.RocketLauncher || weapon == WeaponType.RPG7;
    }

    private int PelletCount(WeaponType weapon)
    {
        switch (weapon)
        {
            case WeaponType.Shotgun:
            case WeaponType.SPAS12:
                return Mathf.Max(1, shotgunPellets);
            case WeaponType.DoubleBarrel:
                return Mathf.Max(1, doubleBarrelPellets);
            case WeaponType.AA12:
                return 4;
            default:
                return 1;
        }
    }

    private float SpreadFor(WeaponType weapon)
    {
        switch (weapon)
        {
            case WeaponType.DoubleBarrel:
                return doubleBarrelSpread;
            case WeaponType.SPAS12:
                return shotgunSpread * 0.85f;
            case WeaponType.AA12:
                return shotgunSpread * 1.1f;
            default:
                return shotgunSpread;
        }
    }

    private float RandomSpreadFor(WeaponType weapon)
    {
        float spread = 0f;
        switch (weapon)
        {
            case WeaponType.SMG:
                spread = smgSpread;
                break;
            case WeaponType.Uzi:
                spread = smgSpread * 1.5f;
                break;
            case WeaponType.P90:
                spread = smgSpread * 0.9f;
                break;
            case WeaponType.AssaultRifle:
            case WeaponType.M4A1:
            case WeaponType.Famas:
            case WeaponType.SCARH:
            case WeaponType.Galil:
            case WeaponType.BurstRifle:
                spread = assaultRifleSpread;
                break;
            case WeaponType.Minigun:
            case WeaponType.RPK:
            case WeaponType.M60:
                spread = minigunSpread;
                break;
        }

        return spread <= 0f ? 0f : Random.Range(-Mathf.Abs(spread) * 0.5f, Mathf.Abs(spread) * 0.5f);
    }

    private void ConfigureBullet(Bullet bullet, WeaponType weapon)
    {
        switch (weapon)
        {
            case WeaponType.Pistol:
                bullet.damage = 1;
                break;
            case WeaponType.Shotgun:
            case WeaponType.SPAS12:
            case WeaponType.DoubleBarrel:
            case WeaponType.AA12:
                bullet.damage = 1;
                // Pellets are close-range crowd control, with no map-wide reach.
                float range = weapon == WeaponType.AA12 ? 6f :
                    weapon == WeaponType.DoubleBarrel ? 6.5f :
                    weapon == WeaponType.SPAS12 ? 8f : 7.5f;
                bullet.lifeTime = Mathf.Min(bullet.lifeTime, range / Mathf.Max(0.1f, bullet.speed));
                break;
            case WeaponType.SMG:
            case WeaponType.Uzi:
            case WeaponType.P90:
                bullet.damage = Mathf.Max(1, smgDamage);
                break;
            case WeaponType.AssaultRifle:
            case WeaponType.M4A1:
            case WeaponType.Galil:
                bullet.damage = Mathf.Max(1, assaultRifleDamage);
                break;
            case WeaponType.SCARH:
                bullet.damage = Mathf.Max(1, assaultRifleDamage + 1);
                break;
            case WeaponType.Minigun:
            case WeaponType.RPK:
                bullet.damage = Mathf.Max(1, minigunDamage);
                break;
            case WeaponType.M60:
                bullet.damage = Mathf.Max(1, minigunDamage + 1);
                break;
            case WeaponType.RocketLauncher:
            case WeaponType.RPG7:
                bullet.damage = Mathf.Max(1, rocketDamage);
                bullet.speed *= Mathf.Max(0.1f, rocketSpeedMultiplier);
                bullet.explosionRadius = Mathf.Max(0.2f, rocketExplosionRadius);
                break;
            case WeaponType.RayGun:
                bullet.damage = Mathf.Max(1, rayGunDamage);
                bullet.speed *= Mathf.Max(0.1f, rayGunSpeedMultiplier);
                break;
            case WeaponType.Sniper:
                bullet.damage = Mathf.Max(1, sniperDamage);
                bullet.speed *= Mathf.Max(0.1f, sniperSpeedMultiplier);
                break;
            case WeaponType.BarrettM82:
                bullet.damage = Mathf.Max(1, sniperDamage + 1);
                bullet.speed *= Mathf.Max(0.1f, sniperSpeedMultiplier);
                break;
            case WeaponType.Dragunov:
                bullet.damage = Mathf.Max(1, sniperDamage - 2);
                bullet.speed *= Mathf.Max(0.1f, sniperSpeedMultiplier * 0.9f);
                break;
            case WeaponType.Revolver:
                bullet.damage = 3;
                break;
            case WeaponType.DesertEagle:
                bullet.damage = 4;
                break;
            default:
                bullet.damage = Mathf.Max(1, bullet.damage);
                break;
        }
    }

    private float FireDelayFor(WeaponType weapon)
    {
        switch (weapon)
        {
            case WeaponType.Shotgun: return shotgunFireRate;
            case WeaponType.SPAS12: return 0.82f;
            case WeaponType.AA12: return 0.55f;
            case WeaponType.SMG: return smgFireRate;
            case WeaponType.Uzi: return 0.15f;
            case WeaponType.P90: return 0.17f;
            case WeaponType.Sniper: return sniperFireRate;
            case WeaponType.Dragunov: return 1.05f;
            case WeaponType.BarrettM82: return 1.9f;
            case WeaponType.AssaultRifle: return assaultRifleFireRate;
            case WeaponType.M4A1: return 0.3f;
            case WeaponType.SCARH: return 0.5f;
            case WeaponType.Galil: return 0.34f;
            case WeaponType.DoubleBarrel: return doubleBarrelFireRate;
            case WeaponType.Minigun: return minigunFireRate;
            case WeaponType.RPK: return 0.19f;
            case WeaponType.M60: return 0.38f;
            case WeaponType.BurstRifle: return burstRifleCooldown;
            case WeaponType.Famas: return burstRifleCooldown * 0.9f;
            case WeaponType.RocketLauncher:
            case WeaponType.RPG7: return rocketFireRate;
            case WeaponType.RayGun: return rayGunFireRate;
            case WeaponType.Revolver: return 0.72f;
            case WeaponType.DesertEagle: return 0.95f;
            default: return fireRate;
        }
    }

    private int BurstDamageFor(WeaponType weapon)
    {
        return Mathf.Max(1, burstRifleDamage);
    }

    private static int MagazineSize(WeaponType weapon)
    {
        switch (weapon)
        {
            case WeaponType.Pistol: return 12;
            case WeaponType.Shotgun: return 6;
            case WeaponType.SMG: return 30;
            case WeaponType.Sniper: return 5;
            case WeaponType.AssaultRifle: return 30;
            case WeaponType.DoubleBarrel: return 2;
            case WeaponType.Minigun: return 80;
            case WeaponType.BurstRifle: return 24;
            case WeaponType.RocketLauncher: return 2;
            case WeaponType.RayGun: return 12;
            case WeaponType.Flamethrower: return 40;
            case WeaponType.Revolver: return 6;
            case WeaponType.DesertEagle: return 7;
            case WeaponType.Uzi: return 32;
            case WeaponType.P90: return 50;
            case WeaponType.M4A1: return 30;
            case WeaponType.Famas: return 30;
            case WeaponType.SCARH: return 20;
            case WeaponType.SPAS12: return 8;
            case WeaponType.AA12: return 10;
            case WeaponType.Dragunov: return 10;
            case WeaponType.BarrettM82: return 5;
            case WeaponType.RPK: return 60;
            case WeaponType.M60: return 75;
            case WeaponType.Galil: return 35;
            case WeaponType.RPG7: return 1;
            default: return 0;
        }
    }

    private void FillSlotAmmo(int slot)
    {
        int capacity = MagazineSizeForSlot(slot);
        magazineAmmo[slot] = capacity;
        reserveAmmo[slot] = capacity * reserveMagazineMultiplier;
    }

    private void BeginReload()
    {
        if (reloading || bursting || reserveAmmo[activeSlot] <= 0 ||
            magazineAmmo[activeSlot] >= MagazineSizeForSlot(activeSlot)) return;
        reloading = true;
        reloadEndsAt = Time.time + Mathf.Max(0.1f, reloadDuration * ReloadScaleFor(CurrentWeapon));
        GameAudio.Play(GameAudio.Cue.Reload, 0.75f, 0.92f);
        UpdateWeaponText();
    }

    public bool CanPackCurrentWeapon()
    {
        return CurrentWeapon != WeaponType.Empty && CurrentPackTier < Mathf.Max(1, maxPackTier);
    }

    public bool PackCurrentWeapon()
    {
        if (!CanPackCurrentWeapon())
            return false;

        packTiers[activeSlot]++;
        reloading = false;
        FillSlotAmmo(activeSlot);
        powerUpMessage = "PACK-A-PUNCH TIER " + CurrentPackTier + "!";
        showingMaxAmmo = true;
        maxAmmoMessageUntil = Time.time + 2f;
        UpdateWeaponText();
        return true;
    }

    public void RefillAllAmmo()
    {
        reloading = false;
        for (int slot = 0; slot < weaponSlots.Length; slot++) FillSlotAmmo(slot);
        powerUpMessage = "MAX AMMO!";
        showingMaxAmmo = true;
        maxAmmoMessageUntil = Time.time + 2f;
        UpdateWeaponText();
    }

    public void ApplyFastReloadPerk()
    {
        reloadDuration = Mathf.Max(0.25f, reloadDuration * 0.5f);
    }

    public void ApplyDoubleDamagePerk()
    {
        damageMultiplier = 2;
    }

    public void ApplyRapidFirePerk()
    {
        // 25% more shots per second, including variants with fixed base delays.
        rapidFireScale = 1f / 1.25f;
    }

    public void ApplyAmmoBeltPerk()
    {
        reserveMagazineMultiplier = Mathf.Max(reserveMagazineMultiplier, 8);
        RefillAllAmmo();
    }

    public void ActivateInstaKill()
    {
        // A second pickup refreshes the duration instead of stacking unlimited time.
        instaKillUntil = Time.time + Mathf.Max(1f, instaKillDuration);
        displayedInstaKillSeconds = Mathf.CeilToInt(instaKillDuration);
        UpdateWeaponText();
    }

    public void DetonateNuke()
    {
        foreach (EnemyHealth enemy in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
            enemy.KillFromNuke();
        powerUpMessage = "NUKE!";
        showingMaxAmmo = true;
        maxAmmoMessageUntil = Time.time + 2f;
        UpdateWeaponText();
    }

    private void UpdateWeaponText()
    {
        if (weaponText == null)
            return;

        weaponText.richText = true;
        weaponText.color = Color.white;
        weaponText.margin = Vector4.zero;
        weaponText.alignment = TextAlignmentOptions.MidlineLeft;
        weaponText.enableAutoSizing = true;
        weaponText.fontSizeMin = 18f;
        weaponText.fontSizeMax = 36f;
        weaponText.text = FormatWeaponSlot(0) + "\n" + FormatWeaponSlot(1);
        if (showingMaxAmmo)
            weaponText.text += "\n<size=75%><color=#176B35><b>" + powerUpMessage + "</b></color></size>";
        if (InstaKillActive)
            weaponText.text += "\n<size=75%><color=#9B2323><b>INSTA-KILL: "
                + Mathf.CeilToInt(instaKillUntil - Time.time) + "s</b></color></size>";

    }

    private string FormatWeaponSlot(int slot)
    {
        bool selected = slot == activeSlot;
        string color = selected ? "#17202E" : "#535969";
        string marker = selected ? "> " : "  ";
        string label = marker + "[" + (slot + 1) + "]  " + FormatColoredWeapon(slot);
        if (weaponSlots[slot] != WeaponType.Empty)
        {
            label += "  " + magazineAmmo[slot] + "/" + reserveAmmo[slot];
            if (selected && showingMelee) label += "  MELEE!";
            else if (selected && reloading) label += "  RELOADING";
            else if (selected && magazineAmmo[slot] == 0)
                label += reserveAmmo[slot] > 0
                    ? (MobileControls.Active ? "  RELOAD" : "  [R] RELOAD")
                    : (MobileControls.Active ? "  MELEE" : "  [V] MELEE");
        }
        return "<color=" + color + ">" + (selected ? "<b>" + label + "</b>" : label) + "</color>";
    }

    private int MagazineSizeForSlot(int slot)
    {
        int baseSize = MagazineSize(weaponSlots[slot]);
        if (packTiers[slot] <= 0)
            return baseSize;

        float perTierBonus = Mathf.Max(0f, packedMagazineMultiplier - 1f);
        // A whole-number bonus such as 80 * 1.6 must not gain a round from float error.
        return Mathf.Max(baseSize + 1, Mathf.CeilToInt(baseSize * (1f + perTierBonus * packTiers[slot]) - 0.0001f));
    }

    private int ModifiedDamage(int baseDamage, int slot)
    {
        float multiplier = damageMultiplier;
        multiplier *= RarityDamageMultiplier(weaponRarities[slot]);
        if (packTiers[slot] > 0)
            multiplier *= 1f + Mathf.Max(0.1f, packedDamageMultiplier - 1f) * packTiers[slot];

        float exactDamage = Mathf.Max(1, baseDamage) * multiplier + damageRemainders[slot];
        int damage = Mathf.Max(1, Mathf.FloorToInt(exactDamage + 0.00001f));
        damageRemainders[slot] = Mathf.Max(0f, exactDamage - damage);
        return damage;
    }

    private float FireRateScale(int slot)
    {
        if (packTiers[slot] <= 0)
            return rapidFireScale;

        return rapidFireScale * Mathf.Pow(Mathf.Clamp(packedFireRateMultiplier, 0.35f, 1f), packTiers[slot]);
    }

    private static float ReloadScaleFor(WeaponType weapon)
    {
        switch (weapon)
        {
            case WeaponType.Minigun: return 2.8f;
            case WeaponType.RPK:
            case WeaponType.M60: return 2.4f;
            case WeaponType.RocketLauncher:
            case WeaponType.RPG7:
            case WeaponType.Flamethrower: return 2f;
            case WeaponType.AA12:
            case WeaponType.Sniper:
            case WeaponType.BarrettM82: return 1.6f;
            case WeaponType.Shotgun:
            case WeaponType.SPAS12:
            case WeaponType.Dragunov:
            case WeaponType.AssaultRifle:
            case WeaponType.M4A1:
            case WeaponType.Galil:
            case WeaponType.SCARH:
            case WeaponType.BurstRifle:
            case WeaponType.Famas: return 1.4f;
            case WeaponType.SMG:
            case WeaponType.Uzi:
            case WeaponType.P90:
            case WeaponType.RayGun:
            case WeaponType.DesertEagle: return 1.2f;
            default: return 1f;
        }
    }

    private static WeaponRarity RollRarity()
    {
        float roll = Random.value;
        if (roll < 0.08f) return WeaponRarity.Legendary;
        if (roll < 0.23f) return WeaponRarity.Epic;
        if (roll < 0.53f) return WeaponRarity.Rare;
        return WeaponRarity.Common;
    }

    private static float RarityDamageMultiplier(WeaponRarity rarity)
    {
        switch (rarity)
        {
            case WeaponRarity.Rare: return 1.1f;
            case WeaponRarity.Epic: return 1.2f;
            case WeaponRarity.Legendary: return 1.35f;
            default: return 1f;
        }
    }

    private static string RarityName(WeaponRarity rarity)
    {
        return rarity.ToString().ToUpperInvariant();
    }

    private static string RarityColor(WeaponRarity rarity)
    {
        switch (rarity)
        {
            case WeaponRarity.Rare: return "#2677FF";
            case WeaponRarity.Epic: return "#A335EE";
            case WeaponRarity.Legendary: return "#FF9F1C";
            default: return "#535969";
        }
    }

    private string FormatPlainWeapon(int slot)
    {
        if (weaponSlots[slot] == WeaponType.Empty)
            return GetWeaponName(WeaponType.Empty);

        string prefix = weaponRarities[slot] == WeaponRarity.Common ? "" : RarityName(weaponRarities[slot]) + " ";
        string suffix = packTiers[slot] > 0 ? " PAP " + packTiers[slot] : "";
        return prefix + GetWeaponName(weaponSlots[slot]) + suffix;
    }

    private string FormatColoredWeapon(int slot)
    {
        string label = FormatPlainWeapon(slot);
        if (weaponSlots[slot] == WeaponType.Empty)
            return label;

        return "<color=" + RarityColor(weaponRarities[slot]) + ">" + label + "</color>";
    }
}
