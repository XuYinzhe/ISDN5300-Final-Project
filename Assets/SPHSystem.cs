using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class SPHSystem : MonoBehaviour
{
    public float particleRange = 1.0f;
    public float coreRange = 0.8f;
    public float particleSize = 0.05f;
    public float particleMass = 1f;
    public int partialCount = 1000;
    public float smoothingRadius = 1f;
    public float restDensity = 1f;
    public float stiffness = 2f;
    public float viscosity = -0.003f;
    public Vector3 gravity = new Vector3(0f,0f,0f);
    public float surfaceTension = 0.001f;
    public float timeStep = 0.2f;
    private float boundaryDamp = 0.7f;
    private List<Particle> particles = new List<Particle>();
    private List<GameObject> particleObjects = new List<GameObject>();

    void Awake()
    {
        for(int i=0; i<partialCount; i++)
        {
            float theta = Random.Range(-Mathf.PI, Mathf.PI);
            float phi = Random.Range(-Mathf.PI/2.0f, Mathf.PI/2.0f);
            float height = Random.Range(coreRange, particleRange);

            Vector3 _position = new Vector3(
                height*Mathf.Sin(theta)*Mathf.Cos(phi),
                height*Mathf.Sin(theta)*Mathf.Sin(phi),
                height*Mathf.Cos(theta)
            );

            Particle particle = new Particle{position = _position};
            particles.Add(particle);

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.localScale = new Vector3(particleSize, particleSize, particleSize);
            sphere.transform.position = _position;
            particleObjects.Add(sphere);
        }

        Kernel.Initialize(smoothingRadius);

        Debug.Log("particles created");
    }

    void Update()
    {
        UpdateSystem();
        for (int i = 0; i < partialCount; i++)
        {   
            particleObjects[i].transform.position = particles[i].position;
        }
    }

    public void CalculateDensityPressure()
    {
        foreach (var particle in particles)
        {
            foreach (var neighbor in particles)
            {
                if (neighbor != particle)
                {
                    Vector3 r = particle.position - neighbor.position;
                    particle.density += particleMass * Kernel.Wpoly6(r);
                }
            }
            particle.density = Mathf.Max(particle.density, 0.0001f);
            particle.pressure = stiffness * (particle.density - restDensity);
        }
    }

    public void CalculateForces()
    {
        foreach (var particle in particles)
        {
            Vector3 pressureForce = Vector3.zero;
            Vector3 viscosityForce = Vector3.zero;
            Vector3 surfaceNormal = Vector3.zero;
            float colorLaplacian = 0.0f;

            foreach (var neighbor in particles)
            {
                if (neighbor != particle)
                {
                    Vector3 r = particle.position - neighbor.position;
                    pressureForce -= particleMass * (particle.pressure / Mathf.Pow(particle.density, 2) + neighbor.pressure / Mathf.Pow(neighbor.density, 2)) * Kernel.WspikyGradient(r);
                    viscosityForce += viscosity * particleMass * (neighbor.velocity - particle.velocity) / neighbor.density * Kernel.WviscosityLaplacian(r);
                    surfaceNormal += particleMass * Kernel.Wpoly6Gradient(r) / neighbor.density;
                    colorLaplacian += particleMass * Kernel.Wpoly6Laplacian(r) / neighbor.density;
                }
            }

            Vector3 gravityForce = gravity * particle.density;
            Vector3 centerForce = -1f*particle.position.normalized;
            centerForce = Vector3.zero;

            if(surfaceNormal.magnitude>0.01f) surfaceNormal = surfaceNormal.normalized;
            Vector3 surfaceTensionForce = -surfaceTension * colorLaplacian * surfaceNormal;

            particle.force = 
                pressureForce + 
                viscosityForce + 
                gravityForce + 
                surfaceTensionForce +
                centerForce;
        }
    }

    public void Integrate()
    {
        for (int i = 0; i < partialCount; i++)
        {
            Particle particle = particles[i];
            particle.velocity += timeStep * particle.force / particleMass;
            particle.position += timeStep * particle.velocity;
            BoxConstrain(ref particle);
            particles[i] = particle;
        }
    }

    public void UpdateSystem()
    {
        CalculateDensityPressure();
        CalculateForces();
        Integrate();
    }

    private void CoreConstrain(ref Particle particle)
    {
        if(particle.position.magnitude < coreRange){
            particle.velocity *= -boundaryDamp;
            particle.position = particle.position.normalized * coreRange;
        }
    }

    private void BoxConstrain(ref Particle particle)
    {
        float edge = 2.5f/2f;

        if(particle.position.x < -edge){
            particle.velocity.x *= -boundaryDamp;
            particle.position.x = -edge;
        }
        else if(particle.position.x > edge){
            particle.velocity.x *= -boundaryDamp;
            particle.position.x = edge;
        }

        if(particle.position.y < -edge){
            particle.velocity.y *= -boundaryDamp;
            particle.position.y = -edge;
        }
        else if(particle.position.y > edge){
            particle.velocity.y *= -boundaryDamp;
            particle.position.y = edge;
        }

        if(particle.position.z < -edge){
            particle.velocity.z *= -boundaryDamp;
            particle.position.z = -edge;
        }
        else if(particle.position.z > edge){
            particle.velocity.z *= -boundaryDamp;
            particle.position.z = edge;
        }

    }
}