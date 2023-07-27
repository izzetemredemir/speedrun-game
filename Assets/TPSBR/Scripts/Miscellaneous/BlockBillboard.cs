using UnityEngine;

namespace TPSBR
{
    public class BlockBillboard : MonoBehaviour
    {
        protected void Start()
        {
            if (transform.parent.localScale.x < 0f)
            {
                // Parent is mirrored, flip billboard so it is shown correctly
                var scale = transform.localScale;
                scale.x = -scale.x;

                transform.localScale = scale;
            }
        }
    }
}
