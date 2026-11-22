using System.Collections.Generic;

namespace Hostfall.Simulation.Computers
{
    public sealed partial class ComputerCampaign
    {
        public const string NetworkStation="pc.netdef", NetworkIncidentPath="/home/operator/archive/network_quarantine.log";
        public NetworkDefenseSession Network {get;private set;}
        public NetworkSecuritySystem NetworkSecurity {get;private set;}
        public bool NetworkCommandOnline=>!CameraIsolated&&Network.Stage==0&&!Network.IsIsolated(NetworkDefenseSession.CommandNode);
        private void InitializeNetwork()
        { Network=new NetworkDefenseSession();NetworkSecurity=new NetworkSecuritySystem();BindNetwork(); }
        private void BindNetwork()
        {
            Network.StageCompleted+=AwardNetworkStage;Network.Changed+=()=>Changed?.Invoke();
        }
        public bool BeginNetwork()
        {
            if(!FirstTaskComplete)return false;
            NetworkSecurity.Start();Network.Start();return true;
        }
        private void AwardNetworkStage(int stage)
        {
            string receipt=NetworkStation+"/"+stage;if(!receipts.Add(receipt))return;
            NetworkSecurity.EarnStage(stage);
            var emitted=new List<WorldActionData>();
            void Add(WorldActionKind kind,string target)
            { var action=new WorldActionData{receipt=receipt,kind=kind,target=target};actions.Add(action);emitted.Add(action); }
            Add(WorldActionKind.NetworkControl,stage==1?"server.command":stage==2?"server.outage":"server.reinforcements");
            if(stage==3)
            {
                Add(WorldActionKind.UnlockDoor,"door.sec02.vault");Add(WorldActionKind.UnlockDoor,"door.sec03.energy");
                Home.WriteFile(NetworkIncidentPath,"NORTHLINE / КАРАНТИН СЕТИ / 0317\n\nОбщий командный канал закрыт инженером Левиным до объявления тревоги.\nЛокальные роботы продолжают изоляцию по собственным датчикам: их контроллеры не подчиняются SUPER.\nПосле восстановления шины обнаружены фрагменты идентичности оператора, передаваемые в энергоцентр.\nПерсонал не покидал комплекс через грузовой узел. Запрос на эвакуацию подписан позже фактической потери связи.\n");
                messages.Add(new HomeMessageData{id=receipt,path=NetworkIncidentPath,text="Найден журнал карантина охранной сети."});
            }
            UpdateStatus();
            foreach(var action in emitted)WorldAction?.Invoke(Clone(action));Changed?.Invoke();
        }
    }
}
