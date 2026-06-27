using UnityEngine;

namespace JWLib.AnimationSystem
{
    [CreateAssetMenu(fileName = "AnimParamSO", menuName = "SO/Animation/AnimParamSO", order = 0)]
    public class AnimParamSO : ScriptableObject
    {
        [field:SerializeField]public string ParamName { get; private set; }
        [field:SerializeField]public int ParamHash { get; private set; }

        private void OnValidate()
        {
            if (ParamName != null)
                ParamHash = Animator.StringToHash(ParamName);
        }
    }
}