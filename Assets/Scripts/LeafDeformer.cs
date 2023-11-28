using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class LeafDeformer : MonoBehaviour
{
    public Transform player;
    public float springForce = 20f;
    public float damping = 5f;
    public float forceMultiplier = 50f;

    Mesh mesh;
    Vector3[] originalVertices, displacedVertices;
    Vector3[] vertexVelocities;

    float uniformScale = 1f;

    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        originalVertices = mesh.vertices;
        displacedVertices = new Vector3[originalVertices.Length];
        for (int i = 0; i < originalVertices.Length; i++)
        {
            displacedVertices[i] = originalVertices[i];
        }
        vertexVelocities = new Vector3[originalVertices.Length];
    }

    void Update()
    {
        uniformScale = transform.localScale.x;
        for (int i = 0; i < displacedVertices.Length; i++)
        {
            UpdateVertex(i);
        }
        mesh.vertices = displacedVertices;
        mesh.RecalculateNormals();
    }

    private void OnCollisionEnter(Collision collision)
    {
        Ray ray = new Ray(collision.transform.position, -collision.transform.up);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            int closestVertexIndex = FindClosestVertexIndex(originalVertices, hit.point);

            Color vertexColor = mesh.colors[closestVertexIndex];
            float vertexForce = 1f - vertexColor.r;

            float forceMagnitude = player.GetComponent<Rigidbody>().mass * vertexForce * forceMultiplier;
            AddDeformingForce(collision.transform.position, forceMagnitude);
        }
    }

    void UpdateVertex(int i)
    {
        Vector3 velocity = vertexVelocities[i];
        Vector3 displacement = displacedVertices[i] - originalVertices[i];
        displacement *= uniformScale;
        velocity -= displacement * springForce * Time.deltaTime;
        velocity *= 1f - damping * Time.deltaTime;
        vertexVelocities[i] = velocity;
        displacedVertices[i] += velocity * (Time.deltaTime / uniformScale);
    }

    public void AddDeformingForce(Vector3 point, float force)
    {
        point = transform.InverseTransformPoint(point);
        for (int i = 0; i < displacedVertices.Length; i++)
        {
            AddForceToVertex(i, point, force);
        }
    }

    void AddForceToVertex(int i, Vector3 point, float force)
    {
        Vector3 pointToVertex = displacedVertices[i] - point;
        pointToVertex *= uniformScale;
        float attenuatedForce = force / (1f + pointToVertex.sqrMagnitude);
        float velocity = attenuatedForce * Time.deltaTime;
        vertexVelocities[i] += pointToVertex.normalized * velocity;
    }

    int FindClosestVertexIndex(Vector3[] verticies, Vector3 point)
    {
        float closestDistance = Mathf.Infinity;
        int closestIndex = -1;

        for (int i = 0; i < verticies.Length; i++)
        {
            float distance = Vector3.Distance(verticies[i], point);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }
        return closestIndex;
    }
}