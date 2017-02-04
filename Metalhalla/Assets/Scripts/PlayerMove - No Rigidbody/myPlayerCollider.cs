﻿using UnityEngine;
using System.Collections;

[RequireComponent (typeof(BoxCollider2D))]
public class myPlayerCollider : MonoBehaviour {

	public LayerMask noCloudCollisionMask;
	public LayerMask generalCollisionMask;

	const float skinWidth = .015f;
	public int horizontalRayCount = 4;
	public int verticalRayCount = 4;

	public float maxSlopeClimbAngle = 45;

	float horizontalRaySpacing;
	float verticalRaySpacing;

	BoxCollider2D collider;
	RaycastOrigins raycastOrigins;
	public CollisionInfo collisions;

//	myPlayerInteractor interactor;

	void Start(){
		collider = GetComponent<BoxCollider2D> ();
//		interactor = GetComponent<myPlayerInteractor> ();
		CalculateRaySpacing ();
	}

	public void CheckMove( ref myPlayerMove playerMove ){
		UpdateRaycastOrigins ();
		collisions.Reset ();
		if (playerMove.speed.x != 0)
			HorizontalCollisions (ref playerMove.speed);
		if (playerMove.speed.y != 0)
			VerticalCollisions (ref playerMove.speed);
	}
		
	void HorizontalCollisions(ref Vector3 speed){
		float directionX = Mathf.Sign (speed.x);
		float rayLength = Mathf.Abs (speed.x) + skinWidth;
		for (int i = 0; i < horizontalRayCount; i++) {
			Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
			rayOrigin += Vector2.up * (horizontalRaySpacing * i);
			RaycastHit2D hit = Physics2D.Raycast (rayOrigin, Vector2.right * directionX, rayLength, generalCollisionMask);


			Debug.DrawRay (rayOrigin, Vector2.right*directionX*rayLength , Color.red);

			// need to add the cloud platform collisions
			if (hit) {
				// this is ok since we check the bottom most with a slope
				float surfaceAngle = Vector2.Angle (hit.normal, Vector2.up);
				print (surfaceAngle);
				if (i == 0 && surfaceAngle <= maxSlopeClimbAngle) {
					float distanceToSlopeStart = 0;
					if (surfaceAngle != collisions.slopeAngleOld) {
						distanceToSlopeStart = hit.distance - skinWidth;
						speed.x -= distanceToSlopeStart * directionX;
					}
					ClimbSlope (ref speed, surfaceAngle);
					speed.x += distanceToSlopeStart * directionX;
				}

				// here comes the change
			//	if (!collisions.climbingSlope || surfaceAngle > maxSlopeClimbAngle) {	// ORIGINAL
				if ((!collisions.climbingSlope || surfaceAngle > maxSlopeClimbAngle) && (hit.transform.gameObject.layer != LayerMask.NameToLayer("CloudPlatform"))) {
			//	if ( !collisions.climbingSlope || (surfaceAngle > maxSlopeClimbAngle && hit.transform.gameObject.layer != LayerMask.NameToLayer("Cloud Platform") )) {	
					speed.x = (hit.distance - skinWidth) * directionX;
					rayLength = hit.distance;

					if (collisions.climbingSlope) {
						speed.y = Mathf.Tan (collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs (speed.x);
					}
					collisions.left = directionX == -1;
					collisions.right = directionX == 1;
				}

			}
		}
	}

	void VerticalCollisions(ref Vector3 speed){
		float directionY = Mathf.Sign (speed.y);
		float rayLength = Mathf.Abs (speed.y) + skinWidth;
		bool hitFromBelow = false;

		for (int i = 0; i < verticalRayCount; i++) {
			Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
			rayOrigin += Vector2.right * (verticalRaySpacing * i + speed.x);
			RaycastHit2D hit;
			//mode for cloud platforms
			if (directionY < 0)
				hit = Physics2D.Raycast (rayOrigin, Vector2.up * directionY, rayLength, generalCollisionMask);
			else
				hit = Physics2D.Raycast (rayOrigin, Vector2.up * directionY, rayLength, noCloudCollisionMask);

			Debug.DrawRay (rayOrigin, Vector2.up*directionY *rayLength , Color.red);

			if (hit) {
				speed.y = (hit.distance - skinWidth) * directionY;
				rayLength = hit.distance;

				if (collisions.climbingSlope) {
					speed.x = speed.y / Mathf.Tan (collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign (speed.x);
				}

				collisions.below = directionY == -1;
				collisions.above = directionY == 1;

			}
		}
	}

	void ClimbSlope (ref Vector3 speed, float slopeAngle){
		float moveDistance = Mathf.Abs (speed.x);
		float climbSpeedY = Mathf.Sin (slopeAngle * Mathf.Deg2Rad) * moveDistance;
		if (speed.y <= climbSpeedY) {
			speed.y = climbSpeedY;
			speed.x = Mathf.Cos (slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign (speed.x);
			collisions.below = true;
			collisions.climbingSlope = true;
		}
	}



	void UpdateRaycastOrigins() {
		Bounds bounds = collider.bounds;
		bounds.Expand (skinWidth * -2);

		raycastOrigins.bottomLeft = new Vector2 (bounds.min.x, bounds.min.y);
		raycastOrigins.bottomRight = new Vector2 (bounds.max.x, bounds.min.y);
		raycastOrigins.topLeft = new Vector2 (bounds.min.x, bounds.max.y);
		raycastOrigins.topRight = new Vector2 (bounds.max.x, bounds.max.y);

	}

	void CalculateRaySpacing(){
		Bounds bounds = collider.bounds;
		bounds.Expand (skinWidth * -2);

		horizontalRayCount = Mathf.Clamp (horizontalRayCount, 2, int.MaxValue);
		verticalRayCount = Mathf.Clamp (verticalRayCount, 2, int.MaxValue);

		horizontalRaySpacing = bounds.size.y / (horizontalRayCount - 1);
		verticalRaySpacing = bounds.size.x / (verticalRayCount - 1);
	}



	struct RaycastOrigins {
		public Vector2 topLeft, topRight;
		public Vector2 bottomLeft, bottomRight;
	}

	public struct CollisionInfo {
		public bool above, below;
		public bool left, right;

		public bool climbingSlope;
		public float slopeAngle, slopeAngleOld;

		public void Reset(){
			above = below = left = right = false;
			climbingSlope = false;
			slopeAngleOld = slopeAngle;
			slopeAngle = 0;
		}
	}
}
