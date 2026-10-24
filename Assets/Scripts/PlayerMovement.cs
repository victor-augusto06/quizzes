using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerMovement : MonoBehaviour
{
    [Header("Configurações de Movimento")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpForce = 11f;

    [Header("Assistência de Pulo")]
    [SerializeField] private float jumpBufferTime = 0.2f;
    [SerializeField] private float coyoteTime = 0.15f;

    [Header("Detecção de Chão")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.25f;
    [SerializeField] private LayerMask groundLayer = ~0;

    [Header("Debug")]
    [SerializeField] private bool isGrounded;

    private Rigidbody2D rb;
    private Collider2D col;
    private float moveInput;
    private float jumpBufferCounter;
    private float coyoteTimeCounter;
    private bool isCollidingGround;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    private void Update()
    {
        // 1. Leitura de Entrada Horizontal
        moveInput = 0f;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                moveInput -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                moveInput += 1f;

            if (Keyboard.current.spaceKey.wasPressedThisFrame ||
                Keyboard.current.wKey.wasPressedThisFrame ||
                Keyboard.current.upArrowKey.wasPressedThisFrame)
            {
                jumpBufferCounter = jumpBufferTime;
            }
        }

        if (Gamepad.current != null)
        {
            float stickX = Gamepad.current.leftStick.x.ReadValue();
            if (Mathf.Abs(stickX) > 0.2f)
                moveInput = Mathf.Sign(stickX);

            if (Gamepad.current.buttonSouth.wasPressedThisFrame ||
                Gamepad.current.dpad.up.wasPressedThisFrame)
            {
                jumpBufferCounter = jumpBufferTime;
            }
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Mathf.Approximately(moveInput, 0f))
        {
            moveInput = Input.GetAxisRaw("Horizontal");
        }
        if (Input.GetButtonDown("Jump") ||
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.W) ||
            Input.GetKeyDown(KeyCode.UpArrow))
        {
            jumpBufferCounter = jumpBufferTime;
        }
#endif

        // 2. Atualizar estado do chão
        UpdateGroundStatus();

        // 3. Gerenciar Coyote Time
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        // 4. Reduzir contador do Jump Buffer
        if (jumpBufferCounter > 0f)
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // 5. Executar pulo se houver buffer de input e o personagem puder pular
        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
        {
            ExecuteJump();
        }
    }

    private void FixedUpdate()
    {
        // Aplicação de velocidade horizontal na física
        Vector2 currentVel = GetVelocity();
        SetVelocity(moveInput * moveSpeed, currentVel.y);
    }

    private void UpdateGroundStatus()
    {
        bool grounded = false;

        // Método A: OverlapCircleAll (ignora o próprio player e filhos)
        if (groundCheck != null)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(groundCheck.position, groundCheckRadius, groundLayer);
            foreach (var hit in hits)
            {
                if (hit != null && hit.gameObject != gameObject && !hit.transform.IsChildOf(transform) && !hit.isTrigger)
                {
                    grounded = true;
                    break;
                }
            }
        }

        // Método B: Cast para baixo a partir do collider do Player (garante chão mesmo se groundCheck falhar)
        if (!grounded && col != null)
        {
            RaycastHit2D[] castHits = new RaycastHit2D[4];
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(groundLayer);
            filter.useTriggers = false;
            int hitCount = col.Cast(Vector2.down, filter, castHits, 0.08f);
            for (int i = 0; i < hitCount; i++)
            {
                if (castHits[i].collider != null &&
                    castHits[i].collider.gameObject != gameObject &&
                    !castHits[i].collider.transform.IsChildOf(transform))
                {
                    grounded = true;
                    break;
                }
            }
        }

        // Método C: Contato físico via OnCollisionStay2D
        if (!grounded && isCollidingGround)
        {
            grounded = true;
        }

        isGrounded = grounded;
    }

    private void ExecuteJump()
    {
        Vector2 vel = GetVelocity();
        SetVelocity(vel.x, jumpForce);
        jumpBufferCounter = 0f;
        coyoteTimeCounter = 0f;
        isGrounded = false;
        isCollidingGround = false;
    }

    private void SetVelocity(float x, float y)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = new Vector2(x, y);
#else
        rb.velocity = new Vector2(x, y);
#endif
    }

    private Vector2 GetVelocity()
    {
#if UNITY_6000_0_OR_NEWER
        return rb.linearVelocity;
#else
        return rb.velocity;
#endif
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                isCollidingGround = true;
                return;
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        isCollidingGround = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
