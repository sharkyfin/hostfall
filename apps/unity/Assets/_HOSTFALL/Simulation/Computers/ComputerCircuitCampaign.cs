namespace Hostfall.Simulation.Computers
{
    public sealed partial class ComputerCampaign
    {
        public const string CircuitStation="pc.circuit",CircuitIncidentPath="/home/operator/archive/power_restore.log";
        public CircuitSession Circuit{get;private set;}
        public bool CircuitAccessible=>CameraIsolated;
        private void InitializeCircuit(){Circuit=new CircuitSession();BindCircuit();}
        private void BindCircuit(){Circuit.CommissionedOnce+=AwardCircuit;Circuit.Changed+=()=>Changed?.Invoke();}
        private void AwardCircuit()
        {
            const string receipt=CircuitStation+"/1";if(!receipts.Add(receipt))return;
            var action=new WorldActionData{receipt=receipt,kind=WorldActionKind.PowerDistribution,target="energy.bus"};actions.Add(action);
            Home.WriteFile(CircuitIncidentPath,"POWER DISTRIBUTION / журнал 0317\n\nЛевин: секции отключены вручную, чтобы остановить перенос сигнатуры оператора.\nРезерв не рассчитан на весь комплекс. Освещение и вентиляция остаются локальными; общий обход подаёт питание на глубинные узлы.\nSUPER зарегистрировал восстановление связи раньше, чем инженер коснулся пульта.\nСледующая контрольная точка: квантовый ретранслятор. Доступ откроется, пока главный шлюз получает питание.\n");
            messages.Add(new HomeMessageData{id=receipt,path=CircuitIncidentPath,text="Восстановлен журнал распределения энергии."});
            WorldAction?.Invoke(Clone(action));Changed?.Invoke();
        }
    }
}
