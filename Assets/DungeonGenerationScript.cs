using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonGenerationScript : MonoBehaviour
{
    public float cellSize;
    public int numRooms;
    public Color color = Color.white;
    public Grid Map;
    private GameObject[] rectangles;
    private Rigidbody2D[] rectanglesRigidBodies;
    private bool dungeonBuilt = false;
    List<GameObject> sortedRectangles;


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

            int width = Mathf.RoundToInt(Mathf.Clamp(Random.Range(meanWidth - stdDevWidth, meanWidth + stdDevWidth + 1), minWidth, maxWidth));

            int meanHeight = 11;
            int stdDevHeight = 8;
            int minHeight = 6;
            int maxHeight = 23;

            int height = Mathf.RoundToInt(Mathf.Clamp(Random.Range(meanHeight - stdDevHeight, meanHeight + stdDevHeight + 1), minHeight, maxHeight));


            GameObject rectangle = new GameObject("Rectangle" + i);
            SpriteRenderer spriteRenderer = rectangle.AddComponent<SpriteRenderer>();
            spriteRenderer.color = color;
            spriteRenderer.sprite = CreateRectangleSprite(cellSize * width, cellSize * height);
            rectangle.transform.position = new Vector2(Random.Range(-25, 25), Random.Range(-25, 25));
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
            Vector2 force = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
            rigidbody.AddForce(force * Random.Range(50f, 100f));

            rectangles[i].AddComponent<RoomScript>();

            sortedRectangles.Add(rectangle);
        }

        // Sort rectangles by area
        sortedRectangles.Sort((r1, r2) => r2.GetComponent<SpriteRenderer>().sprite.texture.width * r2.GetComponent<SpriteRenderer>().sprite.texture.height -
                                        r1.GetComponent<SpriteRenderer>().sprite.texture.width * r1.GetComponent<SpriteRenderer>().sprite.texture.height);
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
                }

                int numRectanglesToChange = Mathf.CeilToInt(sortedRectangles.Count * 0.08f);
                for (int i = 0; i < numRectanglesToChange; i++)
                {
                    SpriteRenderer spriteRenderer = sortedRectangles[i].GetComponent<SpriteRenderer>();
                    spriteRenderer.color = Color.red;
                }
            }
        }
    }

}
