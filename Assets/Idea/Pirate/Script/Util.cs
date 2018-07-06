using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Util {
    public static void SetTfm(Transform transform, float x, float y, float angle) {
        transform.position = new Vector3(x, 0, y);
        transform.rotation = Quaternion.AngleAxis(Mathf.Rad2Deg * angle,
            Vector3.down);
    }
}
