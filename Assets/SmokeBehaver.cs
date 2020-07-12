using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SmokeBehaver : MonoBehaviour {

	public GameObject SmokeSprite;
	public Texture2D SmokeTexture;
	public int Precision;
	public float[,] Fires;
	public float[,] SmokeDensity;
	public bool[,] IsWall;
	private int updateFPS;
	private float accumulatedDeltaTime;

	// Use this for initialization
	void Start () {
		// Init
		Precision = 100;
		Fires = new float[Precision, Precision];
		SmokeDensity = new float[Precision, Precision];
		IsWall = new bool[Precision, Precision];
 		SmokeTexture = new Texture2D(Precision, Precision);
		updateFPS = 1;
		accumulatedDeltaTime = 0.0f;
		
		// find all walls
		GameObject[] wallObjects = GameObject.FindGameObjectsWithTag("Wall");
		// check if precision point is in wall
		Transform t = GetComponent<Transform>().transform;
		// y-z exchange because smoke sprite is rotate 90 degree on x axis
		Vector3 middle = new Vector3(Precision / 2.0f - 0.5f, Precision / 2.0f - 0.5f, (float)Precision);
		for (int x = 0; x < Precision; ++x) {
			for (int y = 0; y < Precision; ++y) {
				Vector3 v = (new Vector3(x, y, 0.0f) - middle) / Precision;
				v = Vector3.Scale(v, t.localScale);
				v = Quaternion.Euler(90, 0, 0) * v;
				for (int i = 0; i < wallObjects.Length; ++i) {
					IsWall[x, y] = wallObjects[i].GetComponent<Collider>().bounds.Contains(v);
					if (IsWall[x, y]) {
						//Debug.Log(x + "," + y + " " + v.ToString() + " " + IsWall[x,y]);
						break;
					}
				}
			}
		}
		// init fire point
		int[] initFire = new int[2]{Random.Range(0, Precision), Random.Range(0, Precision)};
		while(IsWall[initFire[0], initFire[1]]) {
			initFire[0] = Random.Range(0, Precision);
			initFire[1] = Random.Range(0, Precision);
		}
		Debug.Log("initFire: " + initFire[0] + ", " + initFire[1]);
		Fires[initFire[0], initFire[1]] = 1.0f;
		SmokeDensity[initFire[0], initFire[1]] = 1.0f;
		Draw();
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
					int spreadSlowness = 4;
					float maxNeighbor = 0;
					for (int dx = -1; dx <= 1; ++dx) {
						for (int dy = -1; dy <= 1; ++dy) {
							if (x+dx >= 0 && y+dy >= 0 && x+dx < Precision && y+dy < Precision) {
								if (preFires[x+dx, y+dy] > 0) {
									if (dx + dy == 1 || dx + dy == -1) {
										maxNeighbor = Mathf.Max(maxNeighbor, preFires[x+dx, y+dy]);
									}
									else {
										maxNeighbor = Mathf.Max(maxNeighbor, 0.5f * preFires[x+dx, y+dy]);
									}
								}
							}
						}
					}
					Fires[x, y] = (maxNeighbor + preFires[x, y] * spreadSlowness) / (spreadSlowness + Random.value * 3.0f);
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
				if (p == 1.0f || IsWall[x, y]) {
					continue;
				}
				else if (Fires[x, y] == 1) {
					SmokeDensity[x, y] *= 1f + Random.value;
				}
				else {
					float spreadSlowness = 8.0f * (Mathf.Sqrt(p)) + 1;
					int neighborCount = (int)spreadSlowness;
					float newDensity = p * spreadSlowness;
					for (int dx = -1; dx <= 1; ++dx) {
						for (int dy = -1; dy <= 1; ++dy) {
							if (x+dx >= 0 && y+dy >= 0 && x+dx < Precision && y+dy < Precision) {
								if (preSmokeDensity[x+dx, y+dy] > 0.0f) {
									neighborCount++;
									if (dx + dy == 1 || dx + dy == -1) {
										newDensity += preSmokeDensity[x+dx, y+dy];
									}
									else {
										newDensity += preSmokeDensity[x+dx, y+dy] * 0.35336f;
									}
								}
							}
						}
					}
					if (neighborCount > spreadSlowness) {
						SmokeDensity[x, y] = (newDensity * (Random.value + 0.8f)) / (float) neighborCount;
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
				if (IsWall[x, y]) {
					color = new Color(0.0f, 0.0f, 1.0f, 1.0f);
				}
				else {
					color = new Color(0.1f, 0.1f, 0.1f, SmokeDensity[x, y]);
				}
				SmokeTexture.SetPixel(x, y, color);
			}
		}
		SmokeTexture.Apply();
		GetComponent<SpriteRenderer>().sprite = 
			Sprite.Create(SmokeTexture, new Rect(0.0f, 0.0f, SmokeTexture.width, SmokeTexture.height), new Vector2(0.5f, 0.5f));
	}
}
