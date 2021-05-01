using System;
using Cleipnir.ExecutionEngine.Providers;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;

namespace Playground.TravelAgent
{
    public class BankConnection : IPropertyPersistable
    {
        public async CTask<Guid> ReserveFunds(int amount, string creditCardInformation)
        {
            await Sleep.Until(1000);
            var id = Guid.NewGuid();
            Console.WriteLine($"BANK - Reserved amount {amount} for credit card: {creditCardInformation} with id: {id}");
            return id;
        }

        public async CTask CancelReservation(Guid reservationId)
        {
            await Sleep.Until(1000);
            Console.WriteLine($"BANK - Cancelled credit card reservation with id: {reservationId}");
        }

        public async CTask DeductFunds(Guid reservationId)
        {
            await Sleep.Until(1000);
            Console.WriteLine($"BANK - Deducted funds from credit card with reservation id: {reservationId}");
        }
    }
}