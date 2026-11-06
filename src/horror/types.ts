export type Host = 'ws07' | 'edge01' | 'core01';
export type Tone = 'normal' | 'muted' | 'success' | 'warn' | 'danger' | 'entity';
export interface File { content: string; writable: boolean; }
export interface Process { pid: number; name: string; owner: string; command: string; }
export interface Packet { id: string; channel: string; destination: string; signature: string; bytes: number; }
export interface Transcript { id: number; host: Host; command?: string; cwd?: string; text: string; tone: Tone; }
export interface Notice { id: number; sender: string; text: string; tone: Tone; }
export interface Routes { replication: string; operator: string; care: string; }
export interface World {
  version: 3; host: Host; cwd: Record<Host,string>; files: Record<Host,Record<string,File>>;
  directories: Record<Host,string[]>; processes: Record<Host,Process[]>;
  turn: number; nextPid: number; stage: 0|1|2|3; convergence: number;
  workstationSealed: boolean; firewallReady: boolean; routes: Routes;
  controlUp: boolean; careUp: boolean; careOutage: number; blockedPackets: number;
  daemonRunning: boolean; daemonStoppedSafely: boolean;
  outcome: 'active'|'contained'|'costly'|'assimilated';
  history: Transcript[]; notices: Notice[]; commands: string[];
  nextId: number; lastExit: number; env: Record<string,string>;
}
export interface RunResult { ok: boolean; stdout: string; values: unknown[]; error?: string; }
export type CodeRunner = (source:string, inputs?:unknown[])=>Promise<RunResult>;
export interface ShellResult { world: World; editor?: {host:Host;path:string}; }
export const HOSTS: Record<Host,{name:string;role:string;address:string}> = {
  ws07:{name:'WS-07',role:'Рабочая станция',address:'10.17.0.7'},
  edge01:{name:'EDGE-01',role:'Шлюз репликации',address:'10.17.0.1'},
  core01:{name:'CORE-01',role:'Вычислительное ядро',address:'10.17.0.99'},
};
