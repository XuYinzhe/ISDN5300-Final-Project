using UnityEngine;

[System.Serializable]
public class Particle
{
    public Vector3 position = new Vector3(0f,0f,0f);
    public Vector3 velocity = new Vector3(0f,0f,0f);
    public Vector3 force = new Vector3(0f,0f,0f);
    public float pressure = 0f;
    public float density = 0.0001f;
    public bool boundary = false;
}
