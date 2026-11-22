using System;
using System.Collections.Generic;
using Hostfall.Core;
using UnityEngine;

namespace Hostfall.World
{
    // Collision/floor-checked local A*. Searches advance through one shared fair work queue.
    public sealed class RobotNavigation
    {
        public const int MaximumSearchChecksPerFrame=96;
        private static readonly Queue<RobotNavigation> pending=new();
        private static readonly System.Diagnostics.Stopwatch clock=new();
        public static int LastSearchChecks {get;private set;}
        private readonly Transform owner;
        private readonly float radius;
        private readonly List<Vector3> path=new();
        private RaycastHit[] hits=new RaycastHit[16];
        private Collider[] overlaps=new Collider[16];
        private float repath;
        private Vector3 lastGoal;
        private bool searching,queued,directPath;
        private Vector3 searchStart,searchGoal;
        private Vector2 boundsX,boundsZ;
        private int width,height,first,best,current=-1,neighbor,expanded,heapCount;
        private int[] previous=new int[400],heap=new int[400],heapPositions=new int[400];
        private float[] cost=new float[400],priority=new float[400];
        public bool Searching=>searching;
        public RobotNavigation(Transform root,float bodyRadius=.32f){owner=root;radius=bodyRadius;}
        public static void ResetSearchQueue(){pending.Clear();LastSearchChecks=0;}
        public static void ProcessSearchFrame()
        {
            LastSearchChecks=0;clock.Restart();
            // Round-robin one candidate edge at a time. A busy bot cannot consume the frame.
            while(pending.Count>0&&LastSearchChecks<MaximumSearchChecksPerFrame&&clock.Elapsed.TotalMilliseconds<1.2)
            {
                var nav=pending.Dequeue();nav.queued=false;
                if(nav.owner==null||!nav.searching)continue;
                if(nav.StepSearch())LastSearchChecks++;
                if(nav.searching)nav.Enqueue();
            }
            clock.Stop();
        }
        private void Enqueue(){if(queued)return;queued=true;pending.Enqueue(this);}
        public bool Visible(Vector3 from,Vector3 to)
        {
            var delta=to-from;
            int count=Ray(from,delta.normalized,delta.magnitude);
            for(int i=0;i<count;i++)if(!hits[i].transform.IsChildOf(owner))return false;
            return true;
        }
        public float RayLimit(Vector3 origin, Vector3 direction, float length)
        {
            int count = Ray(origin, direction, length);
            for (int i = 0; i < count; i++)
                if (!hits[i].transform.IsChildOf(owner))
                    length = Mathf.Min(length, hits[i].distance);
            return length;
        }
        private int Ray(Vector3 origin,Vector3 direction,float length)
        {
            int count;
            while(true)
            {
                count=Physics.RaycastNonAlloc(origin,direction,hits,length,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore);
                if(count<hits.Length)return count;
                if(hits.Length>=1024){hits=Physics.RaycastAll(origin,direction,length,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore);return hits.Length;}
                Array.Resize(ref hits,hits.Length*2);
            }
        }
        private bool Clear(Vector3 a,Vector3 b)
        {
            var delta=b-a;delta.y=0;float length=delta.magnitude;if(length<.0001f)return true;
            int count;
            while(true)
            {
                count=radius>.7f
                    ?Physics.BoxCastNonAlloc(a+Vector3.up*.91f,new Vector3(1.73f,.85f,1.85f),delta/length,hits,owner.rotation,length+.03f,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore)
                    :Physics.CapsuleCastNonAlloc(a+Vector3.up*(radius+.08f),a+Vector3.up*1.05f,radius,delta/length,hits,length+.03f,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore);
                if(count<hits.Length)break;
                if(hits.Length>=1024)
                {
                    hits=radius>.7f?Physics.BoxCastAll(a+Vector3.up*.91f,new Vector3(1.73f,.85f,1.85f),delta/length,owner.rotation,length+.03f,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore)
                        :Physics.CapsuleCastAll(a+Vector3.up*(radius+.08f),a+Vector3.up*1.05f,radius,delta/length,length+.03f,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore);
                    count=hits.Length;break;
                }
                Array.Resize(ref hits,hits.Length*2);
            }
            for(int i=0;i<count;i++)if(!hits[i].transform.IsChildOf(owner))return false;
            // Long shortcuts must not jump across an unsampled floor hole.
            int samples=Mathf.Max(1,Mathf.CeilToInt(length/.65f));
            for(int i=1;i<=samples;i++)if(!Physics.Raycast(Vector3.Lerp(a,b,i/(float)samples)+Vector3.up*.3f,Vector3.down,.65f,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore))return false;
            return true;
        }
        public bool Move(Vector3 goal,float speed,float dt,Vector2 xb,Vector2 zb)
        {
            if(dt<=0||owner==null)return false;
            var p=owner.position;goal=new Vector3(Mathf.Clamp(goal.x,xb.x,xb.y),p.y,Mathf.Clamp(goal.z,zb.x,zb.y));repath-=dt;
            bool goalChanged=(goal-lastGoal).sqrMagnitude>2.25f;
            if(!searching&&(repath<=0||goalChanged))
            {
                lastGoal=goal;repath=.25f;
                if(Clear(p,goal)){path.Clear();path.Add(goal);directPath=true;}
                else {BeginSearch(p,goal,xb,zb);repath=.65f;}
            }
            if(directPath&&!searching&&path.Count==1)path[0]=goal;
            while(path.Count>0&&Vector3.Distance(p,path[0])<(path.Count==1?.06f:.15f))path.RemoveAt(0);
            if(path.Count==0)return false;
            var delta=path[0]-p;delta.y=0;if(delta.sqrMagnitude<.001f)return false;
            if(!Face(delta,dt,radius>.7f?100:300)||Vector3.Angle(owner.forward,delta)>65)return false;
            var next=Vector3.MoveTowards(p,path[0],speed*dt);
            if(!Clear(p,next)){path.Clear();if(!searching){BeginSearch(p,goal,xb,zb);repath=.65f;}return false;}
            owner.position=next;return true;
        }
        public bool DriveStraight(Vector3 direction, float distance, Vector2 xBounds, Vector2 zBounds)
        {
            var from = owner.position;
            var next = from + direction * distance;
            if (next.x < xBounds.x || next.x > xBounds.y || next.z < zBounds.x || next.z > zBounds.y ||
                !Clear(from, next))
                return false;
            owner.position = next;
            return true;
        }
        public bool Face(Vector3 direction,float dt,float rate=100)
        {
            direction.y=0;if(direction.sqrMagnitude<.001f)return true;
            var before=owner.rotation;owner.rotation=Quaternion.RotateTowards(before,Quaternion.LookRotation(direction),rate*dt);
            if(radius>.7f)
            {
                int count;
                while(true)
                {
                    count=Physics.OverlapBoxNonAlloc(owner.position+Vector3.up*.91f,new Vector3(1.73f,.85f,1.85f),overlaps,owner.rotation,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore);
                    if(count<overlaps.Length)break;
                    if(overlaps.Length>=1024){overlaps=Physics.OverlapBox(owner.position+Vector3.up*.91f,new Vector3(1.73f,.85f,1.85f),owner.rotation,CollisionLayers.WorldMask,QueryTriggerInteraction.Ignore);count=overlaps.Length;break;}
                    Array.Resize(ref overlaps,overlaps.Length*2);
                }
                for(int i=0;i<count;i++)if(!overlaps[i].transform.IsChildOf(owner)){owner.rotation=before;return false;}
            }
            return true;
        }
        private Vector3 Position(int n)=>new(Mathf.Lerp(boundsX.x,boundsX.y,(n%width)/(float)(width-1)),searchStart.y,Mathf.Lerp(boundsZ.x,boundsZ.y,(n/width)/(float)(height-1)));
        private int Index(Vector3 p)=>Mathf.Clamp(Mathf.RoundToInt(Mathf.InverseLerp(boundsZ.x,boundsZ.y,p.z)*(height-1)),0,height-1)*width+Mathf.Clamp(Mathf.RoundToInt(Mathf.InverseLerp(boundsX.x,boundsX.y,p.x)*(width-1)),0,width-1);
        private void BeginSearch(Vector3 start,Vector3 goal,Vector2 xb,Vector2 zb)
        {
            directPath=false;searchStart=start;searchGoal=goal;boundsX=xb;boundsZ=zb;
            width=Mathf.Clamp(Mathf.CeilToInt((xb.y-xb.x)/.8f)+1,2,80);height=Mathf.Clamp(Mathf.CeilToInt((zb.y-zb.x)/.8f)+1,2,80);
            int count=width*height;
            if(previous.Length<count)
            {
                int capacity=Mathf.NextPowerOfTwo(count);Array.Resize(ref previous,capacity);Array.Resize(ref heap,capacity);Array.Resize(ref heapPositions,capacity);Array.Resize(ref cost,capacity);Array.Resize(ref priority,capacity);
            }
            for(int i=0;i<count;i++){previous[i]=-2;cost[i]=float.PositiveInfinity;heapPositions[i]=-1;}
            first=Index(start);best=first;current=-1;expanded=0;heapCount=0;cost[first]=0;previous[first]=-1;Push(first,Vector3.Distance(start,goal));searching=true;Enqueue();
        }
        private bool StepSearch()
        {
            if(current>=0&&neighbor>=9)current=-1;
            if(current<0)
            {
                if(heapCount==0||expanded>=2500){FinishSearch();return false;}
                current=Pop();expanded++;neighbor=0;
                if(current==Index(searchGoal)){best=current;FinishSearch();return false;}
            }
            int direction=neighbor++;int dx=direction%3-1,dz=direction/3-1;
            if(dx==0&&dz==0)return false;
            int x=current%width+dx,z=current/width+dz;if(x<0||x>=width||z<0||z>=height)return false;
            int next=z*width+x;var from=current==first?searchStart:Position(current);var to=Position(next);float candidate=cost[current]+Vector3.Distance(from,to);
            if(candidate>=cost[next])return false;
            if(!Clear(from,to))return true;
            cost[next]=candidate;previous[next]=current;float distance=Vector3.Distance(to,searchGoal);
            if(distance<(Position(best)-searchGoal).magnitude)best=next;
            Push(next,candidate+distance);return true;
        }
        private void FinishSearch()
        {
            searching=false;if(best==first)return;
            path.Clear();int limit=0;
            for(int n=best;n!=first&&n>=0&&limit++<6400;n=previous[n])path.Add(Position(n));path.Reverse();
        }
        private void Push(int node,float score)
        {
            priority[node]=score;int index=heapPositions[node];
            if(index<0){index=heapCount++;heap[index]=node;heapPositions[node]=index;}
            while(index>0){int parent=(index-1)/2;if(priority[heap[parent]]<=score)break;Swap(index,parent);index=parent;}
        }
        private int Pop()
        {
            int node=heap[0];heapPositions[node]=-1;heapCount--;
            if(heapCount==0)return node;heap[0]=heap[heapCount];heapPositions[heap[0]]=0;int index=0;
            while(true){int child=index*2+1;if(child>=heapCount)break;if(child+1<heapCount&&priority[heap[child+1]]<priority[heap[child]])child++;if(priority[heap[index]]<=priority[heap[child]])break;Swap(index,child);index=child;}
            return node;
        }
        private void Swap(int a,int b){int node=heap[a];heap[a]=heap[b];heap[b]=node;heapPositions[heap[a]]=a;heapPositions[heap[b]]=b;}
    }
}
