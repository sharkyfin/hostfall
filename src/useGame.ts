import { useCallback, useEffect, useRef, useState } from 'react';
import { actionPreview, advance, createWorld, execute, observe, replay, review } from './game/engine';
import { advanceLesson } from './game/experience';
import { DEFAULT_SEED } from './game/scenario';
import type { AssetId, Command, CommandEntry, MissionId, ObservationEvent, World } from './game/types';

const SAVE_KEY = 'hostfall.experience.v2';
interface Saved { version: 2; mission: MissionId; seed: string; commands: CommandEntry[]; tick: number; started: boolean; lesson: number; trained: boolean }
export interface Receipt { command: Command | null; ticks: number; completed: boolean; events: ObservationEvent[] }
function load(): { world: World; started: boolean; lesson: number; trained: boolean; error?: string } {
  try {
    const raw = localStorage.getItem(SAVE_KEY);
    if (raw) {
      const saved = JSON.parse(raw) as Saved;
      if (saved.version !== 2 || !['training','night','breach'].includes(saved.mission) || !Array.isArray(saved.commands) || !Number.isInteger(saved.lesson) || saved.lesson < 0 || saved.lesson > 8) throw new Error('Invalid save');
      return { world: replay(saved.seed, saved.commands, saved.tick, saved.mission), started: Boolean(saved.started), lesson: saved.lesson, trained: Boolean(saved.trained) };
    }
  } catch { return { world: createWorld(DEFAULT_SEED, 'training'), started: false, lesson: 0, trained: false, error: 'Не удалось восстановить смену. Можно начать новую; прежняя запись не перезаписана до начала игры.' }; }
  return { world: createWorld(DEFAULT_SEED, 'training'), started: false, lesson: 0, trained: false };
}
export function useGame() {
  const [initial] = useState(load);
  const model = useRef(initial.world);
  const [snapshot, setSnapshot] = useState(() => observe(initial.world));
  const [report, setReport] = useState(() => review(initial.world));
  const [started, setStarted] = useState(initial.started);
  const [lesson, setLesson] = useState(initial.lesson);
  const [trained, setTrained] = useState(initial.trained);
  const [receipt, setReceipt] = useState<Receipt | null>(null);
  const [notice, setNotice] = useState(initial.error ?? '');
  const commit = useCallback((world: World) => { model.current = world; setSnapshot(observe(world)); setReport(review(world)); }, []);
  const act = useCallback((command: Command) => {
    const before = model.current;
    const { world, result } = execute(before, command);
    if (!result.accepted) { setNotice(result.reason); return false; }
    commit(world);
    const existing = new Set(before.observations.map(e=>e.id));
    setReceipt({ command, ticks: result.ticks, completed: Boolean(result.completed), events: world.observations.filter(e=>!existing.has(e.id) && e.title !== 'Операция принята') });
    if (before.scenarioId === 'training') {
      setLesson(step => advanceLesson(step, command, result.completed));
      if (command.type === 'verify' && ['clean','costly'].includes(world.outcome)) setTrained(true);
    }
    return true;
  }, [commit]);
  const step = useCallback(() => {
    const before=model.current; const next=advance(before);
    commit(next);
    const existing=new Set(before.observations.map(e=>e.id));
    setReceipt({command:null,ticks:next.tick-before.tick,completed:true,events:next.observations.filter(e=>!existing.has(e.id))});
  }, [commit]);
  const preview = useCallback((command: Command) => actionPreview(model.current, command), []);
  const openLead = (assetId: AssetId) => { if (snapshot.mission === 'training') setLesson(current => advanceLesson(current, {type:'open',assetId})); };
  const begin = (mission: MissionId, seed = DEFAULT_SEED) => {
    commit(createWorld(seed,mission)); setLesson(0); setReceipt(null); setNotice(''); setStarted(true);
  };
  useEffect(() => {
    if (!started) return;
    try {
      const save: Saved = {version:2,mission:model.current.scenarioId,seed:model.current.seed,commands:model.current.commands,tick:model.current.tick,started,lesson,trained};
      localStorage.setItem(SAVE_KEY,JSON.stringify(save));
    } catch { setNotice('Браузер не разрешает сохранение. Игра продолжает работать.'); }
  },[snapshot,started,lesson,trained]);
  useEffect(()=>{if(!notice)return;const timer=window.setTimeout(()=>setNotice(''),5500);return()=>window.clearTimeout(timer);},[notice]);
  const exportReplay=()=>{
    const blob=new Blob([JSON.stringify({version:2,mission:model.current.scenarioId,seed:model.current.seed,commands:model.current.commands,finalTick:model.current.tick,outcome:model.current.outcome},null,2)],{type:'application/json'});
    const url=URL.createObjectURL(blob);const link=document.createElement('a');link.href=url;link.download=`hostfall-${model.current.scenarioId}.json`;link.click();window.setTimeout(()=>URL.revokeObjectURL(url),1000);
  };
  return { snapshot, report, started, lesson, trained, receipt, dismissReceipt:()=>setReceipt(null), begin, home:()=>setStarted(false), resume:()=>setStarted(true), act, step, preview, openLead, exportReplay, notice, setNotice };
}
