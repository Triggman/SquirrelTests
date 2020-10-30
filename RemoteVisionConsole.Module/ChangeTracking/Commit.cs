using System;
using System.Collections.Generic;

namespace RemoteVisionConsole.Module.ChangeTracking
{
    public class Commit
    {
        public DateTime Time { get; set; }

        public List<Change> Changes { get; set; }
    }


    public class Commit_DesignTime : Commit
    {
        public Commit_DesignTime()
        {
            Time = DateTime.Now;
            Changes = new List<Change>()
            {

               new Change(){Name = "Weight1", NewValue = 100.9, OldValue = 41},
               new Change(){Name = "Output1", NewValue = "input.x1 * weight.w1", OldValue = "input.x1 * weight.w1 + weight.w2"},
           };
        }
    }
}
