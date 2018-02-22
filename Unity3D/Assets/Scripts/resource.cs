using UnityEngine;
using System.Collections;

// This script is attached to resource items that can be picked up

public class resource : MonoBehaviour {

	public GameManager gm;
	
	void Start() {
			gm=GameObject.Find("Ground").GetComponent<GameManager>();
	}
	
	void OnMouseDown() {
		gm.HarvestAt(transform.position.x,transform.position.z);
		gm.target.GetComponent<AnimationC>().target=gameObject;
	}
	
	
}
