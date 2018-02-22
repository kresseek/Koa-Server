using UnityEngine;
using System.Collections;

// Any object that has this script attached will face the main camera
// Used in player names and chat text

public class lookatcamera : MonoBehaviour
{

private Transform target;

	void Start() {
		target=Camera.main.transform;
		GetComponent<Renderer>().material.color = Color.blue;
	}
	
	void Update() {
		
		transform.LookAt(transform.position + target.transform.rotation * Vector3.forward,  target.transform.rotation * Vector3.up);
		
	}

}