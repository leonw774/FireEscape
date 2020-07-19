using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Map : MonoBehaviour {

	public GameObject SmokeSprite;
	public Texture2D SmokeTexture;
	public Transform SmokeTrandform;
	private Vector3 MiddlePoint;
	private GameObject[] wallObjects;

	public const int Precision = 100;
	public float[,] Fires;
	public float[,] SmokeDensity;
	public bool[,] IsWall = new bool[100, 100];
	public int[] initFire;

	private int updateFPS;
	private float accumulatedDeltaTime;

	// Use this for initialization
	void Start () {
		// Init
		// find all walls: check if precision point is in wall
		wallObjects = GameObject.FindGameObjectsWithTag("Wall");
		SmokeTrandform = GameObject.Find("Smoke").GetComponent<Transform>().transform;
		MiddlePoint = new Vector3(Precision / 2.0f - 0.5f, Precision / 2.0f - 0.5f, (float)Precision);

		for (int x = 0; x < Precision; ++x) {
			for (int y = 0; y < Precision; ++y) {
				Vector3 v = PixelToWorldCoord(x, y);
				//Debug.Log(v);
				for (int i = 0; i < wallObjects.Length; ++i) {
					IsWall[x, y] = wallObjects[i].GetComponent<Collider>().bounds.Contains(v);
					if (IsWall[x, y]) {
						//Debug.Log(x + "," + y + " " + v.ToString() + " " + IsWall[x,y]);
						break;
					}
				}
			}
		}
		updateFPS = 10;
		accumulatedDeltaTime = 0.0f;
		Init();
		Draw();
	}
	
	public void Init() {
		Fires = new float[Precision, Precision];
		SmokeDensity = new float[Precision, Precision];
 		SmokeTexture = new Texture2D(Precision, Precision);
		SmokeTexture.filterMode = FilterMode.Point;
		GameObject.Find("Smoke").GetComponent<SpriteRenderer>().sprite = 
			Sprite.Create(SmokeTexture, new Rect(0.0f, 0.0f, SmokeTexture.width, SmokeTexture.height), new Vector2(0.5f, 0.5f));
		// init fire point
		int padding = 20;
		initFire = new int[2]{
			Random.Range((int)Precision/3, Precision-padding),
			Random.Range((int)Precision/3, Precision-padding)};
		while(IsWall[initFire[0], initFire[1]]) {
			initFire[0] = Random.Range((int)Precision/3, Precision-padding);
			initFire[1] = Random.Range((int)Precision/3, Precision-padding);
		}
		Debug.Log("initFire: " + initFire[0] + ", " + initFire[1]);
		Fires[initFire[0], initFire[1]] = 1.0f;
		SmokeDensity[initFire[0], initFire[1]] = 1.0f;
		// randomly add neighbor 8 
		for (int dx = -1; dx <= 1; ++dx) {
			for (int dy = -1; dy <= 1; ++dy) {
				if (initFire[0]+dx >= 0 && initFire[1]+dy >= 0 && initFire[0]+dx < Precision && initFire[1]+dy < Precision) {
					if (Random.value > 0.4f + (Mathf.Abs(dx) + Mathf.Abs(dx)) * 0.2f) {
						Fires[initFire[0]+dx, initFire[1]+dy] = 1.0f;
						SmokeDensity[initFire[0]+dx, initFire[1]+dy] = Random.value;
					}
				}
			}
		}
	}

	// Update is called once per frame
	void Update () {
		accumulatedDeltaTime += Time.deltaTime;
		if (accumulatedDeltaTime + Time.deltaTime > (1.0 / updateFPS)) {
			FireSpread();
			SmokeSpread();
			accumulatedDeltaTime = 0;
			Draw();
		}
	}

	void FireSpread() {
		float[,] preFires = new float[Precision, Precision];
		for (int x = 0; x < Precision; ++x) {
			for (int y = 0; y < Precision; ++y) {
				preFires[x, y] = Fires[x, y];
			}
		}
		for (int x = 0; x < Precision; ++x) {
			for (int y = 0; y < Precision; ++y) {
				float p = preFires[x ,y];
				if (p == 1.0f || IsWall[x, y]) {
					continue;
				}
				else {
					float spreadSlowness = 3.6f;
					float maxNeighbor = 0;
					for (int dx = -1; dx <= 1; ++dx) {
						for (int dy = -1; dy <= 1; ++dy) {
							if (x+dx >= 0 && y+dy >= 0 && x+dx < Precision && y+dy < Precision) {
								if (preFires[x+dx, y+dy] > 0) {
									if (dx + dy == 1 || dx + dy == -1) {
										maxNeighbor = Mathf.Max(maxNeighbor, preFires[x+dx, y+dy]);
									}
									else {
										maxNeighbor = Mathf.Max(maxNeighbor, 0.35336f * preFires[x+dx, y+dy]);
									}
								}
							}
						}
					}
					Fires[x, y] = (maxNeighbor + preFires[x, y] * spreadSlowness) / (spreadSlowness + Random.value * spreadSlowness);
					if (Fires[x, y] > 0.99f) {
						Fires[x, y] = 1.0f;
					}
				}
			}
		}
	}

	void SmokeSpread () {
		float[,] preSmokeDensity = new float[Precision, Precision];
		for (int x = 0; x < Precision; ++x) {
			for (int y = 0; y < Precision; ++y) {
				preSmokeDensity[x, y] = SmokeDensity[x, y];
			}
		}
		for (int x = 0; x < Precision; ++x) {
			for (int y = 0; y < Precision; ++y) {
				float p = preSmokeDensity[x, y];
				if (IsWall[x, y]) {
					continue;
				}
				else if (Fires[x, y] == 1) {
					SmokeDensity[x, y] *= 1f + Random.value;
				}
				else {
					float spreadSlowness;
					spreadSlowness = 4.0f * (Mathf.Sqrt(p)) + 1;
					float neighborWeight = spreadSlowness;
					float newDensity = p * spreadSlowness;
					for (int dx = -1; dx <= 1; ++dx) {
						for (int dy = -1; dy <= 1; ++dy) {
							if (x+dx >= 0 && y+dy >= 0 && x+dx < Precision && y+dy < Precision) {
								if (preSmokeDensity[x+dx, y+dy] > 0.0f) {
									if (dx + dy == 1 || dx + dy == -1) {
										newDensity += preSmokeDensity[x+dx, y+dy];
										neighborWeight += 1.0f;
									}
									else {
										newDensity += preSmokeDensity[x+dx, y+dy] * 0.35336f;
										neighborWeight += 0.35336f;
									}
								}
							}
						}
					}
					if (neighborWeight > spreadSlowness) {
						SmokeDensity[x, y] = (newDensity * (0.81f + Random.value * 0.4f)) / (float) neighborWeight;
						if (SmokeDensity[x, y] > 1)
							SmokeDensity[x, y] = 1;
						else if (SmokeDensity[x, y] < 0.05f)
							SmokeDensity[x, y] = 0.0f;
					}
				}
			}
		}
	}

	void Draw () {
		for (int x = 0; x < Precision; ++x) {
			for (int y = 0; y < Precision; ++y) {
				Color color;
				if (Fires[x, y] == 1.0f) {
					color = new Color(1.0f, 0.1f, 0.1f, 0.8f);
				}
				else {
					color = new Color(0.05f, 0.05f, 0.05f, ((int)(SmokeDensity[x, y] * 10) / 10.0f) * 0.8f);
					//color = new Color(0.05f, 0.05f, 0.05f, SmokeDensity[x, y]);
				}
				if (IsWall[x,y]) color = Color.cyan;
				SmokeTexture.SetPixel(x, y, color);
			}
		}
		SmokeTexture.Apply();
	}

	public Vector2 WorldCoordToPixel(Vector3 worldCoord) {
		Vector3 inverseScale = new Vector3(
			1 / SmokeTrandform.localScale.x,
			1 / SmokeTrandform.localScale.y,
			0);
		Vector3 v = Vector3.Scale((Quaternion.Euler(-90, 0, 0) * worldCoord), inverseScale);
		v = (v * Precision) + MiddlePoint;
		//Debug.Log(v);
		return new Vector2(v.x, v.y);
	}

	public Vector3 PixelToWorldCoord(float x, float y) {
		Vector3 v = (new Vector3(x, y, 0.0f) - MiddlePoint) / Precision;
		v = Quaternion.Euler(90, 0, 0) * Vector3.Scale(v, SmokeTrandform.localScale);
		return v;
	}

	public bool IsInWall(Vector3 worldPos) {
		for (int i = 0; i < wallObjects.Length; ++i) {
			if (wallObjects[i].GetComponent<Collider>().bounds.Contains(worldPos)) {
				return true;
			}
		}
		return false;
	}
}
