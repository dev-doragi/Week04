using System;
using UnityEngine;

public class PlayerEntity : MonoBehaviour
{
    public CapsuleMovement _movement { get; private set; }
    public PlayerInteraction _interaction { get; private set; }
    public PlayerInputAction _input;

    public bool InputLock = false;

    [SerializeField] private Transform axe;

    private void Awake()
    {
        _movement = GetComponent<CapsuleMovement>();
        _interaction = GetComponent<PlayerInteraction>();

        _input = GetComponent<PlayerInputAction>();
    }

    private void Update()
    {
        if (_input.click)
        {
            _interaction.BoatBreaker(axe);
            //_input.click = false;
        } else
        {
            _interaction.ResetOutline();
        }

        if (_input.interact)
        {
            _interaction.Interact();
            _input.interact = false;
        }

        if (_input.drop)
        {
            _interaction.DropItem();
            _input.drop = false;
        }

        if (_input.build)
        {
            _interaction.ApplyWoodPatch();
            _input.build = false;
        }
    }

    private void FixedUpdate()
    {
        if(InputLock == false)
        {
            _movement.FixedTick();
        }
    }

    private void LateUpdate()
    {
        _movement.LateTick();
    }
}
