using UnityEngine;
using System.Collections;

public class DestroyAfterParticles : MonoBehaviour
{
    private ParticleSystem _particleSystem;

    void Start()
    {
        _particleSystem = GetComponent<ParticleSystem>();
        if (_particleSystem == null)
        {
            Debug.LogWarning("DestroyAfterParticles: No ParticleSystem found on this GameObject. Destroying immediately.");
            Destroy(gameObject); // ≈сли нет ParticleSystem, просто удал€ем
            return;
        }

        // «апускаем корутину, котора€ будет ждать окончани€ Particle System
        StartCoroutine(CheckParticlesThenDestroy());
    }

    private IEnumerator CheckParticlesThenDestroy()
    {
        // ∆дем, пока Particle System не закончит проигрывание
        // (испустит все частицы и все существующие частицы исчезнут)
        yield return new WaitForSeconds(_particleSystem.main.duration + _particleSystem.main.startLifetime.constantMax);
        // ƒополнительна€ задержка на случай, если есть какие-то lingering particles или sub-emitters
        yield return new WaitForSeconds(0.5f);

        Destroy(gameObject);
    }
}
