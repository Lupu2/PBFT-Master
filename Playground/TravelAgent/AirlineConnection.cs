using System;
using Cleipnir.ExecutionEngine.Providers;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;

namespace Playground.TravelAgent
{
    public class AirlineConnection : IPropertyPersistable
    {
        public async CTask<Guid> Book(string bookingInformation)
        {
            await Sleep.Until(1000);
            var flightId = Guid.NewGuid();
            Console.WriteLine($"AIRLINE - Booked flight with id: {flightId} for booking: {bookingInformation}");
            return flightId;
        }

        public async CTask CancelBooking(Guid bookingId)
        {
            Console.WriteLine($"AIRLINE - Cancelled Flight with booking id: {bookingId}");
            await Sleep.Until(1000);
        }
    }
}