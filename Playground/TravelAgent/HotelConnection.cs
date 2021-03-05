using System;
using Cleipnir.ExecutionEngine.Providers;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;

namespace Playground.TravelAgent
{
    public class HotelConnection : IPropertyPersistable
    {
        public async CTask<Guid> Book(string bookingInformation)
        {
            await Sleep.Until(1000);
            var bookingId = Guid.NewGuid();
            Console.WriteLine($"HOTEL - Booked Hotel with id: {bookingId} for booking: {bookingInformation}");
            return bookingId;
        }

        public async CTask CancelBooking(Guid bookingId)
        {
            await Sleep.Until(1000);
            Console.WriteLine($"HOTEL - Cancelled hotel booking with id: {bookingId}");
        }
    }
}