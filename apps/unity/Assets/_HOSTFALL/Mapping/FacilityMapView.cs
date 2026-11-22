using System.Collections.Generic;
using System.Linq;
using Hostfall.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace Hostfall.Mapping
{
    public sealed class FacilityMapView : VisualElement
    {
        public FacilityMapWorld World { get; }
        public bool Radar { get; }
        public int Level { get; private set; }
        public event System.Action<int> LevelChanged;
        public Vector2 Center { get; private set; }
        public float Zoom { get; private set; }=14;
        public Vector3 PlayerPosition { get; private set; }
        private float yaw;
        private Vector2 lastPointer;
        private bool dragging;
        private readonly Dictionary<MapRoom,Label> labels=new();
        private readonly List<Vector3> threats=new();
        private readonly List<Rect> occupied=new();private readonly List<MapRoom> visibleRooms=new();
        private readonly Dictionary<(MapShape,MapRoom),(int revision,Rect rect,Vector2[] points)> clipped=new();
        private Vector2[] ClipCached(MapShape shape,MapRoom room,Rect rect)
        {
            var key=(shape,room);
            if(clipped.TryGetValue(key,out var entry)&&entry.revision==shape.Revision&&entry.rect==rect)return entry.points;
            var points=MapGeometry.Clip(shape.Footprint(room.Height),rect);clipped[key]=(shape.Revision,rect,points);return points;
        }
        public FacilityMapView(FacilityMapWorld world,bool radar=false)
        {
            World=world;Radar=radar;name=radar?"radar-canvas":"map-canvas";
            AddToClassList(radar?"radar-canvas":"map-canvas");style.overflow=Overflow.Hidden;
            generateVisualContent+=Draw;
            foreach(var room in World.Rooms)
            {
                var label=new Label{pickingMode=PickingMode.Ignore};label.AddToClassList("map-room-label");
                labels.Add(room,label);Add(label);
            }
            RegisterCallback<GeometryChangedEvent>(_=>UpdateLabels());
            if(!Radar)
            {
                RegisterCallback<WheelEvent>(e=> {ZoomAt(e.localMousePosition,Mathf.Pow(1.14f,-e.delta.y));e.StopPropagation();});
                RegisterCallback<PointerDownEvent>(e=>{if(e.button!=0)return;dragging=true;lastPointer=e.localPosition;this.CapturePointer(e.pointerId);e.StopPropagation();});
                RegisterCallback<PointerMoveEvent>(e=>{if(!dragging)return;var p=(Vector2)e.localPosition;var delta=p-lastPointer;Center+=new Vector2(-delta.x,delta.y)/Zoom;lastPointer=p;Changed();});
                RegisterCallback<PointerUpEvent>(e=>{dragging=false;this.ReleasePointer(e.pointerId);});
                RegisterCallback<PointerCaptureOutEvent>(_=>dragging=false);
            }
            else pickingMode=PickingMode.Ignore;
        }
        public void SetLevel(int level){Level=level;Changed();LevelChanged?.Invoke(level);}
        public void FocusPlayer()
        {
            Center=new Vector2(PlayerPosition.x,PlayerPosition.z);Zoom=14;
            SetLevel(World.At(PlayerPosition)?.Level??0);
        }
        public void FitAll()
        {
            var rooms=World.Rooms.Where(r=>r.Level==Level).ToArray();if(rooms.Length==0)return;
            var bounds=Rect.MinMaxRect(rooms.Min(r=>r.Bounds.xMin),rooms.Min(r=>r.Bounds.yMin),rooms.Max(r=>r.Bounds.xMax),rooms.Max(r=>r.Bounds.yMax));
            Center=bounds.center;
            Zoom=Mathf.Max(1,Mathf.Min((contentRect.width-64)/bounds.width,(contentRect.height-64)/bounds.height));Changed();
        }
        public void ZoomBy(float factor)=>ZoomAt(contentRect.center,factor);
        private void ZoomAt(Vector2 point,float factor)
        {
            var anchor=ToWorld(point);Zoom=Mathf.Clamp(Zoom*factor,1,50);
            Center+=anchor-ToWorld(point);Changed();
        }
        public Vector2 ToScreen(Vector2 world)=>contentRect.center+new Vector2(world.x-Center.x,Center.y-world.y)*Zoom;
        public Vector2 ToWorld(Vector2 screen)=>Center+new Vector2(screen.x-contentRect.center.x,contentRect.center.y-screen.y)/Zoom;
        public void Refresh(Vector3 player,float heading)
        {
            PlayerPosition=player;yaw=heading;
            if(Radar){Center=new Vector2(player.x,player.z);Zoom=contentRect.width/32;Level=World.At(player)?.Level??0;}
            threats.Clear();
            if(Radar)foreach(var enemy in World.Threats)
                if(World.CanSeeThreat(enemy,player+Vector3.up*1.4f)&&World.KnownAt(new Vector3(enemy.transform.position.x,player.y,enemy.transform.position.z),Level))threats.Add(enemy.transform.position);
            Changed();
        }
        private void Changed(){UpdateLabels();MarkDirtyRepaint();}
        private void UpdateLabels()
        {
            if(contentRect.width<1||Zoom<=0)return;
            occupied.Clear();
            var player=ToScreen(new Vector2(PlayerPosition.x,PlayerPosition.z));occupied.Add(new Rect(player.x-13,player.y-13,26,26));
            foreach(var pair in labels)
            {
                var room=pair.Key;var label=pair.Value;
                bool known=World.Exploration.HasVisited(room.Id);
                label.style.display=DisplayStyle.None;
                if(room.Level!=Level || Radar&&known)continue;
                var point=ToScreen(room.Bounds.center);
                float width=known?Mathf.Clamp(room.Bounds.width*Zoom-10,80,144):22;
                float height=known?36:26;
                if(Radar&&room.Bounds.width*Zoom<18)continue;
                var center=new Rect(point.x-width/2,point.y-height/2,width,height);
                Rect rect=default;
                for(int candidate=0;candidate<(known?5:1);candidate++)
                {
                    var check=candidate==0?center:candidate==1?new Rect(ToScreen(new Vector2(room.Bounds.xMax,room.Bounds.center.y)).x+6,center.y,width,height)
                        :candidate==2?new Rect(ToScreen(new Vector2(room.Bounds.xMin,room.Bounds.center.y)).x-width-6,center.y,width,height)
                        :candidate==3?new Rect(center.x,ToScreen(new Vector2(room.Bounds.center.x,room.Bounds.yMax)).y-height-6,width,height)
                        :new Rect(center.x,ToScreen(new Vector2(room.Bounds.center.x,room.Bounds.yMin)).y+6,width,height);
                    if(!contentRect.Contains(check.min)||!contentRect.Contains(check.max))continue;
                    bool overlaps=false;foreach(var used in occupied)if(used.Overlaps(check)){overlaps=true;break;}
                    if(!overlaps){rect=check;break;}
                }
                if(rect.width==0)continue;
                occupied.Add(rect);label.text=known?room.Name:"?";
                label.style.left=rect.x;label.style.top=rect.y;label.style.width=width;label.style.height=height;
                label.EnableInClassList("map-unknown-label",!known);label.style.display=DisplayStyle.Flex;
            }
        }
        private void Draw(MeshGenerationContext ctx)
        {
            if(contentRect.width<1||Zoom<=0)return;
            var painter=ctx.painter2D;
            var visible=Rect.MinMaxRect(ToWorld(new Vector2(0,contentRect.height)).x,ToWorld(new Vector2(0,contentRect.height)).y,ToWorld(new Vector2(contentRect.width,0)).x,ToWorld(new Vector2(contentRect.width,0)).y);
            // Shared world coordinates: north remains up on the radar and full map.
            float step=Radar?4:10;
            for(float x=Mathf.Floor(visible.xMin/step)*step;x<=visible.xMax;x+=step)Line(new Vector2(x,visible.yMin),new Vector2(x,visible.yMax),new Color(.10f,.15f,.18f,.4f));
            for(float z=Mathf.Floor(visible.yMin/step)*step;z<=visible.yMax;z+=step)Line(new Vector2(visible.xMin,z),new Vector2(visible.xMax,z),new Color(.10f,.15f,.18f,.4f));
            visibleRooms.Clear();foreach(var r in World.Rooms)if(r.Level==Level&&r.Bounds.Overlaps(visible))visibleRooms.Add(r);var rooms=visibleRooms;
            foreach(var room in rooms)
            {
                bool known=World.Exploration.HasVisited(room.Id);
                foreach(var floor in room.Floors)
                    Polygon(floor.Footprint(room.Height,true),known?new Color(.09f,.18f,.22f):new Color(.022f,.036f,.05f));
                if(!known)Outline(room.Bounds,new Color(.13f,.19f,.23f));
            }
            foreach(var room in rooms)
            {
                if(!World.Exploration.HasVisited(room.Id))continue;
                var clip=room.Bounds;clip.xMin-=.35f;clip.xMax+=.35f;clip.yMin-=.35f;clip.yMax+=.35f;
                foreach(var shape in World.Shapes)
                {
                    if(shape.Floor||!shape.Active||!shape.HorizontalBounds.Overlaps(clip))continue;
                    var polygon=ClipCached(shape,room,clip);
                    if(polygon.Length<3)continue;
                    Polygon(polygon,shape.Wall?new Color(.29f,.39f,.44f):shape.Stair?new Color(.19f,.32f,.36f):new Color(.15f,.25f,.28f),new Color(.35f,.46f,.49f),.6f);
                }
            }
            foreach(var marker in World.Markers)
            {
                if(!marker.Active||!World.KnownAt(marker.Position,Level,marker.Kind=="door"?.45f:0))continue;
                var p=ToScreen(new Vector2(marker.Position.x,marker.Position.z));if(!contentRect.Contains(p))continue;
                if(marker.Kind=="door")
                {
                    bool open=((FacilityDoor)marker.Source).IsUnlocked;
                    Diamond(p,Radar?2.5f:4,open?new Color(.37f,.73f,.57f):new Color(.90f,.64f,.33f));
                }
                else if(marker.Kind=="terminal")Circle(p,Radar?2:3.5f,new Color(.30f,.77f,.88f));
                else Diamond(p,Radar?2:3,new Color(.85f,.76f,.49f));
            }
            foreach(var threat in threats)Circle(ToScreen(new Vector2(threat.x,threat.z)),3,new Color(.95f,.38f,.31f));
            if((World.At(PlayerPosition)?.Level??0)==Level)
            {
                var p=ToScreen(new Vector2(PlayerPosition.x,PlayerPosition.z));float a=yaw*Mathf.Deg2Rad;
                var forward=new Vector2(Mathf.Sin(a),-Mathf.Cos(a));var right=new Vector2(-forward.y,forward.x);
                painter.fillColor=new Color(.65f,.94f,.95f);painter.BeginPath();painter.MoveTo(p+forward*8);painter.LineTo(p-forward*5+right*4.5f);painter.LineTo(p-forward*3);painter.LineTo(p-forward*5-right*4.5f);painter.ClosePath();painter.Fill();
            }
            void Line(Vector2 a,Vector2 b,Color color){painter.strokeColor=color;painter.lineWidth=1;painter.BeginPath();painter.MoveTo(ToScreen(a));painter.LineTo(ToScreen(b));painter.Stroke();}
            void Outline(Rect r,Color color)
            {Line(r.min,new Vector2(r.xMax,r.yMin),color);Line(new Vector2(r.xMax,r.yMin),r.max,color);Line(r.max,new Vector2(r.xMin,r.yMax),color);Line(new Vector2(r.xMin,r.yMax),r.min,color);}
            void Polygon(Vector2[] polygon,Color fill,Color stroke=default,float width=0)
            {
                if(polygon.Length<3)return;
                painter.BeginPath();painter.MoveTo(ToScreen(polygon[0]));foreach(var v in polygon.Skip(1))painter.LineTo(ToScreen(v));painter.ClosePath();painter.fillColor=fill;painter.Fill();
                if(width>0){painter.strokeColor=stroke;painter.lineWidth=width;painter.Stroke();}
            }
            void Circle(Vector2 p,float radius,Color color){painter.BeginPath();painter.Arc(p,radius,0,360);painter.ClosePath();painter.fillColor=color;painter.Fill();}
            void Diamond(Vector2 p,float size,Color color)
            {painter.BeginPath();painter.MoveTo(p+Vector2.up*size);painter.LineTo(p+Vector2.right*size);painter.LineTo(p+Vector2.down*size);painter.LineTo(p+Vector2.left*size);painter.ClosePath();painter.fillColor=color;painter.Fill();}
        }
    }
}
