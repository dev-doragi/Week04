using UnityEngine;

public class RandomBurstParticle : MonoBehaviour
{
    [SerializeField] private ParticleSystem _ps;
    [SerializeField] private Vector2 _intervalRange = new Vector2(0.5f, 2f);

    private float _timer;

    private void Start()
    {
        SetNextTime();
    }

    private void Update()
    {
        _timer -= Time.deltaTime;

        if (_timer <= 0f)
        {
            _ps.Play(true);
            SetNextTime();
        }
    }

    private void SetNextTime()
    {
        _timer = Random.Range(_intervalRange.x, _intervalRange.y);
    }
}