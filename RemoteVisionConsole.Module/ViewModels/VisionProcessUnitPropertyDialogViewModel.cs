using Prism.Services.Dialogs;
using System.Collections.Generic;

namespace RemoteVisionConsole.Module.ViewModels
{
    public class VisionProcessUnitPropertyDialogViewModel : DialogViewModelBase
    {
        private IEnumerable<PropertyItem> _properties;
        public IEnumerable<PropertyItem> Properties
        {
            get { return _properties; }
            set { SetProperty(ref _properties, value); }
        }

        public override void OnDialogOpened(IDialogParameters parameters)
        {
            var propNames = new[] { "serverAddress", "adapterAssembly",
                "adapterType", "processorAssembly", "processorType" };

            var output = new List<PropertyItem>();
            foreach (var name in propNames)
            {
                var propValue = parameters.GetValue<string>(name);
                if (!string.IsNullOrEmpty(propValue)) output.Add(new PropertyItem { Name = name, Value = propValue });
            }

            Properties = output;
        }
    }
}
