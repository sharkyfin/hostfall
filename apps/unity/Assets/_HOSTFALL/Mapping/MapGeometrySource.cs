using UnityEngine;

namespace Hostfall.Mapping
{
    // Explicit authoring for mechanisms whose function cannot be inferred from a mesh name.
    // No Update: FacilityMapWorld refreshes only the colliders underneath a marked mover.
    public sealed class MapGeometrySource : MonoBehaviour
    {
        [SerializeField] private bool floor, moving;
        [SerializeField] private string roomId, roomName;
        public bool Floor => floor;
        public string RoomId => roomId;
        public string RoomName => roomName;
        public void Configure(bool walkSurface, bool moves, string id = "", string label = "")
        { floor=walkSurface; moving=moves; roomId=id; roomName=label; }
        public static bool IsMoving(Transform source)
        {
            for(var t=source;t!=null;t=t.parent)if(t.TryGetComponent<MapGeometrySource>(out var tag)&&tag.moving)return true;
            return false;
        }
    }
}
