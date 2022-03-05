using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineDebugger : MonoBehaviour
{
    public Material material;
    public Vector2 pos, scl;
    public Color col;

    private Square square;

    private void Start()
    {
        Shape.material = material;
        Shape.shapes = 0;
        square = new Square(transform, pos, scl);
    }

    private void Update()
    {
        square.SetVertecies(pos, scl);
        square.Color = col;

        if (Shape.shapes > 1000) return;

        var position = new Vector2(
            Random.Range(0, Screen.width),
            Random.Range(0, Screen.height));

        var scale = new Vector2(
            Random.Range(0, Screen.width - position.x),
            Random.Range(0, Screen.height - position.y));

        new Square(transform, position, scale).Color = Random.ColorHSV();
    }


    public class Shape
    {
        public static Material material;
        public static int shapes;

        public CanvasRenderer renderer;
        public Mesh mesh;
    }


    public class Square : Shape
    {
        public Color Color
        {
            get => localMaterial.color;
            set
            {
                localMaterial.color = value;
                localMaterial.SetColor("_Color", value);
                renderer.SetMaterial(localMaterial, null);
            }
        }

        public Vector2 position, scale;
        private Material localMaterial;

        public Square(Transform parent, Vector2 position, Vector2 scale)
        {
            localMaterial = new Material(material);

            var rendererObj = new GameObject("Square");
            rendererObj.transform.parent = parent;

            renderer = rendererObj.AddComponent<CanvasRenderer>();
            renderer.SetMaterial(localMaterial, null);
            
            mesh = new Mesh();
            SetVertecies(position, scale);
            mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
            renderer.SetMesh(mesh);

            shapes++;
        }

        public void SetVertecies(Vector2 position, Vector2 scale)
        {
            this.position = position;
            this.scale = scale;

            var V1 = new Vector2(position.x, position.y);
            var V2 = new Vector2(position.x, position.y + scale.y);
            var V3 = new Vector2(position.x + scale.x, position.y + scale.y);
            var V4 = new Vector2(position.x + scale.x, position.y);

            mesh.vertices = new Vector3[] { V1, V2, V3, V4 };
            renderer.SetMesh(mesh);
        }
    }
}
