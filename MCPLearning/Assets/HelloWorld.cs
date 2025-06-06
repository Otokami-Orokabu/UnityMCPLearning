using UnityEngine;

public class HelloWorld : MonoBehaviour
{
    void Start()
    {
        Debug.Log("Hello, World!");
        Debug.Log("Unity MCP Integration is working!");
        Debug.Log($"GameObject: {gameObject.name} says hello at {System.DateTime.Now}");
    }
    
    void Update()
    {
        // 5秒ごとにメッセージを表示
        if (Time.time % 5.0f < Time.deltaTime)
        {
            Debug.Log($"Hello from {gameObject.name} - Time: {Time.time:F1}s");
        }
    }
}