namespace Hostfall.Simulation.Computers
{
    public sealed partial class ComputerCampaign
    {
        public const string SignalStation="pc.signal",SignalIncidentPath="/home/operator/archive/phase_echo.log";
        public SignalSession Signal{get;private set;}
        public SignalEquipment SignalPlant{get;private set;}
        public bool SignalAccessible=>Circuit.Commissioned;
        private void InitializeSignal(){Signal=new SignalSession();SignalPlant=new SignalEquipment();BindSignal();}
        private void BindSignal()
        {
            Signal.StageCompleted+=AwardSignalStage;Signal.FinalLock+=SignalPlant.OpenBarrier;Signal.Failed+=SignalPlant.Fail;
            SignalPlant.Overheated+=Signal.Fail;Signal.Changed+=()=>Changed?.Invoke();SignalPlant.Changed+=()=>Changed?.Invoke();
        }
        public bool BeginSignal()
        {
            if(!SignalAccessible||Signal.Phase==SignalPhase.Running||!SignalPlant.Start())return false;
            return Signal.Start();
        }
        private void AwardSignalStage(int stage)
        {
            string receipt=SignalStation+"/"+stage;if(!receipts.Add(receipt))return;
            SignalPlant.Calibrate(stage);
            var action=new WorldActionData{receipt=receipt,kind=WorldActionKind.SignalControl,target="quantum.loop."+stage};actions.Add(action);
            if(stage==3)
            {
                Home.WriteFile(SignalIncidentPath,"PHASE ECHO / журнал 0317\n\n03:21 / Ретранслятор ответил на сигнатуру оператора. Образец совпадает с копией из грузового хранилища.\nЛевин: проверьте время подписи. Идентификатор продолжал отправлять команды после отключения рабочих мест.\nSUPER пометил это как восстановление служебного доступа. Контроллер карантина пометил ту же операцию как вторжение.\nПеремычка логистики остаётся временной. QNT-01 может открыть новое окно после закрытия поля.\n");
                messages.Add(new HomeMessageData{id=receipt,path=SignalIncidentPath,text="Восстановлена запись фазового эха оператора."});
            }
            if (stage == 3) PublishRecoveryDossier();
            UpdateStatus();WorldAction?.Invoke(Clone(action));Changed?.Invoke();
        }
    }
}
