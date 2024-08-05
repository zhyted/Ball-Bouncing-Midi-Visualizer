using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class CameraScript : MonoBehaviour
{
    GameObject followTarget;
    bool enabled = false;
    public float smoothSpeed = 2000000f;



    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void PlayerFollowStart(GameObject player)
    {
        followTarget = player;
        enabled = true;
    }

    public void FixedUpdate()
    {
        if (enabled)
        {
            var ySub = (followTarget.transform.position.y - gameObject.transform.position.y)/1.4f;

            Vector3 desiredPosition = new Vector3(followTarget.transform.position.x, followTarget.transform.position.y - ySub, -10f);
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
            transform.position = smoothedPosition;
        }
    }
}
