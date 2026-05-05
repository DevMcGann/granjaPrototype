using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerSeedEmitter : MonoBehaviour
{
    [Header("Particle Setup")]
    [SerializeField] private ParticleSystem seedParticles;
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 0.55f, 1.55f);
    [SerializeField] private Vector3 localEulerAngles = new Vector3(65f, 0f, 0f);
    [SerializeField] private LayerMask collisionLayers = ~0;

    [Header("Emission")]
    [SerializeField] private bool playOnAwake = true;
    [SerializeField] private float emissionRate = 18f;
    [SerializeField] private float startLifetime = 0.45f;
    [SerializeField] private float startSpeed = 2.6f;
    [SerializeField] private float startSize = 0.08f;
    [SerializeField] private float sowProgressPerCollisionEvent = 0.08f;

    private readonly List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();

    public bool TryGetSowingProgress(GameObject hitObject, out float progressDelta)
    {
        progressDelta = 0f;

        if (seedParticles == null)
        {
            return false;
        }

        int safeSize = seedParticles.GetSafeCollisionEventSize();
        if (safeSize <= 0)
        {
            return false;
        }

        if (collisionEvents.Capacity < safeSize)
        {
            collisionEvents.Capacity = safeSize;
        }

        int collisionCount = seedParticles.GetCollisionEvents(hitObject, collisionEvents);
        if (collisionCount <= 0)
        {
            return false;
        }

        progressDelta = collisionCount * sowProgressPerCollisionEvent;
        return true;
    }

    private void Awake()
    {
        EnsureParticleSystem();
        ConfigureParticleSystem();

        if (seedParticles != null && playOnAwake && !seedParticles.isPlaying)
        {
            seedParticles.Play();
        }
    }

    private void OnValidate()
    {
        if (!Application.isPlaying && seedParticles != null)
        {
            ApplyEmitterTransform(seedParticles.transform);
        }
    }

    private void EnsureParticleSystem()
    {
        if (seedParticles != null)
        {
            return;
        }

        Transform existingEmitter = transform.Find("SeedEmitter");
        if (existingEmitter != null)
        {
            seedParticles = existingEmitter.GetComponent<ParticleSystem>();
        }

        if (seedParticles != null)
        {
            return;
        }

        GameObject emitterObject = new GameObject("SeedEmitter");
        Transform emitterTransform = emitterObject.transform;
        emitterTransform.SetParent(transform, false);
        ApplyEmitterTransform(emitterTransform);

        seedParticles = emitterObject.AddComponent<ParticleSystem>();

        if (emitterObject.GetComponent<ParticleSystemRenderer>() == null)
        {
            emitterObject.AddComponent<ParticleSystemRenderer>();
        }
    }

    private void ConfigureParticleSystem()
    {
        if (seedParticles == null)
        {
            return;
        }

        ApplyEmitterTransform(seedParticles.transform);

        ParticleSystem.MainModule main = seedParticles.main;
        main.playOnAwake = playOnAwake;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = startLifetime;
        main.startSpeed = startSpeed;
        main.startSize = startSize;
        main.maxParticles = 128;

        ParticleSystem.EmissionModule emission = seedParticles.emission;
        emission.enabled = true;
        emission.rateOverTime = emissionRate;

        ParticleSystem.ShapeModule shape = seedParticles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 12f;
        shape.radius = 0.06f;

        ParticleSystem.CollisionModule collision = seedParticles.collision;
        collision.enabled = true;
        collision.type = ParticleSystemCollisionType.World;
        collision.mode = ParticleSystemCollisionMode.Collision3D;
        collision.collidesWith = collisionLayers;
        collision.sendCollisionMessages = true;
        collision.enableDynamicColliders = true;

        ParticleSystemRenderer renderer = seedParticles.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }
    }

    private void ApplyEmitterTransform(Transform emitterTransform)
    {
        emitterTransform.localPosition = localOffset;
        emitterTransform.localEulerAngles = localEulerAngles;
        emitterTransform.localScale = Vector3.one;
    }
}
