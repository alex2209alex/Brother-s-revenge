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
    public float chanceTileFloor01, chanceTileFloor02;
    public int startingRoom;
    public int meanWidth, stdDevWidth, minWidth, maxWidth;
    public int meanHeight, stdDevHeight, minHeight, maxHeight;

    public float cellSize;
    public Color color = Color.white;
    public Grid Map;
    private GameObject[] rectangles;
    private Rigidbody2D[] rectanglesRigidBodies;
    private bool dungeonBuilt = false;
    List<GameObject> sortedRectangles;

    public Player Player;
    public GameObject playerGO;

    public int numLines;
    public Vector2[] startPoints;
    public Vector2[] endPoints;
    public Color lineColor = Color.blue;
    public float lineWidth = .3f;
    public string sortingLayerName = "Default";
    public int sortingOrder = 100;
    private LineRenderer[] lineRenderers;
    private Delaunator delaunator;
    Vector3 tileSize = new Vector3(1,1,1);

    public Tilemap tilemapFloor, tilemapFloorWalls, tilemapDecoratives, tilemapWalls;
    public Tile tileFloor01, tileBricks01, tileWallUpper, tileWallUpperLeft, tileWallUpperRight, tileWallLowerLeft, tileWallLowerRight, tileWallLeft, tileWallRight;
    public Tile tileFloor02, tileFloor03;
    public Tile tileCornerUpperLeft, tileCornerUpperRight, tileCornerLowerLeft, tileCornerLowerRight, tileCorner02LowerLeft, tileCorner02LowerRight;
    public Tile tileBricks02, tileBricks03;

    //Prefabs
    [SerializeField] private GameObject ChestPrefab;
    [SerializeField] private GameObject Enemy01, Enemy02, Enemy03, Enemy04, Enemy05, Enemy06, Enemy07;
    [SerializeField] private GameObject[] Enemies;
    public GameObject camera;

    Dictionary<int, HashSet<int>> graphFinal = new Dictionary<int, HashSet<int>>();

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

    int GetRandomIntExcluding(int min, int max, List<int> exclusions)
    {
        int randomInt = UnityEngine.Random.Range(min, max);
        int k = 0;

        while (exclusions.Contains(randomInt))
        {
            if (k == 1000) return -1;
            randomInt = UnityEngine.Random.Range(min, max);
            k++;
        }

        return randomInt;
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

    TileBase getRandomTileFloor()
    {
        TileBase tileToPaint;
        float randomValue = Random.value;

        if (randomValue < chanceTileFloor01)
        {
            tileToPaint = tileFloor01;
        }
        else if (randomValue < chanceTileFloor02)
        {
            tileToPaint = tileFloor02;
        }
        else
        {
            tileToPaint = tileFloor03;
        }
        return tileToPaint;
    }
    void Start()
    {
        Enemies = new GameObject[] { Enemy01, Enemy02, Enemy03, Enemy04, Enemy05, Enemy06, Enemy07 };

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
            rectangle.transform.position = new Vector2(UnityEngine.Random.Range(-6, 6), UnityEngine.Random.Range(-6, 6));
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

    void DFS(int currentNode, Dictionary<int, HashSet<int>> graph, HashSet<int> visited, HashSet<int> component)
    {
        visited.Add(currentNode);
        component.Add(currentNode);

        foreach (int neighborNode in graph[currentNode])
        {
            if (!visited.Contains(neighborNode))
            {
                DFS(neighborNode, graph, visited, component);
            }
        }
    }
    void generateHallways(Dictionary<int, HashSet<int>> extraEdgesGraph, HashSet<string> nodePairs, bool initialBuild, int buildStep = 0)
    {
        List<HashSet<int>> connectedComponents = new List<HashSet<int>>();
        if (!initialBuild)
        {
            HashSet<int> visitedNodes = new HashSet<int>();

            foreach (var nodePair2 in graphFinal)
            {
                int fromNode2 = nodePair2.Key;

                if (visitedNodes.Contains(fromNode2))
                    continue;

                HashSet<int> currentComponent = new HashSet<int>();
                DFS(fromNode2, graphFinal, visitedNodes, currentComponent);
                connectedComponents.Add(currentComponent);
            }

            connectedComponents.Sort((a, b) => b.Count.CompareTo(a.Count));
            foreach (HashSet<int> component in connectedComponents)
            {
                string componentString = "Connected Component: ";
                foreach (int node in component)
                {
                    componentString += node.ToString() + " ";
                }
                Debug.Log(componentString);
            }
            if (buildStep == connectedComponents.Count) return;
        }

        foreach (var nodePair in extraEdgesGraph)
        {
            int fromNode = nodePair.Key;
            foreach (int toNode in (initialBuild ? nodePair.Value: connectedComponents[buildStep]))
            {
                Debug.Log("Try: " + fromNode + " " + toNode);
                if (nodePairs.Contains($"{toNode},{fromNode}") || nodePairs.Contains($"{fromNode},{toNode}"))
                {
                    continue;
                }

                Rigidbody2D rb1 = sortedRectangles[fromNode].GetComponent<Rigidbody2D>();
                Vector3Int startPos1 = tilemapFloor.WorldToCell(rb1.position);
                Rigidbody2D rb2 = sortedRectangles[toNode].GetComponent<Rigidbody2D>();
                Vector3Int startPos2 = tilemapFloor.WorldToCell(rb2.position);

                Vector3Int sizeInTiles1 = new Vector3Int(
                    Mathf.RoundToInt(rb1.GetComponent<SpriteRenderer>().bounds.size.x / tileSize.x),
                    Mathf.RoundToInt(rb1.GetComponent<SpriteRenderer>().bounds.size.y / tileSize.y),
                    1);
                Vector3Int sizeInTiles2 = new Vector3Int(
                    Mathf.RoundToInt(rb2.GetComponent<SpriteRenderer>().bounds.size.x / tileSize.x),
                    Mathf.RoundToInt(rb2.GetComponent<SpriteRenderer>().bounds.size.y / tileSize.y),
                    1);

                bool builtL = false;
                bool builtN = false;

                if (startPos1.x < startPos2.x)
                {
                    bool case_orientation = true;
                    if (startPos1.y > startPos2.y)
                    {
                        case_orientation = false;
                    }

                    if ((case_orientation && startPos1.y <= startPos2.y && startPos2.y < startPos1.y + sizeInTiles1.y - 6) || (!case_orientation && startPos1.y > startPos2.y && startPos1.y < startPos2.y + sizeInTiles2.y - 6))
                    {
                        Vector3Int startPosOffset = case_orientation ? startPos2 : startPos1;
                        int startPosOffsetExtra = case_orientation ? 0 : startPos2.x - startPos1.x;

                        bool intersectionN = false;
                        for (int i = 0; i <= startPos2.x - startPos1.x - sizeInTiles1.x + 1; ++i)
                        {
                            int offset = case_orientation ? i * -1 : i + sizeInTiles1.x - 1;

                            if (tilemapFloor.GetTile(startPosOffset + new Vector3Int(offset, 3, 0)) != null)
                            {
                                intersectionN = true;
                                break;
                            }
                            if (tilemapFloor.GetTile(startPosOffset + new Vector3Int(offset, 4, 0)) != null)
                            {
                                intersectionN = true;
                                break;
                            }

                            if (i == 0 || i == startPos2.x - startPos1.x - sizeInTiles1.x + 1) continue;
                            if (tilemapWalls.GetTile(startPosOffset + new Vector3Int(offset, 2, 0)) != null)
                            {
                                intersectionN = true;
                                break;
                            }
                            if (tilemapFloorWalls.GetTile(startPosOffset + new Vector3Int(offset, 2, 0)) != null)
                            {
                                intersectionN = true;
                                break;
                            }
                            if (tilemapWalls.GetTile(startPosOffset + new Vector3Int(offset, 3, 0)) != null)
                            {
                                intersectionN = true;
                                break;
                            }
                            if (tilemapFloorWalls.GetTile(startPosOffset + new Vector3Int(offset, 3, 0)) != null)
                            {
                                intersectionN = true;
                                break;
                            }
                            if (tilemapWalls.GetTile(startPosOffset + new Vector3Int(offset, 5, 0)) != null)
                            {
                                intersectionN = true;
                                break;
                            }
                            if (tilemapFloorWalls.GetTile(startPosOffset + new Vector3Int(offset, 5, 0)) != null)
                            {
                                intersectionN = true;
                                break;
                            }
                            if (tilemapWalls.GetTile(startPosOffset + new Vector3Int(offset, 6, 0)) != null)
                            {
                                intersectionN = true;
                                break;
                            }
                            if (tilemapFloorWalls.GetTile(startPosOffset + new Vector3Int(offset, 6, 0)) != null)
                            {
                                intersectionN = true;
                                break;
                            }
                        }

                        if (!intersectionN)
                        {
                            for (int i = 0; i <= startPos2.x - startPos1.x - sizeInTiles1.x + 1; ++i)
                            {
                                int offset = case_orientation ? i * -1 : i + sizeInTiles1.x - 1;

                                Vector3Int tilePos = startPosOffset + new Vector3Int(offset, 3, 0);
                                TileBase tileToPaint = getRandomTileFloor();
                                tilemapFloor.SetTile(tilePos, tileToPaint);

                                tilePos = startPosOffset + new Vector3Int(offset, 4, 0);
                                tileToPaint = getRandomTileFloor();
                                tilemapFloor.SetTile(tilePos, tileToPaint);

                                tilePos = startPosOffset + new Vector3Int(offset, 3, 0);
                                tileToPaint = tileWallUpper;
                                tilemapWalls.SetTile(tilePos, tileToPaint);

                                tilePos = startPosOffset + new Vector3Int(offset, 6, 0);
                                tileToPaint = tileWallUpper;
                                tilemapWalls.SetTile(tilePos, tileToPaint);

                                tilePos = startPosOffset + new Vector3Int(offset, 5, 0);
                                tileToPaint = tileBricks01;
                                tilemapFloorWalls.SetTile(tilePos, tileToPaint);

                                tilePos = startPosOffset + new Vector3Int(offset, 2, 0);
                                tileToPaint = tileBricks01;
                                tilemapWalls.SetTile(tilePos, tileToPaint);
                            }

                            Vector3Int tilePos2 = startPosOffset + new Vector3Int(0, 6, 0) + new Vector3Int(startPosOffsetExtra, 0, 0);
                            TileBase tileToPaint2 = tileCornerUpperLeft;
                            tilemapWalls.SetTile(tilePos2, tileToPaint2);

                            tilePos2 = startPosOffset + new Vector3Int(startPos1.x - startPos2.x + sizeInTiles1.x - 1, 6, 0) + new Vector3Int(startPosOffsetExtra, 0, 0);
                            tileToPaint2 = tileCornerUpperRight;
                            tilemapWalls.SetTile(tilePos2, tileToPaint2);

                            tilePos2 = startPosOffset + new Vector3Int(0, 5, 0) + new Vector3Int(startPosOffsetExtra, 0, 0);
                            tilemapWalls.SetTile(tilePos2, null);
                            tilePos2 = startPosOffset + new Vector3Int(0, 4, 0) + new Vector3Int(startPosOffsetExtra, 0, 0);
                            tilemapWalls.SetTile(tilePos2, null);

                            tilePos2 = startPosOffset + new Vector3Int(startPos1.x - startPos2.x + sizeInTiles1.x - 1, 5, 0) + new Vector3Int(startPosOffsetExtra, 0, 0);
                            tilemapWalls.SetTile(tilePos2, null);
                            tilePos2 = startPosOffset + new Vector3Int(startPos1.x - startPos2.x + sizeInTiles1.x - 1, 4, 0) + new Vector3Int(startPosOffsetExtra, 0, 0);
                            tilemapWalls.SetTile(tilePos2, null);

                            tilePos2 = startPosOffset + new Vector3Int(0, 5, 0) + new Vector3Int(startPosOffsetExtra, 0, 0);
                            tileToPaint2 = tileBricks03;
                            tilemapFloorWalls.SetTile(tilePos2, tileToPaint2);

                            tilePos2 = startPosOffset + new Vector3Int(startPos1.x - startPos2.x + sizeInTiles1.x - 1, 5, 0) + new Vector3Int(startPosOffsetExtra, 0, 0);
                            tileToPaint2 = tileBricks02;
                            tilemapFloorWalls.SetTile(tilePos2, tileToPaint2);

                            tilePos2 = startPosOffset + new Vector3Int(0, 2, 0) + new Vector3Int(startPosOffsetExtra, 0, 0);
                            tileToPaint2 = tileCornerLowerRight;
                            tilemapWalls.SetTile(tilePos2, tileToPaint2);

                            tilePos2 = startPosOffset + new Vector3Int(startPos1.x - startPos2.x + sizeInTiles1.x - 1, 2, 0) + new Vector3Int(startPosOffsetExtra, 0, 0);
                            tileToPaint2 = tileCornerLowerLeft;
                            tilemapWalls.SetTile(tilePos2, tileToPaint2);

                            tilePos2 = startPosOffset + new Vector3Int(0, 3, 0) + new Vector3Int(startPosOffsetExtra, 0, 0);
                            tileToPaint2 = tileCorner02LowerRight;
                            tilemapWalls.SetTile(tilePos2, tileToPaint2);

                            tilePos2 = startPosOffset + new Vector3Int(startPos1.x - startPos2.x + sizeInTiles1.x - 1, 3, 0) + new Vector3Int(startPosOffsetExtra, 0, 0);
                            tileToPaint2 = tileCorner02LowerLeft;
                            tilemapWalls.SetTile(tilePos2, tileToPaint2);

                            builtN = true;
                            nodePairs.Add($"{fromNode},{toNode}");
                            if (!graphFinal.ContainsKey(fromNode))
                            {
                                graphFinal[fromNode] = new HashSet<int>();
                            }
                            if (!graphFinal.ContainsKey(toNode))
                            {
                                graphFinal[toNode] = new HashSet<int>();
                            }
                            graphFinal[fromNode].Add(toNode);
                            graphFinal[toNode].Add(fromNode);

                            if (!initialBuild)
                            {
                                Debug.Log("Changed graph.");
                                generateHallways(extraEdgesGraph, nodePairs, false, 1);
                                return;
                            }
                            continue;
                        }
                    }

                    if (builtN) continue;
                    if (case_orientation && startPos1.y <= startPos2.y)
                    {
                        float randomNumber = Random.value;
                        for (int j = 0; j < 2; ++j)
                        {
                            if (randomNumber > 0.5f)
                            {
                                for (int startL = startPos2.y + 4; startL < startPos2.y + sizeInTiles2.y - 3; ++startL)
                                {
                                    for (int endL = startPos1.x + sizeInTiles1.x - 4; endL > startPos1.x + 1; --endL)
                                    {
                                        bool intersectionL = false;
                                        for (int i = startPos2.x; i >= endL; --i)
                                        {
                                            if (tilemapFloor.GetTile(new Vector3Int(i, startL, 0)) != null)
                                            {
                                                intersectionL = true;
                                                break;
                                            }
                                            if (tilemapFloor.GetTile(new Vector3Int(i, startL - 1, 0)) != null)
                                            {
                                                intersectionL = true;
                                                break;
                                            }

                                            if (i == startPos2.x) continue;

                                            if (tilemapWalls.GetTile(new Vector3Int(i, startL + 2, 0)) != null)
                                            {
                                                intersectionL = true;
                                                break;
                                            }
                                            if (tilemapFloorWalls.GetTile(new Vector3Int(i, startL + 2, 0)) != null)
                                            {
                                                intersectionL = true;
                                                break;
                                            }
                                            if (tilemapWalls.GetTile(new Vector3Int(i, startL - 1, 0)) != null)
                                            {
                                                intersectionL = true;
                                                break;
                                            }
                                            if (tilemapFloorWalls.GetTile(new Vector3Int(i, startL - 1, 0)) != null)
                                            {
                                                intersectionL = true;
                                                break;
                                            }
                                            if (tilemapWalls.GetTile(new Vector3Int(i, startL + 1, 0)) != null)
                                            {
                                                intersectionL = true;
                                                break;
                                            }
                                            if (tilemapFloorWalls.GetTile(new Vector3Int(i, startL + 1, 0)) != null)
                                            {
                                                intersectionL = true;
                                                break;
                                            }
                                            if (tilemapWalls.GetTile(new Vector3Int(i, startL - 2, 0)) != null)
                                            {
                                                intersectionL = true;
                                                break;
                                            }
                                            if (tilemapFloorWalls.GetTile(new Vector3Int(i, startL - 2, 0)) != null)
                                            {
                                                intersectionL = true;
                                                break;
                                            }
                                        }

                                        if (intersectionL) continue;
                                        for (int i = startPos1.y + sizeInTiles1.y - 1; i <= startL; ++i)
                                        {
                                            if (tilemapFloor.GetTile(new Vector3Int(endL, i, 0)) != null)
                                            {
                                                intersectionL = true;
                                                break;
                                            }
                                            if (tilemapFloor.GetTile(new Vector3Int(endL + 1, i, 0)) != null)
                                            {
                                                intersectionL = true;
                                                break;
                                            }

                                            if (i == startPos1.y + sizeInTiles1.y || i == startPos1.y + sizeInTiles1.y - 1) continue;

                                            if (tilemapWalls.GetTile(new Vector3Int(endL - 1, i, 0)) != null)
                                            {
                                                intersectionL = true;
                                                break;
                                            }
                                            if (tilemapFloorWalls.GetTile(new Vector3Int(endL - 1, i, 0)) != null)
                                            {
                                                intersectionL = true;
                                                break;
                                            }
                                            if (tilemapWalls.GetTile(new Vector3Int(endL + 2, i, 0)) != null)
                                            {
                                                intersectionL = true;
                                                break;
                                            }
                                            if (tilemapFloorWalls.GetTile(new Vector3Int(endL + 2, i, 0)) != null)
                                            {
                                                intersectionL = true;
                                                break;
                                            }
                                        }
                                        if (intersectionL) continue;

                                        for (int i = startPos2.x; i >= endL; --i)
                                        {
                                            Vector3Int tilePos = new Vector3Int(i, startL, 0);
                                            TileBase tileToPaint = getRandomTileFloor();
                                            tilemapFloor.SetTile(tilePos, tileToPaint);

                                            tilePos = new Vector3Int(i, startL - 1, 0);
                                            tileToPaint = getRandomTileFloor();
                                            tilemapFloor.SetTile(tilePos, tileToPaint);

                                            tilePos = new Vector3Int(i, startL + 2, 0);
                                            tileToPaint = tileWallUpper;
                                            tilemapWalls.SetTile(tilePos, tileToPaint);

                                            tilePos = new Vector3Int(i, startL + 1, 0);
                                            tileToPaint = tileBricks01;
                                            tilemapFloorWalls.SetTile(tilePos, tileToPaint);

                                            if (i == endL + 1 || i == endL) continue;
                                            tilePos = new Vector3Int(i, startL - 1, 0);
                                            tileToPaint = tileWallUpper;
                                            tilemapWalls.SetTile(tilePos, tileToPaint);

                                            tilePos = new Vector3Int(i, startL - 2, 0);
                                            tileToPaint = tileBricks01;
                                            tilemapWalls.SetTile(tilePos, tileToPaint);
                                        }

                                        for (int i = startPos1.y + sizeInTiles1.y - 1; i <= startL; ++i)
                                        {
                                            Vector3Int tilePos = new Vector3Int(endL, i, 0);
                                            TileBase tileToPaint = getRandomTileFloor();
                                            tilemapFloor.SetTile(tilePos, tileToPaint);

                                            tilePos = new Vector3Int(endL + 1, i, 0);
                                            tileToPaint = getRandomTileFloor();
                                            tilemapFloor.SetTile(tilePos, tileToPaint);

                                            if (i != startPos1.y + sizeInTiles1.y - 1)
                                            {
                                                tilePos = new Vector3Int(endL - 1, i, 0);
                                                tileToPaint = tileWallLeft;
                                                tilemapWalls.SetTile(tilePos, tileToPaint);

                                                if (i == startL - 1 || i == startL || i == startL + 1) continue;
                                                tilePos = new Vector3Int(endL + 2, i, 0);
                                                tileToPaint = tileWallRight;
                                                tilemapWalls.SetTile(tilePos, tileToPaint);
                                            }
                                        }

                                        Vector3Int tilePos2 = new Vector3Int(endL - 1, startL + 2, 0);
                                        TileBase tileToPaint2 = tileWallUpperLeft;
                                        tilemapWalls.SetTile(tilePos2, tileToPaint2);

                                        tilePos2 = new Vector3Int(endL - 1, startL + 1, 0);
                                        tileToPaint2 = tileWallLeft;
                                        tilemapWalls.SetTile(tilePos2, tileToPaint2);

                                        tilePos2 = new Vector3Int(endL + 2, startPos1.y + sizeInTiles1.y, 0);
                                        tileToPaint2 = tileCornerUpperRight;
                                        tilemapWalls.SetTile(tilePos2, tileToPaint2);

                                        tilePos2 = new Vector3Int(endL + 2, startPos1.y + sizeInTiles1.y - 1, 0);
                                        tileToPaint2 = tileBricks02;
                                        tilemapFloorWalls.SetTile(tilePos2, tileToPaint2);

                                        tilePos2 = new Vector3Int(endL - 1, startPos1.y + sizeInTiles1.y, 0);
                                        tileToPaint2 = tileCornerUpperLeft;
                                        tilemapWalls.SetTile(tilePos2, tileToPaint2);

                                        tilePos2 = new Vector3Int(endL - 1, startPos1.y + sizeInTiles1.y - 1, 0);
                                        tileToPaint2 = tileBricks03;
                                        tilemapFloorWalls.SetTile(tilePos2, tileToPaint2);

                                        tilePos2 = new Vector3Int(endL + 2, startL - 1, 0);
                                        tileToPaint2 = tileCorner02LowerLeft;
                                        tilemapWalls.SetTile(tilePos2, tileToPaint2);

                                        tilePos2 = new Vector3Int(endL + 2, startL - 2, 0);
                                        tileToPaint2 = tileCornerLowerLeft;
                                        tilemapWalls.SetTile(tilePos2, tileToPaint2);

                                        tilePos2 = new Vector3Int(startPos2.x, startL - 1, 0);
                                        tileToPaint2 = tileCorner02LowerRight;
                                        tilemapWalls.SetTile(tilePos2, tileToPaint2);

                                        tilePos2 = new Vector3Int(startPos2.x, startL - 2, 0);
                                        tileToPaint2 = tileCornerLowerRight;
                                        tilemapWalls.SetTile(tilePos2, tileToPaint2);

                                        tilePos2 = new Vector3Int(startPos2.x, startL + 1, 0);
                                        tileToPaint2 = tileBricks03;
                                        tilemapFloorWalls.SetTile(tilePos2, tileToPaint2);

                                        tilePos2 = new Vector3Int(startPos2.x, startL + 2, 0);
                                        tileToPaint2 = tileCornerUpperLeft;
                                        tilemapWalls.SetTile(tilePos2, tileToPaint2);

                                        tilePos2 = new Vector3Int(endL, startPos1.y + sizeInTiles1.y, 0);
                                        tilemapWalls.SetTile(tilePos2, null);

                                        tilePos2 = new Vector3Int(endL + 1, startPos1.y + sizeInTiles1.y, 0);
                                        tilemapWalls.SetTile(tilePos2, null);

                                        tilePos2 = new Vector3Int(endL, startPos1.y + sizeInTiles1.y - 1, 0);
                                        tilemapFloorWalls.SetTile(tilePos2, null);

                                        tilePos2 = new Vector3Int(endL + 1, startPos1.y + sizeInTiles1.y - 1, 0);
                                        tilemapFloorWalls.SetTile(tilePos2, null);

                                        tilePos2 = new Vector3Int(startPos2.x, startL, 0);
                                        tilemapWalls.SetTile(tilePos2, null);

                                        tilePos2 = new Vector3Int(startPos2.x, startL + 1, 0);
                                        tilemapWalls.SetTile(tilePos2, null);

                                        nodePairs.Add($"{fromNode},{toNode}");
                                        if (!graphFinal.ContainsKey(fromNode))
                                        {
                                            graphFinal[fromNode] = new HashSet<int>();
                                        }
                                        if (!graphFinal.ContainsKey(toNode))
                                        {
                                            graphFinal[toNode] = new HashSet<int>();
                                        }
                                        graphFinal[fromNode].Add(toNode);
                                        graphFinal[toNode].Add(fromNode);
                                        Debug.Log("TWO");
                                        builtL = true;
                                        if (!initialBuild)
                                        {
                                            Debug.Log("Changed graph.");
                                            generateHallways(extraEdgesGraph, nodePairs, false, 1);
                                            return;
                                        }
                                        break;
                                    }
                                    if (builtL) break;
                                }
                            }
                            else
                            {
                                if (builtL) break;
                                for (int startL = startPos1.y + sizeInTiles1.y - 3; startL > startPos1.y + 5; --startL)
                                {
                                    for (int endL = startPos2.x + 2; endL < startPos2.x + sizeInTiles2.x - 3; ++endL)
                                    {
                                        bool intersectionL = false;
                                        for (int i = startPos1.x + sizeInTiles1.x - 1; i <= endL + 2; ++i)
                                        {
                                            if (tilemapFloor.GetTile(new Vector3Int(i, startL, 0)) != null)
                                            {
                                                intersectionL = true;
                                                break;
                                            }
                                            if (tilemapFloor.GetTile(new Vector3Int(i, startL - 1, 0)) != null)
                                            {
                                                intersectionL = true;
                                                break;
                                            }

                                            if (i == startPos1.x + sizeInTiles1.x - 1 || i == startPos1.x + sizeInTiles1.x - 2) continue;

                                            if (tilemapWalls.GetTile(new Vector3Int(i, startL + 2, 0)) != null)
                                            {
                                                intersectionL = true;
                                                break;
                                            }
                                            if (tilemapFloorWalls.GetTile(new Vector3Int(i, startL + 2, 0)) != null)
                                            {
                                                intersectionL = true;
                                                break;
                                            }
                                            if (tilemapWalls.GetTile(new Vector3Int(i, startL - 1, 0)) != null)
                                            {
                                                intersectionL = true;
                                                break;
                                            }
                                            if (tilemapFloorWalls.GetTile(new Vector3Int(i, startL - 1, 0)) != null)
                                            {
                                                intersectionL = true;
                                                break;
                                            }
                                            if (tilemapWalls.GetTile(new Vector3Int(i, startL + 1, 0)) != null)
                                            {
                                                intersectionL = true;
                                                break;
                                            }
                                            if (tilemapFloorWalls.GetTile(new Vector3Int(i, startL + 1, 0)) != null)
                                            {
                                                intersectionL = true;
                                                break;
                                            }
                                            if (tilemapWalls.GetTile(new Vector3Int(i, startL - 2, 0)) != null)
                                            {
                                                intersectionL = true;
                                                break;
                                            }
                                            if (tilemapFloorWalls.GetTile(new Vector3Int(i, startL - 2, 0)) != null)
                                            {
                                                intersectionL = true;
                                                break;
                                            }
                                        }
                                        if (intersectionL) continue;

                                        for (int i = startPos2.y; i >= startL - 1; --i)
                                        {
                                            if (tilemapFloor.GetTile(new Vector3Int(endL, i, 0)) != null)
                                            {
                                                intersectionL = true;
                                                break;
                                            }
                                            if (tilemapFloor.GetTile(new Vector3Int(endL + 1, i, 0)) != null)
                                            {
                                                intersectionL = true;
                                                break;
                                            }

                                            if (i == startPos2.y) continue;

                                            if (tilemapWalls.GetTile(new Vector3Int(endL - 1, i, 0)) != null)
                                            {
                                                intersectionL = true;
                                                break;
                                            }
                                            if (tilemapFloorWalls.GetTile(new Vector3Int(endL - 1, i, 0)) != null)
                                            {
                                                intersectionL = true;
                                                break;
                                            }
                                            if (tilemapWalls.GetTile(new Vector3Int(endL + 2, i, 0)) != null)
                                            {
                                                intersectionL = true;
                                                break;
                                            }
                                            if (tilemapFloorWalls.GetTile(new Vector3Int(endL + 2, i, 0)) != null)
                                            {
                                                intersectionL = true;
                                                break;
                                            }
                                        }
                                        if (intersectionL) continue;

                                        for (int i = startPos1.x + sizeInTiles1.x - 1; i <= endL + 1; ++i)
                                        {
                                            Vector3Int tilePos = new Vector3Int(i, startL, 0);
                                            TileBase tileToPaint = getRandomTileFloor();
                                            tilemapFloor.SetTile(tilePos, tileToPaint);

                                            tilePos = new Vector3Int(i, startL - 1, 0);
                                            tileToPaint = getRandomTileFloor();
                                            tilemapFloor.SetTile(tilePos, tileToPaint);

                                            tilePos = new Vector3Int(i, startL - 1, 0);
                                            tileToPaint = tileWallUpper;
                                            tilemapWalls.SetTile(tilePos, tileToPaint);

                                            tilePos = new Vector3Int(i, startL - 2, 0);
                                            tileToPaint = tileBricks01;
                                            tilemapWalls.SetTile(tilePos, tileToPaint);

                                            if (i == endL + 1 || i == endL) continue;
                                            tilePos = new Vector3Int(i, startL + 2, 0);
                                            tileToPaint = tileWallUpper;
                                            tilemapWalls.SetTile(tilePos, tileToPaint);

                                            tilePos = new Vector3Int(i, startL + 1, 0);
                                            tileToPaint = tileBricks01;
                                            tilemapFloorWalls.SetTile(tilePos, tileToPaint);
                                        }

                                        for (int i = startPos2.y + 1; i >= startL - 1; --i)
                                        {
                                            Vector3Int tilePos = new Vector3Int(endL, i, 0);
                                            TileBase tileToPaint = getRandomTileFloor();
                                            tilemapFloor.SetTile(tilePos, tileToPaint);

                                            tilePos = new Vector3Int(endL + 1, i, 0);
                                            tileToPaint = getRandomTileFloor();
                                            tilemapFloor.SetTile(tilePos, tileToPaint);

                                            tilePos = new Vector3Int(endL + 2, i, 0);
                                            tileToPaint = tileWallRight;
                                            tilemapWalls.SetTile(tilePos, tileToPaint);

                                            if (i == startL - 1 || i == startL || i == startL + 1) continue;
                                            tilePos = new Vector3Int(endL - 1, i, 0);
                                            tileToPaint = tileWallLeft;
                                            tilemapWalls.SetTile(tilePos, tileToPaint);
                                        }

                                        Vector3Int tilePos2 = new Vector3Int(endL - 1, startL + 2, 0);
                                        TileBase tileToPaint2 = tileCornerUpperLeft;
                                        tilemapWalls.SetTile(tilePos2, tileToPaint2);

                                        tilePos2 = new Vector3Int(endL - 1, startL + 1, 0);
                                        tileToPaint2 = tileBricks03;
                                        tilemapFloorWalls.SetTile(tilePos2, tileToPaint2);

                                        tilePos2 = new Vector3Int(startPos1.x + sizeInTiles1.x - 1, startL - 1, 0);
                                        tileToPaint2 = tileCorner02LowerLeft;
                                        tilemapWalls.SetTile(tilePos2, tileToPaint2);

                                        tilePos2 = new Vector3Int(startPos1.x + sizeInTiles1.x - 1, startL + 2, 0);
                                        tileToPaint2 = tileCornerUpperRight;
                                        tilemapWalls.SetTile(tilePos2, tileToPaint2);

                                        tilePos2 = new Vector3Int(startPos1.x + sizeInTiles1.x - 1, startL + 1, 0);
                                        tilemapWalls.SetTile(tilePos2, null);

                                        tilePos2 = new Vector3Int(startPos1.x + sizeInTiles1.x - 1, startL, 0);
                                        tilemapWalls.SetTile(tilePos2, null);

                                        tilePos2 = new Vector3Int(startPos1.x + sizeInTiles1.x - 1, startL + 1, 0);
                                        tileToPaint2 = tileBricks02;
                                        tilemapFloorWalls.SetTile(tilePos2, tileToPaint2);

                                        tilePos2 = new Vector3Int(startPos1.x + sizeInTiles1.x - 1, startL - 2, 0);
                                        tileToPaint2 = tileCornerLowerLeft;
                                        tilemapWalls.SetTile(tilePos2, tileToPaint2);

                                        tilePos2 = new Vector3Int(endL + 2, startL - 2, 0);
                                        tileToPaint2 = tileWallLowerRight;
                                        tilemapWalls.SetTile(tilePos2, tileToPaint2);

                                        tilePos2 = new Vector3Int(endL - 1, startPos2.y + 2, 0);
                                        tileToPaint2 = tileCorner02LowerRight;
                                        tilemapWalls.SetTile(tilePos2, tileToPaint2);

                                        tilePos2 = new Vector3Int(endL - 1, startPos2.y + 1, 0);
                                        tileToPaint2 = tileCornerLowerRight;
                                        tilemapWalls.SetTile(tilePos2, tileToPaint2);

                                        tilePos2 = new Vector3Int(endL + 2, startPos2.y + 2, 0);
                                        tileToPaint2 = tileCorner02LowerLeft;
                                        tilemapWalls.SetTile(tilePos2, tileToPaint2);

                                        tilePos2 = new Vector3Int(endL + 2, startPos2.y + 1, 0);
                                        tileToPaint2 = tileCornerLowerLeft;
                                        tilemapWalls.SetTile(tilePos2, tileToPaint2);

                                        tilePos2 = new Vector3Int(endL, startPos2.y + 2, 0);
                                        tilemapWalls.SetTile(tilePos2, null);

                                        tilePos2 = new Vector3Int(endL, startPos2.y + 1, 0);
                                        tilemapWalls.SetTile(tilePos2, null);

                                        tilePos2 = new Vector3Int(endL + 1, startPos2.y + 2, 0);
                                        tilemapWalls.SetTile(tilePos2, null);

                                        tilePos2 = new Vector3Int(endL + 1, startPos2.y + 1, 0);
                                        tilemapWalls.SetTile(tilePos2, null);

                                        nodePairs.Add($"{fromNode},{toNode}");
                                        if (!graphFinal.ContainsKey(fromNode))
                                        {
                                            graphFinal[fromNode] = new HashSet<int>();
                                        }
                                        if (!graphFinal.ContainsKey(toNode))
                                        {
                                            graphFinal[toNode] = new HashSet<int>();
                                        }
                                        graphFinal[fromNode].Add(toNode);
                                        graphFinal[toNode].Add(fromNode);
                                        Debug.Log("ONE");
                                        builtL = true;
                                        if (!initialBuild)
                                        {
                                            Debug.Log("Changed graph.");
                                            generateHallways(extraEdgesGraph, nodePairs, false, 1);
                                            return;
                                        }
                                        break;
                                    }
                                    if (builtL) break;
                                }
                                if (builtL) break;
                            }
                            randomNumber = 1 - randomNumber;
                        }
                    }

                    if (builtL) continue;
                    if (builtN) continue;
                }

                if (builtL) continue;
                if (builtN) continue;
                if (startPos1.y < startPos2.y)
                {
                    bool case_orientation = true;
                    if (startPos1.x > startPos2.x)
                    {
                        case_orientation = false;
                    }

                    if ((case_orientation && startPos1.x <= startPos2.x && startPos2.x < startPos1.x + sizeInTiles1.x - 5) || (!case_orientation && startPos1.x > startPos2.x && startPos1.x < startPos2.x + sizeInTiles2.x - 5))
                    {
                        Vector3Int startPosOffset = case_orientation ? startPos2 : startPos1;
                        int startPosOffsetExtra = case_orientation ? 0 : startPos2.y - startPos1.y;

                        bool intersectionN = false;
                        for (int i = -1; i <= startPos2.y - startPos1.y - sizeInTiles1.y + 1; ++i)
                        {
                            int offset = case_orientation ? i * -1 : i + sizeInTiles1.y;

                            if (tilemapFloor.GetTile(startPosOffset + new Vector3Int(2, offset, 0)) != null)
                            {
                                intersectionN = true;
                                break;
                            }
                            if (tilemapFloor.GetTile(startPosOffset + new Vector3Int(3, offset, 0)) != null)
                            {
                                intersectionN = true;
                                break;
                            }

                            if (i == -1 || i == 0 || i == startPos2.y - startPos1.y - sizeInTiles1.y + 1 || i == startPos2.y - startPos1.y - sizeInTiles1.y) continue;
                            if (tilemapWalls.GetTile(startPosOffset + new Vector3Int(1, offset, 0)) != null)
                            {
                                intersectionN = true;
                                break;
                            }
                            if (tilemapFloorWalls.GetTile(startPosOffset + new Vector3Int(1, offset, 0)) != null)
                            {
                                intersectionN = true;
                                break;
                            }
                            if (tilemapWalls.GetTile(startPosOffset + new Vector3Int(4, offset, 0)) != null)
                            {
                                intersectionN = true;
                                break;
                            }
                            if (tilemapFloorWalls.GetTile(startPosOffset + new Vector3Int(4, offset, 0)) != null)
                            {
                                intersectionN = true;
                                break;
                            }
                        }

                        if (!intersectionN)
                        {
                            for (int i = -1; i <= startPos2.y - startPos1.y - sizeInTiles1.y + 1; ++i)
                            {
                                int offset = case_orientation ? i * -1 : i + sizeInTiles1.y;

                                Vector3Int tilePos = startPosOffset + new Vector3Int(2, offset, 0);
                                TileBase tileToPaint = getRandomTileFloor();
                                tilemapFloor.SetTile(tilePos, tileToPaint);

                                tilePos = startPosOffset + new Vector3Int(3, offset, 0);
                                tileToPaint = getRandomTileFloor();
                                tilemapFloor.SetTile(tilePos, tileToPaint);

                                tilePos = startPosOffset + new Vector3Int(1, offset, 0);
                                tileToPaint = tileWallLeft;
                                tilemapWalls.SetTile(tilePos, tileToPaint);

                                tilePos = startPosOffset + new Vector3Int(4, offset, 0);
                                tileToPaint = tileWallRight;
                                tilemapWalls.SetTile(tilePos, tileToPaint);
                            }

                            Vector3Int tilePos2 = startPosOffset + new Vector3Int(1, 1, 0) + new Vector3Int(0, startPosOffsetExtra, 0);
                            TileBase tileToPaint2 = tileCornerLowerRight;
                            tilemapWalls.SetTile(tilePos2, tileToPaint2);

                            tilePos2 = startPosOffset + new Vector3Int(4, 1, 0) + new Vector3Int(0, startPosOffsetExtra, 0);
                            tileToPaint2 = tileCornerLowerLeft;
                            tilemapWalls.SetTile(tilePos2, tileToPaint2);

                            tilePos2 = startPosOffset + new Vector3Int(1, 2, 0) + new Vector3Int(0, startPosOffsetExtra, 0);
                            tileToPaint2 = tileCorner02LowerRight;
                            tilemapWalls.SetTile(tilePos2, tileToPaint2);

                            tilePos2 = startPosOffset + new Vector3Int(4, 2, 0) + new Vector3Int(0, startPosOffsetExtra, 0);
                            tileToPaint2 = tileCorner02LowerLeft;
                            tilemapWalls.SetTile(tilePos2, tileToPaint2);

                            tilePos2 = startPosOffset + new Vector3Int(2, 1, 0) + new Vector3Int(0, startPosOffsetExtra, 0);
                            tilemapWalls.SetTile(tilePos2, null);
                            tilePos2 = startPosOffset + new Vector3Int(2, 2, 0) + new Vector3Int(0, startPosOffsetExtra, 0);
                            tilemapWalls.SetTile(tilePos2, null);
                            tilePos2 = startPosOffset + new Vector3Int(3, 1, 0) + new Vector3Int(0, startPosOffsetExtra, 0);
                            tilemapWalls.SetTile(tilePos2, null);
                            tilePos2 = startPosOffset + new Vector3Int(3, 2, 0) + new Vector3Int(0, startPosOffsetExtra, 0);
                            tilemapWalls.SetTile(tilePos2, null);

                            tilePos2 = startPosOffset + new Vector3Int(1, startPos1.y - startPos2.y + sizeInTiles1.y, 0) + new Vector3Int(0, startPosOffsetExtra, 0);
                            tileToPaint2 = tileCornerUpperLeft;
                            tilemapWalls.SetTile(tilePos2, tileToPaint2);

                            tilePos2 = startPosOffset + new Vector3Int(4, startPos1.y - startPos2.y + sizeInTiles1.y, 0) + new Vector3Int(0, startPosOffsetExtra, 0);
                            tileToPaint2 = tileCornerUpperRight;
                            tilemapWalls.SetTile(tilePos2, tileToPaint2);

                            tilePos2 = startPosOffset + new Vector3Int(2, startPos1.y - startPos2.y + sizeInTiles1.y, 0) + new Vector3Int(0, startPosOffsetExtra, 0);
                            tilemapWalls.SetTile(tilePos2, null);
                            tilePos2 = startPosOffset + new Vector3Int(2, startPos1.y - startPos2.y + sizeInTiles1.y - 1, 0) + new Vector3Int(0, startPosOffsetExtra, 0);
                            tilemapFloorWalls.SetTile(tilePos2, null);
                            tilePos2 = startPosOffset + new Vector3Int(3, startPos1.y - startPos2.y + sizeInTiles1.y, 0) + new Vector3Int(0, startPosOffsetExtra, 0);
                            tilemapWalls.SetTile(tilePos2, null);
                            tilePos2 = startPosOffset + new Vector3Int(3, startPos1.y - startPos2.y + sizeInTiles1.y - 1, 0) + new Vector3Int(0, startPosOffsetExtra, 0);
                            tilemapFloorWalls.SetTile(tilePos2, null);

                            tilePos2 = startPosOffset + new Vector3Int(1, startPos1.y - startPos2.y + sizeInTiles1.y - 1, 0) + new Vector3Int(0, startPosOffsetExtra, 0);
                            tilemapWalls.SetTile(tilePos2, null);
                            tilePos2 = startPosOffset + new Vector3Int(4, startPos1.y - startPos2.y + sizeInTiles1.y - 1, 0) + new Vector3Int(0, startPosOffsetExtra, 0);
                            tilemapWalls.SetTile(tilePos2, null);

                            tilePos2 = startPosOffset + new Vector3Int(1, startPos1.y - startPos2.y + sizeInTiles1.y - 1, 0) + new Vector3Int(0, startPosOffsetExtra, 0);
                            tileToPaint2 = tileBricks03;
                            tilemapFloorWalls.SetTile(tilePos2, tileToPaint2);

                            tilePos2 = startPosOffset + new Vector3Int(4, startPos1.y - startPos2.y + sizeInTiles1.y - 1, 0) + new Vector3Int(0, startPosOffsetExtra, 0);
                            tileToPaint2 = tileBricks02;
                            tilemapFloorWalls.SetTile(tilePos2, tileToPaint2);

                            nodePairs.Add($"{fromNode},{toNode}");
                            if (!graphFinal.ContainsKey(fromNode))
                            {
                                graphFinal[fromNode] = new HashSet<int>();
                            }
                            if (!graphFinal.ContainsKey(toNode))
                            {
                                graphFinal[toNode] = new HashSet<int>();
                            }
                            graphFinal[fromNode].Add(toNode);
                            graphFinal[toNode].Add(fromNode);
                            if (!initialBuild)
                            {
                                Debug.Log("Changed graph.");
                                generateHallways(extraEdgesGraph, nodePairs, false, 1);
                                return;
                            }
                            continue;
                        }
                    }
                }
            }
        }
        generateHallways(extraEdgesGraph, nodePairs, false, buildStep + 1);
        return;
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
                /*
                for (int i = 0; i < lineIndex; ++i)
                {
                    Debug.Log(startPoints[i]);
                }
                */

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
                    for (int i = 1; i < sizeInTiles.x - 1; ++i)
                    {
                        for (int j = 2; j < sizeInTiles.y - 1; ++j)
                        {
                            Vector3Int tilePos = startPos + new Vector3Int(i, j, 0);
                            TileBase tileToPaint = getRandomTileFloor();
                            tilemapFloor.SetTile(tilePos, tileToPaint);
                        }
                    }

                    //FloorWalls
                    for (int i = 1; i < sizeInTiles.x - 1; ++i)
                    {
                        Vector3Int tilePos = startPos + new Vector3Int(i, sizeInTiles.y - 1, 0);
                        tilemapFloorWalls.SetTile(tilePos, tileBricks01);

                        tilePos = startPos + new Vector3Int(i, sizeInTiles.y, 0);
                        tilemapWalls.SetTile(tilePos, tileWallUpper);
                    }
                    Vector3Int tilePos2 = startPos + new Vector3Int(0, sizeInTiles.y - 1, 0);

                    //Walls
                    for (int j = 2; j < sizeInTiles.y; ++j)
                    {
                        Vector3Int tilePos = startPos + new Vector3Int(0, j, 0);
                        tilemapWalls.SetTile(tilePos, tileWallLeft);

                        tilePos = startPos + new Vector3Int(sizeInTiles.x - 1, j, 0);
                        tilemapWalls.SetTile(tilePos, tileWallRight);
                    }
                    for (int i = 1; i < sizeInTiles.x - 1; ++i)
                    {
                        Vector3Int tilePos = startPos + new Vector3Int(i, 1, 0);
                        tilemapWalls.SetTile(tilePos, tileBricks01);

                        tilePos = startPos + new Vector3Int(i, 2, 0);
                        tilemapWalls.SetTile(tilePos, tileWallUpper);
                    }

                    tilePos2 = startPos + new Vector3Int(0, sizeInTiles.y, 0);
                    tilemapWalls.SetTile(tilePos2, tileWallUpperLeft);
                    tilePos2 = startPos + new Vector3Int(sizeInTiles.x - 1, sizeInTiles.y, 0);
                    tilemapWalls.SetTile(tilePos2, tileWallUpperRight);

                    tilePos2 = startPos + new Vector3Int(0, 1, 0);
                    tilemapWalls.SetTile(tilePos2, tileWallLowerLeft);
                    tilePos2 = startPos + new Vector3Int(sizeInTiles.x - 1, 1, 0);
                    tilemapWalls.SetTile(tilePos2, tileWallLowerRight);
                }

                //GENERATE HALLWAYS
                HashSet<string> nodePairs = new HashSet<string>();

                generateHallways(extraEdgesGraph, nodePairs, true);
                generateHallways(extraEdgesGraph, nodePairs, false, 1);

                List<HashSet<int>> connectedComponents = new List<HashSet<int>>();
                HashSet<int> visitedNodes = new HashSet<int>();

                foreach (var nodePair2 in graphFinal)
                {
                    int fromNode2 = nodePair2.Key;

                    if (visitedNodes.Contains(fromNode2))
                        continue;

                    HashSet<int> currentComponent = new HashSet<int>();
                    DFS(fromNode2, graphFinal, visitedNodes, currentComponent);
                    connectedComponents.Add(currentComponent);
                }

                connectedComponents.Sort((a, b) => b.Count.CompareTo(a.Count));
                foreach (HashSet<int> component in connectedComponents)
                {
                    string componentString = "Connected Component: ";
                    foreach (int node in component)
                    {
                        componentString += node.ToString() + " ";
                    }
                    Debug.Log(componentString);
                }
                Debug.Log("Finished");

                //SELECT MAIN ROOMS
                if (connectedComponents.Count > 0)
                {
                    //Starting Room
                    List<int> firstConnectedComponent = new List<int>(connectedComponents[0]);
                    List<int> excludedValues = new List<int>() { };

                    int connectedRoomsCount = connectedComponents[0].Count;
                    int[] firstIndexElements = new int[connectedRoomsCount];
                    connectedComponents[0].CopyTo(firstIndexElements);
                    int startingRoom = GetRandomIntExcluding(0, connectedComponents[0].Count, excludedValues);
                    excludedValues.Add(startingRoom);
                    Debug.Log("Starting Room done: " + startingRoom);
                    
                    Vector3Int sizeInTilesStart = new Vector3Int(
                    Mathf.RoundToInt(sortedRectangles[firstConnectedComponent[startingRoom]].GetComponent<SpriteRenderer>().bounds.size.x / tileSize.x),
                    Mathf.RoundToInt(sortedRectangles[firstConnectedComponent[startingRoom]].GetComponent<SpriteRenderer>().bounds.size.y / tileSize.y),
                    1);
                    Vector3 newPosition = new Vector3 (sortedRectangles[firstConnectedComponent[startingRoom]].transform.position.x + sizeInTilesStart.x / 2, sortedRectangles[firstConnectedComponent[startingRoom]].transform.position.y + sizeInTilesStart.y / 2, 0);
                    Player.transform.position = newPosition;
                    playerGO.SetActive(true);

                    camera.GetComponent<FollowScript>().spawnedPlayer = true;

                    int treasureRoom = GetRandomIntExcluding(0, connectedComponents[0].Count, excludedValues);
                    excludedValues.Add(treasureRoom);

                    int enemyRoom01 = GetRandomIntExcluding(0, connectedComponents[0].Count, excludedValues);
                    excludedValues.Add(enemyRoom01);

                    int enemyRoom02 = GetRandomIntExcluding(0, connectedComponents[0].Count, excludedValues);
                    excludedValues.Add(enemyRoom02);

                    int enemyRoom03 = GetRandomIntExcluding(0, connectedComponents[0].Count, excludedValues);
                    excludedValues.Add(enemyRoom03);

                    Debug.Log("Treasure Room: " + treasureRoom);
                    Debug.Log("Enemy Room 1: " + enemyRoom01);
                    Debug.Log("Enemy Room 2: " + enemyRoom02);
                    Debug.Log("Enemy Room 3: " + enemyRoom03);

                    //Treasure room

                    Vector3Int sizeInTilesTreasure01 = new Vector3Int(
                    Mathf.RoundToInt(sortedRectangles[firstConnectedComponent[treasureRoom]].GetComponent<SpriteRenderer>().bounds.size.x / tileSize.x),
                    Mathf.RoundToInt(sortedRectangles[firstConnectedComponent[treasureRoom]].GetComponent<SpriteRenderer>().bounds.size.y / tileSize.y),
                    1);

                    Vector3 chest01Position = new Vector3(Mathf.Round(sortedRectangles[firstConnectedComponent[treasureRoom]].transform.position.x + sizeInTilesTreasure01.x / 2) + 1.5f, Mathf.Round(sortedRectangles[firstConnectedComponent[treasureRoom]].transform.position.y + sizeInTilesTreasure01.y / 2) + 0.8f, 0);
                    Vector3 chest02Position = new Vector3(Mathf.Round(sortedRectangles[firstConnectedComponent[treasureRoom]].transform.position.x + sizeInTilesTreasure01.x / 2) - 1.5f, Mathf.Round(sortedRectangles[firstConnectedComponent[treasureRoom]].transform.position.y + sizeInTilesTreasure01.y / 2) + 0.8f, 0);


                    Instantiate(ChestPrefab, chest01Position, Quaternion.identity);
                    Instantiate(ChestPrefab, chest02Position, Quaternion.identity);

                    Debug.Log("Treasure Room done: " + treasureRoom);
                    //Enemies

                    for (int i = 0; i < connectedRoomsCount; ++i)
                    {
                        Debug.Log("i = " + i);
                        if (i != treasureRoom && i != startingRoom && i != enemyRoom01 && i != enemyRoom02 && i != enemyRoom03)
                        {
                            Vector3Int sizeInTilesRoom = new Vector3Int(
                            Mathf.RoundToInt(sortedRectangles[firstConnectedComponent[i]].GetComponent<SpriteRenderer>().bounds.size.x / tileSize.x),
                            Mathf.RoundToInt(sortedRectangles[firstConnectedComponent[i]].GetComponent<SpriteRenderer>().bounds.size.y / tileSize.y),
                            1);

                            Vector3 enemy01Position = new Vector3(Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.x + sizeInTilesRoom.x / 2) + 1.5f, Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.y + sizeInTilesRoom.y / 2) + 0.8f, 0);
                            Vector3 enemy02Position = new Vector3(Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.x + sizeInTilesRoom.x / 2) - 1.5f, Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.y + sizeInTilesRoom.y / 2) + 0.8f, 0);

                            int randomEnemy01 = UnityEngine.Random.Range(0, 7);
                            int randomEnemy02 = UnityEngine.Random.Range(0, 7);

                            GameObject e01 = Instantiate(Enemies[randomEnemy01], enemy01Position, Quaternion.identity);
                            GameObject e02 = Instantiate(Enemies[randomEnemy02], enemy02Position, Quaternion.identity);

                            e01.GetComponent<Enemy>().setPlayer(Player);
                            e01.GetComponent<Enemy>().setPlayerGO(playerGO);
                            e02.GetComponent<Enemy>().setPlayer(Player);
                            e02.GetComponent<Enemy>().setPlayerGO(playerGO);

                        } else if (i != treasureRoom)
                        {
                            if (i == enemyRoom01)
                            {
                                Vector3Int sizeInTilesRoom = new Vector3Int(
                                Mathf.RoundToInt(sortedRectangles[firstConnectedComponent[i]].GetComponent<SpriteRenderer>().bounds.size.x / tileSize.x),
                                Mathf.RoundToInt(sortedRectangles[firstConnectedComponent[i]].GetComponent<SpriteRenderer>().bounds.size.y / tileSize.y),
                                1);

                                Vector3 enemy01Position = new Vector3(Mathf.Round(Random.Range(Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.x + 1), Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.x + sizeInTilesRoom.x - 3))) + 0.5f, Mathf.Round(Random.Range(Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.y + 3), Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.y + sizeInTilesRoom.y - 1))) - 0.5f, 0);
                                Vector3 enemy02Position = new Vector3(Mathf.Round(Random.Range(Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.x + 1), Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.x + sizeInTilesRoom.x - 3))) + 0.5f, Mathf.Round(Random.Range(Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.y + 3), Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.y + sizeInTilesRoom.y - 1))) - 0.5f, 0);
                                Vector3 enemy03Position = new Vector3(Mathf.Round(Random.Range(Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.x + 1), Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.x + sizeInTilesRoom.x - 3))) + 0.5f, Mathf.Round(Random.Range(Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.y + 3), Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.y + sizeInTilesRoom.y - 1))) - 0.5f, 0);
                                Vector3 enemy04Position = new Vector3(Mathf.Round(Random.Range(Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.x + 1), Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.x + sizeInTilesRoom.x - 3))) + 0.5f, Mathf.Round(Random.Range(Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.y + 3), Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.y + sizeInTilesRoom.y - 1))) - 0.5f, 0);

                                int randomEnemy01 = 0;
                                int randomEnemy02 = 0;
                                int randomEnemy03 = 1;
                                int randomEnemy04 = 1;

                                GameObject e01 = Instantiate(Enemies[randomEnemy01], enemy01Position, Quaternion.identity);
                                GameObject e02 = Instantiate(Enemies[randomEnemy02], enemy02Position, Quaternion.identity);
                                GameObject e03 = Instantiate(Enemies[randomEnemy03], enemy03Position, Quaternion.identity);
                                GameObject e04 = Instantiate(Enemies[randomEnemy04], enemy04Position, Quaternion.identity);

                                e01.GetComponent<Enemy>().setPlayer(Player);
                                e01.GetComponent<Enemy>().setPlayerGO(playerGO);
                                e02.GetComponent<Enemy>().setPlayer(Player);
                                e02.GetComponent<Enemy>().setPlayerGO(playerGO);
                                e03.GetComponent<Enemy>().setPlayer(Player);
                                e03.GetComponent<Enemy>().setPlayerGO(playerGO);
                                e04.GetComponent<Enemy>().setPlayer(Player);
                                e04.GetComponent<Enemy>().setPlayerGO(playerGO);
                            } else if (i == enemyRoom02)
                            {
                                Vector3Int sizeInTilesRoom = new Vector3Int(
                                Mathf.RoundToInt(sortedRectangles[firstConnectedComponent[i]].GetComponent<SpriteRenderer>().bounds.size.x / tileSize.x),
                                Mathf.RoundToInt(sortedRectangles[firstConnectedComponent[i]].GetComponent<SpriteRenderer>().bounds.size.y / tileSize.y),
                                1);

                                Vector3 enemy01Position = new Vector3(Mathf.Round(Random.Range(Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.x + 1), Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.x + sizeInTilesRoom.x - 3))) + 0.5f, Mathf.Round(Random.Range(Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.y + 3), Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.y + sizeInTilesRoom.y - 1))) - 0.5f, 0);
                                Vector3 enemy02Position = new Vector3(Mathf.Round(Random.Range(Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.x + 1), Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.x + sizeInTilesRoom.x - 3))) + 0.5f, Mathf.Round(Random.Range(Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.y + 3), Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.y + sizeInTilesRoom.y - 1))) - 0.5f, 0);
                                Vector3 enemy03Position = new Vector3(Mathf.Round(Random.Range(Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.x + 1), Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.x + sizeInTilesRoom.x - 3))) + 0.5f, Mathf.Round(Random.Range(Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.y + 3), Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.y + sizeInTilesRoom.y - 1))) - 0.5f, 0);
                                Vector3 enemy04Position = new Vector3(Mathf.Round(Random.Range(Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.x + 1), Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.x + sizeInTilesRoom.x - 3))) + 0.5f, Mathf.Round(Random.Range(Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.y + 3), Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.y + sizeInTilesRoom.y - 1))) - 0.5f, 0);

                                int randomEnemy01 = 5;
                                int randomEnemy02 = 5;
                                int randomEnemy03 = 5;
                                int randomEnemy04 = 5;

                                GameObject e01 = Instantiate(Enemies[randomEnemy01], enemy01Position, Quaternion.identity);
                                GameObject e02 = Instantiate(Enemies[randomEnemy02], enemy02Position, Quaternion.identity);
                                GameObject e03 = Instantiate(Enemies[randomEnemy03], enemy03Position, Quaternion.identity);
                                GameObject e04 = Instantiate(Enemies[randomEnemy04], enemy04Position, Quaternion.identity);

                                e01.GetComponent<Enemy>().setPlayer(Player);
                                e01.GetComponent<Enemy>().setPlayerGO(playerGO);
                                e02.GetComponent<Enemy>().setPlayer(Player);
                                e02.GetComponent<Enemy>().setPlayerGO(playerGO);
                                e03.GetComponent<Enemy>().setPlayer(Player);
                                e03.GetComponent<Enemy>().setPlayerGO(playerGO);
                                e04.GetComponent<Enemy>().setPlayer(Player);
                                e04.GetComponent<Enemy>().setPlayerGO(playerGO);
                            } else if (i == enemyRoom03)
                            {
                                Vector3Int sizeInTilesRoom = new Vector3Int(
                                Mathf.RoundToInt(sortedRectangles[firstConnectedComponent[i]].GetComponent<SpriteRenderer>().bounds.size.x / tileSize.x),
                                Mathf.RoundToInt(sortedRectangles[firstConnectedComponent[i]].GetComponent<SpriteRenderer>().bounds.size.y / tileSize.y),
                                1);

                                Vector3 enemy01Position = new Vector3(Mathf.Round(Random.Range(Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.x + 1), Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.x + sizeInTilesRoom.x - 3))) + 0.5f, Mathf.Round(Random.Range(Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.y + 3), Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.y + sizeInTilesRoom.y - 1))) - 0.5f, 0);
                                Vector3 enemy02Position = new Vector3(Mathf.Round(Random.Range(Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.x + 1), Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.x + sizeInTilesRoom.x - 3))) + 0.5f, Mathf.Round(Random.Range(Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.y + 3), Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.y + sizeInTilesRoom.y - 1))) - 0.5f, 0);
                                Vector3 enemy03Position = new Vector3(Mathf.Round(Random.Range(Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.x + 1), Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.x + sizeInTilesRoom.x - 3))) + 0.5f, Mathf.Round(Random.Range(Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.y + 3), Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.y + sizeInTilesRoom.y - 1))) - 0.5f, 0);
                                Vector3 enemy04Position = new Vector3(Mathf.Round(Random.Range(Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.x + 1), Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.x + sizeInTilesRoom.x - 3))) + 0.5f, Mathf.Round(Random.Range(Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.y + 3), Mathf.Round(sortedRectangles[firstConnectedComponent[i]].transform.position.y + sizeInTilesRoom.y - 1))) - 0.5f, 0);

                                int randomEnemy01 = 4;
                                int randomEnemy02 = 4;
                                int randomEnemy03 = 4;
                                int randomEnemy04 = 4;

                                GameObject e01 = Instantiate(Enemies[randomEnemy01], enemy01Position, Quaternion.identity);
                                GameObject e02 = Instantiate(Enemies[randomEnemy02], enemy02Position, Quaternion.identity);
                                GameObject e03 = Instantiate(Enemies[randomEnemy03], enemy03Position, Quaternion.identity);
                                GameObject e04 = Instantiate(Enemies[randomEnemy04], enemy04Position, Quaternion.identity);

                                e01.GetComponent<Enemy>().setPlayer(Player);
                                e01.GetComponent<Enemy>().setPlayerGO(playerGO);
                                e02.GetComponent<Enemy>().setPlayer(Player);
                                e02.GetComponent<Enemy>().setPlayerGO(playerGO);
                                e03.GetComponent<Enemy>().setPlayer(Player);
                                e03.GetComponent<Enemy>().setPlayerGO(playerGO);
                                e04.GetComponent<Enemy>().setPlayer(Player);
                                e04.GetComponent<Enemy>().setPlayerGO(playerGO);
                            }
                        }
                    }
                }

                //DEACTIVATE ROOM RECTANGLES
                foreach (GameObject rectangle in rectangles)
                {
                    rectangle.SetActive(false);
                }

                //DEACTIVATE ROOM RECTANGLES
                foreach (GameObject rectangle in rectangles)
                {
                    rectangle.SetActive(false);
                }


                /*
                //REMOVE SINGLE ROOMS
                roomsToDraw = numRectanglesToChange;
                foreach (GameObject rectangle in sortedRectangles)
                {
                    if (graphFinal.ContainsKey(sortedRectangles.IndexOf(rectangle))) continue;

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
                    for (int i = 1; i < sizeInTiles.x - 1; ++i)
                    {
                        for (int j = 2; j < sizeInTiles.y - 1; ++j)
                        {
                            Vector3Int tilePos = startPos + new Vector3Int(i, j, 0);
                            tilemapFloor.SetTile(tilePos, null);
                        }
                    }

                    //FloorWalls
                    for (int i = 1; i < sizeInTiles.x - 1; ++i)
                    {
                        Vector3Int tilePos = startPos + new Vector3Int(i, sizeInTiles.y - 1, 0);
                        tilemapFloorWalls.SetTile(tilePos, null);

                        tilePos = startPos + new Vector3Int(i, sizeInTiles.y, 0);
                        tilemapWalls.SetTile(tilePos, null);
                    }
                    Vector3Int tilePos2 = startPos + new Vector3Int(0, sizeInTiles.y - 1, 0);

                    //Walls
                    for (int j = 2; j < sizeInTiles.y; ++j)
                    {
                        Vector3Int tilePos = startPos + new Vector3Int(0, j, 0);
                        tilemapWalls.SetTile(tilePos, null);

                        tilePos = startPos + new Vector3Int(sizeInTiles.x - 1, j, 0);
                        tilemapWalls.SetTile(tilePos, null);
                    }
                    for (int i = 1; i < sizeInTiles.x - 1; ++i)
                    {
                        Vector3Int tilePos = startPos + new Vector3Int(i, 1, 0);
                        tilemapWalls.SetTile(tilePos, null);

                        tilePos = startPos + new Vector3Int(i, 2, 0);
                        tilemapWalls.SetTile(tilePos, null);
                    }

                    tilePos2 = startPos + new Vector3Int(0, sizeInTiles.y, 0);
                    tilemapWalls.SetTile(tilePos2, null);
                    tilePos2 = startPos + new Vector3Int(sizeInTiles.x - 1, sizeInTiles.y, 0);
                    tilemapWalls.SetTile(tilePos2, null);

                    tilePos2 = startPos + new Vector3Int(0, 1, 0);
                    tilemapWalls.SetTile(tilePos2, null);
                    tilePos2 = startPos + new Vector3Int(sizeInTiles.x - 1, 1, 0);
                    tilemapWalls.SetTile(tilePos2, null);
                }*/

                /*
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
                */
            }
        }
    }

}
