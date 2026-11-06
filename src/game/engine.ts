import { ACTION_TICKS, ASSETS, DEFAULT_SEED, DESTINATIONS, hash, IDENTITY_PERMISSIONS, LINKS, MAX_TICKS, OUTAGE_LIMIT, SERVICE_DEPENDENCIES } from './scenario';
import type { ActionResult, AssetId, AttackerJob, Channel, Command, FindingCode, MissionId, ObservationState, Review, TruthEvent, World } from './types';

type EventInput = Pick<TruthEvent, 'assetId' | 'title' | 'detail' | 'category'> & Partial<Pick<TruthEvent, 'severity' | 'source' | 'accountId' | 'sessionId' | 'finding'>>;
function emit(world: World, input: EventInput, actor: TruthEvent['actor'], causeId: string, delay: number | null = 0) {
  world.events.push({
    severity: 'info', source: 'SYSTEM', ...input, id: `ev-${world.events.length.toString(36).padStart(4, '0')}`,
    tick: world.tick, actor, causeId, deliveryTick: delay === null ? null : world.tick + delay,
  });
}
function deliver(world: World) {
  const seen = new Set(world.observations.map(e => e.id));
  for (const event of world.events) {
    if (event.deliveryTick === null || event.deliveryTick > world.tick || seen.has(event.id)) continue;
    const { actor: _actor, causeId: _cause, deliveryTick: _delivery, ...observation } = event;
    world.observations.push({ ...observation, deliveredAt: world.tick });
  }
}
export function isUp(world: World, assetId: AssetId) {
  const host = world.truth.assets[assetId];
  return !host.isolated && !host.reimaging;
}
export function canReach(world: World, from: AssetId, to: AssetId): boolean {
  if (!isUp(world, from) || !isUp(world, to)) return false;
  const queue = [from]; const visited = new Set<AssetId>();
  while (queue.length) {
    const node = queue.shift()!;
    if (node === to) return true;
    if (visited.has(node)) continue;
    visited.add(node);
    for (const edge of LINKS) if (edge.from === node && isUp(world, edge.to)) queue.push(edge.to);
  }
  return false;
}
export function channelOpen(world: World, channel: Channel): boolean {
  if (world.truth.blocked[channel]) return false;
  if (channel === 'archive') return canReach(world, 'BACKUP-01', 'GW-01');
  if (channel === 'exchange') return isUp(world, 'APP-01') && isUp(world, 'GW-01');
  return isUp(world, 'APP-01') && isUp(world, 'MAIL-01') && isUp(world, 'GW-01');
}
export function createWorld(seed = DEFAULT_SEED, mission: MissionId = 'night'): World {
  const initialFoothold = mission === 'training' || seed === DEFAULT_SEED || hash(`${seed}:initial`) % 3 !== 0;
  const accountId = hash(`${seed}:account`) % 2 ? 'contractor.rudnev' : 'contractor.sokolova';
  const world: World = {
    version: 2, scenarioId: mission, seed, tick: 0, accountId,
    sessionId: `vpn-${hash(`${seed}:session`).toString(16).slice(0, 5)}`,
    truth: {
      assets: Object.fromEntries(ASSETS.map(a => [a.id, { isolated: false, reimaging: false }])) as World['truth']['assets'],
      sessionActive: true, blocked: { exchange: false, mail: false, archive: false },
      identityPermissions: [...structuredClone(IDENTITY_PERMISSIONS), { principal: accountId, origin: 'GW-01', target: 'WS-07', capability: 'remote' }],
      attacker: { footholds: initialFoothold ? ['WS-07'] : [], appControlled: false, staged: 0, leaked: 0, fallbackReady: false, job: null, firstEntryDuration: mission === 'training' ? 12 : 6 + hash(`${seed}:pace`) % 3 },
      business: { ordersDownTicks: 0, backupLostTicks: 0, backupProgress: 0, backupStart: seed === DEFAULT_SEED ? 2 : 1 + hash(`${seed}:backup`) % 3, backupReported: false },
    },
    events: [], observations: [], commands: [], results: {}, outcome: 'active', endReason: '',
  };
  if (mission === 'breach') {
    world.truth.sessionActive = false;
    world.truth.attacker.footholds = ['APP-01'];
    world.truth.attacker.appControlled = true;
    world.truth.attacker.staged = 100;
    emit(world, { assetId: 'APP-01', accountId: 'svc.orders', source: 'EDR + NDR', category: 'network', severity: 'critical', title: 'Магазин готовит внешнюю выгрузку', detail: 'Аудит APP: создан файл выгрузки на 100 единиц заказов. Сетевой датчик: открыт поток APP-01 → GW-01 → files.exchange.invalid. Первая порция ещё не передана. Можно перекрыть этот канал или отключить сервер, но тогда магазин перестанет принимать заказы.' }, 'attacker', 'initial-export');
    emit(world, { assetId: 'IDP-01', accountId, source: 'IDP / AUDIT', category: 'auth', title: 'Первоначальная сессия уже закрыта', detail: 'Предыдущая смена отозвала сессию подрядчика. Незаявленный процесс на APP-01 продолжил работу независимо от неё.' }, 'defender', 'previous-shift');
  } else emit(world, { assetId: 'GW-01', accountId, sessionId: world.sessionId, source: 'VPN / AUTH', category: 'auth', severity: 'warning', title: 'Новый источник удалённого входа', detail: `Сессия ${world.sessionId}: ${accountId} вошёл с нового устройства. Разрешённый ресурс — WS-07. Сам по себе новый источник не доказывает компрометацию.` }, 'attacker', 'initial-session');
  emit(world, { assetId: 'APP-01', source: 'OPERATIONS', category: 'operations', title: 'Ночное окно обслуживания', detail: 'Администратор admin.nina проверяет сервис заказов. Подрядчик имеет доступ к WS-07; служебный доступ svc.deploy разрешает развёртывание на APP-01.' }, 'normal', 'maintenance-schedule');
  emit(world, { assetId: 'BACKUP-01', source: 'SCHEDULER', category: 'operations', title: 'Запланирована резервная копия', detail: 'Задание BKP-0317: svc.backup → DB-01 → archive.cloud.invalid. Ожидается интенсивная передача данных.' }, 'normal', 'backup-schedule');
  if (initialFoothold && mission !== 'breach') emit(world, { assetId: 'WS-07', accountId, sessionId: world.sessionId, source: 'EDR', category: 'endpoint', title: 'Создана локальная задача', detail: 'На WS-07 существует автономная задача remote-maint. Её авторизация не зависит от VPN-сессии.' }, 'attacker', 'initial-foothold', null);
  planAttacker(world); deliver(world); return world;
}
function hasWsControl(world: World) {
  return !world.truth.assets['WS-07'].reimaging && (world.truth.sessionActive || world.truth.attacker.footholds.includes('WS-07'));
}
function hasPermission(world: World, principal: string, origin: AssetId, target: AssetId, capability: string): boolean {
  return world.truth.identityPermissions.some(p => p.principal === principal && p.origin === origin && p.target === target && p.capability === capability);
}
function jobAllowed(world: World, job: AttackerJob): boolean {
  const attacker = world.truth.attacker;
  switch (job.type) {
    case 'persist-ws': return world.truth.sessionActive && canReach(world, 'GW-01', 'WS-07') && hasPermission(world, world.accountId, 'GW-01', 'WS-07', 'remote');
    case 'enter-app': return attacker.footholds.includes('WS-07') && canReach(world, 'WS-07', 'APP-01') && hasPermission(world, 'svc.deploy', 'WS-07', 'APP-01', 'deploy');
    case 'persist-app': return attacker.appControlled && !world.truth.assets['APP-01'].reimaging;
    case 'collect': return attacker.appControlled && canReach(world, 'APP-01', 'DB-01') && hasPermission(world, 'svc.orders', 'APP-01', 'DB-01', 'read-orders');
    case 'reroute': return attacker.appControlled && channelOpen(world, 'mail');
    case 'transfer': return attacker.appControlled && attacker.staged > 0 && channelOpen(world, job.channel!);
  }
}
function planAttacker(world: World) {
  const a = world.truth.attacker;
  if (a.job) return;
  let job: AttackerJob | null = null;
  const make = (type: AttackerJob['type'], ticks: number, channel?: 'exchange' | 'mail'): AttackerJob => ({ type, startedAt: world.tick, readyAt: world.tick + ticks, channel });
  if (!a.appControlled) {
    if (a.footholds.includes('WS-07') && canReach(world, 'WS-07', 'APP-01') && hasPermission(world, 'svc.deploy', 'WS-07', 'APP-01', 'deploy')) job = make('enter-app', a.firstEntryDuration);
    else if (!a.footholds.includes('WS-07') && hasWsControl(world) && canReach(world, 'GW-01', 'WS-07') && hasPermission(world, world.accountId, 'GW-01', 'WS-07', 'remote')) job = make('persist-ws', 3);
  } else if (!a.footholds.includes('APP-01') && !world.truth.assets['APP-01'].reimaging) job = make('persist-app', 2);
  else if (a.staged === 0 && canReach(world, 'APP-01', 'DB-01') && hasPermission(world, 'svc.orders', 'APP-01', 'DB-01', 'read-orders')) job = make('collect', 2);
  else if (a.staged > 0 && channelOpen(world, 'exchange')) job = make('transfer', 1, 'exchange');
  else if (a.staged > 0 && channelOpen(world, 'mail')) job = a.fallbackReady ? make('transfer', 1, 'mail') : make('reroute', 2);
  a.job = job;
  if (job?.type === 'enter-app') emit(world, { assetId: 'APP-01', accountId: world.accountId, sessionId: world.sessionId, source: 'NDR / ACCESS', category: 'auth', severity: 'warning', title: 'Нетипичный запрос к приложению', detail: `${world.accountId}: WS-07 → APP-01. В контексте той же сессии запрошен служебный доступ svc.deploy. Сопоставьте с журналом VPN и планом обслуживания.` }, 'attacker', `enter-app-${world.tick}`, 2);
  if (job?.type === 'reroute') emit(world, { assetId: 'MAIL-01', accountId: 'svc.orders', source: 'MAIL / FLOW', category: 'network', severity: 'warning', title: 'Новый маршрут исходящей передачи', detail: 'APP-01 пытается передать вложение через MAIL-01 → mail-edge.invalid. Прямой файловый канал недоступен; у почтового канала отдельное правило.' }, 'attacker', `reroute-${world.tick}`, 1);
}
function updateAttacker(world: World) {
  const a = world.truth.attacker; const job = a.job;
  if (job && !jobAllowed(world, job)) {
    a.job = null;
    emit(world, { assetId: job.type === 'persist-ws' ? 'WS-07' : 'APP-01', source: 'NDR / FLOW', category: 'network', title: 'Операция потеряла необходимый доступ', detail: 'Активное соединение или доступ стали недоступны. Это подтверждает разрыв операции, но не очистку локальных задач.' }, 'system', `cancel-${job.type}-${world.tick}`, 0);
  } else if (job && job.readyAt <= world.tick) {
    a.job = null;
    switch (job.type) {
      case 'persist-ws':
        a.footholds.push('WS-07');
        emit(world, { assetId: 'WS-07', accountId: world.accountId, sessionId: world.sessionId, source: 'EDR', category: 'endpoint', title: 'Зарегистрирована автономная задача', detail: 'remote-maint запускается локально и может пережить отзыв VPN-сессии. Для проверки нужен снимок EDR.' }, 'attacker', `persist-ws-${world.tick}`, null); break;
      case 'enter-app':
        a.appControlled = true;
        emit(world, { assetId: 'APP-01', accountId: 'svc.deploy', source: 'EDR / PROCESS', category: 'endpoint', severity: 'critical', title: 'Неучтённый процесс развёртывания', detail: 'На APP-01 запущен deploy-worker с WS-07. В окне обслуживания такое развёртывание не заявлено. Отзыв VPN не остановит уже запущенный процесс.' }, 'attacker', `entered-app-${world.tick}`, 1); break;
      case 'persist-app':
        a.footholds.push('APP-01');
        emit(world, { assetId: 'APP-01', accountId: 'svc.orders', source: 'EDR', category: 'endpoint', title: 'Создана локальная задача на APP-01', detail: 'Новая задача имеет доступ к служебному контексту svc.orders.' }, 'attacker', `persist-app-${world.tick}`, null); break;
      case 'collect':
        a.staged = 100;
        emit(world, { assetId: 'DB-01', accountId: 'svc.orders', source: 'DB / AUDIT', category: 'network', severity: 'critical', title: 'Массовое чтение клиентских заказов', detail: 'APP-01 прочитал пакет из 100 модельных единиц данных. Это чтение, а не подтверждённая внешняя утечка. Проверьте исходящие потоки APP-01.' }, 'attacker', `collection-${world.tick}`, 1); break;
      case 'reroute': a.fallbackReady = true; break;
      case 'transfer': {
        const amount = Math.min(job.channel === 'exchange' ? 25 : 10, 100 - a.leaked);
        a.leaked += amount;
        emit(world, { assetId: job.channel === 'mail' ? 'MAIL-01' : 'APP-01', accountId: 'svc.orders', source: 'NDR / EGRESS', category: 'network', severity: 'critical', title: 'Исходящая передача данных', detail: `${job.channel === 'mail' ? 'APP-01 → MAIL-01' : 'APP-01 → GW-01'} → ${DESTINATIONS[job.channel!].label}: передано ${amount} модельных единиц. Уже переданное нельзя вернуть блокировкой.` }, 'attacker', `transfer-${world.tick}`, 0); break;
      }
    }
  }
  planAttacker(world);
}
function updateNormal(world: World) {
  const b = world.truth.business;
  if (world.tick === b.backupStart) emit(world, { assetId: 'BACKUP-01', accountId: 'svc.backup', source: 'NDR / VOLUME', category: 'network', severity: 'warning', title: 'Большой объём исходящего трафика', detail: 'BACKUP-01 → archive.cloud.invalid: началась передача резервной копии BKP-0317. Детектор объёма не определяет намерение процесса.' }, 'normal', 'backup-start');
  if (world.tick >= b.backupStart && world.tick < b.backupStart + 10) {
    if (canReach(world, 'BACKUP-01', 'DB-01') && channelOpen(world, 'archive') && hasPermission(world, 'svc.backup', 'BACKUP-01', 'DB-01', 'snapshot')) b.backupProgress += 10;
    else b.backupLostTicks++;
  }
  if (world.tick === b.backupStart + 10 && !b.backupReported) {
    b.backupReported = true;
    emit(world, { assetId: 'BACKUP-01', accountId: 'svc.backup', source: 'BACKUP / JOB', category: 'business', severity: b.backupProgress >= 100 ? 'info' : 'warning', title: b.backupProgress >= 100 ? 'Резервная копия завершена' : 'Резервная копия не завершена', detail: `Задание BKP-0317: выполнено ${b.backupProgress}%. ${b.backupProgress >= 100 ? 'Контрольная сумма подтверждена.' : 'Часть окна потеряна из-за недоступности маршрута. Это операционный ущерб.'}` }, 'normal', 'backup-complete');
  }
  if (world.tick === 4) emit(world, { assetId: 'APP-01', accountId: 'admin.nina', source: 'CHANGE LOG', category: 'operations', title: 'Плановая проверка администратора', detail: 'CHG-204: admin.nina проверила состояние APP-01. Изменение зарегистрировано; удалённое развёртывание с WS-07 в заявку не входит.' }, 'normal', 'admin-maintenance');
}
function end(world: World, outcome: World['outcome'], reason: string) {
  world.outcome = outcome; world.endReason = reason;
  emit(world, { assetId: 'SOC-01', category: 'operations', source: 'SHIFT / REVIEW', title: 'Смена завершена', detail: reason, severity: outcome === 'failure' ? 'critical' : 'info' }, 'system', 'outcome');
}
function advanceMutable(world: World, beforeActors?: () => void) {
  if (world.outcome !== 'active') return;
  world.tick++;
  beforeActors?.();
  updateNormal(world); updateAttacker(world);
  if (!SERVICE_DEPENDENCIES.every(id => isUp(world, id))) world.truth.business.ordersDownTicks++;
  if (world.truth.attacker.leaked >= 100) end(world, 'failure', 'За пределы компании передан весь целевой пакет данных.');
  else if (world.truth.business.ordersDownTicks >= OUTAGE_LIMIT) end(world, 'failure', 'Сервис заказов недоступен суммарно 3 игровые минуты. Допустимый простой превышен.');
  else if (world.tick >= MAX_TICKS) end(world, 'incomplete', 'Окно смены закончилось. Полное устранение угрозы не подтверждено.');
  deliver(world);
}
export function advance(world: World, ticks = 1): World {
  const next = structuredClone(world);
  for (let i = 0; i < ticks && next.outcome === 'active'; i++) advanceMutable(next);
  return next;
}
export function actionPreview(world: World, command: Command): ActionResult {
  if (world.outcome !== 'active') return { accepted: false, reason: 'Смена завершена. Начните повторное прохождение.', ticks: 0 };
  const ticks = ACTION_TICKS[command.type];
  if ('assetId' in command && !world.truth.assets[command.assetId]) return { accepted: false, reason: 'Неизвестный узел.', ticks: 0 };
  if (command.type === 'revoke' && command.accountId !== world.accountId) return { accepted: false, reason: 'В этой миссии отзыв доступен для выбранного аккаунта подрядчика.', ticks: 0 };
  if (command.type === 'revoke' && !world.truth.sessionActive) return { accepted: false, reason: 'Сессии уже отозваны. Повторный отзыв не удалит локальные задачи.', ticks: 0 };
  if (command.type === 'purge') {
    if (command.assetId !== 'WS-07' && command.assetId !== 'APP-01') return { accepted: false, reason: 'Точечное удаление доступно на станции и сервере магазина.', ticks: 0 };
    const snapshot = [...world.observations].reverse().find(e => e.assetId === command.assetId && e.finding);
    if (!snapshot || !['local-task', 'rogue-process'].includes(snapshot.finding!)) return { accepted: false, reason: 'Сначала проверь процессы: нужен конкретный найденный след, чтобы не остановить полезную задачу.', ticks: 0 };
    if (world.observations.some(e => e.assetId === command.assetId && e.tick >= snapshot.tick && ['Подозрительная задача удалена', 'Доверенный образ восстановлен'].includes(e.title))) return { accepted: false, reason: 'Найденная задача уже удалена. При необходимости запроси новый снимок.', ticks: 0 };
  }
  if ((command.type === 'isolate' || command.type === 'restore') && command.assetId === 'SOC-01') return { accepted: false, reason: 'SOC-01 — управляющий контур. Самоизоляция и восстановление из этой консоли запрещены.', ticks: 0 };
  if (command.type === 'isolate' && world.truth.assets[command.assetId].isolated === command.isolated) return { accepted: false, reason: command.isolated ? 'Узел уже изолирован.' : 'Узел уже подключён к сети.', ticks: 0 };
  if (command.type === 'block' && world.truth.blocked[command.channel] === command.blocked) return { accepted: false, reason: 'Правило уже находится в этом состоянии.', ticks: 0 };
  if (command.type === 'restore' && !world.truth.assets[command.assetId].isolated) return { accepted: false, reason: 'Сначала изолируйте узел. Восстановление выполняется через отдельный управляющий канал.', ticks: 0 };
  return { accepted: true, reason: 'Операция доступна.', ticks };
}
function inspect(world: World, assetId: AssetId, cause: string) {
  let detail = '';
  const finding: FindingCode | undefined = assetId === 'WS-07'
    ? world.truth.attacker.footholds.includes(assetId) ? 'local-task' : 'clean-snapshot'
    : assetId === 'APP-01' ? world.truth.attacker.appControlled ? 'rogue-process' : 'clean-snapshot'
      : assetId === 'BACKUP-01' ? 'scheduled-backup' : assetId === 'GW-01' || assetId === 'IDP-01' ? 'identity-log' : undefined;
  if (assetId === 'WS-07') detail = world.truth.attacker.footholds.includes(assetId)
    ? 'EDR: обнаружена автономная задача remote-maint без зарегистрированного изменения. На станции доступен контекст svc.deploy → APP-01. Задача сохраняется при отзыве VPN. Изоляция разрывает сетевой путь; восстановление из доверенного образа удаляет локальные задачи.'
    : 'EDR: автономная задача remote-maint в текущем снимке не обнаружена. Это результат проверки на данный момент; активная VPN-сессия остаётся отдельным путём доступа.';
  else if (assetId === 'APP-01') detail = world.truth.attacker.appControlled
    ? 'EDR: deploy-worker запущен вне заявки CHG-204. Процесс использует контекст svc.orders → DB-01. Проверьте журнал базы и исходящие потоки. Отзыв сессии подрядчика этот процесс не завершит.'
    : 'EDR: незаявленных процессов развёртывания в текущем снимке нет. APP-01 обслуживает заказы и использует svc.orders для доступа к DB-01.';
  else if (assetId === 'BACKUP-01') detail = 'BKP-0317: подписанный backup-worker; аккаунт svc.backup, источник DB-01, назначение archive.cloud.invalid. Задание совпадает с расписанием. Большой объём соответствует резервному копированию; отключение сорвёт ночное окно.';
  else if (assetId === 'GW-01' || assetId === 'IDP-01') detail = `VPN-аудит: ${world.accountId}, сессия ${world.sessionId}, ресурс WS-07. ${world.truth.sessionActive ? 'Сессия ещё действительна.' : 'Сессия аннулирована защитником.'} Подрядчику не выданы прямые права к DB-01. Локальные задачи на хостах этим датчиком не проверяются.`;
  else if (assetId === 'DB-01') detail = 'DB-аудит: svc.orders имеет доступ с APP-01; svc.backup — с BACKUP-01. Прямого доступа подрядчика к базе нет. Аудит видит запросы, но не содержимое памяти рабочих станций.';
  else if (assetId === 'MAIL-01') detail = `Шлюз обслуживает APP-01 → mail-edge.invalid. ${world.truth.blocked.mail ? 'Исходящее направление запрещено правилом.' : 'Исходящее направление разрешено.'} Проверка почты не подтверждает чистоту APP-01.`;
  else detail = 'Контур управления доступен. Для оценки инцидента сопоставьте VPN, EDR и сетевые события. Отсутствие новых тревог само по себе не подтверждает устранение угрозы.';
  emit(world, { assetId, finding, accountId: assetId === 'WS-07' || assetId === 'GW-01' || assetId === 'IDP-01' ? world.accountId : undefined, sessionId: assetId === 'WS-07' ? world.sessionId : undefined, source: ASSETS.find(a => a.id === assetId)!.sensor.split(' ·')[0], category: 'endpoint', title: `Снимок телеметрии · ${assetId}`, detail }, 'defender', cause);
}
function applyCommand(world: World, command: Command, cause: string) {
  switch (command.type) {
    case 'inspect': inspect(world, command.assetId, cause); break;
    case 'contact':
      if (command.target === 'contractor') emit(world, { assetId: 'WS-07', accountId: world.accountId, sessionId: world.sessionId, source: 'СООБЩЕНИЕ СОТРУДНИКА', category: 'auth', severity: 'warning', title: '«Этот вход не мой»', detail: 'Подрядчик: «Я закончила работу до трёх и отключилась. В 03:17 я не подключалась и никаких задач на сервере не запускала». Это подтверждает чужую сессию, но ещё не говорит, что осталось на станции.' }, 'normal', cause);
      else emit(world, { assetId: 'BACKUP-01', accountId: 'svc.backup', source: 'ДЕЖУРНЫЙ АДМИНИСТРАТОР', category: 'operations', finding: 'scheduled-backup', title: 'Архив подтверждён расписанием', detail: 'Нина: «BKP-0317 — наша ночная копия. svc.backup выгружает её в archive.cloud.invalid. Заявка согласована. Развёртывания приложения с WS-07 в заявке нет».' }, 'normal', cause);
      break;
    case 'purge':
      world.truth.attacker.footholds = world.truth.attacker.footholds.filter(id => id !== command.assetId);
      if (command.assetId === 'APP-01') { world.truth.attacker.appControlled = false; world.truth.attacker.staged = 0; world.truth.attacker.fallbackReady = false; }
      emit(world, { assetId: command.assetId, source: 'EDR / RESPONSE', category: 'endpoint', title: 'Подозрительная задача удалена', detail: `${command.assetId}: остановлена и удалена задача, найденная в снимке. Полезные сервисы продолжают работать. ${world.truth.sessionActive ? 'VPN-сессия всё ещё действует: первоначальный вход остаётся открытым.' : 'Украденная VPN-сессия уже закрыта.'}` }, 'defender', cause);
      break;
    case 'revoke':
      world.truth.sessionActive = false;
      emit(world, { assetId: 'IDP-01', accountId: command.accountId, sessionId: world.sessionId, source: 'IDP / RESPONSE', category: 'auth', title: 'Сессии аккаунта отозваны', detail: `${command.accountId}: VPN-сессия аннулирована. Легитимному пользователю потребуется повторный вход. Локальные задачи и служебные доступы на хостах не удалены.` }, 'defender', cause);
      if (world.truth.attacker.footholds.includes('WS-07') && canReach(world, 'WS-07', 'APP-01')) emit(world, { assetId: 'WS-07', accountId: 'svc.deploy', source: 'NDR / FLOW', category: 'network', severity: 'warning', title: 'После отзыва остался сетевой поток', detail: 'WS-07 → APP-01: соединение в служебном контексте svc.deploy продолжается после закрытия VPN. Нужна проверка процессов на станции.' }, 'system', cause);
      break;
    case 'isolate':
      world.truth.assets[command.assetId].isolated = command.isolated;
      emit(world, { assetId: command.assetId, source: 'NETWORK / RESPONSE', category: 'operations', title: command.isolated ? 'Узел изолирован' : 'Сетевые связи восстановлены', detail: command.isolated ? `${command.assetId}: обычные соединения запрещены. Управляющий канал SOC сохранён. Локальные процессы не удалены. ${ASSETS.find(a => a.id === command.assetId)!.impact}` : `${command.assetId}: сетевые связи снова доступны. Неудалённые локальные задачи также смогут использовать сеть.` }, 'defender', cause); break;
    case 'block':
      world.truth.blocked[command.channel] = command.blocked;
      emit(world, { assetId: 'GW-01', source: 'EGRESS / RESPONSE', category: 'network', title: command.blocked ? 'Исходящее направление запрещено' : 'Исходящее направление разрешено', detail: `${DESTINATIONS[command.channel].label}: ${command.blocked ? 'правило блокировки применено. Остальные направления не затронуты.' : 'правило блокировки снято.'}` }, 'defender', cause); break;
    case 'restore': {
      world.truth.assets[command.assetId].reimaging = false;
      world.truth.attacker.footholds = world.truth.attacker.footholds.filter(id => id !== command.assetId);
      if (command.assetId === 'APP-01') { world.truth.attacker.appControlled = false; world.truth.attacker.staged = 0; world.truth.attacker.fallbackReady = false; }
      emit(world, { assetId: command.assetId, source: 'RECOVERY', category: 'operations', title: 'Доверенный образ восстановлен', detail: `${command.assetId}: локальные задачи, процессы и кэшированные служебные доступы удалены. Узел остаётся изолирован. VPN-сессии аккаунта этим действием не отзываются.` }, 'defender', cause); break;
    }
    case 'verify': {
      const a = world.truth.attacker; const b = world.truth.business;
      if (world.truth.sessionActive || a.footholds.length > 0 || a.appControlled) end(world, 'incomplete', 'Смена закрыта, но остался действующий доступ или локальная опора. Сдерживание не равно устранению угрозы.');
      else if (!SERVICE_DEPENDENCIES.every(id => isUp(world, id))) end(world, 'incomplete', 'Доступ противника устранён, но критический сервис не возвращён в работу.');
      else if (a.leaked > 0 || b.ordersDownTicks > 1 || b.backupLostTicks > 0) end(world, 'costly', 'Угроза устранена. Компания продолжает работать, но инцидент оставил измеримые потери.');
      else end(world, 'clean', 'Доступ противника устранён, заказы доступны. Критической утечки и существенного простоя удалось избежать.');
      break;
    }
  }
}
export function execute(world: World, command: Command, id = `cmd-${world.commands.length + 1}`): { world: World; result: ActionResult } {
  if (world.results[id]) return { world, result: world.results[id] };
  const result = actionPreview(world, command);
  if (!result.accepted) return { world, result };
  const next = structuredClone(world);
  next.commands.push({ id, tick: next.tick, command }); next.results[id] = result;
  const assetId = 'assetId' in command ? command.assetId : command.type === 'revoke' ? 'IDP-01' : 'SOC-01';
  emit(next, { assetId, source: 'SOC / COMMAND', category: 'operations', title: 'Операция принята', detail: `${command.type}: зарезервирован один слот реагирования на ${result.ticks * 30} игровых секунд. Остальная симуляция продолжает действовать.` }, 'defender', id);
  if (command.type === 'restore') next.truth.assets[command.assetId].reimaging = true;
  let completed = false;
  for (let i = 1; i <= result.ticks && next.outcome === 'active'; i++) {
    // Defensive completion precedes attacker validation at the same boundary.
    // Verification closes only after its entire observation window has run.
    advanceMutable(next, i === result.ticks && command.type !== 'verify' ? () => { applyCommand(next, command, id); completed = true; } : undefined);
  }
  if (command.type === 'verify' && next.outcome === 'active') { applyCommand(next, command, id); completed = true; deliver(next); }
  const actualResult = { ...result, completed, ticks: next.tick - world.tick, reason: completed ? 'Операция выполнена.' : 'Операция прервана завершением смены.' };
  next.results[id] = actualResult;
  return { world: next, result: actualResult };
}
export function observe(world: World): ObservationState {
  return {
    seed: world.seed, mission: world.scenarioId, tick: world.tick, accountId: world.accountId, sessionId: world.sessionId,
    outcome: world.outcome, endReason: world.endReason, assets: structuredClone(world.truth.assets),
    events: structuredClone(world.observations), blocked: { ...world.truth.blocked },
    sessionRevoked: !world.truth.sessionActive, ordersUp: SERVICE_DEPENDENCIES.every(id => isUp(world, id)),
    ordersDownTicks: world.truth.business.ordersDownTicks, backupProgress: world.truth.business.backupProgress,
    backupLostTicks: world.truth.business.backupLostTicks, actionsTaken: world.commands.length,
    backupState: world.truth.business.backupProgress >= 100 ? 'complete' : world.tick >= world.truth.business.backupStart + 10 ? 'missed' : world.tick >= world.truth.business.backupStart ? 'running' : 'scheduled',
  };
}
export function review(world: World): Review | null {
  if (world.outcome === 'active') return null;
  return { outcome: world.outcome, reason: world.endReason, leaked: world.truth.attacker.leaked,
    residualHosts: [...new Set([...world.truth.attacker.footholds, ...(world.truth.attacker.appControlled ? ['APP-01' as AssetId] : [])])],
    downTicks: world.truth.business.ordersDownTicks, backupLostTicks: world.truth.business.backupLostTicks,
    actions: world.commands.length, tick: world.tick,
    timeline: world.events.filter(e => e.actor !== 'system' && e.title !== 'Операция принята').map(e => ({ tick: e.tick, title: e.title, actor: e.actor, detail: e.detail })),
  };
}
export function replay(seed: string, entries: World['commands'], finalTick: number, mission: MissionId = 'night'): World {
  if (typeof seed !== 'string' || !seed || seed.length > 48 || !Number.isInteger(finalTick) || finalTick < 0 || finalTick > MAX_TICKS || entries.length > 200) throw new Error('Некорректное сохранение.');
  let world = createWorld(seed, mission);
  const seenIds = new Set<string>();
  for (const entry of entries) {
    if (!entry || typeof entry.id !== 'string' || seenIds.has(entry.id) || !Number.isInteger(entry.tick) || entry.tick < world.tick || entry.tick > finalTick || !validCommand(entry.command)) throw new Error('Некорректная временная последовательность команд.');
    seenIds.add(entry.id);
    world = advance(world, entry.tick - world.tick);
    const result = execute(world, entry.command, entry.id);
    if (!result.result.accepted) throw new Error('Сохранение содержит недопустимую команду.');
    world = result.world;
  }
  if (world.tick > finalTick) throw new Error('Сохранение обрывает принятую операцию.');
  return advance(world, Math.max(0, finalTick - world.tick));
}
function validCommand(value: unknown): value is Command {
  if (!value || typeof value !== 'object' || !('type' in value)) return false;
  if (value.type === 'verify') return true;
  if (value.type === 'contact') return 'target' in value && ['contractor', 'admin'].includes(String(value.target));
  if (value.type === 'revoke') return 'accountId' in value && typeof value.accountId === 'string';
  if (value.type === 'block') return 'channel' in value && ['exchange', 'mail', 'archive'].includes(String(value.channel)) && 'blocked' in value && typeof value.blocked === 'boolean';
  if (!['inspect', 'restore', 'isolate', 'purge'].includes(String(value.type)) || !('assetId' in value) || !ASSETS.some(a => a.id === value.assetId)) return false;
  return value.type !== 'isolate' || 'isolated' in value && typeof value.isolated === 'boolean';
}
