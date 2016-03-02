using UnityEngine;
using System.Collections;

// Part is another serializable data storage class just like WeaponDefinition
[System.Serializable]
public class Part {
	// These three fields need to be defined in the Inspector pane
	public string name; // The name of this part
	public float health; // Th amount of health this part has
	public string[] protectedBy; // The other parts that protect this

	// These two fields are set automatically in Start().
	// Caching like this makes it faster and easier to find these later
	public GameObject go; // The GameObject of this part
	public Material mat; // The Material to show damage
}

public class Enemy_4 : Enemy {
	// Enemy_4 will start offscreen and then pick a random pointon screen to
	// move to. Once it has arrived, it will pick another random point and 
	// continue until the player has shot it down.
	public Vector3[] points; // Stores the p0 and p1 for interpolation
	public float timeStart; // Birth time for this Enemy_4
	public float duration = 4; // Duration of movement

	public Part[] parts; // The array of ship Parts

	void Start(){
		points = new Vector3[2];
		// There is already an initial position chosen by Main.SpawnEnemy()
		// so add it to points as the initial p0 and p1
		points [0] = pos;
		points [1] = pos;

		InitMovement ();

		// Cache GameObject & Material of each Part in parts
		Transform t;
		foreach (Part prt in parts) {
			t = transform.Find(prt.name);
			if (t != null){
				prt.go = t.gameObject;
				prt.mat = prt.go.renderer.material;
			}
		}
	}

	void InitMovement(){
		// Pick a new point to move to that is on screen
		Vector3 p1 = Vector3.zero;
		float esp = Main.S.enemySpawnPadding;
		Bounds cBounds = Utils.camBounds;
		p1.x = Random.Range (cBounds.min.x + esp, cBounds.max.x - esp);
		p1.y = Random.Range (cBounds.min.y + esp, cBounds.max.y - esp);

		points [0] = points [1]; // Shift points[1] to points[0]
		points [1] = p1; // Add p1 as points[1]

		// Reset the time
		timeStart = Time.time;
	}

	public override void Move(){
		// This completely overrides Enemy.Move() with a linear interpolation

		float u = (Time.time - timeStart) / duration;
		if (u >= 1) { // if u >= 1...
			InitMovement (); // ...then initialize movement to a new point
			u=0;
		}

		u = 1 - Mathf.Pow (1 - u, 2); // Apply Ease Ou easing to u

		pos = (1 - u) * points [0] + u*points [1]; // Simple linear interpolation
	}

	// This will override the OnCollisionEnter that is part of Enemy.cs
	// Because of the way that MonoBehavior declares common Uniy functions
	// like OnCollisionEnter(), the override keyword is not necessary
	void OnCollisionEnter(Collision coll){
		GameObject other = coll.gameObject;
		switch (other.tag) {
		case "ProjectileHero":
			Projectile p = other.GetComponent<Projectile>();
			// Enemies don't take damage unless they're on screen
			// This stops the player from shooting them before they are visible
			bounds.center = transform.position + boundsCenterOffset;
			if (bounds.extents == Vector3.zero || Utils.ScreenBoundsCheck(bounds, 
			  BoundsTest.offScreen) != Vector3.zero){
				Destroy (other);
				break;
			}

			// Hut this Enemy
			// Find the GameObject that was hit
			// The Collision coll has contacts[], an array of ContactPoints
			// Because there was a collision, we're guaranteed that there is at
			// least a contacts[0], and ContactPoints have a reference to
			// thisCollider, whic ill be the collider for the part of the 
			// Enemy_4 that was hit.
			GameObject goHit = coll.contacts[0].thisCollider.gameObject;
			Part prtHit = FindPart(goHit);
			if (prtHit == null){ // If prtHit wasn't fnd
				// ...then it's usually because, very rarely, thisCollider on
				// contacts[0] will be the ProjectileHero instead of the ship
				// part. If so, just look for otherCollider instead
				goHit = coll.contacts[0].otherCollider.gameObject;
				prtHit = FindPart(goHit);
			}
			// Check whether this part is still protected
			if (prtHit.protectedBy != null){
				foreach(string s in prtHit.protectedBy){
					// If one of the protecting parts hasn't been destroyed...
					if (!Destroyed(s)){
						// ...then don't damage this part yet
						Destroy (other); // Destroy the ProjectileHero
						return; // return before causing damage
					}
				}
			}
		}
	}
}
