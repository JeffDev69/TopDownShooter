using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class MobileControlsTests
{
    private Touchscreen touchscreen;

    [UnityTest]
    public IEnumerator TouchControlsDriveGameplayAndSurviveRestart()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/game.unity");
        yield return new EnterPlayMode();
        using (new GameplayInputTestScope())
        {
        yield return GameplayInputTestScope.Frames();
        StopEnemies();
        PlayerHealth player = Object.FindFirstObjectByType<PlayerHealth>();
        PlayerShoot shooting = player.GetComponent<PlayerShoot>();
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        player.transform.position = new Vector3(200f, 200f);
        touchscreen = InputSystem.AddDevice<Touchscreen>();
        SendTouch(9, TouchPhase.Began, new Vector2(Screen.width / 2f, Screen.height - 4f));
        yield return GameplayInputTestScope.Frames();
        SendTouch(9, TouchPhase.Ended, new Vector2(Screen.width / 2f, Screen.height - 4f));
        yield return GameplayInputTestScope.Frames();
        Assert.That(MobileControls.Active, Is.True);
        shooting.RefillAllAmmo();
        ScreenCapture.CaptureScreenshot("Logs/mobile-controls-preview.png");

        RectTransform move = Control("Move");
        RectTransform aim = Control("Aim + Fire");
        Vector2 moveCenter = Center(move), aimCenter = Center(aim);
        float scale = move.GetComponentInParent<Canvas>().scaleFactor;
        int ammoBefore = Ammo(shooting);
        SendTouch(1, TouchPhase.Began, moveCenter);
        SendTouch(2, TouchPhase.Began, aimCenter);
        yield return GameplayInputTestScope.Frames();
        SendTouch(1, TouchPhase.Moved, moveCenter + Vector2.right * 80f * scale);
        SendTouch(2, TouchPhase.Moved, aimCenter + Vector2.up * 80f * scale);
        yield return null;
        yield return WaitForGameTime(0.08f);
        Assert.That(body.linearVelocity.x, Is.GreaterThan(4f), "Left thumb moves player");
        Assert.That(body.linearVelocity.magnitude, Is.LessThanOrEqualTo(player.GetComponent<PlayerMovement>().moveSpeed + 0.01f));
        Assert.That(Vector2.Dot(shooting.firePoint.right, Vector2.up), Is.GreaterThan(0.95f), "Right thumb aims upwards");
        yield return WaitForGameTime(0.7f);
        Assert.That(Ammo(shooting), Is.LessThan(ammoBefore - 1), "Holding aim repeats pistol fire");
        SendTouch(1, TouchPhase.Canceled, moveCenter);
        SendTouch(2, TouchPhase.Ended, aimCenter);
        yield return null;
        yield return WaitForGameTime(0.08f);
        Assert.That(MobileControls.Firing, Is.False);
        Assert.That(body.linearVelocity.sqrMagnitude, Is.LessThan(0.001f));
        int ammoReleased = Ammo(shooting);
        yield return WaitForGameTime(0.4f);
        Assert.That(Ammo(shooting), Is.EqualTo(ammoReleased), "Release stops shooting");

        yield return Tap("RELOAD");
        Assert.That(shooting.weaponText.text, Does.Contain("RELOADING"));
        yield return WaitForGameTime(2f);
        Assert.That(Ammo(shooting), Is.EqualTo(ammoBefore));
        shooting.UnlockShotgun();
        Assert.That(shooting.CurrentWeapon, Is.EqualTo(PlayerShoot.WeaponType.Shotgun));
        yield return Tap("SWAP");
        Assert.That(shooting.CurrentWeapon, Is.EqualTo(PlayerShoot.WeaponType.Pistol));
        yield return Tap("MELEE");
        Assert.That(shooting.weaponText.text, Does.Contain("MELEE!"));

        // Exercise purchases through the same on-screen button, not private purchase methods.
        CoinManager.Instance.AddCoins(20000);
        player.GetComponent<PlayerMovement>().enabled = false;
        body.simulated = false;
        foreach (PerkMachine machine in Object.FindObjectsByType<PerkMachine>(FindObjectsSortMode.None))
        {
            yield return Approach(player, machine.machineRenderer);
            Assert.That(machine.GetComponentInChildren<TextMeshPro>().text, Does.Contain("[USE]"));
            yield return Tap("USE");
            Assert.That(player.GetComponent<PlayerPerks>().HasPerk(machine.perkType), Is.True, machine.name);
        }
        HealthMachine heal = Object.FindFirstObjectByType<HealthMachine>();
        player.TakeDamage(2);
        yield return Approach(player, heal.machineRenderer);
        yield return Tap("USE");
        Assert.That(player.currentHealth, Is.EqualTo(player.maxHealth));
        PackAPunchMachine pack = Object.FindFirstObjectByType<PackAPunchMachine>();
        yield return Approach(player, pack.machineRenderer);
        yield return Tap("USE");
        Assert.That(shooting.CurrentPackTier, Is.EqualTo(1));
        MysteryBox mystery = Object.FindFirstObjectByType<MysteryBox>();
        yield return Approach(player, mystery.boxRenderer);
        int coinsBefore = CoinManager.Instance.Coins;
        yield return Tap("USE");
        Assert.That(CoinManager.Instance.Coins, Is.EqualTo(coinsBefore - mystery.price));

        // Death releases both fingers and leaves the existing touch-capable Retry button reachable.
        player.TakeDamage(1000);
        yield return null;
        Assert.That(MobileControls.Active, Is.False);
        Assert.That(player.GameOverUI.activeInHierarchy, Is.True);
        Assert.That(GameObject.Find("Mobile Controls"), Is.Null);
        Object.FindFirstObjectByType<ReloadScene>().ReloadCurrentScene();
        yield return null;
        yield return null;
        StopEnemies();
        Assert.That(MobileControls.Active, Is.True);
        Assert.That(Object.FindObjectsByType<MobileControls>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
        Assert.That(MobileControls.Move, Is.EqualTo(Vector2.zero));
        Assert.That(MobileControls.Firing, Is.False);
        }
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator DesktopInputRemainsAvailable()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/game.unity");
        yield return new EnterPlayMode();
        using (new GameplayInputTestScope())
        {
        // Touchscreen hardware alone must not take over a laptop's mouse controls.
        touchscreen = InputSystem.AddDevice<Touchscreen>();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        yield return GameplayInputTestScope.Frames();
        StopEnemies();
        Assert.That(MobileControls.Active, Is.False);
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
        Mouse mouse = InputSystem.AddDevice<Mouse>();
        try
        {
            PlayerShoot shooting = Object.FindFirstObjectByType<PlayerShoot>();
            shooting.transform.position = new Vector3(200f, 200f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.QueueStateEvent(mouse, new MouseState());
            yield return WaitForGameTime(0.4f);
            shooting.RefillAllAmmo();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.D));
            InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(Screen.width - 30f, Screen.height / 2f) }.WithButton(MouseButton.Left));
            int ammo = Ammo(shooting);
            yield return null;
            yield return WaitForGameTime(0.08f);
            Assert.That(shooting.GetComponent<Rigidbody2D>().linearVelocity.magnitude,
                Is.EqualTo(shooting.GetComponent<PlayerMovement>().moveSpeed).Within(0.01f));
            Assert.That(Ammo(shooting), Is.EqualTo(ammo - 1));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.QueueStateEvent(mouse, new MouseState());
            yield return null;
        }
        finally
        {
            InputSystem.RemoveDevice(keyboard);
            InputSystem.RemoveDevice(mouse);
            InputSystem.RemoveDevice(touchscreen);
            touchscreen = null;
        }
        }
        yield return new ExitPlayMode();
    }

    private static void StopEnemies()
    {
        Object.FindFirstObjectByType<EnemySpawner>().StopAllCoroutines();
        foreach (EnemyHealth enemy in Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
            enemy.gameObject.SetActive(false);
    }

    private static int Ammo(PlayerShoot shooting) =>
        ((int[])typeof(PlayerShoot).GetField("magazineAmmo", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(shooting))[
            (int)typeof(PlayerShoot).GetField("activeSlot", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(shooting)];

    private static RectTransform Control(string name) =>
        GameObject.Find("Mobile Controls/Safe Area/" + name).GetComponent<RectTransform>();
    private static Vector2 Center(RectTransform rect) => RectTransformUtility.WorldToScreenPoint(null, rect.position);
    private void SendTouch(int id, TouchPhase phase, Vector2 position) =>
        InputSystem.QueueStateEvent(touchscreen, new TouchState { touchId = id, phase = phase, position = position });

    private IEnumerator Tap(string button)
    {
        Vector2 position = Center(Control(button));
        SendTouch(3, TouchPhase.Began, position);
        yield return GameplayInputTestScope.Frames();
        SendTouch(3, TouchPhase.Ended, position);
        yield return GameplayInputTestScope.Frames();
    }

    // EditMode tests can enter Play Mode, but their runner does not wait on WaitForSeconds.
    private static IEnumerator WaitForGameTime(float seconds)
    {
        float until = Time.time + seconds;
        while (Time.time < until) yield return null;
    }

    private static IEnumerator Approach(PlayerHealth player, SpriteRenderer machine)
    {
        Bounds bounds = machine.bounds;
        player.transform.position = new Vector3(bounds.center.x, bounds.min.y - 0.5f);
        yield return GameplayInputTestScope.Frames();
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (touchscreen != null && touchscreen.added) InputSystem.RemoveDevice(touchscreen);
        touchscreen = null;
        if (Application.isPlaying) yield return new ExitPlayMode();
    }
}
