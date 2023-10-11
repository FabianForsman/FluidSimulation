using UnityEngine;

public class SphereSpawner : MonoBehaviour
{
    public float particleSize; // Radius of the spheres.
    public Vector2 boundsSize; // Half the size of the bounds.
    private Color sphereColor = new Color(0.2f, 0.6f, 1f); // Color of the spheres.

    public float gravity; // Gravity value (set a default value).
    public float collisionDamping; // How much velocity is lost on collision (set a default value).

    public int numParticles; // Number of particles to spawn.
    public float particleSpacing; // Spacing between particles (set a default value).

    private Vector3[] velocities; // The velocity of the sphere.
    private Vector3[] positions; // The position of the sphere.

    private GameObject spherePrefab; // The sphere prefab;
    private GameObject[] spheres; // Add an array to hold the sphere game objects.

    private bool drawGizmos = true;
    private void Start()
    {
        drawGizmos = false;
        positions = new Vector3[numParticles];
        velocities = new Vector3[numParticles];

        int particlesPerRow = (int)Mathf.Sqrt(numParticles);
        int particlesPerCol = (numParticles - 1) / particlesPerRow + 1;
        float spacing = particleSize * 2 + particleSpacing;
        spheres = new GameObject[numParticles];

        for (int i = 0; i < numParticles; i++)
        {
            float x = (i % particlesPerRow - particlesPerRow / 2 + 0.5f) * spacing;
            float y = (i / particlesPerRow - particlesPerCol / 2 + 0.5f) * spacing;
            Vector3 position = new Vector3(x, y, 0);
            positions[i] = position;
            // Create a sphere game object and store it in the array.
            spheres[i] = CreateSphere(position, particleSize, sphereColor);
        }
    }

    private void Update()
    {

        for (int i = 0; i < positions.Length; i++)
        {
            velocities[i] += Vector3.down * gravity * Time.deltaTime;
            positions[i] += velocities[i] * Time.deltaTime;
            ResolveCollisions(ref positions[i], ref velocities[i]);

            DrawCircle(i, particleSize, sphereColor);
        }
    }

    private void DrawCircle(int index, float size, Color color)
    {
        spheres[index].transform.position = positions[index];
        spheres[index].GetComponent<Renderer>().material.color = color;
        spheres[index].transform.localScale = Vector3.one * size * 2;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, boundsSize);

        if(drawGizmos) {
            Gizmos.color = sphereColor;
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

    private GameObject CreateSphere(Vector3 position, float radius, Color color)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.localScale = Vector3.one * radius * 2;
        sphere.GetComponent<SphereCollider>().enabled = false;
        sphere.transform.position = position;
        sphere.GetComponent<Renderer>().material.color = color;

        return sphere;

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
}
