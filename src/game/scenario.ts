import type { AssetDefinition, AssetId, Channel, IdentityPermission, NetworkLink } from './types';

export const ASSETS: AssetDefinition[] = [
  { id: 'GW-01', label: 'Шлюз доступа', role: 'VPN / Egress', zone: 'Периметр', x: 95, y: 170, sensor: 'VPN + NetFlow', impact: 'Остановка внешнего доступа и приёма заказов.' },
  { id: 'IDP-01', label: 'Каталог учётных записей', role: 'Identity provider', zone: 'Доступ', x: 315, y: 70, sensor: 'Журнал авторизации', impact: 'Недоступна авторизация клиентов и сотрудников.' },
  { id: 'WS-07', label: 'Станция подрядчика', role: 'Workstation', zone: 'Рабочая зона', x: 315, y: 285, sensor: 'EDR · по запросу', impact: 'Приостановка работы одного подрядчика.' },
  { id: 'APP-01', label: 'Сервис заказов', role: 'Application server', zone: 'Приложения', x: 555, y: 170, sensor: 'EDR + сетевой датчик', impact: 'Клиенты не смогут оформить заказ.' },
  { id: 'DB-01', label: 'Клиентские заказы', role: 'Database', zone: 'Данные', x: 795, y: 170, sensor: 'Аудит запросов', impact: 'Остановка приложения и резервного копирования.' },
  { id: 'BACKUP-01', label: 'Резервное копирование', role: 'Backup service', zone: 'Данные', x: 795, y: 285, sensor: 'Журнал заданий', impact: 'Пропуск ночной резервной копии.' },
  { id: 'MAIL-01', label: 'Почтовый шлюз', role: 'Mail relay', zone: 'Приложения', x: 555, y: 285, sensor: 'Журнал доставки', impact: 'Приостановка доставки служебных писем.' },
  { id: 'SOC-01', label: 'Центр мониторинга', role: 'SOC / Management', zone: 'Управление', x: 95, y: 285, sensor: 'Управляющий контур', impact: 'Управляющий узел защищён от самоизоляции.' },
];

export const LINKS: NetworkLink[] = [
  { from: 'GW-01', to: 'WS-07', kind: 'remote' },
  { from: 'WS-07', to: 'APP-01', kind: 'deploy' },
  { from: 'APP-01', to: 'DB-01', kind: 'database' },
  { from: 'APP-01', to: 'GW-01', kind: 'egress' },
  { from: 'APP-01', to: 'MAIL-01', kind: 'mail' },
  { from: 'MAIL-01', to: 'GW-01', kind: 'egress' },
  { from: 'BACKUP-01', to: 'DB-01', kind: 'backup' },
  { from: 'BACKUP-01', to: 'GW-01', kind: 'archive' },
  { from: 'GW-01', to: 'IDP-01', kind: 'identity' },
  { from: 'WS-07', to: 'IDP-01', kind: 'identity' },
];
export const SERVICE_DEPENDENCIES: AssetId[] = ['GW-01', 'IDP-01', 'APP-01', 'DB-01'];
export const DESTINATIONS: Record<Channel, { label: string; description: string }> = {
  exchange: { label: 'files.exchange.invalid', description: 'Исходящий обмен файлами · APP → GW' },
  mail: { label: 'mail-edge.invalid', description: 'Почтовая доставка · APP → MAIL → GW' },
  archive: { label: 'archive.cloud.invalid', description: 'Ночной архив · BACKUP → GW' },
};
export const DEFAULT_SEED = 'NIGHT-0317';
export const MAX_TICKS = 36;
export const OUTAGE_LIMIT = 6;
export const ACTION_TICKS = { inspect: 2, contact: 1, purge: 1, revoke: 1, isolate: 1, block: 1, restore: 4, verify: 3 } as const;
export const IDENTITY_PERMISSIONS: IdentityPermission[] = [
  { principal: 'svc.deploy', origin: 'WS-07', target: 'APP-01', capability: 'deploy' },
  { principal: 'svc.orders', origin: 'APP-01', target: 'DB-01', capability: 'read-orders' },
  { principal: 'svc.backup', origin: 'BACKUP-01', target: 'DB-01', capability: 'snapshot' },
];
export const PERMISSIONS = [
  { principal: 'Подрядчик', resource: 'WS-07', permission: 'Удалённая сессия' },
  { principal: 'svc.deploy на WS-07', resource: 'APP-01', permission: 'Развёртывание приложения' },
  { principal: 'svc.orders на APP-01', resource: 'DB-01', permission: 'Чтение заказов' },
  { principal: 'svc.backup', resource: 'DB-01', permission: 'Резервная копия' },
];
export function hash(value: string): number {
  let n = 2166136261;
  for (let i = 0; i < value.length; i++) n = Math.imul(n ^ value.charCodeAt(i), 16777619);
  return n >>> 0;
}
export function gameTime(tick: number): string {
  const seconds = (3 * 60 + 17) * 60 + tick * 30;
  return `${String(Math.floor(seconds / 3600)).padStart(2, '0')}:${String(Math.floor(seconds / 60) % 60).padStart(2, '0')}:${String(seconds % 60).padStart(2, '0')}`;
}
export function duration(ticks: number): string { return ticks % 2 === 0 ? `${ticks / 2}:00` : `${Math.floor(ticks / 2)}:30`; }
