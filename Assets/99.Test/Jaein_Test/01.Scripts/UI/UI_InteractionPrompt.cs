using TMPro;
using UnityEngine;

public class UI_InteractionPrompt : MonoBehaviour
{
    [SerializeField] private GameObject _root;
    [SerializeField] private TMP_Text _text;

    private void Awake()
    {
        Hide();
    }

    public void Show(string message)
    {
        if (_root != null)
            _root.SetActive(true);

        if (_text != null)
            _text.text = message;
    }

    public void Hide()
    {
        if (_root != null)
            _root.SetActive(false);
    }
}