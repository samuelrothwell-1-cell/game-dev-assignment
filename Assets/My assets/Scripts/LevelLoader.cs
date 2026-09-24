using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO.Hashing;
using System.Linq;
using JetBrains.Annotations;
using Unity.Collections;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using System.Runtime;
using UnityEngine.UIElements;
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
    [SerializeField] Player player;
    [SerializeField] AudioPlayer audioPlayer;
    GameObject air;
    float scale;
    int[][] smartMap;
    GameObject[][] spriteMap;
    GameObject[][] pelletMap;
    [SerializeField] bool manualGeneration = false;
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
        scale = 1;
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
        starterMap = GenerateMap(mapWidth*2, 20);

        if (manualGeneration)
        {
            starterMap = starterMap.Select((x, y) => new {x, y})
            .GroupBy(z => z.y/mapWidth).Select(z => z.Select(x => x.x)).Select(x => x.Concat(x.ToArray().Reverse())).SelectMany(x => x).ToArray();
            starterMap = starterMap.Concat(starterMap.Reverse().Skip(mapWidth*2)).ToArray();
        } else
        {
            spriteMap = initMap(starterMap, mapWidth*2);
        }
        player.coords = new int[] {1, 1};
        player.transform.position = CoordToPosition(player.coords);
    }
    
    public bool TryMove(int[] coords)
    {
        return TryMove(coords[0], coords[1]);
    }
    public bool TryMove(int x, int y)
    {
        int tile = smartMap[y][x];
        if (tile == 1)
        {
            return false;
        } else if (tile != 0) {
            smartMap[y][x] = 0;
            pelletMap[y][x].SetActive(false);
            if (tile == 5)
            {
                audioPlayer.playEat();
            } else
            {
                audioPlayer.playPEat();
            }
        } else
        {
            audioPlayer.playMove();
        }
        return true;
    }
    public Vector3 CoordToPosition(int[] coords, bool offset = true)
    {
        return CoordToPosition(coords[0], coords[1], offset);
    }
    public Vector3 CoordToPosition(int x, int y, bool offset = true)
    {
        if (offset)
        {
            return new Vector3((x + 0.5f)*scale, -(y + 0.5f)*scale, 0);
        }
        return new Vector3(x*scale, -y*scale, 0);
    }
    public int[] GenerateMap(int width, int height)
    {
        System.Random rand = new System.Random();
        //proud of this one
        int[][] newMap = new int[height][];
        newMap = newMap.Select(x => { int[] row = new int[width]; Array.Fill(row, -1); return row; }).ToArray();
        for (int i = 0; i < height; i ++)
        {
            newMap[i][0] = 1;
            newMap[i][width - 1] = 1;
        }
        for (int i = 0; i < width; i ++)
        {
            newMap[0][i] = 1;
            newMap[height - 1][i] = 1;
        }
        for (int i = 1; i < height-1; i ++)
        {
            newMap[i][1] = 0;
            newMap[i][width - 2] = 0;
        }
        for (int i = 1; i < width-1; i ++)
        {
            newMap[1][i] = 0;
            newMap[height - 2][i] = 0;
        }

        int hBoxes =  height/5 - 1;
        int wBoxes =  width/5 - 1;
        int hgap = height%5;
        int wgap = width%5;
        int square = (hBoxes + 1)*(wBoxes + 1);
        (int, int, int, int)[] boxes = new (int, int, int, int)[square];
        boxes = boxes.Select(x => (1, 1, 1, 1)).ToArray();

        for (int i = 0; i < hBoxes; i++)
        {
            for (int j = 0; j < wBoxes; j++)
            {
                int x1 = 5*j + 3 + rand.Next(-2, 1);
                int x2 = 5*j + 8 + rand.Next(-1, 2);
                int y1 = 5*i + 3 + rand.Next(-2, 1);
                int y2 = 5*i + 8 + rand.Next(-1, 2);
                boxes[i*(wBoxes + 1) + j] = (x1, x2, y1, y2);
            }
        }
        if (hgap != 0)
        {
            for (int j = 0; j < wBoxes; j++)
            {
                int x1 = 5*j + 3 + rand.Next(-2, 1);
                int x2 = 5*j + 7 + rand.Next(-1, 2);
                int y1 = Math.Min(5*hBoxes + 3 + rand.Next(-2, 1), height - 2);
                int y2 = Math.Min(5*hBoxes + 7 + rand.Next(-1, 2), height - 2);
                boxes[hBoxes*(wBoxes + 1) + j] = (x1, x2, y1, y2);
            }
        }
        if (wgap != 0)
        {
            for (int i = 0; i < hBoxes; i++)
            {
                int x1 = Math.Min(5*wBoxes + 3 + rand.Next(-2, 1), width - 2);
                int x2 = Math.Min(5*wBoxes + 7 + rand.Next(-1, 2), width - 2);
                int y1 = 5*i + 3 + rand.Next(-2, 1);
                int y2 = 5*i + 7 + rand.Next(-1, 2);
                boxes[i*(wBoxes + 1) + wBoxes] = (x1, x2, y1, y2);
            }
        }
        if (hgap != 0 && wgap != 0)
        {
            
            int x1 = Math.Min(5*wBoxes + 3 + rand.Next(-2, 1), width - 2);
            int x2 = Math.Min(5*wBoxes + 7 + rand.Next(-1, 2), width - 2);
            int y1 = Math.Min(5*hBoxes + 3 + rand.Next(-2, 1), height - 2);
            int y2 = Math.Min(5*hBoxes + 7 + rand.Next(-1, 2), height - 2);
            boxes[hBoxes*(wBoxes + 1) + wBoxes] = (x1, x2, y1, y2);
        }
        boxes = boxes.OrderBy(x => Guid.NewGuid()).ToArray();
        //ghost room
        boxes = boxes.Prepend((width/2-4, width/2+4, height/2-2, height/2+3)).ToArray();
        foreach (var box in boxes)
        {
            for (int i = box.Item1 - 1; i < box.Item2 + 1; i++)
            { 
                if (newMap[box.Item3 - 1][i] == -1)
                {
                    newMap[box.Item3 - 1][i] = 0;
                }
                if (newMap[box.Item4][i] == -1)
                {
                    newMap[box.Item4][i] = 0;
                }
            }
            for (int i = box.Item3 - 1; i < box.Item4 + 1; i++)
            {
                if (newMap[i][box.Item1 - 1] == -1)
                {
                    newMap[i][box.Item1 - 1] = 0;
                }
                if (newMap[i][box.Item2] == -1)
                {
                    newMap[i][box.Item2] = 0;
                }
            }

            for (int i = box.Item1; i < box.Item2; i++)
            {
                for (int j = box.Item3; j < box.Item4; j++)
                {
                    if (newMap[j][i] == -1)
                    {
                        newMap[j][i] = 1;
                    }
                }
            }
        }
        for (int i = 0; i < newMap.Count(); i++)
        {
            for (int j = 0; j < newMap[i].Count(); j++)
            {
                if (newMap[i][j] == 0|| newMap[i][j] == -1)
                {
                    newMap[i][j] = 5;
                }
            }   
        }
        var ghostBox = boxes[0];
        for (int i = ghostBox.Item1 + 1; i < ghostBox.Item2 - 1; i++)
        {
            for (int j = ghostBox.Item3 + 1; j < ghostBox.Item4 - 1; j++)
            {
                newMap[j][i] = 0;
            }
        }
        newMap[height/2-2][width/2] = 0;
        newMap[height/2-2][width/2-1] = 0;
        return newMap.SelectMany(x => x).ToArray();
    }
    public GameObject[][] initMap(int[] map, int width)
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
        obj.transform.position = CoordToPosition(z, y, false);
        return obj; }).ToArray()).ToArray();

        pelletMap = new GameObject[smartMap.Count()][];
        for (int i = 0; i < smartMap.Count(); i++)
        {
            pelletMap[i] = new GameObject[smartMap[i].Count()];
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
                    obj.transform.position = CoordToPosition(j, i);
                    pelletMap[i][j] = obj;
                }
            }
        }

        return maskMap;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
