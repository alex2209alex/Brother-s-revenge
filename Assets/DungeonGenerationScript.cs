using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using DelaunatorSharp;
using DelaunatorSharp.Unity.Extensions;

public class DungeonGenerationScript : MonoBehaviour
{
    public int numRooms;
    public float mainRoomsPercentage = 0.08f;
    public float extraEdgesPercentage = 0.15f;
    public float aspectRatioWeight = 0f;
    public int meanWidth, stdDevWidth, minWidth, maxWidth;
    public int meanHeight, stdDevHeight, minHeight, maxHeight;

    public float cellSize;
    public Color color = Color.white;
    public Grid Map;
    private GameObject[] rectangles;
    private Rigidbody2D[] rectanglesRigidBodies;
    private bool dungeonBuilt = false;
    List<GameObject> sortedRectangles;

    public int numLines;
    public Vector2[] startPoints;
    public Vector2[] endPoints;
    public Color lineColor = Color.blue;
    public float lineWidth = 1f;
    public string sortingLayerName = "Default";
    public int sortingOrder = 100;
    private LineRenderer[] lineRenderers;
    private Delaunator delaunator;

    public Tilemap tilemapFloor, tilemapFloorWalls, tilemapDecoratives, tilemapWalls;
    public Tile tileFloor01, tileBricksLower01, tileBricksUpper01, tileWallUpper, tileWallUpperLeft, tileWallUpperRight, tileWallLower, tileWallLowerLeft, tileWallLowerRight, tileWallUpperCornerLeft, tileWallUpperCornerRight, tileWallLowerCornerLeft, tileWallLowerCornerRight, tileWallLeft, tileWallRight;


    Sprite CreateRectangleSprite(float width, float height)
    {
        Texture2D texture = new Texture2D((int)width, (int)height);
        Color[] colors = new Color[(int)(width * height)];

        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.white;
        }

        texture.SetPixels(colors);
        texture.Apply();

        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, width, height), Vector2.zero);

        return sprite;
    }

    public List<(int, int)> GetAllEdges(Dictionary<int, HashSet<int>> graph)
    {
        List<(int, int)> edges = new List<(int, int)>();
        foreach (var kvp in graph)
        {
            int source = kvp.Key;
            foreach (int destination in kvp.Value)
            {
                edges.Add((source, destination));
            }
        }
        return edges;
    }

    public int Find(int[] parent, int node)
    {
        if (parent[node] != node)
        {
            parent[node] = Find(parent, parent[node]);
        }
        return parent[node];
    }

    public void Union(int[] parent, int[] rank, int node1, int node2)
    {
        int root1 = Find(parent, node1);
        int root2 = Find(parent, node2);

        if (root1 == root2)
        {
            return;
        }

        if (rank[root1] > rank[root2])
        {
            parent[root2] = root1;
        }
        else if (rank[root2] > rank[root1])
        {
            parent[root1] = root2;
        }
        else
        {
            parent[root2] = root1;
            rank[root1]++;
        }
    }

    int roomDistance(int fromNode, int toNode)
    {
        Vector2 startPoint = new Vector2(sortedRectangles[fromNode].transform.position.x + sortedRectangles[fromNode].GetComponent<SpriteRenderer>().bounds.size.x / 2, sortedRectangles[fromNode].transform.position.y + sortedRectangles[fromNode].GetComponent<SpriteRenderer>().bounds.size.y / 2);
        Vector2 endPoint = new Vector2(sortedRectangles[toNode].transform.position.x + sortedRectangles[toNode].GetComponent<SpriteRenderer>().bounds.size.x / 2, sortedRectangles[toNode].transform.position.y + sortedRectangles[toNode].GetComponent<SpriteRenderer>().bounds.size.y / 2);

        float distance = Vector2.Distance(startPoint, endPoint);
        int distanceAsInt = Mathf.RoundToInt(distance);

        return distanceAsInt;
    }

    public Dictionary<int, HashSet<int>> GetMinimalSpanningTree(Dictionary<int, HashSet<int>> graph)
    {
        Dictionary<int, HashSet<int>> minimalSpanningTree = new Dictionary<int, HashSet<int>>();

        List<(int, int)> edges = GetAllEdges(graph);

        edges.Sort((x, y) => roomDistance(x.Item1, x.Item2) - roomDistance(y.Item1, y.Item2));

        int[] parent = new int[graph.Count];
        int[] rank = new int[graph.Count];
        for (int i = 0; i < parent.Length; i++)
        {
            parent[i] = i;
            rank[i] = 0;
        }

        foreach ((int, int) edge in edges)
        {
            int source = edge.Item1;
            int destination = edge.Item2;

            if (Find(parent, source) != Find(parent, destination))
            {
                if (!minimalSpanningTree.ContainsKey(source))
                {
                    minimalSpanningTree[source] = new HashSet<int>();
                }
                if (!minimalSpanningTree.ContainsKey(destination))
                {
                    minimalSpanningTree[destination] = new HashSet<int>();
                }

                minimalSpanningTree[source].Add(destination);
                minimalSpanningTree[destination].Add(source);

                Union(parent, rank, source, destination);
            }
        }

        return minimalSpanningTree;
    }

    void Start()
    {
        cellSize = Map.cellSize[0] * 100;
        rectangles = new GameObject[numRooms];

        rectanglesRigidBodies = new Rigidbody2D[numRooms];

        sortedRectangles = new List<GameObject>();

        for (int i = 0; i < numRooms; i++)
        {
            int width = Mathf.RoundToInt(Mathf.Clamp(UnityEngine.Random.Range(meanWidth - stdDevWidth, meanWidth + stdDevWidth + 1), minWidth, maxWidth));
            int height = Mathf.RoundToInt(Mathf.Clamp(UnityEngine.Random.Range(meanHeight - stdDevHeight, meanHeight + stdDevHeight + 1), minHeight, maxHeight));

            GameObject rectangle = new GameObject("Rectangle" + i);
            SpriteRenderer spriteRenderer = rectangle.AddComponent<SpriteRenderer>();
            spriteRenderer.color = color;
            spriteRenderer.sprite = CreateRectangleSprite(cellSize * width, cellSize * height);
            rectangle.transform.position = new Vector2(UnityEngine.Random.Range(-25, 25), UnityEngine.Random.Range(-25, 25));
            rectangles[i] = rectangle;

            BoxCollider2D boxCollider = rectangles[i].AddComponent<BoxCollider2D>();
            boxCollider.size = new Vector2(width, height);

            Rigidbody2D rigidbody = rectangle.AddComponent<Rigidbody2D>();
            rectanglesRigidBodies[i] = rigidbody;
            rigidbody.mass = 1.0f;
            rigidbody.gravityScale = 0.0f;
            rigidbody.drag = 2f;
            rigidbody.angularDrag = 100f;
            rectangle.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeRotation;
            Vector2 force = new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f)).normalized;
            rigidbody.AddForce(force * UnityEngine.Random.Range(50f, 100f));

            rectangles[i].AddComponent<RoomScript>();
        }
    }

    public Dictionary<int, HashSet<int>> AddExtraEdges(Dictionary<int, HashSet<int>> graph, Dictionary<int, HashSet<int>> minimalSpanningTree, float extraEdgesPercentage)
    {
        Dictionary<int, HashSet<int>> thirdGraph = new Dictionary<int, HashSet<int>>();

        foreach (KeyValuePair<int, HashSet<int>> kvp in minimalSpanningTree)
        {
            thirdGraph[kvp.Key] = new HashSet<int>(kvp.Value);
        }

        int numAdditionalEdges = Mathf.RoundToInt(graph.Count * extraEdgesPercentage);

        List<(int, int)> edgesToAdd = new List<(int, int)>();
        foreach (KeyValuePair<int, HashSet<int>> kvp in graph)
        {
            int source = kvp.Key;
            foreach (int destination in kvp.Value)
            {
                if (!minimalSpanningTree.ContainsKey(source) || !minimalSpanningTree[source].Contains(destination))
                {
                    if (edgesToAdd.Count < numAdditionalEdges)
                    {
                        edgesToAdd.Add((source, destination));
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        foreach ((int, int) edge in edgesToAdd)
        {
            int source = edge.Item1;
            int destination = edge.Item2;

            if (!thirdGraph.ContainsKey(source))
            {
                thirdGraph[source] = new HashSet<int>();
            }
            if (!thirdGraph.ContainsKey(destination))
            {
                thirdGraph[destination] = new HashSet<int>();
            }

            thirdGraph[source].Add(destination);
            thirdGraph[destination].Add(source);
        }

        return thirdGraph;
    }



    private void Update()
    {
        bool allSleeping = true;
        if (!dungeonBuilt)
        {
            foreach (Rigidbody2D rb in rectanglesRigidBodies)
            {
                if (!rb.IsSleeping())
                {
                    allSleeping = false;
                    break;
                }
            }

            if (allSleeping)
            {
                Debug.Log("All rigidbodies have gone to sleep");
                dungeonBuilt = true;

                foreach (GameObject rectangle in rectangles)
                {
                    Rigidbody2D rb = rectangle.GetComponent<Rigidbody2D>();
                    Vector2 roundedPos = new Vector2(Mathf.Round(rb.position.x), Mathf.Round(rb.position.y));
                    rb.isKinematic = true;
                    rb.position = roundedPos;
                    rb.gameObject.transform.position = roundedPos;
                    sortedRectangles.Add(rb.gameObject);
                }

                sortedRectangles.Sort((r1, r2) =>
                {
                    Rect rect1 = r1.GetComponent<SpriteRenderer>().sprite.textureRect;
                    Rect rect2 = r2.GetComponent<SpriteRenderer>().sprite.textureRect;

                    float weightedArea1 = (rect1.width * rect1.height) + aspectRatioWeight * Mathf.Abs(rect1.width - rect1.height);
                    float weightedArea2 = (rect2.width * rect2.height) + aspectRatioWeight * Mathf.Abs(rect2.width - rect2.height);

                    return weightedArea2.CompareTo(weightedArea1);
                });

                int numRectanglesToChange = Mathf.CeilToInt(sortedRectangles.Count * mainRoomsPercentage);

                List<IPoint> points = new List<IPoint>()
                {

                };

                for (int i = 0; i < numRectanglesToChange; i++)
                {
                    SpriteRenderer spriteRenderer = sortedRectangles[i].GetComponent<SpriteRenderer>();
                    spriteRenderer.color = Color.red;

                    points.Insert(i, new Point(sortedRectangles[i].transform.position.x, sortedRectangles[i].transform.position.y));
                }

                delaunator = new Delaunator(points.ToArray());
                int[] triangles = delaunator.Triangles;

                numLines = triangles.Length * 3;
                lineRenderers = new LineRenderer[numLines];
                startPoints = new Vector2[numLines];
                endPoints = new Vector2[numLines];

                Dictionary<int, HashSet<int>> graph = new Dictionary<int, HashSet<int>>();

                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int v1 = triangles[i];
                    int v2 = triangles[i + 1];
                    int v3 = triangles[i + 2];

                    if (!graph.ContainsKey(v1))
                    {
                        graph[v1] = new HashSet<int>();
                    }
                    if (!graph.ContainsKey(v2))
                    {
                        graph[v2] = new HashSet<int>();
                    }
                    if (!graph.ContainsKey(v3))
                    {
                        graph[v3] = new HashSet<int>();
                    }

                    graph[v1].Add(v2);
                    graph[v1].Add(v3);
                    graph[v2].Add(v1);
                    graph[v2].Add(v3);
                    graph[v3].Add(v1);
                    graph[v3].Add(v2);
                }

                Dictionary<int, HashSet<int>> minimalSpanningTree = GetMinimalSpanningTree(graph);
                Dictionary<int, HashSet<int>> extraEdgesGraph = AddExtraEdges(graph, minimalSpanningTree, extraEdgesPercentage);


                int lineIndex = 0;
                foreach (var nodePair in extraEdgesGraph)
                {
                    int fromNode = nodePair.Key;
                    foreach (int toNode in nodePair.Value)
                    {
                        Vector2 startPoint = new Vector2(sortedRectangles[fromNode].transform.position.x + sortedRectangles[fromNode].GetComponent<SpriteRenderer>().bounds.size.x / 2, sortedRectangles[fromNode].transform.position.y + sortedRectangles[fromNode].GetComponent<SpriteRenderer>().bounds.size.y / 2);
                        Vector2 endPoint = new Vector2(sortedRectangles[toNode].transform.position.x + sortedRectangles[toNode].GetComponent<SpriteRenderer>().bounds.size.x / 2, sortedRectangles[toNode].transform.position.y + sortedRectangles[toNode].GetComponent<SpriteRenderer>().bounds.size.y / 2);

                        startPoints[lineIndex] = startPoint;
                        endPoints[lineIndex] = endPoint;
                        ++lineIndex;
                    }
                }
                for (int i = 0; i < lineIndex; ++i)
                {
                    Debug.Log(startPoints[i]);
                }

                //PAINT TILES
                int roomsToDraw = numRectanglesToChange;
                foreach (GameObject rectangle in sortedRectangles)
                {
                    if (roomsToDraw <= 0) break;
                    --roomsToDraw;

                    Rigidbody2D rb = rectangle.GetComponent<Rigidbody2D>();
                    Vector3Int startPos = tilemapFloor.WorldToCell(rb.position);

                    Vector3 tileSize = tilemapFloor.cellSize;
                    Vector3Int sizeInTiles = new Vector3Int(
                        Mathf.RoundToInt(rb.GetComponent<SpriteRenderer>().bounds.size.x / tileSize.x),
                        Mathf.RoundToInt(rb.GetComponent<SpriteRenderer>().bounds.size.y / tileSize.y),
                        1);

                    //Floor
                    for (int i = 0; i < sizeInTiles.x; ++i)
                    {
                        for (int j = 2; j < sizeInTiles.y - 1; ++j)
                        {
                            Vector3Int tilePos = startPos + new Vector3Int(i, j, 0);
                            tilemapFloor.SetTile(tilePos, tileFloor01);
                        }
                    }

                    //FloorWalls
                    for (int i = 1; i < sizeInTiles.x - 1; ++i)
                    {
                        Vector3Int tilePos = startPos + new Vector3Int(i, sizeInTiles.y - 1, 0);
                        tilemapFloorWalls.SetTile(tilePos, tileBricksUpper01);

                        tilePos = startPos + new Vector3Int(i, sizeInTiles.y, 0);
                        tilemapFloorWalls.SetTile(tilePos, tileWallUpper);
                    }
                    Vector3Int tilePos2 = startPos + new Vector3Int(0, sizeInTiles.y - 1, 0);
                    tilemapFloorWalls.SetTile(tilePos2, tileWallUpperCornerLeft);
                    tilePos2 = startPos + new Vector3Int(0, sizeInTiles.y, 0);
                    tilemapFloorWalls.SetTile(tilePos2, tileWallUpperLeft);
                    tilePos2 = startPos + new Vector3Int(sizeInTiles.x - 1, sizeInTiles.y - 1, 0);
                    tilemapFloorWalls.SetTile(tilePos2, tileWallUpperCornerRight);
                    tilePos2 = startPos + new Vector3Int(sizeInTiles.x - 1, sizeInTiles.y, 0);
                    tilemapFloorWalls.SetTile(tilePos2, tileWallUpperRight);

                    //Walls
                    for (int j = 1; j < sizeInTiles.y; ++j)
                    {
                        Vector3Int tilePos = startPos + new Vector3Int(0, j, 0);
                        tilemapWalls.SetTile(tilePos, tileWallLeft);

                        tilePos = startPos + new Vector3Int(sizeInTiles.x - 1, j, 0);
                        tilemapWalls.SetTile(tilePos, tileWallRight);
                    }
                    for (int i = 1; i < sizeInTiles.x - 1; ++i)
                    {
                        Vector3Int tilePos = startPos + new Vector3Int(i, 1, 0);
                        tilemapFloorWalls.SetTile(tilePos, tileBricksLower01);

                        tilePos = startPos + new Vector3Int(i, 2, 0);
                        tilemapWalls.SetTile(tilePos, tileWallLower);
                    }
                    tilePos2 = startPos + new Vector3Int(0, 1, 0);
                    tilemapWalls.SetTile(tilePos2, tileWallLowerCornerLeft);
                    tilePos2 = startPos + new Vector3Int(0, 2, 0);
                    tilemapWalls.SetTile(tilePos2, tileWallLowerLeft);
                    tilePos2 = startPos + new Vector3Int(sizeInTiles.x - 1, 1, 0);
                    tilemapWalls.SetTile(tilePos2, tileWallLowerCornerRight);
                    tilePos2 = startPos + new Vector3Int(sizeInTiles.x - 1, 2, 0);
                    tilemapWalls.SetTile(tilePos2, tileWallLowerRight);
                }

                //DEACTIVATE ROOM RECTANGLES
                foreach (GameObject rectangle in rectangles)
                {
                    rectangle.SetActive(false);
                }

                //DEBUG LINES
                for (int i = 0; i < lineIndex; i++)
                {
                    GameObject lineObj = new GameObject("Line" + i);
                    lineObj.transform.SetParent(transform);

                    LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();

                    lineRenderer.positionCount = 2;
                    lineRenderer.SetPosition(0, startPoints[i]);
                    lineRenderer.SetPosition(1, endPoints[i]);
                    lineRenderer.startColor = lineColor;
                    lineRenderer.endColor = lineColor;
                    lineRenderer.startWidth = lineWidth;
                    lineRenderer.endWidth = lineWidth;

                    lineRenderer.sortingLayerName = sortingLayerName;
                    lineRenderer.sortingOrder = sortingOrder;

                    lineRenderers[i] = lineRenderer;
                }

            }
        }
    }

}
