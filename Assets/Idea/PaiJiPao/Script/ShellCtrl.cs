using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShellCtrl : MonoBehaviour {
	public GameObject explosion;
	public float explosionRadius = 3;
	public float explosionForce = 3;
	public float upwaredsModifier = 1;
	private new Rigidbody rigidbody;

	// Use this for initialization
	void Start () {
		DestroyObject(gameObject, 5);
	}

	void MakeExplosion() {
		Instantiate(explosion, transform.position, Quaternion.identity);
		Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
		Debug.Log("collider count " + colliders.Length);
		foreach(var collider in colliders) {
			Debug.Log(collider.tag);
			if (collider.tag == "rock") {
                Rigidbody rb = collider.GetComponent<Rigidbody>();
                rb.AddExplosionForce(explosionForce, transform.position,
                	explosionRadius, upwaredsModifier);
			}
		}
	}

	void OnDestroy() {
		Debug.Log("Destroy");
		//MakeExplosion();
	}

	void OnCollisionEnter(Collision collision) {
		DestroyObject(gameObject);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
