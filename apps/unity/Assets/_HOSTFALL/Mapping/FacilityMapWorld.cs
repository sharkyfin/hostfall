using System;
using System.Collections.Generic;
using System.Linq;
using Hostfall.Core;
using Hostfall.Devices;
using Hostfall.Interaction;
using Hostfall.Player;
using Hostfall.Simulation;
using Hostfall.World;
using UnityEngine;

namespace Hostfall.Mapping
{
    public sealed class MapRoom
    {
        public string Id, Name, Sector;
        public readonly List<MapShape> Floors=new();
        public Rect Bounds { get; private set; }
        public float Height { get; private set; }
        public int Level=>Height<-.5f?-1:Height<.5f?0:1;
        public void Refresh()
        {
            var b=Floors[0].Collider.bounds;
            foreach(var f in Floors.Skip(1))b.Encapsulate(f.Collider.bounds);
            Bounds=new Rect(b.min.x,b.min.z,b.size.x,b.size.z);Height=b.max.y;
        }
        public bool Contains(Vector3 p){var point=new Vector2(p.x,p.z);foreach(var f in Floors)if(f.HorizontalBounds.Contains(point))return true;return false;}
    }
    public sealed class MapShape
    {
        public readonly Collider Collider;
        public readonly bool Floor, Stair, Wall;
        public readonly bool Moving;public int Revision {get;private set;}
        private Matrix4x4 matrix;
        private Bounds previousBounds;
        private Vector3[] vertices;
        private readonly Dictionary<int,Vector2[]> slices=new();
        public Rect HorizontalBounds { get; private set; }
        public MapShape(Collider c,bool floor)
        {
            Collider=c;Floor=floor;Moving=c.GetComponentInParent<FacilityDoor>()!=null||c.GetComponentInParent<RetractableBridgeController>()!=null||c.attachedRigidbody!=null||MapGeometrySource.IsMoving(c.transform);
            string n=c.name.ToLowerInvariant();
            Stair=n.Contains("stair")||c.GetComponentInParent<Transform>().name.Contains("Stairs");
            Wall=n.Contains("wall")||n.Contains("jamb")||n.Contains("lintel")||n.Contains("bulkhead");
            Refresh(true);
        }
        public bool Active=>Collider!=null && Collider.enabled && !Collider.isTrigger && Collider.gameObject.activeInHierarchy;
        public void Refresh(bool force=false)
        {
            // Explicit refresh must honour edits/resizing too. FacilityMapWorld schedules
            // only moving shapes; static shapes are otherwise cached after construction.
            if(Collider==null)return;
            var m=Collider.transform.localToWorldMatrix;
            var currentBounds=Collider.bounds;
            if(!force&&matrix==m&&previousBounds==currentBounds)return;
            matrix=m;previousBounds=currentBounds;slices.Clear();Revision++;
            if(Collider is BoxCollider box)
            {
                vertices=new Vector3[8];
                for(int i=0;i<8;i++)vertices[i]=m.MultiplyPoint3x4(box.center+Vector3.Scale(box.size*.5f,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));
            }
            else if(Collider is MeshCollider mesh && mesh.sharedMesh!=null && mesh.sharedMesh.isReadable)
                vertices=mesh.sharedMesh.vertices.Select(v=>m.MultiplyPoint3x4(v)).ToArray();
            else
            {
                var b=Collider.bounds;vertices=new[]{b.min,b.max,new Vector3(b.min.x,b.min.y,b.max.z),new Vector3(b.max.x,b.max.y,b.min.z)};
            }
            var bounds=Collider.bounds;HorizontalBounds=new Rect(bounds.min.x,bounds.min.z,bounds.size.x,bounds.size.z);
        }
        public Vector2[] Footprint(float height,bool whole=false)
        {
            int key=whole?int.MinValue:Mathf.RoundToInt(height*100);
            if(slices.TryGetValue(key,out var cached))return cached;
            var b=Collider.bounds;
            float low=height+.05f,high=height+1.75f;
            if(!whole&&(b.max.y<low||b.min.y>high))return slices[key]=Array.Empty<Vector2>();
            var points=new List<Vector2>();
            foreach(var v in vertices)if(whole||Stair||(v.y>=low&&v.y<=high))points.Add(new Vector2(v.x,v.z));
            if(!whole && Collider is BoxCollider)
                for(int i=0;i<8;i++)foreach(int bit in new[]{1,2,4})
                {
                    int j=i^bit;if(j<i)continue;
                    var a=vertices[i];var z=vertices[j];
                    foreach(float plane in new[]{low,high})
                        if((a.y-plane)*(z.y-plane)<0)
                        { var p=Vector3.Lerp(a,z,(plane-a.y)/(z.y-a.y));points.Add(new Vector2(p.x,p.z)); }
                }
            return slices[key]=MapGeometry.Hull(points);
        }
    }
    public sealed class MapMarker
    {
        public Component Source;
        public string Kind, Label;
        public Vector3 Position=>Source.transform.position;
        public bool Active=>Source!=null&&Source.gameObject.activeInHierarchy;
    }
    public sealed class FacilityMapWorld
    {
        public readonly List<MapRoom> Rooms=new();
        public readonly List<MapShape> Shapes=new();
        public readonly List<MapMarker> Markers=new();
        public readonly List<MonoBehaviour> Threats=new();
        private readonly List<MapShape> moving=new();
        public ExplorationState Exploration { get; }
        public Rect Bounds { get; private set; }
        public FacilityMapWorld(ExplorationState exploration)
        {
            Exploration=exploration;
            var groups=new Dictionary<string,MapRoom>();
            foreach(var c in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if(c.isTrigger||c.gameObject.layer==CollisionLayers.Player||c.GetComponentInParent<IDamageable>()!=null)continue;
                string n=c.name.ToLowerInvariant();
                if(n.Contains("ceiling")||n.Contains("roof"))continue;
                bool floor=IsFloor(c);
                var shape=new MapShape(c,floor);Shapes.Add(shape);if(shape.Moving)moving.Add(shape);
                if(!floor)continue;
                string group=FloorGroup(c.transform);string id=group;
                if(!groups.TryGetValue(id,out var room))
                {
                    room=new MapRoom{Id=id,Name=RoomName(c.transform),Sector=SectorName(c.transform)};
                    groups.Add(id,room);Rooms.Add(room);
                }
                room.Floors.Add(shape);
            }
            Rooms.Sort((a,b)=>string.CompareOrdinal(a.Id,b.Id));foreach(var r in Rooms)r.Refresh();Refresh();
            foreach(var t in UnityEngine.Object.FindObjectsByType<ComputerTerminal>(FindObjectsSortMode.None))Markers.Add(new MapMarker{Source=t,Kind="terminal",Label=t.Id.Hostname().ToUpperInvariant()});
            foreach(var t in UnityEngine.Object.FindObjectsByType<DoorTerminalConsole>(FindObjectsSortMode.None))Markers.Add(new MapMarker{Source=t,Kind="terminal",Label=t.DeviceId.ToString().ToUpperInvariant()});
            foreach(var station in UnityEngine.Object.FindObjectsByType<ComputerStation>(FindObjectsSortMode.None))
            {
                var computer = Hostfall.Simulation.Computers.FieldComputer.Find(station.StationId);
                Markers.Add(new MapMarker{Source=station,Kind="terminal",Label=station.StationId==Hostfall.Simulation.Computers.ComputerCampaign.HomeStation?"HOME":computer?.Name ?? "WS-07"});
            }
            foreach(var d in UnityEngine.Object.FindObjectsByType<FacilityDoor>(FindObjectsSortMode.None))Markers.Add(new MapMarker{Source=d,Kind="door",Label="Шлюз"});
            foreach(var c in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if(c is IDamageable && !(c is PlayerHealth))Threats.Add(c);
                if(c is DataCartridgeItem || c is PhysicalMediaItem || c is WeaponPickup || c is AmmoBoxPickup || c is HealthKitPickup)
                    Markers.Add(new MapMarker{Source=c,Kind="item",Label="Предмет"});
            }
        }
        public void Refresh()
        {
            foreach(var shape in moving)shape.Refresh();
            foreach(var room in Rooms){foreach(var floor in room.Floors)if(floor.Moving){room.Refresh();break;}}
            if(Rooms.Count==0)return;
            float x=Rooms.Min(r=>r.Bounds.xMin),z=Rooms.Min(r=>r.Bounds.yMin);
            Bounds=Rect.MinMaxRect(x,z,Rooms.Max(r=>r.Bounds.xMax),Rooms.Max(r=>r.Bounds.yMax));
        }
        public MapRoom At(Vector3 p)
        {
            MapRoom best=null;float distance=float.MaxValue,area=float.MaxValue;
            foreach(var room in Rooms)
            {
                if(room.Height>p.y+.35f||!room.Contains(p))continue;
                float d=Mathf.Abs(p.y-room.Height),a=room.Bounds.width*room.Bounds.height;
                if(d<distance||d==distance&&a<area){best=room;distance=d;area=a;}
            }
            return best;
        }
        public void Visit(Vector3 p)
        {
            var room=At(p);if(room!=null && p.y-room.Height<2)Exploration.Visit(room.Id);
        }
        public bool KnownAt(Vector3 p,int level,float padding=0)
        {
            foreach(var r in Rooms)
                if(r.Level==level&&p.y>=r.Height-.4f&&p.y-r.Height<2.2f&&Exploration.HasVisited(r.Id)
                   &&new Rect(r.Bounds.xMin-padding,r.Bounds.yMin-padding,r.Bounds.width+padding*2,r.Bounds.height+padding*2).Contains(new Vector2(p.x,p.z)))return true;
            return false;
        }

        public bool CanSeeThreat(MonoBehaviour enemy,Vector3 eye,float range=18)
        {
            if(enemy==null||!enemy.isActiveAndEnabled||!enemy.gameObject.activeInHierarchy||((IDamageable)enemy).IsDestroyed)return false;
            var target=enemy.transform.position+Vector3.up*.5f;
            var delta=target-eye;
            if(delta.magnitude>range)return false;
            return !Physics.Raycast(eye,delta.normalized,out var hit,delta.magnitude,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore)
                || hit.collider.GetComponentInParent<IDamageable>()==enemy as IDamageable;
        }
        private static bool IsFloor(Collider c)
        {
            if(c.TryGetComponent<MapGeometrySource>(out var tag))return tag.Floor;
            string n=c.name.ToLowerInvariant();var b=c.bounds;
            if(b.size.x*b.size.z<1 || b.size.y>1.2f || n.Contains("desk")||n.Contains("runner")||n.Contains("guide")||n.Contains("strip"))return false;
            return n.Contains("floor")||n=="east gallery slab"||n=="west gallery slab"||n=="mezzanine slab"||n.Contains("catwalk")&&n.Contains("slab")||n=="relay bridge walkway"||n.Contains("bridge fixed wing")||n=="leaf walkway slab";
        }
        private static string Path(Transform t)=>t.parent==null?t.name:Path(t.parent)+"/"+t.name;
        private static Transform Ancestor(Transform t,string prefix)
        {for(var p=t;p!=null;p=p.parent)if(p.name.StartsWith(prefix,StringComparison.Ordinal))return p;return null;}
        private static string FloorGroup(Transform t)
        {
            if(t.TryGetComponent<MapGeometrySource>(out var source)&&source.Floor&&!string.IsNullOrEmpty(source.RoomId))return source.RoomId;
            foreach(string prefix in new[]{"SOC /","SRV /"}) {var p=Ancestor(t,prefix);if(p!=null)return Path(p);}
            if(t.name.StartsWith("Quantum Chamber Upper Floor")||t.name=="Relay Bridge Walkway")return "s04.quantum-ground";
            if(t.name.Contains("Bridge Fixed Wing")||t.name=="Leaf Walkway Slab")return "s02.bridge";
            return Path(t);
        }
        private static string SectorName(Transform t)
        {
            // Repair connectors can live under another sector's authoring root.
            // Their physical identity is a transition, not the owning Transform's sector.
            if(new[]{"S06-S08", "S09-S10", "SOC-Citadel", "SOC Shortcut", "Logistics Return"}.Any(p=>t.name.StartsWith(p,StringComparison.Ordinal)))return "ПЕРЕХОД";
            if(Ancestor(t,"SOC /")!=null)return "СЕКТОР 00";
            if(Ancestor(t,"SRV /")!=null||t.name=="Corridor floor")return "СЕКТОР 01";
            var s=Ancestor(t,"Sector ");
            if(s==null)return "ПЕРЕХОД";
            return s.name.Contains("Shelter")?"УБЕЖИЩЕ":"СЕКТОР "+s.name.Substring(7,2);
        }
        private static string RoomName(Transform t)
        {
            if(t.TryGetComponent<MapGeometrySource>(out var source)&&source.Floor&&!string.IsNullOrEmpty(source.RoomName))return source.RoomName;
            if(Ancestor(t,"SOC /")!=null)return "Операторская";
            if(Ancestor(t,"SRV /")!=null)return "Серверная";
            string name=t.name;
            if(name.StartsWith("S07 Corridor Floor"))return "Служебный обход";
            if(name.StartsWith("Quantum Chamber Upper Floor")||name=="Relay Bridge Walkway")return "Квантовый мост";
            if(name.Contains("Bridge Fixed Wing")||name=="Leaf Walkway Slab")return "Выдвижной мост";
            if(name=="Mezzanine Slab")return Ancestor(t,"Sector 05")!=null?"Грузовая галерея":"Галерея диспетчерской";
            return Names.TryGetValue(name,out var label)?label:"Переход";
        }
        private static readonly Dictionary<string,string> Names=new()
        {
            ["Corridor floor"]="Служебный проход",["Shelter Floor Slab"]="Мастерская инженера",["Shelter Passage Floor"]="Вход в убежище",
            ["Compute Vault Floor Base"]="Вычислительный узел",["East Gallery Slab"]="Пост наблюдения",["West Gallery Slab"]="Западная галерея",
            ["Armory Floor Base"]="Оружейная",["Energy Center Floor Base"]="Энергоцентр",["Trench Floor"]="Кабельная траншея",
            ["Sector05 Floor Base"]="Логистика",["Sector06 Floor Base"]="Сборочный цех",["Catwalk Slab"]="Галерея робототехники",
            ["S07 Foyer Floor"]="Вестибюль",["S07 Atrium Floor"]="Атриум охраны",["Catwalk T1 Slab"]="Галерея охраны",
            ["S07 CCTV Floor"]="Пост камер",["S07 Archive Floor"]="Архив ключей",["S08 Gallery Floor"]="Нейроархив",
            ["S08 Pods Floor"]="Зал капсул",["S08 Chassis Floor"]="Мастерская корпусов",["S08 Office Floor"]="Исследовательский офис",
            ["S08 Vault Floor"]="Хранилище памяти",["S09 Ring Floor"]="Кольцо узлов",["S09 Dispatch Floor"]="Диспетчерская",
            ["S09 Witness Floor"]="Независимый узел",["S09 Backup Floor"]="Резервная галерея",["S10 Arena Floor"]="Главный процессор",
            ["S10 Trust Floor"]="Сердцевина доверия",["S10 Evac Floor"]="Эвакуация",["Connector Floor"]="Сервисный переход",
            ["Bypass Floor"]="Технический обход",["S06-S08 Airlock Floor"]="Шлюз нейроархива",["S09-S10 Transition Floor"]="Переход к ядру",
            ["SOC-Citadel Link Floor"]="Переход охраны",["SOC Shortcut Threshold Floor"]="Служебный порог",
            ["Logistics Return Floor N"]="Возврат в логистику",["Logistics Return Floor E"]="Поворот к логистике"
        };
    }
    public static class MapGeometry
    {
        public static Vector2[] Hull(IEnumerable<Vector2> source)
        {
            var p=source.Distinct().OrderBy(v=>v.x).ThenBy(v=>v.y).ToArray();if(p.Length<3)return p;
            var h=new List<Vector2>();
            foreach(var v in p){while(h.Count>=2&&Cross(h[h.Count-1]-h[h.Count-2],v-h[h.Count-1])<=0)h.RemoveAt(h.Count-1);h.Add(v);}
            int n=h.Count;
            for(int i=p.Length-2;i>=0;i--){while(h.Count>n&&Cross(h[h.Count-1]-h[h.Count-2],p[i]-h[h.Count-1])<=0)h.RemoveAt(h.Count-1);h.Add(p[i]);}
            h.RemoveAt(h.Count-1);return h.ToArray();
        }
        public static Vector2[] Clip(Vector2[] polygon,Rect rect)
        {
            var p=polygon.ToList();
            for(int side=0;side<4&&p.Count>0;side++)
            {
                var input=p;p=new List<Vector2>();var a=input[input.Count-1];
                foreach(var b in input)
                {
                    bool ai=Inside(a,side),bi=Inside(b,side);
                    if(ai!=bi)
                    {
                        float v=side<2?(side==0?rect.xMin:rect.xMax):(side==2?rect.yMin:rect.yMax);
                        float t=side<2?(v-a.x)/(b.x-a.x):(v-a.y)/(b.y-a.y);p.Add(Vector2.Lerp(a,b,t));
                    }
                    if(bi)p.Add(b);a=b;
                }
            }
            return p.ToArray();
            bool Inside(Vector2 v,int side)=>side==0?v.x>=rect.xMin:side==1?v.x<=rect.xMax:side==2?v.y>=rect.yMin:v.y<=rect.yMax;
        }
        public static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
    }
}
