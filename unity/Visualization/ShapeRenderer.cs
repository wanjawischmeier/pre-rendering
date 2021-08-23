using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Internal;

public class ShapeRenderer
{
    Queue<GameObject> shapes;
    GameObject shape;
    List<GameObject> initLines;
    Camera mainCam;

    public ShapeRenderer(Camera camera, Material material, int size = 0, int points = 2)
    {
        mainCam = camera;

        if (size > 0)
        {
            shapes = new Queue<GameObject>(size);

            for (int i = 0; i < size; i++)
            {
                GameObject gameObject = new GameObject(
                    string.Format("Shape {0}", i.ToString())
                );
                gameObject.transform.parent = camera.transform;

                LineRenderer line = gameObject.AddComponent<LineRenderer>();
                line.receiveShadows = false;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.material = material;
                line.enabled = false;

                shapes.Enqueue(gameObject);
            }
        }
        else
        {
            shape = new GameObject("Shape");
            shape.transform.parent = camera.transform;

            LineRenderer line = shape.AddComponent<LineRenderer>();
            line.receiveShadows = false;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.material = material;
            line.enabled = false;
        }
    }

    public void DrawLine(Vector3 start, Vector3 end, float width = 1)
    {
        DrawLine(new Vector3[] { start, end }, Color.white, width);
    }

    public void DrawLine(Vector3 start, Vector3 end, Color color, float width = 1)
    {
        DrawLine(new Vector3[] { start, end }, color, width);
    }

    public void DrawLine(Vector3[] positions, float width = 1)
    {
        DrawLine(positions, Color.white, width);
    }

    public void DrawLine(Vector2[] positions, float width = 1)
    {
        DrawLine(positions, Color.white, width);
    }

    public void DrawLine(Vector2[] positions, Color color, float width = 1)
    {
        Vector3[] converted = new Vector3[positions.Length];

        for (int i = 0; i < positions.Length; i++)
        {
            converted[i] = new Vector3(positions[i].x, positions[i].y, mainCam.nearClipPlane);
        }

        DrawLine(converted, color, width);
    }

    public void DrawLine(Vector3[] positions, Color color, float width = 1, bool loop = false, int sortingOrder = 0)
    {
        GameObject gameObject;

        if (shapes != null) gameObject = shapes.Dequeue();
        else gameObject = shape;

        LineRenderer line = gameObject.GetComponent<LineRenderer>();
        line.loop = loop;
        line.GetComponent<LineRenderer>().sortingOrder = sortingOrder;
        line.positionCount = positions.Length;
        line.SetPositions(positions);

        Keyframe[] keyframes = new Keyframe[] { new Keyframe(0, width) };
        AnimationCurve curve = new AnimationCurve(keyframes);

        line.widthCurve = curve;
        line.startColor = color;
        line.endColor = Color.blue;
        Debug.Log(color);
        line.SetPositions(positions);
        line.enabled = true;

        if (shapes != null) shapes.Enqueue(gameObject);
    }

    public void DrawRectangle(Vector2 position, Vector2 size, Color color, float lineWidth = 1, int sortingOrder = 0, bool convertFromScreenSpace = false)
    {
        Vector3 converted = new Vector3(position.x, position.y, mainCam.nearClipPlane);

        if (convertFromScreenSpace) converted = mainCam.ScreenToWorldPoint(converted);

        DrawRectangle(converted, size, color, lineWidth, sortingOrder);
    }

    public void DrawRectangle(Vector3 position, Vector2 size, Color color, float lineWidth = 1, int sortingOrder = 0)
    {
        Vector3[] vectors = new Vector3[]
        {
            new Vector3(position.x, position.y, position.z), new Vector3(position.x + size.x, position.y, position.z),
            new Vector3(position.x + size.x, position.y + size.y, position.z), new Vector3(position.x, position.y + size.y, position.z)
        };

        DrawLine(vectors, color, lineWidth, true, sortingOrder);
    }
}
