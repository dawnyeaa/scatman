using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnerGameObject : MonoBehaviour {
    public GameObject meshToPlace;
    
    public float minRadius, maxRadius;
    public Texture2D densityTexture;

    void Start() {
        List<Vector2> list = poisson(minRadius, maxRadius, 30, 100, 100);
        foreach (Vector2 point in list) {
            
        }
    }

    List<Vector2> poisson(float minRad, float maxRad, int k, float width, float height) {
        List<Vector2> points = new List<Vector2>();
        List<Vector2> active = new List<Vector2>();

        Vector2 p0 = new Vector2(Random.Range(0, width), Random.Range(0, height));

        createPoint(p0, Mathf.Lerp(minRad, maxRad, getPointRadius(p0, width, height)));
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
                    GameObject.Destroy(newPointObject);
                }
                else {
                    // otherwise
                    // the point goes into the active list and the points list. 
                    // we've got it, get out
                    points.Add(pnew);
                    active.Add(pnew);
                    found = true;
                    break;
                }
            }

            if (!found)
                active.RemoveAt(random_index);
        }

        return points;
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
        return sampleTexture(densityTexture, u, v);
    }

    private float sampleTexture(Texture2D texture, float u, float v) {
        return 1-texture.GetPixelBilinear(u, v).r;
    }
}
