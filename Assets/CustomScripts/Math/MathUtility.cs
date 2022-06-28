using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MathUtility : MonoBehaviour
{
    private float angle = 0f;
    private Vector3 axis = new Vector3(0, 0, 0);
    public void QuaternionToRotaion(Quaternion quat_in, ref float[] rot_out)
    {
        quat_in.ToAngleAxis(out angle, out axis);
    }
}
