public enum InteractionKeyType
{
    E,
    F
}

public interface IInteractable
{
    InteractionKeyType GetInteractionKey();
    string GetInteractionActionText();
    void Interact(PlayerMovement player);
}