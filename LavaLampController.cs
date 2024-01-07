using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public struct Blob {

    public float size;
    public Vector2 position;

    [HideInInspector]
    public float temperature;

    [HideInInspector]
    public Vector2 velocity;
}

public class LavaLampController : MonoBehaviour
{
    public Material lavaLampMaterial;
    public Renderer quad;

    [Header("Bounds")]
    public Vector2 lampBounds;
    public float topPercent = 1;

    [Header("Physics")]
    public float temperatureGradient = 0.5f;
    public float repulsiveForce = 0.1f;
    public float viscosity = 2f;

    [Header("Blobs")]
    public Blob[] blobs;

    protected float[] blobPositions;
    protected float[] blobWeights;

    protected void OnDrawGizmosSelected() {
        
        Vector3 center = quad.transform.position;

        Vector2 extents = 0.5f * lampBounds;
        Vector3 bottomLeft = center - (Vector3)extents;

        extents.x *= -1;
        Vector3 bottomRight = center - (Vector3)extents;

        extents.x *= topPercent;
        Vector3 topLeft = center + (Vector3)extents;

        extents.x *= -1;
        Vector3 topRight = center + (Vector3)extents;

        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);

        Vector3 origin = center - lampBounds.y * 0.5f * Vector3.up;
        for (int i = 0; i < blobs.Length; i++) {
            Gizmos.DrawWireSphere(origin + (Vector3)blobs[i].position, 0.05f);
        }
    }

    protected void OnValidate() {

        lavaLampMaterial.SetVector("_LampSize", lampBounds);

        if (Application.isPlaying && blobWeights != null) {

            for (int i = 0; i < blobs.Length; i++) {
                blobWeights[i] = blobs[i].size;
            }

            lavaLampMaterial.SetFloatArray("_BlobWeights", blobWeights);
        }
    }

    protected void Awake() {

        // set up material
        quad.material = lavaLampMaterial;
        lavaLampMaterial.SetFloat("_TopPercent", topPercent);
        lavaLampMaterial.SetVector("_LampSize", lampBounds);

        blobPositions = new float[blobs.Length * 2];
        blobWeights = new float[blobs.Length];

        for (int i = 0; i < blobs.Length; i++) {
            blobWeights[i] = blobs[i].size;
        }

        lavaLampMaterial.SetFloatArray("_BlobWeights", blobWeights);

        RandomizeBlobs(blobs);
    }

    protected void FixedUpdate() {

        UpdateBlobs();

        for (int i = 0; i < blobs.Length; i++) {
            blobPositions[2 * i + 0] = blobs[i].position.x;
            blobPositions[2 * i + 1] = blobs[i].position.y;
        }

        lavaLampMaterial.SetFloatArray("_BlobPositions", blobPositions);
    }

    protected void RandomizeBlobs(Blob[] array) {

        for (int i = 0; i < array.Length; i++) {

            array[i].temperature = Random.Range(-1f, 1f);
            array[i].velocity = new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f));
        }
    }

    protected void UpdateBlobs() {

        for (int i = 0; i < blobs.Length; i++) {

            // update temperature
            float centerHeight = lampBounds.y * 0.5f;
            float deltaTemp = (centerHeight - blobs[i].position.y) * temperatureGradient / (lampBounds.y * 0.5f);
            blobs[i].temperature += Mathf.Clamp(deltaTemp, -temperatureGradient, temperatureGradient) * Time.fixedDeltaTime;

            // vertical acceleration from temperature
            float mass = Mathf.Max(0.001f, blobs[i].size);
            blobs[i].velocity += Vector2.up * blobs[i].temperature * Time.fixedDeltaTime / mass;

            // repulsize force
            for (int j = 0; j < blobs.Length; j++) {

                if (i != j) {

                    Vector2 displacement = blobs[i].position - blobs[j].position;
                    float distanceSqr = displacement.sqrMagnitude;
                    blobs[i].velocity += Time.fixedDeltaTime * repulsiveForce * displacement / (distanceSqr * distanceSqr);
                }
            }

            // wall force
            float verticalPercentage = Mathf.Lerp(1, topPercent, blobs[i].position.y / lampBounds.y);
            blobs[i].velocity += blobs[i].position.y < 0            ? Time.fixedDeltaTime * Vector2.up    : Vector2.zero;
            blobs[i].velocity += blobs[i].position.y > lampBounds.y ? Time.fixedDeltaTime * Vector2.down  : Vector2.zero;

            blobs[i].velocity += blobs[i].position.x < -lampBounds.x * 0.5f * verticalPercentage
                                ? Time.fixedDeltaTime * Vector2.right : Vector2.zero;
            blobs[i].velocity += blobs[i].position.x > lampBounds.x * 0.5f * verticalPercentage 
                                ? Time.fixedDeltaTime * Vector2.left : Vector2.zero;

            // update position
            blobs[i].velocity -= Time.fixedDeltaTime * blobs[i].velocity * viscosity;
            blobs[i].position += blobs[i].velocity * Time.fixedDeltaTime;

        }
    }
}
