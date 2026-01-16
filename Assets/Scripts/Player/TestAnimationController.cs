using UnityEngine;

namespace Player
{
    public class TestAnimationController : MonoBehaviour
    {
        [SerializeField] private AnimationClip clip1;
        [SerializeField] private AnimationClip clip2;
        [SerializeField] private AnimationController animationController;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                animationController.PlayAnimation(clip1);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                animationController.PlayAnimation(clip2);
            }
        }
    }
}