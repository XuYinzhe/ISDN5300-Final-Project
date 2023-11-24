using UnityEngine;

public static class Kernel
{
    // refer to http://www.cs.cornell.edu/%7Ebindel/class/cs5220-f11/code/sph-derive.pdf
    public static float h, h2, h3, h4, h5, h6, h8, h9;
    public static float poly6Coefficient;
    public static float poly6GradientCoefficient;
    public static float poly6LaplacianCoefficient;
    public static float spikyGradientCoefficient;
    public static float viscosityLaplacianCoefficient;

    public static void Initialize(float smoothingRadius)
    {
        h = smoothingRadius;
        h2 = Mathf.Pow(h,2);
        h3 = Mathf.Pow(h,3);
        h4 = Mathf.Pow(h,4);
        h5 = Mathf.Pow(h,5);
        h6 = Mathf.Pow(h,6);
        h8 = Mathf.Pow(h,8);
        h9 = Mathf.Pow(h,9);

        poly6Coefficient = 4f / (Mathf.PI * h8);
        poly6GradientCoefficient = -24f / (Mathf.PI * h8);
        poly6LaplacianCoefficient = -48f / (Mathf.PI * h8);
        spikyGradientCoefficient = -30f / (Mathf.PI * h4);
        viscosityLaplacianCoefficient = 40f / (Mathf.PI * h4);
    }

    public static float Wpoly6(Vector3 r)
    {
        float r2 = r.sqrMagnitude;
        if (r2 < h2)
        {
            float term = h2 - r2;
            return poly6Coefficient * Mathf.Pow(term,3);
        }
        return 0f;
    }

    public static Vector3 Wpoly6Gradient(Vector3 r)
    {
        float r2 = r.sqrMagnitude;
        if (r2 < h2)
        {
            float term = h2 - r2;
            return poly6GradientCoefficient * Mathf.Pow(term, 2) * r;
        }
        return Vector3.zero;
    }

    public static float Wpoly6Laplacian(Vector3 r)
    {
        float r2 = r.sqrMagnitude;
        if (r2 < h2)
        {
            float term = h2 - r2;
            return poly6LaplacianCoefficient * term * (h2-3f*r2);
        }
        return 0;
    }

    public static Vector3 WspikyGradient(Vector3 r)
    {
        float rMagnitude = r.magnitude;
        float q = rMagnitude / h;
        if(rMagnitude < h)
        {
            float term = h - rMagnitude;
            return spikyGradientCoefficient * Mathf.Pow(1-q, 2)/q * r;
        }
        return Vector3.zero;
    }

    public static float WviscosityLaplacian(Vector3 r)
    {
        float rMagnitude = r.magnitude;
        float q = rMagnitude / h;
        if(rMagnitude < h)
        {
            return viscosityLaplacianCoefficient * (1-q);
        }
        return 0f;
    }
}