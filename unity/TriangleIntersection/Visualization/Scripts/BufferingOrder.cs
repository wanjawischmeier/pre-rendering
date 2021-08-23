using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BufferingOrder : MonoBehaviour
{
    public RawImage image;
    CanvasRenderer canvasRenderer;

    float width;            float height;
    int colSize;            int rowSize;
    int cCol;               int cRow;
    float colWidth = 10f;   float rowHeight = 10f;
    public int bufferSize;
    public float colSteps;
    int frame = 0;

    void Start()
    {
        canvasRenderer = new CanvasRenderer(image, Color.clear);

        width = image.rectTransform.rect.width;
        height = image.rectTransform.rect.height;

        colSize = (int)Mathf.Floor(width / colWidth);
        rowSize = (int)Mathf.Floor(height / rowHeight);

        cCol = (int)Mathf.Floor((width / 2) / colWidth);
        cRow = (int)Mathf.Floor((height / 2) / rowHeight);

        Color squareCol;

        for (int col = 0; col < colSize; col++)
        {
            for (int row = 0; row < rowSize; row++)
            {
                if (col == cCol && row == cRow) squareCol = Color.white;
                else squareCol = Color.black;

                Vector2 position = new Vector2(col * colWidth, row * rowHeight);
                Vector2 size = new Vector2(colWidth, rowHeight);

                canvasRenderer.DrawRectangle(position, size, squareCol, Color.white);
            }
        }

        if (colSize < rowSize) bufferSize = (int)Mathf.Floor(colSize / 2);
        else bufferSize = (int)Mathf.Floor(rowSize / 2);
        colSteps = 1f / bufferSize;
    }
    
    void Update()
    {
        if (frame < bufferSize)
        {
            int w = frame;

            Vector2 size = new Vector2(colWidth, rowHeight);

            for (int i = -w; i < w; i++)
            {
                float importance = 1 - ((bufferSize - w) * colSteps);
                Color squareCol = new Color(1, importance, 0.2f);

                canvasRenderer.DrawRectangle(
                    new Vector2((cCol + i) * colWidth, (cRow + w) * rowHeight),
                    size, squareCol, Color.black
                );
                canvasRenderer.DrawRectangle(
                    new Vector2((cCol - i) * colWidth, (cRow - w) * rowHeight),
                    size, squareCol, Color.black
                );
                canvasRenderer.DrawRectangle(
                    new Vector2((cCol + w) * colWidth, (cRow - i) * rowHeight),
                    size, squareCol, Color.black
                );
                canvasRenderer.DrawRectangle(
                    new Vector2((cCol -w) * colWidth, (cRow + i) * rowHeight),
                    size, squareCol, Color.black
                );
            }

            frame++;
        }
    }
}
