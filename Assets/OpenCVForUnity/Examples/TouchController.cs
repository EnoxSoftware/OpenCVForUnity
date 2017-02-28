using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Linq;

namespace OpenCVForUnityExample
{
    public class TouchController : MonoBehaviour
    {
        public GameObject Cube;
        public float Speed = 0.1f;
    
        void Update ()
        {
            #if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)

        //Touch
        int touchCount = Input.touchCount;
        
        if (touchCount == 1)
        {
            
            Touch t = Input.GetTouch(0);
            if(EventSystem.current.IsPointerOverGameObject(t.fingerId))return;
            
            switch (t.phase)
            {
            case TouchPhase.Moved:
                
                float xAngle = t.deltaPosition.y * Speed;
                float yAngle = -t.deltaPosition.x * Speed;
                float zAngle = 0;
                
                Cube.transform.Rotate(xAngle, yAngle, zAngle, Space.World);
                
                break;
            }
            
        }

            #else
            //Mouse
            if (Input.GetMouseButton (0)) {
                if (EventSystem.current.IsPointerOverGameObject ())
                    return;
            
                float xAngle = Input.GetAxis ("Mouse Y") * Speed * 80;
                float yAngle = -Input.GetAxis ("Mouse X") * Speed * 80;
                float zAngle = 0;
            
                Cube.transform.Rotate (xAngle, yAngle, zAngle, Space.World);
            }
            #endif
        }
    }
}