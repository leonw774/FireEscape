using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Person {
	public GameObject obj;
	public Rigidbody rigid;
	public Transform transform;
	public MeshRenderer renderer;
	public bool followPlayer;
	public Vector3 nextVelocity;
	public Person(GameObject _obj) {
		obj = _obj;
		rigid = obj.GetComponent<Rigidbody>();
		rigid.velocity = Vector3.zero;
		nextVelocity = new Vector3(Random.value * 0.1f, 0.0f, Random.value * 0.1f);
		renderer = obj.GetComponent<MeshRenderer>();
		renderer.enabled = true;
		renderer.material.color = Color.green;
		transform = obj.GetComponent<Transform>();
		followPlayer = false;
	}
}

public class Crowds : MonoBehaviour {

	public GameObject PrototypePerson;
	public Transform PlayerTransform;
	public List<Person> CrowdsList;
	public int crowdsNum = 30;
	private int padding = 10;
	private Map map;

	// Use this for initialization
	void Start () {
		map = GameObject.Find("Smoke").GetComponent<Map>();
		PrototypePerson = GameObject.Find("Prototype Person");
		PlayerTransform = GameObject.Find("Player").GetComponent<Transform>();
		CrowdsList = new List<Person>();

		for (int i = 0; i < crowdsNum; i++) {
			int x = 0, y = 0;
			while(map.IsWall[x, y] || x <= 0) {
				x = Random.Range(padding, Map.Precision-padding);
				y = Random.Range(padding, Map.Precision-padding);
				if (Mathf.Abs(map.initFire[0] - x) + Mathf.Abs(map.initFire[1] - y) <= 3)
					continue;
			}
			Vector3 pos = map.PixelToWorldCoord(x, y) + Vector3.up * 0.4f;
			//Debug.Log(pos);
			CrowdsList.Add(new Person(Instantiate(PrototypePerson, pos, Quaternion.identity)));
		}
	}

	public void Init() {
		for (int i = 0; i < crowdsNum; i++) {
			int x = 0, y = 0;
			while(map.IsWall[x, y] || x <= 0) {
				x = Random.Range(padding, Map.Precision-padding);
				y = Random.Range(padding, Map.Precision-padding);
			}
			Vector3 pos = map.PixelToWorldCoord(x, y) + Vector3.up * 0.4f;
			//Debug.Log(pos);
			CrowdsList[i].transform.localPosition = pos;
			CrowdsList[i].rigid.velocity = new Vector3(Random.value*0.2f-0.1f, 0.0f, Random.value*0.2f-0.1f);
			CrowdsList[i].followPlayer = false;
			CrowdsList[i].renderer.material.color = Color.green;
		}
	}
	
	// Update is called once per frame
	void Update () {

	}
}
