public enum InteractionKeyType
{
    None,
    E,
    F
}

public interface IInteractable
{
    InteractionKeyType GetInteractionKey();
    string GetInteractionActionText();
    void Interact(PlayerMovement player);
}