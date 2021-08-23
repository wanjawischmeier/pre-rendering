using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pooling : MonoBehaviour
{
    public GameObject preFab;
    [Range(10, 40000)]
    public int poolSize = 20;
    [Range(0.1f, 2)]
    public float movementSpeed = 0.5f;
    public float tolerance;
    Queue<GameObject> pool;
    Vector3 lastPosition = Vector3.zero;

    void Start()
    {
        //Camera.main.transform.position = new Vector3(0, poolSize, 0);

        pool = new Queue<GameObject>();

        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(preFab);
            //obj.SetActive(false);
            //obj.transform.SetParent(transform);
            obj.transform.position = new Vector3(Random.Range(-400, 400), 1, Random.Range(-400, 400));
            pool.Enqueue(obj);
        }
    }

    void Update()
    {
        transform.position = new Vector3(transform.position.x + Input.GetAxis("Horizontal") * movementSpeed, 0.8f, transform.position.z + Input.GetAxis("Vertical") * movementSpeed);

        if (
            transform.position.x < lastPosition.x - tolerance || transform.position.x > lastPosition.x + tolerance ||
            transform.position.z < lastPosition.z - tolerance || transform.position.z > lastPosition.z + tolerance
            )
        {
            GameObject obj = pool.Dequeue();
            
            obj.transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z);
            obj.transform.rotation = Quaternion.Euler(Vector3.zero);
            //obj.GetComponent<Rigidbody>().velocity = Vector3.zero;
            lastPosition = transform.position;
            obj.SetActive(true);

            pool.Enqueue(obj);
        }
    }
}
