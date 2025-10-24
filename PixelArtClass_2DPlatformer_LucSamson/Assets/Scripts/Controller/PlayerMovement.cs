using UnityEngine.InputSystem;
using UnityEngine;

// D�finition une liste d'�tat possible (marche, course, ou dans les airs)
public enum MovementState
{
    Walking,
    Running,
    Aerial
}

// Cet attribut rend obligatoire la pr�sence d'un composant Rigidbody2D sur l'objet
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private PlayerState playerInfo;
    private Rigidbody2D body;

    [Header("Current State")]
    public MovementState currentState = MovementState.Walking;

    [Header("Movement Settings")]
    public MoveSettings walkSettings = new MoveSettings(10, 40, 40, 60);
    public MoveSettings runSettings = new MoveSettings(15, 60, 60, 40);
    public MoveSettings airSettings = new MoveSettings(8, 20, 20, 40);

    [Header("Calcul Interne")]
    [SerializeField] private float velocityX;
    [SerializeField] private float desiredvelocityX;

    [Header("Input Stick")]
    [SerializeField] private bool isMoving;
    [SerializeField] private float horizontalInput;

    [Header("Input Sprint")]
    [SerializeField] private bool isToggleSprint;
    [SerializeField] private bool wantToSprint;

    private void Awake()
    {
        // Recherche du Rigidbody2D sur l'objet (sert � appliquer la physique : vitesse, gravit�...)
        body = GetComponent<Rigidbody2D>();
    }
    private void Update()
    {
        // Rafra�chit l'�tat actuel du joueur (marche, course...)
        CheckCurrentState();
        // G�re l'orientation du personnage (droite/gauche)
        UpdateLookDirection();
    }
    private void FixedUpdate()
    {
        // Calcul et applique la v�locit� physique
        ComputeVelocity();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (!playerInfo.CharacterCanMove)
        {
            horizontalInput = 0;
            velocityX = 0;
            return;
        }

        horizontalInput = context.ReadValue<Vector2>().x;
        isMoving = horizontalInput != 0;
    }
    public void OnSprint(InputAction.CallbackContext context)
    {
        if (isToggleSprint)
        {
            if (context.started) wantToSprint = !wantToSprint;
        }
        else
        {
            if (context.started) wantToSprint = true;
            if (context.canceled) wantToSprint = false;
        }
    }

    private void CheckCurrentState()
    {
        if (playerInfo.IsGrounded)
        {
            if (wantToSprint)
            {
                currentState = MovementState.Running;
            }
            else
            {
                currentState = MovementState.Walking;
            }
        }
        else
        {
            currentState = MovementState.Aerial;
        }
    }
    private void UpdateLookDirection()
    {
        if (isMoving)
        {
            if (horizontalInput < 0)
            {
                // On regarde � gauche : on retourne le sprite horizontalement
                transform.localScale = new Vector3(-1, 1, 1);
            }
            else
            {
                // On regarde � droite : sprite normal
                transform.localScale = new Vector3(1, 1, 1);
            }
        }
    }
    private void ComputeVelocity()
    {
        MoveSettings moveSettings = currentState switch
        {
            MovementState.Walking => walkSettings,
            MovementState.Running => runSettings,
            MovementState.Aerial => airSettings,
            _ => walkSettings
        };

        float maxSpeedChange = float.MaxValue;
        if (isMoving)
        {
            // V�rifie si le joueur change de direction par rapport � sa vitesse actuelle
            bool IsChangingMoveDirection = Mathf.Sign(transform.localScale.x) != Mathf.Sign(body.linearVelocity.x);

            if (IsChangingMoveDirection)
            {
                // Tourner sur place (changement brusque de direction)
                maxSpeedChange = moveSettings.maxTurnSpeed * Time.deltaTime;
            }
            else
            {
                // Acc�l�ration normale
                maxSpeedChange = moveSettings.maxAcceleration * Time.deltaTime;
            }
        }
        else
        {
            // Quand le joueur rel�che toute touche, on freine (d�c�l�ration)
            maxSpeedChange = moveSettings.maxDecceleration * Time.deltaTime;
        }

        desiredvelocityX = horizontalInput * moveSettings.maxSpeed;
        // On approche progressivement la vitesse actuelle vers la vitesse d�sir�e en respectant la limite de "maxSpeedChange"
        velocityX = Mathf.MoveTowards(velocityX, desiredvelocityX, maxSpeedChange);

        body.linearVelocityX = velocityX;
    }
}

// Classe permettant d'organiser les param�tres de d�placement pour les diff�rents �tats (marche, course, air)
[System.Serializable]
public struct MoveSettings
{
    [SerializeField, Range(0f, 20f)] public float maxSpeed;
    [SerializeField, Range(0f, 100f)] public float maxAcceleration;
    [SerializeField, Range(0f, 100f)] public float maxDecceleration;
    [SerializeField, Range(0f, 100f)] public float maxTurnSpeed;

    public MoveSettings(float maxSpeed = 10f, float maxAcceleration = 52f, float maxDecceleration = 52f, float maxTurnSpeed = 80f)
    {
        this.maxSpeed = maxSpeed;
        this.maxAcceleration = maxAcceleration;
        this.maxDecceleration = maxDecceleration;
        this.maxTurnSpeed = maxTurnSpeed;
    }
}