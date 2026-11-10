using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

[DefaultExecutionOrder(-200)]
public class MobileControls : MonoBehaviour
{
    private static MobileControls instance;
    private static bool touchUsed;
    public static bool Active => instance != null && instance.isActiveAndEnabled && instance.touchMode;
    public static Vector2 Move => Active ? instance.move : Vector2.zero;
    public static Vector2 Aim => Active ? instance.aim : Vector2.right;
    public static bool Firing => Active && instance.firing;
    public static bool ReloadPressed => Active && instance.reloadFrame == Time.frameCount;
    public static bool SwitchPressed => Active && instance.switchFrame == Time.frameCount;
    public static bool MeleePressed => Active && instance.meleeFrame == Time.frameCount;
    public static bool InteractPressed => Active && instance.interactFrame == Time.frameCount;

    private bool touchMode;
    private Vector2 move;
    private Vector2 aim = Vector2.right;
    private bool firing;
    private int moveFinger = -1, aimFinger = -1;
    private int reloadFrame = -1, switchFrame = -1, meleeFrame = -1, interactFrame = -1;
    private Canvas canvas;
    private Texture2D circleTexture;
    private Sprite circleSprite;
    private RectTransform safeRoot, movePad, aimPad, moveKnob, aimKnob;
    private RectTransform reloadButton, switchButton, meleeButton, interactButton;
    private TextMeshProUGUI healthLabel, coinsLabel, roundsLabel, weaponLabel, rotateLabel;
    private PlayerHealth health;
    private PlayerShoot shooting;
    private ScoreManager score;
    private GameObject[] originalHud;
    private bool[] originalHudActive;
    private int screenWidth, screenHeight;
    private Rect safeArea;
    private const float StickRadius = 68f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic()
    {
        instance = null;
        touchUsed = false;
    }

    private void Awake()
    {
        instance = this;
        health = GetComponent<PlayerHealth>();
        shooting = GetComponent<PlayerShoot>();
        // Schakel de aanraakbediening in op mobiel of nadat een aanraking is gedetecteerd.
        touchMode = Application.isMobilePlatform || touchUsed;
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        if (canvas != null) canvas.gameObject.SetActive(true);
    }

    private void Update()
    {
        if (!touchMode && Touch.activeTouches.Count > 0)
        {
            touchMode = true;
            touchUsed = true;
        }
        if (!touchMode) return;
        if (canvas == null) CreateOverlay();
        if (screenWidth != Screen.width || screenHeight != Screen.height || safeArea != Screen.safeArea)
            LayoutScreen();
        if (Time.timeScale <= 0f)
        {
            ClearInput();
            return;
        }

        bool moveSeen = false, aimSeen = false;
        foreach (Touch touch in Touch.activeTouches)
        {
            Vector2 position = touch.screenPosition;
            bool ended = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
            if (touch.phase == TouchPhase.Began)
            {
                if (Contains(reloadButton, position)) reloadFrame = Time.frameCount;
                else if (Contains(switchButton, position)) switchFrame = Time.frameCount;
                else if (Contains(meleeButton, position)) meleeFrame = Time.frameCount;
                else if (Contains(interactButton, position)) interactFrame = Time.frameCount;
                else if (moveFinger == -1 && Contains(movePad, position)) moveFinger = touch.touchId;
                else if (aimFinger == -1 && Contains(aimPad, position)) aimFinger = touch.touchId;
            }
            if (touch.touchId == moveFinger)
            {
                moveSeen = !ended;
                move = ended ? Vector2.zero : ReadStick(movePad, position);
                moveKnob.anchoredPosition = move * StickRadius;
                if (ended) moveFinger = -1;
            }
            if (touch.touchId == aimFinger)
            {
                aimSeen = !ended;
                Vector2 direction = ended ? Vector2.zero : ReadStick(aimPad, position);
                firing = direction.sqrMagnitude > 0.04f;
                if (firing) aim = direction.normalized;
                aimKnob.anchoredPosition = direction * StickRadius;
                if (ended) aimFinger = -1;
            }
        }

        if (!moveSeen)
        {
            moveFinger = -1;
            move = Vector2.zero;
            moveKnob.anchoredPosition = Vector2.zero;
        }
        if (!aimSeen)
        {
            aimFinger = -1;
            firing = false;
            aimKnob.anchoredPosition = Vector2.zero;
        }
    }

    private void LateUpdate()
    {
        if (!touchMode || canvas == null) return;
        // Onderdelen van de oorspronkelijke HUD.
        if (originalHud == null)
        {
            originalHud = new[] { GameObject.Find("WeaponsPanel"), GameObject.Find("CoinsPanel"),
                GameObject.Find("RoundsPanel"), health != null && health.healthText != null ? health.healthText.gameObject : null };
            originalHudActive = new bool[originalHud.Length];
            for (int i = 0; i < originalHud.Length; i++)
                if (originalHud[i] != null)
                {
                    originalHudActive[i] = originalHud[i].activeSelf;
                    originalHud[i].SetActive(false);
                }
            score = FindFirstObjectByType<ScoreManager>(FindObjectsInactive.Include);
        }
        if (health != null && health.healthText != null) healthLabel.text = health.healthText.text;
        if (CoinManager.Instance != null) coinsLabel.text = "COINS: " + CoinManager.Instance.Coins;
        if (score != null && score.scoreText != null) roundsLabel.text = score.scoreText.text;
        if (shooting != null && shooting.weaponText != null) weaponLabel.text = shooting.weaponText.text;
    }

    private static bool Contains(RectTransform rect, Vector2 position) =>
        RectTransformUtility.RectangleContainsScreenPoint(rect, position);

    private static Vector2 ReadStick(RectTransform pad, Vector2 position)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(pad, position, null, out Vector2 local);
        Vector2 value = Vector2.ClampMagnitude(local / StickRadius, 1f);
        float magnitude = value.magnitude;
        return magnitude < 0.12f ? Vector2.zero : value.normalized * ((magnitude - 0.12f) / 0.88f);
    }

    private void CreateOverlay()
    {
        CreateCircleSprite();
        GameObject root = new GameObject("Mobile Controls", typeof(RectTransform), typeof(Canvas));
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        safeRoot = Rect("Safe Area", root.transform, Vector2.zero, Vector2.zero, Vector2.zero);
        movePad = Stick("Move", new Vector2(0f, 0f), new Vector2(118f, 118f), out moveKnob);
        aimPad = Stick("Aim + Fire", new Vector2(1f, 0f), new Vector2(-118f, 118f), out aimKnob);
        reloadButton = ActionButton("RELOAD", new Vector2(-278f, 64f));
        switchButton = ActionButton("SWAP", new Vector2(-278f, 160f));
        meleeButton = ActionButton("MELEE", new Vector2(-118f, 280f));
        interactButton = ActionButton("USE", new Vector2(-278f, 280f));

        healthLabel = Panel("Health", new Vector2(0f, 1f), new Vector2(114f, -22f), new Vector2(228f, 44f), 26f);
        coinsLabel = Panel("Coins", new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(210f, 44f), 23f);
        roundsLabel = Panel("Rounds", Vector2.one, new Vector2(-143f, -36f), new Vector2(286f, 72f), 23f);
        weaponLabel = Panel("Weapons", new Vector2(0f, 1f), new Vector2(224f, -118f), new Vector2(448f, 104f), 22f);
        weaponLabel.alignment = TextAlignmentOptions.MidlineLeft;
        rotateLabel = Label("Rotate hint", safeRoot, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(650f, 80f), 32f);
        rotateLabel.text = "Turn your phone sideways\n<size=65%>for a better view</size>";
        LayoutScreen();
    }

    private void LayoutScreen()
    {
        ClearInput();
        screenWidth = Screen.width;
        screenHeight = Screen.height;
        safeArea = Screen.safeArea;
        canvas.scaleFactor = Mathf.Max(0.1f, Mathf.Min(safeArea.width / 960f, safeArea.height / 540f));
        safeRoot.anchorMin = new Vector2(safeArea.xMin / Screen.width, safeArea.yMin / Screen.height);
        safeRoot.anchorMax = new Vector2(safeArea.xMax / Screen.width, safeArea.yMax / Screen.height);
        safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
        rotateLabel.gameObject.SetActive(Screen.height > Screen.width);
        Canvas.ForceUpdateCanvases();
    }

    private RectTransform Stick(string name, Vector2 anchor, Vector2 position, out RectTransform knob)
    {
        RectTransform pad = Circle(name, safeRoot, anchor, position, 208f, new Color(0.04f, 0.08f, 0.12f, 0.60f));
        Circle("Rim", pad, Vector2.one * 0.5f, Vector2.zero, 190f, new Color(0.65f, 0.90f, 1f, 0.24f));
        Circle("Center", pad, Vector2.one * 0.5f, Vector2.zero, 180f, new Color(0.04f, 0.08f, 0.12f, 0.55f));
        knob = Circle("Thumb", pad, Vector2.one * 0.5f, Vector2.zero, 64f, new Color(0.58f, 0.86f, 1f, 0.80f));
        TextMeshProUGUI label = Label("Label", pad, Vector2.one * 0.5f, new Vector2(0f, -70f), new Vector2(190f, 28f), 20f);
        label.text = name.ToUpperInvariant();
        return pad;
    }

    private RectTransform ActionButton(string title, Vector2 position)
    {
        RectTransform button = Circle(title, safeRoot, new Vector2(1f, 0f), position, 88f, new Color(0.05f, 0.12f, 0.19f, 0.88f));
        Label("Label", button, Vector2.one * 0.5f, Vector2.zero, new Vector2(82f, 60f), 19f).text = title;
        return button;
    }

    private TextMeshProUGUI Panel(string name, Vector2 anchor, Vector2 position, Vector2 size, float fontSize)
    {
        RectTransform panel = Rect(name, safeRoot, anchor, position, size);
        Image background = panel.gameObject.AddComponent<Image>();
        background.color = new Color(0.90f, 0.95f, 0.98f, 0.90f);
        background.raycastTarget = false;
        TextMeshProUGUI label = Label("Text", panel, Vector2.one * 0.5f, Vector2.zero, size - new Vector2(16f, 6f), fontSize);
        label.color = new Color(0.06f, 0.09f, 0.13f);
        return label;
    }

    private RectTransform Circle(string name, Transform parent, Vector2 anchor, Vector2 position, float diameter, Color color)
    {
        RectTransform rect = Rect(name, parent, anchor, position, Vector2.one * diameter);
        Image graphic = rect.gameObject.AddComponent<Image>();
        graphic.sprite = circleSprite;
        graphic.color = color;
        graphic.raycastTarget = false;
        return rect;
    }

    private void CreateCircleSprite()
    {
        const int size = 128;
        circleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        circleTexture.name = "Touch control circle";
        circleTexture.filterMode = FilterMode.Bilinear;
        circleTexture.wrapMode = TextureWrapMode.Clamp;
        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), Vector2.one * (size * 0.5f));
                byte alpha = (byte)(Mathf.Clamp01(size * 0.5f - 1f - distance) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
        circleTexture.SetPixels32(pixels);
        circleTexture.Apply(false, true);
        circleSprite = Sprite.Create(circleTexture, new Rect(0f, 0f, size, size), Vector2.one * 0.5f);
    }

    private static TextMeshProUGUI Label(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, float fontSize)
    {
        RectTransform rect = Rect(name, parent, anchor, position, size);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.fontSize = fontSize;
        label.enableAutoSizing = true;
        label.fontSizeMin = fontSize * 0.65f;
        label.fontSizeMax = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    private void ClearInput()
    {
        moveFinger = aimFinger = -1;
        reloadFrame = switchFrame = meleeFrame = interactFrame = -1;
        move = Vector2.zero;
        firing = false;
        if (moveKnob != null) moveKnob.anchoredPosition = Vector2.zero;
        if (aimKnob != null) aimKnob.anchoredPosition = Vector2.zero;
    }

    private void OnApplicationFocus(bool focused) { if (!focused) ClearInput(); }
    private void OnApplicationPause(bool paused) { if (paused) ClearInput(); }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
        ClearInput();
        if (canvas != null) canvas.gameObject.SetActive(false);
        if (originalHud != null)
            for (int i = 0; i < originalHud.Length; i++)
                if (originalHud[i] != null) originalHud[i].SetActive(originalHudActive[i]);
        originalHud = null;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        if (canvas != null) Destroy(canvas.gameObject);
        if (circleSprite != null) Destroy(circleSprite);
        if (circleTexture != null) Destroy(circleTexture);
    }
}
