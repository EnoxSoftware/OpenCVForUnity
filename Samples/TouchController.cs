using UnityEngine;
using System.Collections;
using System.Linq;

public class TouchController : MonoBehaviour
{
	

	public GameObject Cube;

	public float Speed = 0.01f;
	
	void Update()
	{

		int touchCount = Input.touchCount;
		
		if (touchCount == 1)
		{

			Touch t = Input.GetTouch(0);
			switch (t.phase)
			{
			case TouchPhase.Moved:
				

				float xAngle = t.deltaPosition.y * Speed * 10;
				float yAngle = -t.deltaPosition.x * Speed * 10;
				float zAngle = 0;
				

				Cube.transform.Rotate(xAngle, yAngle, zAngle, Space.World);
				
				break;
			}
			
		}
	}
}
