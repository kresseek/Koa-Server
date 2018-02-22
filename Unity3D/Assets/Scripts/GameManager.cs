//-----------------------------------------------------------------------------------------------------------------
// 
//	Mushroom Example
//	Created by : Luis Filipe (filipe@seines.pt)
//	Dec 2010
//
//	Source code in this example is in the public domain.
//  The naruto character model in this demo is copyrighted by Ben Mathis.
//  See Assets/Models/naruto.txt for more details
//
//-----------------------------------------------------------------------------------------------------------------

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PlayerIOClient;

public class ChatEntry {
	public string text = "";
	public bool mine = true;
}

public class GameManager : MonoBehaviour {

	public GameObject target;
	public GameObject PlayerPrefab;
	public GameObject ToadPrefab;
	private Connection pioconnection;
	private List<Message> msgList = new List<Message>(); //  Messsage queue implementation
	private bool joinedroom = false;

	// UI stuff
	private Vector2 scrollPosition;
	private ArrayList entries = new ArrayList();
	private string inputField = "";
	private Rect window = new Rect(10, 10, 300, 150);
	private int toadspicked = 0;
	private string infomsg = "";

	void Start() {
		Application.runInBackground = true;

		// Create a random userid 
		System.Random random = new System.Random();
		string userid = "Guest" + random.Next(0, 10000);

		Debug.Log("Starting");

		PlayerIO.Authenticate(
			"koa-iwxmkotfj0spsdi4dzvnqw",            //Your game id
			"public",                               //Your connection id
			new Dictionary<string, string> {        //Authentication arguments
				{ "userId", userid },
			},
			null,                                   //PlayerInsight segments
			delegate (Client client) {
				Debug.Log("Successfully connected to Player.IO");
				infomsg = "Successfully connected to Player.IO";

				target.transform.Find("NameTag").GetComponent<TextMesh>().text = userid;
				target.transform.name = userid;

				Debug.Log("Create ServerEndpoint");
				// Comment out the line below to use the live servers instead of your development server
				client.Multiplayer.DevelopmentServer = new ServerEndpoint("localhost", 8184);

				Debug.Log("CreateJoinRoom");
				//Create or join the room 
				client.Multiplayer.CreateJoinRoom(
					"UnityDemoRoom",                    //Room id. If set to null a random roomid is used
					"UnityMushrooms",                   //The room type started on the server
					true,                               //Should the room be visible in the lobby?
					null,
					null,
					delegate (Connection connection) {
						Debug.Log("Joined Room.");
						infomsg = "Joined Room.";
						// We successfully joined a room so set up the message handler
						pioconnection = connection;
						pioconnection.OnMessage += handlemessage;
						joinedroom = true;
					},
					delegate (PlayerIOError error) {
						Debug.Log("Error Joining Room: " + error.ToString());
						infomsg = error.ToString();
					}
				);
			},
			delegate (PlayerIOError error) {
				Debug.Log("Error connecting: " + error.ToString());
				infomsg = error.ToString();
			}
		);

	}

	void handlemessage(object sender, Message m) {
		msgList.Add(m);
	}

	void FixedUpdate() {
		// process message queue
		foreach (Message m in msgList) {
			switch (m.Type) {
			case "PlayerJoined":
				GameObject newplayer = GameObject.Instantiate(target) as GameObject;
				newplayer.transform.position = new Vector3(m.GetFloat(1), 0, m.GetFloat(2));
				newplayer.name = m.GetString(0);
				newplayer.transform.Find("NameTag").GetComponent<TextMesh>().text = m.GetString(0);
				break;
			case "ReadyToPlay":
				
				break;
			case "DeckPicked":  // memberitahu ada player yg mengambil deck

				break;
			case "TongPicked":

				break;
			case "Buang":
				
				break;
			case "ErrorBuang":
				
				break;
			case "Harvest":
				GameObject hvplayer = GameObject.Find(m.GetString(0));
				hvplayer.transform.LookAt(new Vector3(m.GetFloat(1), .5f, m.GetFloat(2)));

				// set transform x axis to 0, so the character will be facing forward
				hvplayer.transform.eulerAngles = new Vector3(0, hvplayer.transform.eulerAngles.y, hvplayer.transform.eulerAngles.z);

				// get distance between current position and target position,
				// we'll need to value to know how much the tween will last
				float distance = Vector3.Distance(hvplayer.transform.position, new Vector3(m.GetFloat(1), 0, m.GetFloat(2)));
				// create a tween between current and target position
				iTween.MoveTo(hvplayer, iTween.Hash("x", m.GetFloat(1), "z", m.GetFloat(2), "onstart", "startwalk", "oncomplete", "stopharvest", "time", distance, "delay", 0, "easetype", iTween.EaseType.linear));
				break;
			case "Picked":
				// remove the object when it's picked up
				GameObject removetoad = GameObject.Find("Toad" + m.GetInt(0));
				Destroy(removetoad);

				break;
			case "Chat":
				if (m.GetString(0) != "Server") {
					GameObject chatplayer = GameObject.Find(m.GetString(0));
					chatplayer.transform.Find("Chat").GetComponent<TextMesh>().text = m.GetString(1);
					chatplayer.transform.Find("Chat").GetComponent<MeshRenderer>().material.color = Color.white;
					chatplayer.transform.Find("Chat").GetComponent<chatclear>().lastupdate = Time.time;
				}
				ChatText(m.GetString(0) + " says: " + m.GetString(1), false);
				break;
			case "PlayerLeft":
				// remove characters from the scene when they leave
				GameObject playerd = GameObject.Find(m.GetString(0));
				Destroy(playerd);
				break;
			case "Toad":
				// adds a toadstool to the scene
				GameObject newtoad = GameObject.Instantiate(ToadPrefab) as GameObject;
				newtoad.transform.position = new Vector3(m.GetFloat(1), 0.1f, m.GetFloat(2));
				newtoad.name = "Toad" + m.GetInt(0);
				break;
			case "ToadCount":
				// updates how many toads have been picked up by the player
				toadspicked = m.GetInt(0);
				break;
			}
		}

		// clear message queue after it's been processed
		msgList.Clear();
	}

	void OnMouseDown() {
		// this function responds to mouse clicks on the ground
		// it will send a move request to the server

		// ignore user input if we're not inside a room
		if (!joinedroom) {
			return;
		}

		Vector3 targetPosition = new Vector3(0, 0, 0);

		var playerPlane = new Plane(Vector3.up, target.transform.position);
		var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		var hitdist = 0.0f;
		if (playerPlane.Raycast(ray, out hitdist)) {
			targetPosition = ray.GetPoint(hitdist);
			pioconnection.Send("Move", targetPosition.x, targetPosition.z);
		}
	}


	void OnGUI() {
		window = GUI.Window(1, window, GlobalChatWindow, "Chat");
		GUI.Label(new Rect(10, 160, 150, 20), "Toadstools picked: " + toadspicked);
		if (infomsg != "") {
			GUI.Label(new Rect(10, 180, Screen.width, 20), infomsg);
		}
	}

	public void HarvestAt(float posx, float posz) {
		pioconnection.Send("MoveHarvest", posx, posz);
	}

	public void TryPickup(string id) {
		pioconnection.Send("Pickup", id);
	}

	void GlobalChatWindow(int id) {

		if (!joinedroom) {
			return;
		}

		GUI.FocusControl("Chat input field");

		// Begin a scroll view. All rects are calculated automatically - 
		// it will use up any available screen space and make sure contents flow correctly.
		// This is kept small with the last two parameters to force scrollbars to appear.
		scrollPosition = GUILayout.BeginScrollView(scrollPosition);

		foreach (ChatEntry entry in entries) {
			GUILayout.BeginHorizontal();
			if (!entry.mine) {
				GUILayout.Label(entry.text);
			} else {
				GUI.contentColor = Color.yellow;
				GUILayout.Label(entry.text);
				GUI.contentColor = Color.white;
			}

			GUILayout.EndHorizontal();
			GUILayout.Space(3);

		}
		// End the scrollview we began above.
		GUILayout.EndScrollView();

		if (Event.current.type == EventType.keyDown && Event.current.keyCode == KeyCode.Return && inputField.Length > 0) {

			GameObject chatplayer = GameObject.Find(target.transform.name);
			chatplayer.transform.Find("Chat").GetComponent<TextMesh>().text = inputField;
			chatplayer.transform.Find("Chat").GetComponent<MeshRenderer>().material.color = Color.white;
			chatplayer.transform.Find("Chat").GetComponent<chatclear>().lastupdate = Time.time;

			ChatText(target.transform.name + " says: " + inputField, true);
			pioconnection.Send("Chat", inputField);
			inputField = "";
		}
		GUI.SetNextControlName("Chat input field");
		inputField = GUILayout.TextField(inputField);

		GUI.DragWindow();
	}


	void ChatText(string str, bool own) {
		var entry = new ChatEntry();
		entry.text = str;
		entry.mine = own;

		entries.Add(entry);

		if (entries.Count > 50)
			entries.RemoveAt(0);

		scrollPosition.y = 1000000;
	}

	// #kresna - send to server
	void readyToPlay(/*int noUrut*/){
		pioconnection.Send ("ReadyToPlay" /*, new byte[]{noUrut}*/);
	}
	void tryGetCard(bool isDeck /*, int noUrut*/){
		pioconnection.Send ("AmbilKartu", isDeck);
	}
	void tryDropCard(int idCard){
		pioconnection.Send ("Buang", new byte[]{(byte)idCard});
	}
	void lookBin(int noUrut){
		pioconnection.Send ("LihatBin", new byte[]{(byte)noUrut});
	}
}
