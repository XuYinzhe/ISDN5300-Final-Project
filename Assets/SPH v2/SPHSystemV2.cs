using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;
using UnityEngine;

public class SPHSystemV2 : MonoBehaviour
{
    //public
    [Header("General")]
    public float particleRange = 2.0f;
    public float coreRange = 0.8f;
    public float particleSize = 0.05f;
    public float particleMass = 1f;
    public int partialCount = 1000;
    public bool showParticle = true;
    public Mesh particleMesh;
    public Material particleMaterial;
    public ComputeShader shader;

    [Header("Fluid Constants")]
    [Range(0.001f, 10f)]
    public float smoothingRadius = 1f;
    public float restDensity = 1f;
    [Range(0.1f, 20f)]
    public float stiffness = 2f;
    [Range(0.001f, 10f)]
    public float viscosity = 0.1f;
    [Range(0.001f, 10f)]
    public float surfaceTension = 0.01f;    
    [Range(0.001f, 10f)]
    public float timeStep = 0.2f;

    [Header("External Force")]
    public Vector3 gravity = new Vector3(0f,0f,0f);

    // private
    private RenderParams rp;
    private Particle[] particles;
    private ComputeBuffer particlesBuffer;
    private GraphicsBuffer argsBuffer;
    private int CalculateDensityPressure_ID;
    private int CalculateForces_ID;
    private int Integrate_ID;

    // particle
    [System.Serializable]
    [StructLayout(LayoutKind.Sequential, Size = 44)]
    struct Particle
    {
        public float pressure; // 4
        public float density; // 8
        public Vector3 force; // 20
        public Vector3 velocity; // 32
        public Vector3 position; // 44
    }

    void Awake(){
        Kernel.Initialize(smoothingRadius);
        InitArgsBuffer();
        InitParticles();
        InitParticlesBuffer();
    }

    // update per frame
    void Update(){
        if(showParticle)
            Graphics.RenderMeshIndirect(rp, particleMesh, argsBuffer, 1);
    }

    void FixedUpdate()
    {
        shader.SetFloat("timeStep", timeStep);

        shader.Dispatch(CalculateDensityPressure_ID, partialCount / 100, 1, 1);
        shader.Dispatch(CalculateForces_ID, partialCount / 100, 1, 1);
        shader.Dispatch(Integrate_ID, partialCount / 100, 1, 1);
    }

    public void InitParticles(){
        List<Particle> lparticles = new List<Particle>();
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
            lparticles.Add(particle);
        }
        particles = lparticles.ToArray();
    }

    public void InitArgsBuffer(){
        // uint[] args = {
        //     particleMesh.GetIndexCount(0),
        //     (uint) partialCount,
        //     particleMesh.GetIndexStart(0),
        //     particleMesh.GetBaseVertex(0),
        //     0
        // };
        // argsBuffer = new ComputeBuffer(1, 
        //     args.Length * sizeof(uint), 
        //     ComputeBufferType.IndirectArguments
        // );
        // argsBuffer.SetData(args);

        argsBuffer = new GraphicsBuffer(
            GraphicsBuffer.Target.IndirectArguments, 
            1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        
        rp = new RenderParams(particleMaterial);
        rp.worldBounds = new Bounds(Vector3.zero, particleRange * Vector3.one);
        rp.shadowCastingMode = ShadowCastingMode.Off;

        GraphicsBuffer.IndirectDrawIndexedArgs[] args = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
        args[0].baseVertexIndex = particleMesh.GetBaseVertex(0);
        args[0].indexCountPerInstance = particleMesh.GetIndexCount(0);
        args[0].instanceCount = (uint) partialCount;
        args[0].startIndex = particleMesh.GetIndexStart(0);
        args[0].startInstance = (uint) 0;

        argsBuffer.SetData(args);
    }

    public void InitParticlesBuffer(){
        particlesBuffer = new ComputeBuffer(partialCount, 44);
        particlesBuffer.SetData(particles);

        shader.SetInt("partialCount", partialCount);
        shader.SetFloat("particleMass", particleMass);
        shader.SetFloat("particleRange", particleRange);
        shader.SetFloat("coreRange", coreRange);

        shader.SetFloat("PI", Mathf.PI);
        shader.SetFloat("h", Kernel.h);
        shader.SetFloat("h2", Kernel.h2);
        shader.SetFloat("poly6Coefficient", 
            Kernel.poly6Coefficient);
        shader.SetFloat("poly6GradientCoefficient", 
            Kernel.poly6GradientCoefficient);
        shader.SetFloat("poly6LaplacianCoefficient", 
            Kernel.poly6LaplacianCoefficient);
        shader.SetFloat("spikyGradientCoefficient", 
            Kernel.spikyGradientCoefficient);
        shader.SetFloat("viscosityLaplacianCoefficient", 
            Kernel.viscosityLaplacianCoefficient);

        shader.SetFloat("restDensity", restDensity);
        shader.SetFloat("stiffness", stiffness);
        shader.SetFloat("viscosity", viscosity);
        shader.SetFloat("surfaceTension", surfaceTension);

        CalculateDensityPressure_ID = shader.FindKernel("CalculateDensityPressure");
        CalculateForces_ID = shader.FindKernel("CalculateForces");
        Integrate_ID = shader.FindKernel("Integrate");

        shader.SetBuffer(CalculateDensityPressure_ID, "particles", particlesBuffer);
        shader.SetBuffer(CalculateForces_ID, "particles", particlesBuffer);
        shader.SetBuffer(Integrate_ID, "particles", particlesBuffer);
    }

}
