using UnityEngine;

public class SphereSpawner : MonoBehaviour
{
    public GameObject spherePrefab; // The sphere prefab.
    public float particleSize; // Radius of the spheres.
    public Vector2 boundsSize; // Half the size of the bounds.
    private Color sphereColor = Color.blue; // Color of the spheres.
    

    private GameObject currentSphere; // Reference to the currently spawned sphere.

    public float gravity; // Gravity value.
    public float collisionDamping; // How much velocity is lost on collision.

    private void Start()
    {
        particleSize = 1;
        collisionDamping = 1;
        SpawnSphere(); // Start by spawning the first sphere.
    }

    private void SpawnSphere()
    {
        currentSphere = Instantiate(spherePrefab, transform.position, Quaternion.identity);
        currentSphere.GetComponent<Renderer>().material.color = sphereColor;
        velocity = Vector3.zero; // Reset the velocity for the new sphere.
    }

    private Vector3 velocity; // The velocity of the sphere.
    private Vector3 position; // The position of the sphere.

    private void Update()
    {
        if (currentSphere != null)
        {
            velocity += Vector3.down * gravity * Time.deltaTime;
            position += velocity * Time.deltaTime;
            ResolveCollisions(position);

            DrawCircle(position, particleSize, sphereColor);
        }
    }

    private void DrawCircle(Vector3 position, float particleSize, Color color)
    {
        currentSphere.transform.position = position;
        currentSphere.transform.localScale = Vector3.one * particleSize * 2;
        currentSphere.GetComponent<Renderer>().material.color = color;
    }
    

    private void ResolveCollisions(Vector3 position)
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
