using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDamageble 
{
    int startHealth { get; }
    int curHealth { get; }
    bool Damaged(int damage);
}
