namespace DistributedVisionRunner.Module.ChangeTracking
{
    public class Change
    {
        public string Name { get; set; }
        public object NewValue { get; set; }
        public object OldValue { get; set; }

  
    }

    public class Change_DesignTime : Change
    {
        public Change_DesignTime()
        {
            Name = "Weight1";
            NewValue = 100.0;
            OldValue = 50.1;
        }
    }
}