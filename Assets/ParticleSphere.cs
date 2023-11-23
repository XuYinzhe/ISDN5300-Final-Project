using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleSphere : MonoBehaviour
{
    public int particleCount = 10000;
    public ParticleSystem particleSystem;
    public float particleSize = 0.1f;
    public float particleMass = 0.1f;
    public float particleRange = 1.0f;
    public float gravity = 0.1f;
    [Range(0.001f, 1f)]
    public float timeStep = 0.01f;
    private float accumulatedTime = 0.0f;
    private ParticleSystem.Particle[] particles;

    void Start()
    {
        particles = new ParticleSystem.Particle[particleCount];

        for (int i = 0; i < particleCount; i++)
        {
            float theta = Random.Range(-Mathf.PI, Mathf.PI);
            float phi = Random.Range(-Mathf.PI/2.0f, Mathf.PI/2.0f);

            Vector3 position = new Vector3(
                particleRange*Mathf.Sin(theta)*Mathf.Cos(phi),
                particleRange*Mathf.Sin(theta)*Mathf.Sin(phi),
                particleRange*Mathf.Cos(theta)
            );
            particles[i].position = position;
            particles[i].startColor = Color.black;
            particles[i].startSize = particleSize;
        }

        particleSystem.SetParticles(particles, particles.Length);
        // particleSystem.startLifetime = Mathf.Infinity;
    }

    void Update()
    {
        // Update the particles
        int numParticles = particleSystem.GetParticles(particles);

        for (int i = 0; i < numParticles; i++)
        {
            Vector3 toCenter = particleSystem.transform.position - particles[i].position;
            particles[i].velocity += toCenter.normalized * gravity / particleMass * timeStep;
            particles[i].position += particles[i].velocity * timeStep;
            float colorgrid = Mathf.Clamp01(toCenter.magnitude/particleRange);
            particles[i].startColor = Color.Lerp(Color.blue, Color.red,colorgrid);
        }

        particleSystem.SetParticles(particles, numParticles);
    }
}
