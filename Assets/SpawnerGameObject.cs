using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Channel {
    R,
    G,
    B,
    A
}

public class SpawnerGameObject : MonoBehaviour {
    public GameObject meshToPlace;

    public GameObject[] userTerrains;
    private List<GameObject> terrains;

    private float rayDistance;
    
    public float minRadius, maxRadius;
    public Texture2D densityTexture;
    private Texture2D readableDensityTexture;

    public Channel maskChannel = Channel.R;
    public float maskClippingThreshold = 0.5f;

    void Start() {
        terrains = new List<GameObject>();
        List<GameObject> culledProps = new List<GameObject>();
        foreach (GameObject terrain in userTerrains) {
            setupTerrain(terrain);
        }
        List<GameObject> list = poisson(minRadius, maxRadius, 30, 100, 100);
        foreach (GameObject spawnedObject in list) {
            placeSpawnedObject(spawnedObject, culledProps);
        }
        foreach (GameObject terrain in terrains) {
            cleanupTerrain(terrain);
        }
        foreach (GameObject spawnedObject in culledProps) {
            cullProp(spawnedObject, list);
        }
    }

    List<GameObject> poisson(float minRad, float maxRad, int k, float width, float height) {
        List<Vector2> points = new List<Vector2>();
        List<Vector2> active = new List<Vector2>();

        List<GameObject> spawnedObjects = new List<GameObject>();

        readableDensityTexture = duplicateTexture(densityTexture);

        Vector2 p0 = new Vector2(Random.Range(0, width), Random.Range(0, height));

        spawnedObjects.Add(createPoint(p0, Mathf.Lerp(minRad, maxRad, getPointRadius(p0, width, height))));
        points.Add(p0);
        active.Add(p0);

        while (active.Count > 0) {
            int random_index = Random.Range(0, active.Count);
            Vector2 p = active[random_index];
            float current_radius = Mathf.Lerp(minRad, maxRad, getPointRadius(p, width, height));

            bool found = false;
            for (int tries = 0; tries < k; ++tries) {
                float theta = Random.Range(0, 360);
                float new_radius = Random.Range(current_radius, 2*current_radius);

                float pnewx = p.x + new_radius * Mathf.Cos(theta * Mathf.Deg2Rad);
                float pnewy = p.y + new_radius * Mathf.Sin(theta * Mathf.Deg2Rad);
                Vector2 pnew = new Vector2(pnewx, pnewy);
                
                GameObject newPointObject = createPoint(pnew, Mathf.Lerp(minRad, maxRad, getPointRadius(pnew, width, height)));

                // if the point is not valid
                if (!isValidPoint((int)width, (int)height, newPointObject)) {
                    // remove the instance for this try
                    // try again
                    Destroy(newPointObject);
                }
                else {
                    // otherwise
                    // the point goes into the active list and the points list. 
                    // we've got it, get out
                    spawnedObjects.Add(newPointObject);
                    points.Add(pnew);
                    active.Add(pnew);
                    found = true;
                    break;
                }
            }

            if (!found)
                active.RemoveAt(random_index);
        }

        foreach (GameObject spawnedObject in spawnedObjects) {
            Destroy(spawnedObject.GetComponent<CircleCollider2D>());
        }

        return spawnedObjects;
    }

    private bool isValidPoint(int width, int height, GameObject point) {
        // check out shit here
        Vector2 p = new Vector2(point.transform.position.x, point.transform.position.y);
        if (p.x < 0 || p.x >= width || p.y < 0 || p.y >= height)
            return false;
        
        CircleCollider2D collide = point.GetComponent<CircleCollider2D>();
        List<Collider2D> collisions = new List<Collider2D>();
        collide.OverlapCollider(new ContactFilter2D().NoFilter(), collisions);
        if (collisions.Count > 0) {
            return false;
        }
        
        return true;
    }

    private GameObject createPoint(Vector2 point, float r) {
        GameObject pointObject = Instantiate(meshToPlace, new Vector3(point.x, point.y, 0), Quaternion.identity);
        CircleCollider2D newCollider = pointObject.AddComponent<CircleCollider2D>() as CircleCollider2D;
        newCollider.radius = r/2f;
        return pointObject;
    }

    private float getPointRadius(Vector2 point, float width, float height) {
        float u = point.x/width;
        float v = point.y/height;
        return sampleTexture(readableDensityTexture, u, v);
    }

    private float sampleTexture(Texture2D texture, float u, float v) {
        return 1-texture.GetPixelBilinear(u, v).r;
    }

    private Texture2D duplicateTexture(Texture2D source) {
        RenderTexture renderTex = RenderTexture.GetTemporary(source.width,
                                                             source.height,
                                                             0,
                                                             RenderTextureFormat.Default,
                                                             RenderTextureReadWrite.Linear);
    
        Graphics.Blit(source, renderTex);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTex;
        Texture2D readableText = new Texture2D(source.width, source.height);
        readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
        readableText.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTex);
        return readableText;
    }

    private void placeSpawnedObject(GameObject spawnedObject, List<GameObject> culledProps) {
        Vector3 currentPosition = spawnedObject.transform.position;
        Vector3 aimingVector = Vector3.down;
        
        // RaycastHit[] hits = Physics.RaycastAll((rayDistance * -aimingVector) + position stuff, aimingVector);

        // lets raycast
        RaycastHit[] hits = Physics.RaycastAll(new Vector3(currentPosition.x, rayDistance, currentPosition.y), aimingVector);
        if (hits.Length > 0) {
            foreach (RaycastHit hit in hits) {
                if (terrains.Contains(hit.collider.gameObject)) {
                    GameObject hitTerrain = hit.collider.gameObject;
                    
                    Color[] vertexColors = hitTerrain.GetComponent<MeshFilter>().sharedMesh.colors;
                    int[] triangles = hitTerrain.GetComponent<MeshFilter>().sharedMesh.triangles;

                    Color color1 = vertexColors[triangles[hit.triangleIndex * 3 + 0]];
                    Color color2 = vertexColors[triangles[hit.triangleIndex * 3 + 1]];
                    Color color3 = vertexColors[triangles[hit.triangleIndex * 3 + 2]];

                    Vector3 baryCenter = hit.barycentricCoordinate;

                    // maybe need to make a function to resolve barycentric coordinates.
                    //Color32 vertColor = color1 * baryCenter.x + color2 * baryCenter.y + color3 * baryCenter.z;
                    Color vertColor = barycentricColInterp(baryCenter, color1, color2, color3);
                    
                    Debug.Log("returned value: " + vertColor.r);
                    float vertColorChannel;

                    switch (maskChannel) {
                        case Channel.R:
                            vertColorChannel = vertColor.r;
                            break;
                        case Channel.G:
                            vertColorChannel = vertColor.g;
                            break;
                        case Channel.B:
                            vertColorChannel = vertColor.b;
                            break;
                        default:
                            vertColorChannel = vertColor.a;
                            break;
                    }

                    if (vertColorChannel < maskClippingThreshold) {
                        culledProps.Add(spawnedObject);
                    }
                    else {
                        // we've hit the first terrain
                        // get the point
                        spawnedObject.transform.position = hit.point;
                    }
                    // go to the next object
                    return;
                }
            }
        }
        else {
            // we had no collisions wee woo
            Debug.Log("no collisions fuck");
        }
    }

    private void setupTerrain(GameObject terrain) {
        GameObject newTerrain = Instantiate(terrain);
        terrains.Add(newTerrain);
        if (newTerrain.GetComponent<MeshFilter>() != null) {
            Component[] components = newTerrain.GetComponents(typeof(Component));
            foreach (Component component in components) {
                if (component.GetType() != typeof(Transform) && component.GetType() != typeof(MeshFilter))
                    Destroy(component);
            }
            MeshCollider collider = newTerrain.AddComponent<MeshCollider>();
            collider.sharedMesh = newTerrain.GetComponent<MeshFilter>().sharedMesh;
            rayDistance = Mathf.Max(rayDistance, collider.sharedMesh.bounds.extents.magnitude+1);
        }
        else {
            // doesnt have a mesh wee woo
        }
    }

    private void cleanupTerrain(GameObject terrain) {
        Destroy(terrain);
    }

    private void cullProp(GameObject spawnedObject, List<GameObject> spawnedObjects) {
        if (spawnedObjects.Contains(spawnedObject)) {
            spawnedObjects.Remove(spawnedObject);
        }
        Destroy(spawnedObject);
    }

    private Color barycentricColInterp(Vector3 coords, Color point1, Color point2, Color point3) {
        float rChannel = barycentricInterp(coords, point1.r, point2.r, point3.r);
        float gChannel = barycentricInterp(coords, point1.g, point2.g, point3.g);
        float bChannel = barycentricInterp(coords, point1.b, point2.b, point3.b);
        float aChannel = barycentricInterp(coords, point1.a, point2.a, point3.a);
        
        Debug.Log("float value: " + rChannel);

        return new Color(rChannel, gChannel, bChannel, aChannel);
    }

    private float barycentricInterp(Vector3 coords, float point1, float point2, float point3) {
        return point1 * coords.x + point2 * coords.y + point3 * coords.z;
    }
}
