export class Atmosphere {
  private context:AudioContext|null=null;private level:GainNode|null=null;private oscillators:OscillatorNode[]=[];
  enabled=false;
  async toggle(){
    this.enabled=!this.enabled;
    if(this.enabled){
      if(!this.context){
        this.context=new AudioContext();this.level=this.context.createGain();this.level.gain.value=.025;this.level.connect(this.context.destination);
        for(const frequency of [43,64.7,86.15]){const osc=this.context.createOscillator();osc.type='sine';osc.frequency.value=frequency;osc.connect(this.level);osc.start();this.oscillators.push(osc);}
      }
      await this.context.resume();
    }else await this.context?.suspend();
    return this.enabled;
  }
  pulse(uneasy=false){
    if(!this.enabled||!this.context)return;const context=this.context;const oscillator=context.createOscillator();const gain=context.createGain();
    oscillator.type='sine';oscillator.frequency.setValueAtTime(uneasy?133:640,context.currentTime);oscillator.frequency.exponentialRampToValueAtTime(uneasy?49:420,context.currentTime+.23);
    gain.gain.setValueAtTime(.0001,context.currentTime);gain.gain.exponentialRampToValueAtTime(.035,context.currentTime+.01);gain.gain.exponentialRampToValueAtTime(.0001,context.currentTime+.36);oscillator.connect(gain);gain.connect(context.destination);oscillator.start();oscillator.stop(context.currentTime+.4);
  }
  dispose(){void this.context?.close();this.context=null;this.oscillators=[];}
}
