using System.Collections.Generic;
using UnityEngine;

// Uses the collider to check directions to see if the object is currently on the ground, touching the wall, or touching the ceiling
[RequireComponent(typeof(CapsuleCollider2D))]
public class PlayerState : MonoBehaviour
{
    private CapsuleCollider2D col;

    [Header("Collider Settings")]
    [SerializeField, Tooltip("Length of the ground-checking collider")] 
    private float groundCheckDistance = 0.05f;

    [Header("Layer Masks")]
    [SerializeField, Tooltip("Which layers are read as the ground")] 
    public ContactFilter2D castFilter;

    [Header("Info")]
    [SerializeField] private bool initialCharacterCanMove = true;
    public bool CharacterCanMove;
    public bool IsGrounded { get; private set; }
    [SerializeField] private List<RaycastHit2D> groundCastResult = new List<RaycastHit2D>();

    private void Awake()
    {
        col = GetComponent<CapsuleCollider2D>();
        CharacterCanMove = initialCharacterCanMove;
    }
    private void FixedUpdate()
    {
        IsGrounded = col.Cast(Vector2.down, castFilter, groundCastResult, groundCheckDistance) > 0;
    }

    private void OnDrawGizmos()
    {
        if (col == null) col = GetComponent<CapsuleCollider2D>();

        var originalColor = Gizmos.color;
        Gizmos.color = IsGrounded ? Color.green : Color.red;

        Gizmos.DrawWireCube(col.bounds.center + Vector3.down * groundCheckDistance, col.bounds.size);

        Gizmos.color = originalColor;
    }
}
