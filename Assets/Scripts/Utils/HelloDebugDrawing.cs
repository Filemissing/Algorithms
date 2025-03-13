using UnityEngine;

public class HelloDebugDrawing : MonoBehaviour
{
    
    private DebugDrawingBatcher _gizmoBatcher;
    private DebugDrawingBatcher _rectIntBatcher;
    
    void Start()
    {
        DebugDrawingBatcher.GetInstance().BatchCall(() =>
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(new Vector3(7.5f, 0, 7.5f), 1.0f);
        });
        
        _gizmoBatcher = DebugDrawingBatcher.GetInstance("_gizmoBatcher");
        _rectIntBatcher = DebugDrawingBatcher.GetInstance("_rectIntBatcher");
        
        RectInt a = new RectInt(0, 0, 10, 10);
        RectInt b = new RectInt(5, 5, 10, 10);
        RectInt c = AlgorithmsUtils.Intersect(a, b);
        
        _gizmoBatcher.BatchCall(() =>
        {
            Debug.DrawLine(Vector3.zero, Vector3.right, Color.red);
            Debug.DrawLine(Vector3.zero, Vector3.up, Color.green);
            Debug.DrawLine(Vector3.zero, Vector3.forward, Color.blue);
        });
        
        _rectIntBatcher.BatchCall(() =>
        {
            AlgorithmsUtils.DebugRectInt(a, Color.red);
            AlgorithmsUtils.DebugRectInt(b, Color.green);
            AlgorithmsUtils.DebugRectInt(c, Color.yellow);
        });
    }

}
