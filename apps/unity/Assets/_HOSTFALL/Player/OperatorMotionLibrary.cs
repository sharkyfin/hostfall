using System;
using UnityEngine;
namespace Hostfall.Player
{
    [Serializable]public sealed class MotionEntry{public string name;public AnimationClip clip;}
    public sealed class OperatorMotionLibrary:ScriptableObject
    {
        public string[] joints;public MotionEntry[] clips;
        public AnimationClip Find(string id){foreach(var e in clips)if(e.name==id)return e.clip;return null;}
    }
}
