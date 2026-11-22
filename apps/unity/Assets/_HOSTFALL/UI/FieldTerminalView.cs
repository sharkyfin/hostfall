using System;
using Hostfall.Core;
using Hostfall.Simulation.Computers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Hostfall.UI
{
    public sealed partial class ComputerRuntimeUI
    {
        private FieldTerminalView fieldConsole;

        private void BuildFieldConsole(FieldComputer computer)
        {
            fieldConsole = new FieldTerminalView(view, campaign, computer,
                () => active == current && active != null && controls.Mode == ControlMode.Computer && rig.FocusReady,
                PlayFieldRecording);
        }

        private void BuildRecoveryAuthorization()
        {
            var panel = new VisualElement();
            panel.AddToClassList("hf2-service");
            content.Add(panel);
            panel.Add(Text("Личное подтверждение", "hf2-heading"));
            panel.Add(Text("Сверьте локальные записи смены. Ваша подпись запитает кабину и откроет изолированный вход в ядро."));
            var split = new VisualElement();
            split.AddToClassList("hf2-service-split");
            panel.Add(split);
            var list = new ScrollView();
            list.AddToClassList("hf2-service-files");
            split.Add(list);
            var article = new ScrollView();
            article.AddToClassList("hf2-service-content");
            split.Add(article);
            for (int index = 0; index < ComputerCampaign.RecoveryRecords.Count; index++)
            {
                int recordIndex = index;
                var record = ComputerCampaign.RecoveryRecords[index];
                Button(list, record.Title, () =>
                {
                    if (!rig.FocusReady || !campaign.TryReadRecoveryRecord(recordIndex, out var evidence)) return;
                    article.Clear();
                    var evidenceLabel = Text(evidence);
                    evidenceLabel.name = "recovery-evidence";
                    article.Add(evidenceLabel);
                }, "recovery-record-" + index);
            }

            var status = Text("");
            status.name = "recovery-status";
            panel.Add(status);
            var sign = Button(panel, "Подписать допуск", () =>
            {
                if (rig.FocusReady && campaign.AuthorizeRecovery()) refreshRecovery?.Invoke();
            }, "recovery-authorize");
            refreshRecovery = () =>
            {
                sign.SetEnabled(campaign.RecoveryAccessible && !campaign.RecoveryAuthorized);
                status.text = campaign.RecoveryAuthorized ? "Допуск подписан. Кабина готова к отправлению."
                    : campaign.RecoveryAccessible ? "Локальные свидетельства сверены."
                    : "Откройте шесть записей для сверки.";
            };
            refreshRecovery();
        }
    }

    internal sealed class FieldTerminalView
    {
        private readonly ComputerCampaign campaign;
        private readonly FieldComputer computer;
        private readonly FieldCase dossier;
        private readonly Func<bool> canOperate;
        private readonly Action playRecording;
        private readonly VisualElement root, tabs;
        private readonly ScrollView files, reader;
        private readonly Label status;
        private VisualElement caseControls;
        private Label equipment;
        private int suspect = -1, evidence;

        public FieldTerminalView(VisualElement parent, ComputerCampaign campaign, FieldComputer computer, Func<bool> canOperate, Action playRecording)
        {
            this.campaign = campaign;
            this.computer = computer;
            this.canOperate = canOperate;
            this.playRecording = playRecording;
            dossier = FieldCase.Find(computer.Station);
            root = new VisualElement { name = "field-terminal" };
            root.AddToClassList("hf2-service");
            parent.Add(root);
            tabs = new VisualElement();
            tabs.AddToClassList("hf2-service-tabs");
            root.Add(tabs);
            AddButton(tabs, "Файлы", "field-files", () => Show("files"));
            if (dossier != null) AddButton(tabs, dossier.Title, "field-case", () => Show("case"));
            if (computer.Utilities.Count > 0) AddButton(tabs, "Знания", "field-knowledge", () => Show("knowledge"));

            var split = new VisualElement();
            split.AddToClassList("hf2-service-split");
            root.Add(split);
            files = new ScrollView();
            files.AddToClassList("hf2-service-files");
            split.Add(files);
            reader = new ScrollView();
            reader.AddToClassList("hf2-service-content");
            split.Add(reader);
            status = Label("", "hf2-service-status");
            root.Add(status);
            root.schedule.Execute(RefreshEquipment).Every(200);
            Show(dossier == null ? "files" : "case");
        }

        private void Show(string screen)
        {
            foreach (var button in tabs.Query<Button>().ToList())
                button.EnableInClassList("hf2-service-selected", button.name == "field-" + screen);
            files.Clear();
            ClearReader();
            status.text = "";

            if (screen == "case")
            {
                BuildCase();
                return;
            }
            if (screen == "knowledge")
            {
                foreach (var utility in computer.Utilities)
                    AddButton(files, utility.Name, "field-help-" + utility.Id, () => ReadUtility(utility, true));
                ReadUtility(computer.Utilities[0], true);
                return;
            }

            for (int i = 0; i < computer.Files.Count; i++)
            {
                var file = computer.Files[i];
                AddButton(files, file.Name, "field-file-" + i, () => ReadFile(file));
            }
            foreach (var utility in computer.Utilities)
                AddButton(files, utility.Name, "field-script-" + utility.Id, () => ReadUtility(utility, false));
            if (computer.Utilities.Count > 0)
                reader.Add(Label("Выберите файл. Команды с расширением .svc можно запустить после просмотра. Их действие описано во вкладке «Знания»."));
            else
                ReadFile(computer.Files[0]);
        }

        private void ReadFile(FieldFile file)
        {
            ClearReader();
            reader.Add(Label(file.Name, "hf2-heading"));
            reader.Add(Label(file.Content));
        }

        private void ReadUtility(FieldUtility utility, bool knowledge)
        {
            ClearReader();
            reader.Add(Label(utility.Name, "hf2-heading"));
            if (knowledge)
            {
                reader.Add(Label(utility.Help));
                return;
            }

            reader.Add(Label(utility.Source, "hf2-service-code"));
            AddButton(reader, "Выполнить", "field-run-" + utility.Id,
                () => status.text = campaign.ExecuteFieldUtility(computer.Station, utility.Id));
            if (utility.Id is FieldUtilityId.QuarantineRecall or FieldUtilityId.QuarantineSeal)
            {
                equipment = Label("");
                reader.Add(equipment);
                RefreshEquipment();
            }
        }

        private void RefreshEquipment()
        {
            if (equipment == null) return;
            var session = campaign.Containment;
            var sensors = session.Sensors;
            equipment.text = session.Complete ? "Контур изолирован" :
                session.CanIsolate ? "Датчики подтвердили изоляцию" : sensors.PlayerInside ? "В камере человек" :
                sensors.SouthBlocked || sensors.NorthBlocked ? "Проём занят" : sensors.Zone switch
                {
                    SentryZone.Bay => "R26 в камере",
                    SentryZone.Offline => "R26 отключён",
                    _ => "Камера пуста"
                };
        }

        private void BuildCase()
        {
            if (!campaign.CaseAvailable(computer.Station))
            {
                reader.Add(Label("Материалы заперты локальным допуском."));
                return;
            }
            for (int i = 0; i < dossier.Evidence.Count; i++)
            {
                int index = i;
                AddButton(files, dossier.Evidence[i].Name, "field-evidence-" + i, () => ReadEvidence(index));
            }
            AddButton(files, "Заключение", "field-verdict", ShowVerdict);
            ShowVerdict();
        }

        private void ReadEvidence(int index)
        {
            if (!campaign.ReadCaseEvidence(computer.Station, index)) return;
            ReadFile(dossier.Evidence[index]);
            if (dossier == FieldCase.Camera && index == 0)
                AddCameraFrames(reader);
            else if (dossier == FieldCase.Voice && index == 0)
                AddButton(reader, "Прослушать 03:17", "field-play-voice", playRecording);
            else if (dossier == FieldCase.Voice && index == 1 && NeuroArchiveMedia.Picture != null)
                reader.Add(new Image { image = NeuroArchiveMedia.Picture, uv = NeuroArchiveMedia.Frame(9),
                    scaleMode = ScaleMode.ScaleToFit, style = { height = 224, flexShrink = 0 } });
        }

        private void ShowVerdict()
        {
            ClearReader();
            reader.Add(Label(dossier.Title, "hf2-heading"));
            reader.Add(Label(dossier.Question));
            caseControls = new VisualElement();
            reader.Add(caseControls);
            for (int i = 0; i < dossier.Suspects.Count; i++)
            {
                int index = i;
                AddButton(caseControls, dossier.Suspects[i], "field-suspect-" + i, () =>
                {
                    suspect = index;
                    Refresh();
                });
            }

            var markList = new VisualElement();
            markList.AddToClassList("hf2-service-options");
            caseControls.Add(markList);
            for (int i = 0; i < dossier.Evidence.Count; i++)
            {
                int bit = 1 << i;
                var choice = AddButton(markList, dossier.Evidence[i].Name, "field-mark-" + i, () =>
                {
                    evidence ^= bit;
                    Refresh();
                });
                choice.SetEnabled((campaign.CaseReadMask(computer.Station) & bit) != 0);
            }
            AddButton(caseControls, "Подтвердить", "field-submit", () =>
            {
                status.text = campaign.SubmitCase(computer.Station, suspect, evidence) switch
                {
                    FieldCaseResult.Confirmed => dossier.Confirmation,
                    FieldCaseResult.Incorrect => dossier.Rejected,
                    _ => "Выберите вариант и отметьте два открытых источника."
                };
                Refresh();
            });
            Refresh();
        }

        public void Refresh()
        {
            if (caseControls == null) return;
            bool done = campaign.CaseConfirmed(computer.Station);
            caseControls.style.display = done ? DisplayStyle.None : DisplayStyle.Flex;
            if (done)
            {
                status.text = dossier.Confirmation;
                return;
            }
            for (int i = 0; i < dossier.Suspects.Count; i++)
                caseControls.Q<Button>("field-suspect-" + i).EnableInClassList("hf2-service-selected", i == suspect);
            for (int i = 0; i < dossier.Evidence.Count; i++)
                caseControls.Q<Button>("field-mark-" + i).EnableInClassList("hf2-service-selected", (evidence & (1 << i)) != 0);
        }

        private void AddCameraFrames(VisualElement parent)
        {
            var times = new VisualElement();
            times.AddToClassList("hf2-service-options");
            parent.Add(times);
            var row = new VisualElement();
            row.AddToClassList("hf2-service-options");
            parent.Add(row);
            AddButton(times, "03:16:52", "field-frame-early", () => ShowFrame(false));
            AddButton(times, "03:17:06", "field-frame-late", () => ShowFrame(true));
            ShowFrame(false);

            void ShowFrame(bool late)
            {
                times.Q<Button>("field-frame-early").EnableInClassList("hf2-service-selected", !late);
                times.Q<Button>("field-frame-late").EnableInClassList("hf2-service-selected", late);
                row.Clear();
                Frame("B-02", late ? "Кадр 18314" : "Кадр 17964", "Подъёмник пуст");
                Frame("B-04", "Кадр 06313", "Створка опущена");
                Frame("B-06", late ? "Кадр 24182" : "Кадр 23832", late ? "Лампа погасла" : "Лампа горит", late);
            }

            void Frame(string channel, string number, string description, bool dark = false)
            {
                var tile = new VisualElement();
                tile.AddToClassList("hf2-service-camera");
                row.Add(tile);
                var frame = new Image
                {
                    image = Resources.Load<Texture2D>("Investigation/Camera" + channel.Replace("-", "")),
                    scaleMode = ScaleMode.ScaleAndCrop,
                    tintColor = dark ? new Color(.46f, .53f, .58f) : new Color(.76f, .87f, .80f)
                };
                frame.AddToClassList("hf2-frame");
                tile.Add(Label(channel));
                tile.Add(frame);
                tile.Add(Label(number + "\n" + description));
            }
        }

        private void ClearReader()
        {
            caseControls = null;
            equipment = null;
            reader.Clear();
            reader.scrollOffset = Vector2.zero;
        }

        private Button AddButton(VisualElement parent, string text, string name, Action action)
        {
            var button = new Button(() =>
            {
                if (canOperate()) action();
            }) { text = text, name = name };
            parent.Add(button);
            return button;
        }

        private static Label Label(string text, string style = "hf2-prose")
        {
            var label = new Label(text);
            label.AddToClassList(style);
            return label;
        }
    }
}
