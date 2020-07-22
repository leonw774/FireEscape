using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Control : MonoBehaviour {

	public Rigidbody PlayerRigid;
	public Transform PlayerTransform;
	public MeshRenderer PlayerRenderer;
	public float maxspeed;
	public float speed;
	public float smokeSpReduceRate;
	public float maxhp;
	public float hp;
	public bool endgameflag = false;
	public float minFollowDistance;
	public float crowdsMaxSpeed;

	private int hpUpdateFPS;
	private float accumulatedDeltaTime;

	private Map map;
	private Crowds crowds;
	private Text loseText, winText, winCount;
	private SpriteRenderer TextBackground;

	// Use this for initialization
	void Start () {
		map = GameObject.Find("Smoke").GetComponent<Map>();
		crowds = GameObject.Find("Crowds Control").GetComponent<Crowds>();
		loseText = GameObject.Find("LoseText").GetComponent<Text>();
		winText = GameObject.Find("WinText").GetComponent<Text>();
		winCount = GameObject.Find("WinCount").GetComponent<Text>();
		TextBackground = GameObject.Find("Text Background").GetComponent<SpriteRenderer>();
		PlayerRigid = GetComponent<Rigidbody>();
		PlayerTransform = GetComponent<Transform>();
		PlayerRenderer = GetComponent<MeshRenderer>();
		maxhp = 200f;
		maxspeed = 2.5f;
		smokeSpReduceRate = 0.67f;
		minFollowDistance = 1.0f;
		crowdsMaxSpeed = 8.0f;
		hpUpdateFPS = 5;
		accumulatedDeltaTime = 0;
		Init();
	}
	
	void Init() {
		hp = maxhp;
		speed = maxspeed;
		PlayerTransform.position = new Vector3(-8.0f, 1.0f, -9.0f);
		winText.enabled = false;
		winCount.enabled = false;
		loseText.enabled = false;
		TextBackground.enabled = false;
		map.Init();
		crowds.Init();
	}

	// Update is called once per frame
	void FixedUpdate () {
		UpdateSpeed();
		if (endgameflag) {
			if (Input.GetKey(KeyCode.Space)) {
				Init();
				endgameflag = false;
			}
			PlayerRigid.velocity = PlayerRigid.velocity * 0.9f;
		}
		else {
			if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) {
				PlayerRigid.velocity += Vector3.forward * speed;
			}
			if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) {
				PlayerRigid.velocity += Vector3.left * speed;
			}
			if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) {
				PlayerRigid.velocity += Vector3.back * speed;
			}
			if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) {
				PlayerRigid.velocity += Vector3.right * speed;
			}
			PlayerRigid.velocity = PlayerRigid.velocity * 0.667f;
		}
		accumulatedDeltaTime += Time.deltaTime;
		if (accumulatedDeltaTime + Time.deltaTime > (1.0 / hpUpdateFPS)) {
			UpdateHP();
		}
		UpdateCrowds();
		if (endgameflag) {
			UpdateWinCount();
		}
		else {
			ShowText();
		}
		
	}
	
	void UpdateSpeed() {
		Vector2 pixel = map.WorldCoordToPixel(PlayerTransform.position);
		if (pixel.x > 0 && pixel.y > 0 && pixel.x < Map.Precision && pixel.y < Map.Precision) {
			speed = maxspeed * ((1.0f - smokeSpReduceRate) + smokeSpReduceRate * (1.0f - map.SmokeDensity[(int)pixel.x, (int)pixel.y]));
		}
		else {
			speed = maxspeed;
		}
	}

	void UpdateHP() {
		Vector2 pixel = map.WorldCoordToPixel(PlayerTransform.position);
		if (pixel.x > 0 && pixel.y > 0 && pixel.x < Map.Precision && pixel.y < Map.Precision) {
			hp -= map.SmokeDensity[(int)pixel.x, (int)pixel.y] * 0.5f;
			if (map.Fires[(int)pixel.x, (int)pixel.y] == 1.0f) {
				hp--;
			}
		}
		PlayerRenderer.material.color = (hp * Color.blue + (maxhp - hp) * Color.red) / maxhp;
	}

	void ShowText() {
		if (hp <= 0) {
			// lose
			endgameflag = true;
			loseText.enabled = true;
			TextBackground.enabled = true;
		}
		Vector2 pixel = map.WorldCoordToPixel(PlayerTransform.position);
		if (pixel.x < 0 || pixel.y < 0 || pixel.x > Map.Precision || pixel.y > Map.Precision) {
			// win
			endgameflag = true;
			winText.enabled = true;
			winCount.enabled = true;
			TextBackground.enabled = true;
		}
	}

	void UpdateWinCount() {
		int crowdCount = 0;
		foreach (var person in crowds.CrowdsList) {
			Vector2 pixel = map.WorldCoordToPixel(person.transform.position);
			if (pixel.x < 0 || pixel.y < 0 || pixel.x > Map.Precision || pixel.y > Map.Precision) {
				crowdCount++;
			}
		}
		winCount.text = "You save " + crowdCount + " out of " + crowds.crowdsNum;
	}
	
	void UpdateCrowds() {
		foreach (var person in crowds.CrowdsList) {
			if (person.followPlayer) {
				person.rigid.velocity = person.nextVelocity;
				Vector3 targetPos = PlayerTransform.position + new Vector3(Random.value*6f-3f, 0f, Random.value*6f-3f);
				person.nextVelocity = 0.75f * person.rigid.velocity + (0.75f + (Random.value-0.5f)) * (targetPos - person.transform.position);
				if (person.nextVelocity.magnitude > crowdsMaxSpeed) {
					person.nextVelocity = person.nextVelocity.normalized * crowdsMaxSpeed;
				}
				if (person.rigid.velocity.magnitude < 0.5f) {
					person.rigid.velocity = Vector3.zero;
				}

				person.renderer.material.color = PlayerRenderer.material.color;
			}
			else {
				Vector3 from = PlayerTransform.position;
				Vector3 to = person.transform.position;
				from.y = 1f;
				to.y = 1f;
				if (Vector3.Distance(from, to) < minFollowDistance) {
					person.followPlayer = true;
				}
			}
		}
	}
}
