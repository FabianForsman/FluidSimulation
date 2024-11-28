using UnityEngine;
using System.Threading.Tasks;
using System;

public class SphereSpawner : MonoBehaviour
{
    public float particleSize; // Radius of the spheres.
    private float mass = 1; // Mass of the spheres.
    public Vector2 boundsSize; // Half the size of the bounds.
    private Color particleColor = new Color(0.2f, 0.6f, 1f); // Color of the spheres.

    public float gravity; // Gravity value (set a default value).
    public float collisionDamping; // How much velocity is lost on collision (set a default value).

    public int numParticles; // Number of particles to spawn.
    private float particleSpacing; // Spacing between particles (set a default value).

    public float smoothingRadius; // Radius of the smoothing kernel (set a default value).

    private Vector3[] velocities; // The velocity of the sphere.
    private Vector3[] positions; // The position of the sphere.
    private float[] particleProperties; // The property of the sphere.

    private GameObject spherePrefab; // The sphere prefab;
    private GameObject[] particles; // Add an array to hold the sphere game objects.

    private bool started = false;
    private bool drawGizmos = true;

    private Vector3[] points;
    private float radius;
    private Entry[] spatialLookup;
    private int[] startIndicies;
    private readonly (int x, int y)[] cellOfsets = new (int x, int y)[] {
        (0, 0), (1, 0), (0, 1), (-1, 0), (0, -1),
        (1, 1), (-1, 1), (-1, -1), (1, -1)
    };

    private struct Entry : IComparable<Entry>
{
    public int pointIndex;
    public uint cellKey;

    public Entry(int pointIndex, uint cellKey)
    {
        this.pointIndex = pointIndex;
        this.cellKey = cellKey;
    }

    public int CompareTo(Entry other)
    {
        // Compare by cellKey (you can change this if you want to compare by a different field)
        return this.cellKey.CompareTo(other.cellKey);
    }
}

    
    private void Start()
    {
        started = true;
        gravity = 0; //9.81f;
        numParticles = 1500;
        positions = new Vector3[numParticles];
        velocities = new Vector3[numParticles];
        particles = new GameObject[numParticles];
        particleProperties = new float[numParticles];
        densities = new float[numParticles];
        smoothingRadius = particleSize * 10;
        collisionDamping = 0.8f;
        boundsSize = new Vector3(50, 30, 40);
        pressureMultiplier = 3;
        targetDensity = 8f;

        spatialLookup = new Entry[numParticles];
        startIndicies = new int[numParticles];

        int particlesPerRow = (int)Mathf.Sqrt(numParticles);
        int particlesPerCol = (numParticles - 1) / particlesPerRow + 1;
        float spacing = particleSize * 2 + particleSpacing;

        /*for (int i = 0; i < numParticles; i++)
        {
            float x = (i % particlesPerRow - particlesPerRow / 2 + 0.5f) * spacing;
            float y = (i / particlesPerRow - particlesPerCol / 2 + 0.5f) * spacing;
            Vector3 position = new Vector3(x, y, 0);
            positions[i] = position;
            // Create a sphere game object and store it in the array.
            particles[i] = CreateParticle(position);
        }*/

        CreateParticles(numParticles);
        UpdateDensities();
    }

    private void Update()
    {
        SimulationStep(Time.deltaTime);
        UpdateSpatialLookup(positions, smoothingRadius);
        for (int i = 0; i < positions.Length; i++)
        {
            ForeachPointWithinRadius(positions[i]);
            DrawCircle(i, particleSize, particleColor);
        }
    }

    private void DrawCircle(int index, float size, Color color)
    {
        particles[index].transform.position = positions[index];
        particles[index].GetComponent<Renderer>().material.color = particleColor;
        particles[index].transform.localScale = Vector3.one * size * 2;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, boundsSize);

        if(!started && drawGizmos) {
            Gizmos.color = particleColor;
            int particlesPerRow = (int)Mathf.Sqrt(numParticles);
            int particlesPerCol = (numParticles - 1) / particlesPerRow + 1;
            float spacing = particleSize * 2 + particleSpacing;
            for (int i = 0; i < numParticles; i++)
            {
                float x = (i % particlesPerRow - particlesPerRow / 2 + 0.5f) * spacing;
                float y = (i / particlesPerRow - particlesPerCol / 2 + 0.5f) * spacing;
                Vector3 position = new Vector3(x, y, 0);
                Gizmos.DrawSphere(position, particleSize);
            }
        }        
    }

    private GameObject CreateParticle(Vector3 position)
    {
        GameObject particle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        particle.transform.localScale = Vector3.one * particleSize * 2;
        particle.GetComponent<SphereCollider>().enabled = false;
        particle.transform.position = position;
        particle.GetComponent<Renderer>().material.color = particleColor;

        return particle;
    }

    private void CreateParticles(int nr)
    {
        System.Random rng = new System.Random(nr);
        for (int i = 0; i < positions.Length; i++)
        {
            float x = (float)(rng.NextDouble() - 0.5) * boundsSize.x;
            float y = (float)(rng.NextDouble() - 0.5) * boundsSize.y;
            positions[i] = new Vector3(x, y, 0);
            velocities[i] = Vector3.zero;
            particles[i] = CreateParticle(positions[i]);
        }
    }


    private void ResolveCollisions(ref Vector3 position, ref Vector3 velocity)
    {
        Vector2 halfBoundsSize = boundsSize / 2 - Vector2.one * particleSize; // The size of the bounds, minus the size of the sphere.

        if (Mathf.Abs(position.x) > halfBoundsSize.x)
        {
            position.x = halfBoundsSize.x * Mathf.Sign(position.x);
            velocity.x *= -1 * collisionDamping;
        }
        if (Mathf.Abs(position.y) > halfBoundsSize.y)
        {
            position.y = halfBoundsSize.y * Mathf.Sign(position.y);
            velocity.y *= -1 * collisionDamping;
        }
    }

    // Smoothing kernel for SPH fluids (https://en.wikipedia.org/wiki/Smoothed-particle_hydrodynamics)
    static float SmoothingKernel(float radius, float dst)
    {
        if (dst >= radius) return 0;

        float volume = Mathf.PI * Mathf.Pow(radius, 4) / 6; // Used to normalize the kernel
        return (radius - dst) * (radius - dst) / volume;
    }

    
    static float SmoothingKernelDerivative(float dst, float radius)
    {
        if (dst >= radius) return 0;

        float scale = 12 / (Mathf.PI * Mathf.Pow(radius, 4));
        return (dst - radius) * scale;
    }

    private float CalculateDensity(Vector3 samplePoint)
    {
        float density = 0;

        foreach(Vector3 position in positions)
        {
            float dst = (position - samplePoint).magnitude;
            float influence = SmoothingKernel(smoothingRadius, dst);
            density += mass * influence;
        }

        return density;
    }

    private float[] densities;
    private void UpdateDensities()
    {
        Parallel.For(0, numParticles, i => {
            densities[i] = CalculateDensity(positions[i]);
        });
    }

    public float targetDensity;
    public float pressureMultiplier;
    private float ConvertDensityToPressure(float density)
    {
        float densityError = density - targetDensity;
        float pressure = densityError * -pressureMultiplier;
        return pressure;
    }
    
    private Vector3 CalculatePressureForce(int particleIndex)
    {
        Vector3 pressureForce = Vector3.zero;

        for (int otherParticleIndex = 0; otherParticleIndex < numParticles; otherParticleIndex++)
        {
            if(particleIndex == otherParticleIndex) continue;

            Vector3 offset = positions[otherParticleIndex] - positions[particleIndex]; // Offset between the two particles
            float dst = offset.magnitude; // Distance between the two particles
            Vector3 dir = dst == 0 ? GetRandomDir(): offset / dst; // If they are at the same position, use a random direction
            
            float slope = SmoothingKernelDerivative(dst, smoothingRadius); // The slope of the smoothing kernel at the distance between the two particles
            float density = densities[otherParticleIndex];
            float sharedPressure = CalculateSharedPressure(density, densities[particleIndex]);
            pressureForce += sharedPressure * dir * slope * mass / density;
        }
        return pressureForce;
    }

    private float CalculateSharedPressure(float densityA, float densityB)
    {
        float pressureA = ConvertDensityToPressure(densityA);
        float pressureB = ConvertDensityToPressure(densityB);
        return (pressureA + pressureB) / 2;
    }

    private Vector3 GetRandomDir()
    {
        System.Random rng = new System.Random();
        float x = (float)(rng.NextDouble() - 0.5);
        float y = (float)(rng.NextDouble() - 0.5);
        float z = 0;
        return new Vector3(x, y, z);
    }

    private void SimulationStep(float deltaTime)
    {
        Parallel.For(0, numParticles, i =>
        {
            velocities[i] -= Vector3.down * -gravity * deltaTime;
            densities[i] = CalculateDensity(positions[i]);
        });

        Parallel.For(0, numParticles, i =>
        {
            Vector3 pressureForce = CalculatePressureForce(i);
            Vector3 pressureAcceleration = pressureForce / densities[i]; // a = F/m
            velocities[i] += pressureAcceleration * deltaTime;
        });

        Parallel.For(0, numParticles, i =>
        {
            positions[i] += velocities[i] * deltaTime;
            ResolveCollisions(ref positions[i], ref velocities[i]);
        });
    }

    public void UpdateSpatialLookup(Vector3[] points, float radius)
    {
        Parallel.For(0, points.Length, i =>
        {
            (int cellX, int cellY) = PositionToCellCoord(points[i], radius);
            uint cellKey = GetKeyFromHash(HashCell(cellX, cellY));
            spatialLookup[i] = new Entry(i, cellKey);
            startIndicies[i] = int.MaxValue;
        });

        Array.Sort(spatialLookup);

        Parallel.For(0, points.Length, i =>
        {
            uint key = spatialLookup[i].cellKey;
            uint keyPrev = i == 0 ? uint.MaxValue : spatialLookup[i - 1].cellKey;
            if (key != keyPrev)
            {
                startIndicies[key] = i;
            }
        });
    }

    public (int x, int y) PositionToCellCoord(Vector3 point, float radius)
    {
        int x = (int)(point.x / radius);
        int y = (int)(point.y / radius);
        return (x, y);
    }

    public uint HashCell(int x, int y)
    {
        uint a = (uint)x * 15823;
        uint b = (uint)y * 9737333;
        return a + b;
    }

    public uint GetKeyFromHash(uint hash)
    {
        return hash % (uint) spatialLookup.Length;
    }

    public void ForeachPointWithinRadius(Vector3 samplePoint)
    {
        (int centreX, int centreY) = PositionToCellCoord(samplePoint, radius);
        float sqrRadius = radius * radius;

        foreach ((int offsetX, int offsetY) in cellOfsets)
        {
            uint key = GetKeyFromHash(HashCell(centreX + offsetX, centreY + offsetY));
            int cellStartIndex = startIndicies[key];

            for (int i = cellStartIndex; i < spatialLookup.Length; i++)
            {
                if (spatialLookup[i].cellKey != key) break;

                int particleIndex = spatialLookup[i].pointIndex;
                float sqrDst = (positions[particleIndex] - samplePoint).sqrMagnitude;
                if (sqrDst <= sqrRadius)
                {
                    DrawCircle(particleIndex, particleSize * 1.5f, Color.red);
                }
            }
        }
    }
    
}
