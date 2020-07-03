using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class BoidActor : MonoBehaviour
{
	BoxCollider boidCollider;
	public BoxCollider BoidCollider { get { return boidCollider; } }

	// Start is called before the first frame update
	void Start()
	{
		boidCollider = GetComponent<BoxCollider>();
	}

	public void Move(Vector3 moveVec)
	{
		transform.forward = moveVec;
		transform.position += moveVec * Time.deltaTime /*+ Random.Range(0.00f, 0.02f)*/;
	}
}
