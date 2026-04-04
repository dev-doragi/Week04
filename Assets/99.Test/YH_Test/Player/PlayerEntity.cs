using System;
using UnityEngine;

public class PlayerEntity : MonoBehaviour
{
    public CapsuleMovement _movement { get; private set; }
    public PlayerInteraction _interaction { get; private set; }
    public PlayerInputAction _input;

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
        }
    }

    private void FixedUpdate()
    {
        _movement.FixedTick();
    }

    private void LateUpdate()
    {
        _movement.LateTick();
    }
}
