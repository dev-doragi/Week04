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

        if (_input.interact)
        {
            Debug.Log("f키 클릭");
            _interaction.Interact();
            _input.interact = false;
        }

        if (_input.drop)
        {
            Debug.Log("Q키 클릭");
            _interaction.DropItem();
            _input.drop = false;
        }

        if (_input.build)
        {
            Debug.Log("E키 클릭");
            _interaction.ApplyWoodPatch();
            _input.build = false;
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
