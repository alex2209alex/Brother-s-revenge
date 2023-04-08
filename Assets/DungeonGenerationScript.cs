using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DelaunatorSharp;
using DelaunatorSharp.Unity.Extensions;

public class DungeonGenerationScript : MonoBehaviour
{
    public int numRooms;
    public float mainRoomsPercentage = 0.08f;
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
    public float lineWidth = 2f;
    public string sortingLayerName = "Default";
    public int sortingOrder = 100;

    private LineRenderer[] lineRenderers;


    [SerializeField] private Material material;
    private Delaunator delaunator;


    Sprite CreateRectangleSprite(float width, float height)
    {
        Texture2D texture = new Texture2D((int)width, (int)height);  // create a new texture
        Color[] colors = new Color[(int)(width * height)];  // create an array of colors

        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.white;  // set all the colors to white
        }

        texture.SetPixels(colors);  // set the colors of the texture
        texture.Apply();  // apply the changes to the texture

        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, width, height), Vector2.zero);  // create a new sprite from the texture

        return sprite;  // return the sprite
    }
    
    void Start()
    {
        cellSize = Map.cellSize[0] * 100;
        rectangles = new GameObject[numRooms];

        rectanglesRigidBodies = new Rigidbody2D[numRooms];

        sortedRectangles = new List<GameObject>();

        for (int i = 0; i < numRooms; i++)
        {
            int meanWidth = 10;
            int stdDevWidth = 8;
            int minWidth = 5;
            int maxWidth = 22;

            int width = Mathf.RoundToInt(Mathf.Clamp(UnityEngine.Random.Range(meanWidth - stdDevWidth, meanWidth + stdDevWidth + 1), minWidth, maxWidth));

            int meanHeight = 11;
            int stdDevHeight = 8;
            int minHeight = 6;
            int maxHeight = 23;

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
                // Sort rectangles by area
                sortedRectangles.Sort((r1, r2) => r2.GetComponent<SpriteRenderer>().sprite.texture.width * r2.GetComponent<SpriteRenderer>().sprite.texture.height -
                                                r1.GetComponent<SpriteRenderer>().sprite.texture.width * r1.GetComponent<SpriteRenderer>().sprite.texture.height);

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

                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int triIndex1 = triangles[i];
                    int triIndex2 = triangles[i + 1];
                    int triIndex3 = triangles[i + 2];

                    startPoints[i] = new Vector2(sortedRectangles[triIndex1].transform.position.x + sortedRectangles[triIndex1].GetComponent<SpriteRenderer>().bounds.size.x / 2, sortedRectangles[triIndex1].transform.position.y + sortedRectangles[triIndex1].GetComponent<SpriteRenderer>().bounds.size.y / 2);
                    endPoints[i] = new Vector2(sortedRectangles[triIndex2].transform.position.x + sortedRectangles[triIndex2].GetComponent<SpriteRenderer>().bounds.size.x / 2, sortedRectangles[triIndex2].transform.position.y + sortedRectangles[triIndex2].GetComponent<SpriteRenderer>().bounds.size.y / 2);
                    Debug.Log("start1 pos: " + startPoints[i] + " end pos: " + endPoints[i]);

                    startPoints[i + 1] = new Vector2(sortedRectangles[triIndex2].transform.position.x + sortedRectangles[triIndex2].GetComponent<SpriteRenderer>().bounds.size.x / 2, sortedRectangles[triIndex2].transform.position.y + sortedRectangles[triIndex2].GetComponent<SpriteRenderer>().bounds.size.y / 2);
                    endPoints[i + 1] = new Vector2(sortedRectangles[triIndex3].transform.position.x + sortedRectangles[triIndex3].GetComponent<SpriteRenderer>().bounds.size.x / 2, sortedRectangles[triIndex3].transform.position.y + sortedRectangles[triIndex3].GetComponent<SpriteRenderer>().bounds.size.y / 2);
                    Debug.Log("start2 pos: " + startPoints[i + 1] + " end pos: " + endPoints[i + 1]);

                    startPoints[i + 2] = new Vector2(sortedRectangles[triIndex3].transform.position.x + sortedRectangles[triIndex3].GetComponent<SpriteRenderer>().bounds.size.x / 2, sortedRectangles[triIndex3].transform.position.y + sortedRectangles[triIndex3].GetComponent<SpriteRenderer>().bounds.size.y / 2);
                    endPoints[i + 2] = new Vector2(sortedRectangles[triIndex1].transform.position.x + sortedRectangles[triIndex1].GetComponent<SpriteRenderer>().bounds.size.x / 2, sortedRectangles[triIndex1].transform.position.y + sortedRectangles[triIndex1].GetComponent<SpriteRenderer>().bounds.size.y / 2);
                    Debug.Log("start3 pos: " + startPoints[i + 2] + " end pos: " + endPoints[i + 2]);

                    Debug.Log("Triangle " + (i / 3 + 1) + ": " + triIndex1 + ", " + triIndex2 + ", " + triIndex3);
                }

                // Loop through the number of lines and create a LineRenderer component for each one
                for (int i = 0; i < numLines; i++)
                {
                    // Create a new GameObject for the line
                    GameObject lineObj = new GameObject("Line" + i);
                    lineObj.transform.SetParent(transform); // Set the parent of the line object to the current object

                    // Add a LineRenderer component to the line object
                    LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();

                    // Set the positions, color, and width of the line
                    lineRenderer.positionCount = 2;
                    lineRenderer.SetPosition(0, startPoints[i]);
                    lineRenderer.SetPosition(1, endPoints[i]);
                    lineRenderer.startColor = lineColor;
                    lineRenderer.endColor = lineColor;
                    lineRenderer.startWidth = lineWidth;
                    lineRenderer.endWidth = lineWidth;

                    // Set the sorting layer and order of the line renderer
                    lineRenderer.sortingLayerName = sortingLayerName;
                    lineRenderer.sortingOrder = sortingOrder;

                    // Add the LineRenderer component to the array
                    lineRenderers[i] = lineRenderer;
                }


            }
        }
    }

}
