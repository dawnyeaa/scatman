using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnerGameObject : MonoBehaviour {
    public GameObject meshToPlace;
    
    public float radius;
    // Start is called before the first frame update
    void Start() {
        List<Vector2> list = poisson(radius, 30, 100, 100);
        list.Add(new Vector2(0, 0));
        list.Add(new Vector2(3, 0));
        foreach (Vector2 point in list) {
            Instantiate(meshToPlace, new Vector3(point.x-50, 0, point.y-50), Quaternion.identity);
        }
    }

    List<Vector2> poisson(float rad, int k, float width, float height) {
        List<Vector2> points = new List<Vector2>();
        List<Vector2> active = new List<Vector2>();

        Vector2 p0 = new Vector2(Random.Range(0, width), Random.Range(0, height));

        Vector2[,] grid;
        float cellsize = Mathf.Ceil(rad/1.41421356237f);

        int ncells_width = (int)Mathf.Ceil(width/cellsize) + 1;
        int ncells_height = (int)Mathf.Ceil(height/cellsize) + 1;

        grid = new Vector2[ncells_width,ncells_height];
        for (int i = 0; i < ncells_width; ++i) {
            for (int j = 0; j < ncells_height; ++j) {
                grid[i,j] = new Vector2(Mathf.Infinity, Mathf.Infinity);
            }
        }

        insertPoint(grid, cellsize, p0);
        points.Add(p0);
        active.Add(p0);

        while (active.Count > 0) {
            int random_index = Random.Range(0, active.Count);
            Vector2 p = active[random_index];

            bool found = false;
            for (int tries = 0; tries < k; ++tries) {
                float theta = Random.Range(0, 360);
                float new_radius = Random.Range(rad, 2*rad);

                float pnewx = p.x + new_radius * Mathf.Cos(theta * Mathf.Deg2Rad);
                float pnewy = p.y + new_radius * Mathf.Sin(theta * Mathf.Deg2Rad);
                Vector2 pnew = new Vector2(pnewx, pnewy);

                if (!isValidPoint(grid, cellsize, (int)width, (int)height, ncells_width, ncells_height, pnew, rad))
                    continue;

                points.Add(pnew);
                insertPoint(grid, cellsize, pnew);
                active.Add(pnew);
                found = true;
                break;
            }

            if (!found)
                active.RemoveAt(random_index);
        }

        return points;
    }

    bool isValidPoint(Vector2[,] grid, float cellsize,
                      int width, int height,
                      int gwidth, int gheight,
                      Vector2 p, float radius) {
        // check out shit here
        if (p.x < 0 || p.x >= width || p.y < 0 || p.y >= height)
            return false;
        
        int xindex = (int)Mathf.Floor(p.x/cellsize);
        int yindex = (int)Mathf.Floor(p.y/cellsize);
        int i0 = Mathf.Max(xindex - 1, 0);
        int i1 = Mathf.Min(xindex + 1, gwidth - 1);
        int j0 = Mathf.Max(yindex - 1, 0);
        int j1 = Mathf.Min(yindex + 1, gheight - 1);

        for (int i = i0; i <= i1; ++i)
            for (int j = j0; j <= j1; ++j)
                if (grid[i,j].x != Mathf.Infinity && grid[i,j].y != Mathf.Infinity)
                    if (Vector2.Distance(grid[i,j], p) < radius)
                        return false;
        
        return true;
    }

    void insertPoint(Vector2[,] grid, float cellsize, Vector2 point) {
        int xindex = (int)Mathf.Floor(point.x/cellsize);
        int yindex = (int)Mathf.Floor(point.y/cellsize);
        grid[xindex,yindex] = point;
    }
}
