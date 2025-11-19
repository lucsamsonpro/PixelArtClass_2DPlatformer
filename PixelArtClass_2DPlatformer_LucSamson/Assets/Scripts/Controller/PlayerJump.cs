using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public enum JumpState
{
    Grounded,
    Jumping,
    Falling
}

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerJump : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private PlayerState playerInfo;
    private Rigidbody2D body;
    public Animator animator;

    [Header("Current State")]
    public JumpState currentState;
    public JumpState CurrentState
    {
        get => currentState;
        private set
        {
            if (currentState == value) return;

            //When Exiting 
            switch (currentState)
            {
                case JumpState.Grounded:
                    break;
                case JumpState.Jumping:
                    break;
                case JumpState.Falling:
                    break;
            }

            //When Entering State
            switch (value)
            {
                case JumpState.Grounded:
                    animator.SetBool("isJumping", false);
                    animator.SetBool("isFalling", false);
                    break;
                case JumpState.Jumping:
                    animator.SetBool("isJumping", true);
                    break;
                case JumpState.Falling:
                    animator.SetBool("isFalling", true);
                    break;
            }

            currentState = value;
        }
    }
    private bool IsGrounded => CurrentState == JumpState.Grounded;

    [Header("Jump Settings")]
    [SerializeField, Tooltip("Maximum jump height"), Range(2f, 5.5f)]
    public float jumpHeight = 3f;
    [SerializeField, Tooltip("How long it takes to reach that height before coming back down"), Range(0.2f, 1.25f)]
    public float timeToReachApex = 0.5f;

    [Header("Settings")]
    [SerializeField, Range(1f, 10f), Tooltip("Gravity multiplier to apply when coming down")]
    public float groundGravityMultiplier = 1f;
    [SerializeField, Range(0f, 5f)]
    [Tooltip("Gravity multiplier to apply when going up")]
    public float jumpGravityMultiplier = .95f;
    [SerializeField, Range(1f, 10f)]
    [Tooltip("Gravity multiplier to apply when coming down")]
    public float fallGravityMultiplier = 1.75f;
    [Space]
    [SerializeField, Range(1f, 10f), Tooltip("The fastest speed the character can fall")]
    public float maxFallSpeed = 12f;

    [Header("Options Analogic")]
    [SerializeField, Tooltip("Should the character drop when you let go of jump?")]
    public bool isJumpAnalogic;
    [SerializeField, Tooltip("Gravity multiplier when you let go of jump"), Range(1f, 10f)]
    public float jumpCutOffGravityMultiplier = 1.75f;

    [Header("Calculations")]
    [SerializeField] private bool desiredJump;
    [SerializeField] private bool pressingJump;
    [SerializeField] private float initialJumpForce;
    [SerializeField] private float currentGravityMultiplier;

    [Header("Events")]
    public UnityEvent onJump;

    private void Awake()
    {
        // GetComponent() permet de rechercher sur son objet s'il y a un composant Rigidbody2D
        body = GetComponent<Rigidbody2D>();
        body.gravityScale = groundGravityMultiplier;
    }
    private void Update()
    {   // Animation de saut
        animator.SetFloat("VelocityY", body.linearVelocityY);
        animator.SetBool("isGrounded", playerInfo.IsGrounded);
    }
    private void FixedUpdate()
    {
        CalculateGravityScale();

        //Keep trying to do a jump, for as long as desiredJump is true
        if (desiredJump)
        {
            DoJump();

            //Skip gravity calculations this frame, so currentlyJumping doesn't turn off
            //This makes sure you can't do the coyote time double jump bug
            return;
        }

        CheckPlayerState();

        //Set the character's Rigidbody's velocity
        //But clamp the Y variable within the bounds of the speed limit, for the terminal velocity assist option
        body.linearVelocityY = Mathf.Max(body.linearVelocityY, -maxFallSpeed);
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (!playerInfo.CharacterCanMove) return;

        //When we press the jump button, tell the script that we desire a jump.
        //Also, use the started and canceled contexts to know if we're currently holding the button
        if (context.started)
        {
            desiredJump = true;
            pressingJump = true;
        }
        if (context.canceled)
        {
            pressingJump = false;
        }
    }

    private void DoJump()
    {
        desiredJump = false;
        //Create the jump, provided we are on the ground, in coyote time, or have a double jump available
        if (!IsGrounded)
        {
            //Put in Buffer ?
            return;
        }

        CurrentState = JumpState.Jumping;

        // Compute initial velocity to reach apex at timeToApex:
        // InitialVelocity = GravityScale * TimeToReachApex
        initialJumpForce = GetGravityConstant() * timeToReachApex;
        body.linearVelocityY = initialJumpForce;

        animator.SetTrigger("jump"); // Animation du saut

        onJump?.Invoke();
    }
    private void CheckPlayerState()
    {
        switch (CurrentState)
        {
            case JumpState.Grounded: return;
            case JumpState.Jumping:
                if (body.linearVelocity.y < 0) CurrentState = JumpState.Falling;
                break;
            case JumpState.Falling:
                if (playerInfo.IsGrounded) CurrentState = JumpState.Grounded;
                break;
        }
    }
    private float GetGravityConstant()
    {
        //Formule de physique :
        //Hauteur = (1/2) * Gravité * Temps²
        //Donc Gravité = (2 * Hauteur) / Temps²
        return (2f * jumpHeight) / Mathf.Pow(timeToReachApex, 2f);
    }
    private void CalculateGravityScale()
    {
        var newGravityScale = GetGravityConstant();
        newGravityScale *= CurrentState switch
        {
            JumpState.Jumping => (isJumpAnalogic && !pressingJump) ? jumpGravityMultiplier * jumpCutOffGravityMultiplier : jumpGravityMultiplier,
            JumpState.Falling => fallGravityMultiplier,
            JumpState.Grounded => groundGravityMultiplier,
            _ => groundGravityMultiplier,
        };
        //Gravité = Physics2D.gravity.y * gravityScale = gravityForce
        //Gravité = gravityForce / Physics2D.gravity.y
        //Et comme Physics2D.gravity.y est négatif, on l'inverse en mettant un - devant
        newGravityScale /= -Physics2D.gravity.y;

        currentGravityMultiplier = newGravityScale;
        body.gravityScale = currentGravityMultiplier;
    }
}