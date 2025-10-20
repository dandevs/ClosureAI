using UnityEngine;
using UnityEngine.AI;

namespace ClosureAI.Samples.Shared
{
    public partial class Pawn : MonoBehaviour
    {
        public Inventory Inventory;
        public NavMeshAgent Agent;
        public CharacterController CharacterController;
        public GameObject Model;

        public float MoveSpeed = 3.5f;

        private void OnValidate()
        {
            if (!Inventory) Inventory = GetComponent<Inventory>();
            if (!Agent) Agent = GetComponent<NavMeshAgent>();
            if (!CharacterController) CharacterController = GetComponent<CharacterController>();
        }

        public bool MoveTo(Vector3 position, float stoppingDistance = 0.1f)
        {
            Agent.stoppingDistance = stoppingDistance;
            Agent.SetDestination(position);
            Agent.speed = MoveSpeed;

            if (Agent.pathPending)
                return false;

            return Agent.remainingDistance <= stoppingDistance;
        }

        public void Move(Vector3 direction)
        {
            if (CharacterController)
                CharacterController.SimpleMove(direction * MoveSpeed);
            else
            {
                Agent.speed = MoveSpeed;
                Agent.Move(direction * (Time.deltaTime * Agent.speed));
            }
        }

        public bool LookAtXZ(Vector3 position)
        {
            var direction = position - transform.position;
            direction.y = 0;

            var targetRotation = Quaternion.LookRotation(direction);
            var angle = Quaternion.Angle(transform.rotation, targetRotation);

            if (angle < 2f)
                return true;

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 12f);
            return false;
        }
    }
}
