using UnityEngine;
using System.Collections;

// Helper functions for animation, called by GameManager

public class AnimationC : MonoBehaviour {
	
	public GameManager gm;
	public GameObject target;
	
	void stopwalk()
	{
		gameObject.GetComponent<Animation>().CrossFade("idle");
	}
	
	void stopharvest()
	{
		gameObject.GetComponent<Animation>().CrossFade("idle");
		gm.TryPickup(target.name);
	}
	
	void startwalk()
	{
		gameObject.GetComponent<Animation>().CrossFade("walking");
	}
	
}
