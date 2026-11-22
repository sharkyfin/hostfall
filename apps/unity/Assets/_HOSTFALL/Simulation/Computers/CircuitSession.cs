using System;
using System.Collections.Generic;
using System.Linq;

namespace Hostfall.Simulation.Computers
{
    public enum CircuitCellKind { Empty, Wire, Relay, Source, Load }
    public enum CircuitLoad { MainDoor, Lighting, Turret, Ventilation, Shortcut }
    [Serializable] public sealed class CircuitData
    {
        public int version=1,selected;
        public int[] rotations;
        public bool[] relays,sources;
        public bool energized,tripped,commissioned,started;
        public double cooldown,turretClock,ventOpening;
    }
    // A reciprocal-contact electrical graph. Capacity is shared only inside a connected component.
    // Editing is break-before-make; a trip cuts all outputs and never auto-resets or auto-solves.
    public sealed class CircuitSession
    {
        public const int Width=7,Height=7,Count=Width*Height,North=1,East=2,South=4,West=8;
        private static readonly int[] outputRows={0,1,2,4,6},demands={3,2,2,2,4};
        private static readonly int[] sourceCells={14,42};
        private static readonly int[] capacities={6,3};
        private readonly int[] rotations=new int[Count],components=new int[Count],componentCapacity=new int[Count],componentLoad=new int[Count];
        private readonly bool[] relays=new bool[Count],sources=new bool[2];
        private readonly int[] queue=new int[Count];
        public int Selected{get;private set;}
        public bool Started{get;private set;}
        public bool Energized{get;private set;}
        private double turretClock;
        public double VentOpening{get;private set;}
        public bool Tripped{get;private set;}
        public bool Commissioned{get;private set;}
        public double Cooldown{get;private set;}
        public event Action Changed,CommissionedOnce,TurretPulseRequested;
        public CircuitSession()
        { for(int i=0;i<Count;i++)if(Rotatable(i))rotations[i]=(i*3+1)%4;Evaluate(); }
        public static int OutputCell(CircuitLoad load)=>ValidLoad(load)?outputRows[(int)load]*Width+6:-1;
        public static int Demand(CircuitLoad load)=>ValidLoad(load)?demands[(int)load]:0;
        public static string LoadName(CircuitLoad load)=>load switch
        {CircuitLoad.MainDoor=>"Главный шлюз",CircuitLoad.Lighting=>"Освещение",CircuitLoad.Turret=>"Защитная турель",CircuitLoad.Ventilation=>"Вентиляция",CircuitLoad.Shortcut=>"Короткий путь",_=>""};
        private static bool ValidLoad(CircuitLoad load)=>(int)load>=0&&(int)load<5;
        public static CircuitCellKind Kind(int cell)
        {
            if(cell<0||cell>=Count)return CircuitCellKind.Empty;
            int x=cell%Width,y=cell/Width;
            if(x==0&&(y==2||y==6))return CircuitCellKind.Source;
            if(x==1&&(y==2||y==6)||x==2&&y!=3)return CircuitCellKind.Wire;
            if(x==2&&y==3)return CircuitCellKind.Relay;
            if(!outputRows.Contains(y)||x<3)return CircuitCellKind.Empty;
            return x==6?CircuitCellKind.Load:x==4?CircuitCellKind.Relay:CircuitCellKind.Wire;
        }
        public static bool Rotatable(int cell)=>Kind(cell) is CircuitCellKind.Wire or CircuitCellKind.Relay;
        private static int BaseMask(int cell)
        {
            int x=cell%Width,y=cell/Width;
            if(Kind(cell)==CircuitCellKind.Empty)return 0;
            if(x==0)return East;if(x==6)return West;
            if(x!=2)return East|West;
            return y switch {0=>East|South,1 or 4=>North|East|South,2=>15,3 or 5=>North|South,6=>North|East|West,_=>0};
        }
        public int Rotation(int cell)=>cell>=0&&cell<Count?rotations[cell]:0;
        public int Mask(int cell)
        {
            if(cell<0||cell>=Count)return 0;int mask=BaseMask(cell);
            for(int i=0;i<rotations[cell];i++)mask=((mask<<1)&15)|(mask>>3);
            return mask;
        }
        public bool RelayClosed(int cell)=>cell>=0&&cell<Count&&Kind(cell)==CircuitCellKind.Relay&&relays[cell];
        public bool SourceEnabled(int source)=>source>=0&&source<2&&sources[source];
        public static int SourceCell(int source)=>source>=0&&source<2?sourceCells[source]:-1;
        public static int SourceCapacity(int source)=>source>=0&&source<2?capacities[source]:0;
        public bool Select(int cell)
        {if(Kind(cell)==CircuitCellKind.Empty)return false;Selected=cell;Changed?.Invoke();return true;}
        public bool Rotate(int cell)
        {if(!Rotatable(cell))return false;Started=true;Energized=false;turretClock=0;rotations[cell]=(rotations[cell]+1)%4;Recompute();return true;}
        public bool ToggleRelay(int cell)
        {if(Kind(cell)!=CircuitCellKind.Relay)return false;Started=true;Energized=false;turretClock=0;relays[cell]=!relays[cell];Recompute();return true;}
        public bool ToggleSource(int source)
        {if(source<0||source>=2)return false;Started=true;Energized=false;turretClock=0;sources[source]=!sources[source];Recompute();return true;}
        public bool Powered(CircuitLoad load)=>Energized&&!Tripped&&Available(OutputCell(load));
        public bool Available(int cell)=>cell>=0&&cell<Count&&components[cell]>=0&&componentCapacity[components[cell]]>0;
        public int ConnectedCapacity(int cell)=>cell>=0&&cell<Count&&components[cell]>=0?componentCapacity[components[cell]]:0;
        public int ConnectedDemand(int cell)=>cell>=0&&cell<Count&&components[cell]>=0?componentLoad[components[cell]]:0;
        public bool Overloaded(int cell)=>Available(cell)&&ConnectedDemand(cell)>ConnectedCapacity(cell);
        public bool HasOverload=>Enumerable.Range(0,Count).Any(Overloaded);
        public bool Apply()
        {
            if(Tripped||Cooldown>0)return false;
            Started=true;Evaluate();
            if(HasOverload){Energized=false;Tripped=true;Cooldown=2;Changed?.Invoke();return false;}
            Energized=true;
            if(Powered(CircuitLoad.MainDoor)&&!Commissioned)
            {Commissioned=true;CommissionedOnce?.Invoke();}
            Changed?.Invoke();return true;
        }
        internal bool ApplyServicePreset(bool light)
        {
            if (Cooldown > 0) return false;
            if (Tripped) ResetTrip();
            if (light || !Powered(CircuitLoad.Turret)) turretClock = 0;

            // The service switch selects either the lit maintenance circuit or the local turret.
            // Zero rotation is the factory alignment of the hardwired distribution bus.
            Array.Clear(rotations, 0, rotations.Length);
            Array.Clear(relays, 0, relays.Length);
            relays[4] = true;
            relays[(light ? 1 : 2) * Width + 4] = true;
            sources[0] = true;
            sources[1] = light;
            relays[3 * Width + 2] = light;
            relays[6 * Width + 4] = light;
            return Apply();
        }

        public void Disconnect(){if(!Energized)return;Energized=false;turretClock=0;Changed?.Invoke();}
        public bool ResetTrip()
        {if(!Tripped||Cooldown>0)return false;Tripped=false;Changed?.Invoke();return true;}
        public void AdvanceWorld(double seconds)
        {
            if(!Finite(seconds)||seconds<=0)return;seconds=Math.Min(.25,seconds);
            double desiredVent=Powered(CircuitLoad.Ventilation)?1:0;
            VentOpening=desiredVent>VentOpening?Math.Min(desiredVent,VentOpening+seconds/1.5):Math.Max(desiredVent,VentOpening-seconds/1.5);
            if(Cooldown>0){Cooldown=Math.Max(0,Cooldown-seconds);if(Cooldown<1e-12)Cooldown=0;Changed?.Invoke();}
            if(Powered(CircuitLoad.Turret))
            {turretClock+=seconds;if(turretClock+1e-12>=2.5){turretClock=Math.Max(0,turretClock-2.5);TurretPulseRequested?.Invoke();}}
        }
        private void Recompute(){Evaluate();Changed?.Invoke();}
        private bool Conducts(int cell)=>Kind(cell)!=CircuitCellKind.Empty&&(Kind(cell)!=CircuitCellKind.Relay||relays[cell]);
        private void Evaluate()
        {
            Array.Fill(components,-1);Array.Clear(componentCapacity,0,Count);Array.Clear(componentLoad,0,Count);
            for(int root=0;root<Count;root++)
            {
                if(!Conducts(root)||components[root]>=0)continue;
                int head=0,tail=0;queue[tail++]=root;components[root]=root;
                while(head<tail)
                {
                    int cell=queue[head++],x=cell%Width,y=cell/Width,mask=Mask(cell);
                    for(int direction=0;direction<4;direction++)
                    {
                        int bit=1<<direction;if((mask&bit)==0)continue;
                        int nx=x+(direction==1?1:direction==3?-1:0),ny=y+(direction==2?1:direction==0?-1:0);
                        if(nx<0||nx>=Width||ny<0||ny>=Height)continue;
                        int next=ny*Width+nx;
                        if(components[next]>=0||!Conducts(next)||(Mask(next)&(1<<((direction+2)%4)))==0)continue;
                        components[next]=root;queue[tail++]=next;
                    }
                }
                for(int i=0;i<2;i++)if(sources[i]&&components[sourceCells[i]]==root)componentCapacity[root]+=capacities[i];
                for(int i=0;i<5;i++)if(components[OutputCell((CircuitLoad)i)]==root)componentLoad[root]+=demands[i];
            }
        }
        private static bool Finite(double n)=>!double.IsNaN(n)&&!double.IsInfinity(n);
        public CircuitData Capture()=>new(){selected=Selected,rotations=(int[])rotations.Clone(),relays=(bool[])relays.Clone(),sources=(bool[])sources.Clone(),
            energized=Energized,tripped=Tripped,commissioned=Commissioned,started=Started,cooldown=Cooldown,turretClock=turretClock,ventOpening=VentOpening};
        public static CircuitSession Restore(CircuitData data)
        {
            SaveValidation.Require(data!=null&&data.version==1&&data.rotations?.Length==Count&&data.relays?.Length==Count&&data.sources?.Length==2,"Invalid circuit board");
            SaveValidation.Require(data.selected>=0&&data.selected<Count&&Finite(data.cooldown)&&data.cooldown>=0&&data.cooldown<=2&&
                Finite(data.ventOpening)&&data.ventOpening>=0&&data.ventOpening<=1&&Finite(data.turretClock)&&data.turretClock>=0&&data.turretClock<2.5&&
                (!data.energized||!data.tripped)&& (data.tripped||data.cooldown==0)&&(!data.energized||data.started)&&(!data.commissioned||data.started)&&(!data.tripped||data.started),"Invalid breaker state");
            var game=new CircuitSession();
            for(int i=0;i<Count;i++)SaveValidation.Require(data.rotations[i]>=0&&data.rotations[i]<4&&(Rotatable(i)||data.rotations[i]==0)&&
                (Kind(i)==CircuitCellKind.Relay||!data.relays[i]),"Invalid circuit contact");
            Array.Copy(data.rotations,game.rotations,Count);Array.Copy(data.relays,game.relays,Count);Array.Copy(data.sources,game.sources,2);
            game.Selected=data.selected;game.Started=data.started;game.VentOpening=data.ventOpening;game.turretClock=data.turretClock;game.Energized=data.energized;game.Tripped=data.tripped;game.Commissioned=data.commissioned;game.Cooldown=data.cooldown;game.Evaluate();
            SaveValidation.Require(!game.Energized||!game.HasOverload,"Energized overloaded circuit");
            SaveValidation.Require(!game.Powered(CircuitLoad.MainDoor)||game.Commissioned,"Missing circuit commission");
            SaveValidation.Require(game.Powered(CircuitLoad.Turret)||game.turretClock==0,"Inactive turret clock");
            if(!game.Started){var initial=new CircuitSession();SaveValidation.Require(game.rotations.SequenceEqual(initial.rotations)&&!game.relays.Any(v=>v)&&!game.sources.Any(v=>v),"Modified unstarted circuit");}
            return game;
        }
    }
}
