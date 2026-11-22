using System;
using System.Collections.Generic;
using System.Linq;
namespace Hostfall.Simulation
{
    public sealed class ServiceSwitchState
    {
        private readonly HashSet<string> off=new(StringComparer.Ordinal);
        public bool IsOff(string id)=>off.Contains(id);
        public void SetOff(string id,bool value){if(value)off.Add(id);else off.Remove(id);}
        public string[] Capture()=>off.OrderBy(id=>id).ToArray();
        public void Restore(string[] values)
        {
            if(values==null)return;SaveValidation.Require(values.Length<=64,"Too many light switches");
            foreach(var id in values){SaveValidation.Text(id,128,"Invalid light switch");SaveValidation.Require(id.StartsWith("worklight.",StringComparison.Ordinal)&&off.Add(id),"Invalid or duplicate light switch");}
        }
    }
}
