var originalColor;


var colorStart : Color = Color.red;
var colorEnd : Color = Color.white;
var duration : float = 1.0;

// Attach this script to a gameobject to make it "glow" when mouse is on top of it
// the object must have a collider for the script to work

function Start() {
		originalColor = GetComponent.<Renderer>().material.color;	
}

function OnMouseOver() {

	var lerp : float = Mathf.PingPong (Time.time, duration) / duration;
	GetComponent.<Renderer>().material.color = Color.Lerp (colorStart, colorEnd, lerp);

}

function OnMouseExit() {
	GetComponent.<Renderer>().material.color = originalColor;
}