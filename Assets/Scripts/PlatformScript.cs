using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformScript : MonoBehaviour
{
    // Start is called before the first frame update

    public float savedCalc;
    public List<Dictionary<string, float>> notes;
    public bool touched = false;

    public void setTouched()
    {
        touched = true;
    }

    public float xTeleport;


}
