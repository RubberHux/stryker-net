using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityTestProject.Scripts
{
    public class Incrementor
    {
        public static int Increment(int arg)
        {
            float test = Mathf.MoveTowards(1f, 1f, 1f);

            if (arg >= 0) return arg + 1;
            else return arg - 1;
        }
    }
}
