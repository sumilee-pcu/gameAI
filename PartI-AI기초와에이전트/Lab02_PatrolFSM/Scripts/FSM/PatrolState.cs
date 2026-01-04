using UnityEngine;

public class PatrolState : State
{
    private Transform[] waypoints;
    private int currentWaypointIndex = 0;
    private float moveSpeed;
    private const float ARRIVAL_DISTANCE = 1f;
    private const float ROTATION_SPEED = 5f;

    public PatrolState(GameObject agent, Transform[] waypoints, float moveSpeed)
        : base(agent, "Patrol")
    {
        this.waypoints = waypoints;
        this.moveSpeed = moveSpeed;
    }

    public override void OnUpdate()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Transform target = waypoints[currentWaypointIndex];
        Vector3 direction = (target.position - agent.transform.position).normalized;

        agent.transform.position += direction * moveSpeed * Time.deltaTime;

        if (direction != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            agent.transform.rotation = Quaternion.Slerp(agent.transform.rotation, targetRot, ROTATION_SPEED * Time.deltaTime);
        }

        if (Vector3.Distance(agent.transform.position, target.position) < ARRIVAL_DISTANCE)
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
    }

    public override void OnDrawGizmos()
    {
        if (waypoints != null && waypoints.Length > 0)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(agent.transform.position, waypoints[currentWaypointIndex].position);
        }
    }
}
