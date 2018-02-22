using UnityEngine;
using System.Collections;

// This script is attached to a camera and will make it follow a target in isometric style

public class followcam : MonoBehaviour {

	public GameObject target;
	 
void LateUpdate()
{

	transform.position=new Vector3(target.transform.position.x+2,3,target.transform.position.z+2);
	transform.LookAt(target.transform.position);

}


}