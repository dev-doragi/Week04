using System;
using UnityEngine;

public class PlayerEntity : MonoBehaviour
{
    public CapsuleMovement _movement { get; private set; }
    public PlayerInteraction _interaction { get; private set; }
    public PlayerInputAction _input;

    public bool InputLock = false;
    public bool isHoldAxe = true;

    [SerializeField] private Transform axe;

    private void Awake()
    {
        _movement = GetComponent<CapsuleMovement>();
        _interaction = GetComponent<PlayerInteraction>();

        _input = GetComponent<PlayerInputAction>();
    }

    private void Update()
    {
        // Ŭ�� ���� ��
        if (_input.click && isHoldAxe)
        {
            _interaction.BoatBreaker(axe);
        }
        else
        {
            _interaction.StopChopping();
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
        if (InputLock == false)
        {
            _movement.LateTick();
        }
    }
}
