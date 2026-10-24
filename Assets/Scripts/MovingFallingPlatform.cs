using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controla uma plataforma móvel que permanece estática até sofrer contato físico com o jogador.
/// Após o contato, move-se em linha reta com velocidade constante por 5 segundos.
/// Ao final do período, cessa todo o movimento horizontal e cai naturalmente sob ação da gravidade.
/// A ativação ocorre estritamente uma única vez por contato.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class MovingFallingPlatform : MonoBehaviour
{
    [Header("Configurações de Movimento")]
    [Tooltip("Direção retilínea constante do deslocamento.")]
    [SerializeField] private Vector2 moveDirection = Vector2.right;

    [Tooltip("Velocidade constante do deslocamento horizontal.")]
    [SerializeField] private float moveSpeed = 3f;

    [Tooltip("Duração exata do movimento em segundos.")]
    [SerializeField] private float moveDuration = 5.0f;

    [Header("Configurações de Queda")]
    [Tooltip("Gravidade aplicada à plataforma ao término dos 5 segundos.")]
    [SerializeField] private float fallGravityScale = 2.5f;

    [Header("Identificação do Jogador")]
    [Tooltip("Tag para validação do jogador.")]
    [SerializeField] private string playerTag = "Player";

    [Header("Estado em Execução (Leitura)")]
    [SerializeField] private bool hasActivated = false;
    [SerializeField] private bool isMoving = false;
    [SerializeField] private bool isFalling = false;
    [SerializeField] private float timer = 0f;

    private Rigidbody2D rb;
    private Collider2D col;
    private readonly HashSet<Rigidbody2D> riders = new HashSet<Rigidbody2D>();

    public bool HasActivated => hasActivated;
    public bool IsMoving => isMoving;
    public bool IsFalling => isFalling;
    public float ElapsedTime => timer;
    public float RemainingTime => Mathf.Max(0f, moveDuration - timer);

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        // Requisito 1:
        // Configurada para permanecer parada inicialmente, sem sofrer efeito da gravidade.
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        SetVelocity(Vector2.zero);
    }

    private void FixedUpdate()
    {
        if (isMoving)
        {
            // Requisito 4: O movimento dura exatamente 5 segundos, controlados por script.
            timer += Time.fixedDeltaTime;

            if (timer >= moveDuration)
            {
                EndMovementAndStartFalling();
                return;
            }

            // Requisito 3: Move-se em linha reta, em direção definida a velocidade constante.
            Vector2 targetVelocity = moveDirection.normalized * moveSpeed;
            SetVelocity(targetVelocity);

            // Conduz o jogador suavemente caso esteja sobre a plataforma, evitando escorregamento
            Vector2 displacement = targetVelocity * Time.fixedDeltaTime;
            foreach (var rider in riders)
            {
                if (rider != null && rider.bodyType == RigidbodyType2D.Dynamic)
                {
                    rider.position += displacement;
                }
            }
        }
        else if (isFalling)
        {
            // Requisito 5:
            // 1. Parar completamente o movimento horizontal.
            // 2. Passar a sofrer ação da gravidade, caindo naturalmente.
            Vector2 currentVelocity = GetVelocity();
            SetVelocity(new Vector2(0f, currentVelocity.y));
        }
        else
        {
            // Permanece completamente estática antes de qualquer contato.
            SetVelocity(Vector2.zero);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsPlayer(collision.gameObject)) return;

        // Requisito 2 e 6:
        // Permanece imóvel até o jogador entrar em contato físico.
        // A ativação deve ocorrer apenas uma vez por contato (não reinicia se encostar na fase de queda).
        if (!hasActivated)
        {
            hasActivated = true;
            isMoving = true;
            timer = 0f;
        }

        // Se o contato for na parte superior, registra o jogador como passageiro
        if (isMoving && IsPlayerOnTop(collision))
        {
            Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
            if (playerRb != null && !riders.Contains(playerRb))
            {
                riders.Add(playerRb);
            }
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (isMoving && IsPlayer(collision.gameObject))
        {
            Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                if (IsPlayerOnTop(collision))
                {
                    if (!riders.Contains(playerRb)) riders.Add(playerRb);
                }
                else
                {
                    if (riders.Contains(playerRb)) riders.Remove(playerRb);
                }
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (IsPlayer(collision.gameObject))
        {
            Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
            if (playerRb != null && riders.Contains(playerRb))
            {
                riders.Remove(playerRb);
            }
        }
    }

    /// <summary>
    /// Finaliza a fase de movimento horizontal e aplica física dinâmica com gravidade.
    /// </summary>
    private void EndMovementAndStartFalling()
    {
        isMoving = false;
        isFalling = true;
        riders.Clear();

        // Transição para Rigidbody2D dinâmico sob efeito da gravidade
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = fallGravityScale;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Anula a velocidade horizontal imediatamente
        SetVelocity(Vector2.zero);
    }

    private bool IsPlayer(GameObject obj)
    {
        if (obj == null) return false;
        return (!string.IsNullOrEmpty(playerTag) && obj.CompareTag(playerTag)) ||
               obj.GetComponent<PlayerMovement>() != null ||
               obj.name.Equals("Player", System.StringComparison.OrdinalIgnoreCase);
    }

    private bool IsPlayerOnTop(Collision2D collision)
    {
        if (col == null) return collision.transform.position.y > transform.position.y;

        // O centro do jogador deve estar acima do centro da plataforma
        if (collision.transform.position.y <= col.bounds.center.y)
        {
            return false;
        }

        // Verifica pontos de contato na metade superior da plataforma
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.point.y >= col.bounds.center.y - 0.1f)
            {
                return true;
            }
        }

        return collision.transform.position.y > transform.position.y;
    }

    private void SetVelocity(Vector2 velocity)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = velocity;
#else
        rb.velocity = velocity;
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

    private void OnDrawGizmosSelected()
    {
        // Traça linha demonstrativa no Editor indicando a trajetória de 5 segundos
        Gizmos.color = Color.yellow;
        Vector3 start = transform.position;
        Vector3 direction = (Vector3)(moveDirection.normalized * moveSpeed * moveDuration);
        Vector3 end = start + direction;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireCube(end, transform.lossyScale);
    }
}
