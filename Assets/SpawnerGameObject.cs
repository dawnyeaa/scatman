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
    public GameObject userMeshToPlace;
    private GameObject meshToPlace;

    public GameObject[] userTerrains;
    private List<GameObject> terrains;

    private float rayDistance;
    
    public float minRadius, maxRadius;
    public Texture2D densityTexture;
    private Texture2D readableDensityTexture;

    public Channel maskChannel = Channel.R;
    public float maskClippingThreshold = 0.5f;

    public bool setToStatic = true;

    public int k = 10;

    public Vector2 corner1 = new Vector2(-50, -50);
    public Vector2 corner2 = new Vector2(50, 50);

    public float value = 10;

    public void Spawn() {
        terrains = new List<GameObject>();
        List<GameObject> culledProps = new List<GameObject>();
        foreach (GameObject terrain in userTerrains) {
            setupTerrain(terrain);
        }
        setupMeshToPlace(userMeshToPlace);
        List<GameObject> list = poisson(minRadius, maxRadius, k, corner1, corner2);
        // List<GameObject> list = dumb(100f, 100f);
        foreach (GameObject spawnedObject in list) {
            placeSpawnedObject(spawnedObject, culledProps);
        }
        foreach (GameObject terrain in terrains) {
            cleanupTerrain(terrain);
        }
        cleanupMeshToPlace(meshToPlace);
        foreach (GameObject spawnedObject in culledProps) {
            cullProp(spawnedObject, list);
        }
    }

    List<GameObject> dumb(float width, float height) {
        List<GameObject> spawnedObjects = new List<GameObject>();
        for (int i = 0; i < 50; ++i) {
            for (int j = 0; j < 50; ++j) {
                spawnedObjects.Add(Instantiate(userMeshToPlace, new Vector3((i/50f)*width, (j/50f)*height, 0), Quaternion.identity, this.transform));
            }
        }
        return spawnedObjects;
    }

    private Vector2 getWidthAndHeight(Vector2 corner1, Vector2 corner2) {
        return new Vector2(corner2.x-corner1.x, corner2.y-corner1.y);
    }

    List<GameObject> poisson(float minRad, float maxRad, int k, Vector2 corner1, Vector2 corner2) {
        List<Vector2> points = new List<Vector2>();
        List<Vector2> active = new List<Vector2>();

        List<GameObject> spawnedObjects = new List<GameObject>();

        readableDensityTexture = duplicateTexture(densityTexture);

        Vector2 p0 = new Vector2(Random.Range(corner1.x, corner2.x), Random.Range(corner1.y, corner2.y));

        spawnedObjects.Add(createPoint(p0, Mathf.Lerp(minRad, maxRad, getPointRadius(p0, corner1, corner2))));
        points.Add(p0);
        active.Add(p0);

        int iterations = 0;

        while (active.Count > 0) {
            int random_index = Random.Range(0, active.Count);
            Vector2 p = active[random_index];
            float current_radius = Mathf.Lerp(minRad, maxRad, getPointRadius(p, corner1, corner2));

            bool found = false;
            for (int tries = 0; tries < k; ++tries) {
                float theta = Random.Range(0, 360);
                float new_radius = Random.Range(current_radius, 2*current_radius);

                float pnewx = p.x + new_radius * Mathf.Cos(theta * Mathf.Deg2Rad);
                float pnewy = p.y + new_radius * Mathf.Sin(theta * Mathf.Deg2Rad);
                Vector2 pnew = new Vector2(pnewx, pnewy);

                // if the point is not valid
                if (!isValidPoint(corner1, corner2, pnew)) {
                    // remove the instance for this try
                    // try again
                    continue;
                }
                else {
                    // otherwise
                    // the point goes into the active list and the points list. 
                    // we've got it, get out
                    spawnedObjects.Add(createPoint(pnew, Mathf.Lerp(minRad, maxRad, getPointRadius(pnew, corner1, corner2))));
                    points.Add(pnew);
                    active.Add(pnew);
                    found = true;
                    break;
                }
            }

            if (!found)
                active.RemoveAt(random_index);

            ++iterations;
        }

        foreach (GameObject spawnedObject in spawnedObjects) {
            DestroyImmediate(spawnedObject.GetComponent<CircleCollider2D>());
        }

        return spawnedObjects;
    }

    private bool isValidPoint(Vector2 corner1, Vector2 corner2, Vector2 point) {
        // check out shit here
        if (point.x < corner1.x || point.x >= corner2.x || point.y < corner1.y || point.y >= corner2.y)
            return false;
        
        if (Physics2D.OverlapPointAll(point).Length > 0)
            return false;
        
        return true;
    }

    private GameObject createPoint(Vector2 point, float r) {
        GameObject pointObject = Instantiate(meshToPlace, new Vector3(point.x, point.y, 0), Quaternion.identity, this.transform);
        // if (setToStatic)
        //     pointObject.isStatic = true;
        CircleCollider2D newCollider = pointObject.GetComponent<CircleCollider2D>();
        newCollider.radius = r;
        return pointObject;
    }

    private void setupMeshToPlace(GameObject userMeshToPlace) {
        meshToPlace = Instantiate(userMeshToPlace);
        if (setToStatic)
            meshToPlace.isStatic = true;
        CircleCollider2D newCollider = meshToPlace.AddComponent<CircleCollider2D>() as CircleCollider2D;
    }

    private void cleanupMeshToPlace(GameObject meshToPlace) {
        DestroyImmediate(meshToPlace);
    }

    private float getPointRadius(Vector2 point, Vector2 corner1, Vector2 corner2) {
        float u = Mathf.InverseLerp(corner1.x, corner2.x, point.x);
        float v = Mathf.InverseLerp(corner1.y, corner2.y, point.y);
        float sampled = sampleTexture(readableDensityTexture, u, v);
        return sampled;
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

                    if (vertexColors.Length == 0)
                        Debug.LogError("There are no vertex colors on this mesh");

                    Color color1 = vertexColors[triangles[hit.triangleIndex * 3 + 0]];
                    Color color2 = vertexColors[triangles[hit.triangleIndex * 3 + 1]];
                    Color color3 = vertexColors[triangles[hit.triangleIndex * 3 + 2]];

                    Vector3 baryCenter = hit.barycentricCoordinate;

                    // maybe need to make a function to resolve barycentric coordinates.
                    //Color32 vertColor = color1 * baryCenter.x + color2 * baryCenter.y + color3 * baryCenter.z;
                    Color vertColor = barycentricColInterp(baryCenter, color1, color2, color3);
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
        if (terrain.GetComponent<MeshFilter>() != null) {
            GameObject newTerrain = new GameObject();
            newTerrain.transform.position = terrain.transform.position;
            newTerrain.transform.rotation = terrain.transform.rotation;
            newTerrain.transform.localScale = terrain.transform.localScale;
            newTerrain.transform.SetParent(transform);

            MeshFilter newMeshFilter = newTerrain.AddComponent<MeshFilter>();
            MeshCollider newCollider = newTerrain.AddComponent<MeshCollider>();
            
            Mesh terrainMesh = terrain.GetComponent<MeshFilter>().sharedMesh;
            newMeshFilter.sharedMesh = terrainMesh;
            newCollider.sharedMesh = terrainMesh;
            rayDistance = Mathf.Max(rayDistance, terrainMesh.bounds.extents.magnitude+1);
            
            terrains.Add(newTerrain);
        }
        else {
            // doesnt have a mesh wee woo
        }
    }

    private void cleanupTerrain(GameObject terrain) {
        DestroyImmediate(terrain);
    }

    private void cullProp(GameObject spawnedObject, List<GameObject> spawnedObjects) {
        if (spawnedObjects.Contains(spawnedObject)) {
            spawnedObjects.Remove(spawnedObject);
        }
        DestroyImmediate(spawnedObject);
    }

    public void ResetGenerated() {
        for (int i = transform.childCount; i > 0; --i)
            DestroyImmediate(transform.GetChild(0).gameObject);
    }

    private Color barycentricColInterp(Vector3 coords, Color point1, Color point2, Color point3) {
        float rChannel = barycentricInterp(coords, point1.r, point2.r, point3.r);
        float gChannel = barycentricInterp(coords, point1.g, point2.g, point3.g);
        float bChannel = barycentricInterp(coords, point1.b, point2.b, point3.b);
        float aChannel = barycentricInterp(coords, point1.a, point2.a, point3.a);

        return new Color(rChannel, gChannel, bChannel, aChannel);
    }

    private float barycentricInterp(Vector3 coords, float point1, float point2, float point3) {
        return point1 * coords.x + point2 * coords.y + point3 * coords.z;
    }
}
