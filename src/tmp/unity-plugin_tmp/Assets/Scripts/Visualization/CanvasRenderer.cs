using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class CanvasRenderer
{
    GameObject gameObject;
    RawImage image;
    Texture2D texture;

    public CanvasRenderer(RawImage image, Color bgColor)
    {
        Rect rect = image.rectTransform.rect;
        rect.width = Mathf.CeilToInt(rect.width);
        rect.height = Mathf.CeilToInt(rect.height);

        texture = new Texture2D((int)rect.width, (int)rect.height);
        Color[] bg = new Color[(int)rect.width * (int)rect.height];

        this.image = image;
        image.color = Color.white;
        image.texture = texture;

        for (int i = 0; i < bg.Length - 1; i++)
        {
            bg[i] = bgColor;
        }

        texture.SetPixels(bg);
        texture.Apply();
    }

    public void DrawLine(Vector2 start, Vector2 end, Color color)
    {
        if (end.x < start.x)
        {
            float temp = start.x;
            start.x = end.x;
            end.x = temp;

            temp = start.y;
            start.y = end.y;
            end.y = temp;
        }

        float m = (start.y - end.y) / (start.x - end.x);

        for (float x = start.x; x < end.x; x++)
        {
            float y = m * (x - start.x) + start.y;
            
            texture.SetPixel(Mathf.RoundToInt(x), Mathf.RoundToInt(y), color);
        }

        texture.Apply();
    }

    public void DrawRectangle(Vector2 position, Vector2 size, Color color, Color outline)
    {
        Vector2Int rPos = position.RoundToInt();
        Vector2Int rSize = size.RoundToInt();

        Color[] colors = Enumerable.Repeat(color, rSize.x * rSize.y).ToArray();

        for (int x = 0, y = 0; x < rSize.x; x++)        // Top outline
        {
            SetColor(x, y, rSize.x, colors, out colors, outline);
        }
        for (int x = 0, y = 0; y < rSize.y; y++)        // Left outline
        {
            SetColor(x, y, rSize.x, colors, out colors, outline);
        }
        for (int x = 0, y = rSize.y; x < rSize.x; x++)  // Bottom outline
        {
            SetColor(x, y, rSize.x, colors, out colors, outline);
        }
        for (int x = rSize.x, y = 0; y < rSize.y; y++)  // Right outline
        {
            SetColor(x, y, rSize.x, colors, out colors, outline);
        }


        texture.SetPixels(rPos.x, rPos.y, rSize.x, rSize.y, colors);
        texture.Apply();
    }

    void SetColor(int x, int y, int width, Color[] inColors, out Color[] outColors, Color outline)
    {
        outColors = inColors;

        int idx = x + y * width;
        if (idx < inColors.Length) outColors[idx] = outline;
    }
}

public static class Extensions
{
    public static Vector2 Round(this Vector2 vector)
    {
        Vector2 rVector = new Vector2();

        rVector.x = Mathf.RoundToInt(vector.x);
        rVector.y = Mathf.RoundToInt(vector.y);

        return rVector;
    }

    public static Vector2Int RoundToInt(this Vector2 vector)
    {
        Vector2Int rVector = new Vector2Int();

        rVector.x = Mathf.RoundToInt(vector.x);
        rVector.y = Mathf.RoundToInt(vector.y);

        return rVector;
    }
}