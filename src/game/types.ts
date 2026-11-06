export type AssetId = 'GW-01' | 'IDP-01' | 'WS-07' | 'APP-01' | 'DB-01' | 'BACKUP-01' | 'MAIL-01' | 'SOC-01';
export type Category = 'auth' | 'endpoint' | 'network' | 'operations' | 'business';
export type Severity = 'info' | 'warning' | 'critical';
export type Channel = 'exchange' | 'mail' | 'archive';
export type Outcome = 'active' | 'clean' | 'costly' | 'failure' | 'incomplete';
export type MissionId = 'training' | 'night' | 'breach';
export type FindingCode = 'local-task' | 'rogue-process' | 'clean-snapshot' | 'scheduled-backup' | 'identity-log';

export interface AssetDefinition {
  id: AssetId; label: string; role: string; zone: string; x: number; y: number;
  sensor: string; impact: string;
}
export interface NetworkLink { from: AssetId; to: AssetId; kind: string }
export interface ObservationEvent {
  id: string; tick: number; deliveredAt: number; source: string; assetId: AssetId;
  accountId?: string; sessionId?: string; category: Category; severity: Severity;
  title: string; detail: string;
  finding?: FindingCode;
}
export interface TruthEvent extends Omit<ObservationEvent, 'deliveredAt'> {
  actor: 'attacker' | 'normal' | 'defender' | 'system';
  causeId: string; deliveryTick: number | null;
}
export type Command =
  | { type: 'inspect'; assetId: AssetId }
  | { type: 'contact'; target: 'contractor' | 'admin' }
  | { type: 'purge'; assetId: AssetId }
  | { type: 'revoke'; accountId: string }
  | { type: 'isolate'; assetId: AssetId; isolated: boolean }
  | { type: 'block'; channel: Channel; blocked: boolean }
  | { type: 'restore'; assetId: AssetId }
  | { type: 'verify' };
export interface CommandEntry { id: string; tick: number; command: Command }
export interface ActionResult { accepted: boolean; reason: string; ticks: number; completed?: boolean }
export interface AssetState { isolated: boolean; reimaging: boolean }
export interface IdentityPermission { principal: string; origin: AssetId; target: AssetId; capability: 'remote' | 'deploy' | 'read-orders' | 'snapshot' }
export type AttackerAction = 'persist-ws' | 'enter-app' | 'persist-app' | 'collect' | 'reroute' | 'transfer';
export interface AttackerJob { type: AttackerAction; startedAt: number; readyAt: number; channel?: 'exchange' | 'mail' }
export interface TruthState {
  assets: Record<AssetId, AssetState>;
  sessionActive: boolean;
  identityPermissions: IdentityPermission[];
  attacker: {
    footholds: AssetId[]; appControlled: boolean; staged: number; leaked: number;
    fallbackReady: boolean; job: AttackerJob | null; firstEntryDuration: number;
  };
  blocked: Record<Channel, boolean>;
  business: { ordersDownTicks: number; backupLostTicks: number; backupProgress: number; backupStart: number; backupReported: boolean };
}
export interface World {
  version: 2; scenarioId: MissionId; seed: string; tick: number;
  accountId: string; sessionId: string; truth: TruthState; events: TruthEvent[];
  observations: ObservationEvent[]; commands: CommandEntry[];
  results: Record<string, ActionResult>; outcome: Outcome; endReason: string;
}
export interface ObservationState {
  seed: string; mission: MissionId; tick: number; accountId: string; sessionId: string; outcome: Outcome; endReason: string;
  assets: Record<AssetId, AssetState>; events: ObservationEvent[];
  blocked: Record<Channel, boolean>; sessionRevoked: boolean;
  ordersUp: boolean; ordersDownTicks: number; backupProgress: number; backupLostTicks: number;
  backupState: 'scheduled' | 'running' | 'complete' | 'missed';
  actionsTaken: number;
}
export interface Review {
  outcome: Outcome; reason: string; leaked: number; residualHosts: AssetId[];
  downTicks: number; backupLostTicks: number; actions: number; tick: number;
  timeline: { tick: number; title: string; actor: TruthEvent['actor']; detail: string }[];
}
