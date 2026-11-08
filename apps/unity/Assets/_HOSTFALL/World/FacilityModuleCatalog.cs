using System;
using UnityEngine;
namespace Hostfall.World
{
    [Serializable] public sealed class FacilityModule
    {
        public string id, category;
        public GameObject prefab;
        public Vector3 size;
    }
    [CreateAssetMenu(menuName="HOSTFALL/Facility Module Catalog")]
    public sealed class FacilityModuleCatalog : ScriptableObject { public FacilityModule[] modules; }
}
