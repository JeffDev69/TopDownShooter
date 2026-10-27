using UnityEngine;

// Leest WASD input en beweegt de Rigidbody2D van de player met een constante move speed.
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 movement;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (GetComponent<MobileControls>() == null) gameObject.AddComponent<MobileControls>();
    }

    void Update()
    {
        movement = GameInput.Movement;
    }

    void FixedUpdate()
    {
        rb.linearVelocity = movement * moveSpeed;
    }
}
