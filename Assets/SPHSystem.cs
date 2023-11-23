using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class SPHSystem : MonoBehaviour
{
    [Header("General")]
    public float particleRange = 1.0f;
    public float coreRange = 0.8f;
    public float particleSize = 0.05f;
    public float particleMass = 1f;
    public int partialCount = 1000;
    [Header("Fluid Constants")]
    [Range(0.001f, 10f)]
    public float smoothingRadius = 1f;
    public float restDensity = 1f;
    [Range(0.1f, 20f)]
    public float stiffness = 2f;
    [Range(0.001f, 10f)]
    public float viscosity = 0.01f;
    [Range(0.001f, 10f)]
    public float surfaceTension = 0.01f;    
    [Range(0.001f, 10f)]
    public float timeStep = 0.2f;
    [Header("Repulsion Force")]
    [Range(0.001f, 2f)]
    public float repulsionDistance = 0.01f;

    [Range(0f, 5f)]
    public float repulsion = 0.01f;
    [Header("External Force")]
    public Vector3 gravity = new Vector3(0f,0f,0f);
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

            Vector3 repulsionForce = Vector3.zero;
            foreach (var neighbor in particles)
            {
                if (neighbor != particle)
                {
                    Vector3 r = particle.position - neighbor.position;
                    pressureForce -= particleMass * (particle.pressure / Mathf.Pow(particle.density, 2) + neighbor.pressure / Mathf.Pow(neighbor.density, 2)) * Kernel.WspikyGradient(r);
                    viscosityForce += viscosity * particleMass * (neighbor.velocity - particle.velocity) / neighbor.density * Kernel.WviscosityLaplacian(r);
                    surfaceNormal += particleMass * Kernel.Wpoly6Gradient(r) / neighbor.density;
                    colorLaplacian += particleMass * Kernel.Wpoly6Laplacian(r) / neighbor.density;
                
                    if (r.magnitude < particleSize * repulsionDistance)
                    {
                        float repulsionStrength = 1.0f - r.magnitude / (particleSize * repulsionDistance);
                        repulsionForce += repulsionStrength * r.normalized;
                    }
                }
            }
            repulsionForce = repulsionForce.normalized * repulsion;

            Vector3 gravityForce = gravity * particle.density;
            Vector3 centerForce = -0.5f*particle.position.normalized;

            if(surfaceNormal.magnitude>0.01f) surfaceNormal = surfaceNormal.normalized;
            Vector3 surfaceTensionForce = -surfaceTension * colorLaplacian * surfaceNormal;

            particle.force = 
                pressureForce + 
                viscosityForce + 
                gravityForce + 
                surfaceTensionForce +
                centerForce + 
                repulsionForce;

            // if(float.IsNaN(particle.force.x)){
            //     Debug.Log("Debug");
            //     Debug.Log(pressureForce);
            //     Debug.Log(viscosityForce);
            //     Debug.Log(gravityForce);
            //     Debug.Log(surfaceTensionForce);
            //     Debug.Log(centerForce);
            // }
        }
    }

    public void Integrate()
    {
        for (int i = 0; i < partialCount; i++)
        {
            Particle particle = particles[i];
            particle.velocity += timeStep * particle.force / particleMass;
            particle.position += timeStep * particle.velocity;
            CoreConstrain(ref particle);
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
        float speed = particle.velocity.magnitude;
        float range = 0.005f * speed;
        float jit_x = Random.Range(-range, range);
        float jit_y = Random.Range(-range, range);
        float jit_z = Random.Range(-range, range);
        if(particle.position.magnitude < coreRange){
            particle.velocity += new Vector3(jit_x, jit_y, jit_z);
            particle.velocity = particle.velocity.normalized * speed;
            particle.velocity *= -boundaryDamp;
            particle.position = particle.position.normalized * coreRange;
        }
        else if(particle.position.magnitude > coreRange + particleRange){
            particle.velocity += new Vector3(jit_x, jit_y, jit_z);
            particle.velocity = particle.velocity.normalized * speed;
            particle.velocity *= -boundaryDamp;
            particle.position = particle.position.normalized * (coreRange + particleRange);
        }
    }

    private void BoxConstrain(ref Particle particle)
    {
        float edge = 2.5f/2f;

        if(particle.position.x < -edge){
            particle.velocity.x *= -boundaryDamp;
            particle.position.x = -edge;
            particle.force.x = 0f;
        }
        else if(particle.position.x > edge){
            particle.velocity.x *= -boundaryDamp;
            particle.position.x = edge;
            particle.force.x = 0f;
        }

        if(particle.position.y < -edge){
            particle.velocity.y *= -boundaryDamp;
            particle.position.y = -edge;
            particle.force.y = 0f;
        }
        else if(particle.position.y > edge){
            particle.velocity.y *= -boundaryDamp;
            particle.position.y = edge;
            particle.force.y = 0f;
        }

        if(particle.position.z < -edge){
            particle.velocity.z *= -boundaryDamp;
            particle.position.z = -edge;
            particle.force.z = 0f;
        }
        else if(particle.position.z > edge){
            particle.velocity.z *= -boundaryDamp;
            particle.position.z = edge;
            particle.force.z = 0f;
        }

    }
}