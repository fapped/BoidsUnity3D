using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Boid : MonoBehaviour
{
	public BoidActor Prefab;
	List<BoidActor> actors = new List<BoidActor>();

	[Range(10, 500)]
	public int BoidsCount = 250;

	[Range(0.01f, 0.5f)]
	public float BoidsDensity = 0.08f;

	[Range(0f, 100f)]
	public float Speed = 5f;

	[Range(1f, 10f)]
	public float neighbourRadius = 1.5f;

	[Range(0f, 1f)]
	public float avoidanceRadiusMultiplier = 0.5f;

	float squareMaxSpeed => Speed * Speed;
	float squareNeighbourRadius => neighbourRadius * neighbourRadius;
	float squareAvoidanceRadius => squareNeighbourRadius * avoidanceRadiusMultiplier * avoidanceRadiusMultiplier;

	[Range(0f, 10f)]
	public float CohesionStrength = 4.0f;

	[Range(0f, 10f)]
	public float AlignmentStrength = 1.0f;

	[Range(0f, 10f)]
	public float AvoidanceStrength = 2.0f;

	[Range(0f, 10f)]
	public float RadiusStrength = 0.1f;

	[Range(0f, 50f)]
	public float RadiusSize = 15f;

	public Text textValues;

	// Start is called before the first frame update
	void Start()
	{
		for (int idx = 0; idx < BoidsCount; idx++)
		{
			BoidActor newActor = Instantiate(Prefab, Random.insideUnitSphere * BoidsCount * BoidsDensity, Quaternion.Euler(Vector3.forward * Random.Range(0f, 360f)), transform);

			newActor.GetComponentInChildren<Renderer>().material.color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));

			newActor.name = "actor" + idx;

			actors.Add(newActor);
		}
	}

	// Update is called once per frame
	void Update()
	{
		{
			float newSpeed = 0.0f;
			if ((newSpeed = Input.GetAxis("Speed")) != 0.0f)
			{
				Speed += (newSpeed * 0.1f);

				if (Speed > 100.0f)
					Speed = 100.0f;
				if (Speed < 1.0f)
					Speed = 1.0f;
			}
		}

		{
			float newCohesion = 0.0f;
			if ((newCohesion = Input.GetAxis("Cohesion")) != 0.0f)
			{
				CohesionStrength += (newCohesion * 0.1f);

				if (CohesionStrength > 10.0f)
					CohesionStrength = 10.0f;
				if (CohesionStrength < 0.01f)
					CohesionStrength = 0.01f;
			}
		}

		{
			float newAlignment = 0.0f;
			if ((newAlignment = Input.GetAxis("Alignment")) != 0.0f)
			{
				AlignmentStrength += (newAlignment * 0.1f);

				if (AlignmentStrength > 10.0f)
					AlignmentStrength = 10.0f;
				if (AlignmentStrength < 0.01f)
					AlignmentStrength = 0.01f;
			}
		}

		{
			float newAvoidance = 0.0f;
			if ((newAvoidance = Input.GetAxis("Avoidance")) != 0.0f)
			{
				AvoidanceStrength += (newAvoidance * 0.1f);

				if (AvoidanceStrength > 10.0f)
					AvoidanceStrength = 10.0f;
				if (AvoidanceStrength < 0.01f)
					AvoidanceStrength = 0.01f;
			}
		}

		{
			float newRadius = 0.0f;
			if ((newRadius = Input.GetAxis("Radius")) != 0.0f)
			{
				neighbourRadius += (newRadius * 0.1f);

				if (neighbourRadius > 10.0f)
					neighbourRadius = 10.0f;
				if (neighbourRadius < 1.0f)
					neighbourRadius = 1.0f;
			}
		}

		textValues.text = "Cohesion [7/8] = " + CohesionStrength.ToString("0.00") +
			"\nAlignment [4/5] = " + AlignmentStrength.ToString("0.00") +
			"\nAvoidance [1/2] = " + AvoidanceStrength.ToString("0.00") +
			"\nSpeed [+/-] = " + Speed.ToString("0.00") +
			"\nNeighbour radius [0/,] = " + neighbourRadius.ToString("0.00") +
			"\nAlt+F4 exit";

		foreach (var actor in actors)
		{
			List<Transform> neighbours = GetNeighbours(actor);

			Vector3 movement = Vector3.zero;

			var obstacles = neighbours.Where(neigh => neigh.CompareTag("obstacle"));
			var boids = neighbours.Where(neigh => !neigh.CompareTag("obstacle"));

			//radius
			{
				Vector3 centerOffset = Vector3.zero - actor.transform.position;
				float percentLen = centerOffset.magnitude / RadiusSize;

				if (percentLen > 0.8f)
				{
					Vector3 insideMove = (centerOffset * percentLen * percentLen) * RadiusStrength;
					insideMove = GetSafeValue(insideMove, RadiusStrength);
					movement += insideMove;
				}
			}

			{
				Vector3 cohesionMove = CalcCohesion(actor, boids) * CohesionStrength;
				if (cohesionMove != Vector3.zero)
				{
					cohesionMove = GetSafeValue(cohesionMove, CohesionStrength);
					movement += cohesionMove;
				}
			}

			{
				Vector3 alignmentMove = CalcAlignment(actor, boids) * AlignmentStrength;
				if (alignmentMove != Vector3.zero)
				{
					alignmentMove = GetSafeValue(alignmentMove, AlignmentStrength);
					movement += alignmentMove;
				}
			}

			{
				Vector3 avoidanceMove = CalcAvoidance(actor, boids, this) * AvoidanceStrength;
				if (avoidanceMove != Vector3.zero)
				{
					avoidanceMove = GetSafeValue(avoidanceMove, AvoidanceStrength);
					movement += avoidanceMove;
				}
			}

			{
				Vector3 avoidObstacleMove = CalcAvoidObstacle(actor, obstacles, this) * 5.0f;
				if (avoidObstacleMove != Vector3.zero)
				{
					avoidObstacleMove = GetSafeValue(avoidObstacleMove, 5.0f);
					movement += avoidObstacleMove;
				}
			}

			movement *= 10.0f;	//boost

			if (movement.sqrMagnitude > squareMaxSpeed)
				movement = movement.normalized * Speed;

			actor.Move(movement);
		}
	}

	Vector3 GetSafeValue(Vector3 value, float strength)
	{
		if (value.sqrMagnitude > strength * strength)
		{
			value.Normalize();
			value *= strength;
		}

		return value;
	}

	#region cohesion
	Vector3 currentVelocity;
	public float cohesionSmoothTime = 0.5f;

	Vector3 CalcCohesion(BoidActor actor, IEnumerable<Transform> neighbours)
	{
		Vector3 cohesionMove = Vector3.zero;

		if (neighbours.Any() == false)
			return cohesionMove;

		foreach (var item in neighbours)
			cohesionMove += item.position;

		cohesionMove /= neighbours.Count();

		cohesionMove -= actor.transform.position;
		cohesionMove = Vector3.SmoothDamp(actor.transform.forward, cohesionMove, ref currentVelocity, cohesionSmoothTime, squareMaxSpeed);

		return cohesionMove;
	}
	#endregion

	#region alignment
	Vector3 CalcAlignment(BoidActor actor, IEnumerable<Transform> neighbours)
	{
		if (neighbours.Any() == false)
			return actor.transform.forward;

		Vector3 alignmentMove = Vector3.zero;

		foreach (var item in neighbours)
			alignmentMove += item.transform.forward;

		alignmentMove /= neighbours.Count();

		return alignmentMove;
	}
	#endregion

	#region avoidance
	Vector3 CalcAvoidance(BoidActor actor, IEnumerable<Transform> neighbours, Boid Boid)
	{
		Vector3 avoidanceMove = Vector3.zero;

		if (neighbours.Any() == false)
			return avoidanceMove;

		int avoid = 0;

		foreach (var item in neighbours)
		{
			if (Vector3.SqrMagnitude(item.position - actor.transform.position) < Boid.squareAvoidanceRadius)
			{
				avoid++;
				avoidanceMove += actor.transform.position - item.position;
			}
		}

		if (avoid > 0)
			avoidanceMove /= avoid;

		return avoidanceMove;
	}
	#endregion


	Vector3 CalcAvoidObstacle(BoidActor actor, IEnumerable<Transform> neighbours, Boid Boid)
	{
		Vector3 avoidanceMove = Vector3.zero;

		if (neighbours.Any() == false)
			return avoidanceMove;

		int avoid = 0;

		foreach (var item in neighbours)
		{
			if (item.GetComponent<Renderer>().bounds.SqrDistance(actor.transform.position) < Boid.squareAvoidanceRadius)
			{
				Debug.DrawLine(item.position, actor.transform.position, Color.red, 3.0f);
				avoid++;
				avoidanceMove += actor.transform.position - item.position;
			}
		}

		if (avoid > 0)
			avoidanceMove /= avoid;

		return avoidanceMove;
	}

	List<Transform> GetNeighbours(BoidActor actor)
	{
		List<Transform> context = new List<Transform>();
		Collider[] contextColliders = Physics.OverlapSphere(actor.transform.position, neighbourRadius);

		foreach (var collider in contextColliders)
		{
			if (collider == actor.BoidCollider)
				continue;

			context.Add(collider.transform);
		}

		return context;
	}
}
