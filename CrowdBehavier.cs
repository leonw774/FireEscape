using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EventEnum {
	None, Social, Smoke, Fire, Alarm
};
public enum PersonBehaviour {
	Idle, Social, Stroll, Panic, Escape
};

public class Information {
	public EventEnum Event;
	public Vector3 Direction;
	public Information() {
		Event = EventEnum.None;
	}
	public Information(EventEnum _event) {
		Event = _event;
	}
	public Information(EventEnum _event, Vector3 _direction) {
		Event = _event;
		Direction = _direction;
	}
};

public class Person {

	public PersonBehaviour State;
	public List<Information> Memory;
	public Person socialPartner;
	public Vector3 WalkTarget;
	public float Speed;
	public float pre_dx = 0, pre_dy = 0;

	public GameObject obj;
	public Transform objT;
	public MeshRenderer objRenderer;

	public Person (GameObject _obj) {
		obj = _obj;
		objT = obj.GetComponent<Transform>();
		objRenderer = obj.GetComponent<MeshRenderer>();
		objRenderer.enabled = true;

		float r = Random.value;
		SetState(PersonBehaviour.Idle);
		Memory = new List<Information>();
		socialPartner = null;
		WalkTarget = Vector3.zero;
	}

	public void SetState(PersonBehaviour newState) {
		State = newState;
		switch(newState) {
			case PersonBehaviour.Idle:
				objRenderer.material.color = Color.blue;
				Speed = 1f;
				break;
			case PersonBehaviour.Social:
				objRenderer.material.color = Color.cyan;
				Speed = 2f;
				break;
			case PersonBehaviour.Stroll:
				objRenderer.material.color = Color.green;
				Speed = 2f;
				break;
			case PersonBehaviour.Panic:
				objRenderer.material.color = Color.yellow;
				Speed = 5f;
				break;
			case PersonBehaviour.Escape:
				objRenderer.material.color = Color.red;
				Speed = 6f;
				break;
			default:
				objRenderer.material.color = Color.grey;
				Speed = 1f;
				break;
		}
	}

	public void Move(Vector3 dPos) {
		objT.Translate(dPos);
	}

	public void MoveToWalkTarget(float delta) {
		objT.Translate((WalkTarget - objT.localPosition).normalized * Speed * delta);
	}
}

public class CrowdBehavier : MonoBehaviour {

	public GameObject PrototypePerson;
	public List<Person> Crowds;
	public Vector2[] CrowdingArea;
	public int Precision;

	private WallMap wallMap;
	private SmokeBehavier smokeMap;
	private int padding;
	private float minObserveDistance;
	private float accumulatedDeltaTime;
	private float updateFPS;

	// Use this for initialization
	void Start () {
		// init
		wallMap = GameObject.Find("Stage").GetComponent<WallMap>();
		smokeMap = GameObject.Find("Smoke").GetComponent<SmokeBehavier>();
		padding = 10;
		minObserveDistance = 3.0f;
		accumulatedDeltaTime = 0;
		updateFPS = 30;

		Precision = WallMap.Precision;
		PrototypePerson = GameObject.Find("Prototype Person");
		CrowdingArea = new Vector2[Random.Range(3, 8)];
		Crowds = new List<Person>();
		// find crowding area

		for (int i  = 0; i < CrowdingArea.Length; i++) {
			int ca_x = 0, ca_y = 0;
			while(wallMap.IsWall[ca_x, ca_y] || ca_x <= 0) {
				ca_x = Random.Range(padding, Precision-padding);
				ca_y = Random.Range(padding, Precision-padding);
			}
			CrowdingArea[i] = new Vector2(ca_x, ca_x);
			// spread people around crowding area
			int crowndNum = 4 + Random.Range(-2, 6);
			for (int n = 0; n < crowndNum; n++) {
				float randx = 0, randy = 0;
				while(wallMap.IsWall[(int) (randx + 0.5), (int) (randy + 0.5)] || randx <= 0) {
					randx = Mathf.Clamp(ca_x + Random.value * 10, 0, Precision - 1);
					randy = Mathf.Clamp(ca_y + Random.value * 10, 0, Precision - 1);
				}
				Vector3 pos = wallMap.PixelToWorldCoord(randx, randy) + Vector3.up;
				Crowds.Add(new Person(Instantiate(PrototypePerson, pos, Quaternion.identity)));
			}
		}

	}

	float Gaussian(float mean, float sigma) {
		float rand1 = Random.Range(0.0f, 1.0f);
		float rand2 = Random.Range(0.0f, 1.0f);
	
		float n = Mathf.Sqrt(-2.0f * Mathf.Log(rand1)) * Mathf.Cos((2.0f * Mathf.PI) * rand2);
		return mean + sigma * n;
	}

	float Gaussian(float mean, float sigma, float min, float max) {
		float rand1 = Random.Range(0.0f, 1.0f);
		float rand2 = Random.Range(0.0f, 1.0f);
	
		float n = Mathf.Sqrt(-2.0f * Mathf.Log(rand1)) * Mathf.Cos((2.0f * Mathf.PI) * rand2);
		return Mathf.Clamp(mean + sigma * n, min, max);
	}
	// Update is called once per frame
	void Update () {
		accumulatedDeltaTime += Time.deltaTime;
		if (accumulatedDeltaTime + Time.deltaTime > (1.0 / updateFPS)) {
			UpdateInformation();
			for (int n = 0; n < Crowds.Count; n++)
			{
				UpdateState(Crowds[n]);
				UpdateWalkTarget(Crowds[n]);
				// Than try to move to target
				Crowds[n].MoveToWalkTarget(Time.deltaTime);
			}
			accumulatedDeltaTime = 0;
		}
	}

	void UpdateInformation() {
		for (int n = 0; n < Crowds.Count; n++)
		{
			bool memoryAdded = false;
			// Observe
			// check if can see Fire
			for (int x = 0; x < Precision; ++x) {
				for (int y = 0; y < Precision; ++y) {
					if (smokeMap.Fires[x, y] == 1.0f) {
						Vector3 from = Crowds[n].objT.localPosition;
						from.y = 0;
						Vector3 to = wallMap.PixelToWorldCoord(x, y);
						if (Vector3.Distance(from, to) < minObserveDistance) {
							//Debug.Log("Fire check:" + from + to);
							if (!Physics.Linecast(from, to)) {
								Crowds[n].Memory.Add(new Information(EventEnum.Fire, to-from));
								x = Precision;
								memoryAdded = true;
								break;
							}
						}
					}
				}
			}
			if (memoryAdded) break;
			// check if Smoke is arround
			Vector2 pixel = wallMap.WorldCoordToPixel(Crowds[n].objT.localPosition);
			int px = (int)pixel.x;
			int py = (int)pixel.y;
			for (int dx = -1; dx <= 1; ++dx) {
				for (int dy = -1; dy <= 1; ++dy) {
					if (px+dx >= 0 && py+dy >= 0 && px+dx < Precision && py+dy < Precision) {
						if (smokeMap.SmokeDensity[px+dx, py+dy] > 0.1f) {
							Vector3 to = wallMap.PixelToWorldCoord(px+dx, py+dy);
							Crowds[n].Memory.Add(new Information(EventEnum.Smoke, to-Crowds[n].objT.localPosition));
							dx = 1;
							memoryAdded = true;
							break;
						}
					}
				}
			}
			if (memoryAdded) break;
			// Try receive Alarm: if there are panic or escaping people around, it receive Alarm
			for (int m = 0; m < Crowds.Count; m++) {
				if (n != m) {
					if (Crowds[m].State == PersonBehaviour.Escape || Crowds[m].State == PersonBehaviour.Panic) {
						Vector3 from = Crowds[m].objT.localPosition;
						Vector3 to = Crowds[n].objT.localPosition;
						if (!Physics.Linecast(from, to)) {
							if (Vector3.Distance(from, to) < minObserveDistance) {
								Crowds[n].Memory.Add(new Information(EventEnum.Alarm, to-from));
								m = Crowds.Count;
								memoryAdded = true;
								break;
							}
						}
					}
				}
			}
			if (memoryAdded) break;
			if (Crowds[n].State == PersonBehaviour.Social) Crowds[n].Memory.Add(new Information(EventEnum.Social));
			else Crowds[n].Memory.Add(new Information(EventEnum.None));
		}
	}

	void UpdateState(Person person) {
		// Fire or Smoke in Memory
		if (person.Memory.Exists(x => x.Event == EventEnum.Smoke || x.Event == EventEnum.Fire)) {
			EventEnum lastevent = person.Memory[person.Memory.Count - 1].Event;
			if (lastevent != EventEnum.Smoke && lastevent != EventEnum.Fire) {
				person.SetState(PersonBehaviour.Panic);
			}
			else {
				person.SetState(PersonBehaviour.Escape);
			}
		}
		// Alarm Information is in Memory
		else if (person.Memory.Exists(x => x.Event == EventEnum.Alarm)) {
			person.SetState(PersonBehaviour.Panic);
		}
		else {
			if (Vector3.Distance(person.WalkTarget, person.objT.localPosition) < 0.1f) {
				person.WalkTarget = Vector3.zero;
				float rand = Random.value;
				if (person.State == PersonBehaviour.Idle) {
					if (rand > 0.2) {
						if (FindSocialPartner(person)) {
							person.SetState(PersonBehaviour.Social);
						}
					}
					else {
						person.SetState(PersonBehaviour.Stroll);
					}
				}
				else if (person.State == PersonBehaviour.Social) {
					if (rand > 0.7) {
						person.SetState(PersonBehaviour.Idle);
						person.socialPartner.SetState(PersonBehaviour.Idle);
					}
					else if (rand > 0.5) {
						person.SetState(PersonBehaviour.Stroll);
						person.socialPartner.SetState(PersonBehaviour.Stroll);
					}
				}
				else if (person.State == PersonBehaviour.Stroll) {
					if (rand > 0.5) {
						if (FindSocialPartner(person)) {
							person.SetState(PersonBehaviour.Social);
						}
						else {
							person.SetState(PersonBehaviour.Idle);
						}
					}
					else {
						person.SetState(PersonBehaviour.Idle);
					}
				}
			}
		}
	}

	bool FindSocialPartner(Person person) {
		int tryLimit = 20, tryCount = 0;
		for (; tryCount < tryLimit; tryCount++) {
			Person candidate = Crowds[Random.Range(0, Crowds.Count)];
			if (candidate != person) {
				if (!Physics.Linecast(person.objT.localPosition, candidate.objT.localPosition)) {
					candidate.SetState(PersonBehaviour.Social);
					person.socialPartner = candidate;
					candidate.socialPartner = person;
					return true;
				}
			}
		}
		return false;
	}

	void UpdateWalkTarget(Person person) {
		//Debug.Log("Move");
		Vector2 randCircle;
		Vector3 randDir;
		RaycastHit rh;
		// First find WalkTarget according to state
		if (person.WalkTarget == Vector3.zero ||
			person.State == PersonBehaviour.Social ||
			Vector3.Distance(person.WalkTarget, person.objT.localPosition) < 0.1f) {
			switch(person.State) {
				case PersonBehaviour.Idle:
					do {
						randCircle = Random.insideUnitCircle * 0.1f;
						person.WalkTarget = person.objT.localPosition + new Vector3(randCircle.x, 0f, randCircle.y);
					} while (wallMap.IsInWall(person.WalkTarget));
					break;
				case PersonBehaviour.Social:
					//find social partner
					do {
						randCircle = Random.insideUnitCircle * 0.25f;
						person.WalkTarget = person.socialPartner.objT.localPosition + new Vector3(randCircle.x, 0f, randCircle.y);
					} while (wallMap.IsInWall(person.WalkTarget));
					break;
				case PersonBehaviour.Stroll:
					randCircle = Random.insideUnitCircle;
					randDir = new Vector3(randCircle.x, 0, randCircle.y);
					if (Physics.Raycast(person.objT.localPosition, randDir, out rh)) {
						person.WalkTarget = person.objT.localPosition + randDir * (rh.distance * Random.value * 0.5f);
					}
					else {
						person.WalkTarget = person.objT.localPosition + randDir;
					}
					break;
				case PersonBehaviour.Panic:
					// look at directions around and choose the longer way to go
					randCircle = Random.insideUnitCircle;
					randDir = new Vector3(randCircle.x, 0, randCircle.y).normalized;
					Physics.Raycast(person.objT.localPosition, randDir, out rh);
					person.WalkTarget = person.objT.localPosition + randDir * (rh.distance * Random.value * 0.25f);
					break;
				
				case PersonBehaviour.Escape:
					// go opposite direction of fire
					Information dangerInfo = person.Memory.Find(x => x.Event == EventEnum.Smoke || x.Event == EventEnum.Fire);
					Vector3 oppositeDangerDir = -dangerInfo.Direction.normalized;
					person.WalkTarget = person.objT.localPosition + oppositeDangerDir;
					break;
				default:
					break;
			}
		}
	}
}
