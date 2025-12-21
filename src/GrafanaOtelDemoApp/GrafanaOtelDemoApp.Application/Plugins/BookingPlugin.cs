namespace GrafanaOtelDemoApp.Application.Plugins
{
    using Microsoft.SemanticKernel;
    using System.ComponentModel;
    public sealed class BookingPlugin
    {
        [KernelFunction("FindAvailableRooms")]
        [Description("Finds available conference rooms for today.")]
        public async Task<List<string>> FindAvailableRoomsAsync()
        {
            // Simulate a remote call to a booking system.
            await Task.Delay(1000);
            return ["Room 101", "Room 201", "Room 301"];
        }

        [KernelFunction("BookRoom")]
        [Description("Books a conference room.")]
        public async Task<string> BookRoomAsync(string room)
        {
            // Simulate a remote call to a booking system.
            await Task.Delay(1000);
            return $"Room {room} booked.";
        }
    }
}
