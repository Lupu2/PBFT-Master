using System;
using Cleipnir.Helpers;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;

namespace Playground.TravelAgent
{
    public class Workflow : IPropertyPersistable
    {
        public BankConnection BankConnection { get; set; } = new BankConnection();
        public AirlineConnection AirlineConnection { get; set; } = new AirlineConnection();
        public HotelConnection HotelConnection { get; set; } = new HotelConnection();
        
        public async CTask Do()
        {
            Console.WriteLine("TRAVEL AGENT - Workflow Initialized");
            var compensatingActions = new CList<Func<CTask>>();

            try
            {
                var reservationId = await BankConnection.ReserveFunds(1_000, "1234-1234-1234-1234");
                compensatingActions.Add(() => BankConnection.CancelReservation(reservationId));
                var carBookingId = await AirlineConnection.Book("Mazda 2 Sport");
                compensatingActions.Add(() => AirlineConnection.CancelBooking(carBookingId));
                var hotelBookingId = await HotelConnection.Book("Hotel Dangleterre Suite");
                compensatingActions.Add(() => HotelConnection.CancelBooking(hotelBookingId));
                await BankConnection.DeductFunds(reservationId);
            }
            catch (Exception)
            {
                compensatingActions.ForEach(a => a());
                throw;
            }

            Console.WriteLine("TRAVEL AGENT - Workflow completed!");
        }
    }
}