using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneVirtualFixture : BaseVirtualFixture
{
    private bool IsVfBeingTouched = false;
    private Vector3 normal_vector = new Vector3(0, 0, 0);

    private void Update()
    {
        IsVfBeingTouched = GetInteractionStatus();
        ChangeMaterialOnContact(IsVfBeingTouched);
        GetNormalVectorAtContactPoint(IsVfBeingTouched, ref normal_vector);

    }

}
