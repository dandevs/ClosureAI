#if UNITASK_INSTALLED
using ClosureBT.Samples.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ClosureBT.Samples.ContextAI
{
    [SelectionBase]
    public class ContextSamplePlayer : MonoBehaviour
    {
        public ContextSampleCamera CameraController;
        public Transform Target;
        public Pawn Pawn;
        public ContextSampleNPC NPC;

        private void Update()
        {
            var keyboard = Keyboard.current;
            var moveInput = Vector3.zero;

            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed) moveInput.z += 1f;
                if (keyboard.sKey.isPressed) moveInput.z -= 1f;
                if (keyboard.aKey.isPressed) moveInput.x -= 1f;
                if (keyboard.dKey.isPressed) moveInput.x += 1f;
                moveInput = moveInput.normalized;
            }

            Pawn.MoveSpeed = 5.5f;
            Pawn.Move(moveInput);

            var lookToPos = SampleUtils.PlaneCastMouse(Camera.main, transform.position);

            // if (lookToPos != Vector3.zero)
            Pawn.LookAtXZ(lookToPos);
        }

        private void OnTriggerEnter(Collider other)
        {
            var areaAI = other.GetComponentInParent<ContextAreaAI>();

            if (areaAI)
                NPC.CustomAI = areaAI.GetAI(NPC);
        }

        private void OnTriggerExit(Collider other)
        {
            var areaAI = other.GetComponentInParent<ContextAreaAI>();

            if (areaAI)
                NPC.CustomAI = null;
        }
    }
}
#endif
