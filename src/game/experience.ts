import type { AssetId, Command, MissionId, ObservationEvent, ObservationState } from './types';

export const MISSIONS: Record<MissionId, { title: string; subtitle: string; description: string; goal: string; label: string; minutes: string }> = {
  training: { title: 'Первый сигнал', subtitle: 'Твоя первая смена', description: 'Нина проведёт через один инцидент: чужой вход, проверка станции и безопасная остановка.', goal: 'Разберись с чужим входом вместе с Ниной', label: 'ОБУЧЕНИЕ', minutes: '2–3 минуты' },
  night: { title: 'Тихий вход', subtitle: 'Ночная смена', description: 'Чужой вход или ночные работы? Выбери, какой след проверить, прежде чем перекрывать сеть.', goal: 'Найди чужой доступ и сохрани магазин в работе', label: 'ИНЦИДЕНТ 01', minutes: '4–6 минут' },
  breach: { title: 'Последний рубеж', subtitle: 'Данные на выход', description: 'Магазин готовит выгрузку заказов наружу. Отключить сервер быстро. Остановить только чужой процесс — сложнее.', goal: 'Останови выгрузку, не отключая магазин', label: 'ИНЦИДЕНТ 02', minutes: '3–5 минут' },
};
export const LESSONS = [
  { title: 'Сначала — один след', text: 'На чужом устройстве открыли доступ под именем сотрудника. Нажми на карточку входа внизу. Пока ты читаешь, время стоит.', task: 'Открой подозрительный вход', focus: 'lead', asset: 'GW-01' },
  { title: 'Проверь, кто за экраном', text: 'Новый вход ещё не доказывает атаку. Спросим сотрудницу, действительно ли она подключилась. Это займёт 30 игровых секунд.', task: 'Спроси подрядчика о входе', focus: 'contact', asset: 'GW-01' },
  { title: 'Закрой чужой вход', text: 'Сотрудница не подключалась. Отзови её сессию — это пропуск в сеть. Ей придётся войти заново, зато магазин продолжит работать.', task: 'Отзови сессию подрядчика', focus: 'revoke', asset: 'GW-01' },
  { title: 'Почему связь осталась?', text: 'Пропуск закрыт, но датчик всё ещё видит соединение со станции. На ней могла остаться самостоятельная задача. Проверим процессы.', task: 'Проверь процессы на WS-07', focus: 'inspect', asset: 'WS-07' },
  { title: 'Перекрой путь', text: 'Найдена незаявленная задача. Изоляция отключит станцию от сети и не даст ей добраться до магазина. Саму задачу это не удалит.', task: 'Изолируй WS-07', focus: 'isolate', asset: 'WS-07' },
  { title: 'Удали причину', text: 'Теперь у нас есть конкретная найденная задача. Удалим её точечно: весь компьютер переустанавливать не обязательно.', task: 'Удали найденную задачу', focus: 'purge', asset: 'WS-07' },
  { title: 'Верни людям работу', text: 'Вход закрыт, задача удалена. Верни станцию в сеть: держать безопасный компьютер отключённым больше не нужно.', task: 'Подключи WS-07 обратно', focus: 'reconnect', asset: 'WS-07' },
  { title: 'Проверь, что получилось', text: 'Магазин не останавливался. Наблюдай за сетью ещё полторы игровые минуты и закрой учебную смену. Эта проверка покажет, не остался ли доступ.', task: 'Проверить результат', focus: 'verify', asset: 'WS-07' },
] as const;
export type LessonInteraction = { type: 'open'; assetId: AssetId } | Command;
export function advanceLesson(step: number, interaction: LessonInteraction, accepted = true): number {
  if (!accepted) return step;
  const correct = [
    () => interaction.type === 'open' && interaction.assetId === 'GW-01',
    () => interaction.type === 'contact' && interaction.target === 'contractor',
    () => interaction.type === 'revoke',
    () => interaction.type === 'inspect' && interaction.assetId === 'WS-07',
    () => interaction.type === 'isolate' && interaction.assetId === 'WS-07' && interaction.isolated,
    () => interaction.type === 'purge' && interaction.assetId === 'WS-07',
    () => interaction.type === 'isolate' && interaction.assetId === 'WS-07' && !interaction.isolated,
    () => interaction.type === 'verify',
  ];
  return correct[step]?.() ? step + 1 : step;
}
export function lessonAllows(step: number, command: Command): boolean { return advanceLesson(step, command) > step; }
export function conciseEvent(event: ObservationEvent): { title: string; text: string } {
  switch (event.finding) {
    case 'local-task': return { title: 'На станции осталась автозадача', text: 'Незаявленная remote-maint запускается без VPN и использует служебный доступ к магазину. Теперь её можно удалить точечно.' };
    case 'rogue-process': return { title: 'Чужой процесс на сервере магазина', text: 'Найден deploy-worker вне плана работ. Он читает заказы от имени приложения. Можно остановить этот процесс, сохранив сам магазин.' };
    case 'clean-snapshot': return { title: 'В снимке нет посторонних задач', text: 'Проверка не нашла незаявленного процесса на этом узле. Это снимок на текущий момент; открытый удалённый вход остаётся отдельным риском.' };
    case 'scheduled-backup': return { title: 'Ночная копия подтверждена', text: 'Подписанный процесс, разрешённый аккаунт и назначение совпадают с расписанием. Отключение архива сорвёт полезную работу.' };
    case 'identity-log': return { title: 'Вход ведёт на станцию WS-07', text: 'У подрядчика есть пропуск к рабочей станции, а не прямо к базе заказов. Если след продолжается, проверь процессы на станции.' };
  }
  if (event.title === 'Новый источник удалённого входа') return { title: 'Кто-то вошёл под именем подрядчика', text: 'Новое устройство открыло сессию к WS-07. Это может быть рабочий вход — проверь, кто за ним стоит.' };
  if (event.title === 'Магазин готовит внешнюю выгрузку') return { title: 'Заказы готовы к отправке наружу', text: 'На сервере магазина создан пакет из 100 единиц заказов и открыт внешний канал. Первая порция ещё не ушла. Следующее действие даст процессу время.' };
  if (event.title === 'Нетипичный запрос к приложению') return { title: 'Этот же аккаунт обращается к магазину', text: 'Вход VPN и запрос к APP-01 связаны одной сессией. Станция подрядчика использует доступ к серверу приложения.' };
  if (event.title === 'Большой объём исходящего трафика') return { title: 'Архив отправляет много данных', text: 'Резервный сервер начал внешнюю передачу. Сверь её с ночными работами, прежде чем отключать.' };
  if (event.title === 'После отзыва остался сетевой поток') return { title: 'Вход закрыт. Соединение осталось.', text: 'WS-07 всё ещё обращается к магазину через служебный доступ. Проверь, какой процесс продолжает работу.' };
  if (event.title === 'Новый маршрут исходящей передачи') return { title: 'Передача уходит через почту', text: 'Прямой канал закрыт. Теперь APP-01 пробует MAIL-01. Можно перекрыть и почту или остановить сам процесс на APP-01.' };
  if (event.title === 'Неучтённый процесс развёртывания') return { title: 'Посторонний процесс уже на APP-01', text: 'Станция запустила процесс на сервере магазина. Одной защиты станции теперь недостаточно: проверь сам APP-01.' };
  return { title: event.title, text: event.detail };
}
export function latestFinding(state: ObservationState, assetId: AssetId): ObservationEvent | undefined {
  const finding = [...state.events].reverse().find(e => e.assetId === assetId && e.finding);
  if (!finding) return undefined;
  if (state.events.some(e => e.assetId === assetId && e.tick >= finding.tick && ['Подозрительная задача удалена', 'Доверенный образ восстановлен'].includes(e.title))) return undefined;
  return finding;
}
export function observedTransfer(state: ObservationState): number {
  return state.events.filter(e => e.title === 'Исходящая передача данных').reduce((sum, event) => sum + Number(event.detail.match(/передано (\d+)/)?.[1] ?? 0), 0);
}
export function coachMessage(state: ObservationState): { title: string; text: string } {
  const events = state.events;
  if (!state.ordersUp) return { title: 'Магазин не принимает заказы', text: 'Отключение сервера остановило и полезную работу. Верни сервис до трёх игровых минут суммарного простоя.' };
  const fallback = [...events].reverse().find(e => e.title === 'Новый маршрут исходящей передачи');
  const appCleanup = [...events].reverse().find(e => e.assetId === 'APP-01' && ['Подозрительная задача удалена', 'Доверенный образ восстановлен'].includes(e.title));
  if (fallback && (!appCleanup || fallback.tick > appCleanup.tick) && !state.blocked.mail) return { title: 'Противник меняет маршрут', text: 'Закрыт прямой канал, но почта остаётся открытой. У тебя выбор: перекрыть второй выход или убрать процесс на APP-01.' };
  const clean = events.some(e => ['Подозрительная задача удалена', 'Доверенный образ восстановлен'].includes(e.title));
  if (clean && state.sessionRevoked) return { title: 'Проверь последствия', text: 'Вход закрыт, найденная задача удалена. Верни отключённые узлы и проверь периметр перед завершением.' };
  if (state.mission === 'breach' && !state.blocked.exchange) return { title: 'Решение нужно до следующей порции', text: 'Блокировка канала сохранит заказы в работе. Изоляция сервера остановит все его соединения — вместе с магазином.' };
  if (latestFinding(state, 'APP-01')?.finding === 'rogue-process') return { title: 'Найден конкретный процесс', text: 'Теперь можно удалить только незаявленную задачу. Изоляция всего сервера тоже сработает, но прервёт заказы.' };
  if (state.mission === 'breach' && state.blocked.exchange && !appCleanup) return { title: 'Первый выход закрыт. Найди процесс.', text: 'Заказы продолжают работать. Проверь процессы магазина: пока причина остаётся внутри, она может найти другой выход.' };
  if (latestFinding(state, 'WS-07')?.finding === 'local-task') return { title: 'Логин и задача — разные опоры', text: 'Отзыв закроет украденный пропуск. Найденную локальную задачу нужно остановить отдельно.' };
  if (state.sessionRevoked && latestFinding(state, 'WS-07')?.finding === 'clean-snapshot') return { title: 'Станция проверена. Проверь периметр.', text: 'В текущем снимке посторонней задачи нет, чужой вход закрыт. Финальное наблюдение проверит, не остался ли другой доступ.' };
  if (state.sessionRevoked) return { title: 'Вход закрыт. Проверь станцию.', text: 'Отзыв пропуска не удаляет процессы, запущенные раньше. Проверь WS-07 и посмотри, остались ли сетевые обращения.' };
  return { title: 'Какой след важнее?', text: 'Вход подрядчика и большой поток архива выглядят подозрительно по разным причинам. Проверь человека или расписание, затем выбирай вмешательство.' };
}
