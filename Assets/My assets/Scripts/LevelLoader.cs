using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor.Rendering;
using UnityEngine;
public class LevelLoader : MonoBehaviour
{
    /*
0,e Empty,,,,,,,,,,,,
1,x Outside corner (double lined corener piece in orginal game),,,,,,,,,,,,
2,o Outside wall (double line in original game),,,,,,,,,,,,
3,c Inside corner  (single lined corener piece in orginal game),,,,,,,,,,,,
4,i Inside wall (single line in orginal game),,,,,,,,,,,,
5,s Standard pallet,,,,,,,,,,,,
6,p Power pallet,,,,,,,,,,,,
7,t A t junction piece for connecting with ajoining regions,,,,,,,,,,,,
8,"g A wall that ghosts can exit through, but not enter.",,,,,,,,,,,,*/

    [SerializeField] GameObject corner;
    [SerializeField] GameObject edge;
    [SerializeField] GameObject innerCorner;
    [SerializeField] GameObject pellet;
    [SerializeField] GameObject powerPellet;
    [SerializeField] GameObject Cherry;
    GameObject air;
    int[][] smartMap;
    GameObject[][] spriteMap;
    GameObject[][] pelletMap;
    Dictionary<int,(GameObject, int)> mask = new Dictionary<int, (GameObject, int)>();
    int mapWidth = 14;
    int[] starterMap = new int[] {  1,2,2,2,2,2,2,2,2,2,2,2,2,7,
                                    2,5,5,5,5,5,5,5,5,5,5,5,5,4,
                                    2,5,3,4,4,3,5,3,4,4,4,3,5,4,
                                    2,6,4,0,0,4,5,4,0,0,0,4,5,4,
                                    2,5,3,4,4,3,5,3,4,4,4,3,5,3,
                                    2,5,5,5,5,5,5,5,5,5,5,5,5,5,
                                    2,5,3,4,4,3,5,3,3,5,3,4,4,4,
                                    2,5,3,4,4,3,5,4,4,5,3,4,4,3,
                                    2,5,5,5,5,5,5,4,4,5,5,5,5,4,
                                    1,2,2,2,2,1,5,4,3,4,4,3,0,4,
                                    0,0,0,0,0,2,5,4,3,4,4,3,0,3,
                                    0,0,0,0,0,2,5,4,4,0,0,0,0,0,
                                    0,0,0,0,0,2,5,4,4,0,3,4,4,8,
                                    2,2,2,2,2,1,5,3,3,0,4,0,0,0,
                                    0,0,0,0,0,0,5,0,0,0,4,0,0,0};
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        air = Instantiate(edge);
        air.GetComponent<SpriteRenderer>().enabled = false;
        int dim = 4;
        int[] adds = new int[dim];
        for (int i = 0; i < dim; i++)
        {
            adds[i] = (int)Math.Pow(2,i);
        }
        for (int i = 1; i < dim; i++)
        {
            for (int j = 0; j < dim; j++)
            {
                int tot = 0;
                for (int k = 0; k < i; k++)
                {
                    tot += adds[(j+k)%dim];
                }
                GameObject obj = innerCorner;
                switch (i)
                {
                    case 1:
                        obj = corner;
                        break;
                    case 2:
                        obj = edge;
                        break;
                }
                mask[tot] = (obj, j);
            }
        }
        starterMap = starterMap.Select((x, y) => new {x, y})
        .GroupBy(z => z.y/mapWidth).Select(z => z.Select(x => x.x)).Select(x => x.Concat(x.ToArray().Reverse())).SelectMany(x => x).ToArray();
        starterMap = starterMap.Concat(starterMap.Reverse()).ToArray();
        spriteMap = initMap(starterMap, mapWidth * 2, 1);
    }
    public GameObject[][] initMap(int[] map, int width, float scale)
    {
        //wall peices: 1, 2, 3, 4, 8
        int[] walls = new int[] {1, 2, 3, 4, 7, 8};
        //empty peices: 0, 5, 6
        int[] pellets = new int[] {5, 6};

        int height = map.Count()/width;
        smartMap = map.Select(x => walls.Contains(x)? 1 : pellets.Contains(x)? x : 0)
        .Select((x, y) => new {x, y})
        .GroupBy(z => z.y/width)
        .Select(a => a.Select(z => z.x))
        .Select(x => x.ToArray()).ToArray();
        
        int[][] wallMap = smartMap.Prepend(Enumerable.Repeat(0, width))
        .Append(Enumerable.Repeat(0, width))
        .Select(a => a.Prepend(0).Append(0))
        .Select(a => a.Zip(a.Skip(1), (v1, v2) => (v1 == 1? 1 : 0) + (v2 == 1? 2 : 0)).ToArray())
        .ToArray();

        GameObject[][] maskMap = wallMap.Zip(wallMap.Skip(1), (v1, v2) => v1
        .Zip(v2, (a, b) => a + (b == 2? 1 : b == 1? 2 : b) * 4)).Select((row, y) => row
        .Select(x => mask.TryGetValue(x, out var temp) ? temp : (air, 0)).Select((x, z) => 
        { GameObject obj = Instantiate(x.Item1); 
        obj.transform.Rotate(new Vector3(0, 0, -90f*x.Item2)); 
        obj.transform.position = new Vector3(z*scale, -y*scale, 0); 
        return obj; }).ToArray()).ToArray();

        for (int i = 0; i < smartMap.Count(); i++)
        {
            for (int j = 0; j < smartMap[i].Count(); j++)
            {
                if (smartMap[i][j] != 0 && smartMap[i][j] != 1)
                {
                    GameObject obj;
                    if (smartMap[i][j] == 5)
                    {
                        obj = Instantiate(pellet);
                    }
                    else
                    {
                        obj = Instantiate(powerPellet);
                    }
                    obj.transform.position = new Vector3((j + 0.5f)*scale, -(i + 0.5f)*scale, 0);
                }
            }
        }

        return maskMap;
    }
    /*public void initMap(int[] map, int width, double scale)
    {
        int height = map.Count()/width;
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                
            }
        }
    }*/

    // Update is called once per frame
    void Update()
    {
        
    }
}
