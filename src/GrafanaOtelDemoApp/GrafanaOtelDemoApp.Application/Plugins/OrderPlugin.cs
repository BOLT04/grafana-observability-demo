namespace GrafanaOtelDemoApp.Application.Plugins
{
    using Microsoft.SemanticKernel;
    using System.ComponentModel;
    public sealed class OrderPlugin
    {
        [KernelFunction, Description("Place an order for the specified item.")]
        public string PlaceOrder([Description("The name of the item to be ordered.")] string itemName)
        {
            Task.Delay(2000).Wait();
            return "success";
        }
    }
}
